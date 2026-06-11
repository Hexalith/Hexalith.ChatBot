namespace Hexalith.ChatBot.Server.Projections;

internal interface IGovernedControlStateProjectionStore
{
    Task<GovernedControlStateView?> GetAsync(
        string tenantId,
        string subjectClass,
        string subjectRef,
        CancellationToken cancellationToken = default);

    Task SaveAsync(GovernedControlStateView view, CancellationToken cancellationToken = default);
}
