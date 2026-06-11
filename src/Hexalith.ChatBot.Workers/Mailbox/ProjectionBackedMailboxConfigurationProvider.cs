using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Workers.Mailbox;

public interface IMailboxSourceControlProjection
{
    ValueTask<MailboxSourceControlState?> GetControlStateAsync(
        string tenantId,
        string mailboxSourceRef,
        CancellationToken cancellationToken);
}

public interface IMailboxSourceRateLimitProjection
{
    ValueTask<MailboxRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string mailboxSourceRef,
        CancellationToken cancellationToken);
}

public sealed class ProjectionBackedMailboxConfigurationProvider(
    IMailboxConfigurationProvider configuredProvider,
    IMailboxSourceControlProjection controlProjection,
    IMailboxSourceRateLimitProjection rateLimitProjection) : IMailboxConfigurationProvider
{
    public async ValueTask<ControlledMailboxPattern?> ResolvePatternAsync(
        string tenantId,
        string notificationMailboxId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(notificationMailboxId);

        ControlledMailboxPattern? configured = await configuredProvider
            .ResolvePatternAsync(tenantId, notificationMailboxId, cancellationToken)
            .ConfigureAwait(false);
        if (configured is null)
        {
            return null;
        }

        MailboxSourceControlState controlState = await controlProjection
            .GetControlStateAsync(tenantId, configured.MailboxId, cancellationToken)
            .ConfigureAwait(false)
            ?? configured.ControlState;
        MailboxRateLimitState? rateLimit = await rateLimitProjection
            .GetRateLimitAsync(tenantId, configured.MailboxId, cancellationToken)
            .ConfigureAwait(false)
            ?? configured.RateLimit;

        return configured with
        {
            ControlState = controlState,
            RateLimit = rateLimit,
        };
    }
}

public sealed class StaticMailboxSourceControlProjection(MailboxSourceControlState? state = null) : IMailboxSourceControlProjection
{
    public ValueTask<MailboxSourceControlState?> GetControlStateAsync(
        string tenantId,
        string mailboxSourceRef,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(state);
}

public sealed class StaticMailboxSourceRateLimitProjection(MailboxRateLimitState? state = null) : IMailboxSourceRateLimitProjection
{
    public ValueTask<MailboxRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string mailboxSourceRef,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(state);
}
