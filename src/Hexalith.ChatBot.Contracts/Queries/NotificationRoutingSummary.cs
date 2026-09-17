using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record NotificationRoutingSummary(
    string ActiveSnapshotRef,
    IReadOnlyList<NotificationRoutingSummaryRow> Rows,
    string RoutingFingerprint,
    long SourceVersion,
    string SchemaVersion,
    string CorrelationId);
