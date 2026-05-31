using Dapr.Client;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Production governed operation projection store backed by the DAPR <c>chatbot-statestore</c> (Redis). It is
/// the swap for <see cref="InMemoryGovernedOperationProjectionStore"/> once a DAPR sidecar is present. Order
/// tolerance and idempotency are enforced upstream by <see cref="GovernedOperationProjectionHandler"/> (the
/// handler reads-checks-writes by source version), so this adapter is a straight tenant-partitioned get/save.
/// </summary>
/// <remarks>
/// Not registered by default in M0 (no sidecar in the unit/integration sandbox). The Tier-3 Aspire E2E wires
/// it against the real state store. Build-only here; runtime behaviour is exercised under the DAPR topology.
/// </remarks>
internal sealed class DaprGovernedOperationViewStore(DaprClient daprClient) : IGovernedOperationProjectionStore
{
    /// <summary>The DAPR state store component name for the ChatBot read models.</summary>
    public const string StateStoreName = "chatbot-statestore";

    public async Task<GovernedOperationView?> GetAsync(string tenantId, string noteId, CancellationToken cancellationToken = default)
        => await daprClient
            .GetStateAsync<GovernedOperationView?>(
                StateStoreName,
                GovernedOperationView.KeyFor(tenantId, noteId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

    public async Task SaveAsync(GovernedOperationView view, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        await daprClient
            .SaveStateAsync(
                StateStoreName,
                GovernedOperationView.KeyFor(view.TenantId, view.NoteId),
                view,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
