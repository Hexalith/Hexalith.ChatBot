using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Projects a governed notification-routing snapshot into the summary-safe read-back model. Read-back is bounded by
/// the routing schema: any entry referencing an undeclared state class, scope, recipient role, or channel is
/// dropped, and an invalid snapshot projects to an empty (denied) summary. Rows expose only declared
/// roles/channels/state-classes — never recipient PII.
/// </summary>
internal static class NotificationRoutingSnapshotProjector
{
    public static NotificationRoutingSummary Create(
        NotificationRoutingChangeSet snapshot,
        string activeSnapshotRef,
        long sourceVersion,
        string routingFingerprint,
        string correlationId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(activeSnapshotRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        bool valid = NotificationRoutingSchema.Validate(snapshot).IsValid &&
            NotificationRoutingSchema.IsSafeFingerprint(routingFingerprint);

        IReadOnlyList<NotificationRoutingSummaryRow> rows = valid
            ? snapshot.Entries
                .Where(IsDeclared)
                .Select(static entry => new NotificationRoutingSummaryRow(
                    NotificationStateClasses.ToWireValue(entry.StateClass),
                    AdminScopes.ToWireValue(entry.Scope),
                    AdminRoles.ToWireValue(entry.RecipientRole),
                    NotificationChannels.ToWireValue(entry.Channel)))
                .OrderBy(static row => row.StateClass, StringComparer.Ordinal)
                .ThenBy(static row => row.Scope, StringComparer.Ordinal)
                .ToArray()
            : [];

        return new NotificationRoutingSummary(
            activeSnapshotRef,
            rows,
            valid ? routingFingerprint : "sha256:denied",
            valid ? Math.Max(0, sourceVersion) : 0,
            NotificationRoutingSchemaVersions.V1,
            correlationId);
    }

    private static bool IsDeclared(NotificationRoutingEntry entry)
        => NotificationStateClasses.All.Contains(entry.StateClass) &&
            AdminScopes.All.Contains(entry.Scope) &&
            AdminRoles.All.Contains(entry.RecipientRole) &&
            NotificationChannels.All.Contains(entry.Channel);
}
