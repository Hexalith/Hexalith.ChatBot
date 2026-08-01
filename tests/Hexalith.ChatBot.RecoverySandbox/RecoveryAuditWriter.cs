using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Controllable writer used to exercise ChatBot's required pre-commit audit seam.</summary>
internal sealed class RecoveryAuditWriter(RecoveryScopedOutageState state) : IAuditWriter
{
    /// <inheritdoc />
    public ValueTask RecordAuthorizationFailureAsync(
        ChatBotAuthorizationFailureAuditFact fact,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<AuditWriteResult> RecordPreCommitAsync(
        AuditEnvelope envelope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();
        if (state.IsFaulted("audit-store"))
        {
            state.RecordFaultObservation("audit-store");
            return ValueTask.FromResult(AuditWriteResult.Unavailable());
        }

        _ = state.RecordEffect("audit-store", envelope.TenantId, envelope.CorrelationId);
        return ValueTask.FromResult(AuditWriteResult.Success);
    }

    /// <inheritdoc />
    public ValueTask<AuditWriteResult> RecordPostCommitAsync(
        AuditEnvelope envelope,
        CancellationToken cancellationToken)
        => RecordPreCommitAsync(envelope, cancellationToken);
}
