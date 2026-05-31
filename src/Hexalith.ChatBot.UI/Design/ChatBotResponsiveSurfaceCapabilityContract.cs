namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Canonical governed UI responsive capability contract for web surfaces.
/// </summary>
public static class ChatBotResponsiveSurfaceCapabilityContract
{
    /// <summary>Gets the viewport tier capabilities ordered from phone to desktop.</summary>
    public static IReadOnlyList<ChatBotResponsiveSurfaceCapability> All { get; } =
    [
        new(
            ChatBotViewportTier.Phone,
            "Phone",
            MinimumWidthCssPixels: 0,
            MaximumWidthCssPixels: 599,
            SupportsFullWorkflow: false,
            AllowsStackedPanels: true,
            SupportsTriage: true,
            AllowsTwoColumnShell: false,
            RequiresSafetyCriticalStateVisible: true,
            RequiredBehavior: "Phone supports triage, status lookup, simple safe actions, and handoff without hiding safety-critical state."),
        new(
            ChatBotViewportTier.Tablet,
            "Tablet",
            MinimumWidthCssPixels: 600,
            MaximumWidthCssPixels: 899,
            SupportsFullWorkflow: true,
            AllowsStackedPanels: true,
            SupportsTriage: true,
            AllowsTwoColumnShell: false,
            RequiresSafetyCriticalStateVisible: true,
            RequiredBehavior: "Tablet may stack conversation, detail, and panels while keeping review and approval flows complete."),
        new(
            ChatBotViewportTier.Desktop,
            "Desktop",
            MinimumWidthCssPixels: 900,
            MaximumWidthCssPixels: null,
            SupportsFullWorkflow: true,
            AllowsStackedPanels: false,
            SupportsTriage: false,
            AllowsTwoColumnShell: true,
            RequiresSafetyCriticalStateVisible: true,
            RequiredBehavior: "Desktop and laptop remain the primary full governed workflow surface with a stable two-column shell."),
    ];

    /// <summary>Gets the responsive capability for the requested viewport tier.</summary>
    /// <param name="tier">Viewport tier.</param>
    /// <returns>The matching capability.</returns>
    public static ChatBotResponsiveSurfaceCapability Get(ChatBotViewportTier tier)
        => All.Single(capability => capability.Tier == tier);
}
