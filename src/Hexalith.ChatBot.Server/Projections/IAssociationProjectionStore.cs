namespace Hexalith.ChatBot.Server.Projections;

internal interface IAssociationProjectionStore
{
    Task<AssociationCandidateView?> GetAsync(
        string tenantId,
        string associationId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(AssociationCandidateView view, CancellationToken cancellationToken = default);
}
