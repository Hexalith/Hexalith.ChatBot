namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Describes what a governed ChatBot surface must support at a web viewport tier.
/// </summary>
/// <param name="Tier">Viewport tier.</param>
/// <param name="Label">Human-readable tier label.</param>
/// <param name="MinimumWidthCssPixels">Inclusive minimum width in CSS pixels.</param>
/// <param name="MaximumWidthCssPixels">Inclusive maximum width in CSS pixels, when bounded.</param>
/// <param name="SupportsFullWorkflow">Whether the tier supports full governed workflow completion.</param>
/// <param name="AllowsStackedPanels">Whether conversation, detail, and panels may stack instead of sitting side by side.</param>
/// <param name="SupportsTriage">Whether the tier supports phone-style triage interactions.</param>
/// <param name="AllowsTwoColumnShell">Whether the tier allows the two-column governed shell.</param>
/// <param name="RequiresSafetyCriticalStateVisible">Whether project, state, reason, and safe actions must remain visible.</param>
/// <param name="RequiredBehavior">Concise UX requirement for the tier.</param>
public sealed record ChatBotResponsiveSurfaceCapability(
    ChatBotViewportTier Tier,
    string Label,
    int MinimumWidthCssPixels,
    int? MaximumWidthCssPixels,
    bool SupportsFullWorkflow,
    bool AllowsStackedPanels,
    bool SupportsTriage,
    bool AllowsTwoColumnShell,
    bool RequiresSafetyCriticalStateVisible,
    string RequiredBehavior);
