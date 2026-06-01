using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<ProjectConversationReadStatus>))]
public enum ProjectConversationReadStatus
{
    [EnumMember(Value = "current")]
    Current,

    [EnumMember(Value = "empty")]
    Empty,

    [EnumMember(Value = "stale")]
    Stale,

    [EnumMember(Value = "degraded")]
    Degraded,

    [EnumMember(Value = "blocked")]
    Blocked,
}
