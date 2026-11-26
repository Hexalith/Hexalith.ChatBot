namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Represents the metadata-only deterministic gate report.
/// </summary>
public sealed class GateReport
{
    /// <summary>Gets or sets the report schema version.</summary>
    public string SchemaVersion { get; set; } = "2.0";

    /// <summary>Gets or sets the evaluated story key.</summary>
    public string StoryKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the exact base revision.</summary>
    public string BaseCommit { get; set; } = string.Empty;

    /// <summary>Gets or sets the exact head revision.</summary>
    public string HeadCommit { get; set; } = string.Empty;

    /// <summary>Gets or sets the implementation scope digest.</summary>
    public string ImplementationDigest { get; set; } = string.Empty;

    /// <summary>Gets or sets the policy version.</summary>
    public string PolicyVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the evaluation passed.</summary>
    public bool Passed { get; set; }

    /// <summary>Gets or sets the normalized File List count.</summary>
    public int FileListCount { get; set; }

    /// <summary>Gets or sets the scoped diff count.</summary>
    public int ScopedDiffCount { get; set; }

    /// <summary>Gets or sets the independent base-to-head event-path count.</summary>
    public int EventPathCount { get; set; }

    /// <summary>Gets or sets the checked item count.</summary>
    public int CheckedItemCount { get; set; }

    /// <summary>Gets or sets the mapped item count.</summary>
    public int MappedItemCount { get; set; }

    /// <summary>Gets or sets the evaluated repository scopes.</summary>
    public IReadOnlyList<string> RepositoryScopes { get; set; } = [];

    /// <summary>Gets or sets machine-derived lane results.</summary>
    public IReadOnlyList<LaneResult> Lanes { get; set; } = [];

    /// <summary>Gets or sets primary path verdicts.</summary>
    public IReadOnlyList<PrimaryPathVerdict> PrimaryPaths { get; set; } = [];

    /// <summary>Gets or sets stable metadata-only failures.</summary>
    public IReadOnlyList<GateIssue> Issues { get; set; } = [];

    /// <summary>Gets or sets the evaluation timestamp.</summary>
    public DateTimeOffset EvaluatedAtUtc { get; set; }
}
