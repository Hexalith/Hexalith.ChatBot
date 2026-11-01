using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.EventStore.Aspire;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using YamlDotNet.Serialization;

namespace Hexalith.ChatBot.IntegrationTests.PolicyIsolation;

/// <summary>Run-owned test composition around the existing AppHost, with real owner commands and no admission substitutions.</summary>
internal sealed class PolicyRefusalPreflightTopology : IAsyncDisposable
{
    private readonly List<string> _containers = [];
    private readonly List<DevelopmentRouteCatalogPreparedOwner> _owners = [];
    private readonly Dictionary<string, string> _tokens = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _eventStoreTokens = new(StringComparer.Ordinal);
    private readonly Dictionary<string, byte[]> _policyBodies = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _artifactHashes = new(StringComparer.Ordinal);
    private readonly string _workspace;
    private byte[]? _sourceBinding;
    private byte[]? _immutableRequest;
    private string? _mtlsRunDirectory;
    private string? _mtlsCertificateDirectory;
    private int _diagnosticSequence;
    private bool _disposed;
    private IDistributedApplicationTestingBuilder? _builder;
    private DevelopmentRouteCatalogComposition? _catalog;
    private DistributedApplication? _application;
    private HttpClient? _eventStore;
    private HttpClient? _chatBot;

    /// <summary>Creates private handoff storage; secrets and native payloads are never retained in receipts.</summary>
    private PolicyRefusalPreflightTopology(string workspace)
    {
        _workspace = workspace;
        RunId = ChatBotCommandId.New().ToString().ToLowerInvariant();
        DirectoryPath = Directory.CreateTempSubdirectory("chatbot-policy-refusal-" + RunId + "-").FullName;
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(DirectoryPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    /// <summary>Gets the private per-run evidence exchange directory.</summary>
    internal string DirectoryPath { get; }

    /// <summary>Gets the unique producer run identity.</summary>
    internal string RunId { get; }

    /// <summary>Gets the exact current-candidate evidence request, finalized by the producer test.</summary>
    internal JsonObject Request { get; private set; } = new();

    /// <summary>Gets the run-owned application.</summary>
    internal DistributedApplication Application => _application ?? throw new InvalidOperationException("Preflight topology is not started.");

    /// <summary>Creates and prepares both real positive tenants, preserving unsupported prerequisites as failures.</summary>
    internal static async Task<PolicyRefusalPreflightTopology> StartAsync(CancellationToken token)
    {
        string workspace = FindWorkspace();
        var topology = new PolicyRefusalPreflightTopology(workspace);
        try
        {
            await topology.StartCoreAsync(token).ConfigureAwait(false);
            return topology;
        }
        catch (Exception exception)
        {
            List<Exception> failures = [new InvalidOperationException("Preflight preparation failed (" + exception.GetType().Name + ").")];
            try { await topology.RetainFailureAsync(exception).ConfigureAwait(false); }
            catch (Exception diagnostic) { failures.Add(new InvalidOperationException("Private preparation diagnostic failed (" + diagnostic.GetType().Name + ").")); }
            try
            {
                await topology.WriteAsync("blocked.json", new { topology.RunId, classification = "blocked-no-native-acceptance",
                    stage = "source-topology-or-real-owner-preparation", exceptionType = exception.GetType().Name,
                    nativeAcceptance = false, a6Accepted = false, te2Accepted = false }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception diagnostic) { failures.Add(new InvalidOperationException("Blocked receipt failed (" + diagnostic.GetType().Name + ").")); }
            finally
            {
                try { await topology.DisposeAsync().ConfigureAwait(false); }
                catch (Exception cleanup) { failures.Add(cleanup); }
            }
            throw new AggregateException("Preflight blocked; private diagnostics: " + topology.DirectoryPath, failures);
        }
    }

    /// <summary>Sends the exact prebound bytes and retains generic HTTP observations without asserting a policy-gate cause.</summary>
    internal async Task ObserveHttpDependencyAsync(string tenantId, string principalId, CancellationToken token)
    {
        await RecheckBindingAsync(token).ConfigureAwait(false);
        JsonNode submission = Request["submissions"]!.AsArray().Single(item => item!["tenantId"]!.GetValue<string>() == tenantId
            && item["principalId"]!.GetValue<string>() == principalId)!;
        string commandId = submission["commandId"]!.GetValue<string>();
        string correlationId = submission["correlationId"]!.GetValue<string>();
        string taskId = submission["taskId"]!.GetValue<string>();
        byte[] bytes = _policyBodies[principalId];
        if (PolicyRefusalNativeObservationVerifier.Hash(bytes) != submission["requestBodySha256"]!.GetValue<string>())
        {
            throw new InvalidOperationException("Preflight blocked: immutable request bytes changed.");
        }
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
        {
            Content = new ByteArrayContent(bytes),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[principalId]);
        request.Headers.Add("X-Correlation-ID", correlationId);
        request.Headers.Add("X-Hexalith-Task-Id", taskId);
        request.Headers.Add("X-Hexalith-Surface-Origin", "ui");
        using HttpResponseMessage response = await _chatBot!.SendAsync(request, token).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.ServiceUnavailable)
        {
            throw new InvalidOperationException("Preflight blocked: generic HTTP dependency response was not observed for " + tenantId + "; HTTP " + (int)response.StatusCode + ".");
        }
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false));
        JsonElement root = problem.RootElement;
        if (root.GetProperty("code").GetString() != "dependency_degraded" || !root.GetProperty("retryable").GetBoolean()
            || root.GetProperty("clientAction").GetString() != "retry-later"
            || root.GetProperty("correlationId").GetString() != correlationId || root.GetProperty("taskId").GetString() != taskId)
        {
            throw new InvalidOperationException("Policy refusal preflight blocked: canonical dependency semantics differ.");
        }
        await WriteAsync("http-" + principalId + ".json", new { RunId, tenantId, principalId, commandId, correlationId, taskId,
            requestBodySha256 = submission["requestBodySha256"]!.GetValue<string>(),
            status = 503, code = "dependency_degraded", retryable = true, clientAction = "retry-later",
            policyGateCauseVerified = false, classification = "generic-http-observation-only-unaccepted" }, token).ConfigureAwait(false);
    }

    /// <summary>Retains the exact missing owner observation seam; a generic 503 cannot prove that the policy gate ran.</summary>
    internal static void RequirePolicyGateCauseObservation()
        => throw new InvalidOperationException("Preflight blocked/unknown: ChatBot must provide an independently observable, exact-command/correlation/tenant/principal-bound policy-snapshot-unavailable admission cause. InMemoryAuditWriter.AuthorizationFailures has no supported cross-process reader; no production API or producer cause fact is authorized here.");

    /// <summary>Seals the full metadata/body/artifact binding before either observation or request exists.</summary>
    internal async Task SealRequestAsync(CancellationToken token)
    {
        await RecheckBindingAsync(token).ConfigureAwait(false);
        await WriteAsync("request.json", Request, token).ConfigureAwait(false);
        _immutableRequest = await File.ReadAllBytesAsync(Path.Combine(DirectoryPath, "request.json"), token).ConfigureAwait(false);
        Request["requestSha256"] = PolicyRefusalNativeObservationVerifier.Hash(_immutableRequest);
    }

    /// <summary>Waits for an externally supplied signed observation without creating or approving one.</summary>
    internal async Task<byte[]> WaitForObserverAsync(string phase, CancellationToken token)
    {
        string path = Path.Combine(DirectoryPath, phase + ".observer.json");
        while (!File.Exists(path)) { await Task.Delay(250, token).ConfigureAwait(false); }
        var info = new FileInfo(path);
        if (info.Length is <= 0 or > 16 * 1024 * 1024 || info.LinkTarget is not null)
        {
            throw new InvalidOperationException("Independent observation handoff is missing or invalid.");
        }
        return await File.ReadAllBytesAsync(path, token).ConfigureAwait(false);
    }

    /// <summary>Writes only metadata to the private exchange directory.</summary>
    internal async Task WriteAsync(string name, object value, CancellationToken token)
    {
        string path = Path.Combine(DirectoryPath, name);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, new JsonSerializerOptions { WriteIndented = true });
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.Read };
        if (!OperatingSystem.IsWindows()) { options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite; }
        FileStream stream = new(path, options);
        await using var streamLifetime = stream.ConfigureAwait(false);
        await stream.WriteAsync(bytes, token).ConfigureAwait(false);
    }

    /// <summary>Stops only this testing application and removes only containers carrying this exact run's label.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) { return; }
        _disposed = true;
        List<Exception> failures = [];
        JsonArray steps = [];
        foreach (byte[] body in _policyBodies.Values) { CryptographicOperations.ZeroMemory(body); }
        _policyBodies.Clear();
        _tokens.Clear();
        _eventStoreTokens.Clear();
        async Task CleanupAsync(string step, Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
                steps.Add(new JsonObject { ["step"] = step, ["removedOrDisposed"] = true });
            }
            catch (Exception exception)
            {
                failures.Add(new InvalidOperationException("Run-owned cleanup failed: " + step + " (" + exception.GetType().Name + ")."));
                steps.Add(new JsonObject { ["step"] = step, ["removedOrDisposed"] = false, ["exceptionType"] = exception.GetType().Name });
                try { await RetainFailureAsync(exception).ConfigureAwait(false); }
                catch (Exception diagnostic) { failures.Add(new InvalidOperationException("Private cleanup diagnostic failed: " + diagnostic.GetType().Name)); }
            }
        }
        await CleanupAsync("chatbot-http-client", () => { _chatBot?.Dispose(); return Task.CompletedTask; }).ConfigureAwait(false);
        await CleanupAsync("eventstore-http-client", () => { _eventStore?.Dispose(); return Task.CompletedTask; }).ConfigureAwait(false);
        if (_application is not null)
        {
            await CleanupAsync("owned-application", () => _application.DisposeAsync().AsTask()).ConfigureAwait(false);
        }
        if (_builder is not null)
        {
            await CleanupAsync("owned-builder", () => _builder.DisposeAsync().AsTask()).ConfigureAwait(false);
        }
        if (_catalog is not null) { await CleanupAsync("owned-catalog", () => _catalog.DisposeAsync().AsTask()).ConfigureAwait(false); }
        foreach (DevelopmentRouteCatalogPreparedOwner owner in _owners)
        {
            await CleanupAsync("owned-owner-preparation", () => owner.DisposeAsync().AsTask()).ConfigureAwait(false);
        }
        foreach (string container in _containers.AsEnumerable().Reverse())
        {
            await CleanupAsync("owned-container-" + container, async () =>
            {
                string present = (await RunAsync("docker", ["ps", "--all", "--filter", "name=^/" + container + "$", "--format", "{{.Names}}"], CancellationToken.None).ConfigureAwait(false)).Trim();
                if (string.IsNullOrEmpty(present)) { return; }
                if (present != container) { throw new InvalidOperationException("Refusing ambiguous container cleanup."); }
                using JsonDocument inspection = JsonDocument.Parse(await RunAsync("docker", ["inspect", container], CancellationToken.None).ConfigureAwait(false));
                JsonElement actual = inspection.RootElement[0];
                string label = actual.GetProperty("Config").GetProperty("Labels").GetProperty("io.hexalith.policy-refusal.run").GetString()!;
                if (label != RunId) { throw new InvalidOperationException("Refusing cleanup of an unowned container."); }
                string id = actual.GetProperty("Id").GetString()!;
                List<string> anonymous = [];
                foreach (JsonElement mount in actual.GetProperty("Mounts").EnumerateArray().Where(mount => mount.GetProperty("Type").GetString() == "volume"))
                {
                    string volume = mount.GetProperty("Name").GetString()!;
                    string destination = mount.GetProperty("Destination").GetString()!;
                    if (volume.Length != 64 || !volume.All(char.IsAsciiHexDigit)
                        || !actual.GetProperty("Config").GetProperty("Volumes").TryGetProperty(destination, out _))
                    {
                        throw new InvalidOperationException("Refusing removal of a volume without exact anonymous ownership.");
                    }
                    string[] attached = (await RunAsync("docker", ["ps", "--all", "--filter", "volume=" + volume, "--format", "{{.ID}}"], CancellationToken.None).ConfigureAwait(false))
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (attached.Length != 1 || !id.StartsWith(attached[0], StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Refusing removal of a shared volume.");
                    }
                    anonymous.Add(volume);
                }
                _ = await RunAsync("docker", ["rm", "--force", "--volumes", container], CancellationToken.None).ConfigureAwait(false);
                string remaining = await RunAsync("docker", ["ps", "--all", "--filter", "id=" + id, "--format", "{{.ID}}"], CancellationToken.None).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(remaining)) { throw new InvalidOperationException("Owned container removal could not be verified."); }
                foreach (string volume in anonymous)
                {
                    string left = await RunAsync("docker", ["volume", "ls", "--filter", "name=^" + volume + "$", "--format", "{{.Name}}"], CancellationToken.None).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(left)) { throw new InvalidOperationException("Owned anonymous volume removal could not be verified."); }
                }
            }).ConfigureAwait(false);
        }
        foreach (string directory in new[] { _mtlsCertificateDirectory, _mtlsRunDirectory }.OfType<string>().Distinct(StringComparer.Ordinal))
        {
            await CleanupAsync("owned-mtls-directory", () =>
            {
                if (Directory.Exists(directory)) { Directory.Delete(directory, recursive: true); }
                if (Directory.Exists(directory)) { throw new IOException("Owned mTLS directory removal could not be verified."); }
                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }
        try { await WriteAsync("cleanup.json", new { RunId, classification = "sanitized-run-owned-cleanup", complete = failures.Count == 0, steps }, CancellationToken.None).ConfigureAwait(false); }
        catch (Exception receipt) { failures.Add(new InvalidOperationException("Cleanup receipt failed: " + receipt.GetType().Name)); }
        if (failures.Count > 0) { throw new AggregateException("Run-owned cleanup incomplete; private diagnostics: " + DirectoryPath, failures); }
    }

    private async Task StartCoreAsync(CancellationToken token)
    {
        DateTimeOffset started = DateTimeOffset.UtcNow;
        string manifestPath = Path.Combine(_workspace, "_bmad-output/implementation-artifacts/evidence/story-1-6/native-refusal-preflight/source-binding.json");
        byte[] manifest = await File.ReadAllBytesAsync(manifestPath, token).ConfigureAwait(false);
        _sourceBinding = manifest;
        await RecheckBindingAsync(token).ConfigureAwait(false);
        using JsonDocument binding = JsonDocument.Parse(manifest);
        string revision = binding.RootElement.GetProperty("rootRevision").GetString()!;
        (string redis, int redisPort) = await ContainerAsync("redis", "redis:7.4.2", 6379,
            ["redis-server", "--appendonly", "yes", "--save", ""], token).ConfigureAwait(false);
        string redisInfo = await RunAsync("docker", ["exec", redis, "redis-cli", "--raw", "INFO", "server"], token).ConfigureAwait(false);
        string redisRunId = redisInfo.Split('\n').Single(line => line.StartsWith("run_id:", StringComparison.Ordinal))[7..].Trim();
        string image = (await RunAsync("docker", ["inspect", "--format", "{{.Image}}", redis], token).ConfigureAwait(false)).Trim();
        _builder = await DistributedApplicationTestingBuilder.CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(
            ["--environment=Development", "--Dapr:RedisHost=127.0.0.1:" + redisPort,
                "--Dapr:PlacementHostAddress=127.0.0.1:1", "--Dapr:SchedulerHostAddress=127.0.0.1:1",
                "--EnableKeycloak=true", "--KeycloakPersistent=false",
                "--ChatBot:LiveRecoveryValidation:MailboxClientSecret=" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32))], token).ConfigureAwait(false);
        if (new[] { "Projects", "Memories" }.Any(owner => _builder.Configuration.GetSection("ChatBot:" + owner).Exists())
            || _builder.Configuration["Dapr:Mtls:CertificateDirectory"] is not null)
        {
            throw new InvalidOperationException("Preflight blocked: ambient external owner or certificate configuration escapes disposable scope.");
        }
        ContainerResource security = _builder.Resources.OfType<ContainerResource>().Single(resource => resource.Name == "security");
        _builder.CreateResourceBuilder(security).WithLifetime(ContainerLifetime.Session);
        foreach (ProjectResource project in _builder.Resources.OfType<ProjectResource>())
        {
            foreach (EndpointAnnotation endpoint in project.Annotations.OfType<EndpointAnnotation>())
            {
                endpoint.Port = null;
                endpoint.TargetPort = null;
            }
            foreach (DaprSidecarAnnotation sidecar in project.Annotations.OfType<DaprSidecarAnnotation>())
            {
                DaprSidecarOptionsAnnotation options = sidecar.Sidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>().Single();
                object document = new DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization().Build()
                    .Deserialize<object>(File.ReadAllText(options.Options.Config ?? throw new InvalidOperationException("Preflight blocked: sidecar configuration missing.")));
                JsonObject configuration = JsonNode.Parse(new SerializerBuilder().JsonCompatible().Build().Serialize(document))!.AsObject();
                configuration["spec"]!["mtls"] = new JsonObject { ["enabled"] = true };
                configuration["spec"]!["nameResolution"] = new JsonObject { ["component"] = "sqlite", ["version"] = "v1",
                    ["configuration"] = new JsonObject { ["connectionString"] = Path.Combine(DirectoryPath, "discovery.db") } };
                string privateConfig = Path.Combine(DirectoryPath, project.Name + ".discovery.configuration.json");
                await File.WriteAllBytesAsync(privateConfig, JsonSerializer.SerializeToUtf8Bytes(configuration), token).ConfigureAwait(false);
                sidecar.Sidecar.Annotations[sidecar.Sidecar.Annotations.IndexOf(options)] = new DaprSidecarOptionsAnnotation(options.Options with
                {
                    Config = privateConfig,
                    DaprHttpPort = Port(), DaprGrpcPort = Port(), DaprInternalGrpcPort = Port(),
                });
            }
        }
        string sentryConfiguration = Path.Combine(DirectoryPath, "sentry.configuration.json");
        await PrivateBytesAsync(sentryConfiguration, JsonSerializer.SerializeToUtf8Bytes(new { apiVersion = "dapr.io/v1alpha1", kind = "Configuration",
            metadata = new { name = "policy-refusal-sentry" }, spec = new { mtls = new { enabled = true, workloadCertTTL = "24h" } } }), token).ConfigureAwait(false);
        (_mtlsRunDirectory, _mtlsCertificateDirectory) = global::DaprSelfHostedMtls.AddLocalApplicationInfrastructure(_builder, _workspace, sentryConfiguration);
        IResourceBuilder<ProjectResource> Project(string name) => _builder.CreateResourceBuilder(_builder.Resources.OfType<ProjectResource>().Single(resource => resource.Name == name));
        var gateway = Project("eventstore");
        // The real Tenants bootstrap owner represents the operator through its signed workload protocol.
        // Enroll only that owner's two actual command domains in this disposable composition.
        gateway.WithEnvironment("Authentication__BoundCommand__AllowedDomains__tenants", "tenants,global-administrators");
        // These are the real compiled technical-owner inventories. ChatBot has no compiled catalog declaration;
        // selecting a fictional ChatBot owner would hide a missing platform prerequisite.
        foreach (string name in new[] { "tenants", "parties", "projects" })
        {
            _owners.Add(await Project(name).PrepareEventStoreDevelopmentOwnerAsync(TimeSpan.FromMinutes(3), token).ConfigureAwait(false));
        }
        string component = Path.Combine(DirectoryPath, "actor-component.json");
        await File.WriteAllBytesAsync(component, JsonSerializer.SerializeToUtf8Bytes(new { apiVersion = "dapr.io/v1alpha1", kind = "Component",
            metadata = new { name = "statestore" }, spec = new { type = "state.redis", version = "v1", metadata = new[] {
                new { name = "redisHost", value = "127.0.0.1:" + redisPort }, new { name = "actorStateStore", value = "true" }, new { name = "keyPrefix", value = "none" } } } }), token).ConfigureAwait(false);
        List<DevelopmentRouteCatalogReceiver> receivers = [];
        foreach (string name in new[] { "eventstore", "tenants", "parties", "projects", "chatbot" })
        {
            var receiver = Project(name);
            List<DevelopmentRouteCatalogInvocation> traffic = [];
            if (name == "eventstore")
            {
                foreach (string caller in new[] { "tenants", "parties", "projects", "chatbot" })
                {
                    foreach (string method in new[] { "api/v1/streams/bound/workload", "api/v1/queries/owner-authority/workload", "api/v1/commands/bound/workload" })
                    {
                        traffic.Add(new(Project(caller), method, ["POST"]));
                    }
                }
            }
            else
            {
                foreach (string method in new[] { "process", "replay-state", "query", "project" }) { traffic.Add(new(gateway, method, ["POST"])); }
            }
            receivers.Add(new(receiver, traffic));
        }
        _catalog = _builder.AddEventStoreDevelopmentRouteCatalog(gateway, _owners.Select(owner => owner.Owner).ToArray(), receivers, component);
        Request = new JsonObject
        {
            ["schema"] = "hexalith.chatbot.policy-refusal-native.v1", ["rootRevision"] = revision,
            ["sourceSha256"] = PolicyRefusalNativeObservationVerifier.Hash(manifest), ["runId"] = RunId,
            ["producerId"] = "chatbot-policy-refusal-producer",
            ["backing"] = new JsonObject { ["containerId"] = redis, ["redisRunId"] = redisRunId, ["imageDigest"] = image, ["database"] = 0 },
            ["runStartedUtc"] = started,
            ["tenants"] = new JsonArray(new JsonObject { ["tenantId"] = "tenant-alpha", ["principalId"] = "actor-alpha" },
                new JsonObject { ["tenantId"] = "tenant-beta", ["principalId"] = "actor-beta" }),
        };
        _application = await _builder.BuildAsync(token).ConfigureAwait(false);
        await _application.StartAsync(token).ConfigureAwait(false);
        // Bind the toolkit's actual rendered inputs after the supported authority observer has run.
        // The independent operator must also inspect the live process arguments and provider; this is a handoff,
        // not a producer-issued claim that the runtime loaded these bytes.
        JsonArray inputs = [];
        foreach (ExecutableResource executable in _application.Services.GetRequiredService<DistributedApplicationModel>().Resources.OfType<ExecutableResource>()
            .Where(resource => resource.Name.EndsWith("-dapr-cli", StringComparison.Ordinal)).OrderBy(resource => resource.Name, StringComparer.Ordinal))
        {
            List<object> arguments = [];
            foreach (CommandLineArgsCallbackAnnotation callback in executable.Annotations.OfType<CommandLineArgsCallbackAnnotation>())
            {
                await callback.Callback(new CommandLineArgsCallbackContext(arguments, executable, token)).ConfigureAwait(false);
            }
            HashSet<string> files = new(StringComparer.Ordinal);
            for (int index = 0; index + 1 < arguments.Count; index++)
            {
                if (arguments[index] is "--resources-path" && arguments[index + 1] is string path)
                {
                    foreach (string file in Directory.EnumerateFiles(path).Where(file => Path.GetExtension(file) is ".yaml" or ".yml" or ".json")) { files.Add(Path.GetFullPath(file)); }
                }
                if (arguments[index] is "--config" && arguments[index + 1] is string config) { files.Add(Path.GetFullPath(config)); }
            }
            if (files.Count == 0) { throw new InvalidOperationException("Preflight blocked: actual sidecar inputs could not be completely bound."); }
            inputs.Add(new JsonObject { ["sidecar"] = executable.Name, ["files"] = new JsonArray(files.Order(StringComparer.Ordinal).Select(file => (JsonNode)new JsonObject
                { ["path"] = file, ["sha256"] = BindFileHash(file) }).ToArray()) });
        }
        byte[] configurationBytes = JsonSerializer.SerializeToUtf8Bytes(new { redisHost = "127.0.0.1:" + redisPort,
            database = 0, catalogRoot = _catalog.RootDigest, renderedSidecarInputs = inputs,
            sentryConfiguration = new { path = sentryConfiguration, sha256 = BindFileHash(sentryConfiguration) },
            security = new { enabled = true, persistent = false, lifetime = "session" },
            bootstrapOwner = "tenants", bootstrapAllowedDomains = new[] { "tenants", "global-administrators" } });
        await File.WriteAllBytesAsync(Path.Combine(DirectoryPath, "configuration.json"), configurationBytes, token).ConfigureAwait(false);
        _ = BindFileHash(Path.Combine(DirectoryPath, "configuration.json"));
        Request["configurationSha256"] = PolicyRefusalNativeObservationVerifier.Hash(configurationBytes);
        foreach (string resource in new[] { "security", "eventstore", "tenants", "parties", "projects", "chatbot" })
        {
            await _application.ResourceNotifications.WaitForResourceHealthyAsync(resource, token).ConfigureAwait(false);
        }
        _eventStore = _application.CreateHttpClient("eventstore", "http");
        _chatBot = _application.CreateHttpClient("chatbot", "http");
        _eventStore.Timeout = TimeSpan.FromSeconds(30);
        _chatBot.Timeout = TimeSpan.FromSeconds(30);
        await RecheckBindingAsync(token).ConfigureAwait(false);
        await ProvisionAsync(token).ConfigureAwait(false);
        PreparePolicyRequests();
        await BindRuntimeArtifactsAsync(token).ConfigureAwait(false);
        await RecheckBindingAsync(token).ConfigureAwait(false);
    }

    private async Task ProvisionAsync(CancellationToken token)
    {
        string admin = await TokenAsync("admin-user", "admin-pass", "hexalith-eventstore", token).ConfigureAwait(false);
        await BootstrapAsync(admin, token).ConfigureAwait(false);
        foreach ((string tenant, string actor) in new[] { ("tenant-alpha", "actor-alpha"), ("tenant-beta", "actor-beta") })
        {
            await OwnerCommandAsync(admin, "system", "tenants", tenant, "CreateTenant", new { tenantId = tenant, name = "Policy refusal fixture", description = "Disposable synthetic fixture" }, token).ConfigureAwait(false);
            await OwnerCommandAsync(admin, "system", "tenants", tenant, "AddUserToTenant", new { tenantId = tenant, userId = actor, role = "TenantOwner" }, token).ConfigureAwait(false);
            string ownerToken = await TokenAsync(actor, actor + "-pass", "hexalith-eventstore", token).ConfigureAwait(false);
            _eventStoreTokens[actor] = ownerToken;
            await OwnerCommandAsync(ownerToken, tenant, "party", actor, "Hexalith.Parties.Contracts.Commands.CreateParty",
                new { partyId = actor, type = 1, personDetails = new { firstName = "Policy", lastName = "Fixture" } }, token).ConfigureAwait(false);
            _tokens[actor] = await TokenAsync(actor, actor + "-pass", "hexalith-chatbot", token).ConfigureAwait(false);
            await ReadMembershipAsync(tenant, actor, token).ConfigureAwait(false);
        }
        foreach ((string tenant, string actor) in new[] { ("tenant-alpha", "actor-alpha"), ("tenant-beta", "actor-beta") })
        {
            string resource = "policy-preflight-" + actor;
            string key = ChatBotAuthorityGrantKey.For(tenant, actor, resource, nameof(SubmitTenantPolicyChange), "admin:policy", "human", ChatBotSurfaceOrigin.Ui);
            var grant = new GrantChatBotAuthority(key, ChatBotCommandId.New().ToString(), actor, resource, nameof(SubmitTenantPolicyChange), "admin:policy", "human", ChatBotSurfaceOrigin.Ui, DateTimeOffset.UtcNow.AddMinutes(30));
            using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands") { Content = JsonContent.Create(new {
                commandId = ChatBotCommandId.New().ToString(), commandType = nameof(GrantChatBotAuthority), command = grant, origin = "ui", requestSchemaVersion = "v1" }) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[actor]);
            using HttpResponseMessage response = await _chatBot!.SendAsync(request, token).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                throw new InvalidOperationException("Preflight blocked: real ChatBot admin:policy grant did not commit; accepted A13 owner-dispatched atomic participation is required. HTTP " + (int)response.StatusCode + ".");
            }
            using JsonDocument submitted = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false));
            string id = submitted.RootElement.GetProperty("commandId").GetString()!;
            await WaitCommittedAsync(_eventStoreTokens[actor], id, token).ConfigureAwait(false);
        }
    }

    private async Task ReadMembershipAsync(string tenant, string actor, CancellationToken token)
    {
        // Authoritative owner query reads the current native source stream, including the exact subject and role.
        while (true)
        {
            var ownerQuery = new SubmitQueryRequest("system", "tenants", tenant,
                "Hexalith.Tenants.Contracts.Queries.ResolveTenantOwnerAuthority", Payload: JsonSerializer.SerializeToElement(
                    new { tenantId = tenant, principalId = actor, operation = nameof(SubmitTenantPolicyChange), actorClass = "human", origin = "Ui" }));
            var bound = new BoundOwnerQueryRequest(ownerQuery, actor);
            Dictionary<string, string?> environment = await CredentialEnvironmentAsync("chatbot", token).ConfigureAwait(false);
            string proof = BoundOwnerQueryProof.Issue(bound, "chatbot", environment["Authentication:BoundStreamRead:PrivateKey"]!, DateTimeOffset.UtcNow);
            string assertion = await AssertionAsync("chatbot", environment, "eventstore:owner-authority-query", null, token).ConfigureAwait(false);
            using HttpClient transport = InvocationClient("chatbot", environment);
            using HttpRequestMessage query = new(HttpMethod.Post, "v1.0/invoke/eventstore/method/api/v1/queries/owner-authority/workload")
                { Content = JsonContent.Create(bound) };
            query.Headers.Add(BoundOwnerQueryProof.HeaderName, proof);
            query.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
            using HttpResponseMessage response = await transport.SendAsync(query, token).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using JsonDocument readback = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false));
                JsonElement result = readback.RootElement.GetProperty("payload");
                if (result.GetProperty("allowed").GetBoolean() && result.GetProperty("tenantId").GetString() == tenant
                    && result.GetProperty("principalId").GetString() == actor && result.GetProperty("sourceVersion").GetInt64() > 0)
                {
                    await WriteAsync("owner-" + actor + ".json", new { RunId, tenantId = tenant, principalId = actor,
                        sourceVersion = result.GetProperty("sourceVersion").GetInt64(), membershipRole = "TenantOwner",
                        classification = "producer-owner-readback-only-unaccepted" }, token).ConfigureAwait(false);
                    return;
                }
            }
            if (response.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.ServiceUnavailable))
            {
                throw new InvalidOperationException("Preflight blocked: authoritative tenant owner readback refused; HTTP " + (int)response.StatusCode + ".");
            }
            await Task.Delay(250, token).ConfigureAwait(false);
        }
    }

    private async Task BootstrapAsync(string adminToken, CancellationToken token)
    {
        Dictionary<string, string?> environment = await CredentialEnvironmentAsync("tenants", token).ConfigureAwait(false);
        string id = ChatBotCommandId.New().ToString();
        var command = new SubmitCommandRequest(id, "system", "global-administrators", "global-administrators", "BootstrapGlobalAdmin",
            JsonSerializer.SerializeToElement(new { userId = "admin-user" }), CorrelationId: id);
        var bound = new BoundWorkloadCommandRequest(command, "admin-user");
        string proof = BoundWorkloadCommandProof.Issue(bound, "tenants", environment["Authentication:BoundStreamRead:PrivateKey"]!, DateTimeOffset.UtcNow);
        string assertion = await AssertionAsync("tenants", environment, EventStoreGatewayWorkloadOperations.CommandSubmit,
            new Dictionary<string, string> { [EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType] = "system",
                [EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType] = "global-administrators" }, token).ConfigureAwait(false);
        using HttpClient transport = InvocationClient("tenants", environment);
        using HttpRequestMessage request = new(HttpMethod.Post, "v1.0/invoke/eventstore/method/api/v1/commands/bound/workload") { Content = JsonContent.Create(bound) };
        request.Headers.Add(BoundWorkloadCommandProof.HeaderName, proof);
        request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
        using HttpResponseMessage response = await transport.SendAsync(request, token).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Accepted)
        {
            throw new InvalidOperationException("Preflight blocked: supported bound Tenants bootstrap refused; HTTP " + (int)response.StatusCode + ".");
        }
        using JsonDocument accepted = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false));
        await WaitCommittedAsync(adminToken, accepted.RootElement.GetProperty("correlationId").GetString()!, token).ConfigureAwait(false);
    }

    private async Task<Dictionary<string, string?>> CredentialEnvironmentAsync(string caller, CancellationToken token)
    {
        ProjectResource project = _builder!.Resources.OfType<ProjectResource>().Single(resource => resource.Name == caller);
        Dictionary<string, object> values = new(StringComparer.Ordinal);
        EnvironmentCallbackContext context = new(_builder.ExecutionContext, project, values, token);
        foreach (EnvironmentCallbackAnnotation callback in project.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await callback.Callback(context).ConfigureAwait(false);
        }
        Dictionary<string, string?> result = new(StringComparer.Ordinal);
        foreach ((string key, object value) in values)
        {
            if (!key.StartsWith("Authentication__", StringComparison.Ordinal) && key != "DAPR_API_TOKEN") { continue; }
            result.Add(key.Replace("__", ":", StringComparison.Ordinal), value is IValueProvider provider
                ? await provider.GetValueAsync(token).ConfigureAwait(false) : value.ToString());
        }
        return result;
    }

    private async Task<string> AssertionAsync(string caller, Dictionary<string, string?> environment, string operation,
        IReadOnlyDictionary<string, string>? bindings, CancellationToken token)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(_builder!.Environment);
        services.AddLogging();
        services.AddEventStoreWorkloadAssertionIssuer(new ConfigurationBuilder().AddInMemoryCollection(environment).Build(), options => options.Workload = caller);
        ServiceProvider credentials = services.BuildServiceProvider();
        await using var credentialsLifetime = credentials.ConfigureAwait(false);
        return await credentials.GetRequiredService<IWorkloadAssertionIssuer>().IssueAsync(new("eventstore", operation, bindings), token).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Preflight blocked: the real owner workload authority refused the exact fixture operation.");
    }

    private HttpClient InvocationClient(string caller, Dictionary<string, string?> environment)
    {
        var client = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false })
            { BaseAddress = Application.GetEndpoint(caller + "-dapr-cli", "http"), Timeout = TimeSpan.FromSeconds(30) };
        if (environment.TryGetValue("DAPR_API_TOKEN", out string? apiToken) && !string.IsNullOrWhiteSpace(apiToken))
        {
            client.DefaultRequestHeaders.Add("dapr-api-token", apiToken);
        }
        return client;
    }

    private async Task OwnerCommandAsync(string accessToken, string tenant, string domain, string aggregateId, string commandType, object payload, CancellationToken token)
    {
        string messageId = ChatBotCommandId.New().ToString();
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands") { Content = JsonContent.Create(new {
            messageId, tenant, domain, aggregateId, commandType, payload = JsonSerializer.SerializeToElement(payload), correlationId = messageId }) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using HttpResponseMessage response = await _eventStore!.SendAsync(request, token).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Accepted) { throw new InvalidOperationException("Preflight blocked: real owner command " + commandType + " refused; HTTP " + (int)response.StatusCode + "."); }
        using JsonDocument accepted = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false));
        string correlation = accepted.RootElement.GetProperty("correlationId").GetString()!;
        await WaitCommittedAsync(accessToken, correlation, token).ConfigureAwait(false);
    }

    private async Task WaitCommittedAsync(string accessToken, string id, CancellationToken token)
    {
        while (true)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/commands/status/" + Uri.EscapeDataString(id));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using HttpResponseMessage response = await _eventStore!.SendAsync(request, token).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK) { throw new InvalidOperationException("Preflight blocked: real owner completion status unavailable."); }
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false));
            JsonElement root = document.RootElement;
            string state = root.GetProperty("status").GetString()!;
            if (state == "Completed")
            {
                if (root.GetProperty("eventCount").GetInt32() <= 0 || root.GetProperty("committedEventSequence").GetInt64() <= 0)
                {
                    throw new InvalidOperationException("Preflight blocked: HTTP completion lacks committed owner events.");
                }
                return;
            }
            if (state is "Rejected" or "Failed" or "PublishFailed") { throw new InvalidOperationException("Preflight blocked: real owner command did not commit (" + state + ")."); }
            await Task.Delay(250, token).ConfigureAwait(false);
        }
    }

    private async Task<string> TokenAsync(string username, string password, string client, CancellationToken token)
    {
        using HttpClient security = new() { BaseAddress = Application.GetEndpoint("security", "http"), Timeout = TimeSpan.FromSeconds(15) };
        using FormUrlEncodedContent form = new(new Dictionary<string, string> { ["grant_type"] = "password", ["client_id"] = client,
            ["username"] = username, ["password"] = password, ["scope"] = "openid" });
        using HttpResponseMessage response = await security.PostAsync("/realms/hexalith/protocol/openid-connect/token", form, token).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK) { throw new InvalidOperationException("Preflight blocked: real scoped identity acquisition refused; HTTP " + (int)response.StatusCode + "."); }
        using JsonDocument result = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false));
        return result.RootElement.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Preflight blocked: identity credential absent.");
    }

    private async Task<(string Id, int Port)> ContainerAsync(string name, string image, int target, string[] arguments, CancellationToken token)
    {
        string containerName = "policy-refusal-" + name + "-" + RunId;
        _containers.Add(containerName);
        string id = (await RunAsync("docker", ["run", "--detach", "--label", "io.hexalith.policy-refusal.run=" + RunId,
            "--name", containerName, "--tmpfs", "/data:rw,nosuid,nodev,noexec,size=64m", "--publish", "127.0.0.1::" + target, image, .. arguments], token).ConfigureAwait(false)).Trim();
        string mapping = (await RunAsync("docker", ["port", id, target + "/tcp"], token).ConfigureAwait(false)).Trim();
        if (!mapping.StartsWith("127.0.0.1:", StringComparison.Ordinal) || !int.TryParse(mapping[10..], out int port))
        {
            throw new InvalidOperationException("Preflight blocked: run-owned provider endpoint was not allocated on loopback.");
        }
        return (id, port);
    }

    private static int Port()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string FindWorkspace()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx"))) { directory = directory.Parent; }
        return directory?.FullName ?? throw new InvalidOperationException("Preflight blocked: owning workspace unavailable.");
    }

    /// <summary>Rechecks the identical candidate, full consumed-repository dirty inventory and selected runtime bytes.</summary>
    internal async Task RecheckBindingAsync(CancellationToken token)
    {
        const string manifestRelative = "_bmad-output/implementation-artifacts/evidence/story-1-6/native-refusal-preflight/source-binding.json";
        byte[] current = await File.ReadAllBytesAsync(Path.Combine(_workspace, manifestRelative), token).ConfigureAwait(false);
        if (_sourceBinding is null || !current.AsSpan().SequenceEqual(_sourceBinding))
        {
            throw new InvalidOperationException("Preflight blocked: source inspection binding changed during execution.");
        }
        if (_immutableRequest is not null)
        {
            byte[] requestBytes = await File.ReadAllBytesAsync(Path.Combine(DirectoryPath, "request.json"), token).ConfigureAwait(false);
            if (!requestBytes.AsSpan().SequenceEqual(_immutableRequest)) { throw new InvalidOperationException("Preflight blocked: immutable request file changed."); }
            using JsonDocument original = JsonDocument.Parse(_immutableRequest);
            foreach (JsonProperty property in original.RootElement.EnumerateObject())
            {
                if (!JsonElement.DeepEquals(property.Value, JsonSerializer.SerializeToElement(Request[property.Name])))
                {
                    throw new InvalidOperationException("Preflight blocked: immutable request metadata/body/artifact binding changed.");
                }
            }
        }
        using JsonDocument binding = JsonDocument.Parse(current);
        JsonElement[] repositories = binding.RootElement.GetProperty("repositories").EnumerateArray().ToArray();
        string declarations = await RunAsync("git", ["-C", _workspace, "config", "-f", ".gitmodules", "--get-regexp", "^submodule\\..*\\.path$"], token).ConfigureAwait(false);
        HashSet<string> declared = declarations.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line[(line.IndexOf(' ') + 1)..].Trim()).ToHashSet(StringComparer.Ordinal);
        declared.Add(".");
        if (repositories.Length != declared.Count || !repositories.Select(repository => repository.GetProperty("path").GetString()!).ToHashSet(StringComparer.Ordinal).SetEquals(declared))
        {
            throw new InvalidOperationException("Preflight blocked: consumed repository inventory is incomplete or duplicated.");
        }
        JsonElement[] sources = binding.RootElement.GetProperty("sources").EnumerateArray().ToArray();
        HashSet<string> allowedDirty = sources.Where(source => source.GetProperty("kind").GetString() == "uncommitted-implementation-bytes")
            .Select(source => source.GetProperty("path").GetString()!).ToHashSet(StringComparer.Ordinal);
        allowedDirty.Add(manifestRelative);
        allowedDirty.Add("_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md");
        foreach (JsonElement repository in repositories)
        {
            string relative = repository.GetProperty("path").GetString()!;
            string actual = (await RunAsync("git", ["-C", Path.Combine(_workspace, relative), "rev-parse", "HEAD"], token).ConfigureAwait(false)).Trim();
            if (actual != repository.GetProperty("worktreeRevision").GetString()
                || relative == "." && actual != binding.RootElement.GetProperty("rootRevision").GetString())
            {
                throw new InvalidOperationException("Preflight blocked: inspected candidate moved.");
            }
            if (relative != ".")
            {
                foreach (string expression in new[] { "HEAD:" + relative, ":" + relative })
                {
                    string link = (await RunAsync("git", ["-C", _workspace, "rev-parse", expression], token).ConfigureAwait(false)).Trim();
                    if (link != actual) { throw new InvalidOperationException("Preflight blocked: root/index gitlink and checkout disagree."); }
                }
            }
            string inventory = await RunAsync("git", ["-C", Path.Combine(_workspace, relative), "status", "--porcelain=v1", "-z", "--untracked-files=all", "--ignore-submodules=all"], token).ConfigureAwait(false);
            foreach (string entry in inventory.Split('\0', StringSplitOptions.RemoveEmptyEntries))
            {
                if (entry.Length < 4 || entry[..2].Any(character => character is 'R' or 'C' or 'D'))
                {
                    throw new InvalidOperationException("Preflight blocked: unbound renamed or deleted consumed input.");
                }
                string path = relative == "." ? entry[3..] : relative + "/" + entry[3..];
                if (!allowedDirty.Contains(path)) { throw new InvalidOperationException("Preflight blocked: unbound dirty consumed-repository input (" + path + ")."); }
            }
        }
        foreach (JsonElement source in sources)
        {
            string path = Path.Combine(_workspace, source.GetProperty("path").GetString()!);
            if (HashFile(path) != source.GetProperty("worktreeSha256").GetString())
            {
                throw new InvalidOperationException("Preflight blocked: inspected source/configuration bytes changed.");
            }
        }
        foreach ((string path, string hash) in _artifactHashes)
        {
            if (HashFile(path) != hash) { throw new InvalidOperationException("Preflight blocked: selected runtime artifact changed during execution."); }
        }
    }

    private void PreparePolicyRequests()
    {
        JsonArray submissions = [];
        foreach ((string tenant, string actor) in new[] { ("tenant-alpha", "actor-alpha"), ("tenant-beta", "actor-beta") })
        {
            string commandId = ChatBotCommandId.New().ToString();
            string correlationId = ChatBotCommandId.New().ToString();
            string taskId = ChatBotCommandId.New().ToString();
            var command = new SubmitTenantPolicyChange("policy-preflight-" + actor, "preflight-source", "preflight-proposed", 0,
                TenantPolicySchema.DefaultM0Values.Select(value => value.KnobId).ToArray(), new TenantPolicyChangeSet(TenantPolicySchema.DefaultM0Values),
                "policy-preflight", actor, TenantPolicySchemaVersions.M0, correlationId, "preflight-old", "preflight-new");
            byte[] body = JsonSerializer.SerializeToUtf8Bytes(new { commandId, commandType = nameof(SubmitTenantPolicyChange), command,
                origin = "ui", requestSchemaVersion = "v1" }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            _policyBodies.Add(actor, body);
            submissions.Add(new JsonObject { ["tenantId"] = tenant, ["principalId"] = actor, ["commandId"] = commandId,
                ["commandType"] = nameof(SubmitTenantPolicyChange), ["correlationId"] = correlationId, ["taskId"] = taskId,
                ["requestBodySha256"] = PolicyRefusalNativeObservationVerifier.Hash(body), ["method"] = "POST", ["path"] = "/api/v1/commands",
                ["contentType"] = "application/json; charset=utf-8", ["origin"] = "ui", ["requestSchemaVersion"] = "v1" });
        }
        Request["submissions"] = submissions;
        Request["causeObservationPrerequisite"] = "ChatBot-owned cross-process reader for exact policy-snapshot-unavailable admission facts; currently unavailable";
    }

    private async Task BindRuntimeArtifactsAsync(CancellationToken token)
    {
        JsonArray artifacts = [];
        void Add(string resource, string path)
        {
            string full = Path.GetFullPath(path);
            string hash = HashFile(full);
            _artifactHashes[full] = hash;
            artifacts.Add(new JsonObject { ["resourceId"] = resource, ["path"] = full, ["sha256"] = hash, ["bytes"] = new FileInfo(full).Length });
        }
        void AddDirectory(string resource, string target)
        {
            foreach (string file in Directory.EnumerateFiles(Path.GetDirectoryName(target)!).Where(file => Path.GetExtension(file) is ".dll"
                || file.EndsWith(".deps.json", StringComparison.Ordinal) || file.EndsWith(".runtimeconfig.json", StringComparison.Ordinal)).Order(StringComparer.Ordinal))
            {
                Add(resource, file);
            }
        }
        AddDirectory("producer", typeof(PolicyRefusalPreflightTopology).Assembly.Location);
        string appHostOutput = await RunAsync("dotnet", ["msbuild", Path.Combine(_workspace, "src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj"),
            "-nologo", "-m:1", "-getProperty:TargetPath", "-p:Configuration=Debug"], token).ConfigureAwait(false);
        string appHostTarget = appHostOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last();
        if (!Path.IsPathFullyQualified(appHostTarget) || !File.Exists(appHostTarget)) { throw new InvalidOperationException("Preflight blocked: actual AppHost runtime artifact unavailable."); }
        AddDirectory("apphost", appHostTarget);
        foreach (ProjectResource project in _builder!.Resources.OfType<ProjectResource>().OrderBy(project => project.Name, StringComparer.Ordinal))
        {
#pragma warning disable ASPIREEXTENSION001
            var launch = await project.CreateLaunchConfigurationAsync(ExecutableLaunchMode.NoDebug, token).ConfigureAwait(false)
                as ProjectLaunchConfiguration ?? throw new InvalidOperationException("Preflight blocked: actual selected child launch unavailable.");
#pragma warning restore ASPIREEXTENSION001
            string evaluated = await RunAsync("dotnet", ["msbuild", launch.ProjectPath, "-nologo", "-m:1", "-getProperty:TargetPath",
                "-p:Configuration=" + (launch.BuildConfiguration ?? "Debug")], token).ConfigureAwait(false);
            string target = evaluated.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last();
            if (!Path.IsPathFullyQualified(target) || !File.Exists(target)) { throw new InvalidOperationException("Preflight blocked: evaluated child artifact unavailable."); }
            AddDirectory(project.Name, target);
        }
        foreach (ExecutableResource executable in Application.Services.GetRequiredService<DistributedApplicationModel>().Resources.OfType<ExecutableResource>()
            .Where(executable => executable.Name.EndsWith("-dapr-cli", StringComparison.Ordinal)).OrderBy(executable => executable.Name, StringComparer.Ordinal))
        {
            if (!Path.IsPathFullyQualified(executable.Command) || !File.Exists(executable.Command))
            {
                throw new InvalidOperationException("Preflight blocked: selected native sidecar artifact identity unavailable.");
            }
            Add(executable.Name, executable.Command);
        }
        Request["runtimeArtifacts"] = artifacts;
    }

    private static string HashFile(string path) => PolicyRefusalNativeObservationVerifier.Hash(File.ReadAllBytes(path));

    private string BindFileHash(string path)
    {
        string hash = HashFile(path);
        _artifactHashes[Path.GetFullPath(path)] = hash;
        return hash;
    }

    private static async Task PrivateBytesAsync(string path, byte[] bytes, CancellationToken token)
    {
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.Read };
        if (!OperatingSystem.IsWindows()) { options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite; }
        FileStream file = new(path, options);
        await using var lifetime = file.ConfigureAwait(false);
        await file.WriteAsync(bytes, token).ConfigureAwait(false);
    }

    private async Task RetainFailureAsync(Exception exception)
    {
        string path = Path.Combine(DirectoryPath, "private-failure-" + Interlocked.Increment(ref _diagnosticSequence) + ".txt");
        await PrivateBytesAsync(path, Encoding.UTF8.GetBytes(exception.ToString()), CancellationToken.None).ConfigureAwait(false);
    }

    private async Task<string> RunAsync(string executable, string[] arguments, CancellationToken token)
    {
        string directory = Path.Combine(DirectoryPath, "diagnostic-" + Interlocked.Increment(ref _diagnosticSequence));
        Directory.CreateDirectory(directory);
        if (!OperatingSystem.IsWindows()) { File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); }
        await PrivateBytesAsync(Path.Combine(directory, "command.json"), JsonSerializer.SerializeToUtf8Bytes(new { executable, arguments, workingDirectory = Environment.CurrentDirectory }), CancellationToken.None).ConfigureAwait(false);
        await PrivateBytesAsync(Path.Combine(directory, "stdout.bin"), [], CancellationToken.None).ConfigureAwait(false);
        await PrivateBytesAsync(Path.Combine(directory, "stderr.bin"), [], CancellationToken.None).ConfigureAwait(false);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(60));
        using Process process = new() { StartInfo = new ProcessStartInfo(executable) { UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true } };
        foreach (string argument in arguments) { process.StartInfo.ArgumentList.Add(argument); }
        int? exit = null;
        string? failure = null;
        try
        {
            process.Start();
            process.StandardInput.Close();
            Task stdout = CaptureAsync(process.StandardOutput.BaseStream, Path.Combine(directory, "stdout.bin"), deadline);
            Task stderr = CaptureAsync(process.StandardError.BaseStream, Path.Combine(directory, "stderr.bin"), deadline);
            try { await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false); }
            finally
            {
                if (!process.HasExited) { process.Kill(entireProcessTree: true); }
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                exit = process.ExitCode;
                await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            }
        }
        catch (Exception exception) { failure = exception.GetType().Name; }
        await PrivateBytesAsync(Path.Combine(directory, "exit.json"), JsonSerializer.SerializeToUtf8Bytes(new { exitCode = exit, failure,
            boundedOutputLimitBytesPerStream = 1024 * 1024, cancelled = token.IsCancellationRequested,
            timedOut = deadline.IsCancellationRequested && !token.IsCancellationRequested }), CancellationToken.None).ConfigureAwait(false);
        if (failure is not null || exit != 0) { throw new InvalidOperationException("Preflight prerequisite failed; private exact-command/stdout/stderr diagnostics: " + directory); }
        return await File.ReadAllTextAsync(Path.Combine(directory, "stdout.bin"), token).ConfigureAwait(false);
    }

    private static async Task CaptureAsync(Stream source, string path, CancellationTokenSource deadline)
    {
        var options = new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Write, Share = FileShare.Read };
        if (!OperatingSystem.IsWindows()) { options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite; }
        FileStream destination = new(path, options);
        await using var lifetime = destination.ConfigureAwait(false);
        byte[] buffer = new byte[8192];
        int total = 0;
        try
        {
            while (await source.ReadAsync(buffer, deadline.Token).ConfigureAwait(false) is int read && read > 0)
            {
                int retained = Math.Min(read, 1024 * 1024 - total);
                if (retained > 0) { await destination.WriteAsync(buffer.AsMemory(0, retained), deadline.Token).ConfigureAwait(false); total += retained; }
                if (retained != read) { throw new InvalidOperationException("Private prerequisite diagnostics exceeded their bounded capacity."); }
            }
        }
        catch { await deadline.CancelAsync().ConfigureAwait(false); throw; }
    }
}
