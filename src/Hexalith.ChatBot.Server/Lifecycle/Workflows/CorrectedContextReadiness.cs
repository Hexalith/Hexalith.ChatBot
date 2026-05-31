namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed record CorrectedContextReadiness(
    bool IsUsable,
    string Status,
    string ReasonCode,
    IReadOnlyList<string> PendingStoreKeys);
