using System.Globalization;
using System.Text.Json;

using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Evaluates the release evidence gate <b>out of process</b>, against the uploaded artifact only.
/// <para>
/// This is the authoritative gate. The live lane's own in-process evaluation runs inside the test that produced the
/// evidence, from an attempt object that test builds — a run grading its own homework. This class runs in a separate
/// workflow job, reads the retained manifests and the run's observation summary from the downloaded artifact, applies
/// the thresholds the <b>release path</b> configures, and evaluates against a fresh wall clock so the staleness and
/// freshness branches are genuinely reachable.
/// </para>
/// </summary>
public sealed class LiveRecoveryEvidenceGateReplayTests
{
    private const string EvidenceDirectoryVariable = "HEXALITH_CHATBOT_RECOVERY_EVIDENCE_DIR";
    private const string RequiredVariable = "HEXALITH_CHATBOT_RECOVERY_EVIDENCE_REQUIRED";
    private const string ExpectedDatasetsVariable = "HEXALITH_CHATBOT_RECOVERY_EXPECTED_DATASETS";
    private const string MaximumEvidenceAgeHoursVariable = "HEXALITH_CHATBOT_RECOVERY_MAX_EVIDENCE_AGE_HOURS";
    private const string ExpectedDatasetVersionVariable = "HEXALITH_CHATBOT_RECOVERY_EXPECTED_DATASET_VERSION";
    private const string MinimumDatasetVolumeVariable = "HEXALITH_CHATBOT_RECOVERY_MINIMUM_DATASET_VOLUME";
    private const string RequiredCommitVariable = "HEXALITH_CHATBOT_RECOVERY_REQUIRED_COMMIT";
    private const string MaximumMeasurableCeilingSecondsVariable = "HEXALITH_CHATBOT_RECOVERY_MAX_MEASURABLE_CEILING_SECONDS";

    [Fact]
    public async Task RetainedLiveRecoveryEvidenceShouldPassTheReleaseGateOutOfProcess()
    {
        string? evidenceDirectory = Environment.GetEnvironmentVariable(EvidenceDirectoryVariable);
        bool required = string.Equals(
            Environment.GetEnvironmentVariable(RequiredVariable),
            "1",
            StringComparison.Ordinal);
        if (required && string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            throw new InvalidOperationException(
                $"The required release evidence gate has no {EvidenceDirectoryVariable} to evaluate.");
        }

        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(evidenceDirectory),
            $"Set {EvidenceDirectoryVariable} to a retained live-recovery evidence directory to replay the release gate.");

        Directory.Exists(evidenceDirectory)
            .ShouldBeTrue($"The retained evidence directory '{evidenceDirectory}' does not exist.");

        JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web);

        // The uploaded artifact roots at TestResults, so the run's own evidence sits under live-recovery/{runId}.
        // Searching recursively also means exactly one summary must be present: two would mean two runs' evidence was
        // merged into one artifact, which is precisely the incoherent input the gate must not be handed.
        string[] summaryPaths = Directory.GetFiles(
            evidenceDirectory!,
            LiveRecoveryValidationAttemptSummary.FileName,
            SearchOption.AllDirectories);
        summaryPaths.Length.ShouldBe(
            1,
            $"Expected exactly one {LiveRecoveryValidationAttemptSummary.FileName} under '{evidenceDirectory}'.");
        string summaryPath = summaryPaths[0];

        LiveRecoveryValidationAttemptSummary summary;
        await using (FileStream summaryStream = File.OpenRead(summaryPath))
        {
            summary = (await JsonSerializer
                .DeserializeAsync<LiveRecoveryValidationAttemptSummary>(
                    summaryStream,
                    serializerOptions,
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true)).ShouldNotBeNull();
        }

        List<RecoveryValidationEvidenceManifest> manifests = [];
        foreach (string manifestFile in Directory.GetFiles(evidenceDirectory!, "*.manifest.json", SearchOption.AllDirectories))
        {
            await using FileStream stream = File.OpenRead(manifestFile);
            RecoveryValidationEvidenceManifest? manifest = await JsonSerializer
                .DeserializeAsync<RecoveryValidationEvidenceManifest>(
                    stream,
                    serializerOptions,
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            manifests.Add(manifest.ShouldNotBeNull());
        }

        manifests.Count.ShouldBeGreaterThan(0, "The retained evidence contains no manifests.");

        LiveRecoveryValidationEvidenceAttempt attempt = new(
            summary.Enabled,
            summary.RunId,
            summary.StartedAtUtc,
            summary.CompletedAtUtc,
            summary.LatestAttemptCompletedSuccessfully,
            manifests,
            summary.AlertsDeliveredByJob);

        LiveRecoveryValidationGatePolicy policy = required
            ? LiveRecoveryValidationGatePolicy.ForRelease(
                ExpectedDatasets(required),
                targetDeviationsBlockRelease: true,
                RecoveryValidationEvidenceManifest.LiveDriverMode,
                MaximumEvidenceAge(),
                expectedDatasetVersion: RequiredValue(ExpectedDatasetVersionVariable),
                minimumDatasetVolume: (int)PositiveNumber(MinimumDatasetVolumeVariable, unset: null),
                maximumMeasurableRecoveryCeilingSeconds: PositiveNumber(MaximumMeasurableCeilingSecondsVariable, unset: null),
                requiredRepositoryCommit: Optional(RequiredCommitVariable))
            : new LiveRecoveryValidationGatePolicy(
                ExpectedDatasets(required),
                TargetDeviationsBlockRelease: true,
                RecoveryValidationEvidenceManifest.LiveDriverMode,
                MaximumEvidenceAge(),
                ExpectedDatasetVersion: Optional(ExpectedDatasetVersionVariable),
                MinimumDatasetVolume: (int)PositiveNumber(MinimumDatasetVolumeVariable, unset: 0),
                RequiredRepositoryCommit: Optional(RequiredCommitVariable),
                MaximumMeasurableRecoveryCeilingSeconds: PositiveNumber(MaximumMeasurableCeilingSecondsVariable, unset: 0));

        LiveRecoveryValidationEvidenceGateDecision decision = LiveRecoveryValidationEvidenceGate.Evaluate(
            attempt,
            policy,
            DateTimeOffset.UtcNow);

        decision.IsStopShip.ShouldBeFalse(
            $"The release evidence gate rejected the retained evidence: {string.Join(", ", decision.StopShipReasons)}.");

        // Not failures. They are the limits on what this pass may be cited as evidence for, and they must be visible in
        // the job log rather than inferred by a reader of the manifest.
        foreach (string limitation in decision.ClaimLimitationReasons)
        {
            TestContext.Current.TestOutputHelper?.WriteLine($"claim limitation: {limitation}");
        }
    }

    private static IReadOnlyList<string> ExpectedDatasets(bool required)
    {
        string? raw = Environment.GetEnvironmentVariable(ExpectedDatasetsVariable);
        string configured = string.IsNullOrWhiteSpace(raw) ? "recovery-baseline" : raw.Trim();
        string[] datasets = [.. configured.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        if (datasets.Length == 0)
        {
            if (required)
            {
                throw new InvalidOperationException(
                    $"{ExpectedDatasetsVariable} resolved to an empty dataset list for a required gate run.");
            }

            return ["recovery-baseline"];
        }

        return datasets;
    }

    /// <summary>
    /// Reads the configured maximum evidence age. A malformed value <b>throws</b> rather than falling back: silently
    /// applying the 8-day default meant a typo in a tightened setting loosened the required gate's staleness bound with
    /// no signal anywhere.
    /// </summary>
    private static TimeSpan MaximumEvidenceAge()
    {
        string? configured = Environment.GetEnvironmentVariable(MaximumEvidenceAgeHoursVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            return TimeSpan.FromDays(8);
        }

        return double.TryParse(configured, NumberStyles.Float, CultureInfo.InvariantCulture, out double hours) && hours > 0
            ? TimeSpan.FromHours(hours)
            : throw new InvalidOperationException(
                $"{MaximumEvidenceAgeHoursVariable} is set to '{configured}', which is not a positive number of hours.");
    }

    /// <summary>Returns a configured value, or <see langword="null"/> when the release path leaves it unpinned.</summary>
    private static string? Optional(string variable)
    {
        string? configured = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(configured) ? null : configured.Trim();
    }

    /// <summary>
    /// Reads a variable that a REQUIRED gate run must pin. Fails closed rather than calling
    /// <see cref="LiveRecoveryValidationGatePolicy.ForRelease"/> with a <see langword="null"/> anchor: an unset
    /// required env var is a broken required-gate invocation, not a legitimate "leave it unpinned" choice.
    /// </summary>
    private static string RequiredValue(string variable)
    {
        string? configured = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(configured)
            ? throw new InvalidOperationException($"{variable} must be set when the release evidence gate is required.")
            : configured.Trim();
    }

    /// <summary>
    /// Reads a positive numeric policy input. When <paramref name="unset"/> is <see langword="null"/>, an unset
    /// variable fails closed (required gate runs). When unset is a number, that value is used for optional local runs.
    /// </summary>
    private static double PositiveNumber(string variable, double? unset)
    {
        string? configured = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            return unset ?? throw new InvalidOperationException(
                $"{variable} must be a positive number when the release evidence gate is required.");
        }

        return double.TryParse(configured, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && value > 0
            ? value
            : throw new InvalidOperationException($"{variable} is set to '{configured}', which is not a positive number.");
    }
}
