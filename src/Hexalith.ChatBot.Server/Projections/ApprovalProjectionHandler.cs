namespace Hexalith.ChatBot.Server.Projections;

internal sealed class ApprovalProjectionHandler(IProjectConversationProjectionStore conversationStore)
{
    private readonly IProjectConversationProjectionStore _conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));

    public enum ProjectionOutcome
    {
        Applied,
        Ignored,
    }

    public async Task<ProjectionOutcome> HandleAsync(PublishedApprovalEvent published, CancellationToken cancellationToken = default)
    {
        ApprovalEventView? view = ApprovalProjectionTranslator.TryCreateView(published);
        if (view is null)
        {
            return ProjectionOutcome.Ignored;
        }

        await _conversationStore.UpsertApprovalEventAsync(view, cancellationToken).ConfigureAwait(false);
        return ProjectionOutcome.Applied;
    }
}
