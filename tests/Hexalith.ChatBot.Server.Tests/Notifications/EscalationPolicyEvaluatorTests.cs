using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Notifications;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Notifications;

public sealed class EscalationPolicyEvaluatorTests
{
    private static readonly ISystemClock Clock = new FixedClock(new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ItemOverAgeThresholdShouldEscalate()
    {
        IReadOnlyList<EscalationDelivery> escalations = Evaluate(
            FailurePolicy(ageThresholdSeconds: 3600, severity: EscalationSeverity.High),
            Queue(Item(OperationalQueueFamily.FailedIngestion, ageSeconds: 7200, risk: "low")));

        EscalationDelivery escalation = escalations.ShouldHaveSingleItem();
        escalation.BreachReason.ShouldBe(EscalationBreachReason.AgeThreshold);
        escalation.Notification.StateClass.ShouldBe(NotificationStateClass.Failure);
    }

    [Fact]
    public void ItemUnderBothThresholdsShouldNotEscalate()
    {
        Evaluate(
            FailurePolicy(ageThresholdSeconds: 3600, severity: EscalationSeverity.High),
            Queue(Item(OperationalQueueFamily.FailedIngestion, ageSeconds: 60, risk: "low")))
            .ShouldBeEmpty();
    }

    [Fact]
    public void ItemMeetingSeverityThresholdShouldEscalateRegardlessOfAge()
    {
        IReadOnlyList<EscalationDelivery> escalations = Evaluate(
            FailurePolicy(ageThresholdSeconds: 3600, severity: EscalationSeverity.High),
            Queue(Item(OperationalQueueFamily.FailedIngestion, ageSeconds: 1, risk: "high")));

        EscalationDelivery escalation = escalations.ShouldHaveSingleItem();
        escalation.BreachReason.ShouldBe(EscalationBreachReason.SeverityThreshold);
        escalation.Severity.ShouldBe(EscalationSeverity.High);
    }

    [Fact]
    public void TerminalAndResolvedItemsShouldNeverEscalate()
    {
        // Even far over both thresholds, terminal/resolved items are excluded.
        Evaluate(
            FailurePolicy(ageThresholdSeconds: 1, severity: EscalationSeverity.Low),
            Queue(Item(OperationalQueueFamily.FailedIngestion, ageSeconds: 999999, risk: "high", isTerminal: true)))
            .ShouldBeEmpty();

        Evaluate(
            FailurePolicy(ageThresholdSeconds: 1, severity: EscalationSeverity.Low),
            Queue(Item(OperationalQueueFamily.FailedIngestion, ageSeconds: 999999, risk: "high", status: "resolved")))
            .ShouldBeEmpty();

        Evaluate(
            FailurePolicy(ageThresholdSeconds: 1, severity: EscalationSeverity.Low),
            Queue(Item(OperationalQueueFamily.FailedIngestion, ageSeconds: 999999, risk: "high", status: "Rejected")))
            .ShouldBeEmpty();
    }

    [Fact]
    public void AgeThresholdShouldBeStrictlyGreaterAtTheBoundary()
    {
        // age == threshold does NOT breach (strictly-greater); severity below threshold; so no escalation.
        Evaluate(
            FailurePolicy(ageThresholdSeconds: 3600, severity: EscalationSeverity.High),
            Queue(Item(OperationalQueueFamily.FailedIngestion, ageSeconds: 3600, risk: "low")))
            .ShouldBeEmpty();

        // age == threshold + 1 breaches.
        Evaluate(
            FailurePolicy(ageThresholdSeconds: 3600, severity: EscalationSeverity.High),
            Queue(Item(OperationalQueueFamily.FailedIngestion, ageSeconds: 3601, risk: "low")))
            .ShouldHaveSingleItem();
    }

    [Fact]
    public void SeverityThresholdShouldBeAtOrAboveAtTheBoundary()
    {
        // severity == threshold breaches (at-or-above), independent of age.
        Evaluate(
            FailurePolicy(ageThresholdSeconds: 999999, severity: EscalationSeverity.Medium),
            Queue(Item(OperationalQueueFamily.FailedIngestion, ageSeconds: 1, risk: "medium")))
            .ShouldHaveSingleItem();
    }

    [Fact]
    public void EscalationShouldRouteToTheConfiguredTargetViaTheRoutingEngine()
    {
        IReadOnlyList<EscalationDelivery> escalations = Evaluate(
            FailurePolicy(ageThresholdSeconds: 3600, severity: EscalationSeverity.High, targetRole: AdminRole.OperationsAdmin, channel: NotificationChannel.OperatorAlert),
            Queue(Item(OperationalQueueFamily.FailedIngestion, ageSeconds: 7200, risk: "low")),
            Candidate("operator-001", "operations-admin"));

        NotificationDelivery delivery = escalations.ShouldHaveSingleItem().Notification;
        delivery.RecipientRole.ShouldBe(AdminRole.OperationsAdmin);
        delivery.Channel.ShouldBe(NotificationChannel.OperatorAlert);
        delivery.RecipientRef.ShouldBe("operator-001");
        delivery.ReasonCode.ShouldBe(EscalationPolicyEvaluator.AgeBreachReasonCode);
    }

    [Fact]
    public void UnauthorizedTargetShouldReceiveRedactedFormWithNoExistenceLeakage()
    {
        IReadOnlyList<EscalationDelivery> escalations = Evaluate(
            FailurePolicy(ageThresholdSeconds: 3600, severity: EscalationSeverity.High),
            [new EscalationQueueItem(Item(OperationalQueueFamily.FailedIngestion, ageSeconds: 7200, risk: "high"), ItemProjectRef: "project-x")],
            Candidate("operator-owner", "operations-admin", projectRef: "project-x"),
            Candidate("operator-blind", "operations-admin"));

        escalations.Count.ShouldBe(2);

        EscalationDelivery authorized = escalations.Single(e => e.Notification.RecipientRef == "operator-owner");
        authorized.Notification.Visibility.ShouldBe(NotificationContentVisibility.ItemContext);
        authorized.Notification.ItemRef.ShouldBe("item-77");

        EscalationDelivery redacted = escalations.Single(e => e.Notification.RecipientRef == "operator-blind");
        redacted.Notification.Visibility.ShouldBe(NotificationContentVisibility.MetadataRedacted);
        redacted.Notification.ItemRef.ShouldBeNull();

        // No resource-existence leakage: the redacted escalation must be indistinguishable from safe-not-found.
        string json = JsonSerializer.Serialize(redacted, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("item-77");
        json.ShouldNotContain("project-x");
    }

    [Fact]
    public void SchemaInvalidPolicyShouldProduceNoEscalationsFailClosed()
    {
        // A policy with a `retry` entry is schema-invalid; the evaluator fires nothing (fail-closed).
        EscalationPolicyChangeSet invalid = new(
        [
            new EscalationPolicyEntry(NotificationStateClass.Retry, AdminScope.Operate, 1, EscalationSeverity.Low, AdminRole.OperationsAdmin, NotificationChannel.InApp),
        ]);

        Evaluate(invalid, Queue(Item(OperationalQueueFamily.FailedIngestion, ageSeconds: 999999, risk: "high")))
            .ShouldBeEmpty();
    }

    [Fact]
    public void AgeShouldBeServerMeasuredFromTheInjectedClockNotItemSuppliedTime()
    {
        // Item claims AgeSeconds 0, but the server-side freshness timestamp is 2h before "now": the clock-measured age wins.
        AdminQueueSummaryProjectionItem item = Item(OperationalQueueFamily.FailedIngestion, ageSeconds: 0, risk: "low") with
        {
            FreshnessTimestampUtc = Clock.UtcNow.AddSeconds(-7200),
        };

        Evaluate(
            FailurePolicy(ageThresholdSeconds: 3600, severity: EscalationSeverity.High),
            [new EscalationQueueItem(item)])
            .ShouldHaveSingleItem();
    }

    [Fact]
    public void RetryableHealthyItemsShouldNeverEscalate()
    {
        // A retryable-operation item maps to the transient `retry` class, which is never an escalatable entry.
        Evaluate(
            FailurePolicy(ageThresholdSeconds: 1, severity: EscalationSeverity.Low),
            Queue(Item(OperationalQueueFamily.RetryableOperation, ageSeconds: 999999, risk: "high", health: ChatBotHealthStatus.Healthy)))
            .ShouldBeEmpty();
    }

    [Fact]
    public void PendingApprovalItemShouldEscalateAgainstTheApprovalPendingEntry()
    {
        IReadOnlyList<EscalationDelivery> escalations = Evaluate(
            ClassPolicy(NotificationStateClass.ApprovalPending, ageThresholdSeconds: 3600, severity: EscalationSeverity.High),
            Queue(Item(OperationalQueueFamily.PendingApproval, ageSeconds: 7200, risk: "low")));

        EscalationDelivery escalation = escalations.ShouldHaveSingleItem();
        escalation.Notification.StateClass.ShouldBe(NotificationStateClass.ApprovalPending);
        escalation.BreachReason.ShouldBe(EscalationBreachReason.AgeThreshold);
    }

    [Fact]
    public void AmbiguousAssociationItemShouldEscalateAgainstTheReviewNeededEntry()
    {
        IReadOnlyList<EscalationDelivery> escalations = Evaluate(
            ClassPolicy(NotificationStateClass.ReviewNeeded, ageThresholdSeconds: 999999, severity: EscalationSeverity.Medium),
            Queue(Item(OperationalQueueFamily.AmbiguousAssociation, ageSeconds: 1, risk: "high")));

        EscalationDelivery escalation = escalations.ShouldHaveSingleItem();
        escalation.Notification.StateClass.ShouldBe(NotificationStateClass.ReviewNeeded);
        escalation.BreachReason.ShouldBe(EscalationBreachReason.SeverityThreshold);
    }

    [Fact]
    public void QuarantineSignalShouldDominateTheQueueFamilyMapping()
    {
        // The item's family would otherwise map to ApprovalPending, but an explicit quarantine status dominates,
        // so it escalates against the Quarantine entry (and not an ApprovalPending one).
        IReadOnlyList<EscalationDelivery> escalations = Evaluate(
            new EscalationPolicyChangeSet(
            [
                new EscalationPolicyEntry(NotificationStateClass.ApprovalPending, AdminScope.Operate, 1, EscalationSeverity.Low, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
                new EscalationPolicyEntry(NotificationStateClass.Quarantine, AdminScope.Operate, 3600, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
            ]),
            Queue(Item(OperationalQueueFamily.PendingApproval, ageSeconds: 7200, risk: "low", status: "quarantine-hold")));

        EscalationDelivery escalation = escalations.ShouldHaveSingleItem();
        escalation.Notification.StateClass.ShouldBe(NotificationStateClass.Quarantine);
    }

    [Fact]
    public void DegradedHealthShouldPromoteARetryableItemToTheDegradedEntry()
    {
        // A retryable-family item with degraded health is promoted out of the transient `retry` class to `degraded`,
        // which is an escalatable class.
        IReadOnlyList<EscalationDelivery> escalations = Evaluate(
            ClassPolicy(NotificationStateClass.Degraded, ageThresholdSeconds: 3600, severity: EscalationSeverity.High),
            Queue(Item(OperationalQueueFamily.RetryableOperation, ageSeconds: 7200, risk: "low", health: ChatBotHealthStatus.Degraded)));

        EscalationDelivery escalation = escalations.ShouldHaveSingleItem();
        escalation.Notification.StateClass.ShouldBe(NotificationStateClass.Degraded);
        escalation.BreachReason.ShouldBe(EscalationBreachReason.AgeThreshold);
    }

    private static EscalationPolicyChangeSet ClassPolicy(
        NotificationStateClass stateClass,
        int ageThresholdSeconds,
        EscalationSeverity severity,
        AdminRole targetRole = AdminRole.OperationsAdmin,
        NotificationChannel channel = NotificationChannel.OperatorAlert)
        => new(
        [
            new EscalationPolicyEntry(stateClass, AdminScope.Operate, ageThresholdSeconds, severity, targetRole, channel),
        ]);

    private static IReadOnlyList<EscalationDelivery> Evaluate(
        EscalationPolicyChangeSet policy,
        IReadOnlyList<EscalationQueueItem> items,
        params NotificationRecipientCandidate[] candidates)
        => EscalationPolicyEvaluator.Evaluate(
            items,
            policy,
            candidates.Length == 0 ? [Candidate("operator-001", "operations-admin")] : candidates,
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            Clock);

    private static EscalationPolicyChangeSet FailurePolicy(
        int ageThresholdSeconds,
        EscalationSeverity severity,
        AdminRole targetRole = AdminRole.OperationsAdmin,
        NotificationChannel channel = NotificationChannel.OperatorAlert)
        => new(
        [
            new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, ageThresholdSeconds, severity, targetRole, channel),
        ]);

    private static IReadOnlyList<EscalationQueueItem> Queue(AdminQueueSummaryProjectionItem item)
        => [new EscalationQueueItem(item)];

    private static AdminQueueSummaryProjectionItem Item(
        OperationalQueueFamily family,
        int ageSeconds,
        string risk = "medium",
        bool isTerminal = false,
        string status = "NeedsReview",
        ChatBotHealthStatus health = ChatBotHealthStatus.Degraded)
        => new(
            QueueRef: "queue:operations",
            ItemRef: "item-77",
            Status: status,
            OwnerClass: "operations",
            Health: health,
            AgeSeconds: ageSeconds,
            QueueFamily: family,
            Risk: risk,
            IsTerminal: isTerminal);

    private static NotificationRecipientCandidate Candidate(string recipientRef, string role, string? projectRef = null)
    {
        List<Claim> claims =
        [
            new Claim("sub", recipientRef),
            new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
            new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
        ];
        if (!string.IsNullOrWhiteSpace(projectRef))
        {
            claims.Add(new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, projectRef));
        }

        return new NotificationRecipientCandidate(recipientRef, new ClaimsPrincipal(new ClaimsIdentity(claims, "test")));
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
