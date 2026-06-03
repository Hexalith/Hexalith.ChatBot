using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Lifecycle.Retry;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Tests.Observability;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public sealed class RetryPolicyTests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("graph_throttled", true, "mailbox-operator")]
    [InlineData("graph_subscription_expired", true, "mailbox-admin")]
    [InlineData("graph_token_expired", true, "mailbox-admin")]
    [InlineData("graph_permission_revoked", false, "tenant-admin")]
    [InlineData("mailbox_scope_mismatch", false, "mailbox-admin")]
    public void PolicyShouldClassifyFiniteReasonCodes(string reasonCode, bool retryable, string ownerRole)
    {
        RetryPolicyDecision decision = RetryFailurePolicy.Classify(reasonCode, 1, ObservedAt);

        decision.IsRetryable.ShouldBe(retryable);
        decision.OwnerRole.ShouldBe(ownerRole);
        decision.SafeNextAction.ShouldNotBeNullOrWhiteSpace();
        decision.ReasonCode.ShouldBe(reasonCode);
        decision.MaxAttempts.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void PolicyShouldExhaustAfterMaxAttemptsAndExposeTerminalRecoveryMetadata()
    {
        RetryPolicyDecision decision = RetryFailurePolicy.Classify("graph_throttled", RetryFailurePolicy.DefaultMaxAttempts, ObservedAt);

        decision.IsRetryable.ShouldBeFalse();
        decision.IsExhausted.ShouldBeTrue();
        decision.TerminalReasonCode.ShouldBe("retry_exhausted");
        decision.ManualRecoveryAction.ShouldBe("escalate-to-operations");
        decision.NextRetryAt.ShouldBeNull();
    }

    [Fact]
    public void PolicyShouldUseBoundedExponentialBackoffWithDeterministicJitter()
    {
        RetryPolicyDecision first = RetryFailurePolicy.Classify("graph_throttled", 1, ObservedAt);
        RetryPolicyDecision second = RetryFailurePolicy.Classify("graph_throttled", 2, ObservedAt);

        first.NextRetryAt.ShouldNotBeNull();
        second.NextRetryAt.ShouldNotBeNull();
        second.NextRetryAt!.Value.ShouldBeGreaterThan(first.NextRetryAt!.Value);
        second.NextRetryAt.Value.ShouldBeLessThan(ObservedAt.AddMinutes(15));
    }

    [Fact]
    public async Task AlertEmitterShouldUseOperatorAlertSinkForRetryExhaustion()
    {
        RecordingAlertSink alerts = new();
        RetryFailureAlertEmitter emitter = new(alerts, new FixedClock());
        RetryPolicyDecision decision = RetryFailurePolicy.Classify("graph_throttled", RetryFailurePolicy.DefaultMaxAttempts, ObservedAt);

        await emitter
            .EmitIfRequiredAsync(decision, "tenant-alpha", "retry", "correlation-alpha", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        OperatorAlert alert = alerts.Alerts.ShouldHaveSingleItem();
        alert.Kind.ShouldBe(OperatorAlertKind.RetryExhausted);
        alert.ReasonCode.ShouldBe("graph_throttled");
        alert.TenantId.ShouldBe("tenant-alpha");
        alert.CommandName.ShouldBe("retry");
        alert.CorrelationId.ShouldBe("correlation-alpha");
        alert.RaisedAt.ShouldBe(ObservedAt);
    }

    [Fact]
    public async Task AlertEmitterShouldRecordRetryExhaustionMetricForTheBoundTenant()
    {
        RecordingChatBotMetrics metrics = new();
        RetryFailureAlertEmitter emitter = new(new RecordingAlertSink(), new FixedClock(), metrics);
        RetryPolicyDecision decision = RetryFailurePolicy.Classify("graph_throttled", RetryFailurePolicy.DefaultMaxAttempts, ObservedAt);

        await emitter
            .EmitIfRequiredAsync(decision, "tenant-alpha", "retry", "correlation-alpha", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        metrics.RetryExhaustedTenants.ShouldHaveSingleItem().ShouldBe("tenant-alpha");
    }

    [Fact]
    public async Task AlertEmitterShouldNotRecordRetryExhaustionMetricForRetryableDegradation()
    {
        RecordingChatBotMetrics metrics = new();
        RetryFailureAlertEmitter emitter = new(new RecordingAlertSink(), new FixedClock(), metrics);
        RetryPolicyDecision decision = RetryFailurePolicy.Classify("dispatch_unavailable", 1, ObservedAt);

        await emitter
            .EmitIfRequiredAsync(decision, "tenant-alpha", "command-execution", "correlation-alpha", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        metrics.RetryExhaustedTenants.ShouldBeEmpty();
    }

    [Fact]
    public async Task AlertEmitterShouldUseOperatorAlertSinkForDependencyDegradation()
    {
        RecordingAlertSink alerts = new();
        RetryFailureAlertEmitter emitter = new(alerts, new FixedClock());
        RetryPolicyDecision decision = RetryFailurePolicy.Classify("dispatch_unavailable", 1, ObservedAt);

        await emitter
            .EmitIfRequiredAsync(decision, "tenant-alpha", "command-execution", "correlation-alpha", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        OperatorAlert alert = alerts.Alerts.ShouldHaveSingleItem();
        alert.Kind.ShouldBe(OperatorAlertKind.DependencyDegraded);
        alert.ReasonCode.ShouldBe("dispatch_unavailable");
    }

    private sealed class RecordingAlertSink : IOperatorAlertSink
    {
        public List<OperatorAlert> Alerts { get; } = [];

        public ValueTask EmitAsync(OperatorAlert alert, CancellationToken cancellationToken)
        {
            Alerts.Add(alert);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => ObservedAt;
    }
}
