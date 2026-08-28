namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Bounded metadata-only proof that both attempts to retain one canonical live-recovery report failed. This marker is
/// diagnostic only: it never substitutes for recovery evidence and never changes a canonical report verdict.
/// </summary>
/// <param name="SchemaVersion">The fixed marker schema version.</param>
/// <param name="Kind">The fixed marker kind.</param>
/// <param name="RunId">The canonical live-recovery run ULID.</param>
/// <param name="JobId">The closed live-recovery job token.</param>
/// <param name="Scenario">The closed scenario token for the job.</param>
/// <param name="FailedAtUtc">The UTC instant at which both evidence writes had failed.</param>
/// <param name="ReasonCode">The fixed retention-failure reason code.</param>
internal sealed record RecoveryValidationEvidenceRetentionFailureMarker(
    string SchemaVersion,
    string Kind,
    string RunId,
    string JobId,
    string Scenario,
    DateTimeOffset FailedAtUtc,
    string ReasonCode)
{
    /// <summary>The only supported marker schema.</summary>
    public const string CurrentSchemaVersion = "chatbot.recovery-retention-failure.v1";

    /// <summary>The only supported marker kind.</summary>
    public const string RetentionFailureKind = "evidence-retention-failure";

    /// <summary>The stable reason code proving both evidence writes failed.</summary>
    public const string EvidenceRetentionFailedReasonCode = "evidence_retention_failed";

    /// <summary>The fixed projection-rebuild scenario token; dataset references are never copied into markers.</summary>
    public const string ProjectionRebuildScenario = "projection-rebuild";

    /// <summary>The maximum serialized UTF-8 marker size accepted by the file side channel.</summary>
    public const int MaximumSerializedBytes = 1_024;

    /// <summary>Creates a marker using only the closed metadata vocabulary.</summary>
    public static RecoveryValidationEvidenceRetentionFailureMarker Create(
        string runId,
        string jobId,
        string scenario,
        DateTimeOffset failedAtUtc)
        => new(
            CurrentSchemaVersion,
            RetentionFailureKind,
            runId,
            jobId,
            scenario,
            failedAtUtc.ToUniversalTime(),
            EvidenceRetentionFailedReasonCode);

    /// <summary>Returns whether every field is bounded and belongs to the closed marker vocabulary.</summary>
    public bool IsValid()
        => string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal) &&
            string.Equals(Kind, RetentionFailureKind, StringComparison.Ordinal) &&
            RecoveryValidationEvidenceManifest.IsCanonicalUlid(RunId) &&
            LiveRecoveryValidationJobs.All.Contains(JobId) &&
            IsClosedScenario(JobId, Scenario) &&
            FailedAtUtc.Offset == TimeSpan.Zero &&
            string.Equals(ReasonCode, EvidenceRetentionFailedReasonCode, StringComparison.Ordinal);

    /// <summary>Returns whether <paramref name="scenario"/> is a closed token for <paramref name="jobId"/>.</summary>
    internal static bool IsClosedScenario(string jobId, string scenario)
        => jobId switch
        {
            LiveRecoveryValidationJobs.Continuity => ContinuityDrillScenarios.Contains(scenario),
            LiveRecoveryValidationJobs.ControlledLossPath => string.Equals(
                scenario,
                ControlledLossPathReport.SubscriptionNotificationRejectionScenario,
                StringComparison.Ordinal),
            LiveRecoveryValidationJobs.ProjectionRebuild =>
                string.Equals(scenario, ProjectionRebuildScenario, StringComparison.Ordinal),
            LiveRecoveryValidationJobs.ScopedOutage => ScopedOutageDependencies.Contains(scenario),
            _ => false,
        };
}
