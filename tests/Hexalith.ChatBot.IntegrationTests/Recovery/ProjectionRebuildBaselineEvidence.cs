using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Bounded metadata-only baseline locators and WORM-history tokens passed from the independent seed oracle to the
/// rebuild driver. It deliberately carries neither source object graphs nor the seed WORM store.
/// </summary>
internal sealed record ProjectionRebuildBaselineEvidence(
    IReadOnlyList<string> SourceIntakeIds,
    IReadOnlyDictionary<string, string> GovernedHistoryTokens,
    string ProjectionSchemaVersion,
    int WormRecordCount,
    int WormOperationCount);
