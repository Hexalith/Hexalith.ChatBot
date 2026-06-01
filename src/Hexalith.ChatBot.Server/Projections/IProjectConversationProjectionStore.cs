namespace Hexalith.ChatBot.Server.Projections;

internal interface IProjectConversationProjectionStore
{
    Task UpsertAsync(ProjectConversationItemView item, CancellationToken cancellationToken = default);

    Task<ProjectConversationPage> ReadPageAsync(
        string tenantId,
        string projectId,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken = default);
}
