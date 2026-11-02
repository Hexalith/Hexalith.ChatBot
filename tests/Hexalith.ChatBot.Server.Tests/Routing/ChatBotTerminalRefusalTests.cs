using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Routing;
using Hexalith.ChatBot.Server.Tests.TrustedAuthority;
using Hexalith.ChatBot.Tests.TrustedAuthority;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Routing;

/// <summary>Compatibility controls for the real ordinary SDK refusal seam; these supply no native qualification.</summary>
public sealed class ChatBotTerminalRefusalTests
{
    private const string Note = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAA";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAB";
    private const string PrivateText = "must-not-appear-in-denial";

    /// <summary>Exercises exact denial ordering without calling accepting admission, protected state or dispatch.</summary>
    [Theory]
    [InlineData("valid", ChatBotAuthorizationReasonCodes.AuthorizationDenied)]
    [InlineData("null-extensions", ChatBotAuthorizationReasonCodes.AuthorizationDenied)]
    [InlineData("unsafe-identifiers", ChatBotAuthorizationReasonCodes.AuthorizationDenied)]
    [InlineData("missing-relay", ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("user-mismatch", ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("tenant-mismatch", ChatBotAuthorizationReasonCodes.TenantMismatch)]
    [InlineData("ambiguous-origin", ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("case-origin", ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("null-origin", ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("empty-origin", ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("invalid-origin", ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("null-actor", ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("empty-client", ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("malformed-payload", ChatBotAuthorizationReasonCodes.InvalidCommandPayload)]
    [InlineData("authentication-before-payload", ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("tenant-before-payload", ChatBotAuthorizationReasonCodes.TenantMismatch)]
    public async Task UnsupportedCommandRetainsTypedDenialOrderAndSafeAuditing(string mode, string reason)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        IIdempotencyStore reservations = DispatchProxy.Create<IIdempotencyStore, ProtectedAccessProbe>();
        ICommandDispatcher dispatch = DispatchProxy.Create<ICommandDispatcher, ProtectedAccessProbe>();
        CountingGovernedOperationStore state = new()
        {
            View = new("tenant-alpha", Note, GovernedOperationView.CurrentSchemaVersion, "retained", "v1", "metadata_only", "test", 1, clock.UtcNow, clock.UtcNow),
        };
        GovernedOperationView retained = state.View;
        InMemoryAuditWriter audit = new();
        InMemoryAuthorizationFailureCounter counter = new(clock);
        ClaimsPrincipal principal = mode is "user-mismatch" or "tenant-mismatch" or "tenant-before-payload"
            ? TrustedAuthorityFixture.Principal() : Relay(mode is "missing-relay" or "authentication-before-payload" ? "wrong-operation" : "domain-service:process");
        using WebApplicationFactory<Program> factory = Factory(principal, services =>
        {
            services.AddSingleton<ISystemClock>(clock);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
            services.AddSingleton(reservations);
            services.AddSingleton(dispatch);
            services.AddSingleton<IGovernedOperationProjectionStore>(state);
            services.AddSingleton<IAuditWriter>(audit);
            services.AddSingleton<IAuthorizationFailureCounter>(counter);
        });
        using HttpClient client = factory.CreateClient();
        Dictionary<string, string> extensions = new(StringComparer.Ordinal)
        {
            ["actorType"] = "service", ["serviceClientId"] = "client-alpha", ["taskId"] = "sensitive\nunsafe",
        };
        switch (mode)
        {
            case "ambiguous-origin": extensions["surfaceOrigin"] = "api"; extensions["SurfaceOrigin"] = "api"; break;
            case "case-origin": extensions["SurfaceOrigin"] = "api"; break;
            case "null-origin": extensions["surfaceOrigin"] = null!; break;
            case "empty-origin": extensions["surfaceOrigin"] = ""; break;
            case "invalid-origin": extensions["surfaceOrigin"] = "invented"; break;
            case "null-actor": extensions["actorType"] = null!; break;
            case "empty-client": extensions["serviceClientId"] = ""; break;
        }
        CommandEnvelope command = Command() with
        {
            MessageId = mode == "unsafe-identifiers" ? "unsafe/command" : CommandId,
            CorrelationId = mode == "unsafe-identifiers" ? "unsafe/correlation" : CorrelationId,
            TenantId = mode is "tenant-mismatch" or "tenant-before-payload" ? "tenant-other" : "tenant-alpha",
            UserId = mode is "tenant-mismatch" or "tenant-before-payload" ? "actor-alpha" : "service-account-client-alpha",
            Payload = mode is "malformed-payload" or "authentication-before-payload" or "tenant-before-payload" ? "{"u8.ToArray() : Command().Payload,
            Extensions = mode == "null-extensions" ? null : extensions,
        };
        using HttpResponseMessage response = await client.PostAsJsonAsync("/process", new DomainServiceRequest(command, null), TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        DomainServiceWireResult result = (await response.Content.ReadFromJsonAsync<DomainServiceWireResult>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldNotBeNull();
        result.IsRejection.ShouldBeTrue();
        result.ResultPayload.ShouldBeNull();
        using (JsonDocument wire = JsonDocument.Parse(responseBody))
        {
            wire.RootElement.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)
                .ShouldBe(["events", "isRejection", "resultPayload"]);
            wire.RootElement.GetProperty("events")[0].EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)
                .ShouldBe(["eventTypeName", "payload", "serializationFormat"]);
            wire.RootElement.GetProperty("resultPayload").ValueKind.ShouldBe(JsonValueKind.Null);
        }
        responseBody.ShouldNotContain(PrivateText);
        DomainServiceWireEvent rejection = result.Events.ShouldHaveSingleItem();
        rejection.EventTypeName.ShouldBe(typeof(ChatBotDomainServiceAdmissionRejected).FullName);
        rejection.SerializationFormat.ShouldBe("json");
        rejection.Payload.Length.ShouldBeLessThanOrEqualTo(1024);
        using (JsonDocument payload = JsonDocument.Parse(rejection.Payload))
        {
            payload.RootElement.GetProperty("ReasonCode").GetString().ShouldBe(reason);
            payload.RootElement.GetProperty("CommandType").GetString().ShouldBe(nameof(ExecuteAdminQueueOperation));
            payload.RootElement.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)
                .ShouldBe(["CommandId", "CommandType", "CorrelationId", "ReasonCode"]);
            payload.RootElement.GetProperty("CommandId").GetString().ShouldBe(mode == "unsafe-identifiers" ? null : CommandId);
            payload.RootElement.GetProperty("CorrelationId").GetString().ShouldBe(mode == "unsafe-identifiers" ? null : CorrelationId);
            payload.RootElement.GetRawText().ShouldNotContain(PrivateText);
            payload.RootElement.GetRawText().ShouldNotContain("PrivateText");
        }
        if (reason == ChatBotAuthorizationReasonCodes.InvalidCommandPayload)
        {
            audit.AuthorizationFailures.ShouldBeEmpty();
            counter.ReadAndReset().ShouldBeEmpty();
        }
        else
        {
            var fact = audit.AuthorizationFailures.ShouldHaveSingleItem();
            fact.ReasonCode.ShouldBe(reason);
            fact.TaskId.ShouldBeNull();
            fact.ShouldBe(new ChatBotAuthorizationFailureAuditFact(
                reason == ChatBotAuthorizationReasonCodes.AuthenticationDenied && mode != "user-mismatch" ? "unavailable" : "tenant-alpha",
                mode == "user-mismatch" || reason == ChatBotAuthorizationReasonCodes.TenantMismatch ? "actor-alpha"
                    : reason == ChatBotAuthorizationReasonCodes.AuthenticationDenied ? "anonymous" : "service-account-client-alpha",
                nameof(ExecuteAdminQueueOperation), reason, mode == "unsafe-identifiers" ? "unavailable" : CorrelationId,
                null, "api"));
            JsonSerializer.Serialize(fact).ShouldNotContain(PrivateText);
            JsonSerializer.Serialize(fact).ShouldNotContain("PrivateText");
            counter.ReadAndReset().ShouldHaveSingleItem().FailureCount.ShouldBe(1);
        }
        owner.Requests.ShouldBeEmpty();
        audit.Envelopes.ShouldBeEmpty();
        ((ProtectedAccessProbe)(object)reservations).Calls.ShouldBe(0);
        ((ProtectedAccessProbe)(object)dispatch).Calls.ShouldBe(0);
        state.Reads.ShouldBe(0); state.Writes.ShouldBe(0); state.View.ShouldBe(retained);
        await AssertOrdinaryAuthorityAsync(factory.Services).ConfigureAwait(true);
    }

    /// <summary>Changes only actual SDK sender authority after a valid typed refusal, then proves no callback or work.</summary>
    [Theory]
    [InlineData("missing-root")]
    [InlineData("stale-root")]
    [InlineData("duplicate-root")]
    [InlineData("missing-epoch")]
    [InlineData("stale-epoch")]
    [InlineData("duplicate-epoch")]
    [InlineData("missing-process-proof")]
    [InlineData("stale-process-proof")]
    [InlineData("duplicate-process-proof")]
    [InlineData("missing-surface")]
    [InlineData("duplicate-surface")]
    public async Task InvalidSdkAuthorityCannotInvokeTerminalCallback(string mode)
    {
        ArgumentNullException.ThrowIfNull(mode);
        InMemoryAuditWriter audit = new();
        IIdempotencyStore reservations = DispatchProxy.Create<IIdempotencyStore, ProtectedAccessProbe>();
        ClaimsPrincipal principal = Relay("domain-service:process");
        using WebApplicationFactory<Program> factory = Factory(principal, services =>
        {
            services.AddSingleton<IAuditWriter>(audit);
            services.AddSingleton(reservations);
        });
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage valid = await client.PostAsJsonAsync("/process", new DomainServiceRequest(Command(), null), TestContext.Current.CancellationToken).ConfigureAwait(true);
        valid.StatusCode.ShouldBe(HttpStatusCode.OK);
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        HttpRouteCatalogFixture fixture = factory.Services.GetRequiredService<HttpRouteCatalogFixture>();
        if (mode.EndsWith("surface", StringComparison.Ordinal))
        {
            ClaimsIdentity identity = (ClaimsIdentity)principal.Identity!;
            if (mode == "missing-surface") identity.RemoveClaim(identity.FindFirst("eventstore:surface")!);
            else identity.AddClaim(new Claim("eventstore:surface", "service"));
        }
        else
        {
            fixture.TransformInvocation = request =>
            {
                string header = mode.EndsWith("root", StringComparison.Ordinal) ? RouteCatalogInvocationHandler.RootHeader
                    : mode.EndsWith("epoch", StringComparison.Ordinal) ? RouteCatalogInvocationHandler.EpochHeader
                    : request.Headers.Select(entry => entry.Key).Single(name => name.StartsWith("X-Hexalith-Route-Catalog-", StringComparison.Ordinal)
                        && name != RouteCatalogInvocationHandler.RootHeader && name != RouteCatalogInvocationHandler.EpochHeader
                        && name != RouteCatalogInvocationHandler.BindingHeader);
                string[] original = request.Headers.GetValues(header).ToArray();
                request.Headers.Remove(header);
                if (mode.StartsWith("stale", StringComparison.Ordinal)) request.Headers.TryAddWithoutValidation(header, header == RouteCatalogInvocationHandler.EpochHeader ? "99" : new string('0', 64));
                if (mode.StartsWith("duplicate", StringComparison.Ordinal)) request.Headers.TryAddWithoutValidation(header, new[] { original.Single(), original.Single() });
            };
        }
        using HttpResponseMessage invalid = await client.PostAsJsonAsync("/process", new DomainServiceRequest(Command(), null), TestContext.Current.CancellationToken).ConfigureAwait(true);
        invalid.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        audit.AuthorizationFailures.ShouldHaveSingleItem();
        audit.Envelopes.ShouldBeEmpty();
        ((ProtectedAccessProbe)(object)reservations).Calls.ShouldBe(0);
        await AssertOrdinaryAuthorityAsync(factory.Services).ConfigureAwait(true);
    }

    /// <summary>The authenticated unknown-domain projection retains its 404 and no protected state effects.</summary>
    [Fact]
    public async Task UnknownProjectionPreservesNotFoundAndCatalogDenialWithoutEffects()
    {
        CountingGovernedOperationStore state = new();
        InMemoryAuditWriter audit = new();
        using WebApplicationFactory<Program> factory = Factory(Relay("domain-service:project"), services =>
        {
            services.AddSingleton<IGovernedOperationProjectionStore>(state);
            services.AddSingleton<IAuditWriter>(audit);
        });
        using HttpClient client = factory.CreateClient();
        ProjectionRequest request = new("tenant-alpha", "folders", Note, []);
        using HttpResponseMessage valid = await client.PostAsJsonAsync("/project", request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        valid.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        factory.Services.GetRequiredService<HttpRouteCatalogFixture>().TransformInvocation = invocation =>
        {
            invocation.Headers.Remove(RouteCatalogInvocationHandler.RootHeader);
            invocation.Headers.TryAddWithoutValidation(RouteCatalogInvocationHandler.RootHeader, new string('0', 64));
        };
        using HttpResponseMessage invalid = await client.PostAsJsonAsync("/project", request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        invalid.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        state.Reads.ShouldBe(0); state.Writes.ShouldBe(0); state.View.ShouldBeNull();
        audit.AuthorizationFailures.ShouldBeEmpty(); audit.Envelopes.ShouldBeEmpty();
        await AssertOrdinaryAuthorityAsync(factory.Services).ConfigureAwait(true);
    }

    /// <summary>The sender and receiver keep committed leases while the asynchronous terminal callback is active.</summary>
    [Fact]
    public async Task TerminalCallbackHoldsAuthorityWhileDenialAuditingIsActive()
    {
        TerminalRefusalLeaseAuditWriter audit = new();
        using WebApplicationFactory<Program> factory = Factory(Relay("domain-service:process"), services => services.AddSingleton<IAuditWriter>(audit));
        using HttpClient client = factory.CreateClient();
        Task<HttpResponseMessage> pending = client.PostAsJsonAsync("/process", new DomainServiceRequest(Command(), null), TestContext.Current.CancellationToken);
        try
        {
            Task first = await Task.WhenAny(audit.Entered, pending)
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken).ConfigureAwait(true);
            if (first == pending)
            {
                HttpResponseMessage early;
                try { early = await pending.ConfigureAwait(true); }
                catch (Exception exception)
                {
                    throw new InvalidOperationException("The request failed before denial auditing began.", exception);
                }
                using (early)
                {
                    throw new InvalidOperationException($"The request returned HTTP {(int)early.StatusCode} before denial auditing began.");
                }
            }
            pending.IsCompleted.ShouldBeFalse();
            var state = (await factory.Services.GetRequiredService<IRouteCatalogStateStore>().ReadAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).State;
            state.Leases.Count.ShouldBe(2);
        }
        finally { audit.Release(); }
        using HttpResponseMessage response = await pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<DomainServiceWireResult>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true))!.IsRejection.ShouldBeTrue();
        await AssertOrdinaryAuthorityAsync(factory.Services).ConfigureAwait(true);
    }

    /// <summary>Removed incoming headers never survive transmission; content headers and bytes use the outgoing content.</summary>
    [Fact]
    public async Task InvocationTransportSynchronizesOutgoingGeneralAndContentHeaders()
    {
        DefaultHttpContext context = new();
        context.Request.Headers["X-Removed-General"] = "original";
        context.Request.Headers["Content-Language"] = "original";
        context.Request.Headers.ContentType = "application/original";
        using HttpRouteCatalogFixture fixture = new()
        {
            TransformInvocation = request =>
            {
                request.Headers.Remove("X-Removed-General");
                request.Content!.Headers.Remove("Content-Language");
            },
        };
        using ServiceProvider services = new ServiceCollection().AddSingleton(fixture).BuildServiceProvider();
        context.RequestServices = services;
        using HttpRequestMessage outgoing = new(HttpMethod.Post, "http://localhost/process")
        {
            Content = new StringContent("outgoing-body", Encoding.UTF8, "application/json"),
        };
        outgoing.Headers.Add("X-Removed-General", "outgoing-removed");
        outgoing.Headers.Add("X-Transmitted", "outgoing");
        outgoing.Content.Headers.Add("Content-Language", "outgoing-removed");
        bool delivered = false;
        using HttpMessageInvoker transport = new(new HttpRouteCatalogInvocationTransport(context, async () =>
        {
            delivered = true;
            context.Request.Headers.ContainsKey("X-Removed-General").ShouldBeFalse();
            context.Request.Headers.ContainsKey("Content-Language").ShouldBeFalse();
            context.Request.Headers["X-Transmitted"].ToString().ShouldBe("outgoing");
            context.Request.ContentType.ShouldBe("application/json; charset=utf-8");
            using StreamReader reader = new(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            (await reader.ReadToEndAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBe("outgoing-body");
        }));
        using HttpResponseMessage response = await transport.SendAsync(outgoing, TestContext.Current.CancellationToken).ConfigureAwait(true);
        delivered.ShouldBeTrue();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>A configured ordinary channel reaches denial auditing; an explicit invalid token cannot reach any protected effect.</summary>
    [Fact]
    public async Task OrdinaryDeliveryUsesConfiguredChannelAndPreservesInvalidCallerToken()
    {
        InMemoryAuditWriter audit = new();
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        IIdempotencyStore reservations = DispatchProxy.Create<IIdempotencyStore, ProtectedAccessProbe>();
        ICommandDispatcher dispatch = DispatchProxy.Create<ICommandDispatcher, ProtectedAccessProbe>();
        CountingGovernedOperationStore state = new();
        using WebApplicationFactory<Program> factory = Factory(Relay("domain-service:process"), services =>
        {
            services.AddSingleton<IAuditWriter>(audit);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
            services.AddSingleton(reservations);
            services.AddSingleton(dispatch);
            services.AddSingleton<IGovernedOperationProjectionStore>(state);
        });
        using HttpClient client = factory.CreateClient();
        HttpRouteCatalogFixture fixture = factory.Services.GetRequiredService<HttpRouteCatalogFixture>();
        fixture.TransformInvocation = request => request.Headers.Contains(DaprAppChannelToken.HeaderName).ShouldBeTrue();
        using HttpResponseMessage valid = await client.PostAsJsonAsync("/process", new DomainServiceRequest(Command(), null), TestContext.Current.CancellationToken).ConfigureAwait(true);
        valid.StatusCode.ShouldBe(HttpStatusCode.OK);
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        fixture.TransformInvocation = request => request.Headers.GetValues(DaprAppChannelToken.HeaderName).ShouldBe(["invalid-fixture-channel"]);
        using HttpRequestMessage invalid = new(HttpMethod.Post, "/process") { Content = JsonContent.Create(new DomainServiceRequest(Command(), null)) };
        invalid.Headers.Add(DaprAppChannelToken.HeaderName, "invalid-fixture-channel");
        using HttpResponseMessage refused = await client.SendAsync(invalid, TestContext.Current.CancellationToken).ConfigureAwait(true);
        refused.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        audit.AuthorizationFailures.ShouldHaveSingleItem(); audit.Envelopes.ShouldBeEmpty();
        owner.Requests.ShouldBeEmpty();
        ((ProtectedAccessProbe)(object)reservations).Calls.ShouldBe(0);
        ((ProtectedAccessProbe)(object)dispatch).Calls.ShouldBe(0);
        state.Reads.ShouldBe(0); state.Writes.ShouldBe(0); state.View.ShouldBeNull();
        await AssertOrdinaryAuthorityAsync(factory.Services).ConfigureAwait(true);
    }

    private static WebApplicationFactory<Program> Factory(ClaimsPrincipal principal, Action<IServiceCollection> configure)
        => new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureFiniteReceiver().ConfigureServices(services =>
        {
            SdkRouteFixtureAuthorization.AllowCanonicalRouteHandlers(services);
            services.AddFixturePrincipalAuthentication().AddSingleton<IStartupFilter>(new TrustedAuthorityPrincipalStartupFilter(_ => principal));
            configure(services);
        }));

    private static CommandEnvelope Command() => new(CommandId, "tenant-alpha", "chatbot", Note, nameof(ExecuteAdminQueueOperation),
        JsonSerializer.SerializeToUtf8Bytes(new { OperationId = Note, PrivateText }), CorrelationId, null,
        "service-account-client-alpha", new() { ["actorType"] = "service", ["serviceClientId"] = "client-alpha" });

    private static ClaimsPrincipal Relay(string operation) => new(new ClaimsIdentity([
        new(ClaimTypes.NameIdentifier, "workload:eventstore"), new("eventstore:workload", "eventstore"),
        new("eventstore:operation", operation), new("eventstore:surface", "service")], "EventStoreWorkload"));

    private static async Task AssertOrdinaryAuthorityAsync(IServiceProvider services)
    {
        var state = (await services.GetRequiredService<IRouteCatalogStateStore>().ReadAsync(TestContext.Current.CancellationToken).ConfigureAwait(false)).State;
        state.Leases.ShouldBeEmpty();
        RouteCatalogActivation committed = state.Committed.ShouldNotBeNull();
        committed.TechnicalHttpReceivers.ShouldBeNull();
        committed.RequiredHosts.ShouldAllBe(host => host.ExecutableDigest != null);
        RouteCatalogEnvelope catalog = RouteCatalogCodec.Decode(committed.CanonicalCatalog, committed.RootDigest);
        catalog.SchemaVersion.ShouldBe(3);
        catalog.Declarations.Count.ShouldBe(73);
        catalog.Declarations.ShouldNotContain(declaration => declaration.MessageType == nameof(ExecuteAdminQueueOperation));
        ChatBotCatalogAdapters.Create().Count.ShouldBe(64);
        ChatBotCanonicalDispatchTarget.IsSupported(nameof(ExecuteAdminQueueOperation)).ShouldBeFalse();
    }
}
