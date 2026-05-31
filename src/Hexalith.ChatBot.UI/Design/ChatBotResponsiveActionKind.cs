namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Responsive action categories that influence touch target sizing.
/// </summary>
public enum ChatBotResponsiveActionKind
{
    /// <summary>Standard governed action.</summary>
    Standard,

    /// <summary>Approval or confirmation action.</summary>
    Approval,

    /// <summary>Destructive or irreversible action.</summary>
    Destructive,
}
