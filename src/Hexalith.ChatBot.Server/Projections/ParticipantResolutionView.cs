using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

public sealed record ParticipantResolutionView(
    string TenantId,
    string ResolutionId,
    string IntakeId,
    string SourceMailboxId,
    string SourceParticipantId,
    string? PartyId,
    ParticipantResolutionStatus Status,
    ParticipantResolutionBlockedReason? Reason,
    IReadOnlyList<ParticipantReviewAction> AllowedReviewActions,
    ProjectConversationParticipantDisplayKind DisplayKind,
    string SafeDisplayLabel,
    string EvidenceReference,
    string EvidenceFingerprint,
    string SchemaVersion,
    string SourceProvenance,
    string DerivationKernelVersion,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string CorrelationId,
    DateTimeOffset RecordedAt,
    DateTimeOffset LastUpdatedAt)
{
    public const string CurrentSchemaVersion = "chatbot.participant-resolution-view.v1";
    public const string CurrentDerivationKernelVersion = "participant-resolution.kernel.v1";
    public const string MetadataOnlyRedactionState = "metadata_only";
    public const string CollaborationRetentionClass = "collaboration_input";
    public const string MailboxSourceProvenance = "m365-mailbox-intake";

    public static string KeyFor(string tenantId, string resolutionId, string sourceParticipantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolutionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceParticipantId);
        return $"{tenantId}:participant-resolution:{resolutionId}:{sourceParticipantId}";
    }
}
