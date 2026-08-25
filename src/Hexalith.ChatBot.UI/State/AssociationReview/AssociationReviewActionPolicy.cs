namespace Hexalith.ChatBot.UI.State.AssociationReview;

/// <summary>Outcome of the correction panel's status resolution.</summary>
public enum AssociationCorrectionStatus
{
    Blocked,
    Delayed,
    Pending,
    Partial,
    ProjectionPending,
    Success,
}

/// <summary>
/// Decides which association actions an operator may take and which reason is shown when they may not.
/// This is a governance-visible gate, so it lives outside the Razor <c>@code</c> block where unit tests can
/// execute it directly - the repository renders no components under test, so logic left in markup can only
/// ever be "verified" by asserting that its source text exists.
/// </summary>
public static class AssociationReviewActionPolicy
{
    /// <summary>
    /// Server-supplied disabled reasons in the order they should be reported when several apply. Decision
    /// actions and the correction submit share one list: a reason that blocks the association blocks both,
    /// and two divergent lists previously left <c>policy-blocked</c>, <c>target-unauthorized</c>,
    /// <c>audit-unavailable</c>, <c>already-corrected</c> and <c>projection-invalidation-unavailable</c>
    /// unable to disable any decision action.
    /// </summary>
    public static readonly string[] DisabledReasonPriority =
    [
        "not-authorized",
        "target-unauthorized",
        "policy-blocked",
        "already-decided",
        "already-corrected",
        "evidence-expired",
        "stale-evidence",
        "corrected-context-stale",
        "correction-delayed",
        "projection-pending",
        "projection-invalidation-unavailable",
        "audit-unavailable",
    ];

    /// <summary>Resolves the reason a decision action is disabled, or an empty string when it is enabled.</summary>
    public static string ResolveDecisionDisabledReasonCode(
        bool isTerminal,
        bool isSubmitting,
        bool requiresCandidate,
        bool hasSelectedCandidate,
        IReadOnlyList<string> disabledReasons)
    {
        ArgumentNullException.ThrowIfNull(disabledReasons);

        if (isTerminal || disabledReasons.Contains("terminal-state", StringComparer.Ordinal))
        {
            return "terminal-state";
        }

        // A governed command is already in flight. Without this the same durable decision is submitted once
        // per click.
        if (isSubmitting)
        {
            return "submit-in-flight";
        }

        if (requiresCandidate && !hasSelectedCandidate)
        {
            return "candidate-required";
        }

        return FirstApplicable(disabledReasons);
    }

    /// <summary>Resolves the reason the correction submit is disabled, or an empty string when it is enabled.</summary>
    public static string ResolveCorrectionDisabledReasonCode(
        bool canCorrect,
        bool isSubmitting,
        bool hasSelectedCandidate,
        IReadOnlyList<string> disabledReasons)
    {
        ArgumentNullException.ThrowIfNull(disabledReasons);

        if (!canCorrect)
        {
            return "correction-invalid-lifecycle";
        }

        if (isSubmitting)
        {
            return "submit-in-flight";
        }

        if (!hasSelectedCandidate)
        {
            return "correction-target-required";
        }

        return FirstApplicable(disabledReasons);
    }

    /// <summary>
    /// Resolves the correction panel's status. Ordinal-insensitive on the server status tokens so a
    /// differently-cased value cannot fall through to <see cref="AssociationCorrectionStatus.Success"/>.
    /// </summary>
    public static AssociationCorrectionStatus ResolveCorrectionStatus(
        IReadOnlyList<string> disabledReasons,
        string? propagationStatus,
        string? downstreamImpactStatus,
        string? correctedProjectId,
        bool isCorrectedContextStale)
    {
        ArgumentNullException.ThrowIfNull(disabledReasons);

        if (disabledReasons.Contains("target-unauthorized", StringComparer.Ordinal)
            || disabledReasons.Contains("policy-blocked", StringComparer.Ordinal))
        {
            return AssociationCorrectionStatus.Blocked;
        }

        if (Matches(propagationStatus, "delayed")
            || disabledReasons.Contains("correction-delayed", StringComparer.Ordinal))
        {
            return AssociationCorrectionStatus.Delayed;
        }

        if (Matches(downstreamImpactStatus, "pending")
            || Matches(downstreamImpactStatus, "correcting")
            || isCorrectedContextStale)
        {
            return AssociationCorrectionStatus.Pending;
        }

        if (Matches(downstreamImpactStatus, "preview-only"))
        {
            return AssociationCorrectionStatus.Partial;
        }

        if (string.IsNullOrWhiteSpace(correctedProjectId))
        {
            return AssociationCorrectionStatus.ProjectionPending;
        }

        // An unrecognized downstream status must not be reported as a completed correction.
        return string.IsNullOrWhiteSpace(downstreamImpactStatus) || Matches(downstreamImpactStatus, "complete")
            ? AssociationCorrectionStatus.Success
            : AssociationCorrectionStatus.Pending;
    }

    private static string FirstApplicable(IReadOnlyList<string> disabledReasons)
        => Array.Find(DisabledReasonPriority, reason => disabledReasons.Contains(reason, StringComparer.Ordinal))
            ?? string.Empty;

    private static bool Matches(string? value, string token)
        => string.Equals(value, token, StringComparison.OrdinalIgnoreCase);
}
