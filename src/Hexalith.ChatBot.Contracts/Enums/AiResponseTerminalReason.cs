using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<AiResponseTerminalReason>))]
public enum AiResponseTerminalReason
{
    [EnumMember(Value = "none")]
    None,

    [EnumMember(Value = "completed")]
    Completed,

    [EnumMember(Value = "user-stopped")]
    UserStopped,

    [EnumMember(Value = "cancelled")]
    Cancelled,

    [EnumMember(Value = "failed")]
    Failed,

    [EnumMember(Value = "unavailable")]
    Unavailable,
}
