using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<ApprovalStatus>))]
public enum ApprovalStatus
{
    [EnumMember(Value = "pending")]
    Pending,

    [EnumMember(Value = "approved")]
    Approved,

    [EnumMember(Value = "rejected")]
    Rejected,

    [EnumMember(Value = "revision-requested")]
    RevisionRequested,

    [EnumMember(Value = "cancelled")]
    Cancelled,

    [EnumMember(Value = "executed")]
    Executed,

    [EnumMember(Value = "failed")]
    Failed,
}
