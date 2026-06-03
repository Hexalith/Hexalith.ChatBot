using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Composes the WORM hash chain (Story 9.1, NFR49a) <b>behind</b> the existing post-commit audit seam. It decorates the
/// in-process <see cref="InMemoryAuditWriter"/> (preserving its <see cref="IAuditHistoryReader"/> surface for the Story
/// 1.9 audit-history UI) and, on every post-commit write, also appends the envelope to the per-tenant
/// <see cref="IWormAuditStore"/> chain.
/// <para>
/// The append is <b>fail-open-then-reconcile</b> (two-phase audit, D4): a chain-append failure is surfaced as
/// <see cref="AuditWriteResult.Unavailable"/> so the gateway's established
/// <c>QueueReplayIntentAsync</c> + <c>PostCommitAuditReconciliationRequired</c> path rebuilds the gap from the durable
/// event log. The chain never gates the commit — pre-commit and authorization-failure writes pass straight through to
/// the inner writer and never touch the chain.
/// </para>
/// </summary>
internal sealed class ChainedAuditWriter(InMemoryAuditWriter inner, IWormAuditStore wormStore) : IAuditWriter, IAuditHistoryReader
{
    public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
        => inner.RecordAuthorizationFailureAsync(fact, cancellationToken);

    public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        => inner.RecordPreCommitAsync(envelope, cancellationToken);

    public async ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        // The inner writer records the envelope for the metadata-only history surface first. If even that fails the
        // chain is not attempted — the existing reconcile path already owns that failure.
        AuditWriteResult innerResult = await inner.RecordPostCommitAsync(envelope, cancellationToken).ConfigureAwait(false);
        if (!innerResult.Succeeded)
        {
            return innerResult;
        }

        WormAuditAppendOutcome append = await wormStore.AppendAsync(envelope, cancellationToken).ConfigureAwait(false);
        return append.Succeeded
            ? AuditWriteResult.Success
            : AuditWriteResult.Unavailable(append.ReasonCode);
    }

    public IReadOnlyList<AuditEnvelope> GetPostCommitEnvelopes(string tenantId, string commandId)
        => inner.GetPostCommitEnvelopes(tenantId, commandId);
}
