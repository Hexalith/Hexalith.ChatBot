using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

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
