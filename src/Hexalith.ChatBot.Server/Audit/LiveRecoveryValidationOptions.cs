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

    /// <summary>
    /// Gets or sets the exact configured baseline-corpus volume. Evidence manifests report this separately from the
    /// number of corpus records or resources each scenario actually exercised.
    /// </summary>
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
    /// Gets the smallest number of scenarios a complete sweep runs: both continuity drills, the separate controlled
    /// loss path, every canonical scoped outage, and at least one projection-rebuild dataset.
    /// </summary>
    public static int MinimumSweepScenarioCount
        => ContinuityDrillScenarios.All.Count + ScopedOutageDependencies.All.Count + 2;

    /// <summary>Topology startup and cleanup margin reserved inside <see cref="WorkflowTimeout"/>.</summary>
    private static readonly TimeSpan TopologyMargin = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Guards the serial-budget arithmetic below. <c>PerScenarioTimeout * MinimumSweepScenarioCount</c> overflows for
    /// values near <see cref="TimeSpan.MaxValue"/>, and a validator that throws reports a stack trace instead of a
    /// configuration error.
    /// </summary>
    private static readonly TimeSpan MaximumTimeSpan = TimeSpan.MaxValue;

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

        if (RestorationTimeout >= PerScenarioTimeout)
        {
            return $"{nameof(RestorationTimeout)} must be shorter than {nameof(PerScenarioTimeout)}.";
        }

        if (WorkflowTimeout >= RunnerBudget)
        {
            return $"{nameof(WorkflowTimeout)} must be shorter than {nameof(RunnerBudget)} so the in-process deadline fails closed first.";
        }

        // The sweep runs at least MinimumSweepScenarioCount scenarios SERIALLY inside one WorkflowTimeout, so a
        // per-scenario budget the workflow cannot afford is not a budget — it is silently truncated by the outer
        // deadline, and the tail scenarios yield no evidence at all.
        //
        // This replaces the former `PerScenarioTimeout >= RecoveryTargets.MaxRto` rule, which demanded a 4-hour
        // per-scenario budget that nine serial scenarios can never afford inside a sub-RunnerBudget workflow. That
        // rule read as "the lane can measure through the recovery target" while being arithmetically unsatisfiable;
        // RestorationTimeout, published per manifest as MeasurableRecoveryCeilingSeconds, is the honest statement of
        // what the lane can actually measure, and the gate discloses any target above it as a claim limitation.
        if (PerScenarioTimeout > MaximumTimeSpan / MinimumSweepScenarioCount)
        {
            return $"{nameof(WorkflowTimeout)} must cover {MinimumSweepScenarioCount} serial {nameof(PerScenarioTimeout)} budgets plus topology startup and cleanup margin.";
        }

        try
        {
            TimeSpan serialBudget = (PerScenarioTimeout * MinimumSweepScenarioCount) + TopologyMargin;
            if (WorkflowTimeout < serialBudget)
            {
                return $"{nameof(WorkflowTimeout)} must cover {MinimumSweepScenarioCount} serial {nameof(PerScenarioTimeout)} budgets plus topology startup and cleanup margin.";
            }
        }
        catch (OverflowException)
        {
            return $"{nameof(WorkflowTimeout)} serial budget overflows {nameof(TimeSpan)}.";
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
