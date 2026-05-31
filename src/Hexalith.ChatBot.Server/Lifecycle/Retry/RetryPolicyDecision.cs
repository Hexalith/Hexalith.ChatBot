namespace Hexalith.ChatBot.Server.Lifecycle.Retry;

internal sealed record RetryPolicyDecision(
    string ReasonCode,
    bool IsRetryable,
    bool IsExhausted,
    int RetryCount,
    int MaxAttempts,
    DateTimeOffset? NextRetryAt,
    string OwnerRole,
    string TerminalReasonCode,
    string SafeNextAction,
    string ManualRecoveryAction);
