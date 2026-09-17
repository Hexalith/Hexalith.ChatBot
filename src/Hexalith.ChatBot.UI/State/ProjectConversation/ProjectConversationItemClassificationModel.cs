namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationItemClassificationModel(
    string Kind,
    string KernelVersion,
    double ConfidenceScore,
    string MessageCode,
    IReadOnlyList<string> SourceEvidenceIds,
    string RedactionState);
