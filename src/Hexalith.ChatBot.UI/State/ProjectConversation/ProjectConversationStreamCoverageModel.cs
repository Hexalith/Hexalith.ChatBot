namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationStreamCoverageModel(
    string StateOwnerAggregateId,
    long FromSourceVersion,
    long ThroughSourceVersion,
    bool IsContiguous,
    bool CoversAllKnownItems);
