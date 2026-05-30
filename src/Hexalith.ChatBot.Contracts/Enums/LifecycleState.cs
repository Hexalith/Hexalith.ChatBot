using System.Runtime.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

public enum LifecycleState
{
    [EnumMember(Value = "pending")]
    Pending,

    [EnumMember(Value = "accepted")]
    Accepted,

    [EnumMember(Value = "running")]
    Running,

    [EnumMember(Value = "succeeded")]
    Succeeded,

    [EnumMember(Value = "failed")]
    Failed,

    [EnumMember(Value = "rejected")]
    Rejected,

    [EnumMember(Value = "cancelled")]
    Cancelled,
}
