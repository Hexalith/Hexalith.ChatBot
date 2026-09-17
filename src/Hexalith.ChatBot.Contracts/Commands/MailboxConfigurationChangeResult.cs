using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record MailboxConfigurationChangeResult(
    bool Accepted,
    string ConfigurationChangeId,
    string ActiveSnapshotRef,
    string ReasonCode);
