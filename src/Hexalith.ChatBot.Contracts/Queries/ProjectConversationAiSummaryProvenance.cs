namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record ProjectConversationAiSummaryProvenance(
    string GeneratedBy,
    DateTimeOffset? GeneratedAtUtc,
    IReadOnlyList<string> SourceEvidenceIds,
    string? ContextPackageId,
    string? ContextPackageVersion,
    string RedactionState);
