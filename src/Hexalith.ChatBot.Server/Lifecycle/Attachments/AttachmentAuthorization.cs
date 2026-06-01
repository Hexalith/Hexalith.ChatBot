using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal interface IAttachmentAuthorizationService
{
    AttachmentAuthorizationResult Authorize(ProjectConversationAttachmentStorageCandidate candidate);
}

internal enum AttachmentAuthorizationState
{
    Authorized,
    Redacted,
    Unavailable,
    Retryable,
}

internal sealed record AttachmentAuthorizationResult(
    AttachmentAuthorizationState State,
    ProjectConversationAttachmentSafetyOutcomeView? RedactedOutcome)
{
    public static AttachmentAuthorizationResult Authorized()
        => new(AttachmentAuthorizationState.Authorized, null);

    public static AttachmentAuthorizationResult Redacted(ProjectConversationAttachmentStorageCandidate candidate, long sourceVersion, string correlationId)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return new AttachmentAuthorizationResult(
            AttachmentAuthorizationState.Redacted,
            new ProjectConversationAttachmentSafetyOutcomeView(
                candidate.TenantId,
                candidate.ProjectId,
                candidate.AssociationId,
                candidate.IntakeId,
                candidate.ProviderAttachmentId,
                candidate.Ordinal,
                ProjectConversationAttachmentStatus.Unavailable,
                "redacted",
                [],
                "redacted",
                "none",
                "attachment_authorization_redacted",
                sourceVersion,
                correlationId,
                AttachmentUnsafeHandling.Quarantine));
    }
}

internal sealed class ProjectionAttachmentAuthorizationService : IAttachmentAuthorizationService
{
    public AttachmentAuthorizationResult Authorize(ProjectConversationAttachmentStorageCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return string.Equals(candidate.RedactionState, "metadata_only", StringComparison.Ordinal)
            ? AttachmentAuthorizationResult.Authorized()
            : AttachmentAuthorizationResult.Redacted(candidate, candidate.SourceVersion, candidate.CorrelationId);
    }
}
