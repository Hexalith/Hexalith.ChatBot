using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.13 (AC1/AC2/AC3) coverage for the pure scoped-outage degradation verdict function: <c>contained</c> iff all
/// three NFR59 isolation assertions pass, the observed scope equals the expected scope, in-flight items resume
/// recoverable, and no duplicate side effect; <c>breached</c> when each serious assertion fails (independently and
/// combined); the deviation list enumerates exactly the breached dimensions in a stable order (appending
/// <c>scope_recording_exceeded</c> when the recording is late); the first-breach locator is deterministic; and the
/// evaluator consumes no clock/IO. Mirrors <see cref="ContinuityDrillEvaluatorTests"/>.
/// </summary>
public sealed class ScopedOutageDegradationEvaluatorTests
{
    private static readonly DateTimeOffset Started = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ScopeRecordingTargetIsPinnedToTheFiveMinuteNfr41Budget()
        => RecoveryTargets.MaxScopeRecordingLatency.ShouldBe(TimeSpan.FromMinutes(5));

    [Fact]
    public void ContainedWhenAllAssertionsPassAndScopeMatchesAndRecoveryHolds()
    {
        ScopedOutageDegradationMeasurement measurement = Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox);

        ScopedOutageDegradationEvaluator.Evaluate(measurement).ShouldBe(ScopedOutageDegradationVerdicts.Contained);
        ScopedOutageDegradationEvaluator.Deviations(measurement, scopeRecordedWithinTarget: true).ShouldBeEmpty();
        ScopedOutageDegradationEvaluator.FirstBreachLocator(measurement).ShouldBeNull();
    }

    [Fact]
    public void BreachedWhenCrossTenantLeakageDetected()
    {
        ScopedOutageDegradationMeasurement measurement = Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox) with { CrossTenantLeakageDetected = true };

        ScopedOutageDegradationEvaluator.Evaluate(measurement).ShouldBe(ScopedOutageDegradationVerdicts.Breached);
        ScopedOutageDegradationEvaluator.Deviations(measurement, true).ShouldBe([ScopedOutageDegradationEvaluator.CrossTenantLeakageDeviation]);
    }

    [Fact]
    public void BreachedWhenUnauthorizedMutationDetected()
    {
        ScopedOutageDegradationMeasurement measurement = Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox) with { UnauthorizedMutationDetected = true };

        ScopedOutageDegradationEvaluator.Evaluate(measurement).ShouldBe(ScopedOutageDegradationVerdicts.Breached);
        ScopedOutageDegradationEvaluator.Deviations(measurement, true).ShouldBe([ScopedOutageDegradationEvaluator.UnauthorizedMutationDeviation]);
    }

    [Fact]
    public void BreachedWhenSilentDataLossDetected()
    {
        ScopedOutageDegradationMeasurement measurement = Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox) with { SilentDataLossDetected = true };

        ScopedOutageDegradationEvaluator.Evaluate(measurement).ShouldBe(ScopedOutageDegradationVerdicts.Breached);
        ScopedOutageDegradationEvaluator.Deviations(measurement, true).ShouldBe([ScopedOutageDegradationEvaluator.SilentDataLossDeviation]);
    }

    [Fact]
    public void BreachedWhenObservedScopeEscapesExpectedScope()
    {
        // The outage was expected to degrade only a mailbox, but it degraded the whole tenant — an NFR58 scope escape.
        ScopedOutageDegradationMeasurement measurement = Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Tenant);

        ScopedOutageDegradationEvaluator.Evaluate(measurement).ShouldBe(ScopedOutageDegradationVerdicts.Breached);
        ScopedOutageDegradationEvaluator.Deviations(measurement, true).ShouldBe([ScopedOutageDegradationEvaluator.ScopeEscapeDeviation]);
    }

    [Fact]
    public void BreachedWhenInflightItemsNotRecoverable()
    {
        ScopedOutageDegradationMeasurement measurement = Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox) with { InflightItemsRecoverable = false };

        ScopedOutageDegradationEvaluator.Evaluate(measurement).ShouldBe(ScopedOutageDegradationVerdicts.Breached);
        ScopedOutageDegradationEvaluator.Deviations(measurement, true).ShouldBe([ScopedOutageDegradationEvaluator.InflightNotRecoverableDeviation]);
    }

    [Fact]
    public void BreachedWhenDuplicateSideEffectDetected()
    {
        ScopedOutageDegradationMeasurement measurement = Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox) with { DuplicateSideEffectDetected = true };

        ScopedOutageDegradationEvaluator.Evaluate(measurement).ShouldBe(ScopedOutageDegradationVerdicts.Breached);
        ScopedOutageDegradationEvaluator.Deviations(measurement, true).ShouldBe([ScopedOutageDegradationEvaluator.DuplicateSideEffectDeviation]);
    }

    [Fact]
    public void DeviationsEnumerateAllBreachedDimensionsInStableOrderIncludingLateRecording()
    {
        ScopedOutageDegradationMeasurement measurement = Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Tenant) with
        {
            CrossTenantLeakageDetected = true,
            UnauthorizedMutationDetected = true,
            SilentDataLossDetected = true,
            InflightItemsRecoverable = false,
            DuplicateSideEffectDetected = true,
        };

        ScopedOutageDegradationEvaluator.Deviations(measurement, scopeRecordedWithinTarget: false).ShouldBe(
        [
            ScopedOutageDegradationEvaluator.CrossTenantLeakageDeviation,
            ScopedOutageDegradationEvaluator.UnauthorizedMutationDeviation,
            ScopedOutageDegradationEvaluator.SilentDataLossDeviation,
            ScopedOutageDegradationEvaluator.ScopeEscapeDeviation,
            ScopedOutageDegradationEvaluator.InflightNotRecoverableDeviation,
            ScopedOutageDegradationEvaluator.DuplicateSideEffectDeviation,
            ScopedOutageDegradationEvaluator.ScopeRecordingExceededDeviation,
        ]);
    }

    [Fact]
    public void ContainedButLateRecordingAppendsOnlyTheRecordingDeviation()
    {
        // A clean, contained validation whose scope recording was late: only the recalibration-signal deviation appears.
        ScopedOutageDegradationMeasurement measurement = Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox);

        ScopedOutageDegradationEvaluator.Evaluate(measurement).ShouldBe(ScopedOutageDegradationVerdicts.Contained);
        ScopedOutageDegradationEvaluator.Deviations(measurement, scopeRecordedWithinTarget: false)
            .ShouldBe([ScopedOutageDegradationEvaluator.ScopeRecordingExceededDeviation]);
    }

    [Fact]
    public void FirstBreachLocatorIsTheFirstFailedAssertionInStableOrderAndDeterministic()
    {
        // Leakage is first in the stable order even though scope escape is also present.
        ScopedOutageDegradationMeasurement measurement = Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Tenant) with { CrossTenantLeakageDetected = true };

        string? locator = ScopedOutageDegradationEvaluator.FirstBreachLocator(measurement);
        locator.ShouldBe($"scope:{ScopedOutageScopes.Tenant}|deviation:{ScopedOutageDegradationEvaluator.CrossTenantLeakageDeviation}");
        locator.ShouldBe(ScopedOutageDegradationEvaluator.FirstBreachLocator(measurement)); // deterministic across runs
    }

    [Fact]
    public void FirstBreachLocatorForAScopeEscapeNamesTheScopeEscapeDeviation()
    {
        ScopedOutageDegradationMeasurement measurement = Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Operation);

        ScopedOutageDegradationEvaluator.FirstBreachLocator(measurement)
            .ShouldBe($"scope:{ScopedOutageScopes.Operation}|deviation:{ScopedOutageDegradationEvaluator.ScopeEscapeDeviation}");
    }

    [Fact]
    public void FirstBreachLocatorForARecoveryBreachNamesTheRecoveryDeviationAtTheContainedScope()
    {
        // No scope escape (observed == expected), but in-flight items did not resume recoverable: the locator names the
        // recovery-class deviation at the (unescaped) observed scope — distinct from the scope-escape locator shape.
        ScopedOutageDegradationMeasurement measurement = Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox) with { InflightItemsRecoverable = false };

        ScopedOutageDegradationEvaluator.FirstBreachLocator(measurement)
            .ShouldBe($"scope:{ScopedOutageScopes.Mailbox}|deviation:{ScopedOutageDegradationEvaluator.InflightNotRecoverableDeviation}");
    }

    [Fact]
    public void EvaluatorRejectsANullMeasurement()
    {
        // Purity guard: the pure surface validates its input rather than dereferencing null (no clock/IO, fail-fast).
        Should.Throw<ArgumentNullException>(() => ScopedOutageDegradationEvaluator.Evaluate(null!));
        Should.Throw<ArgumentNullException>(() => ScopedOutageDegradationEvaluator.Deviations(null!, scopeRecordedWithinTarget: true));
        Should.Throw<ArgumentNullException>(() => ScopedOutageDegradationEvaluator.FirstBreachLocator(null!));
    }

    private static ScopedOutageDegradationMeasurement Clean(string expectedScope, string observedScope)
        => new(
            expectedScope,
            observedScope,
            CrossTenantLeakageDetected: false,
            UnauthorizedMutationDetected: false,
            SilentDataLossDetected: false,
            InflightItemsRecoverable: true,
            DuplicateSideEffectDetected: false,
            ScopeRecordingLatency: TimeSpan.FromMinutes(1),
            Started,
            Started + TimeSpan.FromMinutes(2));
}
