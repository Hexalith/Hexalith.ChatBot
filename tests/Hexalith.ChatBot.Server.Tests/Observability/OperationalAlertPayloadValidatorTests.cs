using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Observability;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Observability;

public sealed class OperationalAlertPayloadValidatorTests
{
    private static readonly DateTimeOffset FiredAt = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AcceptsAllFiveAlertKindPayloads()
    {
        List<OperationalAlertPayload> payloads =
        [
            Payload(
                OperatorAlertKind.AuditProjectionLagBreached,
                "tenant:tenant-alpha",
                AuditProjectionLagAlertEvaluator.OwnerRole,
                AuditProjectionLagAlertEvaluator.NextSafeAction,
                AuditProjectionLagAlertEvaluator.ReasonCode),
            Payload(
                OperatorAlertKind.RetryExhausted,
                "tenant:tenant-alpha",
                RetryExhaustionAlertEvaluator.OwnerRole,
                RetryExhaustionAlertEvaluator.NextSafeAction,
                RetryExhaustionAlertEvaluator.ReasonCode),
            Payload(
                OperatorAlertKind.ApprovalQueueAgeBreached,
                "tenant:tenant-alpha",
                ApprovalQueueAgeAlertEvaluator.OwnerRole,
                ApprovalQueueAgeAlertEvaluator.NextSafeAction,
                ApprovalQueueAgeAlertEvaluator.ReasonCode),
            Payload(
                OperatorAlertKind.SubscriptionExpiryImminent,
                "tenant:tenant-alpha mailbox:mb-1",
                SubscriptionExpiryAlertEvaluator.OwnerRole,
                SubscriptionExpiryAlertEvaluator.NextSafeAction,
                SubscriptionExpiryAlertEvaluator.ReasonCode),
            Payload(
                OperatorAlertKind.AuthorizationFailureSpike,
                "tenant:tenant-alpha",
                AuthorizationFailureSpikeEvaluator.OwnerRole,
                AuthorizationFailureSpikeEvaluator.NextSafeAction,
                AuthorizationFailureSpikeEvaluator.ReasonCode),
        ];

        foreach (OperationalAlertPayload payload in payloads)
        {
            OperationalAlertPayload.Validate(payload).ShouldBeEmpty();
            OperationalAlertPayload.IsValid(payload).ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData("secret")]
    [InlineData("password")]
    [InlineData("bearer")]
    [InlineData("token")]
    [InlineData("exception")]
    [InlineData("file.txt")]
    [InlineData("data.json")]
    [InlineData("data.xml")]
    public void RejectsMarkerBannedAffectedScope(string banned)
    {
        OperationalAlertPayload payload = Valid() with { AffectedScope = $"tenant:{banned}" };

        OperationalAlertPayload.Validate(payload).ShouldContain("affected_scope_invalid");
    }

    [Fact]
    public void RejectsMarkerBannedReasonCode()
        => OperationalAlertPayload.Validate(Valid() with { ReasonCode = "secret_reason" })
            .ShouldContain("reason_code_invalid");

    [Fact]
    public void RejectsHighCardinalityValue()
    {
        string longProjectName = new('a', 201);
        OperationalAlertPayload payload = Valid() with { TenantRef = longProjectName };

        OperationalAlertPayload.Validate(payload).ShouldContain("tenant_ref_invalid");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsEmptyOrWhitespaceFields(string blank)
    {
        OperationalAlertPayload.Validate(Valid() with { OwnerRole = blank })
            .ShouldContain("owner_role_invalid");
        OperationalAlertPayload.Validate(Valid() with { AffectedScope = blank })
            .ShouldContain("affected_scope_invalid");
        OperationalAlertPayload.Validate(Valid() with { CorrelationId = blank })
            .ShouldContain("correlation_id_invalid");
    }

    [Fact]
    public void RejectsNonUtcFiredAt()
        => OperationalAlertPayload.Validate(Valid() with { FiredAtUtc = new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.FromHours(2)) })
            .ShouldContain("fired_at_not_utc");

    private static OperationalAlertPayload Valid()
        => Payload(
            OperatorAlertKind.AuditProjectionLagBreached,
            "tenant:tenant-alpha",
            "operations-admin",
            "review-audit-projection-lag",
            "audit_projection_lag_breached");

    private static OperationalAlertPayload Payload(
        OperatorAlertKind kind,
        string affectedScope,
        string ownerRole,
        string nextSafeAction,
        string reasonCode)
        => new(kind, affectedScope, ownerRole, nextSafeAction, reasonCode, "tenant-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAW", FiredAt);
}
