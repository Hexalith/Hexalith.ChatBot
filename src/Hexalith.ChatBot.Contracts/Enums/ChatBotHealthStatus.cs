using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<ChatBotHealthStatus>))]
public enum ChatBotHealthStatus
{
    [EnumMember(Value = "healthy")]
    Healthy,

    [EnumMember(Value = "degraded")]
    Degraded,

    [EnumMember(Value = "failed")]
    Failed,

    [EnumMember(Value = "unknown")]
    Unknown,
}
