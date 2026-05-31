namespace Hexalith.ChatBot.UI.Design;

/// <summary>Describes one governed ChatBot semantic token slot and its Fluent-backed aliases.</summary>
/// <param name="Name">The stable semantic slot name used by ChatBot UI markup and CSS.</param>
/// <param name="Meaning">The governed product meaning for the slot.</param>
/// <param name="BackgroundAlias">The ChatBot CSS custom property for the slot background.</param>
/// <param name="ForegroundAlias">The ChatBot CSS custom property for the slot foreground.</param>
public sealed record ChatBotSemanticToken(
    string Name,
    string Meaning,
    string BackgroundAlias,
    string ForegroundAlias);
