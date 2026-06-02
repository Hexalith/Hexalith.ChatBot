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

public sealed class EscalationEvaluationCoordinatorTests
{
    private static readonly ISystemClock Clock = new FixedClock(new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task FiredEscalationShouldEmitMetadataOnlyAuditWithCorrelationContextAndDeliver()
    {
        RecordingAuditWriter audit = new();
        InMemoryNotificationSink sink = new();
        EscalationEvaluationCoordinator coordinator = new(sink, audit, Clock);

        EscalationEvaluationOutcome outcome = await coordinator.EvaluateAndDeliverAsync(
            [new EscalationQueueItem(BreachingItem())],
            FailurePolicy(),
            [Candidate("operator-001", "operations-admin")],
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        outcome.Fired.ShouldBe(1);
        outcome.Delivered.ShouldBe(1);
        outcome.AuditUnavailable.ShouldBe(0);
        sink.Deliveries.Count.ShouldBe(1);

        // Each fired escalation emits its own metadata-only audit record carrying the item's correlation context (FR59).
        AuditEnvelope envelope = audit.Envelopes.ShouldHaveSingleItem();
        envelope.TenantId.ShouldBe("tenant-alpha");
        envelope.CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        envelope.SourceEvidenceRefs.ShouldContain("admin-operation:escalation-fired");
        envelope.SourceEvidenceRefs.ShouldContain("correlation:01ARZ3NDEKTSV4RRFFQ69G5FAW");
        envelope.SourceEvidenceRefs.ShouldContain("escalation-state-class:failure");
        envelope.SourceEvidenceRefs.ShouldContain("escalation-target-role:operations-admin");
        envelope.SourceEvidenceRefs.ShouldContain("escalation-channel:operator-alert");

        string json = JsonSerializer.Serialize(audit.Envelopes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("@");
        json.ShouldNotContain("address", Case.Insensitive);
    }

    [Fact]
    public async Task AuditUnavailableShouldFailClosedAndDeliverNothing()
    {
        RecordingAuditWriter audit = new() { PreCommitResult = AuditWriteResult.Unavailable() };
        InMemoryNotificationSink sink = new();
        EscalationEvaluationCoordinator coordinator = new(sink, audit, Clock);

        EscalationEvaluationOutcome outcome = await coordinator.EvaluateAndDeliverAsync(
            [new EscalationQueueItem(BreachingItem())],
            FailurePolicy(),
            [Candidate("operator-001", "operations-admin")],
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        outcome.Fired.ShouldBe(1);
        outcome.Delivered.ShouldBe(0);
        outcome.AuditUnavailable.ShouldBe(1);
        sink.Deliveries.ShouldBeEmpty();
    }

    private static EscalationPolicyChangeSet FailurePolicy()
        => new(
        [
            new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, 3600, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
        ]);

    private static AdminQueueSummaryProjectionItem BreachingItem()
        => new(
            QueueRef: "queue:operations",
            ItemRef: "item-77",
            Status: "NeedsReview",
            OwnerClass: "operations",
            Health: ChatBotHealthStatus.Failed,
            AgeSeconds: 7200,
            QueueFamily: OperationalQueueFamily.FailedIngestion,
            Risk: "high");

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
