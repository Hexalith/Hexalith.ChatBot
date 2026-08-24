namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Carries the statically validated producer requirements for one exact completion event.
/// </summary>
/// <param name="RequiresTopology">Whether a current-run topology lane is declared.</param>
/// <param name="RequiresRecovery">Whether the single current-run recovery lane is declared.</param>
/// <param name="RetainedLocators">The distinct retained artifact locators required by active contracts.</param>
public sealed record CompletionProductionPlan(
    bool RequiresTopology,
    bool RequiresRecovery,
    IReadOnlyList<string> RetainedLocators);
