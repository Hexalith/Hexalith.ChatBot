namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Mechanical policy for preventing stacked active modal dialogs and sheets.
/// </summary>
public static class ChatBotOverlayPolicy
{
    /// <summary>Returns whether <paramref name="requested" /> may be activated with the current overlays.</summary>
    /// <param name="activeOverlays">Currently active overlays.</param>
    /// <param name="requested">Requested overlay kind.</param>
    /// <returns><see langword="true" /> when activation is permitted.</returns>
    public static bool AllowsActivation(IEnumerable<ChatBotOverlayKind> activeOverlays, ChatBotOverlayKind requested)
    {
        ArgumentNullException.ThrowIfNull(activeOverlays);

        return !IsModal(requested)
            || !activeOverlays.Any(IsModal);
    }

    /// <summary>Returns whether the overlay blocks activation of another modal dialog or sheet.</summary>
    /// <param name="kind">Overlay kind.</param>
    /// <returns><see langword="true" /> for modal dialogs and modal sheets.</returns>
    public static bool IsModal(ChatBotOverlayKind kind)
        => kind is ChatBotOverlayKind.ModalDialog or ChatBotOverlayKind.ModalSheet;

    /// <summary>Returns whether the overlay must preserve Escape close and focus-return semantics.</summary>
    /// <param name="kind">Overlay kind.</param>
    /// <returns><see langword="true" /> for overlays that can be the active topmost interaction layer.</returns>
    public static bool RequiresEscapeAndFocusReturn(ChatBotOverlayKind kind)
        => kind is ChatBotOverlayKind.ModalDialog
            or ChatBotOverlayKind.ModalSheet
            or ChatBotOverlayKind.Popover
            or ChatBotOverlayKind.EvidenceDrawer
            or ChatBotOverlayKind.ReviewPanel;

    /// <summary>Returns whether the overlay should be represented as a labelled complementary region instead of a stacked modal.</summary>
    /// <param name="kind">Overlay kind.</param>
    /// <returns><see langword="true" /> for side-panel evidence and review regions.</returns>
    public static bool IsComplementaryRegion(ChatBotOverlayKind kind)
        => kind is ChatBotOverlayKind.EvidenceDrawer
            or ChatBotOverlayKind.ReviewPanel
            or ChatBotOverlayKind.ComplementaryRegion;
}
