using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public static class MailboxConfigurationSchema
{
    public const int MaxPatterns = 64;
    public const int MaxRoutingRules = 64;
    public const int MaxProviderConnections = 32;
    public const string LeastPrivilegeInboundPermission = "Mail.Read";

    public static MailboxConfigurationValidationResult Validate(MailboxConfigurationChangeSet? changeSet)
    {
        if (changeSet is null)
        {
            return MailboxConfigurationValidationResult.Invalid("mailbox_configuration_empty");
        }

        List<string> errors = [];
        ValidatePatterns(changeSet.MonitoredPatterns, errors);
        ValidateRoutingRules(changeSet.RoutingRules, errors);
        ValidateProviderConnections(changeSet.ProviderConnections, errors);
        ValidatePermissionStatuses(changeSet.PermissionStatuses, errors);

        return errors.Count == 0
            ? MailboxConfigurationValidationResult.Valid
            : new MailboxConfigurationValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    public static bool IsSafeMailboxToken(string? value)
        => TenantPolicySchema.IsSafePolicyToken(value);

    public static bool IsSafeFingerprint(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
            value.StartsWith("sha256:", StringComparison.Ordinal) &&
            value.Length <= 160 &&
            value.Skip("sha256:".Length).All(static character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    private static void ValidatePatterns(IReadOnlyList<MonitoredMailboxPattern>? patterns, List<string> errors)
    {
        if (patterns is null || patterns.Count == 0 || patterns.Count > MaxPatterns)
        {
            errors.Add("mailbox_patterns_invalid");
            return;
        }

        HashSet<string> mailboxIds = new(StringComparer.Ordinal);
        foreach (MonitoredMailboxPattern pattern in patterns)
        {
            if (!IsSafeMailboxToken(pattern.MailboxId) || !mailboxIds.Add(pattern.MailboxId))
            {
                errors.Add("mailbox_id_invalid");
            }

            if (!IsSafeMailboxToken(pattern.SourceContext))
            {
                errors.Add("mailbox_source_context_invalid");
            }

            if (!IsSafeMailboxToken(pattern.ProviderConnectionRef))
            {
                errors.Add("mailbox_provider_connection_ref_invalid");
            }

            if (!IsSafeMailboxToken(pattern.PatternRef))
            {
                errors.Add("mailbox_pattern_ref_invalid");
            }
        }
    }

    private static void ValidateRoutingRules(IReadOnlyList<MailboxRoutingRule>? rules, List<string> errors)
    {
        if (rules is null || rules.Count > MaxRoutingRules)
        {
            errors.Add("mailbox_routing_rules_invalid");
            return;
        }

        HashSet<string> ruleIds = new(StringComparer.Ordinal);
        foreach (MailboxRoutingRule rule in rules)
        {
            if (!ruleIds.Add(rule.RoutingRuleId))
            {
                errors.Add("mailbox_routing_rule_duplicate");
            }

            if (!IsSafeMailboxToken(rule.RoutingRuleId))
            {
                errors.Add("mailbox_routing_rule_id_invalid");
            }

            if (rule.Kind is MailboxRoutingRuleKind.Unknown || !Enum.IsDefined(rule.Kind))
            {
                errors.Add("mailbox_routing_rule_kind_invalid");
            }

            if (!IsSafeMailboxToken(rule.SourceContext))
            {
                errors.Add("mailbox_source_context_invalid");
            }

            if (!IsSafeMailboxToken(rule.TargetRef))
            {
                errors.Add("mailbox_routing_target_ref_invalid");
            }

            if (rule.Priority is < 0 or > 1000)
            {
                errors.Add("mailbox_routing_priority_invalid");
            }

            if (!IsSafeMailboxToken(rule.ReasonCode))
            {
                errors.Add("mailbox_reason_code_invalid");
            }
        }
    }

    private static void ValidateProviderConnections(IReadOnlyList<MailboxProviderConnectionMetadata>? providers, List<string> errors)
    {
        if (providers is null || providers.Count == 0 || providers.Count > MaxProviderConnections)
        {
            errors.Add("mailbox_provider_connections_invalid");
            return;
        }

        HashSet<string> providerRefs = new(StringComparer.Ordinal);
        foreach (MailboxProviderConnectionMetadata provider in providers)
        {
            if (!IsSafeMailboxToken(provider.ProviderConnectionRef) || !providerRefs.Add(provider.ProviderConnectionRef))
            {
                errors.Add("mailbox_provider_connection_ref_invalid");
            }

            if (provider.ProviderKind is MailboxProviderKind.Unknown || !Enum.IsDefined(provider.ProviderKind))
            {
                errors.Add("mailbox_provider_kind_invalid");
            }

            if (!IsSafeFingerprint(provider.CredentialFingerprint))
            {
                errors.Add("mailbox_provider_fingerprint_invalid");
            }

            if (!IsSafeMailboxToken(provider.PermissionEvidenceRef))
            {
                errors.Add("mailbox_permission_evidence_ref_invalid");
            }

            if (provider.Freshness is MailboxPermissionFreshnessState.Unknown || !Enum.IsDefined(provider.Freshness))
            {
                errors.Add("mailbox_permission_freshness_invalid");
            }
        }
    }

    private static void ValidatePermissionStatuses(IReadOnlyList<MailboxPermissionStatus>? statuses, List<string> errors)
    {
        if (statuses is null)
        {
            errors.Add("mailbox_permission_statuses_invalid");
            return;
        }

        foreach (MailboxPermissionStatus status in statuses)
        {
            if (!IsSafeMailboxToken(status.PermissionStatusRef))
            {
                errors.Add("mailbox_permission_status_ref_invalid");
            }

            if (!IsSafeMailboxToken(status.ProviderConnectionRef))
            {
                errors.Add("mailbox_provider_connection_ref_invalid");
            }

            if (!string.Equals(status.Permission, LeastPrivilegeInboundPermission, StringComparison.Ordinal))
            {
                errors.Add("mailbox_permission_invalid");
            }

            if (status.Freshness is MailboxPermissionFreshnessState.Unknown || !Enum.IsDefined(status.Freshness))
            {
                errors.Add("mailbox_permission_freshness_invalid");
            }

            if (!IsSafeMailboxToken(status.PermissionEvidenceRef))
            {
                errors.Add("mailbox_permission_evidence_ref_invalid");
            }

            if (!IsSafeMailboxToken(status.ReasonCode))
            {
                errors.Add("mailbox_reason_code_invalid");
            }
        }
    }
}
