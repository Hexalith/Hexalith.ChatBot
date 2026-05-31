namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Governed web viewport tiers ordered from narrowest to widest.
/// </summary>
public enum ChatBotViewportTier
{
    /// <summary>Phone triage surface with safety-critical state preserved.</summary>
    Phone,

    /// <summary>Tablet surface where conversation, detail, and panels may stack.</summary>
    Tablet,

    /// <summary>Desktop and laptop surface for the full governed workflow.</summary>
    Desktop,
}
