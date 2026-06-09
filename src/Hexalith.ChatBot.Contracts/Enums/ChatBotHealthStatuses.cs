namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Maps between <see cref="ChatBotHealthStatus"/> and its stable wire token. Health status is always an
/// explicit value drawn from the closed <see cref="ChatBotHealthStatus"/> contract — never a string derived
/// from counts (Story 1.6, AC5). An unrecognized wire token collapses to
/// <see cref="ChatBotHealthStatus.Unknown"/> (the fail-safe), never to a fabricated healthy.
/// </summary>
public static class ChatBotHealthStatuses
{
    /// <summary>The wire token used when status cannot be resolved.</summary>
    public const string DefaultWireValue = "unknown";

    /// <summary>Returns the stable wire token for a health status.</summary>
    /// <param name="status">The health status.</param>
    /// <returns>The wire token (for example <c>"healthy"</c> or <c>"degraded"</c>).</returns>
    public static string ToWireValue(ChatBotHealthStatus status)
        => status switch
        {
            ChatBotHealthStatus.Healthy => "healthy",
            ChatBotHealthStatus.Degraded => "degraded",
            ChatBotHealthStatus.Failed => "failed",
            ChatBotHealthStatus.Unknown => "unknown",
            _ => DefaultWireValue,
        };

    /// <summary>
    /// Resolves a declared wire token to a closed health status, defaulting to
    /// <see cref="ChatBotHealthStatus.Unknown"/> for an absent, blank, or unrecognized value.
    /// </summary>
    /// <param name="wireValue">The declared status token.</param>
    /// <returns>The resolved health status.</returns>
    public static ChatBotHealthStatus FromWireValueOrUnknown(string? wireValue)
        => wireValue?.Trim().ToLowerInvariant() switch
        {
            "healthy" => ChatBotHealthStatus.Healthy,
            "degraded" => ChatBotHealthStatus.Degraded,
            "failed" => ChatBotHealthStatus.Failed,
            _ => ChatBotHealthStatus.Unknown,
        };
}
