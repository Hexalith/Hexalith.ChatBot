namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Focus containment and focus-return metadata for overlays that can become topmost interaction layers.
/// </summary>
/// <param name="OverlayKind">Overlay kind.</param>
/// <param name="ContainsFocusWhenModal">Whether focus is contained while the overlay is modal.</param>
/// <param name="ClosesWithEscapeWhenTopmost">Whether Escape closes a non-destructive topmost overlay.</param>
/// <param name="ReturnsFocusToInvoker">Whether focus returns to the invoking control after close.</param>
/// <param name="UsesComplementaryRegionWhenNonModal">Whether non-modal drawer/review content is a labelled complementary region.</param>
public sealed record ChatBotFocusReturnContract(
    ChatBotOverlayKind OverlayKind,
    bool ContainsFocusWhenModal,
    bool ClosesWithEscapeWhenTopmost,
    bool ReturnsFocusToInvoker,
    bool UsesComplementaryRegionWhenNonModal)
{
    /// <summary>Gets a value indicating whether overlay focus behavior is fully specified.</summary>
    public bool IsComplete
        => ClosesWithEscapeWhenTopmost
            && ReturnsFocusToInvoker
            && (!ChatBotOverlayPolicy.IsModal(OverlayKind) || ContainsFocusWhenModal)
            && (!ChatBotOverlayPolicy.IsComplementaryRegion(OverlayKind) || UsesComplementaryRegionWhenNonModal);

    /// <summary>Creates the default focus-return contract for an overlay kind.</summary>
    /// <param name="kind">Overlay kind.</param>
    /// <returns>A focus-return contract.</returns>
    public static ChatBotFocusReturnContract ForOverlay(ChatBotOverlayKind kind)
        => new(
            kind,
            ContainsFocusWhenModal: ChatBotOverlayPolicy.RequiresFocusContainment(kind),
            ClosesWithEscapeWhenTopmost: ChatBotOverlayPolicy.AllowsEscapeCloseWhenTopmost(kind),
            ReturnsFocusToInvoker: ChatBotOverlayPolicy.RequiresFocusReturn(kind),
            UsesComplementaryRegionWhenNonModal: ChatBotOverlayPolicy.IsComplementaryRegion(kind));
}
