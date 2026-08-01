namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Post-renewal Worker reconciliation and no-loss/no-duplicate/isolation assertions.</summary>
internal sealed record RecoverySubscriptionEndState(
    DateTimeOffset RecoveredAtUtc,
    int DeliveredCount,
    bool NoSilentLoss,
    bool NoDuplicateSideEffects,
    bool TenantIsolationPreserved,
    bool UnauthorizedMutationAbsent);
