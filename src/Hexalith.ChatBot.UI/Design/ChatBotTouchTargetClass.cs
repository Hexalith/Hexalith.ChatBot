namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Governed touch target sizing classes.
/// </summary>
public enum ChatBotTouchTargetClass
{
    /// <summary>Primary phone/tablet target for guarded, approval, destructive, and streaming actions.</summary>
    Primary,

    /// <summary>Dense secondary target that must still satisfy the WCAG pointer target floor.</summary>
    DenseSecondary,
}
