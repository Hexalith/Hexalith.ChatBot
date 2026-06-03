using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// Pure, deterministic, fail-safe mapping from an already-computed server-side signal to the coarse
/// <see cref="ErrorBudgetBurnState"/> published for an SLO (Story 8.3, AC5). It mirrors the
/// <c>AuditProjectionLagEvaluator</c> shape: no IO, no clock, no OTel histogram querying, no count-derived
/// percentage math. The only fully-wired signal today is the audit-projection-lag health; every SLO whose live
/// signal is not yet wired publishes <see cref="ErrorBudgetBurnState.Unknown"/> (honest no-data) rather than a
/// fabricated <see cref="ErrorBudgetBurnState.WithinBudget"/>.
/// </summary>
internal static class ErrorBudgetBurnEvaluator
{
    /// <summary>
    /// Maps a coarse health signal to the corresponding error-budget burn state. <see cref="ChatBotHealthStatus.Unknown"/>
    /// (or any undefined value) maps to <see cref="ErrorBudgetBurnState.Unknown"/> — never a fabricated within-budget.
    /// </summary>
    /// <param name="health">The already-computed coarse health signal.</param>
    /// <returns>The coarse, fail-safe error-budget burn state.</returns>
    public static ErrorBudgetBurnState FromHealth(ChatBotHealthStatus health)
        => health switch
        {
            ChatBotHealthStatus.Healthy => ErrorBudgetBurnState.WithinBudget,
            ChatBotHealthStatus.Degraded => ErrorBudgetBurnState.Approaching,
            ChatBotHealthStatus.Failed => ErrorBudgetBurnState.Exhausted,
            _ => ErrorBudgetBurnState.Unknown,
        };
}
