namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The single authoritative test-tenant predicate (Story 9.4, FR95/FR95a, addendum §Replay Isolation). Replay isolation
/// is achieved <b>by construction</b>, not by a flag on a production tenant: a tenant is a replay/test tenant iff its id
/// satisfies <see cref="IsTestTenant"/>, and that one predicate is consumed everywhere a test-tenant decision is made —
/// the outbound adapter selection (<see cref="Adapters.Mailbox.ReplayAwareOutboundMailboxSender"/>) and the nightly
/// isolation probe (<see cref="ReplayIsolationProbeCoordinator"/>). There is never a second, drifting check.
/// <para>
/// The discriminator is a deterministic, configuration-free convention: a reserved
/// <see cref="ReplayTestTenantPrefix"/> tenant-id prefix. A production tenant id never carries this prefix, so a
/// production tenant can <b>never</b> resolve the test-mode adapter ("Production tenants do not have access to the
/// test-mode adapter"). The rule lives in one place so server, adapter selection, and probe agree by construction.
/// </para>
/// <para>
/// Fail-closed (Epic 8/9 no-fabrication doctrine): an empty or unsafe tenant id is <b>not</b> a test tenant — it is
/// treated as production, so the probe sweep includes it and the test-mode adapter is never selected for it. The id must
/// be an <see cref="AuditMetadata.IsSafeStableIdentifier"/>-safe bounded token before it can be classified at all.
/// </para>
/// </summary>
internal static class ReplayTenantPolicy
{
    /// <summary>
    /// The reserved tenant-id prefix that marks a replay/test tenant. Configuration-free and single-source: the same
    /// constant is the only discriminator used by adapter selection and the isolation probe.
    /// </summary>
    public const string ReplayTestTenantPrefix = "replay-test:";

    /// <summary>
    /// Returns <see langword="true"/> when the tenant id is a replay/test tenant (a safe token carrying the reserved
    /// <see cref="ReplayTestTenantPrefix"/> <b>and a non-empty suffix</b>). Every other value — including empty,
    /// whitespace, the bare prefix, an unsafe token, or any production tenant id — is <see langword="false"/>
    /// (fail-closed → treated as production).
    /// <para>
    /// The suffix must be non-empty because <c>:</c> is itself a safe token character, so the bare
    /// <c>replay-test:</c> would otherwise classify as a test tenant and <see cref="StorageTenantFor"/> would derive
    /// an <b>empty</b> physical tenant — the default, unpartitioned namespace. That is precisely the "guarded label,
    /// unguarded data" failure this policy exists to prevent.
    /// </para>
    /// </summary>
    public static bool IsTestTenant(string? tenantId)
        => AuditMetadata.IsSafeStableIdentifier(tenantId) &&
            tenantId!.StartsWith(ReplayTestTenantPrefix, StringComparison.Ordinal) &&
            tenantId.Length > ReplayTestTenantPrefix.Length;

    /// <summary>
    /// Derives the physical/storage tenant id for a replay/test tenant by stripping the reserved prefix, or
    /// <see langword="null"/> when the argument is not a test tenant.
    /// <para>
    /// Some stores cannot carry the <c>:</c> in <see cref="ReplayTestTenantPrefix"/>, so a live-validation topology
    /// necessarily writes under a physical name. Deriving that name here keeps <see cref="IsTestTenant"/> the single
    /// discriminator: without this, a caller had to accept an arbitrary physical tenant alongside a
    /// <c>replay-test:</c> label and guard it with a separate ad-hoc check, so the fail-closed predicate protected the
    /// label while the data was written somewhere it did not cover.
    /// </para>
    /// </summary>
    public static string? StorageTenantFor(string? tenantId)
    {
        if (!IsTestTenant(tenantId))
        {
            return null;
        }

        // Strip exactly one prefix, then reject a suffix that still carries the separator. `:` is a legal identifier
        // character (it has to be, for the prefix itself), so a doubled label like `replay-test:replay-test:x` or an
        // embedded `replay-test:a:b` previously derived a physical tenant still containing `:` — the very character
        // this method exists to remove, because some stores cannot carry it.
        string storageTenant = tenantId![ReplayTestTenantPrefix.Length..];
        return storageTenant.Contains(':', StringComparison.Ordinal) ? null : storageTenant;
    }
}
