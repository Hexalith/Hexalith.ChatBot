using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public static class TenantPolicySchemaVersions
{
    public const string M0 = "tenant-policy-schema.m0.v1";
    public const string M1Preview = "tenant-policy-schema.m1-preview.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([M0, M1Preview], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

public static class TenantPolicyKnobIds
{
    public const string AssociationTHigh = "association.t-high";
    public const string AssociationTLow = "association.t-low";
    public const string AttachmentsUnsafeHandling = "attachments.unsafe-handling";
    public const string AiActionLowRiskAllowed = "ai-action.low-risk-allowed";
    public const string MailboxRoutingRules = "mailbox.routing-rules";
    public const string ApprovalRouting = "approval.routing";
    public const string AdminPermissionScopes = "admin.permission-scopes";
    public const string AllowlistVersionPin = "allowlist.version-pin";
    public const string ClassifierExplanationLayerEnabled = "classifier.explanation-layer-enabled";
    public const string InboundAuthenticityStrictness = "inbound-authenticity.strictness";
    public const string ApprovalPriorityWeights = "approval.priority-weights";
    public const string NotificationThrottleCeilings = "notification.throttle-ceilings";
    public const string ReviewerBacklogThreshold = "notification.reviewer-backlog-threshold";
}

public static class TenantPolicyUnsafeAttachmentHandling
{
    public const string Quarantine = "quarantine";
    public const string Block = "block";
    public const string RejectMessage = "reject-message";

    public static IReadOnlyList<string> All { get; } = [Quarantine, Block, RejectMessage];
}

public sealed record TenantPolicyKnobDefinition(
    string KnobId,
    TenantPolicyKnobType Type,
    TenantPolicyKnobSensitivity Sensitivity,
    string SchemaVersion,
    double? Minimum = null,
    double? Maximum = null,
    IReadOnlyList<string>? EnumValues = null);

public sealed record TenantPolicyValue(
    string KnobId,
    double? NumberValue = null,
    string? StringValue = null,
    bool? BoolValue = null,
    IReadOnlyDictionary<AiActionRiskActionClass, bool>? AiActionLowRiskAllowed = null,
    IReadOnlyList<string>? StringListValue = null,
    IReadOnlyList<AdminScope>? AdminScopesValue = null,
    ApprovalPriorityWeights? ApprovalPriorityWeightsValue = null,
    NotificationThrottleCeilings? NotificationThrottleCeilingsValue = null,
    ReviewerBacklogThreshold? ReviewerBacklogThresholdValue = null);

public sealed record TenantPolicyChangeSet(
    IReadOnlyList<TenantPolicyValue> Values);

public sealed record TenantPolicySnapshotMetadata(
    string SnapshotId,
    string SchemaVersion,
    string SupersedesSnapshotId,
    string SourceChangeId,
    string ActorRef,
    AdminScope ScopeUsed,
    IReadOnlyList<string> ChangedKnobIds,
    string SourceVersion,
    DateTimeOffset Timestamp,
    string CorrelationId,
    string ReasonCode,
    string PolicySnapshotId);

public sealed record TenantPolicyValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors)
{
    public static TenantPolicyValidationResult Valid { get; } = new(true, []);

    public static TenantPolicyValidationResult Invalid(params string[] errors)
        => new(false, errors);
}

public static class TenantPolicySchema
{
    private static readonly IReadOnlyDictionary<string, TenantPolicyKnobDefinition> DefinitionMap = BuildDefinitions()
        .ToDictionary(static definition => definition.KnobId, StringComparer.Ordinal);

    public static IReadOnlyList<AiActionRiskActionClass> RequiredAiActionClasses { get; } =
    [
        AiActionRiskActionClass.ModifiesState,
        AiActionRiskActionClass.ExposesFiles,
        AiActionRiskActionClass.SendsExternal,
        AiActionRiskActionClass.CreatesTasks,
        AiActionRiskActionClass.InvokesTools,
        AiActionRiskActionClass.ActsOnBehalf,
    ];

    public static IReadOnlyList<TenantPolicyKnobDefinition> Definitions { get; } = DefinitionMap.Values
        .OrderBy(static definition => definition.KnobId, StringComparer.Ordinal)
        .ToArray();

    public static IReadOnlyList<TenantPolicyValue> DefaultM0Values { get; } =
    [
        new(TenantPolicyKnobIds.AssociationTHigh, NumberValue: 0.90),
        new(TenantPolicyKnobIds.AssociationTLow, NumberValue: 0.60),
        new(TenantPolicyKnobIds.AttachmentsUnsafeHandling, StringValue: TenantPolicyUnsafeAttachmentHandling.Quarantine),
        new(TenantPolicyKnobIds.AiActionLowRiskAllowed, AiActionLowRiskAllowed: RequiredAiActionClasses.ToDictionary(static value => value, static _ => false)),
        new(TenantPolicyKnobIds.MailboxRoutingRules, StringListValue: []),
    ];

    public static bool TryGetDefinition(string? knobId, out TenantPolicyKnobDefinition definition)
        => DefinitionMap.TryGetValue(knobId ?? string.Empty, out definition!);

    public static bool IsSensitive(string knobId)
        => TryGetDefinition(knobId, out TenantPolicyKnobDefinition definition) &&
            definition.Sensitivity is TenantPolicyKnobSensitivity.SecuritySensitive;

    public static TenantPolicyValidationResult Validate(TenantPolicyChangeSet? changeSet)
    {
        if (changeSet?.Values is null || changeSet.Values.Count == 0)
        {
            return TenantPolicyValidationResult.Invalid("policy_change_set_empty");
        }

        List<string> errors = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        Dictionary<string, TenantPolicyValue> values = new(StringComparer.Ordinal);
        foreach (TenantPolicyValue value in changeSet.Values)
        {
            if (!IsSafePolicyToken(value.KnobId) || !seen.Add(value.KnobId))
            {
                errors.Add("policy_knob_id_invalid");
                continue;
            }

            if (!DefinitionMap.TryGetValue(value.KnobId, out TenantPolicyKnobDefinition? definition))
            {
                errors.Add($"unknown_knob:{value.KnobId}");
                continue;
            }

            values[value.KnobId] = value;
            errors.AddRange(ValidateValue(definition, value, values));
        }

        return errors.Count == 0
            ? TenantPolicyValidationResult.Valid
            : new TenantPolicyValidationResult(false, errors);
    }

    public static bool IsSafePolicyToken(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
            value.Length <= 160 &&
            value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or ':');

    private static IEnumerable<TenantPolicyKnobDefinition> BuildDefinitions()
    {
        yield return new(TenantPolicyKnobIds.AssociationTHigh, TenantPolicyKnobType.Double, TenantPolicyKnobSensitivity.SecuritySensitive, TenantPolicySchemaVersions.M0, 0.80, 1.00);
        yield return new(TenantPolicyKnobIds.AssociationTLow, TenantPolicyKnobType.Double, TenantPolicyKnobSensitivity.SecuritySensitive, TenantPolicySchemaVersions.M0, 0.50, 1.00);
        yield return new(TenantPolicyKnobIds.AttachmentsUnsafeHandling, TenantPolicyKnobType.Enum, TenantPolicyKnobSensitivity.Standard, TenantPolicySchemaVersions.M0, EnumValues: TenantPolicyUnsafeAttachmentHandling.All);
        yield return new(TenantPolicyKnobIds.AiActionLowRiskAllowed, TenantPolicyKnobType.AiActionLowRiskMap, TenantPolicyKnobSensitivity.SecuritySensitive, TenantPolicySchemaVersions.M0);
        yield return new(TenantPolicyKnobIds.MailboxRoutingRules, TenantPolicyKnobType.StringList, TenantPolicyKnobSensitivity.Standard, TenantPolicySchemaVersions.M0);
        yield return new(TenantPolicyKnobIds.ApprovalRouting, TenantPolicyKnobType.String, TenantPolicyKnobSensitivity.SecuritySensitive, TenantPolicySchemaVersions.M1Preview);
        yield return new(TenantPolicyKnobIds.AdminPermissionScopes, TenantPolicyKnobType.AdminScopeList, TenantPolicyKnobSensitivity.SecuritySensitive, TenantPolicySchemaVersions.M1Preview);
        yield return new(TenantPolicyKnobIds.AllowlistVersionPin, TenantPolicyKnobType.String, TenantPolicyKnobSensitivity.SecuritySensitive, TenantPolicySchemaVersions.M1Preview);
        yield return new(TenantPolicyKnobIds.ClassifierExplanationLayerEnabled, TenantPolicyKnobType.Boolean, TenantPolicyKnobSensitivity.SecuritySensitive, TenantPolicySchemaVersions.M1Preview);
        yield return new(TenantPolicyKnobIds.InboundAuthenticityStrictness, TenantPolicyKnobType.Enum, TenantPolicyKnobSensitivity.SecuritySensitive, TenantPolicySchemaVersions.M1Preview, EnumValues: ["permissive", "strict", "paranoid"]);

        // Story 7.8: standard triage-tuning knob (not security-sensitive — no blanket two-person rule). Lives in the
        // M1 set alongside `approval.routing`. The closed weight-set shape (exactly three declared dimensions, each
        // bounded by ApprovalPriorityWeights.Minimum/Maximum) is enforced in ValidateApprovalPriorityWeights.
        yield return new(TenantPolicyKnobIds.ApprovalPriorityWeights, TenantPolicyKnobType.ApprovalPriorityWeights, TenantPolicyKnobSensitivity.Standard, TenantPolicySchemaVersions.M1Preview, Commands.ApprovalPriorityWeights.MinimumWeight, Commands.ApprovalPriorityWeights.MaximumWeight);

        // Story 7.9: standard triage-tuning knob (not security-sensitive — no blanket two-person rule). Lives in the
        // M1 set alongside `approval.routing`/`approval.priority-weights`. The closed ceiling-set shape (exactly two
        // declared window dimensions, each bounded by NotificationThrottleCeilings.Minimum/Hourly|DailyMaximum) is
        // enforced in ValidateNotificationThrottleCeilings. The NFR46 maximum is a hard cap a tenant may only lower.
        yield return new(TenantPolicyKnobIds.NotificationThrottleCeilings, TenantPolicyKnobType.NotificationThrottleCeilings, TenantPolicyKnobSensitivity.Standard, TenantPolicySchemaVersions.M1Preview, Commands.NotificationThrottleCeilings.Minimum, Commands.NotificationThrottleCeilings.HourlyMaximum);

        // Story 7.10: standard triage-tuning knob (not security-sensitive — no blanket two-person rule). Lives in the
        // M1 set alongside the other anti-fatigue knobs. The closed single-dimension shape (exactly the
        // ReviewerBacklogThreshold record, bounded by Minimum/Maximum) is enforced in ValidateReviewerBacklogThreshold.
        // The NFR46 maximum (25) is a hard cap a tenant may only lower — never raise above (which would hide a backlog).
        yield return new(TenantPolicyKnobIds.ReviewerBacklogThreshold, TenantPolicyKnobType.ReviewerBacklogThreshold, TenantPolicyKnobSensitivity.Standard, TenantPolicySchemaVersions.M1Preview, Commands.ReviewerBacklogThreshold.Minimum, Commands.ReviewerBacklogThreshold.Maximum);
    }

    private static IEnumerable<string> ValidateValue(
        TenantPolicyKnobDefinition definition,
        TenantPolicyValue value,
        IReadOnlyDictionary<string, TenantPolicyValue> values)
        => definition.Type switch
        {
            TenantPolicyKnobType.Double => ValidateDouble(definition, value, values),
            TenantPolicyKnobType.Enum => ValidateEnum(definition, value),
            TenantPolicyKnobType.Boolean => ValidateBoolean(value),
            TenantPolicyKnobType.String => ValidateString(value),
            TenantPolicyKnobType.StringList => ValidateStringList(value),
            TenantPolicyKnobType.AdminScopeList => ValidateAdminScopes(value),
            TenantPolicyKnobType.AiActionLowRiskMap => ValidateAiActionLowRiskMap(value),
            TenantPolicyKnobType.ApprovalPriorityWeights => ValidateApprovalPriorityWeights(definition, value),
            TenantPolicyKnobType.NotificationThrottleCeilings => ValidateNotificationThrottleCeilings(definition, value),
            TenantPolicyKnobType.ReviewerBacklogThreshold => ValidateReviewerBacklogThreshold(definition, value),
            _ => ["policy_knob_type_invalid"],
        };

    private static IEnumerable<string> ValidateNotificationThrottleCeilings(
        TenantPolicyKnobDefinition definition,
        TenantPolicyValue value)
    {
        // Closed ceiling set: exactly the NotificationThrottleCeilings record — no other value field may be set, and no
        // extra window dimension can be introduced. Above-maximum/out-of-range ceilings are rejected with the existing
        // safe codes so a tenant can only lower the NFR46 cap, never raise it.
        if (value.NotificationThrottleCeilingsValue is not { } ceilings || value.NumberValue is not null ||
            value.StringValue is not null || value.BoolValue is not null || value.AiActionLowRiskAllowed is not null ||
            value.StringListValue is not null || value.AdminScopesValue is not null || value.ApprovalPriorityWeightsValue is not null ||
            value.ReviewerBacklogThresholdValue is not null)
        {
            yield return $"wrong_value_type:{definition.KnobId}";
            yield break;
        }

        if (!ceilings.IsWithinBounds)
        {
            yield return $"range_invalid:{definition.KnobId}";
        }
    }

    private static IEnumerable<string> ValidateReviewerBacklogThreshold(
        TenantPolicyKnobDefinition definition,
        TenantPolicyValue value)
    {
        // Closed single-dimension threshold: exactly the ReviewerBacklogThreshold record — no other value field may be
        // set, and no extra dimension can be introduced. Above-maximum/out-of-range thresholds are rejected with the
        // existing safe codes so a tenant can only lower the NFR46 cap (alert sooner), never raise it above 25.
        if (value.ReviewerBacklogThresholdValue is not { } threshold || value.NumberValue is not null ||
            value.StringValue is not null || value.BoolValue is not null || value.AiActionLowRiskAllowed is not null ||
            value.StringListValue is not null || value.AdminScopesValue is not null || value.ApprovalPriorityWeightsValue is not null ||
            value.NotificationThrottleCeilingsValue is not null)
        {
            yield return $"wrong_value_type:{definition.KnobId}";
            yield break;
        }

        if (!threshold.IsWithinBounds)
        {
            yield return $"range_invalid:{definition.KnobId}";
        }
    }

    private static IEnumerable<string> ValidateApprovalPriorityWeights(
        TenantPolicyKnobDefinition definition,
        TenantPolicyValue value)
    {
        // Closed weight set: exactly the ApprovalPriorityWeights record — no other value field may be set, and no extra
        // dimension can be introduced. NaN/Infinity/out-of-range weights are rejected with the existing safe codes.
        if (value.ApprovalPriorityWeightsValue is not { } weights || value.NumberValue is not null ||
            value.StringValue is not null || value.BoolValue is not null || value.AiActionLowRiskAllowed is not null ||
            value.StringListValue is not null || value.AdminScopesValue is not null || value.NotificationThrottleCeilingsValue is not null ||
            value.ReviewerBacklogThresholdValue is not null)
        {
            yield return $"wrong_value_type:{definition.KnobId}";
            yield break;
        }

        if (!weights.IsWithinBounds)
        {
            yield return $"range_invalid:{definition.KnobId}";
        }
    }

    private static IEnumerable<string> ValidateDouble(
        TenantPolicyKnobDefinition definition,
        TenantPolicyValue value,
        IReadOnlyDictionary<string, TenantPolicyValue> values)
    {
        if (value.NumberValue is not { } number || double.IsNaN(number) || double.IsInfinity(number) ||
            value.StringValue is not null || value.BoolValue is not null || value.AiActionLowRiskAllowed is not null ||
            value.StringListValue is not null || value.AdminScopesValue is not null || value.ApprovalPriorityWeightsValue is not null || value.NotificationThrottleCeilingsValue is not null ||
            value.ReviewerBacklogThresholdValue is not null)
        {
            yield return $"wrong_value_type:{definition.KnobId}";
            yield break;
        }

        if (definition.Minimum is { } minimum && number < minimum ||
            definition.Maximum is { } maximum && number > maximum)
        {
            yield return $"range_invalid:{definition.KnobId}";
        }

        if (string.Equals(definition.KnobId, TenantPolicyKnobIds.AssociationTLow, StringComparison.Ordinal) &&
            values.TryGetValue(TenantPolicyKnobIds.AssociationTHigh, out TenantPolicyValue? high) &&
            high.NumberValue is { } highValue &&
            number >= highValue)
        {
            yield return "range_invalid:association.t-low";
        }
    }

    private static IEnumerable<string> ValidateEnum(TenantPolicyKnobDefinition definition, TenantPolicyValue value)
    {
        if (value.StringValue is null || value.NumberValue is not null || value.BoolValue is not null ||
            value.AiActionLowRiskAllowed is not null || value.StringListValue is not null || value.AdminScopesValue is not null ||
            value.ApprovalPriorityWeightsValue is not null || value.NotificationThrottleCeilingsValue is not null ||
            value.ReviewerBacklogThresholdValue is not null)
        {
            yield return $"wrong_value_type:{definition.KnobId}";
            yield break;
        }

        if (definition.EnumValues is null || !definition.EnumValues.Contains(value.StringValue, StringComparer.Ordinal))
        {
            yield return $"enum_invalid:{definition.KnobId}";
        }
    }

    private static IEnumerable<string> ValidateBoolean(TenantPolicyValue value)
    {
        if (value.BoolValue is null || value.NumberValue is not null || value.StringValue is not null ||
            value.AiActionLowRiskAllowed is not null || value.StringListValue is not null || value.AdminScopesValue is not null ||
            value.ApprovalPriorityWeightsValue is not null || value.NotificationThrottleCeilingsValue is not null ||
            value.ReviewerBacklogThresholdValue is not null)
        {
            yield return $"wrong_value_type:{value.KnobId}";
        }
    }

    private static IEnumerable<string> ValidateString(TenantPolicyValue value)
    {
        if (value.StringValue is null || !IsSafePolicyToken(value.StringValue) || value.NumberValue is not null ||
            value.BoolValue is not null || value.AiActionLowRiskAllowed is not null || value.StringListValue is not null ||
            value.AdminScopesValue is not null || value.ApprovalPriorityWeightsValue is not null || value.NotificationThrottleCeilingsValue is not null ||
            value.ReviewerBacklogThresholdValue is not null)
        {
            yield return $"wrong_value_type:{value.KnobId}";
        }
    }

    private static IEnumerable<string> ValidateStringList(TenantPolicyValue value)
    {
        if (value.StringListValue is null || value.NumberValue is not null || value.StringValue is not null ||
            value.BoolValue is not null || value.AiActionLowRiskAllowed is not null || value.AdminScopesValue is not null ||
            value.ApprovalPriorityWeightsValue is not null || value.NotificationThrottleCeilingsValue is not null ||
            value.ReviewerBacklogThresholdValue is not null ||
            value.StringListValue.Count > 64 || !value.StringListValue.All(IsSafePolicyToken))
        {
            yield return $"wrong_value_type:{value.KnobId}";
        }
    }

    private static IEnumerable<string> ValidateAdminScopes(TenantPolicyValue value)
    {
        if (value.AdminScopesValue is null || value.NumberValue is not null || value.StringValue is not null ||
            value.BoolValue is not null || value.AiActionLowRiskAllowed is not null || value.StringListValue is not null ||
            value.ApprovalPriorityWeightsValue is not null || value.NotificationThrottleCeilingsValue is not null ||
            value.ReviewerBacklogThresholdValue is not null ||
            value.AdminScopesValue.Count == 0 || value.AdminScopesValue.Count > AdminScopes.All.Count ||
            value.AdminScopesValue.Distinct().Count() != value.AdminScopesValue.Count ||
            !value.AdminScopesValue.All(AdminScopes.All.Contains))
        {
            yield return $"wrong_value_type:{value.KnobId}";
        }
    }

    private static IEnumerable<string> ValidateAiActionLowRiskMap(TenantPolicyValue value)
    {
        if (value.AiActionLowRiskAllowed is null || value.NumberValue is not null || value.StringValue is not null ||
            value.BoolValue is not null || value.StringListValue is not null || value.AdminScopesValue is not null ||
            value.ApprovalPriorityWeightsValue is not null || value.NotificationThrottleCeilingsValue is not null ||
            value.ReviewerBacklogThresholdValue is not null)
        {
            yield return $"wrong_value_type:{value.KnobId}";
            yield break;
        }

        IReadOnlyList<AiActionRiskActionClass> keys = value.AiActionLowRiskAllowed.Keys.ToArray();
        if (keys.Count != RequiredAiActionClasses.Count ||
            keys.Distinct().Count() != RequiredAiActionClasses.Count ||
            !RequiredAiActionClasses.All(required => value.AiActionLowRiskAllowed.ContainsKey(required)))
        {
            yield return "ai_action_low_risk_map_invalid";
        }
    }
}
