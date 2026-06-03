using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Observability;

public sealed class AuditProjectionLagAlertEvaluatorTests
{
    private static readonly DateTimeOffset FiredAt = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ChatBotHealthStatus.Degraded)]
    [InlineData(ChatBotHealthStatus.Failed)]
    public void FiresForDegradedOrFailedWithValidSafeTokens(ChatBotHealthStatus health)
    {
        AuditProjectionLagStatus status = new(health, "lagging", 200, FiredAt);

        OperationalAlertPayload? alert = AuditProjectionLagAlertEvaluator.Evaluate(
            status, "tenant-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAW", FiredAt);

        alert.ShouldNotBeNull();
        alert.AlertKind.ShouldBe(OperatorAlertKind.AuditProjectionLagBreached);
        alert.ReasonCode.ShouldBe("audit_projection_lag_breached");
        alert.OwnerRole.ShouldBe("operations-admin");
        alert.NextSafeAction.ShouldBe("review-audit-projection-lag");
        alert.AffectedScope.ShouldBe("tenant:tenant-alpha");
        OperationalAlertPayload.IsValid(alert).ShouldBeTrue();
    }

    [Theory]
    [InlineData(ChatBotHealthStatus.Healthy)]
    [InlineData(ChatBotHealthStatus.Unknown)]
    public void SuppressesForHealthyOrUnknown(ChatBotHealthStatus health)
    {
        AuditProjectionLagStatus status = new(health, "current", 0, FiredAt);

        AuditProjectionLagAlertEvaluator.Evaluate(status, "tenant-alpha", "corr-1", FiredAt)
            .ShouldBeNull();
    }
}
