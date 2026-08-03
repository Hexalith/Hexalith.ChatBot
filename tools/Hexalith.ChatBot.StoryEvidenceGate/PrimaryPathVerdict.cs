namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Describes one required primary-path verdict.
/// </summary>
/// <param name="PathClass">The policy path class.</param>
/// <param name="Executed">Whether a recognized primary lane executed successfully.</param>
/// <param name="Lane">The satisfying lane, when present.</param>
public sealed record PrimaryPathVerdict(string PathClass, bool Executed, string? Lane);
