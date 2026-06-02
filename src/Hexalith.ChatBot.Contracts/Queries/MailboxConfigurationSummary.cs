using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Metadata-only mailbox configuration summary for S5 mailbox administration.
/// </summary>
public sealed record MailboxConfigurationSummary(
    string ActiveSnapshotRef,
    string SchemaVersion,
    IReadOnlyList<MonitoredMailboxPattern> MonitoredPatterns,
    IReadOnlyList<MailboxRoutingRule> RoutingRules,
    IReadOnlyList<MailboxProviderConnectionMetadata> ProviderConnections,
    IReadOnlyList<MailboxHealthStatusRecord> Health,
    string SummaryFreshness,
    string CorrelationId);
