using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal static class AiActionRiskClassifier
{
    public const string CurrentVersion = "chatbot.ai-action-risk-classifier.m0.v1";

    private static readonly IReadOnlyDictionary<AiActionRiskActionClass, int> ClassOrder =
        new Dictionary<AiActionRiskActionClass, int>
        {
            [AiActionRiskActionClass.ModifiesState] = 0,
            [AiActionRiskActionClass.ExposesFiles] = 1,
            [AiActionRiskActionClass.SendsExternal] = 2,
            [AiActionRiskActionClass.CreatesTasks] = 3,
            [AiActionRiskActionClass.InvokesTools] = 4,
            [AiActionRiskActionClass.ActsOnBehalf] = 5,
        };

    public static AiActionRiskClassificationRecord Classify(AiActionRiskInputTuple input)
    {
        ArgumentNullException.ThrowIfNull(input);

        AiActionRiskInputTuple effective = ApplyKnownCommandMetadata(input);
        bool hasUnknownActionClass = HasUnknownActionClass(effective.ActionClasses);
        string allowlistVersion = effective.CommandAllowlistVersion ?? "unavailable";
        string authority = effective.RequesterAuthorityClass ?? "undeclared";
        IReadOnlyList<AiActionRiskActionClass> orderedClasses = OrderKnownClasses(effective.ActionClasses);
        effective = effective with { ActionClasses = orderedClasses };

        string? indeterminate = FirstIndeterminateReason(effective, hasUnknownActionClass);
        bool unsupported = IsUnsupported(effective);
        bool hasRiskyClass = orderedClasses.Count > 0;
        bool defaultRequiresApproval = effective.CommandDefaultRisk is AiActionRiskClass.ApprovalRequired;
        bool policyRequiresApproval = string.Equals(effective.TenantPolicyClassification, "approval-required", StringComparison.Ordinal);
        bool lowRisk = indeterminate is null &&
            !unsupported &&
            !hasRiskyClass &&
            !defaultRequiresApproval &&
            string.Equals(effective.EffectSurface, "read-only", StringComparison.Ordinal) &&
            string.Equals(effective.TenantPolicyClassification, "low-risk", StringComparison.Ordinal) &&
            effective.CommandDefaultRisk is AiActionRiskClass.LowRisk &&
            string.Equals(effective.ProjectAuthorizationState, "authorized", StringComparison.Ordinal);

        string reason = unsupported
            ? ChatBotRefusalReasonCodes.UnsupportedAction
            : indeterminate is not null
                ? $"indeterminate_{indeterminate}"
                : hasRiskyClass
                    ? "risky_action_class"
                    : defaultRequiresApproval || policyRequiresApproval
                        ? "command_default_requires_approval"
                        : lowRisk
                            ? "low_risk_tuple"
                            : "indeterminate_tuple";

        string? finalIndeterminate = indeterminate ?? (lowRisk || unsupported || hasRiskyClass || defaultRequiresApproval || policyRequiresApproval ? null : "tuple");
        return new AiActionRiskClassificationRecord(
            lowRisk ? AiActionRiskClass.LowRisk : AiActionRiskClass.ApprovalRequired,
            orderedClasses,
            CurrentVersion,
            effective,
            effective.PolicySnapshotId,
            allowlistVersion,
            effective.CommandDefaultRisk,
            authority,
            reason,
            "metadata_only",
            "collaboration_input",
            "chatbot.ai-action-risk-classification.v1",
            effective.CorrelationId,
            DateTimeOffset.UtcNow,
            finalIndeterminate,
            unsupported);
    }

    private static AiActionRiskInputTuple ApplyKnownCommandMetadata(AiActionRiskInputTuple input)
    {
        AiActionCommandMetadata? metadata = AiActionCommandMetadataProvider.TryGet(input.IntendedCommandName);
        if (metadata is null)
        {
            return input;
        }

        return input with
        {
            ActionClasses = input.ActionClasses is { Count: > 0 } ? input.ActionClasses : metadata.ActionClasses,
            EffectSurface = string.IsNullOrWhiteSpace(input.EffectSurface) ? metadata.EffectSurface : input.EffectSurface,
            TenantPolicyClassification = string.IsNullOrWhiteSpace(input.TenantPolicyClassification)
                ? metadata.TenantPolicyClassification
                : input.TenantPolicyClassification,
            CommandAllowlistVersion = string.IsNullOrWhiteSpace(input.CommandAllowlistVersion)
                ? metadata.CommandAllowlistVersion
                : input.CommandAllowlistVersion,
            CommandDefaultRisk = input.CommandDefaultRisk ?? metadata.CommandDefaultRisk,
            AllowlistMetadataState = string.IsNullOrWhiteSpace(input.AllowlistMetadataState) ? "declared" : input.AllowlistMetadataState,
        };
    }

    private static IReadOnlyList<AiActionRiskActionClass> OrderKnownClasses(IReadOnlyList<AiActionRiskActionClass>? classes)
        => (classes ?? [])
            .Where(static value => ClassOrder.ContainsKey(value))
            .Distinct()
            .OrderBy(static value => ClassOrder[value])
            .ToArray();

    private static bool HasUnknownActionClass(IReadOnlyList<AiActionRiskActionClass>? classes)
        => classes?.Any(static value => !ClassOrder.ContainsKey(value)) == true;

    private static string? FirstIndeterminateReason(AiActionRiskInputTuple input, bool hasUnknownActionClass)
    {
        if (string.IsNullOrWhiteSpace(input.IntendedCommandName))
        {
            return "missing_command";
        }

        if (hasUnknownActionClass)
        {
            return "unknown_action_class";
        }

        if (string.IsNullOrWhiteSpace(input.EffectSurface))
        {
            return "missing_effect_surface";
        }

        if (string.IsNullOrWhiteSpace(input.TenantPolicyClassification))
        {
            return "missing_policy_classification";
        }

        if (string.IsNullOrWhiteSpace(input.RequesterAuthorityClass))
        {
            return "missing_requester_authority";
        }

        if (string.IsNullOrWhiteSpace(input.CommandAllowlistVersion) ||
            input.CommandDefaultRisk is null ||
            string.IsNullOrWhiteSpace(input.AllowlistMetadataState))
        {
            return "missing_allowlist_metadata";
        }

        if (string.IsNullOrWhiteSpace(input.ProjectAuthorizationState))
        {
            return "missing_project_authorization";
        }

        if (!string.Equals(input.EffectSurface, "read-only", StringComparison.Ordinal) &&
            !string.Equals(input.EffectSurface, "project-conversation", StringComparison.Ordinal))
        {
            return "unknown_effect_surface";
        }

        return null;
    }

    private static bool IsUnsupported(AiActionRiskInputTuple input)
        => string.Equals(input.TenantPolicyClassification, "disallowed", StringComparison.Ordinal) ||
            string.Equals(input.TenantPolicyClassification, "unsupported", StringComparison.Ordinal) ||
            string.Equals(input.AllowlistMetadataState, "disallowed", StringComparison.Ordinal) ||
            string.Equals(input.AllowlistMetadataState, "unsupported", StringComparison.Ordinal);
}
