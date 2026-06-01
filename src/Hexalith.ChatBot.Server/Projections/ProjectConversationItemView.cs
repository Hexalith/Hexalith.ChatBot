using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record ProjectConversationItemView(
    string TenantId,
    string ProjectId,
    string? ProjectDisplayName,
    string ItemId,
    ProjectConversationItemKind Kind,
    ProjectConversationActorKind ActorKind,
    string ActorLabel,
    DateTimeOffset OccurredAt,
    LifecycleState LifecycleState,
    AssociationThresholdBand ThresholdBand,
    double ConfidenceScore,
    string AssociationId,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    string SourceProvenance,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion,
    long SourceVersion,
    string CorrelationId,
    string? DecisionLabel = null,
    string? SafeNextAction = null)
{
    public const string CurrentSchemaVersion = "chatbot.project-conversation-item.v1";

    public static string KeyFor(string tenantId, string projectId, string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        return $"{tenantId}:project-conversation:{projectId}:{itemId}";
    }

    public static bool ShouldReplace(ProjectConversationItemView existing, ProjectConversationItemView incoming)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(incoming);
        return incoming.SourceVersion >= existing.SourceVersion;
    }

    public static ProjectConversationItemView? FromAssociation(AssociationCandidateView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (string.IsNullOrWhiteSpace(view.ProjectId))
        {
            return null;
        }

        ProjectConversationItemKind kind = view.DecisionKind is null && view.CorrectionKind is null
            ? ProjectConversationItemKind.EmailDerived
            : ProjectConversationItemKind.SystemDecision;
        ProjectConversationActorKind actor = kind == ProjectConversationItemKind.SystemDecision
            ? ProjectConversationActorKind.SystemDecision
            : ProjectConversationActorKind.Mailbox;
        string label = kind == ProjectConversationItemKind.SystemDecision
            ? "System decision"
            : "Mailbox event";
        string? decisionLabel = view.CorrectionKind?.ToString() ?? view.DecisionKind?.ToString();

        return new ProjectConversationItemView(
            view.TenantId,
            view.ProjectId,
            view.ProjectDisplayName,
            view.AssociationId,
            kind,
            actor,
            label,
            view.DetectedAt,
            view.LifecycleState,
            view.ThresholdBand,
            view.ConfidenceScore,
            view.AssociationId,
            view.SourceMailboxId,
            view.SourceConversationId,
            view.SourceThreadId,
            view.SourceProvenance,
            view.RedactionState,
            view.RetentionClass,
            CurrentSchemaVersion,
            view.SourceVersion,
            view.CorrelationId,
            decisionLabel,
            view.SafeNextAction);
    }
}
