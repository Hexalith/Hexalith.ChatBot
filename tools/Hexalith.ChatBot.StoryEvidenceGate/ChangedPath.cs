namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Describes a normalized changed path and its canonical Git identity.
/// </summary>
/// <param name="Repository">The contract repository scope.</param>
/// <param name="Path">The repository-relative path.</param>
/// <param name="Status">The Git change status.</param>
/// <param name="Mode">The Git file mode.</param>
/// <param name="ObjectId">The blob or gitlink object identifier.</param>
public sealed record ChangedPath(string Repository, string Path, string Status, string Mode, string ObjectId);
