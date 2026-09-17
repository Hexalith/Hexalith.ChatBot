using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Closed, Tenant-Policy-Schema-bounded escalation schema. State classes are restricted to the five escalatable
/// classes (<c>retry</c> is transient and deliberately excluded); scopes, severities, target roles, and channels are
/// finite declared sets; age thresholds are non-negative integers within declared bounds; values outside the declared
/// types/ranges are rejected with a safe reason code (FR73, FR75d). Mirrors <see cref="NotificationRoutingSchema"/>.
/// </summary>
public static class EscalationPolicySchema
{
    public const int MaxEntries = 64;

    /// <summary>One-year ceiling on configurable age thresholds; keeps the bounded knob from becoming effectively infinite.</summary>
    public const int MaxAgeThresholdSeconds = 365 * 24 * 60 * 60;

    /// <summary>
    /// The five escalatable state classes named in the epic AC (<c>review-needed</c>, <c>approval-pending</c>,
    /// <c>failure</c>, <c>degraded</c>, <c>quarantine</c>). <c>retry</c> is a transient state handled by the
    /// retry/backoff path and is deliberately excluded from escalation.
    /// </summary>
    public static IReadOnlySet<NotificationStateClass> EscalatableStateClasses { get; } =
        new HashSet<NotificationStateClass>
        {
            NotificationStateClass.ReviewNeeded,
            NotificationStateClass.ApprovalPending,
            NotificationStateClass.Failure,
            NotificationStateClass.Degraded,
            NotificationStateClass.Quarantine,
        };

    public static string PolicyKey(NotificationStateClass stateClass, AdminScope scope)
        => $"{NotificationStateClasses.ToWireValue(stateClass)}:{AdminScopes.ToWireValue(scope)}";

    public static EscalationPolicyValidationResult Validate(EscalationPolicyChangeSet? changeSet)
    {
        if (changeSet?.Entries is null || changeSet.Entries.Count == 0 || changeSet.Entries.Count > MaxEntries)
        {
            return EscalationPolicyValidationResult.Invalid("escalation_policy_entries_invalid");
        }

        List<string> errors = [];
        HashSet<string> keys = new(StringComparer.Ordinal);
        foreach (EscalationPolicyEntry entry in changeSet.Entries)
        {
            if (!Enum.IsDefined(entry.StateClass) || !EscalatableStateClasses.Contains(entry.StateClass))
            {
                errors.Add("escalation_policy_state_class_invalid");
            }

            if (!Enum.IsDefined(entry.Scope) || !AdminScopes.All.Contains(entry.Scope))
            {
                errors.Add("escalation_policy_scope_invalid");
            }

            if (!Enum.IsDefined(entry.SeverityThreshold) || !EscalationSeverities.All.Contains(entry.SeverityThreshold))
            {
                errors.Add("escalation_policy_severity_invalid");
            }

            if (!Enum.IsDefined(entry.EscalationTargetRole) || !AdminRoles.All.Contains(entry.EscalationTargetRole))
            {
                errors.Add("escalation_policy_target_role_invalid");
            }

            if (!Enum.IsDefined(entry.EscalationChannel) || !NotificationChannels.All.Contains(entry.EscalationChannel))
            {
                errors.Add("escalation_policy_channel_invalid");
            }

            if (entry.AgeThresholdSeconds < 0 || entry.AgeThresholdSeconds > MaxAgeThresholdSeconds)
            {
                errors.Add("escalation_policy_age_threshold_invalid");
            }

            // Closed map: each (state-class × scope) key may appear at most once.
            if (Enum.IsDefined(entry.StateClass) && Enum.IsDefined(entry.Scope) &&
                !keys.Add(PolicyKey(entry.StateClass, entry.Scope)))
            {
                errors.Add("escalation_policy_duplicate_key");
            }
        }

        return errors.Count == 0
            ? EscalationPolicyValidationResult.Valid
            : new EscalationPolicyValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    public static bool IsSafeEscalationToken(string? value)
        => TenantPolicySchema.IsSafePolicyToken(value);

    public static bool IsSafeFingerprint(string? value)
        => MailboxConfigurationSchema.IsSafeFingerprint(value);
}
