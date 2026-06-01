using Hexalith.ChatBot.Contracts.Queries;

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
