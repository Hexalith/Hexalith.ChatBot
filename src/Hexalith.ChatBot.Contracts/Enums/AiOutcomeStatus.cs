using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<AiOutcomeStatus>))]
public enum AiOutcomeStatus
{
    [EnumMember(Value = "proposed")]
    Proposed,

    [EnumMember(Value = "blocked")]
    Blocked,

    [EnumMember(Value = "denied")]
    Denied,

    [EnumMember(Value = "pending-approval")]
    PendingApproval,

    [EnumMember(Value = "approved")]
    Approved,

    [EnumMember(Value = "executing")]
    Executing,

    [EnumMember(Value = "succeeded")]
    Succeeded,

    [EnumMember(Value = "failed")]
    Failed,

    [EnumMember(Value = "invalidated")]
    Invalidated,

    [EnumMember(Value = "unknown")]
    Unknown,
}
