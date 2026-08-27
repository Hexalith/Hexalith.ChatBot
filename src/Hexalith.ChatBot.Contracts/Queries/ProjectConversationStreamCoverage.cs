namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>Authoritative contiguous event coverage for one lifecycle-owning aggregate stream.</summary>
public sealed record ProjectConversationStreamCoverage(
    string StateOwnerAggregateId,
    long FirstSourceVersion,
    long LastSourceVersion,
    bool IsContiguous,
    bool CoversAllKnownEvents);
