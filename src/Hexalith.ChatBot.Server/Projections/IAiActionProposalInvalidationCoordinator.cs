namespace Hexalith.ChatBot.Server.Projections;

internal interface IAiActionProposalInvalidationCoordinator
{
    Task InvalidateAsync(AssociationCandidateView correctedAssociation, CancellationToken cancellationToken = default);
}
