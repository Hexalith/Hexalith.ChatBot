using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Story 9.10 (AC4, NFR7/FR68): the server-callable fail-closed consent gate. It composes the pure
/// <see cref="ConsentRequirementPolicy.Evaluate"/> (subject kind + profile ⇒ required/not-required) with the pure
/// <see cref="ConsentGate.Evaluate"/> (requirement + active-basis status ⇒ satisfied/blocked-missing-basis), returning
/// a <see cref="ConsentGateDecisions"/> token. This is the decision the AI-processing / retention execution paths will
/// consult before mutating state.
/// <para>
/// DEFERRED (AC1 inert-control-floor): the live wiring into the <c>ProposeAIAction</c> and retention <b>execution</b>
/// call sites is NOT shipped in this story — those fan-outs are modeled as documented hooks
/// (<see cref="EvaluateForGovernedAction"/> is the call the live worker will make), exactly as Story 9.9's
/// <c>DeletionErasureRunner.DestroyNonAuditStoreSubjectAsync</c> modeled the non-audit-store destruction runtime. The
/// decision itself is real and tested at the pure-function layer; only the live fan-out is deferred.
/// </para>
/// </summary>
internal static class ConsentGateEvaluator
{
    /// <summary>
    /// The fail-closed gate decision for a governed action over <paramref name="subjectKind"/>. Resolves the
    /// requirement disposition from <paramref name="profile"/> (unknown kind / missing entry ⇒ <c>required</c>), then
    /// gates on <paramref name="activeRecordStatus"/> (only an <c>active</c> basis satisfies a <c>required</c> kind).
    /// </summary>
    public static string EvaluateForGovernedAction(
        string? subjectKind,
        ConsentRequirementProfile profile,
        string? activeRecordStatus)
    {
        ArgumentNullException.ThrowIfNull(profile);

        string disposition = ConsentRequirementPolicy.Evaluate(subjectKind, profile);
        return ConsentGate.Evaluate(disposition, activeRecordStatus);
    }
}

/// <summary>
/// Story 9.10 (AC1/AC4, inert-control-floor): the DEFERRED server seam that will build a tenant-overridden
/// <see cref="ConsentRequirementProfile"/> from a referenced tenant-policy snapshot. For now it returns the
/// regulatory-profile default (<see cref="ConsentRequirementMatrix.Published"/>) — the live policy-snapshot →
/// requirement-profile merge (the tenant-policy-knob override that lets a tenant additionally require a basis beyond
/// the regulatory default) is M2-deferred. The <see cref="ConsentRequirementMatrix.Published"/> seed + the pure
/// evaluator ship now; this mapper is the seam the override will populate.
/// </summary>
internal static class ConsentRequirementProfileMapper
{
    /// <summary>
    /// Resolves the requirement profile for <paramref name="policySnapshotId"/>. DEFERRED: the live merge of the
    /// tenant policy-snapshot override over the regulatory default is not wired in this story — it returns the
    /// published regulatory-profile matrix unchanged. See the Story 9.10 ADR deferrals.
    /// </summary>
    public static ConsentRequirementProfile ProfileFor(string? policySnapshotId)
    {
        _ = policySnapshotId;

        // Deferred: no tenant policy-snapshot → requirement-profile merge ships in Story 9.10. The regulatory-profile
        // default biases every governed subject kind to `required` (fail-closed).
        return ConsentRequirementMatrix.Published;
    }
}
