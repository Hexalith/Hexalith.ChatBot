using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public static class ComplianceAdministrationSchemaVersions
{
    public const string V1 = "compliance-admin-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

public static class ComplianceRetentionClassIds
{
    public const string SourceEmailMetadata = "source-email-metadata";
    public const string Attachments = "attachments";
    public const string AssociationRecords = "association-records";
    public const string EvidenceSnapshots = "evidence-snapshots";
    public const string ApprovalRecords = "approval-records";
    public const string PolicySnapshots = "policy-snapshots";
    public const string LifecycleState = "lifecycle-state";
    public const string WorkflowLinkMaps = "workflow-link-maps";
    public const string AiPromptsOutputsContext = "ai-prompts-outputs-context";
    public const string LogsSupportBundles = "logs-support-bundles";
    public const string AuditRecords = "audit-records";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(
            [
                SourceEmailMetadata,
                Attachments,
                AssociationRecords,
                EvidenceSnapshots,
                ApprovalRecords,
                PolicySnapshots,
                LifecycleState,
                WorkflowLinkMaps,
                AiPromptsOutputsContext,
                LogsSupportBundles,
                AuditRecords,
            ],
            StringComparer.Ordinal);
}

public sealed record ComplianceAuditFilterRef(
    string FilterRef,
    string FilterKey,
    string ValueRef);

public sealed record ComplianceAuditQueryFilters(
    string QueryRef,
    IReadOnlyList<ComplianceAuditFilterRef> Filters,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int Limit);

public sealed record ComplianceAuditResultRow(
    string AuditRecordRef,
    string ActorRef,
    string ActorType,
    string CommandRef,
    string ResourceRef,
    string Decision,
    string ReasonCode,
    string CorrelationId,
    DateTimeOffset RecordedAtUtc,
    string PolicySnapshotId,
    ComplianceAuditRedactionState RedactionState,
    ComplianceEscalationStatus EscalationStatus,
    string SafeNextAction);

public sealed record ComplianceAuditDetail(
    string AuditRecordRef,
    string CommandRef,
    string ResourceRef,
    string CorrelationId,
    DateTimeOffset RecordedAtUtc,
    string PolicySnapshotId,
    ComplianceAuditRedactionState RedactionState,
    ComplianceEscalationStatus EscalationStatus,
    IReadOnlyList<string> VisibleMetadataRefs,
    string SafeNextAction,
    string RedactionReasonCode);

public sealed record ComplianceInvestigationIntentMetadata(
    string InvestigationId,
    string QueryRef,
    IReadOnlyList<string> FilterRefs,
    string ReasonCode,
    string RequesterRef,
    long SourceVersion,
    string CorrelationId,
    string PolicySnapshotId,
    ComplianceAuditRedactionState RedactionState,
    ComplianceEscalationStatus EscalationStatus);

public sealed record RetentionWindow(
    string RetentionClassId,
    string RetentionWindowRef,
    int WindowDays);

public sealed record RetentionConfigurationChangeSet(
    IReadOnlyList<RetentionWindow> Windows);

public sealed record RetentionSnapshotMetadata(
    string SnapshotId,
    string SchemaVersion,
    string SupersedesSnapshotId,
    string SupersededBySnapshotId,
    string SourceChangeId,
    string ActorRef,
    AdminScope ScopeUsed,
    IReadOnlyList<string> ChangedRetentionClassIds,
    long SourceVersion,
    DateTimeOffset EffectiveAtUtc,
    string CorrelationId,
    string ReasonCode,
    string PolicySnapshotId,
    string OldSnapshotFingerprint,
    string NewSnapshotFingerprint);

public sealed record RetentionValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors)
{
    public static RetentionValidationResult Valid { get; } = new(true, []);

    public static RetentionValidationResult Invalid(params string[] errors)
        => new(false, errors);
}

public sealed record RequestComplianceInvestigation(
    string InvestigationId,
    string QueryRef,
    IReadOnlyList<string> FilterRefs,
    string ReasonCode,
    string RequesterRef,
    long SourceVersion,
    string CorrelationId,
    string PolicySnapshotId,
    ComplianceAuditRedactionState RedactionState,
    ComplianceEscalationStatus EscalationStatus,
    string SchemaVersion) : IChatBotCommand;

public sealed record RequestComplianceEscalation(
    string EscalationId,
    string InvestigationId,
    string AuditRecordRef,
    string ReasonCode,
    string RequesterRef,
    string EscalationTargetRef,
    long SourceVersion,
    string CorrelationId,
    string PolicySnapshotId,
    ComplianceAuditRedactionState RedactionState,
    ComplianceEscalationStatus EscalationStatus,
    string SchemaVersion) : IChatBotCommand;

public sealed record SubmitRetentionConfigurationChange(
    string RetentionChangeId,
    string SourceRetentionSnapshotId,
    string ProposedRetentionSnapshotId,
    long SourceVersion,
    RetentionConfigurationChangeSet ChangeSet,
    string ReasonCode,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId,
    string PolicySnapshotId,
    string OldRetentionSnapshotFingerprint,
    string NewRetentionSnapshotFingerprint,
    DateTimeOffset EffectiveAtUtc) : IChatBotCommand;

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
