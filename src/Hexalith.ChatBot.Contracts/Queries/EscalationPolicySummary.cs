using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record EscalationPolicySummary(
    string ActiveSnapshotRef,
    IReadOnlyList<EscalationPolicySummaryRow> Rows,
    string EscalationFingerprint,
    long SourceVersion,
    string SchemaVersion,
    string CorrelationId);
