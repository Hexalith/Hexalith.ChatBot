using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Observability;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Observability;

/// <summary>
/// Story 9.2 (AC2, NFR50a): the fraction→budget evaluator is a pure, deterministic, fail-safe mapping from an
/// already-computed <see cref="AuditCompletenessMeasurement"/> to <see cref="ErrorBudgetBurnState"/>. It maps the
/// fraction against the single 99.5% threshold and reports <see cref="ErrorBudgetBurnState.Unknown"/> when the
/// measurement is unmeasurable — never a fabricated within-budget.
/// </summary>
public sealed class AuditCompletenessBudgetEvaluatorTests
{
    private static AuditCompletenessMeasurement Measured(double fraction)
        => new(
            "tenant-alpha",
            IsMeasurable: true,
            ReconstructableCount: 0,
            TotalCount: 0,
            fraction,
            WindowStartUtc: default,
            WindowEndUtc: default,
            FirstDivergingOperationLocator: null,
            AuditCompletenessMeasurement.MeasuredReasonCode);

    [Theory]
    [InlineData(1.0, ErrorBudgetBurnState.WithinBudget)]
    [InlineData(0.995, ErrorBudgetBurnState.WithinBudget)]
    [InlineData(0.9949, ErrorBudgetBurnState.Exhausted)]
    [InlineData(0.5, ErrorBudgetBurnState.Exhausted)]
    [InlineData(0.0, ErrorBudgetBurnState.Exhausted)]
    public void FractionMapsToBudgetStateAcrossTheNinetyNinePointFiveThreshold(double fraction, ErrorBudgetBurnState expected)
        => AuditCompletenessBudgetEvaluator.FromMeasurement(Measured(fraction)).ShouldBe(expected);

    [Fact]
    public void UnmeasurableMapsToUnknownNeverFabricatedWithinBudget()
    {
        AuditCompletenessMeasurement unmeasurable = AuditCompletenessMeasurement.Unmeasurable("tenant-alpha", default, default);

        AuditCompletenessBudgetEvaluator.FromMeasurement(unmeasurable).ShouldBe(ErrorBudgetBurnState.Unknown);
    }

    [Fact]
    public void MappingIsDeterministicForTheSameInput()
    {
        AuditCompletenessMeasurement measurement = Measured(0.994);
        ErrorBudgetBurnState first = AuditCompletenessBudgetEvaluator.FromMeasurement(measurement);
        AuditCompletenessBudgetEvaluator.FromMeasurement(measurement).ShouldBe(first);
    }
}
