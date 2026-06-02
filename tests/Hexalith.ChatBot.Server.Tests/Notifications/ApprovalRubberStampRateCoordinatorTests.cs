using System.Linq;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Notifications;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Notifications;

public sealed class ApprovalRubberStampRateCoordinatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly ISystemClock Clock = new FixedClock(Now);
    private const string Tenant = "tenant-alpha";
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    [Fact]
    public async Task FiredRevisitWritesExactlyOneMetadataOnlyEnvelopeWithRubberStampTokens()
    {
        RecordingAuditWriter audit = new();
        ApprovalRubberStampRateCoordinator coordinator = new(audit, Clock);

        // 4 of 20 rubber-stamp = 20 % > 15 % → fires.
        ApprovalRubberStampRateOutcome outcome = await coordinator.EvaluateAndRecordAsync(
            Mix(rubberStamp: 4, slow: 16, reviewer: "reviewer-a"),
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        outcome.Evaluated.ShouldBe(1);
        outcome.Triggered.ShouldBe(1);
        outcome.AuditUnavailable.ShouldBe(0);

        AuditEnvelope envelope = audit.Envelopes.ShouldHaveSingleItem();
        envelope.TenantId.ShouldBe(Tenant);
        envelope.CorrelationId.ShouldBe(Correlation);
        envelope.SourceEvidenceRefs.ShouldContain("admin-operation:approval-tuning-revisit-triggered");
        envelope.SourceEvidenceRefs.ShouldContain("risk-class:approval-required");
        envelope.SourceEvidenceRefs.ShouldContain("rubber-stamp-count:4");
        envelope.SourceEvidenceRefs.ShouldContain("approval-total:20");
        envelope.SourceEvidenceRefs.ShouldContain("rubber-stamp-rate-permille:200");
        envelope.SourceEvidenceRefs.ShouldContain("rubber-stamp-latency-seconds:5");
        envelope.SourceEvidenceRefs.ShouldContain("fatigue-fraction-percent:15");
        envelope.SourceEvidenceRefs.ShouldContain("rolling-window-days:7");
        envelope.SourceEvidenceRefs.ShouldContain("reviewer-rubber-stamp:reviewer-a:4:20");
        envelope.StateTransition.ShouldBe("Observed->TuningRevisitTriggered");
        envelope.Outcome.ShouldBe("revisit-triggered");
    }

    [Fact]
    public async Task AuditUnavailableFailsClosedAndRecordsNothing()
    {
        RecordingAuditWriter audit = new() { PreCommitResult = AuditWriteResult.Unavailable() };
        ApprovalRubberStampRateCoordinator coordinator = new(audit, Clock);

        ApprovalRubberStampRateOutcome outcome = await coordinator.EvaluateAndRecordAsync(
            Mix(rubberStamp: 4, slow: 16, reviewer: "reviewer-a"),
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        outcome.Evaluated.ShouldBe(1);
        outcome.Triggered.ShouldBe(0);
        outcome.AuditUnavailable.ShouldBe(1);
        // The envelope was attempted (recorded by the writer) but the side-effect count stays zero — fail closed.
        outcome.Triggered.ShouldBe(0);
    }

    [Fact]
    public async Task NoTenantCrossingWritesNoEnvelope()
    {
        RecordingAuditWriter audit = new();
        ApprovalRubberStampRateCoordinator coordinator = new(audit, Clock);

        // Exactly 15 % (3 of 20) does not cross.
        ApprovalRubberStampRateOutcome outcome = await coordinator.EvaluateAndRecordAsync(
            Mix(rubberStamp: 3, slow: 17, reviewer: "reviewer-a"),
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        outcome.Evaluated.ShouldBe(1);
        outcome.Triggered.ShouldBe(0);
        outcome.AuditUnavailable.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordedEnvelopeLeaksNoProjectProposalOrPii()
    {
        RecordingAuditWriter audit = new();
        ApprovalRubberStampRateCoordinator coordinator = new(audit, Clock);

        await coordinator.EvaluateAndRecordAsync(
            Mix(rubberStamp: 4, slow: 16, reviewer: "reviewer-a"),
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        string json = JsonSerializer.Serialize(audit.Envelopes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("project-", Case.Insensitive);
        json.ShouldNotContain("proposal-", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("@");
        json.ShouldNotContain("address", Case.Insensitive);
        // The rubber-stamp / fatigue / rolling-window tokens are present.
        json.ShouldContain("rubber-stamp-count");
        json.ShouldContain("fatigue-fraction-percent");
        json.ShouldContain("rolling-window-days");
    }

    [Fact]
    public async Task FiredEnvelopeCarriesEveryReviewerBreakdownAndWorkerPostCommitMetadata()
    {
        RecordingAuditWriter audit = new();
        ApprovalRubberStampRateCoordinator coordinator = new(audit, Clock);

        // reviewer-a: 3 rubber-stamp / 3; reviewer-b: 1 rubber-stamp / 17 → tenant 4 of 20 = 20 % > 15 % fires.
        List<ApprovalDecisionSample> decisions =
        [
            .. Latencies("reviewer-a", rubberStamp: 3, slow: 0),
            .. Latencies("reviewer-b", rubberStamp: 1, slow: 16),
        ];

        await coordinator.EvaluateAndRecordAsync(decisions, Tenant, Correlation, TestContext.Current.CancellationToken);

        AuditEnvelope envelope = audit.Envelopes.ShouldHaveSingleItem();
        // Every reviewer's exact breakdown is carried (deterministic reviewer order).
        envelope.SourceEvidenceRefs.ShouldContain("reviewer-rubber-stamp:reviewer-a:3:3");
        envelope.SourceEvidenceRefs.ShouldContain("reviewer-rubber-stamp:reviewer-b:1:17");
        // Structural metadata: fail-closed post-commit worker envelope, metadata-only redaction, stable FR41 reason code.
        envelope.Phase.ShouldBe(AuditCommitPhase.PostCommit);
        envelope.SurfaceOrigin.ShouldBe("worker");
        envelope.RedactionDecision.ShouldBe("metadata_only");
        envelope.ReasonCode.ShouldBe(ApprovalRubberStampRateEvaluator.TuningRevisitReasonCode);
    }

    [Fact]
    public async Task UnsafeReviewerRefIsDroppedFromEnvelopeButStillCountedInTenantAggregate()
    {
        RecordingAuditWriter audit = new();
        ApprovalRubberStampRateCoordinator coordinator = new(audit, Clock);

        // An unsafe reviewer ref (embedded space + "secret" marker) must never reach the audit envelope, yet its
        // decisions still count toward the tenant aggregate that fires the FR41 revisit (4 of 20 = 20 %).
        const string unsafeReviewer = "reviewer secret-9";
        ApprovalRubberStampRateOutcome outcome = await coordinator.EvaluateAndRecordAsync(
            Mix(rubberStamp: 4, slow: 16, reviewer: unsafeReviewer),
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        outcome.Triggered.ShouldBe(1);
        AuditEnvelope envelope = audit.Envelopes.ShouldHaveSingleItem();
        // Tenant aggregate still recorded.
        envelope.SourceEvidenceRefs.ShouldContain("rubber-stamp-count:4");
        envelope.SourceEvidenceRefs.ShouldContain("approval-total:20");
        // No per-reviewer ref leaks the unsafe token.
        envelope.SourceEvidenceRefs.ShouldNotContain(static r => r.StartsWith("reviewer-rubber-stamp:", StringComparison.Ordinal));
        string json = JsonSerializer.Serialize(audit.Envelopes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("secret", Case.Insensitive);
    }

    private static List<ApprovalDecisionSample> Latencies(string? reviewer, int rubberStamp, int slow)
    {
        DateTimeOffset decidedAt = Now.AddHours(-1);
        List<ApprovalDecisionSample> decisions = [];
        for (int i = 0; i < rubberStamp; i++)
        {
            decisions.Add(new ApprovalDecisionSample(
                Tenant, reviewer, decidedAt.AddSeconds(-1), decidedAt,
                ApprovalDecisionKind.Approve, AiActionRiskClass.ApprovalRequired));
        }

        for (int i = 0; i < slow; i++)
        {
            decisions.Add(new ApprovalDecisionSample(
                Tenant, reviewer, decidedAt.AddSeconds(-60), decidedAt,
                ApprovalDecisionKind.Approve, AiActionRiskClass.ApprovalRequired));
        }

        return decisions;
    }

    private static List<ApprovalDecisionSample> Mix(int rubberStamp, int slow, string? reviewer)
    {
        DateTimeOffset decidedAt = Now.AddHours(-1);
        List<ApprovalDecisionSample> decisions = [];
        for (int i = 0; i < rubberStamp; i++)
        {
            decisions.Add(new ApprovalDecisionSample(
                Tenant, reviewer, decidedAt.AddSeconds(-1), decidedAt,
                ApprovalDecisionKind.Approve, AiActionRiskClass.ApprovalRequired));
        }

        for (int i = 0; i < slow; i++)
        {
            decisions.Add(new ApprovalDecisionSample(
                Tenant, reviewer, decidedAt.AddSeconds(-60), decidedAt,
                ApprovalDecisionKind.Approve, AiActionRiskClass.ApprovalRequired));
        }

        return decisions;
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class RecordingAuditWriter : IAuditWriter
    {
        public List<AuditEnvelope> Envelopes { get; } = [];

        public AuditWriteResult PreCommitResult { get; init; } = AuditWriteResult.Success;

        public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            Envelopes.Add(envelope);
            return ValueTask.FromResult(PreCommitResult);
        }

        public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            Envelopes.Add(envelope);
            return ValueTask.FromResult(AuditWriteResult.Success);
        }
    }
}
