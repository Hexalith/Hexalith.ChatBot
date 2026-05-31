namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Single ChatBot-owned semantic token contract. Values are aliases over Fluent/FrontComposer custom
/// properties; the stylesheet owns concrete CSS mappings.
/// </summary>
public static class ChatBotSemanticTokenContract
{
    /// <summary>Gets the exact semantic slots supported by the ChatBot UI foundation.</summary>
    public static IReadOnlyList<ChatBotSemanticToken> Slots { get; } =
    [
        new(
            "neutral",
            "workspace, panes, queues, and audit metadata",
            "--chatbot-color-neutral-background",
            "--chatbot-color-neutral-foreground"),
        new(
            "brand",
            "primary actions and selected navigation only",
            "--chatbot-color-brand-background",
            "--chatbot-color-brand-foreground"),
        new(
            "info",
            "evidence, context, and non-terminal status",
            "--chatbot-color-info-background",
            "--chatbot-color-info-foreground"),
        new(
            "warning",
            "ambiguity, approval-required, stale, degraded, and manual review states",
            "--chatbot-color-warning-background",
            "--chatbot-color-warning-foreground"),
        new(
            "danger",
            "blocked, unauthorized, failed, quarantined, rejected, and terminal states",
            "--chatbot-color-danger-background",
            "--chatbot-color-danger-foreground"),
        new(
            "success",
            "completed, approved, stored, command-success, and projection-complete states",
            "--chatbot-color-success-background",
            "--chatbot-color-success-foreground"),
    ];

    /// <summary>Gets the semantic token slot with the specified name.</summary>
    /// <param name="name">The stable semantic slot name.</param>
    /// <returns>The matching semantic token slot.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when <paramref name="name" /> is not declared.</exception>
    public static ChatBotSemanticToken GetSlot(string name)
        => Slots.Single(slot => string.Equals(slot.Name, name, StringComparison.Ordinal));
}
