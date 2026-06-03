namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>Stable wire tokens and helpers for <see cref="ErrorBudgetBurnState"/>.</summary>
public static class ErrorBudgetBurnStates
{
    public const string Unknown = "unknown";
    public const string WithinBudget = "within-budget";
    public const string Approaching = "approaching";
    public const string Exhausted = "exhausted";

    public static IReadOnlyList<ErrorBudgetBurnState> All { get; } =
    [
        ErrorBudgetBurnState.Unknown,
        ErrorBudgetBurnState.WithinBudget,
        ErrorBudgetBurnState.Approaching,
        ErrorBudgetBurnState.Exhausted,
    ];

    public static bool TryFromWireValue(string? value, out ErrorBudgetBurnState state)
    {
        state = ErrorBudgetBurnState.Unknown;
        switch (value?.Trim().ToLowerInvariant())
        {
            case Unknown:
                state = ErrorBudgetBurnState.Unknown;
                return true;
            case WithinBudget:
                state = ErrorBudgetBurnState.WithinBudget;
                return true;
            case Approaching:
                state = ErrorBudgetBurnState.Approaching;
                return true;
            case Exhausted:
                state = ErrorBudgetBurnState.Exhausted;
                return true;
            default:
                return false;
        }
    }

    public static string ToWireValue(ErrorBudgetBurnState state)
        => state switch
        {
            ErrorBudgetBurnState.Unknown => Unknown,
            ErrorBudgetBurnState.WithinBudget => WithinBudget,
            ErrorBudgetBurnState.Approaching => Approaching,
            ErrorBudgetBurnState.Exhausted => Exhausted,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported error-budget burn state."),
        };
}
