using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record ProjectConversationAttachmentStorageCandidate(
    string TenantId,
    string ProjectId,
    string AssociationId,
    string IntakeId,
    string SourceMailboxId,
    string ProviderMessageId,
    string ProviderAttachmentId,
    int Ordinal,
    string? SafeDisplayName,
    string? ContentType,
    long? SizeInBytes,
    ProjectConversationAttachmentStatus StorageStatus,
    string? FolderId,
    string? FileId,
    string RedactionState,
    long SourceVersion,
    string CorrelationId);

internal sealed record ProjectConversationAttachmentStorageOutcomeView(
    string TenantId,
    string ProjectId,
    string AssociationId,
    string IntakeId,
    string ProviderAttachmentId,
    int Ordinal,
    ProjectConversationAttachmentStatus StorageStatus,
    string? FolderId,
    string? FileId,
    string DuplicateState,
    string RetryState,
    string AiContextEligibility,
    IReadOnlyList<string> AllowedActions,
    long SourceVersion,
    string CorrelationId)
{
    public static ProjectConversationAttachmentStorageOutcomeView Stored(
        ProjectConversationAttachmentStorageCandidate candidate,
        string folderId,
        string fileId,
        string duplicateState,
        string retryState,
        string aiContextEligibility,
        IReadOnlyList<string> allowedActions,
        long sourceVersion,
        string correlationId)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentException.ThrowIfNullOrWhiteSpace(folderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new ProjectConversationAttachmentStorageOutcomeView(
            candidate.TenantId,
            candidate.ProjectId,
            candidate.AssociationId,
            candidate.IntakeId,
            candidate.ProviderAttachmentId,
            candidate.Ordinal,
            ProjectConversationAttachmentStatus.Captured,
            folderId,
            fileId,
            duplicateState,
            retryState,
            aiContextEligibility,
            allowedActions,
            sourceVersion,
            correlationId);
    }

    public static ProjectConversationAttachmentStorageOutcomeView Failed(
        ProjectConversationAttachmentStorageCandidate candidate,
        ProjectConversationAttachmentStatus status,
        string duplicateState,
        string retryState,
        string aiContextEligibility,
        long sourceVersion,
        string correlationId)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (status is ProjectConversationAttachmentStatus.Captured)
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Failure outcomes must not claim captured storage.");
        }

        return new ProjectConversationAttachmentStorageOutcomeView(
            candidate.TenantId,
            candidate.ProjectId,
            candidate.AssociationId,
            candidate.IntakeId,
            candidate.ProviderAttachmentId,
            candidate.Ordinal,
            status,
            null,
            null,
            duplicateState,
            retryState,
            aiContextEligibility,
            [],
            sourceVersion,
            correlationId);
    }
}

internal sealed record ProjectConversationAttachmentSafetyOutcomeView(
    string TenantId,
    string ProjectId,
    string AssociationId,
    string IntakeId,
    string ProviderAttachmentId,
    int Ordinal,
    ProjectConversationAttachmentStatus ScanStatus,
    string AiContextEligibility,
    IReadOnlyList<string> AllowedActions,
    string RetryState,
    string SafeNextAction,
    string ReasonCode,
    long SourceVersion,
    string CorrelationId,
    string UnsafeHandling,
    bool SupersedesTerminalState = false);
