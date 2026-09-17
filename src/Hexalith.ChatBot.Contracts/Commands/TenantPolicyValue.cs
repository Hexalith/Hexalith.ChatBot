using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

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
