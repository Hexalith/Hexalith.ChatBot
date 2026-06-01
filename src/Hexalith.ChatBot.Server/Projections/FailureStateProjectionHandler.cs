namespace Hexalith.ChatBot.Server.Projections;

internal sealed class FailureStateProjectionHandler(IProjectConversationProjectionStore conversationStore)
{
    private readonly IProjectConversationProjectionStore _conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));

    public enum ProjectionOutcome
    {
        Applied,
        Ignored,
    }

    public async Task<ProjectionOutcome> HandleAsync(PublishedFailureStateEvent published, CancellationToken cancellationToken = default)
    {
        FailureStateEventView? view = FailureStateProjectionTranslator.TryCreateView(published);
        if (view is null)
        {
            return ProjectionOutcome.Ignored;
        }

        await _conversationStore.UpsertFailureStateEventAsync(view, cancellationToken).ConfigureAwait(false);
        return ProjectionOutcome.Applied;
    }
}
