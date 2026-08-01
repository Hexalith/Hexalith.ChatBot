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

        LiveRecoveryValidationGatePolicy policy = new(
            ExpectedDatasets(),
            TargetDeviationsBlockRelease: true,
            RecoveryValidationEvidenceManifest.LiveDriverMode,
            MaximumEvidenceAge());

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

    private static IReadOnlyList<string> ExpectedDatasets()
    {
        string configured = Environment.GetEnvironmentVariable(ExpectedDatasetsVariable) ?? "recovery-baseline";
        return [.. configured.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

    private static TimeSpan MaximumEvidenceAge()
    {
        string? configured = Environment.GetEnvironmentVariable(MaximumEvidenceAgeHoursVariable);
        return double.TryParse(configured, NumberStyles.Float, CultureInfo.InvariantCulture, out double hours) && hours > 0
            ? TimeSpan.FromHours(hours)
            : TimeSpan.FromDays(8);
    }
}
