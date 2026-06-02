using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public static class NotificationRoutingSchemaVersions
{
    public const string V1 = "notification-routing-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// A single closed routing-map entry: the recipient role and delivery channel that hear about a
/// <c>(state-class × scope)</c> pair. Keys and values are finite enums/tokens only — never free-form strings.
/// </summary>
public sealed record NotificationRoutingEntry(
    NotificationStateClass StateClass,
    AdminScope Scope,
    AdminRole RecipientRole,
    NotificationChannel Channel);

/// <summary>
/// The closed, typed routing map: a set of <c>(state-class × scope) → { recipient-role, channel }</c> entries.
/// </summary>
public sealed record NotificationRoutingChangeSet(
    IReadOnlyList<NotificationRoutingEntry> Entries);

public sealed record NotificationRoutingSnapshotMetadata(
    string SnapshotId,
    string SchemaVersion,
    string SupersedesSnapshotId,
    string SourceChangeId,
    string ActorRef,
    AdminScope ScopeUsed,
    IReadOnlyList<string> ChangedRouteKeys,
    string SourceVersion,
    DateTimeOffset Timestamp,
    string CorrelationId,
    string ReasonCode,
    string RoutingFingerprint);

public sealed record NotificationRoutingChangeResult(
    bool Accepted,
    string RoutingChangeId,
    string ActiveSnapshotRef,
    string ReasonCode);

public sealed record NotificationRoutingValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors)
{
    public static NotificationRoutingValidationResult Valid { get; } = new(true, []);

    public static NotificationRoutingValidationResult Invalid(params string[] errors)
        => new(false, errors);
}

/// <summary>
/// Closed, Tenant-Policy-Schema-bounded routing schema. State classes, scopes, recipient roles, and channels are
/// finite declared sets; values outside the declared types are rejected with a safe reason code (FR73, FR75d).
/// </summary>
public static class NotificationRoutingSchema
{
    public const int MaxEntries = 64;

    public static string RouteKey(NotificationStateClass stateClass, AdminScope scope)
        => $"{NotificationStateClasses.ToWireValue(stateClass)}:{AdminScopes.ToWireValue(scope)}";

    public static NotificationRoutingValidationResult Validate(NotificationRoutingChangeSet? changeSet)
    {
        if (changeSet?.Entries is null || changeSet.Entries.Count == 0 || changeSet.Entries.Count > MaxEntries)
        {
            return NotificationRoutingValidationResult.Invalid("notification_routing_entries_invalid");
        }

        List<string> errors = [];
        HashSet<string> keys = new(StringComparer.Ordinal);
        foreach (NotificationRoutingEntry entry in changeSet.Entries)
        {
            if (!Enum.IsDefined(entry.StateClass) || !NotificationStateClasses.All.Contains(entry.StateClass))
            {
                errors.Add("notification_routing_state_class_invalid");
            }

            if (!Enum.IsDefined(entry.Scope) || !AdminScopes.All.Contains(entry.Scope))
            {
                errors.Add("notification_routing_scope_invalid");
            }

            if (!Enum.IsDefined(entry.RecipientRole) || !AdminRoles.All.Contains(entry.RecipientRole))
            {
                errors.Add("notification_routing_recipient_role_invalid");
            }

            if (!Enum.IsDefined(entry.Channel) || !NotificationChannels.All.Contains(entry.Channel))
            {
                errors.Add("notification_routing_channel_invalid");
            }

            // Closed map: each (state-class × scope) key may appear at most once.
            if (Enum.IsDefined(entry.StateClass) && Enum.IsDefined(entry.Scope) &&
                !keys.Add(RouteKey(entry.StateClass, entry.Scope)))
            {
                errors.Add("notification_routing_duplicate_key");
            }
        }

        return errors.Count == 0
            ? NotificationRoutingValidationResult.Valid
            : new NotificationRoutingValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    public static bool IsSafeRoutingToken(string? value)
        => TenantPolicySchema.IsSafePolicyToken(value);

    public static bool IsSafeFingerprint(string? value)
        => MailboxConfigurationSchema.IsSafeFingerprint(value);
}
