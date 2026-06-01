namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal interface IMailboxMessageContentSource
{
    Task<MailboxMessageContentResult> GetAsync(
        string tenantId,
        string projectId,
        string sourceMessageId,
        CancellationToken cancellationToken = default);
}
