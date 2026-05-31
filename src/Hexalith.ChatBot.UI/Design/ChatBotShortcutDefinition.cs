namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Metadata-only shortcut definition guarded against unsafe text-entry defaults.
/// </summary>
/// <param name="Id">Stable shortcut identifier.</param>
/// <param name="Key">Stable key or key chord label.</param>
/// <param name="Scope">Shortcut scope.</param>
/// <param name="RequiresModifier">Whether the shortcut requires Ctrl, Alt, Meta, or Shift.</param>
/// <param name="IsCharacterKeyShortcut">Whether the shortcut is a single-character shortcut.</param>
/// <param name="CanBeDisabledGlobally">Whether the shortcut can be disabled globally.</param>
/// <param name="CanBeRemapped">Whether the shortcut can be remapped.</param>
/// <param name="PreferenceEntryLabel">Preferences entry used to manage this shortcut.</param>
public sealed record ChatBotShortcutDefinition(
    string Id,
    string Key,
    ChatBotShortcutScope Scope,
    bool RequiresModifier,
    bool IsCharacterKeyShortcut,
    bool CanBeDisabledGlobally = ChatBotShortcutPreferenceContract.CanDisableGlobally,
    bool CanBeRemapped = ChatBotShortcutPreferenceContract.SupportsRemapping,
    string PreferenceEntryLabel = ChatBotShortcutPreferenceContract.EntryLabel)
{
    /// <summary>Gets a value indicating whether this shortcut is enabled by default inside text-entry controls.</summary>
    public bool IsAllowedByDefaultInTextEntry
        => !IsTextEntryScope
            || (RequiresModifier && !IsCharacterKeyShortcut);

    /// <summary>Gets a value indicating whether the scope is composer, search, filter, or configuration text entry.</summary>
    public bool IsTextEntryScope
        => Scope is ChatBotShortcutScope.Composer
            or ChatBotShortcutScope.Search
            or ChatBotShortcutScope.Filter
            or ChatBotShortcutScope.ConfigurationForm;
}
