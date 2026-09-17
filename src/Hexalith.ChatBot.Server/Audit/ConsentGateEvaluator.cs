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
