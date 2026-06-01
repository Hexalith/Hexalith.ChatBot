using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<TaskIntentState>))]
public enum TaskIntentState
{
    [EnumMember(Value = "captured")]
    Captured,

    [EnumMember(Value = "rejected")]
    Rejected,

    [EnumMember(Value = "blocked")]
    Blocked,

    [EnumMember(Value = "converted")]
    Converted,

    [EnumMember(Value = "not-actionable")]
    NotActionable,

    [EnumMember(Value = "duplicate")]
    Duplicate,

    [EnumMember(Value = "already-handled")]
    AlreadyHandled,

    [EnumMember(Value = "out-of-scope")]
    OutOfScope,
}
