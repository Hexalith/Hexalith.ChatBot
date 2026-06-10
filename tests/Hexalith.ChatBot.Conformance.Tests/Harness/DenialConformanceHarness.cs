using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;

namespace Hexalith.ChatBot.Conformance.Tests.Harness;

internal sealed record DenialOutcome(
    string ArmName,
    string SurfaceOrigin,
    string ReasonCode,
    string Category,
    string Code,
    string ClientAction,
    string DetailsVisibility,
    string CorrelationId,
    string? TaskId,
    int Status,
    int DispatchCount,
    int CoarseIdempotencyRecordCount);

internal static class DenialConformanceHarness
{
    private const string ActorId = "actor-alpha";
    private const string Tenant = "tenant-alpha";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TaskId = "01ARZ3NDEKTSV4RRFFQ69G5FAX";
    private const string CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";

    public static async Task<DenialOutcome> RunAuthenticationDeniedAsync(ISurfaceArm arm, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arm);

        return await RunAsync(
            arm,
            new DenyingAuthenticationStage(),
            new ClaimsTenantBindingStage(),
            new PassThroughAuthorizationStage(),
            new RecordGovernedNote("note-auth-denied"),
            nameof(RecordGovernedNote),
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task<DenialOutcome> RunTenantMismatchAsync(ISurfaceArm arm, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arm);

        return await RunAsync(
            arm,
            new ClaimsAuthenticationStage(),
            new ClaimsTenantBindingStage(),
            new PassThroughAuthorizationStage(),
            new CrossTenantProbeCommand(
                "tenant-beta",
                "restricted project",
                "candidate evidence",
                "file metadata",
                "cursor-token",
                "/restricted/path",
                "provider-payload",
                "stack trace"),
            nameof(RecordGovernedNote),
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task<DenialOutcome> RunAuthorizationDeniedAsync(
        ISurfaceArm arm,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arm);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);

        return await RunAsync(
            arm,
            new ClaimsAuthenticationStage(),
            new ClaimsTenantBindingStage(),
            new DenyingAuthorizationStage(reasonCode),
            new RecordGovernedNote($"note-{reasonCode}"),
            nameof(RecordGovernedNote),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<DenialOutcome> RunAsync(
        ISurfaceArm arm,
        IAuthenticationStage authentication,
        ITenantBindingStage tenantBinding,
        IAuthorizationStage authorization,
        object command,
        string commandType,
        CancellationToken cancellationToken)
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        FixedConformanceClock clock = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(clock);
        InMemoryOperationStatusStore statusStore = new();
        CommandGateway gateway = new(
            authentication,
            tenantBinding,
            authorization,
            new PassThroughRiskClassifier(),
            new PassThroughApprovalGate(),
            idempotencyStore,
            auditWriter,
            new NoOpReplayIntentQueue(),
            new NoOpOperatorAlertSink(),
            statusStore,
            clock,
            new CommandSubmissionLifecycleTransitionGuard(),
            dispatcher,
            new ChatBotProblemDetailsFactory(new CoarseUserFacingRedactionStage(), new InMemoryUserFacingMessageTelemetry()),
            new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway
            .SubmitAsync(Submission(command, commandType, arm.Origin), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsAccepted || result.Problem is null)
        {
            throw new InvalidOperationException($"Arm '{arm.Name}' denial intent was unexpectedly accepted.");
        }

        ChatBotAuthorizationFailureAuditFact failure = auditWriter.AuthorizationFailures.Single();
        ProblemDetails problem = result.Problem;

        return new DenialOutcome(
            arm.Name,
            failure.SurfaceOrigin,
            failure.ReasonCode,
            problem.Category.ToString(),
            problem.Code,
            problem.ClientAction.ToString(),
            problem.Details.Visibility.ToString(),
            problem.CorrelationId,
            problem.TaskId,
            problem.Status,
            dispatcher.DispatchCount,
            idempotencyStore.RecordCount);
    }

    private static ChatBotCommandSubmission Submission(object command, string commandType, ChatBotSurfaceOrigin origin)
        => new(
            Principal(),
            new CommandSubmissionRequest
            {
                CommandId = CommandId,
                CommandType = commandType,
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            CorrelationId,
            TaskId,
            origin);

    private static ClaimsPrincipal Principal()
        => new(
            new ClaimsIdentity(
                [new Claim("sub", ActorId), new Claim("eventstore:tenant", Tenant)],
                "test"));

    private sealed class DenyingAuthenticationStage : IAuthenticationStage
    {
        public ValueTask<ChatBotAuthenticationResult> AuthenticateAsync(
            ChatBotCommandSubmission submission,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(ChatBotAuthenticationResult.Denied(ChatBotAuthorizationReasonCodes.AuthenticationDenied));
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
