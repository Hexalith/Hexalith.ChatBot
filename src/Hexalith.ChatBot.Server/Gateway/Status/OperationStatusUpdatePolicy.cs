using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Server.Gateway.Status;

/// <summary>Shared policy evaluated inside each atomic update, including every SDK ETag retry.</summary>
internal static class OperationStatusUpdatePolicy
{
    /// <summary>Preserves immutable identity and monotonic canonical state; rejects conflicting equal revisions.</summary>
    public static OperationStatusRecord Apply(OperationStatusRecord? current, OperationStatusRecord incoming)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        incoming = Snapshot(incoming);
        ValidateCanonical(incoming);
        if (current is null)
        {
            return incoming;
        }
        if (current.TenantId != incoming.TenantId || current.OperationId != incoming.OperationId ||
            current.CommandId != incoming.CommandId || current.CorrelationId != incoming.CorrelationId ||
            current.AcceptedAt != incoming.AcceptedAt || current.OperationClass != incoming.OperationClass ||
            current.Origin != incoming.Origin || current.CreatedAt != incoming.CreatedAt ||
            current.EquivalenceVersion != incoming.EquivalenceVersion || current.PredecessorOperationId != incoming.PredecessorOperationId ||
            !(current.ResourceIds ?? []).SequenceEqual(incoming.ResourceIds ?? []))
        {
            throw new OperationStatusUpdateConflictException();
        }
        // Legacy workflow receipts did not carry canonical revisions and may restart an old workflow. Keep that
        // compatibility explicit: never upgrade them implicitly or let them overwrite a canonical fold.
        if (current.CanonicalRevision is null && incoming.CanonicalRevision is null)
        {
            return incoming.LastUpdatedAt < current.LastUpdatedAt ? current : incoming;
        }
        if (current.CanonicalRevision is null || incoming.CanonicalRevision is null ||
            current.ExpectedRevision != incoming.ExpectedRevision || current.MaxAttempts != incoming.MaxAttempts ||
            current.OriginalOperationId != incoming.OriginalOperationId ||
            JsonSerializer.Serialize(current.PriorOutcome) != JsonSerializer.Serialize(incoming.PriorOutcome))
        {
            throw new OperationStatusUpdateConflictException();
        }
        if (incoming.CanonicalRevision < current.CanonicalRevision)
        {
            return current;
        }
        if (incoming.CanonicalRevision == current.CanonicalRevision)
        {
            if (JsonSerializer.Serialize(current) != JsonSerializer.Serialize(incoming))
            {
                throw new OperationStatusUpdateConflictException();
            }
            return current;
        }
        bool terminal = current.ExecutionState is CommandExecutionState.Committed or CommandExecutionState.Rejected or CommandExecutionState.Failed;
        bool projectionAdvances = current.ExecutionState == CommandExecutionState.Committed &&
            current.ProjectionState != CommandProjectionState.Projected &&
            (incoming.ProjectionState == CommandProjectionState.Projected ||
                (current.ProjectionState == CommandProjectionState.ProjectionPending && incoming.ProjectionState == CommandProjectionState.ProjectionLagging));
        bool executionAdvances = !terminal && current.ExecutionState != incoming.ExecutionState &&
            (incoming.ExecutionState != CommandExecutionState.Received) &&
            (current.ExecutionState != CommandExecutionState.Received || incoming.ExecutionState == CommandExecutionState.Admitted || incoming.CanonicalRevision - current.CanonicalRevision >= 2);
        if ((!projectionAdvances && !executionAdvances) ||
            incoming.LastUpdatedAt < current.LastUpdatedAt || incoming.RetryCount < current.RetryCount ||
            incoming.DuplicateAttemptCount < current.DuplicateAttemptCount ||
            (current.CommittedRevision is not null && current.CommittedRevision != incoming.CommittedRevision) ||
            (current.ProjectionState == CommandProjectionState.Projected && incoming.ProjectionState != CommandProjectionState.Projected) ||
            (current.ExecutionState == CommandExecutionState.Admitted && incoming.ExecutionState == CommandExecutionState.Received) ||
            (terminal && (current.ExecutionState != incoming.ExecutionState || current.TerminalReason != incoming.TerminalReason ||
                current.TerminalReasonCode != incoming.TerminalReasonCode || current.FailureReasonCode != incoming.FailureReasonCode ||
                current.ReasonCode != incoming.ReasonCode || current.LifecycleState != incoming.LifecycleState ||
                current.RetryCount != incoming.RetryCount || current.NextRetryAt != incoming.NextRetryAt ||
                current.OwnerRole != incoming.OwnerRole || current.DuplicateSafetyNote != incoming.DuplicateSafetyNote ||
                !current.SafeNextActions.SequenceEqual(incoming.SafeNextActions))) ||
            (current.PartialOutputCodes ?? []).Except(incoming.PartialOutputCodes ?? []).Any())
        {
            throw new OperationStatusUpdateConflictException();
        }
        return incoming;
    }

    /// <summary>Prevents mutable generated outcomes and arrays from changing stored identity outside the policy.</summary>
    public static OperationStatusRecord Snapshot(OperationStatusRecord record)
        => record with
        {
            ResourceIds = record.ResourceIds?.ToArray(),
            PartialOutputCodes = record.PartialOutputCodes?.ToArray(),
            SafeNextActions = record.SafeNextActions.ToArray(),
            PriorOutcome = record.PriorOutcome is not { } previous ? null : new()
            {
                CommandId = previous.CommandId, OperationId = previous.OperationId, TaskId = previous.TaskId,
                CorrelationId = previous.CorrelationId, AcceptedAt = previous.AcceptedAt, LifecycleState = previous.LifecycleState,
                ReasonCode = previous.ReasonCode, RetryEligible = previous.RetryEligible,
                PriorOutcome = previous.PriorOutcome is not { } prior ? null : new()
                {
                    CommandId = prior.CommandId, OperationId = prior.OperationId, TaskId = prior.TaskId,
                    CorrelationId = prior.CorrelationId, AcceptedAt = prior.AcceptedAt, LifecycleState = prior.LifecycleState,
                    ReasonCode = prior.ReasonCode, RetryEligible = prior.RetryEligible,
                },
            },
        };

    private static void ValidateCanonical(OperationStatusRecord record)
    {
        if (record.CanonicalRevision is null)
        {
            if (record.ExecutionState is not null || record.ProjectionState is not null || record.ExpectedRevision is not null ||
                record.CommittedRevision is not null || record.LastTransition is not null)
            {
                throw new OperationStatusUpdateConflictException();
            }
            return;
        }
        bool committed = record.ExecutionState == CommandExecutionState.Committed;
        string expectedCompletion = record.ExecutionState is CommandExecutionState.Rejected or CommandExecutionState.Failed ? OperationStatusRecord.Failed
            : committed && record.ProjectionState == CommandProjectionState.Projected ? OperationStatusRecord.Completed : OperationStatusRecord.AcceptedProjectionPending;
        Hexalith.ChatBot.Client.Generated.LifecycleState expectedLifecycle = record.ExecutionState switch
        {
            CommandExecutionState.Received => Hexalith.ChatBot.Client.Generated.LifecycleState.Received,
            CommandExecutionState.Rejected => Hexalith.ChatBot.Client.Generated.LifecycleState.Rejected,
            CommandExecutionState.Failed => Hexalith.ChatBot.Client.Generated.LifecycleState.Failed,
            _ => Hexalith.ChatBot.Client.Generated.LifecycleState.Proposed,
        };
        bool validCommit = committed
            ? record.ExpectedRevision is >= 0 and < long.MaxValue && record.CommittedRevision == record.ExpectedRevision + 1
            : record.CommittedRevision is null;
        if (record.CanonicalRevision < 0 || record.ExpectedRevision is null or < 0 ||
            !ChatBotOperationId.TryParse(record.OperationId, out _) || record.OperationId != record.CommandId ||
            record.ExecutionState is not { } execution || !Enum.IsDefined(execution) ||
            record.ProjectionState is not { } projection || !Enum.IsDefined(projection) ||
            record.Origin is null || ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigins.FromWireValueOrDefault(record.Origin)) != record.Origin ||
            record.ResourceIds is null || record.ResourceIds.Any(resource => !Hexalith.ChatBot.Server.Operations.CommandOperationMetadataValidation.IsValidResourceId(resource)) ||
            record.CreatedAt is null || record.CreatedAt.Value.Offset != TimeSpan.Zero || record.EquivalenceVersion != "v1" ||
            record.LastTransition is not { } transition || transition.Revision != record.CanonicalRevision ||
            transition.State != record.ExecutionState || transition.ProjectionState != record.ProjectionState ||
            transition.CommittedRevision != record.CommittedRevision || transition.OccurredAt.Offset != TimeSpan.Zero ||
            transition.OccurredAt != record.LastUpdatedAt || !Hexalith.ChatBot.Server.Operations.CommandOperationMetadataValidation.IsValidActorId(transition.ActorId) ||
            !ChatBotIdentity.IsValidUlid(transition.CorrelationId) || !ChatBotMessageCatalog.IsKnown(transition.ReasonCode) ||
            transition.ReasonCode == ChatBotMessageCodes.GenericDenial ||
            !validCommit || record.LifecycleState != expectedLifecycle ||
            (!committed && record.ProjectionState != CommandProjectionState.ProjectionPending) ||
            record.CompletionStatus != expectedCompletion ||
            (record.CompletionStatus == OperationStatusRecord.Completed && (!committed || record.ProjectionState != CommandProjectionState.Projected)))
        {
            throw new OperationStatusUpdateConflictException();
        }
    }
}
