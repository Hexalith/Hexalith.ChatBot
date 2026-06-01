namespace Hexalith.ChatBot.Server.Projections;

internal interface IProjectConversationProjectionStore
{
    Task UpsertAsync(ProjectConversationItemView item, CancellationToken cancellationToken = default);

    Task<ProjectConversationSourceEmailView?> GetSourceEmailAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken = default);

    Task UpsertSourceEmailAsync(
        ProjectConversationSourceEmailView source,
        CancellationToken cancellationToken = default);

    Task<ProjectConversationPage> ReadPageAsync(
        string tenantId,
        string projectId,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken = default);
}
