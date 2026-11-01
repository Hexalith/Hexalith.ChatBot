using System.Security.Cryptography;
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

    /// <summary>Retains the existing SDK authority, trust validator and receiver guard.</summary>
    public static void Configure(IServiceCollection services)
    {
        services.RemoveAll<IRouteCatalogStateStore>();
        services.AddSingleton<IRouteCatalogStateStore, HttpRouteCatalogStateStore>();
        services.AddSingleton<HttpRouteCatalogFixture>();
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
            RouteCatalogEnvelope envelope = RouteCatalogCodec.Seal(new(2, 1, "Development", false,
                owner.Declarations, owner.Targets, consumer.AdmissionPolicies!, "", ""));
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

    private static Task<RouteCatalogHostObservation> ObserveOwnerAsync(IServiceProvider services, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        IReadOnlyList<RouteContractDeclaration> declarations = CompiledRouteCatalogDeclarations.Read([typeof(ChatBotCatalogDeclarations).Assembly]);
        var endpoints = services.GetRequiredService<EndpointDataSource>().Endpoints;
        string appId = services.GetRequiredService<IOptions<DomainProjectionIdentityOptions>>().Value.AppId;
        return Task.FromResult(Observation(services, appId, declarations,
            DevelopmentDomainRouteCatalog.Targets(services, endpoints, declarations), [],
            RouteCatalogDomainObservation.Capabilities(services), RouteCatalogDomainObservation.Auxiliary(services, endpoints)));
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
        byte[][] topology = [JsonSerializer.SerializeToUtf8Bytes(new
        {
            kind = "Component", metadata = new { name = coordinates.StateStore },
            spec = new { type = "state.redis", metadata = new[] { new { name = "keyPrefix", value = "none" } } }, scopes = hosts,
        }), JsonSerializer.SerializeToUtf8Bytes(new
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
            }), admission, capabilities, auxiliary, coordinates);
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
