using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.EventStore.Aspire;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.DependencyInjection;

using YamlDotNet.Serialization;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>Owns a genuinely new business backend, private catalog custody and actual compiled-owner observations.</summary>
internal sealed class LiveCatalogFixture : IAsyncDisposable
{
    private readonly string _directory;
    private readonly string _runId;
    private readonly string _containerName;
    private readonly List<DevelopmentRouteCatalogPreparedOwner> _owners = [];
    private DevelopmentRouteCatalogComposition? _catalog;
    private string? _mtlsRunDirectory;
    private string? _certificateDirectory;
    private bool _disposed;

    private LiveCatalogFixture(string directory, string runId)
    {
        _directory = directory;
        _runId = runId;
        _containerName = "chatbot-live-store-" + runId;
    }

    /// <summary>Gets the actual endpoint of this fixture's newly created private Redis backend.</summary>
    public string RedisHost { get; private set; } = string.Empty;

    /// <summary>Gets the Tenants hosted bootstrap receipt path inside this run's private catalog directory.</summary>
    public string BootstrapReceiptPath =>
        _catalog is null
            ? throw new InvalidOperationException("The live catalog has not been composed.")
            : Path.Combine(_catalog.Directory, "tenants.bootstrap.json");

    /// <summary>Creates fresh owner material only alongside a newly created, labeled and isolated business backend.</summary>
    public static async Task<LiveCatalogFixture> CreateAsync(CancellationToken cancellationToken)
    {
        string directory = Directory.CreateTempSubdirectory("chatbot-live-catalog-").FullName;
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        LiveCatalogFixture fixture = new(directory, Guid.NewGuid().ToString("N"));
        try
        {
            _ = await RunAsync(["run", "--detach", "--name", fixture._containerName,
                "--label", "io.hexalith.chatbot-live.run=" + fixture._runId,
                "--label", "io.hexalith.chatbot-live.scope=" + Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar),
                "--label", "io.hexalith.chatbot-live.custody=" + fixture._directory,
                "--tmpfs", "/data:rw,nosuid,nodev,noexec,size=64m", "--publish", "127.0.0.1::6379",
                "redis:7.4.2", "redis-server", "--appendonly", "yes", "--save", ""], cancellationToken).ConfigureAwait(false);
            using (JsonDocument storage = JsonDocument.Parse(await RunAsync(["inspect", "--format",
                "{\"mounts\":{{json .Mounts}},\"tmpfs\":{{json .HostConfig.Tmpfs}},\"id\":{{json .Id}}}", fixture._containerName], cancellationToken).ConfigureAwait(false)))
            {
                JsonElement actual = storage.RootElement;
                if (actual.GetProperty("mounts").EnumerateArray().Any(mount => mount.GetProperty("Type").GetString() is "volume" or "bind")
                    || !actual.GetProperty("tmpfs").TryGetProperty("/data", out _))
                {
                    throw new InvalidOperationException("The new business backend must use its own tmpfs and cannot reuse a bind or persistent volume.");
                }

                Console.WriteLine("CHATBOT_LIVE_BACKEND_OWNERSHIP " + JsonSerializer.Serialize(new
                {
                    runId = fixture._runId, containerId = actual.GetProperty("id").GetString(), storage = "new-private-tmpfs",
                }));
            }
            string endpoint = (await RunAsync(["port", fixture._containerName, "6379/tcp"], cancellationToken).ConfigureAwait(false)).Trim();
            if (!endpoint.StartsWith("127.0.0.1:", StringComparison.Ordinal)
                || !int.TryParse(endpoint[10..], out int port) || port is <= IPEndPoint.MinPort or > IPEndPoint.MaxPort)
            {
                throw new InvalidOperationException("The owned business backend did not expose one loopback endpoint.");
            }

            fixture.RedisHost = endpoint;
            Stopwatch readiness = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    string response = await RunAsync(["exec", fixture._containerName, "redis-cli", "PING"], cancellationToken).ConfigureAwait(false);
                    if (response.Trim() != "PONG") throw new InvalidOperationException("The owned backend did not answer its readiness probe.");
                    break;
                }
                catch (InvalidOperationException) when (readiness.Elapsed < TimeSpan.FromSeconds(15))
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }
            return fixture;
        }
        catch (Exception startupFailure)
        {
            try { await fixture.DisposeAsync().ConfigureAwait(false); }
            catch (Exception cleanupFailure) { throw new AggregateException("Owned backend startup and cleanup failed.", startupFailure, cleanupFailure); }
            throw;
        }
    }

    /// <summary>Enrolls actual compiled owners and exact finite traffic through the public SDK before model construction.</summary>
    public async Task ConfigureAsync(IDistributedApplicationTestingBuilder builder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(builder);
        foreach (ProjectResource project in builder.Resources.OfType<ProjectResource>())
        {
            foreach (DaprSidecarAnnotation sidecar in project.Annotations.OfType<DaprSidecarAnnotation>())
            {
                DaprSidecarOptionsAnnotation options = sidecar.Sidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>().Single();
                string source = options.Options.Config ?? throw new InvalidOperationException("The selected sidecar lacks its configuration.");
                JsonObject configuration = PrepareSidecarConfiguration(
                    File.ReadAllText(source), Path.Combine(_directory, "discovery.db"));
                string path = Path.Combine(_directory, project.Name + ".configuration.json");
                await WritePrivateAsync(path, JsonSerializer.SerializeToUtf8Bytes(configuration), cancellationToken).ConfigureAwait(false);
                sidecar.Sidecar.Annotations[sidecar.Sidecar.Annotations.IndexOf(options)] = new DaprSidecarOptionsAnnotation(options.Options with { Config = path });
            }
        }

        string sentryConfiguration = Path.Combine(_directory, "sentry.configuration.json");
        await WritePrivateAsync(sentryConfiguration, JsonSerializer.SerializeToUtf8Bytes(new
        {
            apiVersion = "dapr.io/v1alpha1", kind = "Configuration", metadata = new { name = "chatbot-live-sentry" },
            spec = new { mtls = new { enabled = true, workloadCertTTL = "24h" } },
        }), cancellationToken).ConfigureAwait(false);
        string repositoryRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../.."));
        (_mtlsRunDirectory, _certificateDirectory) = global::DaprSelfHostedMtls.AddLocalApplicationInfrastructure(builder, repositoryRoot, sentryConfiguration);

        foreach (ContainerResource container in builder.Resources.OfType<ContainerResource>())
        {
            builder.CreateResourceBuilder(container).WithContainerRuntimeArgs(
                "--label", "io.hexalith.chatbot-live.run=" + _runId,
                "--label", "io.hexalith.chatbot-live.scope=" + Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar),
                "--label", "io.hexalith.chatbot-live.custody=" + _directory,
                "--label", "io.hexalith.chatbot-live.role=aspire-resource");
        }


        IResourceBuilder<ProjectResource> Project(string name)
            => builder.CreateResourceBuilder(builder.Resources.OfType<ProjectResource>().Single(resource => resource.Name == name));
        IResourceBuilder<ProjectResource> gateway = Project("eventstore");
        gateway.WithEnvironment("Authentication__BoundCommand__AllowedDomains__tenants", "global-administrators")
            .WithEnvironment("EventStore__Projections__WorkersEnabled", "false")
            .WithEnvironment("EventStore__Projections__DefaultRefreshIntervalMs", "0")
            .WithEnvironment("EventStore__Projections__Domains__global-administrators__RefreshIntervalMs", "0")
            .WithEnvironment("EventStore__Projections__Domains__tenants__RefreshIntervalMs", "0")
            .WithEnvironment("EventStore__Projections__Domains__projects__RefreshIntervalMs", "0")
            .WithEnvironment("EventStore__Projections__Domains__chatbot__RefreshIntervalMs", "0")
            .WithEnvironment("EventStore__Projections__Domains__party__RefreshIntervalMs", "0")
            .WithEnvironment("EventStore__ProjectionDispatch__CatalogRefreshInterval", "01:00:00");
        // Catalog composition later sets DOTNET_ENVIRONMENT on every receiver. Capture that value before
        // PrepareEventStoreDevelopmentOwnerAsync so ObserveStarts does not treat the same assignment as drift.
        foreach (string name in new[] { "tenants", "parties", "projects", "chatbot" })
        {
            Project(name)
                .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
                .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");
        }

        // Catalog composition sets DevelopmentBootstrap:Directory on Projects. That host then requires exactly
        // one of SupportedReads or IsolatedLiveFixtures; this umbrella enrolls Projects as a catalog owner.
        Project("projects").WithEnvironment("Projects__SupportedReads__Enabled", "true");

        foreach ((string name, string? declarations, string? adapters) in new[]
        {
            ("tenants", "Hexalith.Tenants.Contracts.dll", "Hexalith.Tenants.Server.dll"),
            ("parties", "Hexalith.Parties.Contracts.dll", "Hexalith.Parties.EventStoreExtensions.dll"),
            ("projects", (string?)null, (string?)null),
            ("chatbot", (string?)null, (string?)null),
        })
        {
            // Tenants and Parties enroll their declarations/adapters in separate real runtime
            // dependencies. The public split-artifact overload verifies both against this child.
            // ChatBot keeps declarations and adapters on the same compiled server image.
            _owners.Add(declarations is null
                ? await Project(name).PrepareEventStoreDevelopmentOwnerAsync(TimeSpan.FromMinutes(3), cancellationToken).ConfigureAwait(false)
                : await Project(name).PrepareEventStoreDevelopmentOwnerAsync(declarations, adapters!, TimeSpan.FromMinutes(3), cancellationToken).ConfigureAwait(false));
        }

        IResourceBuilder<ProjectResource> tenants = Project("tenants");
        IResourceBuilder<ProjectResource> parties = Project("parties");
        IResourceBuilder<ProjectResource> projects = Project("projects");
        IResourceBuilder<ProjectResource> chatbot = Project("chatbot");
        DevelopmentRouteCatalogInvocation[] DomainCalls(params string[] methods) => methods
            .Select(method => new DevelopmentRouteCatalogInvocation(gateway, method, ["POST"]))
            .ToArray();
        DevelopmentRouteCatalogReceiver[] receivers =
        [
            new(gateway,
            [
                new(tenants, "api/v1/commands/bound/workload", ["POST"]),
                new(tenants, "api/v1/streams/read/workload", ["POST"]),
                new(parties, "api/v1/commands/bound/workload", ["POST"]),
                new(parties, "api/v1/streams/read/workload", ["POST"]),
                new(projects, "api/v1/queries/owner-authority/workload", ["POST"]),
                new(projects, "api/v1/streams/read/workload", ["POST"]),
                new(projects, "api/v1/commands", ["POST"]),
                new(chatbot, "api/v1/commands/bound/workload", ["POST"]),
                new(chatbot, "api/v1/commands", ["POST"]),
                new(chatbot, "api/v1/streams/read/workload", ["POST"]),
                new(chatbot, "api/v1/queries/owner-authority/workload", ["POST"]),
            ]) { TechnicalHttp = true },
            new(tenants, DomainCalls("process", "replay-state", "query", "project", "admin/operational-index-metadata")),
            new(parties, DomainCalls("process", "replay-state", "query", "project", "admin/operational-index-metadata")),
            new(projects, DomainCalls("process", "query", "project", "project/v2", "project/rebuild/v1", "admin/operational-index-metadata")),
            new(chatbot, EventStoreDomainServiceRoutes.Operational
                .Select(route => new DevelopmentRouteCatalogInvocation(gateway, route.Route.Trim('/'), ["POST"]))
                .ToArray()),
        ];

        string component = Path.Combine(_directory, "business-store.component.json");
        await WritePrivateAsync(component, JsonSerializer.SerializeToUtf8Bytes(new
        {
            apiVersion = "dapr.io/v1alpha1", kind = "Component", metadata = new { name = "statestore" },
            spec = new { type = "state.redis", version = "v1", metadata = new[]
            {
                new { name = "redisHost", value = RedisHost }, new { name = "actorStateStore", value = "true" }, new { name = "keyPrefix", value = "none" },
            } },
        }), cancellationToken).ConfigureAwait(false);
        // This key ring is created with the actual new container above, never with default dapr-init Redis.
        DevelopmentRouteCatalogCompositionOptions custody = new(Path.Combine(_directory, "owner-custody"),
            "chatbot-route-catalog", "catalog-authority", "fixture-v1",
            new Dictionary<string, string> { ["fixture-v1"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }, "fixture-v1");
        _catalog = builder.AddEventStoreDevelopmentRouteCatalog(gateway, _owners.Select(owner => owner.Owner).ToArray(), receivers, component, custody);
        Project("tenants").WithEnvironment(
            "Tenants__BootstrapBoundWorkloadReceiptPath",
            Path.Combine(_catalog.Directory, "tenants.bootstrap.json"));
        builder.Services.AddSingleton<LiveCatalogFixture>(_ => this);
    }

    /// <summary>Applies the fixture's mTLS and isolated discovery settings to a sidecar configuration.</summary>
    internal static JsonObject PrepareSidecarConfiguration(string yaml, string discoveryDatabase)
    {
        object? document = new DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization().Build()
            .Deserialize<object>(yaml);
        if (JsonNode.Parse(new SerializerBuilder().JsonCompatible().Build().Serialize(document)) is not JsonObject configuration)
        {
            throw new InvalidOperationException("The selected sidecar configuration must be an object.");
        }

        if (!configuration.TryGetPropertyValue("spec", out JsonNode? configuredSpec))
        {
            configuredSpec = new JsonObject();
            configuration["spec"] = configuredSpec;
        }

        if (configuredSpec is not JsonObject spec)
        {
            throw new InvalidOperationException("The selected sidecar configuration spec must be an object.");
        }

        spec["mtls"] = new JsonObject { ["enabled"] = true };
        spec["nameResolution"] = new JsonObject
        {
            ["component"] = "sqlite", ["version"] = "v1",
            ["configuration"] = new JsonObject { ["connectionString"] = discoveryDatabase },
        };
        return configuration;
    }

    /// <summary>Retains genuine custody until the application stops, then removes only this fixture's backend and private inputs.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        List<Exception> failures = [];
        async Task CleanupAsync(Func<Task> action)
        {
            try { await action().ConfigureAwait(false); }
            catch (Exception exception) { failures.Add(exception); }
        }

        if (_catalog is not null) await CleanupAsync(() => _catalog.DisposeAsync().AsTask()).ConfigureAwait(false);
        foreach (DevelopmentRouteCatalogPreparedOwner owner in _owners)
        {
            await CleanupAsync(() => owner.DisposeAsync().AsTask()).ConfigureAwait(false);
        }

        bool backendRemoved = false;
        await CleanupAsync(async () =>
        {
            string present = (await RunAsync(["ps", "--all", "--filter", "name=^/" + _containerName + "$", "--format", "{{.Names}}"], CancellationToken.None).ConfigureAwait(false)).Trim();
            if (present.Length == 0) { backendRemoved = true; return; }
            if (present != _containerName) throw new InvalidOperationException("The owned backend name is ambiguous.");
            string label = (await RunAsync(["inspect", "--format", "{{index .Config.Labels \"io.hexalith.chatbot-live.run\"}}", _containerName], CancellationToken.None).ConfigureAwait(false)).Trim();
            if (label != _runId) throw new InvalidOperationException("The business backend ownership label changed.");
            _ = await RunAsync(["rm", "--force", "--volumes", _containerName], CancellationToken.None).ConfigureAwait(false);
            string remaining = await RunAsync(["ps", "--all", "--filter", "name=^/" + _containerName + "$", "--format", "{{.Names}}"], CancellationToken.None).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(remaining)) throw new InvalidOperationException("Owned business backend removal was not verified.");
            backendRemoved = true;
            Console.WriteLine("CHATBOT_LIVE_BACKEND_REMOVED " + _runId);
        }).ConfigureAwait(false);
        IEnumerable<string> privateDirectories = new[] { _certificateDirectory, _mtlsRunDirectory }.OfType<string>();
        // Native controllers may finish disposing after this singleton. Leave private custody
        // for job cleanup unless every task-owned container is already confirmed absent.
        string remainingOwned = await RunAsync(["ps", "--all", "--filter", "label=io.hexalith.chatbot-live.run=" + _runId,
            "--format", "{{.ID}}"], CancellationToken.None).ConfigureAwait(false);
        if (backendRemoved && string.IsNullOrWhiteSpace(remainingOwned)) privateDirectories = privateDirectories.Append(_directory);
        foreach (string directory in privateDirectories.Distinct(StringComparer.Ordinal))
        {
            await CleanupAsync(() => { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); return Task.CompletedTask; }).ConfigureAwait(false);
        }

        if (failures.Count > 0) throw new AggregateException("Owned live-catalog fixture cleanup failed.", failures);
    }

    private static async Task WritePrivateAsync(string path, byte[] bytes, CancellationToken cancellationToken)
    {
        FileStreamOptions options = new() { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.Read };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        FileStream stream = new(path, options);
        await using var streamLifetime = stream.ConfigureAwait(false);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        ProcessStartInfo start = new("docker") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Unable to start the owned fixture operation.");
        Task<string> output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> error = process.StandardError.ReadToEndAsync(cancellationToken);
        try { await process.WaitForExitAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(60), cancellationToken).ConfigureAwait(false); }
        catch
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        string text = await output.ConfigureAwait(false);
        string failure = await error.ConfigureAwait(false);
        if (process.ExitCode != 0) throw new InvalidOperationException("Owned fixture operation failed: " + failure);
        return text;
    }
}
