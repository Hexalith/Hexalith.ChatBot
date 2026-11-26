namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Carries the output of one read-only Git command.
/// </summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="StandardOutput">The standard output.</param>
/// <param name="StandardError">The standard error.</param>
public sealed record GitCommandResult(int ExitCode, string StandardOutput, string StandardError);
