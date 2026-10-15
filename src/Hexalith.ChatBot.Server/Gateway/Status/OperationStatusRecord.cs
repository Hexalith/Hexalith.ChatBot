using System.Text.Json.Serialization;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway.Idempotency;

namespace Hexalith.ChatBot.Server.Gateway.Status;

// The generated enums (LifecycleState, and ChatBotMessageCode inside the prior outcome) are persisted in DAPR state as
// their stable [EnumMember] wire values rather than generation-dependent ordinals. Integer tokens are still accepted but
// interpreted with the current generated numbering, so they decode correctly only for records written by the same enum
// generation; records persisted before a renumbering misdecode (accepted while the product is pre-release).
internal sealed record OperationStatusRecord(
    string TenantId,
    string OperationId,
    string CommandId,
    string CorrelationId,
    [property: JsonConverter(typeof(EnumMemberWireValueJsonConverter<LifecycleState>))] LifecycleState LifecycleState,
    int RetryCount,
    string CompletionStatus,
    string AuditStatus,
    string[] SafeNextActions,
    string? TerminalReason,
    DateTimeOffset AcceptedAt,
    DateTimeOffset LastUpdatedAt,
    string OperationClass = "command-execution",
    int MaxAttempts = 1,
    DateTimeOffset? NextRetryAt = null,
    string? DuplicateSafetyNote = null,
    string? OwnerRole = null,
    string? FailureReasonCode = null,
    string? TerminalReasonCode = null,
    string[]? PartialOutputCodes = null,
    string? OriginalOperationId = null,
    int DuplicateAttemptCount = 0,
    string? WorkflowInstanceId = null,
    string? WorkflowStatus = null,
    int WorkflowRetryCount = 0,
    string? WorkflowLastFailureCode = null,
    string ReasonCode = ChatBotMessageCodes.CommandAccepted,
    [property: JsonConverter(typeof(EnumMemberWireValueObjectJsonConverter<CommandSubmissionResponse>))] CommandSubmissionResponse? PriorOutcome = null,
    Hexalith.ChatBot.Contracts.Enums.CommandExecutionState? ExecutionState = null,
    Hexalith.ChatBot.Contracts.Enums.CommandProjectionState? ProjectionState = null,
    long? CanonicalRevision = null,
    long? ExpectedRevision = null,
    long? CommittedRevision = null,
    string? Origin = null,
    string[]? ResourceIds = null,
    DateTimeOffset? CreatedAt = null,
    string? EquivalenceVersion = null,
    string? PredecessorOperationId = null,
    Hexalith.ChatBot.Server.Operations.CommandOperationTransition? LastTransition = null)
{
    public const string AcceptedProjectionPending = "accepted-projection-pending";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string AuditCommitted = "committed";
    public const string AuditReconciling = "reconciling";

    /// <summary>
    /// Resolves a stored receipt without rewriting its historical identity. New gateway receipts supply CommandId
    /// explicitly; the task fallback applies only to historical receipts which lacked OperationId.
    /// </summary>
    public static string OperationIdFor(CommandSubmissionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return string.IsNullOrWhiteSpace(response.OperationId) ? response.TaskId ?? response.CommandId : response.OperationId;
    }

    /// <summary>Maps an authoritative folded canonical outcome. This pure mapping does not perform runtime writes.</summary>
    public static OperationStatusRecord FromCanonical(Hexalith.ChatBot.Server.Operations.CommandOperationState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Hexalith.ChatBot.Server.Operations.CommandOperationTransition fact = state.Transitions[^1];
        Hexalith.ChatBot.Server.Operations.CommandOperationTransition outcome = state.Transitions.FirstOrDefault(
            transition => transition.State is Hexalith.ChatBot.Contracts.Enums.CommandExecutionState.Committed or
                Hexalith.ChatBot.Contracts.Enums.CommandExecutionState.Rejected or Hexalith.ChatBot.Contracts.Enums.CommandExecutionState.Failed) ?? fact;
        bool failed = state.ExecutionState is Hexalith.ChatBot.Contracts.Enums.CommandExecutionState.Failed or Hexalith.ChatBot.Contracts.Enums.CommandExecutionState.Rejected;
        string completion = failed ? Failed : state.ProjectionState == Hexalith.ChatBot.Contracts.Enums.CommandProjectionState.Projected ? Completed : AcceptedProjectionPending;
        LifecycleState legacy = state.ExecutionState switch
        {
            Hexalith.ChatBot.Contracts.Enums.CommandExecutionState.Received => LifecycleState.Received,
            Hexalith.ChatBot.Contracts.Enums.CommandExecutionState.Rejected => LifecycleState.Rejected,
            Hexalith.ChatBot.Contracts.Enums.CommandExecutionState.Failed => LifecycleState.Failed,
            _ => LifecycleState.Proposed,
        };
        return new(state.Identity.TenantId, state.Identity.OperationId, state.Identity.OperationId, state.Identity.CorrelationId,
            legacy, state.Attempt - 1, completion, state.CommittedRevision is null ? AuditReconciling : AuditCommitted,
            [ChatBotMessageCatalog.Resolve(outcome.ReasonCode).NextAction], state.IsTerminal ? outcome.ReasonCode : null,
            state.Identity.CreatedAt, fact.OccurredAt, state.Identity.OperationClass, state.MaxAttempts,
            FailureReasonCode: failed ? outcome.ReasonCode : null, TerminalReasonCode: state.IsTerminal ? outcome.ReasonCode : null,
            OriginalOperationId: state.Identity.PredecessorOperationId ?? state.Identity.OperationId,
            ReasonCode: outcome.ReasonCode, ExecutionState: state.ExecutionState, ProjectionState: state.ProjectionState,
            CanonicalRevision: state.Revision, ExpectedRevision: state.Concurrency.ExpectedRevision, CommittedRevision: state.CommittedRevision,
            Origin: Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigins.ToWireValue(state.Identity.Origin),
            ResourceIds: state.Identity.ResourceIds.ToArray(), CreatedAt: state.Identity.CreatedAt,
            EquivalenceVersion: state.Identity.EquivalenceVersion, PredecessorOperationId: state.Identity.PredecessorOperationId, LastTransition: fact,
            PriorOutcome: state.PriorOutcome is not { } previous ? null : new CommandSubmissionResponse
            {
                CommandId = previous.OperationId, OperationId = previous.OperationId, CorrelationId = previous.CorrelationId,
                AcceptedAt = previous.CreatedAt,
                LifecycleState = previous.State == Hexalith.ChatBot.Contracts.Enums.CommandExecutionState.Rejected ? LifecycleState.Rejected : LifecycleState.Failed,
                ReasonCode = Enum.GetValues<ChatBotMessageCode>().First(code => typeof(ChatBotMessageCode).GetField(code.ToString())!
                    .GetCustomAttributes(typeof(System.Runtime.Serialization.EnumMemberAttribute), false)
                    .OfType<System.Runtime.Serialization.EnumMemberAttribute>().Any(attribute => attribute.Value == previous.ReasonCode)),
                RetryEligible = false,
            });
    }

    public static OperationStatusRecord Accepted(
        string tenantId,
        CommandSubmissionResponse response,
        bool auditReconciliationRequired,
        DateTimeOffset lastUpdatedAt,
        string operationClass = "command-execution")
    {
        ArgumentNullException.ThrowIfNull(response);

        int retryCount = string.Equals(operationClass, CoarseIdempotencyOperationClass.Retry.Code, StringComparison.Ordinal) ? 1 : 0;
        int maxAttempts = string.Equals(operationClass, CoarseIdempotencyOperationClass.Retry.Code, StringComparison.Ordinal) ? 5 : 1;

        return new OperationStatusRecord(
            tenantId,
            OperationIdFor(response),
            response.CommandId,
            response.CorrelationId,
            response.LifecycleState,
            retryCount,
            AcceptedProjectionPending,
            auditReconciliationRequired ? AuditReconciling : AuditCommitted,
            [ChatBotMessageNextActions.None],
            null,
            response.AcceptedAt.ToUniversalTime(),
            lastUpdatedAt.ToUniversalTime(),
            operationClass,
            maxAttempts,
            NextRetryAt: null,
            DuplicateSafetyNote: null,
            OwnerRole: null,
            FailureReasonCode: null,
            TerminalReasonCode: null,
            PartialOutputCodes: [],
            OriginalOperationId: OperationIdFor(response),
            DuplicateAttemptCount: 0,
            ReasonCode: ChatBotMessageCodes.CommandAccepted);
    }
}
