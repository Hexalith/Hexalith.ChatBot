using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

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
