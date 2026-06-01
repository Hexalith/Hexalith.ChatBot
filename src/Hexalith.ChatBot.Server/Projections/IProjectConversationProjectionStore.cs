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

    Task UpsertParticipantResolutionAsync(
        ParticipantResolutionView participant,
        CancellationToken cancellationToken = default);

    Task UpsertAttachmentReferencesAsync(
        ProjectConversationAttachmentSetView attachments,
        CancellationToken cancellationToken = default);

    Task UpsertApprovalEventAsync(
        ApprovalEventView approval,
        CancellationToken cancellationToken = default);

    Task UpsertFailureStateEventAsync(
        FailureStateEventView failure,
        CancellationToken cancellationToken = default);

    Task UpsertAiOutcomeEventAsync(
        AiOutcomeEventView outcome,
        CancellationToken cancellationToken = default);

    Task<ProjectConversationPage> ReadPageAsync(
        string tenantId,
        string projectId,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken = default);
}
