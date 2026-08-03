namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Carries normalized CLI validation options.
/// </summary>
public sealed class GateOptions
{
    /// <summary>Gets or sets the repository root.</summary>
    public required string RepositoryRoot { get; init; }

    /// <summary>Gets or sets the policy path.</summary>
    public required string PolicyPath { get; init; }

    /// <summary>Gets or sets the story path.</summary>
    public required string StoryPath { get; init; }

    /// <summary>Gets or sets the evidence contract path.</summary>
    public required string ContractPath { get; init; }

    /// <summary>Gets or sets the proposed target status.</summary>
    public required string TargetStatus { get; init; }

    /// <summary>Gets or sets the exact base revision.</summary>
    public required string BaseCommit { get; init; }

    /// <summary>Gets or sets the exact head revision.</summary>
    public required string HeadCommit { get; init; }

    /// <summary>Gets or sets the results root.</summary>
    public required string ResultsRoot { get; init; }

    /// <summary>Gets or sets an optional report path.</summary>
    public string? ReportPath { get; init; }

    /// <summary>Gets or sets the evaluation clock.</summary>
    public DateTimeOffset NowUtc { get; init; } = DateTimeOffset.UtcNow;
}
