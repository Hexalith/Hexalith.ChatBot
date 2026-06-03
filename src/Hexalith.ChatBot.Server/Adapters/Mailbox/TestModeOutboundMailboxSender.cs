using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

/// <summary>
/// The test-mode outbound adapter (Story 9.4, AC1, FR95/FR95a, addendum §Replay Isolation). It is the outbound sender
/// for a <b>test tenant only</b> (selected by <see cref="ReplayAwareOutboundMailboxSender"/> via
/// <see cref="Audit.ReplayTenantPolicy.IsTestTenant"/>). On every send it <b>intercepts</b> the call, records the
/// metadata-only would-have-sent envelope to the test tenant's <see cref="IOutboundTraceStore"/>, and returns
/// <see cref="OutboundMailboxSendResult.Sent(string)"/> with the <c>adapter:mailbox-outbound-testmode</c> ref —
/// <b>without contacting any external system</b>. There is no external client to inject, so it cannot send by
/// construction.
/// <para>
/// Returning <c>Sent</c> is deliberate: the aggregate's <c>AdapterStatus == "sent"</c> path then runs <b>identically</b>
/// to production, so a replay run exercises the real success flow end-to-end while no message ever leaves the boundary.
/// The trace record carries only the safe identity tokens already on the request plus the replay run id and a server UTC
/// timestamp — never recipient/subject/body (NFR2/NFR42).
/// </para>
/// </summary>
internal sealed class TestModeOutboundMailboxSender(IOutboundTraceStore traceStore, ISystemClock clock)
    : IOutboundMailboxSender
{
    /// <summary>The adapter ref returned for an intercepted test-mode send — distinct from the production adapter ref.</summary>
    public const string TestModeAdapterRef = "adapter:mailbox-outbound-testmode";

    public async ValueTask<OutboundMailboxSendResult> SendAsync(
        OutboundMailboxSendRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        // Record the would-have-sent envelope BEFORE returning success — the trace is the evidence the run never sent.
        OutboundTraceRecord record = OutboundTraceRecord.FromRequest(request, clock.UtcNow);
        await traceStore.RecordAsync(record, cancellationToken).ConfigureAwait(false);

        // No external client exists on this sender, so success is returned without any external contact.
        return OutboundMailboxSendResult.Sent(TestModeAdapterRef);
    }
}

/// <summary>
/// The single tenant-aware adapter-selection seam (Story 9.4, AC1). It is the registered
/// <see cref="IOutboundMailboxSender"/> the dispatcher resolves, and it routes each send by the <b>one</b> authoritative
/// predicate <see cref="Audit.ReplayTenantPolicy.IsTestTenant"/>: a <b>test tenant</b> resolves the
/// <see cref="TestModeOutboundMailboxSender"/> (intercept + record, never send); <b>every production tenant</b> resolves
/// the existing production sender unchanged. There is exactly one decision point, so production tenants are never
/// reachable to the test-mode adapter ("Production tenants do not have access to the test-mode adapter").
/// </summary>
internal sealed class ReplayAwareOutboundMailboxSender(
    IOutboundMailboxSender productionSender,
    TestModeOutboundMailboxSender testModeSender) : IOutboundMailboxSender
{
    public ValueTask<OutboundMailboxSendResult> SendAsync(
        OutboundMailboxSendRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Audit.ReplayTenantPolicy.IsTestTenant(request.TenantId)
            ? testModeSender.SendAsync(request, cancellationToken)
            : productionSender.SendAsync(request, cancellationToken);
    }
}
