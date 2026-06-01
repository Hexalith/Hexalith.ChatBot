using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.UI.State.ProjectConversation;

namespace Hexalith.ChatBot.UI.Services;

/// <summary>
/// UI-owned S1 project conversation read service. Reads only through <see cref="IChatBotClient"/>.
/// </summary>
public sealed class ProjectConversationService(IChatBotClient client)
{
    private readonly IChatBotClient _client = client ?? throw new ArgumentNullException(nameof(client));

    public async Task<ProjectConversationModel> GetProjectConversationAsync(
        string projectId,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        ProjectConversationResponse response = await _client
            .GetProjectConversationAsync(projectId, cursor, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new ProjectConversationModel(
            response.ProjectId,
            string.IsNullOrWhiteSpace(response.ProjectDisplayName) ? response.ProjectId : response.ProjectDisplayName,
            response.TenantContext,
            response.Status.ToString(),
            response.ConversationState.ToString(),
            response.Items.Select(MapItem).ToArray(),
            response.Page.NextCursor,
            response.Page.HasMore,
            response.Page.PageSize,
            response.SourceProvenance.ToString(),
            response.RedactionState.ToString(),
            response.RetentionClass.ToString(),
            response.SchemaVersion.ToString(),
            response.CorrelationId,
            string.IsNullOrWhiteSpace(response.SafeNextAction) ? "none" : response.SafeNextAction);
    }

    private static ProjectConversationItemModel MapItem(ProjectConversationItem item)
        => new(
            item.ItemId,
            item.Kind.ToString(),
            item.ActorKind.ToString(),
            item.ActorLabel,
            item.OccurredAt,
            item.LifecycleState.ToString(),
            item.ThresholdBand.ToString(),
            item.ConfidenceScore,
            item.AssociationId,
            item.SourceMailboxId,
            item.SourceProviderMessageId,
            item.InternetMessageId,
            item.SourceConversationId,
            item.SourceThreadId,
            item.SourceReceivedAtUtc,
            item.SourceSentAtUtc,
            item.SourceCreatedAtUtc,
            item.SourceTimezone,
            item.SourceProvenanceDisplayToken,
            item.SourceProvenance.ToString(),
            item.RedactionState.ToString(),
            item.RetentionClass.ToString(),
            item.SchemaVersion.ToString(),
            item.SourceVersion,
            item.CorrelationId,
            item.ProjectId,
            item.ProjectDisplayName,
            item.DecisionLabel,
            item.SafeNextAction);
}
