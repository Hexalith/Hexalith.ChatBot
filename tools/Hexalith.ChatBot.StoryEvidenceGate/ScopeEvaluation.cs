namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Represents a reconciled change scope and its deterministic digest.
/// </summary>
/// <param name="Scopes">The explicit repository scopes.</param>
/// <param name="ChangedPaths">The exact owned changed paths.</param>
/// <param name="Digest">The canonical SHA-256 digest.</param>
public sealed record ScopeEvaluation(
    IReadOnlyList<RepositoryScope> Scopes,
    IReadOnlyList<ChangedPath> ChangedPaths,
    string Digest);
