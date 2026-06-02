using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<TenantPolicyKnobType>))]
public enum TenantPolicyKnobType
{
    [EnumMember(Value = "double")]
    Double,

    [EnumMember(Value = "enum")]
    Enum,

    [EnumMember(Value = "boolean")]
    Boolean,

    [EnumMember(Value = "string")]
    String,

    [EnumMember(Value = "string-list")]
    StringList,

    [EnumMember(Value = "admin-scope-list")]
    AdminScopeList,

    [EnumMember(Value = "ai-action-low-risk-map")]
    AiActionLowRiskMap,

    [EnumMember(Value = "approval-priority-weights")]
    ApprovalPriorityWeights,

    [EnumMember(Value = "notification-throttle-ceilings")]
    NotificationThrottleCeilings,
}
