namespace Hexalith.ChatBot.Server.Projections;

internal interface IParticipantResolutionProjectionStore
{
    Task<ParticipantResolutionView?> GetAsync(
        string tenantId,
        string resolutionId,
        string sourceParticipantId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(ParticipantResolutionView view, CancellationToken cancellationToken = default);
}
