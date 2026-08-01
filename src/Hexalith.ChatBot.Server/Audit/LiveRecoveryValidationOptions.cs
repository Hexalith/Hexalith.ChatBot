namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Fail-closed configuration contract for the destructive Story 12.15 live-recovery lane. Product defaults leave the
/// lane disabled; only the Tier-3/AppHost harness binds the controller secret and constructs live drivers.
/// </summary>
internal sealed class LiveRecoveryValidationOptions
{
    /// <summary>The only controller capability accepted by the Aspire Tier-3 harness.</summary>
    public const string AspireControllerCapability = "aspire-resource-commands-v1";

    /// <summary>Gets or sets whether the destructive live-validation lane is explicitly enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the sandbox environment name; live validation is forbidden in Production.</summary>
    public string EnvironmentName { get; set; } = string.Empty;

    /// <summary>Gets or sets the dedicated <c>replay-test:</c> tenant reference.</summary>
    public string TestTenantRef { get; set; } = string.Empty;

    /// <summary>Gets or sets the configured baseline dataset locator.</summary>
    public string DatasetRef { get; set; } = string.Empty;

    /// <summary>Gets or sets the configured baseline dataset version.</summary>
    public string DatasetVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets the exact expected baseline dataset volume.</summary>
    public int DatasetVolume { get; set; }

    /// <summary>Gets or sets the projection schema version expected from the baseline dataset.</summary>
    public string ProjectionSchemaVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets the isolated validation-store partition locator.</summary>
    public string ValidationPartitionRef { get; set; } = string.Empty;

    /// <summary>Gets or sets the closed controller capability token.</summary>
    public string ControllerCapability { get; set; } = string.Empty;

    /// <summary>Gets or sets the workflow-injected controller secret; it must never be retained as evidence.</summary>
    public string ControllerSecret { get; set; } = string.Empty;

    /// <summary>Gets or sets the deadline for one scenario.</summary>
    public TimeSpan PerScenarioTimeout { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets the deadline for restoration and post-restore health.
    /// <para>
    /// This value is also the lane's <b>measurable recovery ceiling</b>: a recovery slower than this budget is
    /// cancelled and converts to <c>unmeasurable</c>, so it can never be reported as a miss of
    /// <see cref="RecoveryTargets.MaxRto"/>. When it is shorter than that target — which is the practical case for a
    /// sandbox lane — the run demonstrates recovery within THIS budget only. The ceiling is published in every
    /// evidence manifest so the narrower claim travels with the evidence.
    /// </para>
    /// </summary>
    public TimeSpan RestorationTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Gets or sets the outer serialized workflow deadline, including topology startup and cleanup margin.</summary>
    public TimeSpan WorkflowTimeout { get; set; } = TimeSpan.FromHours(5);

    /// <summary>
    /// Gets or sets the hard budget the CI runner will kill the job at (the workflow's <c>timeout-minutes</c>).
    /// <para>
    /// <see cref="WorkflowTimeout"/> must be strictly shorter than this. Without the bound, the in-process deadline —
    /// added precisely so a hung scenario fails closed through its <c>finally</c> blocks — can be configured past the
    /// point where the runner kills the process mid-injection, leaving EventStore or Keycloak stopped.
    /// </para>
    /// </summary>
    public TimeSpan RunnerBudget { get; set; } = TimeSpan.FromMinutes(330);

    /// <summary>
    /// Gets the smallest number of scenarios a complete sweep runs: both continuity drills, every canonical scoped
    /// outage, and at least one projection-rebuild dataset.
    /// </summary>
    public static int MinimumSweepScenarioCount
        => ContinuityDrillScenarios.All.Count + ScopedOutageDependencies.All.Count + 1;

    /// <summary>Gets or sets the required workflow cadence.</summary>
    public TimeSpan Cadence { get; set; } = TimeSpan.FromDays(7);

    /// <summary>Gets or sets the absolute directory into which Tier-3 evidence is written.</summary>
    public string EvidenceDirectory { get; set; } = string.Empty;

    /// <summary>Gets or sets the stable CI artifact locator retained in manifests.</summary>
    public string EvidenceLocator { get; set; } = string.Empty;

    /// <summary>Gets or sets the maximum evidence age accepted by the release gate.</summary>
    public TimeSpan MaximumEvidenceAge { get; set; } = TimeSpan.FromDays(8);

    /// <summary>Validates safe defaults and, when enabled, every destructive-lane prerequisite.</summary>
    /// <returns>A stable configuration error, or <see langword="null"/> when valid.</returns>
    public string? Validate()
    {
        if (PerScenarioTimeout <= TimeSpan.Zero)
        {
            return $"{nameof(PerScenarioTimeout)} must be positive.";
        }

        if (RestorationTimeout <= TimeSpan.Zero)
        {
            return $"{nameof(RestorationTimeout)} must be positive.";
        }

        if (Cadence <= TimeSpan.Zero)
        {
            return $"{nameof(Cadence)} must be positive.";
        }

        if (MaximumEvidenceAge <= TimeSpan.Zero)
        {
            return $"{nameof(MaximumEvidenceAge)} must be positive.";
        }

        if (WorkflowTimeout <= TimeSpan.Zero)
        {
            return $"{nameof(WorkflowTimeout)} must be positive.";
        }

        if (!Enabled)
        {
            return null;
        }

        if (string.Equals(EnvironmentName, "Production", StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase) is false &&
             string.Equals(EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase) is false))
        {
            return $"{nameof(EnvironmentName)} must identify a Testing or Development sandbox.";
        }

        if (!ReplayTenantPolicy.IsTestTenant(TestTenantRef))
        {
            return $"{nameof(TestTenantRef)} must use the replay-test: tenant prefix.";
        }

        if (PerScenarioTimeout < RecoveryTargets.MaxRto)
        {
            return $"{nameof(PerScenarioTimeout)} must permit measurement through the {RecoveryTargets.MaxRto} recovery target.";
        }

        if (RestorationTimeout >= PerScenarioTimeout)
        {
            return $"{nameof(RestorationTimeout)} must be shorter than {nameof(PerScenarioTimeout)}.";
        }

        TimeSpan requiredWorkflowTimeout = RecoveryTargets.MaxRto + RestorationTimeout + TimeSpan.FromMinutes(30);
        if (WorkflowTimeout < requiredWorkflowTimeout)
        {
            return $"{nameof(WorkflowTimeout)} must include the recovery target plus topology startup and cleanup margin.";
        }

        if (WorkflowTimeout >= RunnerBudget)
        {
            return $"{nameof(WorkflowTimeout)} must be shorter than {nameof(RunnerBudget)} so the in-process deadline fails closed first.";
        }

        // The per-scenario budget is nominal: the sweep runs at least MinimumSweepScenarioCount scenarios serially
        // inside one WorkflowTimeout, so PerScenarioTimeout is unreachable in aggregate by design. What must hold is
        // weaker but real — every scenario's fair share of the workflow has to at least cover restoration, or the
        // sweep provably cannot finish and the tail scenarios yield no evidence at all.
        TimeSpan fairScenarioShare = WorkflowTimeout / MinimumSweepScenarioCount;
        if (RestorationTimeout >= fairScenarioShare)
        {
            return $"{nameof(RestorationTimeout)} must fit within each scenario's share of {nameof(WorkflowTimeout)} across {MinimumSweepScenarioCount} scenarios.";
        }

        if (MaximumEvidenceAge < Cadence)
        {
            return $"{nameof(MaximumEvidenceAge)} must be at least {nameof(Cadence)}.";
        }

        if (!AuditMetadata.IsSafeStableIdentifier(DatasetRef))
        {
            return $"{nameof(DatasetRef)} must be a safe non-empty metadata locator.";
        }

        if (!AuditMetadata.IsSafeStableIdentifier(DatasetVersion))
        {
            return $"{nameof(DatasetVersion)} must be a safe non-empty metadata token.";
        }

        if (DatasetVolume <= 0)
        {
            return $"{nameof(DatasetVolume)} must be positive.";
        }

        if (!AuditMetadata.IsSafeStableIdentifier(ProjectionSchemaVersion))
        {
            return $"{nameof(ProjectionSchemaVersion)} must be a safe non-empty metadata token.";
        }

        if (!AuditMetadata.IsSafeStableIdentifier(ValidationPartitionRef))
        {
            return $"{nameof(ValidationPartitionRef)} must identify an isolated validation-store partition.";
        }

        if (!string.Equals(ControllerCapability, AspireControllerCapability, StringComparison.Ordinal))
        {
            return $"{nameof(ControllerCapability)} must be the closed {AspireControllerCapability} capability.";
        }

        if (string.IsNullOrWhiteSpace(ControllerSecret))
        {
            return $"{nameof(ControllerSecret)} must be supplied by the Tier-3 workflow.";
        }

        if (string.IsNullOrWhiteSpace(EvidenceDirectory) || !Path.IsPathFullyQualified(EvidenceDirectory))
        {
            return $"{nameof(EvidenceDirectory)} must be an absolute path.";
        }

        return RecoveryValidationEvidenceManifest.IsSafeArtifactLocator(EvidenceLocator)
            ? null
            : $"{nameof(EvidenceLocator)} must be a safe artifact locator.";
    }
}
