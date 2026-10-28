using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.UI.State.GovernedOperations;

/// <summary>Structured, metadata-only audit history read through the governed client.</summary>
/// <param name="AuditStatus">The status returned by the audit-history read.</param>
/// <param name="Entries">The immutable entry metadata in server record order.</param>
public sealed record OperationAuditHistoryModel(
    OperationAuditStatus AuditStatus,
    IReadOnlyList<OperationAuditEntryModel> Entries);
