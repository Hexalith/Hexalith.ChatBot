using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Observability;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Observability;

public sealed class RetryExhaustionAlertEvaluatorTests
{
    private static readonly DateTimeOffset FiredAt = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FiresWhenExhaustionOccurredWithValidSafeTokens()
    {
        OperationalAlertPayload? alert = RetryExhaustionAlertEvaluator.Evaluate(
            exhaustionOccurred: true, "tenant-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAW", FiredAt);

        alert.ShouldNotBeNull();
        alert.AlertKind.ShouldBe(OperatorAlertKind.RetryExhausted);
        alert.ReasonCode.ShouldBe("retry_exhaustion_threshold_exceeded");
        alert.OwnerRole.ShouldBe("operations-admin");
        alert.NextSafeAction.ShouldBe("review-failed-queue");
        alert.AffectedScope.ShouldBe("tenant:tenant-alpha");
        OperationalAlertPayload.IsValid(alert).ShouldBeTrue();
    }

    [Fact]
    public void SuppressesWhenNoExhaustion()
        => RetryExhaustionAlertEvaluator.Evaluate(false, "tenant-alpha", "corr-1", FiredAt)
            .ShouldBeNull();
}
