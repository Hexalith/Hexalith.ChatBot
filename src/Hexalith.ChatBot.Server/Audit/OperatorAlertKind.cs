namespace Hexalith.ChatBot.Server.Audit;

internal enum OperatorAlertKind
{
    AuditUnavailable,
    TenantScopeUnresolved,
    PostCommitAuditReconciliationRequired,
    CorrectionDelayed,
    RetryExhausted,
    DependencyDegraded,

    // Story 8.4 (NFR43): the four additional default operational alert thresholds wired alongside the existing
    // RetryExhausted kind (which already matches AC2's retry-exhaustion firing condition).
    AuditProjectionLagBreached,
    SubscriptionExpiryImminent,
    ApprovalQueueAgeBreached,
    AuthorizationFailureSpike,

    // Story 9.1 (NFR49a): the nightly per-tenant WORM chain verification detected a broken/incomplete chain. A
    // metadata-only operator alert to the on-call security engineer, fail-closed (an incomplete verification is itself
    // a breach signal, never a silent pass).
    AuditChainBroken,

    // Story 9.2 (NFR50a): the per-tenant audit-completeness fraction dropped below the 99.5% rolling-7-day target, OR
    // the measurement could not complete (unmeasurable → breach, never a silent pass). This is a P1 incident — audit
    // completeness is the compliance proof that "complete audit" is measured, not assumed — so the downstream incident
    // routing must treat it at P1 severity (the alert/payload encodes P1 explicitly).
    AuditCompletenessBudgetBreached,

    // Story 9.4 (FR95a, addendum §Replay Isolation): the nightly isolation probe found a replay-marked record in a
    // PRODUCTION tenant's outbound-trace store or WORM chain — or the sweep could not complete (Unknown → breach, never
    // a silent pass). This is a stop-ship / M2-gating defect: a passing M2 release requires zero such breaches. Emitted
    // fail-closed (audit-then-deliver), exactly one alert per breached production tenant.
    ReplayIsolationBreach,

    // Story 9.5 (FR55a/NFR9a/NFR59): the synthetic cross-tenant derived-store probe observed an owner tenant's seeded
    // sentinel through an intruder tenant's store-access scope — or the seed/read-back could not complete (Unknown →
    // breach, never a silent pass). Derived-store isolation is physical partitioning, not application filtering, so a
    // successful cross-tenant read is a stop-ship / M2-gating defect: a passing M2 release requires zero such breaches.
    // Emitted fail-closed (audit-then-deliver), exactly one alert per breached ordered tenant pair.
    DerivedStoreIsolationBreach,

    // Story 9.11 (NFR56/A10): the M2 continuity drill missed an RPO/RTO target (or detected data loss), OR could not
    // complete (unmeasurable → the fail-safe breach, never a silent pass). An RPO/RTO miss remains a distinct A10
    // target deviation that flags recalibration and records a follow-up; the external evidence gate applies the
    // approved blocking policy without relabeling it as a structural breach. Emitted fail-closed (audit-then-deliver),
    // exactly one alert per breached drill.
    ContinuityDrillTargetMissed,

    // Story 9.12 (NFR57/NFR49a): the projection-rebuild validation found a non-deterministic rebuild (divergent — the
    // serious NFR49a/invariant-#11 breach: it makes evidence snapshots / approval records non-reproducible), missed the
    // 4-hr rebuild target (DurationWithinTarget == false — a recovery-time miss / recalibration signal, like the 9.11
    // RPO/RTO miss), or could not complete (unmeasurable → the fail-safe breach, never a silent pass). Emitted
    // fail-closed (audit-then-deliver), exactly one alert per breached validation.
    ProjectionRebuildValidationFailed,

    // Story 9.13 (NFR58/NFR59/NFR41): the scoped-outage degradation validation found an isolation/scope/recovery breach
    // (breached — the serious NFR58/NFR59 breach: cross-tenant leakage, unauthorized mutation, silent data loss, scope
    // escape, non-recoverable in-flight, or duplicate side effect), recorded the incident scope late
    // (ScopeRecordedWithinTarget == false — an NFR41 monitoring-latency miss / recalibration signal), or could not
    // complete (unmeasurable → the fail-safe breach). Emitted fail-closed (audit-then-deliver), exactly one alert per
    // breached validation. Distinct from the Story 8.5 runtime DependencyDegraded alert (this is the validation breach,
    // not the live degradation).
    ScopedOutageDegradationBreach,
}
