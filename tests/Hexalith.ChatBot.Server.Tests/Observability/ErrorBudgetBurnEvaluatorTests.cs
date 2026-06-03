using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Observability;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Observability;

/// <summary>
/// Story 8.3 AC5: the error-budget burn evaluator is a pure, deterministic, fail-safe mapping from an
/// already-computed coarse health signal to the published <see cref="ErrorBudgetBurnState"/>. It reports
/// <see cref="ErrorBudgetBurnState.Unknown"/> when the signal is absent/unknown — never a fabricated within-budget.
/// </summary>
public sealed class ErrorBudgetBurnEvaluatorTests
{
    [Theory]
    [InlineData(ChatBotHealthStatus.Healthy, ErrorBudgetBurnState.WithinBudget)]
    [InlineData(ChatBotHealthStatus.Degraded, ErrorBudgetBurnState.Approaching)]
    [InlineData(ChatBotHealthStatus.Failed, ErrorBudgetBurnState.Exhausted)]
    [InlineData(ChatBotHealthStatus.Unknown, ErrorBudgetBurnState.Unknown)]
    public void FromHealthShouldMapEachSignalToItsCoarseBurnState(ChatBotHealthStatus health, ErrorBudgetBurnState expected)
        => ErrorBudgetBurnEvaluator.FromHealth(health).ShouldBe(expected);

    [Fact]
    public void AbsentOrUndefinedSignalShouldMapToUnknownNeverFabricatedWithinBudget()
    {
        ErrorBudgetBurnEvaluator.FromHealth(ChatBotHealthStatus.Unknown).ShouldBe(ErrorBudgetBurnState.Unknown);
        ErrorBudgetBurnEvaluator.FromHealth((ChatBotHealthStatus)999).ShouldBe(ErrorBudgetBurnState.Unknown);
    }

    [Fact]
    public void MappingShouldBeDeterministicForTheSameInput()
    {
        foreach (ChatBotHealthStatus health in Enum.GetValues<ChatBotHealthStatus>())
        {
            ErrorBudgetBurnState first = ErrorBudgetBurnEvaluator.FromHealth(health);
            ErrorBudgetBurnEvaluator.FromHealth(health).ShouldBe(first);
        }
    }
}
