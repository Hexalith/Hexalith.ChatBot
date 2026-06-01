using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record ProjectConversationItemClassification(
    ProjectConversationClassificationKind Kind,
    string KernelVersion,
    double ConfidenceScore,
    string MessageCode,
    IReadOnlyList<string> SourceEvidenceIds,
    string RedactionState);
