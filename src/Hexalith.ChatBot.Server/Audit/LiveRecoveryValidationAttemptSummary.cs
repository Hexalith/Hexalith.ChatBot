namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The metadata-only observations a live-recovery run retains alongside its manifests so the release evidence gate can
/// be evaluated in a <b>separate process</b> from the run that produced the evidence.
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
    /// <summary>The canonical file name this summary is retained under, beside the run's manifests.</summary>
    public const string FileName = "attempt.summary.json";

    public required bool Enabled { get; init; }

    public required string RunId { get; init; }

    public required DateTimeOffset StartedAtUtc { get; init; }

    public required DateTimeOffset? CompletedAtUtc { get; init; }

    public required bool LatestAttemptCompletedSuccessfully { get; init; }

    public required IReadOnlyDictionary<string, int> AlertsDeliveredByJob { get; init; }
}
