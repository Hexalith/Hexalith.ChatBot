namespace Hexalith.ChatBot.Server.Projections;

internal sealed class AiOutcomeProjectionHandler(IProjectConversationProjectionStore conversationStore)
{
    private readonly IProjectConversationProjectionStore _conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));

    public enum ProjectionOutcome
    {
        Applied,
        Ignored,
    }

    public async Task<ProjectionOutcome> HandleAsync(PublishedAiOutcomeEvent published, CancellationToken cancellationToken = default)
    {
        AiOutcomeEventView? view = AiOutcomeProjectionTranslator.TryCreateView(published);
        if (view is null)
        {
            return ProjectionOutcome.Ignored;
        }

        await _conversationStore.UpsertAiOutcomeEventAsync(view, cancellationToken).ConfigureAwait(false);
        return ProjectionOutcome.Applied;
    }
}
