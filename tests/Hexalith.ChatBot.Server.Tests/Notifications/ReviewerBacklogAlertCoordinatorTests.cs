using System.Linq;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Notifications;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Notifications;

public sealed class ReviewerBacklogAlertCoordinatorTests
{
    private static readonly ISystemClock Clock = new FixedClock(new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task FiredAlertEmitsExactlyOneMetadataOnlyEnvelopeWithBacklogTokensAndDelivers()
    {
        RecordingAuditWriter audit = new();
        InMemoryNotificationSink sink = new();
        ReviewerBacklogAlertCoordinator coordinator = new(sink, audit, Clock);

        ReviewerBacklogAlertOutcome outcome = await coordinator.EvaluateAndDeliverAsync(
            BacklogItems("reviewer-a", 26, ageSeconds: 4200),
            [Candidate("admin-001", "tenant-admin")],
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ReviewerBacklogThreshold.SafeDefault,
            TestContext.Current.CancellationToken);

        outcome.Fired.ShouldBe(1);
        outcome.Delivered.ShouldBe(1);
        outcome.AuditUnavailable.ShouldBe(0);
        sink.Deliveries.Count.ShouldBe(1);

        // Exactly one metadata-only envelope per fired alert, carrying the three reviewer-attention signals.
        AuditEnvelope envelope = audit.Envelopes.ShouldHaveSingleItem();
        envelope.TenantId.ShouldBe("tenant-alpha");
        envelope.CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        envelope.SourceEvidenceRefs.ShouldContain("admin-operation:reviewer-backlog-alert-fired");
        envelope.SourceEvidenceRefs.ShouldContain("notification-state-class:approval-pending");
        envelope.SourceEvidenceRefs.ShouldContain("notification-channel:in-app");
        envelope.SourceEvidenceRefs.ShouldContain("admin-scope:see-only");
        envelope.SourceEvidenceRefs.ShouldContain("recipient-role:tenant-admin");
        envelope.SourceEvidenceRefs.ShouldContain("backlog-depth:26");
        envelope.SourceEvidenceRefs.ShouldContain("backlog-oldest-age-seconds:4200");
        envelope.SourceEvidenceRefs.ShouldContain("backlog-threshold:25");
        envelope.SourceEvidenceRefs.ShouldContain("reviewer:reviewer-a");

        // The aggregate alert never carries a per-resource item ref (NFR2 — indistinguishable from safe-not-found).
        envelope.SourceEvidenceRefs.ShouldNotContain(r => r.StartsWith("notification-item:", StringComparison.Ordinal));

        string json = JsonSerializer.Serialize(audit.Envelopes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("@");
        json.ShouldNotContain("address", Case.Insensitive);
        json.ShouldNotContain("project-", Case.Insensitive);
    }

    [Fact]
    public async Task AuditUnavailableFailsClosedAndDeliversNothing()
    {
        RecordingAuditWriter audit = new() { PreCommitResult = AuditWriteResult.Unavailable() };
        InMemoryNotificationSink sink = new();
        ReviewerBacklogAlertCoordinator coordinator = new(sink, audit, Clock);

        ReviewerBacklogAlertOutcome outcome = await coordinator.EvaluateAndDeliverAsync(
            BacklogItems("reviewer-a", 26),
            [Candidate("admin-001", "tenant-admin")],
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ReviewerBacklogThreshold.SafeDefault,
            TestContext.Current.CancellationToken);

        outcome.Fired.ShouldBe(1);
        outcome.Delivered.ShouldBe(0);
        outcome.AuditUnavailable.ShouldBe(1);
        sink.Deliveries.ShouldBeEmpty();
    }

    [Fact]
    public async Task NoBacklogOverThresholdProducesNoAlertAndNoAudit()
    {
        RecordingAuditWriter audit = new();
        InMemoryNotificationSink sink = new();
        ReviewerBacklogAlertCoordinator coordinator = new(sink, audit, Clock);

        ReviewerBacklogAlertOutcome outcome = await coordinator.EvaluateAndDeliverAsync(
            BacklogItems("reviewer-a", 25),
            [Candidate("admin-001", "tenant-admin")],
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ReviewerBacklogThreshold.SafeDefault,
            TestContext.Current.CancellationToken);

        outcome.Fired.ShouldBe(0);
        outcome.Delivered.ShouldBe(0);
        sink.Deliveries.ShouldBeEmpty();
        audit.Envelopes.ShouldBeEmpty();
    }

    [Fact]
    public async Task MultipleFiredAlertsEmitExactlyOneMetadataOnlyEnvelopeEach()
    {
        RecordingAuditWriter audit = new();
        InMemoryNotificationSink sink = new();
        ReviewerBacklogAlertCoordinator coordinator = new(sink, audit, Clock);

        List<AdminQueueSummaryProjectionItem> items =
        [
            .. BacklogItems("reviewer-a", 26),
            .. BacklogItems("reviewer-b", 30),
        ];

        ReviewerBacklogAlertOutcome outcome = await coordinator.EvaluateAndDeliverAsync(
            items,
            [Candidate("admin-001", "tenant-admin")],
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ReviewerBacklogThreshold.SafeDefault,
            TestContext.Current.CancellationToken);

        // Two reviewers over threshold → two alerts, exactly one metadata-only envelope and one delivery per fired alert.
        outcome.Fired.ShouldBe(2);
        outcome.Delivered.ShouldBe(2);
        outcome.AuditUnavailable.ShouldBe(0);
        audit.Envelopes.Count.ShouldBe(2);
        sink.Deliveries.Count.ShouldBe(2);
        audit.Envelopes.ShouldAllBe(e => e.SourceEvidenceRefs.Contains("admin-operation:reviewer-backlog-alert-fired"));
    }

    private static List<AdminQueueSummaryProjectionItem> BacklogItems(string reviewer, int count, int ageSeconds = 100)
        => Enumerable.Range(0, count).Select(i => new AdminQueueSummaryProjectionItem(
            QueueRef: "queue:approvals",
            ItemRef: $"i-{reviewer}-{i}",
            Status: "pending",
            OwnerClass: "operations",
            Health: ChatBotHealthStatus.Degraded,
            AgeSeconds: ageSeconds,
            QueueFamily: OperationalQueueFamily.PendingApproval,
            AssigneeRef: reviewer)).ToList();

    private static NotificationRecipientCandidate Candidate(string recipientRef, string role)
    {
        List<Claim> claims =
        [
            new Claim("sub", recipientRef),
            new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
            new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
        ];
        return new NotificationRecipientCandidate(recipientRef, new ClaimsPrincipal(new ClaimsIdentity(claims, "test")));
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
