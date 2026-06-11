using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Notifications;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Notifications;

public sealed class OperationalAlertWiringCoordinatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);
    private static readonly ISystemClock Clock = new FixedClock(Now);
    private const string Tenant = "tenant-alpha";
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    [Fact]
    public async Task AllFiveEvaluatorsFireAuditPreCommitAndDeliverToOwnerRole()
    {
        RecordingAuditWriter audit = new();
        InMemoryNotificationSink sink = new();
        OperationalAlertWiringCoordinator coordinator = BuildCoordinator(audit, sink, withSignals: true);

        OperationalAlertOutcome outcome = await coordinator.EvaluateAndDeliverAsync(
            QueueSnapshot(),
            [Candidate("ops-1", "operations-admin"), Candidate("mbx-1", "mailbox-admin"), Candidate("tnt-1", "tenant-admin")],
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        // lag + retry + approval-queue + subscription + auth-spike = 5 fired alerts, each audited pre-commit and
        // delivered exactly once to its owner role.
        outcome.Fired.ShouldBe(5);
        outcome.Delivered.ShouldBe(5);
        outcome.AuditUnavailable.ShouldBe(0);
        audit.Envelopes.Count.ShouldBe(5);
        sink.Deliveries.Count.ShouldBe(5);

        audit.Envelopes.ShouldAllBe(e => e.SourceEvidenceRefs.Contains("admin-operation:operational-alert-fired"));
        audit.Envelopes.ShouldAllBe(e => e.Phase == AuditCommitPhase.PreCommit);
        audit.Envelopes.ShouldAllBe(e => e.StateTransition == "Open->Alerted");
        audit.Envelopes.ShouldAllBe(e => e.TenantId == Tenant);

        // Every delivered payload is a well-formed, tenant-safe, metadata-only alert with no restricted content.
        string json = JsonSerializer.Serialize(audit.Envelopes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("@");
        json.ShouldNotContain("project-", Case.Insensitive);
    }

    [Fact]
    public async Task AllFiveAlertsRouteOnlyToExpectedHumanOwnerRolesAsMetadataRedactedDeliveries()
    {
        RecordingAuditWriter audit = new();
        InMemoryNotificationSink sink = new();
        OperationalAlertWiringCoordinator coordinator = BuildCoordinator(audit, sink, withSignals: true);

        OperationalAlertOutcome outcome = await coordinator.EvaluateAndDeliverAsync(
            QueueSnapshot(),
            [
                Candidate("ops-1", "operations-admin"),
                Candidate("mbx-1", "mailbox-admin"),
                Candidate("tnt-1", "tenant-admin"),
                Candidate("policy-1", "policy-admin"),
            ],
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        outcome.Fired.ShouldBe(5);
        outcome.Delivered.ShouldBe(5);

        IReadOnlyDictionary<string, NotificationDelivery> deliveries = sink.Deliveries.ToDictionary(static d => d.ReasonCode);
        deliveries.Keys.ShouldBe(
            [
                "audit_projection_lag_breached",
                "retry_exhaustion_threshold_exceeded",
                "approval_queue_age_threshold_exceeded",
                "subscription_expiry_threshold_exceeded",
                "authorization_failure_spike_detected",
            ],
            ignoreOrder: true);

        AssertDelivery(
            deliveries["audit_projection_lag_breached"],
            NotificationStateClass.Degraded,
            AdminRole.OperationsAdmin,
            "ops-1");
        AssertDelivery(
            deliveries["retry_exhaustion_threshold_exceeded"],
            NotificationStateClass.Retry,
            AdminRole.OperationsAdmin,
            "ops-1");
        AssertDelivery(
            deliveries["approval_queue_age_threshold_exceeded"],
            NotificationStateClass.ApprovalPending,
            AdminRole.OperationsAdmin,
            "ops-1");
        AssertDelivery(
            deliveries["subscription_expiry_threshold_exceeded"],
            NotificationStateClass.Degraded,
            AdminRole.MailboxAdmin,
            "mbx-1");
        AssertDelivery(
            deliveries["authorization_failure_spike_detected"],
            NotificationStateClass.Degraded,
            AdminRole.TenantAdmin,
            "tnt-1");

        sink.Deliveries.ShouldNotContain(static d => d.RecipientRef == "policy-1");
        string json = JsonSerializer.Serialize(sink.Deliveries, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("project-", Case.Insensitive);
        json.ShouldNotContain("TopSecret", Case.Insensitive);
        json.ShouldNotContain("@");
        json.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task AuditUnavailableFailsClosedAndDeliversNothing()
    {
        RecordingAuditWriter audit = new() { PreCommitResult = AuditWriteResult.Unavailable() };
        InMemoryNotificationSink sink = new();
        OperationalAlertWiringCoordinator coordinator = BuildCoordinator(audit, sink, withSignals: true);

        OperationalAlertOutcome outcome = await coordinator.EvaluateAndDeliverAsync(
            QueueSnapshot(),
            [Candidate("ops-1", "operations-admin"), Candidate("mbx-1", "mailbox-admin"), Candidate("tnt-1", "tenant-admin")],
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        outcome.Fired.ShouldBe(5);
        outcome.Delivered.ShouldBe(0);
        outcome.AuditUnavailable.ShouldBe(5);
        sink.Deliveries.ShouldBeEmpty();
    }

    [Fact]
    public async Task NoSignalsProducesNoAlertsAndNoAudit()
    {
        RecordingAuditWriter audit = new();
        InMemoryNotificationSink sink = new();
        OperationalAlertWiringCoordinator coordinator = BuildCoordinator(audit, sink, withSignals: false);

        OperationalAlertOutcome outcome = await coordinator.EvaluateAndDeliverAsync(
            [],
            [Candidate("ops-1", "operations-admin")],
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        outcome.Fired.ShouldBe(0);
        outcome.Delivered.ShouldBe(0);
        outcome.AuditUnavailable.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        sink.Deliveries.ShouldBeEmpty();
    }

    [Fact]
    public async Task NonHumanPrincipalCannotReceiveDeliveryButAlertsStillAudit()
    {
        RecordingAuditWriter audit = new();
        InMemoryNotificationSink sink = new();
        OperationalAlertWiringCoordinator coordinator = BuildCoordinator(audit, sink, withSignals: true);

        OperationalAlertOutcome outcome = await coordinator.EvaluateAndDeliverAsync(
            QueueSnapshot(),
            [NonHumanCandidate("svc-1", "operations-admin")],
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        outcome.Fired.ShouldBe(5);
        outcome.Delivered.ShouldBe(0);
        outcome.AuditUnavailable.ShouldBe(0);
        sink.Deliveries.ShouldBeEmpty();
        audit.Envelopes.Count.ShouldBe(5);
    }

    [Fact]
    public async Task UnscopedHumanPrincipalCannotReceiveDeliveryButAlertsStillAudit()
    {
        RecordingAuditWriter audit = new();
        InMemoryNotificationSink sink = new();
        OperationalAlertWiringCoordinator coordinator = BuildCoordinator(audit, sink, withSignals: true);

        OperationalAlertOutcome outcome = await coordinator.EvaluateAndDeliverAsync(
            QueueSnapshot(),
            [UnscopedCandidate("ops-1")],
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        outcome.Fired.ShouldBe(5);
        outcome.Delivered.ShouldBe(0);
        outcome.AuditUnavailable.ShouldBe(0);
        sink.Deliveries.ShouldBeEmpty();
        audit.Envelopes.Count.ShouldBe(5);
    }

    private static OperationalAlertWiringCoordinator BuildCoordinator(
        IAuditWriter audit,
        INotificationSink sink,
        bool withSignals)
    {
        FakeLagSource lagSource = new(withSignals
            ? [new AuditProjectionLagReading(Tenant, 0, 200, Now)]
            : []);
        InMemoryRetryExhaustionAlertSource retrySource = new();
        InMemoryAuthorizationFailureCounter authCounter = new(Clock);
        if (withSignals)
        {
            retrySource.Signal(Tenant);
            for (int i = 0; i < AuthorizationFailureSpikeEvaluator.DefaultAuthFailureBaselineCount + 1; i++)
            {
                authCounter.Record(Tenant, Now);
            }
        }

        return new OperationalAlertWiringCoordinator(sink, audit, lagSource, retrySource, authCounter, Clock);
    }

    private static List<AdminQueueSummaryProjectionItem> QueueSnapshot()
        =>
        [
            new AdminQueueSummaryProjectionItem(
                QueueRef: "queue:approvals",
                ItemRef: "i-approval-1",
                Status: "pending",
                OwnerClass: "operations",
                Health: ChatBotHealthStatus.Degraded,
                AgeSeconds: ApprovalQueueAgeAlertEvaluator.BusinessDayAlertThresholdSeconds + 1,
                QueueFamily: OperationalQueueFamily.PendingApproval),
            new AdminQueueSummaryProjectionItem(
                QueueRef: "queue:ingestion",
                ItemRef: "i-ingestion-1",
                Status: "failed",
                OwnerClass: "mailbox",
                Health: ChatBotHealthStatus.Degraded,
                AgeSeconds: 10,
                QueueFamily: OperationalQueueFamily.FailedIngestion,
                MailboxRef: "mb-1",
                FailureState: "graph-subscription-expired"),
        ];

    private static NotificationRecipientCandidate Candidate(string recipientRef, string role)
        => BuildCandidate(recipientRef, role, ParticipantAuthorizationStage.HumanActorValue);

    private static NotificationRecipientCandidate NonHumanCandidate(string recipientRef, string role)
        => BuildCandidate(recipientRef, role, "service");

    private static NotificationRecipientCandidate UnscopedCandidate(string recipientRef)
    {
        List<Claim> claims =
        [
            new Claim("sub", recipientRef),
            new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
        ];
        return new NotificationRecipientCandidate(recipientRef, new ClaimsPrincipal(new ClaimsIdentity(claims, "test")));
    }

    private static NotificationRecipientCandidate BuildCandidate(string recipientRef, string role, string actorType)
    {
        List<Claim> claims =
        [
            new Claim("sub", recipientRef),
            new Claim(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
            new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
        ];
        return new NotificationRecipientCandidate(recipientRef, new ClaimsPrincipal(new ClaimsIdentity(claims, "test")));
    }

    private static void AssertDelivery(
        NotificationDelivery delivery,
        NotificationStateClass stateClass,
        AdminRole role,
        string recipientRef)
    {
        delivery.StateClass.ShouldBe(stateClass);
        delivery.Channel.ShouldBe(NotificationChannel.InApp);
        delivery.RecipientRole.ShouldBe(role);
        delivery.Scope.ShouldBe(AdminScope.SeeOnly);
        delivery.RecipientRef.ShouldBe(recipientRef);
        delivery.TenantRef.ShouldBe(Tenant);
        delivery.ItemRef.ShouldBeNull();
        delivery.QueueRef.ShouldBe("queue:operational-alerts");
        delivery.CorrelationId.ShouldBe(Correlation);
        delivery.Visibility.ShouldBe(NotificationContentVisibility.MetadataRedacted);
        delivery.RaisedAtUtc.ShouldBe(Now);
    }

    private sealed class FakeLagSource(IReadOnlyList<AuditProjectionLagReading> readings) : IAuditProjectionLagSource
    {
        public IReadOnlyList<AuditProjectionLagReading> ReadCurrent() => readings;
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
