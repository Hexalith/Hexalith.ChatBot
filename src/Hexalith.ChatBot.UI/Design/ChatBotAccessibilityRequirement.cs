namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// A named accessibility floor requirement for governed UI surfaces.
/// </summary>
/// <param name="Name">Requirement name.</param>
/// <param name="RequiredBehavior">Mechanical behavior future surfaces must preserve.</param>
public sealed record ChatBotAccessibilityRequirement(
    string Name,
    string RequiredBehavior)
{
    /// <summary>Gets a value indicating whether the requirement has usable metadata.</summary>
    public bool IsComplete
        => !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(RequiredBehavior);
}
