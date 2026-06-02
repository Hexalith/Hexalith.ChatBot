using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Projects a governed escalation-policy snapshot into the summary-safe read-back model. Read-back is bounded by the
/// escalation schema: any entry referencing an undeclared state class, scope, severity, target role, or channel is
/// dropped, and an invalid snapshot projects to an empty (denied) summary. Rows expose only declared
/// thresholds/roles/channels/state-classes/severities — never recipient PII. Mirrors
/// <see cref="NotificationRoutingSnapshotProjector"/>.
/// </summary>
internal static class EscalationPolicySnapshotProjector
{
    public static EscalationPolicySummary Create(
        EscalationPolicyChangeSet snapshot,
        string activeSnapshotRef,
        long sourceVersion,
        string escalationFingerprint,
        string correlationId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(activeSnapshotRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        bool valid = EscalationPolicySchema.Validate(snapshot).IsValid &&
            EscalationPolicySchema.IsSafeFingerprint(escalationFingerprint);

        IReadOnlyList<EscalationPolicySummaryRow> rows = valid
            ? snapshot.Entries
                .Where(IsDeclared)
                .Select(static entry => new EscalationPolicySummaryRow(
                    NotificationStateClasses.ToWireValue(entry.StateClass),
                    AdminScopes.ToWireValue(entry.Scope),
                    entry.AgeThresholdSeconds,
                    EscalationSeverities.ToWireValue(entry.SeverityThreshold),
                    AdminRoles.ToWireValue(entry.EscalationTargetRole),
                    NotificationChannels.ToWireValue(entry.EscalationChannel)))
                .OrderBy(static row => row.StateClass, StringComparer.Ordinal)
                .ThenBy(static row => row.Scope, StringComparer.Ordinal)
                .ToArray()
            : [];

        return new EscalationPolicySummary(
            activeSnapshotRef,
            rows,
            valid ? escalationFingerprint : "sha256:denied",
            valid ? Math.Max(0, sourceVersion) : 0,
            EscalationPolicySchemaVersions.V1,
            correlationId);
    }

    private static bool IsDeclared(EscalationPolicyEntry entry)
        => EscalationPolicySchema.EscalatableStateClasses.Contains(entry.StateClass) &&
            AdminScopes.All.Contains(entry.Scope) &&
            EscalationSeverities.All.Contains(entry.SeverityThreshold) &&
            AdminRoles.All.Contains(entry.EscalationTargetRole) &&
            NotificationChannels.All.Contains(entry.EscalationChannel);
}
