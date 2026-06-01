namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Metadata-only source evidence range for a detected task intent.
/// </summary>
public sealed record TaskIntentSourceEvidenceOffset(
    string EvidenceReference,
    int? StartOffset,
    int? EndOffset,
    string? Token);
