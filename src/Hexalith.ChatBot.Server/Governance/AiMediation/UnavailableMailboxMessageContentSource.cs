namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal sealed class UnavailableMailboxMessageContentSource : IMailboxMessageContentSource
{
    public Task<MailboxMessageContentResult> GetAsync(
        string tenantId,
        string projectId,
        string sourceMessageId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new MailboxMessageContentResult(false, TaskIntentReasonCodes.SourceUnavailable));
    }
}
