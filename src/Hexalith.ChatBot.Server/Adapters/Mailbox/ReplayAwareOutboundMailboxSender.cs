using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

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
