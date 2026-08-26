using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.Conversations;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class TaskIntentProjectionHandler(IProjectConversationProjectionStore conversationStore)
{
    private readonly IProjectConversationProjectionStore _conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));

    public enum ProjectionOutcome
    {
        Applied,
        Ignored,
    }

    public async Task<ProjectionOutcome> HandleAsync(PublishedTaskIntentEvent published, CancellationToken cancellationToken = default)
    {
        if (published.UserMessage is not null)
        {
            await _conversationStore
                .UpsertProjectConversationMessageAsync(
                    published.UserMessage with { SourceVersion = published.SequenceNumber },
                    cancellationToken)
                .ConfigureAwait(false);
            return ProjectionOutcome.Applied;
        }

        if (published.AiResponseCancellation is not null)
        {
            AiResponseGenerationCancellationRequested cancellation = published.AiResponseCancellation;
            await _conversationStore.UpsertAiOutcomeEventAsync(new AiOutcomeEventView(
                cancellation.TenantId,
                cancellation.ProjectId,
                Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.OutcomeRecorded,
                Hexalith.ChatBot.Contracts.Enums.AiOutcomeStatus.Executing,
                cancellation.RequestedAtUtc,
                published.SequenceNumber,
                cancellation.CorrelationId,
                cancellation.ActorId,
                "human",
                RequestId: cancellation.ResponseId,
                SourceConversationItemId: cancellation.ConversationId,
                OperationId: cancellation.GenerationId,
                SafeNextAction: "wait-for-executor",
                AiResponseSequence: published.SequenceNumber,
                AiResponseProgressState: "cancelling",
                AiResponseTerminalReason: "none",
                AiResponseVisibilityState: "metadata_only",
                AiResponseIsTerminal: false,
                RedactionState: cancellation.RedactionState), cancellationToken).ConfigureAwait(false);
            return ProjectionOutcome.Applied;
        }

        if (published.AiResponseCancellationConfirmed is not null)
        {
            AiResponseGenerationCancellationConfirmed confirmed = published.AiResponseCancellationConfirmed;
            await _conversationStore.UpsertAiOutcomeEventAsync(new AiOutcomeEventView(
                confirmed.TenantId,
                confirmed.ProjectId,
                Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.OutcomeRecorded,
                Hexalith.ChatBot.Contracts.Enums.AiOutcomeStatus.Succeeded,
                confirmed.ConfirmedAtUtc,
                published.SequenceNumber,
                confirmed.CorrelationId,
                "ai-action-executor",
                "system",
                RequestId: confirmed.ResponseId,
                SourceConversationItemId: confirmed.ConversationId,
                OperationId: confirmed.GenerationId,
                SafeNextAction: confirmed.SafeNextAction,
                AiResponseSequence: published.SequenceNumber,
                AiResponseProgressState: "stopped",
                AiResponseTerminalReason: "user-stopped",
                AiResponseVisibilityState: "metadata_only",
                AiResponseIsTerminal: true,
                RedactionState: confirmed.RedactionState), cancellationToken).ConfigureAwait(false);
            return ProjectionOutcome.Applied;
        }

        if (published.AiResponseCancellationFailed is not null)
        {
            AiResponseGenerationCancellationFailed failed = published.AiResponseCancellationFailed;
            await _conversationStore.UpsertAiOutcomeEventAsync(new AiOutcomeEventView(
                failed.TenantId,
                failed.ProjectId,
                Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.ExecutionFailed,
                Hexalith.ChatBot.Contracts.Enums.AiOutcomeStatus.Failed,
                failed.FailedAtUtc,
                published.SequenceNumber,
                failed.CorrelationId,
                "ai-action-executor",
                "system",
                RequestId: failed.ResponseId,
                SourceConversationItemId: failed.ConversationId,
                OperationId: failed.GenerationId,
                FailureCode: failed.FailureReasonCode,
                SafeNextAction: failed.SafeNextAction,
                AiResponseSequence: published.SequenceNumber,
                AiResponseProgressState: "unavailable",
                AiResponseTerminalReason: "unavailable",
                AiResponseVisibilityState: "metadata_only",
                AiResponseIsTerminal: true,
                RedactionState: failed.RedactionState), cancellationToken).ConfigureAwait(false);
            return ProjectionOutcome.Applied;
        }

        TaskIntentRecord? record = TaskIntentProjectionTranslator.TryCreateRecord(published);
        if (record is null)
        {
            return ProjectionOutcome.Ignored;
        }

        await _conversationStore.UpsertTaskIntentAsync(record, cancellationToken).ConfigureAwait(false);
        AiOutcomeEventView? proposal = TaskIntentProjectionTranslator.TryCreateAiOutcome(published);
        if (proposal is not null)
        {
            await _conversationStore.UpsertAiOutcomeEventAsync(proposal, cancellationToken).ConfigureAwait(false);
        }

        if (published.Proposal is not null)
        {
            await _conversationStore.UpsertAiActionProposalAsync(record.TenantId, published.Proposal, cancellationToken).ConfigureAwait(false);
        }

        return ProjectionOutcome.Applied;
    }
}
