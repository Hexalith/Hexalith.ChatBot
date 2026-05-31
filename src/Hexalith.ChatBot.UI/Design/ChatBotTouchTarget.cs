namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Governed touch target rules for responsive ChatBot UI.
/// </summary>
public static class ChatBotTouchTarget
{
    /// <summary>Gets the primary phone/tablet target minimum in CSS pixels.</summary>
    public const int PrimaryMinimumCssPixels = 44;

    /// <summary>Gets the dense secondary target minimum in CSS pixels.</summary>
    public const int DenseSecondaryMinimumCssPixels = 24;

    /// <summary>Gets the minimum size for a touch target class.</summary>
    /// <param name="targetClass">Touch target class.</param>
    /// <returns>Minimum size in CSS pixels.</returns>
    public static int MinimumSizeFor(ChatBotTouchTargetClass targetClass)
        => targetClass switch
        {
            ChatBotTouchTargetClass.Primary => PrimaryMinimumCssPixels,
            ChatBotTouchTargetClass.DenseSecondary => DenseSecondaryMinimumCssPixels,
            _ => throw new ArgumentOutOfRangeException(nameof(targetClass), targetClass, "Unknown touch target class."),
        };

    /// <summary>
    /// Determines whether dense secondary sizing is allowed for an action at a viewport tier.
    /// </summary>
    /// <param name="actionKind">Responsive action kind.</param>
    /// <param name="tier">Viewport tier.</param>
    /// <returns><see langword="true"/> when compact dense sizing is permitted.</returns>
    public static bool CanUseDenseSecondarySizing(ChatBotResponsiveActionKind actionKind, ChatBotViewportTier tier)
        => actionKind switch
        {
            ChatBotResponsiveActionKind.Approval or ChatBotResponsiveActionKind.Destructive
                when tier is ChatBotViewportTier.Phone or ChatBotViewportTier.Tablet => false,
            _ => true,
        };
}
