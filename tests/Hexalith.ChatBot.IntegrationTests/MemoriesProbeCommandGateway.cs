using System.Net;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Hexalith.EventStore.Contracts.Commands;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Task-owned loopback transport fixture that adds a real, narrowly scoped Keycloak service bearer to Memories'
/// supported EventStore gateway boundary. It forwards every accepted response from the real EventStore API.
/// </summary>
internal sealed class MemoriesProbeCommandGateway : IAsyncDisposable
{
    private readonly string _clientId = $"story-memories-provisioner-{Guid.NewGuid():N}";
    private readonly string _clientSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private readonly string _adminPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private readonly WebApplication _host;
    private MemoriesProbeDomainTransport? _domain;
    private readonly ConcurrentDictionary<string, string> _acceptedTenants = new(StringComparer.Ordinal);
    private string? _serviceSubject;
    private HttpClient? _eventStore;
    private int _acceptedCommands;
    private int _lastStatusCode;
    private string? _lastRequestDomain;
    private string? _lastRequestType;

    private MemoriesProbeCommandGateway(WebApplication host)
    {
        _host = host;
    }

    public string BaseAddress => _host.Urls.Single() + "/";

    public int AcceptedCommands => Volatile.Read(ref _acceptedCommands);

    public int LastStatusCode => Volatile.Read(ref _lastStatusCode);

    public string? LastRequestDomain => Volatile.Read(ref _lastRequestDomain);

    public string? LastRequestType => Volatile.Read(ref _lastRequestType);

    public string OwnerModuleSha256 => _domain!.OwnerModuleSha256;

    public string OwnerModulePath => _domain!.OwnerModulePath;

    public string OwnerModuleVersion => _domain!.OwnerModuleVersion;

    public string OwnerLibraryVersion => _domain!.OwnerLibraryVersion;

    public string ServerAssemblyPath => _domain!.ServerAssemblyPath;

    public int ReplayedCommands => _domain?.ReplayedCommands ?? 0;

    public int PersistedCommands { get; private set; }

    public static async Task<MemoriesProbeCommandGateway> StartAsync(CancellationToken cancellationToken)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        WebApplication host = builder.Build();
        MemoriesProbeCommandGateway gateway = new(host);
        host.MapPost("/api/v1/commands", gateway.ForwardAsync);
        host.MapPost("/owner-process", (DomainServiceRequest request, HttpContext context) =>
            context.Connection.RemoteIpAddress is not null && IPAddress.IsLoopback(context.Connection.RemoteIpAddress)
                && gateway._serviceSubject is string subject && gateway._domain is MemoriesProbeDomainTransport domain
                ? Results.Ok(domain.Process(request, subject))
                : Results.StatusCode(StatusCodes.Status403Forbidden));
        try
        {
            await host.StartAsync(cancellationToken).ConfigureAwait(false);
            return gateway;
        }
        catch
        {
            await host.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public void Configure(IDistributedApplicationTestingBuilder builder)
    {
        IResource security = builder.Resources.Single(resource => resource.Name == "security");
        security.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["KC_BOOTSTRAP_ADMIN_USERNAME"] = "story-probe-admin";
            context.EnvironmentVariables["KC_BOOTSTRAP_ADMIN_PASSWORD"] = _adminPassword;
        }));
        IResource memories = builder.Resources.Single(resource => resource.Name == "memories");
        memories.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
            context.EnvironmentVariables["EventStoreIntegration__CommandGateway__BaseAddress"] = BaseAddress));
        IResource eventStore = builder.Resources.Single(resource => resource.Name == "eventstore");
        eventStore.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            foreach (string tenant in new[] { "tenant-alpha", "tenant-beta" })
            {
                string registration = $"EventStore__DomainServices__Registrations__{tenant}|memories-tenants|v1__";
                context.EnvironmentVariables[registration + "AppId"] = BaseAddress.TrimEnd('/');
                context.EnvironmentVariables[registration + "MethodName"] = "owner-process";
                context.EnvironmentVariables[registration + "TenantId"] = tenant;
                context.EnvironmentVariables[registration + "Domain"] = "memories-tenants";
                context.EnvironmentVariables[registration + "Version"] = "v1";
            }
        }));
    }

    public async Task ConnectAsync(DistributedApplication application, CancellationToken cancellationToken)
    {
        if (_eventStore is not null)
        {
            return;
        }

        application.ResourceNotifications.TryGetCurrentState("memories", out ResourceEvent? ownerResource).ShouldBeTrue();
        ownerResource.ShouldNotBeNull();
        string executable = ownerResource.Snapshot.Properties.Single(property => property.Name == "executable.path").Value as string
            ?? throw new InvalidOperationException("The live owner executable path is required for exact module binding.");
        Path.GetFileNameWithoutExtension(executable).ShouldBe("dotnet");
        object argumentsValue = ownerResource.Snapshot.Properties.Single(property => property.Name == "executable.args").Value
            ?? throw new InvalidOperationException("The live owner argument vector is missing.");
        string[] arguments = argumentsValue is IEnumerable<string> items ? items.ToArray()
            : throw new InvalidOperationException($"The live owner argument vector is required; found {argumentsValue.GetType().Name}.");
        arguments[0].ShouldBe("run");
        arguments.ShouldContain("--no-build");
        int projectIndex = Array.IndexOf(arguments, "--project");
        int configurationIndex = Array.IndexOf(arguments, "--configuration");
        projectIndex.ShouldBeGreaterThanOrEqualTo(0);
        configurationIndex.ShouldBeGreaterThanOrEqualTo(0);
        string projectPath = arguments[projectIndex + 1];
        Path.GetFileName(projectPath).ShouldBe("Hexalith.Memories.Server.csproj");
        ownerResource.Snapshot.Properties.Single(property => property.Name == "project.path").Value.ShouldBe(projectPath);
        string configuration = arguments[configurationIndex + 1];
        configuration.ShouldBe("Release", "The owner transport must execute the same Release owner module as the live server.");
        string serverAssembly = Path.Combine(Path.GetDirectoryName(projectPath)!, "bin", configuration, "net10.0", "Hexalith.Memories.Server.dll");
        _domain = new MemoriesProbeDomainTransport(serverAssembly);

        using HttpClient keycloak = new() { BaseAddress = application.GetEndpoint("security", "http"), Timeout = TimeSpan.FromSeconds(30) };
        using FormUrlEncodedContent adminForm = new(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = "story-probe-admin",
            ["password"] = _adminPassword,
        });
        using HttpResponseMessage adminResponse = await keycloak.PostAsync(
            "/realms/master/protocol/openid-connect/token", adminForm, cancellationToken).ConfigureAwait(false);
        adminResponse.StatusCode.ShouldBe(HttpStatusCode.OK, "The task-owned Keycloak bootstrap administrator must be ready.");
        using JsonDocument adminToken = JsonDocument.Parse(await adminResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        keycloak.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken.RootElement.GetProperty("access_token").GetString());
        object[] mappers =
        [
            new { name = "eventstore-audience", protocol = "openid-connect", protocolMapper = "oidc-audience-mapper", config = new Dictionary<string, string>
            {
                ["included.client.audience"] = "hexalith-eventstore", ["access.token.claim"] = "true", ["id.token.claim"] = "false",
            } },
            ClaimMapper("tenants", "tenant-alpha tenant-beta", "String"),
            ClaimMapper("domains", "memories-tenants", "String"),
            ClaimMapper("permissions", "register-tenant update-tenant-lifecycle-status", "String"),
        ];
        using HttpResponseMessage created = await keycloak.PostAsJsonAsync("/admin/realms/hexalith/clients", new
        {
            clientId = _clientId,
            secret = _clientSecret,
            enabled = true,
            protocol = "openid-connect",
            publicClient = false,
            serviceAccountsEnabled = true,
            standardFlowEnabled = false,
            directAccessGrantsEnabled = false,
            protocolMappers = mappers,
        }, cancellationToken).ConfigureAwait(false);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        keycloak.DefaultRequestHeaders.Authorization = null;
        using FormUrlEncodedContent serviceForm = new(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
        });
        using HttpResponseMessage serviceResponse = await keycloak.PostAsync(
            "/realms/hexalith/protocol/openid-connect/token", serviceForm, cancellationToken).ConfigureAwait(false);
        serviceResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument serviceToken = JsonDocument.Parse(await serviceResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        string bearer = serviceToken.RootElement.GetProperty("access_token").GetString()!;
        string encodedClaims = bearer.Split('.')[1].Replace('-', '+').Replace('_', '/');
        encodedClaims = encodedClaims.PadRight(encodedClaims.Length + ((4 - (encodedClaims.Length % 4)) % 4), '=');
        using JsonDocument claims = JsonDocument.Parse(Convert.FromBase64String(encodedClaims));
        claims.RootElement.GetProperty("tenants").GetString().ShouldBe("tenant-alpha tenant-beta");
        claims.RootElement.GetProperty("domains").GetString().ShouldBe("memories-tenants");
        claims.RootElement.GetProperty("permissions").GetString().ShouldBe("register-tenant update-tenant-lifecycle-status");
        claims.RootElement.TryGetProperty("global_admin", out _).ShouldBeFalse();
        _serviceSubject = claims.RootElement.GetProperty("sub").GetString();
        HttpClient eventStore = application.CreateHttpClient("eventstore", "http");
        eventStore.Timeout = TimeSpan.FromSeconds(30);
        eventStore.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        try
        {
            await AssertOwnerScopeDeniedAsync(eventStore, "tenant-outside-probe", "memories-tenants", cancellationToken).ConfigureAwait(false);
            await AssertOwnerScopeDeniedAsync(eventStore, "tenant-alpha", "chatbot", cancellationToken).ConfigureAwait(false);
            _eventStore = eventStore;
        }
        catch
        {
            eventStore.Dispose();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _eventStore?.Dispose();
        try
        {
            await _host.StopAsync().ConfigureAwait(false);
        }
        finally
        {
            await _host.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task AssertPersistedOwnerCommandsAsync(CancellationToken cancellationToken)
    {
        HttpClient eventStore = _eventStore ?? throw new InvalidOperationException("The real gateway is not connected.");
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(1));
        foreach ((string messageId, string tenant) in _acceptedTenants.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            while (true)
            {
                using HttpResponseMessage response = await eventStore.GetAsync($"/api/v1/commands/status/{messageId}", deadline.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    CommandStatusQueryResponse? status = await response.Content.ReadFromJsonAsync<CommandStatusQueryResponse>(deadline.Token).ConfigureAwait(false);
                    status.ShouldNotBeNull();
                    if (status.Status == nameof(CommandStatus.Completed))
                    {
                        status.TenantId.ShouldBe(tenant);
                        status.AggregateId.ShouldBe(tenant);
                        status.Domain.ShouldBe("memories-tenants");
                        status.EventCount.GetValueOrDefault().ShouldBeGreaterThanOrEqualTo(1);
                        status.CommittedEventSequence.GetValueOrDefault().ShouldBeGreaterThanOrEqualTo(1L);
                        PersistedCommands++;
                        break;
                    }

                    status.Status.ShouldNotBe(nameof(CommandStatus.Rejected));
                }
                else
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250), deadline.Token).ConfigureAwait(false);
            }
        }

        PersistedCommands.ShouldBeGreaterThanOrEqualTo(4);
    }

    private static object ClaimMapper(string name, string value, string type)
        => new { name, protocol = "openid-connect", protocolMapper = "oidc-hardcoded-claim-mapper", config = new Dictionary<string, string>
        {
            ["claim.name"] = name, ["claim.value"] = value, ["jsonType.label"] = type,
            ["access.token.claim"] = "true", ["id.token.claim"] = "false", ["userinfo.token.claim"] = "false",
        } };

    private static async Task AssertOwnerScopeDeniedAsync(HttpClient eventStore, string tenant, string domain, CancellationToken cancellationToken)
    {
        using HttpResponseMessage denied = await eventStore.PostAsJsonAsync("/api/v1/commands", new
        {
            messageId = Hexalith.ChatBot.Contracts.Identities.ChatBotCommandId.New().ToString(),
            tenant,
            domain,
            aggregateId = tenant,
            commandType = "register-tenant",
            payload = new { tenantId = tenant, displayName = tenant, registeredAt = DateTimeOffset.UtcNow },
        }, cancellationToken).ConfigureAwait(false);
        denied.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "The real EventStore API must enforce the service fixture's tenant and domain scope.");
    }

    private async Task<IResult> ForwardAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        using JsonDocument body = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken).ConfigureAwait(false);
        JsonElement command = body.RootElement;
        string? tenant = command.GetProperty("tenant").GetString();
        string? domain = command.GetProperty("domain").GetString();
        string? type = command.GetProperty("commandType").GetString();
        Volatile.Write(ref _lastRequestDomain, domain);
        Volatile.Write(ref _lastRequestType, type);
        if (tenant is not ("tenant-alpha" or "tenant-beta") || domain != "memories-tenants"
            || type is not ("register-tenant" or "update-tenant-lifecycle-status"))
        {
            Volatile.Write(ref _lastStatusCode, StatusCodes.Status403Forbidden);
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!command.GetProperty("payload").TryGetProperty("tenantId", out JsonElement payloadTenant)
            || !string.Equals(payloadTenant.GetString(), tenant, StringComparison.Ordinal))
        {
            Volatile.Write(ref _lastStatusCode, StatusCodes.Status403Forbidden);
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        HttpClient? eventStore = _eventStore;
        if (eventStore is null)
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        using StringContent payload = new(command.GetRawText(), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await eventStore.PostAsync("/api/v1/commands", payload, cancellationToken).ConfigureAwait(false);
        Volatile.Write(ref _lastStatusCode, (int)response.StatusCode);
        string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Accepted)
        {
            Interlocked.Increment(ref _acceptedCommands);
            _acceptedTenants.TryAdd(command.GetProperty("messageId").GetString()!, tenant!);
        }

        return Results.Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json", statusCode: (int)response.StatusCode);
    }
}
