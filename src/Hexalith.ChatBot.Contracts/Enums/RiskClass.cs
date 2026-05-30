using System.Runtime.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

public enum RiskClass
{
    [EnumMember(Value = "none")]
    None,

    [EnumMember(Value = "low")]
    Low,

    [EnumMember(Value = "medium")]
    Medium,

    [EnumMember(Value = "high")]
    High,

    [EnumMember(Value = "blocked")]
    Blocked,
}
