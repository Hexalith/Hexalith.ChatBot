using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public static class ComplianceAdministrationSchema
{
    public const int MaxAuditFilters = 16;
    public const int MaxAuditSearchLimit = 500;
    public const int MinimumRetentionWindowDays = 30;
    public const int MaximumRetentionWindowDays = 3650;
    public const int MinimumAuditRetentionWindowDays = 2555;

    private static readonly IReadOnlySet<string> AuditFilterKeys = new HashSet<string>(
        [
            "tenant",
            "actor",
            "actor-type",
            "command",
            "resource",
            "decision",
            "reason",
            "correlation",
            "policy-snapshot",
            // Story 9.3 (FR56): the surface AC requires querying by message id and by command surface. `message-id`
            // matches the source-message:/provider-message: tokens carried in the audit envelope's source-evidence
            // refs; `surface` matches the envelope's surface origin (api/ui/cli/mcp/worker/mailbox/ai). FilterKey is a
            // free string validated against this set, so adding keys is a backward-compatible v1 change — it widens
            // the accepted set without altering ComplianceAuditFilterRef's wire shape. The matching arms in
            // ComplianceAuditReadPolicy.MatchesFilter MUST stay in lock-step with this set.
            "message-id",
            "surface",
            "time",
        ],
        StringComparer.Ordinal);

    public static bool IsSafeComplianceToken(string? value)
        => TenantPolicySchema.IsSafePolicyToken(value);

    public static bool IsSafeFingerprint(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
            value.StartsWith("sha256:", StringComparison.Ordinal) &&
            value.Length <= 160 &&
            value.Skip("sha256:".Length).All(static character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    public static RetentionValidationResult ValidateRetentionChangeSet(RetentionConfigurationChangeSet? changeSet)
    {
        if (changeSet?.Windows is not { Count: > 0 } windows || windows.Count > ComplianceRetentionClassIds.All.Count)
        {
            return RetentionValidationResult.Invalid("retention_windows_invalid");
        }

        List<string> errors = [];
        HashSet<string> classes = new(StringComparer.Ordinal);
        HashSet<string> refs = new(StringComparer.Ordinal);
        foreach (RetentionWindow window in windows)
        {
            if (!ComplianceRetentionClassIds.All.Contains(window.RetentionClassId) || !classes.Add(window.RetentionClassId))
            {
                errors.Add("retention_class_invalid");
            }

            if (!IsSafeComplianceToken(window.RetentionWindowRef) || !refs.Add(window.RetentionWindowRef))
            {
                errors.Add("retention_window_ref_invalid");
            }

            if (window.WindowDays is < MinimumRetentionWindowDays or > MaximumRetentionWindowDays)
            {
                errors.Add("retention_window_bounds_invalid");
            }

            if (string.Equals(window.RetentionClassId, ComplianceRetentionClassIds.AuditRecords, StringComparison.Ordinal) &&
                window.WindowDays < MinimumAuditRetentionWindowDays)
            {
                errors.Add("audit_retention_window_bounds_invalid");
            }
        }

        return errors.Count == 0
            ? RetentionValidationResult.Valid
            : new RetentionValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    public static RetentionValidationResult ValidateAuditQueryFilters(ComplianceAuditQueryFilters? query)
    {
        if (query is null ||
            !IsSafeComplianceToken(query.QueryRef) ||
            query.Filters is not { Count: > 0 } ||
            query.Filters.Count > MaxAuditFilters ||
            query.Limit is < 1 or > MaxAuditSearchLimit ||
            !IsUtc(query.FromUtc) ||
            !IsUtc(query.ToUtc) ||
            query.ToUtc < query.FromUtc)
        {
            return RetentionValidationResult.Invalid("audit_query_invalid");
        }

        List<string> errors = [];
        HashSet<string> refs = new(StringComparer.Ordinal);
        foreach (ComplianceAuditFilterRef filter in query.Filters)
        {
            if (!IsSafeComplianceToken(filter.FilterRef) || !refs.Add(filter.FilterRef))
            {
                errors.Add("audit_filter_ref_invalid");
            }

            if (!AuditFilterKeys.Contains(filter.FilterKey))
            {
                errors.Add("audit_filter_key_invalid");
            }

            if (!IsSafeComplianceToken(filter.ValueRef))
            {
                errors.Add("audit_filter_value_invalid");
            }
        }

        return errors.Count == 0
            ? RetentionValidationResult.Valid
            : new RetentionValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    public static bool IsValidRedactionState(ComplianceAuditRedactionState state)
        => state is not ComplianceAuditRedactionState.Unknown && Enum.IsDefined(state);

    public static bool IsValidEscalationStatus(ComplianceEscalationStatus status)
        => status is not ComplianceEscalationStatus.Unknown && Enum.IsDefined(status);

    public static bool IsUtc(DateTimeOffset value)
        => value.Offset == TimeSpan.Zero;
}
