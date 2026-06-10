using System.Text.Json;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class AiOutcomeProjectionHandler(IProjectConversationProjectionStore conversationStore)
{
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web);
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

    public async Task<ProjectionOutcome> HandleAsync(PublishedAiActionExecutionEvent published, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PublishedAiOutcomeEvent> outcomes = ApprovedAiActionOutcomeProjectionTranslator.TryCreatePublishedEvents(published);
        if (outcomes.Count == 0)
        {
            // Story 4.4 (AC5/AC6): the low-risk AI assistance execution events ride the same published envelope and
            // topic as the approved-action events, so try the low-risk translator when the approved one yields nothing.
            outcomes = LowRiskAiOutcomeProjectionTranslator.TryCreatePublishedEvents(published);
        }

        if (outcomes.Count == 0)
        {
            return ProjectionOutcome.Ignored;
        }

        foreach (PublishedAiOutcomeEvent outcome in outcomes)
        {
            _ = await HandleAsync(outcome, cancellationToken).ConfigureAwait(false);
        }

        return ProjectionOutcome.Applied;
    }

    public async Task<ProjectionOutcome> HandleAsync(JsonElement published, CancellationToken cancellationToken = default)
    {
        if (published.TryGetProperty("eventTypeName", out _))
        {
            PublishedAiActionExecutionEvent? source = published.Deserialize<PublishedAiActionExecutionEvent>(ReadOptions);
            return source is null
                ? ProjectionOutcome.Ignored
                : await HandleAsync(source, cancellationToken).ConfigureAwait(false);
        }

        PublishedAiOutcomeEvent? outcome = published.Deserialize<PublishedAiOutcomeEvent>(ReadOptions);
        return outcome is null
            ? ProjectionOutcome.Ignored
            : await HandleAsync(outcome, cancellationToken).ConfigureAwait(false);
    }
}
