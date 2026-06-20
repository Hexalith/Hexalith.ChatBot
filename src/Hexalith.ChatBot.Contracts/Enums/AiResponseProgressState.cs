using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<AiResponseProgressState>))]
public enum AiResponseProgressState
{
    [EnumMember(Value = "pending")]
    Pending,

    [EnumMember(Value = "rendering")]
    Rendering,

    [EnumMember(Value = "cancelling")]
    Cancelling,

    [EnumMember(Value = "completed")]
    Completed,

    [EnumMember(Value = "stopped")]
    Stopped,

    [EnumMember(Value = "cancelled")]
    Cancelled,

    [EnumMember(Value = "failed")]
    Failed,

    [EnumMember(Value = "unavailable")]
    Unavailable,
}
