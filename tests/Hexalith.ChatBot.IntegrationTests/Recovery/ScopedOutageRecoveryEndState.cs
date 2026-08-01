namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Metadata-only end-state assertions captured after dependency restoration.</summary>
internal sealed record ScopedOutageRecoveryEndState(
    bool AffectedOperationRecovered,
    bool CrossTenantLeakageDetected,
    bool SilentDataLossDetected,
    bool DuplicateSideEffectDetected);
