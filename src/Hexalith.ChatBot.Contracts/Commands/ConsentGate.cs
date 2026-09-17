using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The pure AC4 fail-closed gate decision (NFR7/FR68). Given a requirement disposition and the active-basis status of
/// the subject, returns a <see cref="ConsentGateDecisions"/> token: <c>not-required ⇒ satisfied</c>; <c>required</c>
/// AND an <c>active</c> basis ⇒ <c>satisfied</c>; <c>required</c> with a <c>null</c>/<c>withdrawn</c>/<c>expired</c>/
/// <c>superseded</c> status ⇒ <c>blocked-missing-basis</c>; an UNKNOWN disposition biases to <c>blocked-missing-basis</c>.
/// This is a real, testable function — not a comment.
/// </summary>
public static class ConsentGate
{
    public static string Evaluate(string? requirementDisposition, string? activeRecordStatus)
    {
        // not-required ⇒ satisfied without a basis record.
        if (string.Equals(requirementDisposition, ConsentRequirementDispositions.NotRequired, StringComparison.Ordinal))
        {
            return ConsentGateDecisions.Satisfied;
        }

        // required ⇒ only an active basis satisfies; everything else (null / withdrawn / expired / superseded)
        // fails closed. An unknown disposition also biases to blocked (fail-closed over convenience).
        if (string.Equals(requirementDisposition, ConsentRequirementDispositions.Required, StringComparison.Ordinal) &&
            string.Equals(activeRecordStatus, ConsentRecordStatuses.Active, StringComparison.Ordinal))
        {
            return ConsentGateDecisions.Satisfied;
        }

        return ConsentGateDecisions.BlockedMissingBasis;
    }
}
