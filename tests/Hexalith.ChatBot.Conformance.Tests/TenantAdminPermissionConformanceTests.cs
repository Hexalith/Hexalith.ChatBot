using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

public sealed class TenantAdminPermissionConformanceTests
{
    [Theory]
    [InlineData(ChatBotSurfaceOrigin.Ui, ParticipantAuthorizationStage.ServiceActorValue, nameof(AssignTenantAdminRole), ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized)]
    [InlineData(ChatBotSurfaceOrigin.Api, ParticipantAuthorizationStage.ServiceActorValue, nameof(AssignTenantAdminRole), ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized)]
    [InlineData(ChatBotSurfaceOrigin.Cli, ParticipantAuthorizationStage.ServiceActorValue, nameof(AssignTenantAdminRole), ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized)]
    [InlineData(ChatBotSurfaceOrigin.Mcp, ParticipantAuthorizationStage.ServiceActorValue, nameof(AssignTenantAdminRole), ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized)]
    [InlineData(ChatBotSurfaceOrigin.Worker, ParticipantAuthorizationStage.ServiceActorValue, nameof(ExecuteAdminQueueOperation), ChatBotAuthorizationReasonCodes.AuthorizationDenied)]
    [InlineData(ChatBotSurfaceOrigin.Mailbox, ParticipantAuthorizationStage.ServiceActorValue, nameof(ExecuteAdminQueueOperation), ChatBotAuthorizationReasonCodes.AuthorizationDenied)]
    [InlineData(ChatBotSurfaceOrigin.Ai, ParticipantAuthorizationStage.AiActorValue, nameof(AssignTenantAdminRole), ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized)]
    [InlineData(ChatBotSurfaceOrigin.Ai, ParticipantAuthorizationStage.AiActorValue, nameof(ExecuteAdminQueueOperation), ChatBotAuthorizationReasonCodes.AuthorizationDenied)]
    public async Task AutomationSurfacesShouldNotBypassTenantAdminAuthorization(
        ChatBotSurfaceOrigin origin,
        string actorType,
        string commandType,
        string reasonCode)
    {
        BackendLaneOutcome outcome = await RunBackendLaneAsync(
            Principal(actorType, AdminRoles.TenantAdmin),
            Command(commandType),
            origin,
            TestContext.Current.CancellationToken);

        outcome.IsAccepted.ShouldBeFalse();
        outcome.AuthorizationFailures.Count.ShouldBe(1);
        outcome.AuthorizationFailures.Single().ReasonCode.ShouldBe(reasonCode);
        outcome.AuthorizationFailures.Single().SurfaceOrigin.ShouldBe(ChatBotSurfaceOrigins.ToWireValue(origin));
        outcome.DispatchCount.ShouldBe(0);
        outcome.IdempotencyRecordCount.ShouldBe(0);
        outcome.AuditEnvelopes.Count.ShouldBe(0);
        outcome.SerializedProblem.ShouldNotContain("tenant-admin", Case.Insensitive);
        outcome.SerializedProblem.ShouldNotContain("policy-snapshot-admin-v1", Case.Insensitive);
    }

    [Fact]
    public async Task HumanTenantAdminAssignmentShouldEmitMetadataOnlyAuditRefs()
    {
        BackendLaneOutcome outcome = await RunBackendLaneAsync(
            Principal(ParticipantAuthorizationStage.HumanActorValue, AdminRoles.TenantAdmin),
            AdminAssignment(),
            ChatBotSurfaceOrigin.Ui,
            TestContext.Current.CancellationToken);

        outcome.IsAccepted.ShouldBeTrue();
        outcome.DispatchCount.ShouldBe(1);
        outcome.AuthorizationFailures.ShouldBeEmpty();
        outcome.AuditEnvelopes.Count.ShouldBe(2);

        foreach (AuditEnvelope envelope in outcome.AuditEnvelopes)
        {
            envelope.SourceEvidenceRefs.ShouldContain("admin-role:tenant-admin");
            envelope.SourceEvidenceRefs.ShouldContain("admin-role:operations-admin");
            envelope.SourceEvidenceRefs.ShouldContain("admin-operation:assign-role");
            envelope.SourceEvidenceRefs.ShouldContain("admin-subject:actor-beta");
            envelope.SourceEvidenceRefs.ShouldContain("policy-snapshot:policy-snapshot-admin-v1");
            envelope.SourceEvidenceRefs.ShouldContain("reason:security-owner-request");
            envelope.SurfaceOrigin.ShouldBe(ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Ui));
        }

        string serializedAudit = JsonSerializer.Serialize(outcome.AuditEnvelopes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        serializedAudit.ShouldNotContain("raw", Case.Insensitive);
        serializedAudit.ShouldNotContain("bearer", Case.Insensitive);
        serializedAudit.ShouldNotContain("secret", Case.Insensitive);
        serializedAudit.ShouldNotContain("project-alpha", Case.Insensitive);
        serializedAudit.ShouldNotContain("evidence content", Case.Insensitive);
    }

    private static async Task<BackendLaneOutcome> RunBackendLaneAsync(
        ClaimsPrincipal principal,
        object command,
        ChatBotSurfaceOrigin origin,
        CancellationToken cancellationToken)
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        FixedConformanceClock clock = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(clock);
        InMemoryOperationStatusStore operationStatusStore = new();
        CommandGateway gateway = new(
            new ClaimsAuthenticationStage(),
            new ClaimsTenantBindingStage(),
            new ParticipantAuthorizationStage(),
            new PassThroughRiskClassifier(),
            new PassThroughApprovalGate(),
            idempotencyStore,
            auditWriter,
            new NoOpReplayIntentQueue(),
            new NoOpOperatorAlertSink(),
            operationStatusStore,
            clock,
            new CommandSubmissionLifecycleTransitionGuard(),
            dispatcher,
            new ChatBotProblemDetailsFactory(new CoarseUserFacingRedactionStage(), new InMemoryUserFacingMessageTelemetry()),
            new ChatBotSpineCommandAllowlist());

        ChatBotCommandSubmission submission = new(
            principal,
            new CommandSubmissionRequest
            {
                CommandId = CrossTenantIsolationHarness.CommandId,
                CommandType = command.GetType().Name,
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            CrossTenantIsolationHarness.CorrelationId,
            CrossTenantIsolationHarness.TaskId,
            origin);

        ChatBotGatewayResult result = await gateway.SubmitAsync(submission, cancellationToken).ConfigureAwait(false);
        string serializedProblem = result.Problem is null
            ? "{}"
            : JsonSerializer.Serialize(result.Problem, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return new BackendLaneOutcome(
            result.IsAccepted,
            dispatcher.DispatchCount,
            idempotencyStore.RecordCount,
            auditWriter.AuthorizationFailures,
            auditWriter.Envelopes,
            serializedProblem);
    }

    private static ClaimsPrincipal Principal(string actorType, string role)
        => new(new ClaimsIdentity(
            [
                new Claim("sub", CrossTenantIsolationHarness.BoundActorId),
                new Claim("eventstore:tenant", CrossTenantLeakageCorpus.BoundTenant),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
            ],
            "test"));

    private static object Command(string commandType)
        => commandType switch
        {
            nameof(AssignTenantAdminRole) => AdminAssignment(),
            nameof(ExecuteAdminQueueOperation) => AdminQueueOperationCommand(),
            _ => throw new ArgumentOutOfRangeException(nameof(commandType), commandType, "Unsupported admin command type."),
        };

    private static AssignTenantAdminRole AdminAssignment()
        => new(
            "assignment-001",
            "actor-beta",
            AdminRole.OperationsAdmin,
            "security-owner-request",
            "policy-snapshot-admin-v1",
            3);

    private static ExecuteAdminQueueOperation AdminQueueOperationCommand()
        => new(
            "operation-001",
            AdminQueueOperation.Retry,
            AdminScope.Operate,
            "queue:failure",
            ["item:001"],
            1,
            "dependency-degraded",
            "policy-snapshot-admin-v1",
            7,
            "metadata_only");

    private sealed record BackendLaneOutcome(
        bool IsAccepted,
        int DispatchCount,
        int IdempotencyRecordCount,
        IReadOnlyList<ChatBotAuthorizationFailureAuditFact> AuthorizationFailures,
        IReadOnlyList<AuditEnvelope> AuditEnvelopes,
        string SerializedProblem);
}
