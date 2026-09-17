namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationAiSummaryProvenanceModel(
    string GeneratedBy,
    DateTimeOffset? GeneratedAtUtc,
    IReadOnlyList<string> SourceEvidenceIds,
    string? ContextPackageId,
    string? ContextPackageVersion,
    string RedactionState);
