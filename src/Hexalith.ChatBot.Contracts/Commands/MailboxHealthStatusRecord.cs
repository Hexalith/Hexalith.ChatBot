using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record MailboxHealthStatusRecord(
    string HealthRef,
    string MailboxId,
    MailboxProcessingHealth Health,
    MailboxDegradationReasonCode ReasonCode,
    MailboxPermissionFreshnessState PermissionFreshness,
    string OwnerRole,
    string SafeNextAction,
    string SafeRecoveryText,
    DateTimeOffset ObservedAt);
