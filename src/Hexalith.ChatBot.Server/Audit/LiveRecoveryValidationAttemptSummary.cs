namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The metadata-only observations a live-recovery run retains so the release evidence gate can be evaluated in a
/// <b>separate process</b> from the run that produced the evidence. It is written to the workflow-owned
/// retention-failure root, not beside the manifests: total loss of the canonical evidence directory is precisely the
/// case the gate must still be able to classify, and a summary retained inside that directory would be lost with it.
/// <para>
/// Manifests alone are not sufficient input for the gate: alert delivery counts and whether the sweep completed are
/// facts only the run can observe, and neither is derivable from the manifest set. They are therefore written to the
/// artifact as observations. Everything the gate <i>judges</i> them by — targets, expected coverage, blocking policy,
/// required driver mode, freshness — lives in <see cref="LiveRecoveryValidationGatePolicy"/> and comes from the
/// release path instead.
/// </para>
/// </summary>
internal sealed record LiveRecoveryValidationAttemptSummary
{
    /// <summary>The canonical file name this summary is retained under, inside the independent marker root.</summary>
    public const string FileName = "attempt.summary.json";

    public required bool Enabled { get; init; }

    public required string RunId { get; init; }

    public required DateTimeOffset StartedAtUtc { get; init; }

    public required DateTimeOffset? CompletedAtUtc { get; init; }

    public required bool LatestAttemptCompletedSuccessfully { get; init; }

    public required IReadOnlyDictionary<string, int> AlertsDeliveredByJob { get; init; }
}
