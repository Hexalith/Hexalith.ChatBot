using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record MailboxConfigurationChangeSet(
    IReadOnlyList<MonitoredMailboxPattern> MonitoredPatterns,
    IReadOnlyList<MailboxRoutingRule> RoutingRules,
    IReadOnlyList<MailboxProviderConnectionMetadata> ProviderConnections,
    IReadOnlyList<MailboxPermissionStatus> PermissionStatuses);
