namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Maps between <see cref="ChatBotSurfaceOrigin"/> and its stable wire token. A missing or unknown
/// wire value collapses to <see cref="ChatBotSurfaceOrigin.Api"/> (the safe default) so an unattributed
/// or malformed declaration is still audited, never rejected and never trusted as an arbitrary rewrite.
/// </summary>
public static class ChatBotSurfaceOrigins
{
    /// <summary>The wire token used when no origin is declared.</summary>
    public const string DefaultWireValue = "api";

    /// <summary>
    /// Resolves a declared wire token to a closed surface origin, defaulting to
    /// <see cref="ChatBotSurfaceOrigin.Api"/> for an absent, blank, or unrecognized value.
    /// </summary>
    /// <param name="wireValue">The adapter-declared origin token.</param>
    /// <returns>The resolved surface origin.</returns>
    public static ChatBotSurfaceOrigin FromWireValueOrDefault(string? wireValue)
        => wireValue?.Trim().ToLowerInvariant() switch
        {
            "ui" => ChatBotSurfaceOrigin.Ui,
            "api" => ChatBotSurfaceOrigin.Api,
            "cli" => ChatBotSurfaceOrigin.Cli,
            "mcp" => ChatBotSurfaceOrigin.Mcp,
            "worker" => ChatBotSurfaceOrigin.Worker,
            "mailbox" => ChatBotSurfaceOrigin.Mailbox,
            "ai" => ChatBotSurfaceOrigin.Ai,
            _ => ChatBotSurfaceOrigin.Api,
        };

    /// <summary>Returns the stable wire token for a surface origin.</summary>
    /// <param name="origin">The surface origin.</param>
    /// <returns>The wire token (for example <c>"ui"</c> or <c>"api"</c>).</returns>
    public static string ToWireValue(ChatBotSurfaceOrigin origin)
        => origin switch
        {
            ChatBotSurfaceOrigin.Ui => "ui",
            ChatBotSurfaceOrigin.Api => "api",
            ChatBotSurfaceOrigin.Cli => "cli",
            ChatBotSurfaceOrigin.Mcp => "mcp",
            ChatBotSurfaceOrigin.Worker => "worker",
            ChatBotSurfaceOrigin.Mailbox => "mailbox",
            ChatBotSurfaceOrigin.Ai => "ai",
            _ => DefaultWireValue,
        };
}
