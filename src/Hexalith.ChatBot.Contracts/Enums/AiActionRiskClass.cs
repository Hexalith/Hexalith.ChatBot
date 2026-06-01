using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<AiActionRiskClass>))]
public enum AiActionRiskClass
{
    [EnumMember(Value = "low-risk")]
    LowRisk,

    [EnumMember(Value = "approval-required")]
    ApprovalRequired,
}
