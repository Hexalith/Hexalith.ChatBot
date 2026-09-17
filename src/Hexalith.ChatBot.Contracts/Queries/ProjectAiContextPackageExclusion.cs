namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Metadata-only record of a file excluded from an AI context package.
/// </summary>
public sealed record ProjectAiContextPackageExclusion(
    string ReferenceToken,
    string ReasonCode,
    string? SourceEvidenceReference = null);
