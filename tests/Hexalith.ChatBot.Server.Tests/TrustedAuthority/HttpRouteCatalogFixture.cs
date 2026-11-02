using System.Security.Cryptography;
using System.Reflection;

using Hexalith.EventStore.Client.Routing;
using Hexalith.EventStore.Client.Discovery;
using Hexalith.EventStore.Client.Handlers;
using System.Text.Json;

using Hexalith.ChatBot.Server.Routing;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.Configuration;
using Hexalith.EventStore.Server.Routing;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hexalith.ChatBot.Server.Tests.TrustedAuthority;

/// <summary>Commits an isolated Development catalog from real owner receivers and installed SDK consumer policies.</summary>
internal sealed class HttpRouteCatalogFixture : IDisposable
{
    private const string ConsumerAppId = "chatbot-http-fixture-consumer";
    private readonly SemaphoreSlim _activation = new(1, 1);
    private RouteCatalogGenerationReadback? _generation;
    private ServiceProvider? _consumer;

    /// <summary>Allows boundary controls to send missing or stale authority without automatic fixture headers.</summary>
    public bool AutomaticActivation { get; set; } = true;

    /// <summary>Allows negative controls to corrupt an actual SDK invocation after sender proof generation.</summary>
    public Action<HttpRequestMessage>? TransformInvocation { get; set; }

    /// <summary>Retains the existing SDK authority, trust validator and receiver guard.</summary>
    public static void Configure(IServiceCollection services)
    {
        // This fixture drives prepare/readiness/commit itself. Do not let the independent file coordinator
        // overwrite its observed host identity while an in-process activation is being prepared.
        foreach (ServiceDescriptor descriptor in services.Where(static descriptor =>
            descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationFactory?.Method.DeclaringType?.DeclaringType == typeof(DevelopmentRouteCatalogBootstrap)).ToArray())
        {
            services.Remove(descriptor);
        }
        services.RemoveAll<IRouteCatalogStateStore>();
        services.AddSingleton<IRouteCatalogStateStore, HttpRouteCatalogStateStore>();
        services.AddSingleton<HttpRouteCatalogFixture>();
        services.AddSingleton<FiniteRouteCatalogReceiver>();
        services.AddSingleton<IStartupFilter, HttpRouteCatalogStartupFilter>();
        services.AddSingleton<IOptions<RouteCatalogHostOptions>>(Options.Create(new RouteCatalogHostOptions
        {
            Profile = "Development", DevelopmentFixture = true,
        }));
        services.AddEventStoreRouteCatalogObservation(async (provider, token) =>
        {
            using IServiceScope scope = provider.CreateScope();
            return await ObserveOwnerAsync(scope.ServiceProvider, token).ConfigureAwait(false);
        });
    }

    /// <summary>Runs prepare, independent readiness for both participants, and commit through the SDK authority.</summary>
    public async Task<RouteCatalogGenerationReadback> ActivateAsync(IServiceProvider services, CancellationToken token)
    {
        await _activation.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (_generation is not null) { return _generation; }
            using IServiceScope scope = services.CreateScope();
            IServiceProvider provider = scope.ServiceProvider;
            RouteCatalogHostObservation owner = await ObserveOwnerAsync(provider, token).ConfigureAwait(false);
            RouteCatalogHostObservation consumer = await ObserveConsumerAsync(provider, token).ConfigureAwait(false);
            RouteCatalogHostIdentity[] hosts = [ObservedRouteCatalogHost.Identity(owner), ObservedRouteCatalogHost.Identity(consumer)];
            IOptions<RouteCatalogHostOptions> ownerOptions = provider.GetRequiredService<IOptions<RouteCatalogHostOptions>>();
            ownerOptions.Value.Identity = hosts[0];
            RouteCatalogEnvelope envelope = VersionedRouteCatalogBuilder.Build(new RouteCatalogGenerationInput(
                1, "Development", owner.Declarations, owner.Operations!,
                owner.Targets.Select(target => new RouteCompositionBinding(target.RouteId, target.AppId,
                    target.Method, target.ContractVersion, target.IsRuntimeOverride, target.ResolverTenantPattern)).ToArray(),
                consumer.AdmissionPolicies!));
            RouteCatalogActivation candidate = new("chatbot-http-fixture", RouteCatalogCodec.Encode(envelope),
                RouteCatalogCodec.ComputeRootDigest(envelope), null, hosts, "Development-fixture", "",
                owner.AuxiliaryOperations, owner.ProjectionCapabilities);
            candidate = candidate with { TrustProof = RouteCatalogActivationBinding.ComputeDigest(candidate) };
            IRouteCatalogAuthority authority = provider.GetRequiredService<IRouteCatalogAuthority>();
            await authority.PrepareAsync(candidate, token).ConfigureAwait(false);
            await authority.ReadyAsync(candidate.AttemptId, token).ConfigureAwait(false);
            IOptions<RouteCatalogHostOptions> consumerOptions = Options.Create(new RouteCatalogHostOptions
            {
                Identity = hosts[1], Profile = "Development", DevelopmentFixture = true,
            });
            RouteCatalogAuthority consumerAuthority = new(provider.GetRequiredService<IRouteCatalogStateStore>(),
                new DevelopmentRouteCatalogTrustValidator(consumerOptions, provider.GetRequiredService<IHostEnvironment>()),
                consumerOptions, new ObservedRouteCatalogHost(cancellation => ObserveConsumerAsync(provider, cancellation)));
            await consumerAuthority.ReadyAsync(candidate.AttemptId, token).ConfigureAwait(false);
            await authority.CommitAsync(candidate.AttemptId, token).ConfigureAwait(false);
            _generation = await authority.ReadbackAsync(token).ConfigureAwait(false);
            return _generation;
        }
        finally { _activation.Release(); }
    }

    /// <summary>Captures actual installed adapters and proves availability of each configured disposable fixture key.</summary>
    public Task<IReadOnlyList<IdempotencyCatalogEntry>> ObserveAdmissionAsync(IServiceProvider services, CancellationToken token)
        => RouteCatalogAdmissionObservation.CaptureAsync(Consumer(services), ConsumerAppId, token);

    /// <inheritdoc/>
    public void Dispose()
    {
        _consumer?.Dispose();
        _activation.Dispose();
    }

    private static async Task<RouteCatalogHostObservation> ObserveOwnerAsync(IServiceProvider services, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        FiniteRouteCatalogReceiver finite = services.GetRequiredService<FiniteRouteCatalogReceiver>();
        await finite.WaitForCaptureAsync(token).ConfigureAwait(false);
        IReadOnlyList<RouteContractDeclaration> declarations = CompiledRouteCatalogDeclarations.Read([typeof(ChatBotCatalogDeclarations).Assembly]);
        var endpoints = services.GetRequiredService<EndpointDataSource>().Endpoints;
        string appId = services.GetRequiredService<IOptions<DomainProjectionIdentityOptions>>().Value.AppId;
        return Observation(services, appId, declarations,
            DevelopmentDomainRouteCatalog.Targets(services, endpoints, declarations), [],
            RouteCatalogDomainObservation.Capabilities(services), RouteCatalogDomainObservation.Auxiliary(services, endpoints)) with
        {
            ReceiverSurface = finite.Observe(),
            Operations = CompiledRouteCatalogDeclarations.ReadOperations([typeof(ChatBotCatalogDeclarations).Assembly]),
        };
    }

    private async Task<RouteCatalogHostObservation> ObserveConsumerAsync(IServiceProvider services, CancellationToken token)
        => Observation(services, ConsumerAppId, [], [], await ObserveAdmissionAsync(services, token).ConfigureAwait(false), [], []);

    private static RouteCatalogHostObservation Observation(IServiceProvider services, string appId,
        IReadOnlyList<RouteContractDeclaration> declarations, IReadOnlyList<RouteCatalogEntry> targets,
        IReadOnlyList<IdempotencyCatalogEntry> admission, IReadOnlyList<RouteCatalogProjectionCapability> capabilities,
        IReadOnlyList<RouteCatalogAuxiliaryOperation> auxiliary)
    {
        string owner = services.GetRequiredService<IOptions<DomainProjectionIdentityOptions>>().Value.AppId;
        string[] hosts = [owner, ConsumerAppId];
        RouteCatalogStateCoordinates coordinates = services.GetRequiredService<IRouteCatalogStateStore>().Coordinates!;
        // These documents describe this in-process authority cell only; no deployed Dapr component is claimed.
        HttpRouteCatalogFixtureFiles files = services.GetRequiredService<HttpRouteCatalogFixtureFiles>();
        byte[][] topology = [files.Component, appId == owner ? files.AccessControl : JsonSerializer.SerializeToUtf8Bytes(new
        {
            spec = new { accessControl = new { defaultAction = "deny", trustDomain = "public" } },
        })];
        var scheme = services.GetRequiredService<IOptionsMonitor<AuthenticationSchemeOptions>>().Get(SdkRouteFixtureAuthorization.SchemeName);
        return new(appId, appId, declarations, targets, hosts, topology,
            JsonSerializer.SerializeToUtf8Bytes(new
            {
                environment = services.GetRequiredService<IHostEnvironment>().EnvironmentName,
                transport = SdkRouteFixtureAuthorization.SchemeName, scheme.ClaimsIssuer,
                consumer = nameof(ConfigurationIdempotencyDigestKeyProvider),
            }), admission, capabilities, auxiliary, coordinates, Operations: [])
        {
            ExecutableOwners = ExecutableOwners(services, appId == owner),
        };
    }

    private static IReadOnlyDictionary<string, string> ExecutableOwners(IServiceProvider services, bool owner)
    {
        Assembly[] assemblies;
        if (owner)
        {
            DiscoveryResult discovery = services.GetRequiredService<DiscoveryResult>();
            assemblies = [typeof(ChatBotCatalogDeclarations).Assembly, typeof(DevelopmentDomainRouteCatalog).Assembly,
                typeof(RouteCatalogInvocationHandler).Assembly, typeof(HttpRouteCatalogFixture).Assembly,
                .. discovery.Aggregates.Select(aggregate => aggregate.Type.Assembly),
                .. discovery.Aggregates.Select(aggregate => services.GetRequiredKeyedService<IDomainProcessor>(aggregate.DomainName).GetType().Assembly),
                .. ChatBotCatalogDeclarations.CommandTypes.Select(type => type.Assembly),
                .. services.GetServices<IDomainQueryHandler>().Select(handler => handler.GetType().Assembly),
                .. services.GetServices<IDomainProjectionHandler>().Select(handler => handler.GetType().Assembly)];
        }
        else
        {
            assemblies = [typeof(ConfigurationIdempotencyDigestKeyProvider).Assembly,
                typeof(RouteCatalogAdmissionObservation).Assembly, typeof(RouteCatalogInvocationHandler).Assembly,
                .. ChatBotCatalogAdapters.Create().Select(adapter => adapter.GetType().Assembly)];
        }
        return assemblies.Distinct().OrderBy(assembly => assembly.FullName, StringComparer.Ordinal)
            .ToDictionary(assembly => assembly.FullName!, assembly =>
                Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly.Location))), StringComparer.Ordinal);
    }

    private ServiceProvider Consumer(IServiceProvider owner)
    {
        if (_consumer is not null) { return _consumer; }
        ServiceCollection services = new();
        services.AddSingleton(owner.GetRequiredService<IHostEnvironment>());
        foreach (IDeclaredIdempotencyIntentAdapter adapter in ChatBotCatalogAdapters.Create())
        {
            services.AddSingleton<IIdempotencyIntentAdapter>(adapter);
        }
        services.AddSingleton<IOptions<IdempotencyAdmissionOptions>>(Options.Create(new IdempotencyAdmissionOptions
        {
            Enabled = true, ActiveDigestKeyVersion = "fixture-v1", DigestKeySource = IdempotencyDigestKeySource.Configuration,
            DigestKeySecretStoreName = "fixture-configuration", DigestKeySecretName = "chatbot-fixture-ring", DigestKeySecretGeneration = "fixture-v1",
            DigestKeys = new(StringComparer.Ordinal) { ["fixture-v1"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) },
        }));
        services.AddSingleton<IIdempotencyDigestKeyProvider, ConfigurationIdempotencyDigestKeyProvider>();
        _consumer = services.BuildServiceProvider();
        return _consumer;
    }
}
