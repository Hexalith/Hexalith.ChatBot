using System.Net;
using System.Text.Json;

using Hexalith.ChatBot.RecoverySandbox;
using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Always-run contracts for the live Tier-3 startup boundary.</summary>
public sealed class LiveContinuityAspireE2eContractTests
{
    [Theory]
    [InlineData(RecoveryNotificationIdentity.PreFaultPhase)]
    [InlineData(RecoveryNotificationIdentity.LossPhase)]
    [InlineData(RecoveryNotificationIdentity.PostRecoveryPhase)]
    public void ControlledLossRouteUsesOnlyClosedNotificationPhases(string phase)
    {
        string identity = RecoveryNotificationIdentity.Compose(
            "provider-message",
            RecoveryNotificationIdentity.ControlledLossLane,
            phase);

        identity.ShouldBe($"provider-message-controlled-loss-{phase}");
    }

    [Fact]
    public void ControlledLossRouteRejectsOpenEndedNotificationPhase()
        => Should.Throw<InvalidOperationException>(() => RecoveryNotificationIdentity.Compose(
            "provider-message",
            RecoveryNotificationIdentity.ControlledLossLane,
            "caller-supplied"));

    [Theory]
    [InlineData(RecoveryNotificationIdentity.ContinuityLane, RecoveryNotificationIdentity.PreFaultPhase)]
    [InlineData(RecoveryNotificationIdentity.ContinuityLane, RecoveryNotificationIdentity.LossPhase)]
    [InlineData(RecoveryNotificationIdentity.ContinuityLane, RecoveryNotificationIdentity.PostRecoveryPhase)]
    [InlineData(RecoveryNotificationIdentity.GraphLane, RecoveryNotificationIdentity.PreFaultPhase)]
    [InlineData(RecoveryNotificationIdentity.GraphLane, RecoveryNotificationIdentity.LossPhase)]
    [InlineData(RecoveryNotificationIdentity.GraphLane, RecoveryNotificationIdentity.PostRecoveryPhase)]
    [InlineData(RecoveryNotificationIdentity.ControlledLossLane, RecoveryNotificationIdentity.CheckpointPhase)]
    [InlineData(RecoveryNotificationIdentity.ControlledLossLane, RecoveryNotificationIdentity.RecoveryPhase)]
    public void NotificationPhasesCannotCrossClosedLaneOwnership(string lane, string phase)
        => Should.Throw<InvalidOperationException>(() => RecoveryNotificationIdentity.Compose(
            "provider-message",
            lane,
            phase));

    [Theory]
    [InlineData(null, 300)]
    [InlineData("265", 265)]
    [InlineData(" 265 ", 265)]
    [InlineData("1", 1)]
    [InlineData("300", 300)]
    public void RecoveryWorkflowTimeoutHonorsTheBoundedCompletionOverride(string? configured, int expectedMinutes)
    {
        LiveContinuityAspireE2eTests.RecoveryWorkflowTimeout(configured)
            .ShouldBe(TimeSpan.FromMinutes(expectedMinutes));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("301")]
    [InlineData("not-a-number")]
    public void RecoveryWorkflowTimeoutRejectsValuesOutsideTheRunnerBudget(string configured)
    {
        Should.Throw<InvalidOperationException>(() =>
            LiveContinuityAspireE2eTests.RecoveryWorkflowTimeout(configured));
    }

    [Fact]
    public void HostedControlledLossBudgetCanMeasurePastRpoTargetWithinSmallestWorkflowWindow()
    {
        LiveRecoveryValidationOptions options = new()
        {
            Enabled = true,
            EnvironmentName = "Testing",
            TestTenantRef = "replay-test:recovery-validation",
            DatasetRef = "recovery-baseline",
            DatasetVersion = "v1",
            DatasetVolume = 6,
            ProjectionSchemaVersion = "schema-v1",
            ValidationPartitionRef = "recovery-partition-v1",
            ControllerCapability = LiveRecoveryValidationOptions.AspireControllerCapability,
            ControllerSecret = "test-secret",
            PerScenarioTimeout = TimeSpan.FromMinutes(20),
            RestorationTimeout = TimeSpan.FromMinutes(3),
            WorkflowTimeout = TimeSpan.FromMinutes(250),
            EvidenceDirectory = Path.GetTempPath(),
            EvidenceLocator = "artifact://live-recovery/test",
        };

        options.PerScenarioTimeout.ShouldBeGreaterThan(RecoveryTargets.MaxRpo);
        options.Validate().ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ControlledRunnerOrSinkFailureWritesFourthJobMarkerBeforeRethrow(bool sinkFails)
    {
        InvalidOperationException primary = new(sinkFails ? "sink failed" : "runner failed");
        CapturingRetentionFailureSink markerSink = new();

        InvalidOperationException observed = await Should.ThrowAsync<InvalidOperationException>(() =>
            LiveContinuityAspireE2eTests.RunControlledLossAndRetainAsync(
                sinkFails
                    ? _ => ValueTask.FromResult<ControlledLossPathReport>(null!)
                    : _ => ValueTask.FromException<ControlledLossPathReport>(primary),
                (_, _) => sinkFails ? ValueTask.FromException(primary) : ValueTask.CompletedTask,
                markerSink,
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                TimeSpan.FromSeconds(1),
                TestContext.Current.CancellationToken).AsTask());

        observed.ShouldBeSameAs(primary);
        RecoveryValidationEvidenceRetentionFailureMarker marker = markerSink.Markers.ShouldHaveSingleItem();
        marker.JobId.ShouldBe(LiveRecoveryValidationJobs.ControlledLossPath);
        marker.Scenario.ShouldBe(ControlledLossPathReport.SubscriptionNotificationRejectionScenario);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.UnprocessableEntity, false)]

    // The three statuses the enumeration exists for. Without them, restoring the `>= 500` catch-all passed all
    // nine original rows, so the change's whole point was asserted only by prose.
    [InlineData(HttpStatusCode.NotImplemented, false)]
    [InlineData(HttpStatusCode.HttpVersionNotSupported, false)]
    [InlineData(HttpStatusCode.InsufficientStorage, true)]
    public void MailboxAdmissionStartupStatusClassificationIsFailClosed(
        HttpStatusCode statusCode,
        bool expectedTransient)
    {
        LiveContinuityAspireE2eTests.IsTransientMailboxAdmissionStatus(statusCode).ShouldBe(expectedTransient);
    }

    /// <summary>
    /// The admission proof is the problem <c>type</c>. <c>dispatch-unavailable</c> is emitted only from
    /// <c>CommandGateway</c>'s accepted branch, so it proves admission; <c>audit-unavailable</c> (the pre-commit
    /// denial), an authorization denial, a body that is not a problem document, and a matching <c>code</c> under a
    /// different <c>type</c> must not.
    /// </summary>
    /// <param name="problemDetails">The verbatim response body.</param>
    /// <param name="expected">Whether the body proves the caller was admitted.</param>
    [Theory]
    [InlineData(
        "{\"type\":\"https://hexalith.dev/errors/chatbot/dispatch-unavailable\",\"status\":503,\"code\":\"audit_unavailable\"}",
        true)]
    [InlineData(
        "{\"type\":\"https://hexalith.dev/errors/chatbot/audit-unavailable\",\"status\":503,\"code\":\"audit_unavailable\"}",
        false)]
    [InlineData(
        "{\"type\":\"https://hexalith.dev/errors/chatbot/authorization-denied\",\"status\":403,\"code\":\"authorization_denied\"}",
        false)]
    [InlineData("{\"code\":\"audit_unavailable\"}", false)]
    [InlineData("{\"type\":\"https://hexalith.dev/errors/chatbot/dispatch-unavailable-x\"}", false)]
    [InlineData("[\"dispatch-unavailable\"]", false)]
    [InlineData("not json at all", false)]
    [InlineData("", false)]
    public void MailboxAdmissionProofRequiresTheDispatchUnavailableProblemType(string problemDetails, bool expected)
        => LiveContinuityAspireE2eTests.IsDispatchUnavailableProblem(problemDetails).ShouldBe(expected);

    /// <summary>
    /// The admission proof requires BOTH the dispatch-unavailable problem type AND a 503; the body predicate alone
    /// would accept that document under any status, which is not what the production check does.
    /// </summary>
    /// <param name="statusCode">The status observed alongside the dispatch-unavailable body.</param>
    /// <param name="expectedAdmissionProof">Whether that pair proves admission.</param>
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.BadGateway, false)]
    public void MailboxAdmissionProofRequiresServiceUnavailableAlongsideTheProblemType(
        HttpStatusCode statusCode,
        bool expectedAdmissionProof)
    {
        const string body =
            "{\"type\":\"https://hexalith.dev/errors/chatbot/dispatch-unavailable\",\"status\":503}";

        // Drives the PRODUCTION predicate. Recomputing the conjunction inline here asserted the test against its
        // own re-implementation: deleting the ServiceUnavailable half of the production check left this green.
        LiveContinuityAspireE2eTests
            .ProvesMailboxAdmission(statusCode, body)
            .ShouldBe(expectedAdmissionProof);
    }

    /// <summary>
    /// The stage's failure path is what tells the out-of-process gate the hosted attempt was incomplete. Its
    /// destination directory and its false completion flag were previously asserted only by source-text matches, so
    /// writing the summary into the canonical evidence directory, or leaving the flag true, would have surfaced
    /// nowhere but a hosted run.
    /// </summary>
    [Fact]
    public async Task ControlledLossStageFailureRetainsAnIncompleteAttemptSummaryBesideTheMarker()
    {
        InvalidOperationException primary = new("runner failed");
        CapturingRetentionFailureSink markerSink = new();
        string retentionFailureDirectory = Path.Combine(
            Path.GetTempPath(),
            $"hexalith-chatbot-controlled-loss-summary-{Guid.NewGuid():N}");
        string evidenceDirectory = Path.Combine(retentionFailureDirectory, "evidence");
        Directory.CreateDirectory(evidenceDirectory);
        DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5);
        try
        {
            InvalidOperationException observed = await Should.ThrowAsync<InvalidOperationException>(() =>
                LiveContinuityAspireE2eTests.RunControlledLossStageAsync(
                    _ => ValueTask.FromException<ControlledLossPathReport>(primary),
                    (_, _) => ValueTask.CompletedTask,
                    markerSink,
                    retentionFailureDirectory,
                    enabled: true,
                    "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                    startedAtUtc,
                    continuityAlertsDelivered: 2,
                    TimeSpan.FromSeconds(5),
                    TestContext.Current.CancellationToken).AsTask());

            observed.ShouldBeSameAs(primary);
            markerSink.Markers.ShouldHaveSingleItem();

            // The summary must land in the retention-failure root the replay loader reads, never in the evidence
            // directory the manifests occupy.
            string summaryPath = Path.Combine(
                retentionFailureDirectory,
                LiveRecoveryValidationAttemptSummary.FileName);
            File.Exists(summaryPath).ShouldBeTrue();
            File.Exists(Path.Combine(evidenceDirectory, LiveRecoveryValidationAttemptSummary.FileName))
                .ShouldBeFalse();

            LiveRecoveryValidationAttemptSummary summary = JsonSerializer.Deserialize<LiveRecoveryValidationAttemptSummary>(
                await File.ReadAllTextAsync(summaryPath, TestContext.Current.CancellationToken),
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            summary.LatestAttemptCompletedSuccessfully.ShouldBeFalse();
            summary.Enabled.ShouldBeTrue();
            summary.RunId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
            summary.StartedAtUtc.ShouldBe(startedAtUtc, TimeSpan.FromSeconds(1));
            summary.CompletedAtUtc.ShouldNotBeNull();
            summary.AlertsDeliveredByJob[LiveRecoveryValidationJobs.Continuity].ShouldBe(2);
            foreach (string jobId in LiveRecoveryValidationJobs.All)
            {
                summary.AlertsDeliveredByJob.ShouldContainKey(jobId);
            }
        }
        finally
        {
            if (Directory.Exists(retentionFailureDirectory))
            {
                Directory.Delete(retentionFailureDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    /// A successful stage must not leave an incomplete-attempt summary behind: the success path writes its own
    /// summary later, and a stray failed one would stop-ship an otherwise complete hosted attempt.
    /// </summary>
    [Fact]
    public async Task ControlledLossStageSuccessWritesNoIncompleteAttemptSummary()
    {
        CapturingRetentionFailureSink markerSink = new();
        string retentionFailureDirectory = Path.Combine(
            Path.GetTempPath(),
            $"hexalith-chatbot-controlled-loss-summary-{Guid.NewGuid():N}");
        try
        {
            ControlledLossPathReport returned = await LiveContinuityAspireE2eTests.RunControlledLossStageAsync(
                _ => ValueTask.FromResult<ControlledLossPathReport>(null!),
                (_, _) => ValueTask.CompletedTask,
                markerSink,
                retentionFailureDirectory,
                enabled: true,
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                DateTimeOffset.UtcNow,
                continuityAlertsDelivered: 0,
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken);

            returned.ShouldBeNull();
            markerSink.Markers.ShouldBeEmpty();
            File.Exists(Path.Combine(retentionFailureDirectory, LiveRecoveryValidationAttemptSummary.FileName))
                .ShouldBeFalse();
        }
        finally
        {
            if (Directory.Exists(retentionFailureDirectory))
            {
                Directory.Delete(retentionFailureDirectory, recursive: true);
            }
        }
    }

    private sealed class CapturingRetentionFailureSink : IRecoveryValidationEvidenceRetentionFailureSink
    {
        public List<RecoveryValidationEvidenceRetentionFailureMarker> Markers { get; } = [];

        public ValueTask RecordAsync(
            RecoveryValidationEvidenceRetentionFailureMarker marker,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Markers.Add(marker);
            return ValueTask.CompletedTask;
        }
    }
}
