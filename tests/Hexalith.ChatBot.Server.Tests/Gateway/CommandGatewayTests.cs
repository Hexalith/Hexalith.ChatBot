using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed class CommandGatewayTests
{
    private const string ActorId = "actor-alpha";
    private const string BoundTenant = "tenant-alpha";
    private const string OtherTenant = "tenant-beta";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TaskId = "01ARZ3NDEKTSV4RRFFQ69G5FAX";

    [Fact]
    public async Task GatewayShouldExecuteAdmissionStagesBeforeDispatch()
    {
        List<string> stages = [];
        RecordingDispatcher dispatcher = new(stages);
        CommandGateway gateway = new(
            new RecordingAuthenticationStage(stages),
            new RecordingTenantBindingStage(stages),
            new RecordingAuthorizationStage(stages, ChatBotAuthorizationResult.Allowed()),
            new RecordingRiskClassifier(stages),
            new RecordingApprovalGate(stages),
            new RecordingIdempotencyStore(stages),
            new RecordingAuditWriter(stages),
            dispatcher);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "allowed-resource")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        stages.ShouldBe(
            [
                "auth",
                "tenant-bind",
                "authorize",
                "risk-classify",
                "approval-gate",
                "coarse-idempotency",
                "pre-commit-audit",
                "dispatch",
                "post-commit-audit",
            ]);
    }

    [Fact]
    public async Task MissingAuthenticationShouldReturnSafeProblemDetailsAndSkipDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(dispatcher, auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(new ClaimsPrincipal(new ClaimsIdentity()), new TenantScopedCommand(BoundTenant, "payload-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(401);
        result.Problem.Category.ShouldBe(ProblemDetailsCategory.Authentication_failure);
        result.Problem.Code.ShouldBe("authentication_denied");
        result.Problem.CorrelationId.ShouldBe(CorrelationId);
        result.Problem.TaskId.ShouldBe(TaskId);
        result.Problem.Details.Visibility.ShouldBe(ProblemDetailsDetailsVisibility.Metadata_only);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        auditWriter.AuthorizationFailures[0].ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthenticationDenied);
        Serialized(result.Problem).ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task MissingTenantContextShouldReturnSafeProblemDetailsAndSkipDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(dispatcher, auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(null), new TenantScopedCommand(BoundTenant, "payload-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(403);
        result.Problem.Category.ShouldBe(ProblemDetailsCategory.Authorization_denied);
        result.Problem.Code.ShouldBe("authorization_denied");
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        auditWriter.AuthorizationFailures[0].ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMissing);
        Serialized(result.Problem).ShouldNotContain(BoundTenant, Case.Insensitive);
        Serialized(result.Problem).ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task AmbiguousTenantContextShouldFailClosedAndSkipDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(dispatcher, auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(PrincipalWithTenantClaims(BoundTenant, OtherTenant), new TenantScopedCommand(BoundTenant, "payload-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(403);
        result.Problem.Code.ShouldBe("authorization_denied");
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        auditWriter.AuthorizationFailures[0].ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMissing);

        string serialized = Serialized(result.Problem);
        serialized.ShouldNotContain(BoundTenant, Case.Insensitive);
        serialized.ShouldNotContain(OtherTenant, Case.Insensitive);
        serialized.ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task MalformedTenantContextShouldFailClosedAndSkipDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(dispatcher, auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(PrincipalWithTenantClaims("tenant alpha"), new TenantScopedCommand(BoundTenant, "payload-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(403);
        result.Problem.Code.ShouldBe("authorization_denied");
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        auditWriter.AuthorizationFailures[0].ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMissing);
        Serialized(result.Problem).ShouldNotContain("tenant alpha", Case.Insensitive);
        Serialized(result.Problem).ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task CrossTenantTargetMismatchShouldAuditOnceAndNeverDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(dispatcher, auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(OtherTenant, "restricted-project-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(403);
        result.Problem.Code.ShouldBe("authorization_denied");
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        ChatBotAuthorizationFailureAuditFact fact = auditWriter.AuthorizationFailures[0];
        fact.TenantId.ShouldBe(BoundTenant);
        fact.ActorId.ShouldBe(ActorId);
        fact.CommandType.ShouldBe(nameof(TenantScopedCommand));
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMismatch);
        fact.CorrelationId.ShouldBe(CorrelationId);
        fact.TaskId.ShouldBe(TaskId);

        string serialized = Serialized(result.Problem);
        serialized.ShouldNotContain(BoundTenant, Case.Insensitive);
        serialized.ShouldNotContain(OtherTenant, Case.Insensitive);
        serialized.ShouldNotContain("restricted-project-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task CrossTenantScopedIdentifierMismatchShouldAuditOnceAndNeverDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(dispatcher, auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedIdentifierCommand($"{OtherTenant}:projects:project-1")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(403);
        result.Problem.Code.ShouldBe("authorization_denied");
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        ChatBotAuthorizationFailureAuditFact fact = auditWriter.AuthorizationFailures[0];
        fact.TenantId.ShouldBe(BoundTenant);
        fact.ActorId.ShouldBe(ActorId);
        fact.CommandType.ShouldBe(nameof(TenantScopedIdentifierCommand));
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMismatch);
        fact.CorrelationId.ShouldBe(CorrelationId);
        fact.TaskId.ShouldBe(TaskId);

        string serialized = Serialized(result.Problem);
        serialized.ShouldNotContain(BoundTenant, Case.Insensitive);
        serialized.ShouldNotContain(OtherTenant, Case.Insensitive);
        serialized.ShouldNotContain("project-1", Case.Insensitive);
    }

    [Fact]
    public async Task AuthorizationDeniedAndSafeNotFoundShouldBeIndistinguishableToCaller()
    {
        RecordingDispatcher deniedDispatcher = new();
        RecordingDispatcher hiddenDispatcher = new();
        CommandGateway deniedGateway = Gateway(
            deniedDispatcher,
            authorizationStage: new DenyingAuthorizationStage(ChatBotAuthorizationReasonCodes.AuthorizationDenied));
        CommandGateway hiddenGateway = Gateway(
            hiddenDispatcher,
            authorizationStage: new DenyingAuthorizationStage(ChatBotAuthorizationReasonCodes.SafeNotFound));

        ChatBotGatewayResult denied = await deniedGateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "denied-resource")),
            TestContext.Current.CancellationToken);
        ChatBotGatewayResult hidden = await hiddenGateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "missing-resource")),
            TestContext.Current.CancellationToken);

        deniedDispatcher.DispatchCount.ShouldBe(0);
        hiddenDispatcher.DispatchCount.ShouldBe(0);
        denied.Problem.ShouldNotBeNull();
        hidden.Problem.ShouldNotBeNull();
        denied.Problem.Status.ShouldBe(hidden.Problem.Status);
        denied.Problem.Category.ShouldBe(hidden.Problem.Category);
        denied.Problem.Code.ShouldBe(hidden.Problem.Code);
        denied.Problem.Message.ShouldBe(hidden.Problem.Message);
        denied.Problem.ClientAction.ShouldBe(hidden.Problem.ClientAction);
        denied.Problem.Retryable.ShouldBe(hidden.Problem.Retryable);
        denied.Problem.Details.Visibility.ShouldBe(hidden.Problem.Details.Visibility);
    }

    private static CommandGateway Gateway(
        ICommandDispatcher dispatcher,
        IAuthorizationStage? authorizationStage = null,
        IAuditWriter? auditWriter = null)
        => new(
            new ClaimsAuthenticationStage(),
            new ClaimsTenantBindingStage(),
            authorizationStage ?? new PassThroughAuthorizationStage(),
            new PassThroughRiskClassifier(),
            new PassThroughApprovalGate(),
            new PassThroughIdempotencyStore(),
            auditWriter ?? new RecordingAuditWriter(),
            dispatcher);

    private static ChatBotCommandSubmission Submission(ClaimsPrincipal principal, object command)
        => new(
            principal,
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = command.GetType().Name,
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            CorrelationId,
            TaskId);

    private static ClaimsPrincipal Principal(string? tenantId)
    {
        List<Claim> claims = [new("sub", ActorId)];
        if (tenantId is not null)
        {
            claims.Add(new Claim("eventstore:tenant", tenantId));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static ClaimsPrincipal PrincipalWithTenantClaims(params string[] tenantIds)
    {
        List<Claim> claims = [new("sub", ActorId)];
        claims.AddRange(tenantIds.Select(static tenantId => new Claim("eventstore:tenant", tenantId)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static string Serialized(ProblemDetails problem)
        => JsonSerializer.Serialize(problem, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private sealed record TenantScopedCommand(string TenantId, string ResourceName);

    private sealed record TenantScopedIdentifierCommand(string ProjectId);

    private sealed class RecordingAuthenticationStage(List<string> stages) : IAuthenticationStage
    {
        public ValueTask<ChatBotAuthenticationResult> AuthenticateAsync(ChatBotCommandSubmission submission, CancellationToken cancellationToken)
        {
            stages.Add("auth");
            return ValueTask.FromResult(ChatBotAuthenticationResult.Authenticated(ActorId, submission.Principal));
        }
    }

    private sealed class RecordingTenantBindingStage(List<string> stages) : ITenantBindingStage
    {
        public ValueTask<ChatBotTenantBindingResult> BindTenantAsync(
            ChatBotCommandSubmission submission,
            ChatBotAuthenticatedActor actor,
            CancellationToken cancellationToken)
        {
            stages.Add("tenant-bind");
            return ValueTask.FromResult(ChatBotTenantBindingResult.Bound(new ChatBotTenantBinding(BoundTenant)));
        }
    }

    private sealed class RecordingAuthorizationStage(List<string> stages, ChatBotAuthorizationResult result) : IAuthorizationStage
    {
        public ValueTask<ChatBotAuthorizationResult> AuthorizeAsync(
            ChatBotCommandSubmission submission,
            ChatBotAuthenticatedActor actor,
            ChatBotTenantBinding tenantBinding,
            CancellationToken cancellationToken)
        {
            stages.Add("authorize");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class RecordingRiskClassifier(List<string> stages) : IRiskClassifier
    {
        public ValueTask<ChatBotRiskClassification> ClassifyAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            stages.Add("risk-classify");
            return ValueTask.FromResult(ChatBotRiskClassification.PassThrough);
        }
    }

    private sealed class RecordingApprovalGate(List<string> stages) : IApprovalGate
    {
        public ValueTask<ChatBotApprovalResult> EvaluateAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            stages.Add("approval-gate");
            return ValueTask.FromResult(ChatBotApprovalResult.Approved);
        }
    }

    private sealed class RecordingIdempotencyStore(List<string>? stages = null) : IIdempotencyStore
    {
        public ValueTask RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            stages?.Add("coarse-idempotency");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingAuditWriter(List<string>? stages = null) : IAuditWriter
    {
        public List<ChatBotAuthorizationFailureAuditFact> AuthorizationFailures { get; } = [];

        public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
        {
            AuthorizationFailures.Add(fact);
            return ValueTask.CompletedTask;
        }

        public ValueTask RecordPreCommitAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            stages?.Add("pre-commit-audit");
            return ValueTask.CompletedTask;
        }

        public ValueTask RecordPostCommitAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            stages?.Add("post-commit-audit");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingDispatcher(List<string>? stages = null) : ICommandDispatcher
    {
        public int DispatchCount { get; private set; }

        public ValueTask<ChatBotDispatchResult> DispatchAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            DispatchCount++;
            stages?.Add("dispatch");
            return ValueTask.FromResult(new ChatBotDispatchResult(DateTimeOffset.UtcNow));
        }
    }

    private sealed class DenyingAuthorizationStage(string reasonCode) : IAuthorizationStage
    {
        public ValueTask<ChatBotAuthorizationResult> AuthorizeAsync(
            ChatBotCommandSubmission submission,
            ChatBotAuthenticatedActor actor,
            ChatBotTenantBinding tenantBinding,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(ChatBotAuthorizationResult.Denied(reasonCode));
    }
}
