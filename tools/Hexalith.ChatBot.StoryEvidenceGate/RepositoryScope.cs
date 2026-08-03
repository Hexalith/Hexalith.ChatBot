namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Defines one explicit root or root-declared-submodule scope.
/// </summary>
/// <param name="Name">The contract scope name.</param>
/// <param name="Path">The root-relative repository path.</param>
/// <param name="BaseCommit">The exact repository base revision.</param>
/// <param name="HeadCommit">The exact repository head revision.</param>
/// <param name="IncludeWorkingTree">Whether local worktree changes participate.</param>
/// <param name="IncludePaths">The exact paths owned by the record.</param>
public sealed record RepositoryScope(
    string Name,
    string Path,
    string BaseCommit,
    string HeadCommit,
    bool IncludeWorkingTree,
    IReadOnlySet<string> IncludePaths);
