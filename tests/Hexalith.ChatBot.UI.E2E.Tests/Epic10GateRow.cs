namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>
/// Defines the exact source-local release-readiness markers required for one canonical UI surface.
/// </summary>
/// <param name="Surface">The canonical release-readiness surface name.</param>
/// <param name="RequiredMarkersByPath">The required marker strings keyed by the source file that must contain them.</param>
internal sealed record Epic10GateRow(
    string Surface,
    IReadOnlyDictionary<string, IReadOnlyList<string>> RequiredMarkersByPath);
