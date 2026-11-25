using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Tests.Projections;

/// <summary>Records attachment capture and proposal invalidation requests.</summary>
internal sealed class RecordingAssociationProjectionSideEffects
    : IAttachmentCaptureCoordinator, IAiActionProposalInvalidationCoordinator
{
    /// <summary>Gets the number of attachment captures requested.</summary>
    public int AttachmentCaptures { get; private set; }

    /// <summary>Gets the number of proposal invalidations requested.</summary>
    public int ProposalInvalidations { get; private set; }

    /// <inheritdoc/>
    public Task<AttachmentCaptureCoordinatorResult> CaptureAsync(
        AttachmentCaptureCoordinatorRequest request,
        CancellationToken cancellationToken)
    {
        AttachmentCaptures++;
        return Task.FromResult(new AttachmentCaptureCoordinatorResult(0, 0, 0));
    }

    /// <inheritdoc/>
    public Task InvalidateAsync(AssociationCandidateView correctedAssociation, CancellationToken cancellationToken)
    {
        ProposalInvalidations++;
        return Task.CompletedTask;
    }
}
