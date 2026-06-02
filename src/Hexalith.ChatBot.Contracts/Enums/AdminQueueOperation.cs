using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<AdminQueueOperation>))]
public enum AdminQueueOperation
{
    [EnumMember(Value = "retry")]
    Retry,

    [EnumMember(Value = "requeue")]
    Requeue,

    [EnumMember(Value = "quarantine")]
    Quarantine,

    [EnumMember(Value = "dismiss")]
    Dismiss,

    [EnumMember(Value = "claim")]
    Claim,

    [EnumMember(Value = "assign")]
    Assign,

    [EnumMember(Value = "prioritize")]
    Prioritize,
}
