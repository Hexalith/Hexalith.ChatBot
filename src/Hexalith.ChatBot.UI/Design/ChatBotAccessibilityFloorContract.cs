namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Canonical accessibility floor requirements for governed ChatBot UI surfaces.
/// </summary>
public static class ChatBotAccessibilityFloorContract
{
    /// <summary>Gets the ordered requirements every governed UI surface inherits.</summary>
    public static IReadOnlyList<ChatBotAccessibilityRequirement> RequiredContracts { get; } =
    [
        new(
            "Keyboard operation",
            "All governed workflows expose keyboard-reachable entry, review, approval, retry, correction, stop, and explanation controls."),
        new(
            "Repeated landmark naming",
            "Repeated page, region, complementary, status, and alert landmarks carry unique accessible names within a surface."),
        new(
            "Visible-order focus sequence",
            "Skip links, main content, current h1 heading, project context, primary region, complementary region, and status summary follow visible reading and action order."),
        new(
            "Focus return",
            "Dialogs, sheets, drawers, popovers, and review panels restore focus to the invoking control after close or Escape."),
        new(
            "Disabled-action explanation",
            "Unavailable governed actions remain focusable, expose aria-disabled, link to a reachable reason, and fail closed when activated."),
        new(
            "Busy-region focus preservation",
            "Busy regions set and clear aria-busy on the same labelled node and preserve focus or move it to a labelled landing target after content swaps."),
        new(
            "Validation error association",
            "Validation failures focus a summary before the affected panel and associate invalid fields with field messages."),
        new(
            "Off-surface redaction equivalence",
            "Export, copy, download, read-aloud, handoff, audit, and evidence artifacts use the redacted display payload and expose equivalent redaction guidance."),
    ];
}
