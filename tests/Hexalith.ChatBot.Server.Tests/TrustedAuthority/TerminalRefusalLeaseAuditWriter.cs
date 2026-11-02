using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Tests.TrustedAuthority;

/// <summary>Holds the real denial callback so its committed SDK lease can be observed before response completion.</summary>
internal sealed class TerminalRefusalLeaseAuditWriter : IAuditWriter
{
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets completion when the callback enters safe denial auditing.</summary>
    public Task Entered => _entered.Task;

    /// <summary>Lets the bounded rejection finish.</summary>
    public void Release() => _release.TrySetResult();

    /// <inheritdoc/>
    public async ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
    {
        _entered.TrySetResult();
        await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Terminal refusal cannot precommit.");

    /// <inheritdoc/>
    public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Terminal refusal cannot postcommit.");
}
