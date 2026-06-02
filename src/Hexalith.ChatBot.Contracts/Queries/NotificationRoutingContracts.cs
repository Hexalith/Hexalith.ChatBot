using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Metadata-only tenant-scoped notification routing read request. Read-back is summary-safe
/// (roles/channels/state-classes), never recipient PII.
/// </summary>
public sealed record GetNotificationRoutingSummary(
    AdminScope ScopeUsed,
    string ActiveSnapshotRef,
    string CorrelationId);

/// <summary>
/// A summary-safe routing-map row. All fields are declared enum/token values; no recipient PII.
/// </summary>
public sealed record NotificationRoutingSummaryRow(
    string StateClass,
    string Scope,
    string RecipientRole,
    string Channel);

public sealed record NotificationRoutingSummary(
    string ActiveSnapshotRef,
    IReadOnlyList<NotificationRoutingSummaryRow> Rows,
    string RoutingFingerprint,
    long SourceVersion,
    string SchemaVersion,
    string CorrelationId);
