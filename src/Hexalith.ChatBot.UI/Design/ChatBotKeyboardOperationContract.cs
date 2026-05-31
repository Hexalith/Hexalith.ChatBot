namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Keyboard operation metadata for a governed UI surface.
/// </summary>
/// <param name="SurfaceName">Surface name.</param>
/// <param name="RequiredKeyboardPaths">Keyboard paths the surface must preserve.</param>
/// <param name="RequiresVisibleFocus">Whether visible focus is required for reachable controls.</param>
/// <param name="RequiresShortcutGovernance">Whether shortcut governance applies to text-entry paths.</param>
/// <param name="RequiresNoHoverOnlyCriticalActions">Whether critical actions must avoid hover-only disclosure.</param>
public sealed record ChatBotKeyboardOperationContract(
    string SurfaceName,
    IReadOnlyList<string> RequiredKeyboardPaths,
    bool RequiresVisibleFocus,
    bool RequiresShortcutGovernance,
    bool RequiresNoHoverOnlyCriticalActions)
{
    /// <summary>Gets a value indicating whether required keyboard metadata is complete.</summary>
    public bool IsComplete
        => !string.IsNullOrWhiteSpace(SurfaceName)
            && RequiredKeyboardPaths is { Count: > 0 }
            && RequiredKeyboardPaths.All(static path => !string.IsNullOrWhiteSpace(path))
            && RequiresVisibleFocus
            && RequiresShortcutGovernance
            && RequiresNoHoverOnlyCriticalActions;

    /// <summary>Creates the default keyboard operation contract for a governed surface.</summary>
    /// <param name="SurfaceName">Surface name.</param>
    /// <param name="RequiredKeyboardPaths">Keyboard paths the surface must preserve.</param>
    /// <returns>A governed keyboard operation contract.</returns>
    public static ChatBotKeyboardOperationContract CreateGovernedSurface(
        string SurfaceName,
        IReadOnlyList<string> RequiredKeyboardPaths)
        => new(
            SurfaceName,
            RequiredKeyboardPaths,
            RequiresVisibleFocus: true,
            RequiresShortcutGovernance: true,
            RequiresNoHoverOnlyCriticalActions: true);
}
