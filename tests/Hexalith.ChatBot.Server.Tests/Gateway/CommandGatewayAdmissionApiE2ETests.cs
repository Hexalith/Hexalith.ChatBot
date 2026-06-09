using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed class CommandGatewayAdmissionApiE2ETests
{
    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptTenantBoundSubmissionAfterAdmissionStages()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "allowed-resource"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        dispatcher.DispatchCount.ShouldBe(1);
        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.TenantId == "tenant-alpha");
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.ActorId == "actor-alpha");
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.CommandName == "TenantScopedCommand");

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("lifecycleState").GetString().ShouldBe("Proposed");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("allowed-resource", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldRejectUnauthenticatedSubmissionBeforeDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: null,
            dispatcher,
            auditWriter);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "restricted-project-sentinel"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        ChatBotAuthorizationFailureAuditFact fact = auditWriter.AuthorizationFailures.Single();
        fact.TenantId.ShouldBe("unavailable");
        fact.ActorId.ShouldBe("anonymous");
        fact.CommandType.ShouldBe("TenantScopedCommand");
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthenticationDenied);
        fact.CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        fact.TaskId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authentication_failure");
        root.GetProperty("code").GetString().ShouldBe(ChatBotAuthorizationReasonCodes.AuthenticationDenied);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("restricted-project-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldRejectCrossTenantTargetBeforeDispatchAndRecordMetadataOnlyAuditFact()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                CommandSubmissionRequest(
                    "tenant-beta",
                    "restricted-project-sentinel-C:\\\\secret\\\\item-/tmp/raw-exception"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        ChatBotAuthorizationFailureAuditFact fact = auditWriter.AuthorizationFailures.Single();
        fact.TenantId.ShouldBe("tenant-alpha");
        fact.ActorId.ShouldBe("actor-alpha");
        fact.CommandType.ShouldBe("TenantScopedCommand");
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMismatch);
        fact.CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        fact.TaskId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authorization_denied");
        root.GetProperty("code").GetString().ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("tenant-beta", Case.Insensitive);
        body.ShouldNotContain("restricted-project-sentinel", Case.Insensitive);
        body.ShouldNotContain("/tmp/raw-exception", Case.Insensitive);
        body.ShouldNotContain("C:\\", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldFailClosedWhenPreCommitAuditIsUnavailable()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new() { PreCommitResult = AuditWriteResult.Unavailable() };
        InMemoryAuditReplayIntentQueue replayQueue = new();
        InMemoryOperatorAlertSink alertSink = new();
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            replayQueue,
            alertSink);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                CommandSubmissionRequest("tenant-alpha", "allowed-resource-C:\\\\secret\\\\item-/tmp/raw-exception"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.Count.ShouldBe(1);
        auditWriter.Envelopes[0].Phase.ShouldBe(AuditCommitPhase.PreCommit);
        replayQueue.Intents.Count.ShouldBe(1);
        replayQueue.Intents[0].Kind.ShouldBe(AuditReplayIntentKind.PreCommitOperationReplay);
        replayQueue.Intents[0].ReasonCode.ShouldBe(AuditFailureReasonCodes.AuditUnavailable);
        alertSink.Alerts.Count.ShouldBe(1);
        alertSink.Alerts[0].Kind.ShouldBe(OperatorAlertKind.AuditUnavailable);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("internal_error");
        root.GetProperty("code").GetString().ShouldBe(AuditFailureReasonCodes.AuditUnavailable);
        root.GetProperty("retryable").GetBoolean().ShouldBeTrue();
        root.GetProperty("clientAction").GetString().ShouldBe("retry-later");
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("allowed-resource", Case.Insensitive);
        body.ShouldNotContain("/tmp/raw-exception", Case.Insensitive);
        body.ShouldNotContain("C:\\", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptAndQueueReconciliationWhenPostCommitAuditFails()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new() { PostCommitResult = AuditWriteResult.Unavailable("post_commit_sink_unavailable") };
        InMemoryAuditReplayIntentQueue replayQueue = new();
        InMemoryOperatorAlertSink alertSink = new();
        InMemoryOperationStatusStore operationStatusStore = new();
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            replayQueue,
            alertSink,
            operationStatusStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "allowed-resource"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        dispatcher.DispatchCount.ShouldBe(1);
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        replayQueue.Intents.Count.ShouldBe(1);
        replayQueue.Intents[0].Kind.ShouldBe(AuditReplayIntentKind.PostCommitAuditReconciliation);
        replayQueue.Intents[0].ReasonCode.ShouldBe("post_commit_sink_unavailable");
        alertSink.Alerts.Count.ShouldBe(1);
        alertSink.Alerts[0].Kind.ShouldBe(OperatorAlertKind.PostCommitAuditReconciliationRequired);
        alertSink.Alerts[0].ReasonCode.ShouldBe("post_commit_sink_unavailable");

        OperationStatusRecord? status = await operationStatusStore
            .TryGetAsync("tenant-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAX", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        status.ShouldNotBeNull();
        status.AuditStatus.ShouldBe(OperationStatusRecord.AuditReconciling);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("allowed-resource", Case.Insensitive);
    }

    private static WebApplicationFactory<Program> GatewayFactory(
        string? tenantId,
        RecordingDispatcher dispatcher,
        RecordingAuditWriter auditWriter,
        InMemoryAuditReplayIntentQueue? replayQueue = null,
        InMemoryOperatorAlertSink? alertSink = null,
        InMemoryOperationStatusStore? operationStatusStore = null)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.ConfigureServices(
                    services =>
                    {
                        if (tenantId is not null)
                        {
                            services.AddSingleton<IStartupFilter>(new TestPrincipalStartupFilter(tenantId));
                        }

                        services.AddSingleton<ICommandDispatcher>(dispatcher);
                        services.AddSingleton<IAuditWriter>(auditWriter);
                        if (replayQueue is not null)
                        {
                            services.AddSingleton<IAuditReplayIntentQueue>(replayQueue);
                        }

                        if (alertSink is not null)
                        {
                            services.AddSingleton<IOperatorAlertSink>(alertSink);
                        }

                        if (operationStatusStore is not null)
                        {
                            services.AddSingleton<IOperationStatusStore>(operationStatusStore);
                        }

                        services.AddSingleton<IIdempotencyStore>(_ => new InMemoryCoarseIdempotencyStore(new SystemClock()));
                        services.AddSingleton<ISpineCommandAllowlist>(_ => new AllowAllSpineCommandAllowlist());
                    }));

    private static HttpRequestMessage CommandSubmissionRequest(string tenantId, string resourceName)
    {
        string payload =
            $$"""
            {
              "commandId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "commandType": "TenantScopedCommand",
              "command": {
                "tenantId": "{{tenantId}}",
                "resourceName": "{{resourceName}}"
              },
              "requestSchemaVersion": "v1"
            }
            """;

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        return request;
    }

    private sealed class TestPrincipalStartupFilter(string tenantId) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(
                    async (context, continuation) =>
                    {
                        context.User = new ClaimsPrincipal(new ClaimsIdentity(
                            [
                                new Claim("sub", "actor-alpha"),
                                new Claim("eventstore:tenant", tenantId),
                                new Claim("requester_authority_class", "project-contributor"),
                            ],
                            "test"));
                        await continuation().ConfigureAwait(false);
                    });
                next(app);
            };
    }

    private sealed class RecordingDispatcher : ICommandDispatcher
    {
        public int DispatchCount { get; private set; }

        public ValueTask<ChatBotDispatchResult> DispatchAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DispatchCount++;
            return ValueTask.FromResult(new ChatBotDispatchResult(new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero)));
        }
    }

    private sealed class RecordingAuditWriter : IAuditWriter
    {
        private readonly List<ChatBotAuthorizationFailureAuditFact> _authorizationFailures = [];
        private readonly List<AuditEnvelope> _envelopes = [];

        public IReadOnlyList<ChatBotAuthorizationFailureAuditFact> AuthorizationFailures => _authorizationFailures;

        public IReadOnlyList<AuditEnvelope> Envelopes => _envelopes;

        public AuditWriteResult PreCommitResult { get; init; } = AuditWriteResult.Success;

        public AuditWriteResult PostCommitResult { get; init; } = AuditWriteResult.Success;

        public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _authorizationFailures.Add(fact);
            return ValueTask.CompletedTask;
        }

        public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _envelopes.Add(envelope);
            return ValueTask.FromResult(PreCommitResult);
        }

        public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _envelopes.Add(envelope);
            return ValueTask.FromResult(PostCommitResult);
        }
    }

    private sealed class AllowAllSpineCommandAllowlist : ISpineCommandAllowlist
    {
        public bool IsAllowed(string? commandType) => true;
    }
}
