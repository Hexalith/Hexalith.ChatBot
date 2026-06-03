namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Pure, deterministic verifier for one ordered tenant pair's derived-store cross-tenant isolation (Story 9.5, AC2,
/// FR55a). It is an <b>active negative probe</b>: given the owner tenant's seeded sentinel resource ids and the ids
/// actually <b>observable through the intruder tenant's store-access scope</b>, a non-empty intersection ⇒
/// <see cref="DerivedStoreIsolationStatus.Breach"/> (the intruder physically read the owner's data — isolation failed
/// below the application layer). An empty intersection ⇒ <see cref="DerivedStoreIsolationStatus.Clean"/>.
/// <para>
/// It returns a metadata-only <see cref="DerivedStoreIsolationVerificationResult"/> — the owner/intruder refs, the
/// status, a bounded reason code, and a safe first-offender locator (the first leaked sentinel) — never derived-store
/// content. Mirrors <see cref="ReplayIsolationVerifier"/>. The verifier itself never returns
/// <see cref="DerivedStoreIsolationStatus.Unknown"/>; the coordinator maps a seed/read that throws to <c>Unknown</c> — a
/// breach signal — rather than a silent pass (the same fail-closed split as the replay/chain verifier-coordinator).
/// </para>
/// </summary>
internal static class DerivedStoreIsolationVerifier
{
    /// <summary>
    /// Verifies one ordered tenant pair. The first owner sentinel that appears in the intruder-observable set is the
    /// breach locator (deterministic: owner-sentinel order is preserved so the locator is stable).
    /// </summary>
    /// <param name="ownerTenant">The tenant that seeded the sentinels.</param>
    /// <param name="intruderTenant">The tenant whose scope attempted the cross-tenant read.</param>
    /// <param name="ownerSentinelResourceIds">The sentinel resource ids seeded into the owner's partitions.</param>
    /// <param name="idsObservableThroughIntruderScope">The owner-sentinel ids the intruder's scope could actually read back.</param>
    /// <returns>The metadata-only verification result.</returns>
    public static DerivedStoreIsolationVerificationResult Verify(
        string ownerTenant,
        string intruderTenant,
        IReadOnlyList<string> ownerSentinelResourceIds,
        IReadOnlyList<string> idsObservableThroughIntruderScope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerTenant);
        ArgumentException.ThrowIfNullOrWhiteSpace(intruderTenant);
        ArgumentNullException.ThrowIfNull(ownerSentinelResourceIds);
        ArgumentNullException.ThrowIfNull(idsObservableThroughIntruderScope);

        HashSet<string> observable = new(idsObservableThroughIntruderScope, StringComparer.Ordinal);

        for (int index = 0; index < ownerSentinelResourceIds.Count; index++)
        {
            string sentinel = ownerSentinelResourceIds[index];
            if (observable.Contains(sentinel))
            {
                return new DerivedStoreIsolationVerificationResult(
                    ownerTenant,
                    intruderTenant,
                    DerivedStoreIsolationStatus.Breach,
                    DerivedStoreIsolationVerificationResult.BreachReasonCode,
                    Locator(sentinel));
            }
        }

        return new DerivedStoreIsolationVerificationResult(
            ownerTenant,
            intruderTenant,
            DerivedStoreIsolationStatus.Clean,
            DerivedStoreIsolationVerificationResult.CleanReasonCode,
            FirstOffenderLocator: null);
    }

    // A safe, bounded locator for the first leaked sentinel: its safe token when available, else a fixed marker.
    private static string Locator(string sentinelResourceId)
        => AuditMetadata.SafeOptionalToken(sentinelResourceId) is { } safe
            ? $"derived-store-sentinel:{safe}"
            : "derived-store-sentinel:unsafe-token";
}
