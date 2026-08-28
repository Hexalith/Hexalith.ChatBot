using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>One deterministic governed operation reconstructed from one or more WORM envelopes.</summary>
internal sealed record WormOperationGroup(
    string ResourceId,
    string CorrelationId,
    IReadOnlyList<WormAuditChainRecord> Records);
