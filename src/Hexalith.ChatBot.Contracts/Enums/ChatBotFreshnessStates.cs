namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>Stable wire tokens and helpers for <see cref="ChatBotFreshnessState"/>.</summary>
public static class ChatBotFreshnessStates
{
    public const string Fresh = "fresh";
    public const string Stale = "stale";
    public const string Expired = "expired";

    public static IReadOnlyList<ChatBotFreshnessState> All { get; } =
    [
        ChatBotFreshnessState.Fresh,
        ChatBotFreshnessState.Stale,
        ChatBotFreshnessState.Expired,
    ];

    public static bool TryFromWireValue(string? value, out ChatBotFreshnessState state)
    {
        state = ChatBotFreshnessState.Fresh;
        switch (value?.Trim().ToLowerInvariant())
        {
            case Fresh:
                state = ChatBotFreshnessState.Fresh;
                return true;
            case Stale:
                state = ChatBotFreshnessState.Stale;
                return true;
            case Expired:
                state = ChatBotFreshnessState.Expired;
                return true;
            default:
                return false;
        }
    }

    public static string ToWireValue(ChatBotFreshnessState state)
        => state switch
        {
            ChatBotFreshnessState.Fresh => Fresh,
            ChatBotFreshnessState.Stale => Stale,
            ChatBotFreshnessState.Expired => Expired,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported freshness state."),
        };
}
