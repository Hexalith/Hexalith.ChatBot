namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The metadata-only result of one ordered-pair cross-tenant isolation probe (Story 9.5, AC2, FR55a). Carries the owner
/// and intruder tenant refs, the status, a bounded reason code, and a safe first-offender locator token (the first
/// owner sentinel the intruder could observe) — <b>never</b> any derived-store content. <see cref="IsBreach"/> folds the
/// fail-closed doctrine: anything other than <see cref="DerivedStoreIsolationStatus.Clean"/> is a breach to be alerted
/// (a probe that cannot complete is never a silent pass). Mirrors <see cref="ReplayIsolationVerificationResult"/>.
/// <para>
/// Unlike the Story 9.4 replay probe (which scans for replay-marked records), this is an <b>active negative probe</b>:
/// a <i>successful</i> cross-tenant read — the intruder observing the owner's sentinel — is the breach.
/// </para>
/// </summary>
/// <param name="OwnerTenantRef">The tenant that owns the seeded sentinels (the data at risk).</param>
/// <param name="IntruderTenantRef">The tenant whose scope attempted the cross-tenant read.</param>
/// <param name="Status">The probe status.</param>
/// <param name="ReasonCode">The bounded reason code.</param>
/// <param name="FirstOffenderLocator">A safe locator for the first leaked sentinel, or null when clean/incomplete.</param>
internal sealed record DerivedStoreIsolationVerificationResult(
    string OwnerTenantRef,
    string IntruderTenantRef,
    DerivedStoreIsolationStatus Status,
    string ReasonCode,
    string? FirstOffenderLocator)
{
    /// <summary>Gets a value indicating whether this result is a breach (anything other than <see cref="DerivedStoreIsolationStatus.Clean"/>).</summary>
    public bool IsBreach => Status != DerivedStoreIsolationStatus.Clean;

    /// <summary>The reason code for a clean pair-probe (the intruder observed nothing of the owner's data).</summary>
    public const string CleanReasonCode = "derived_store_isolation_clean";

    /// <summary>The reason code for a breach (the intruder observed the owner's sentinel through the store seam).</summary>
    public const string BreachReasonCode = "derived_store_isolation_breach";

    /// <summary>The reason code for an incomplete probe (the seam threw during seed or read-back) — a fail-closed breach signal.</summary>
    public const string ProbeIncompleteReasonCode = "derived_store_isolation_probe_incomplete";
}
