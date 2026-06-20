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
            await _conversationStore.UpsertProjectConversationMessageAsync(published.UserMessage, cancellationToken).ConfigureAwait(false);
            return ProjectionOutcome.Applied;
        }

        if (published.AiResponseCancellation is not null)
        {
            AiResponseGenerationCancellationRequested cancellation = published.AiResponseCancellation;
            await _conversationStore.UpsertAiOutcomeEventAsync(new AiOutcomeEventView(
                cancellation.TenantId,
                cancellation.ProjectId,
                Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.OutcomeRecorded,
                Hexalith.ChatBot.Contracts.Enums.AiOutcomeStatus.Succeeded,
                cancellation.RequestedAtUtc,
                cancellation.SourceVersion,
                cancellation.CorrelationId,
                cancellation.ActorId,
                "human",
                RequestId: cancellation.ResponseId,
                SourceConversationItemId: cancellation.ConversationId,
                OperationId: cancellation.GenerationId,
                SafeNextAction: cancellation.SafeNextAction,
                AiResponseSequence: cancellation.SourceVersion,
                AiResponseProgressState: "stopped",
                AiResponseTerminalReason: "user-stopped",
                AiResponseVisibilityState: "metadata_only",
                AiResponseIsTerminal: true,
                RedactionState: cancellation.RedactionState), cancellationToken).ConfigureAwait(false);
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
