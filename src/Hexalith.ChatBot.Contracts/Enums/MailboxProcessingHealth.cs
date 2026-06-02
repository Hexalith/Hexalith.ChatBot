using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<MailboxProcessingHealth>))]
public enum MailboxProcessingHealth
{
    [EnumMember(Value = "unknown")]
    Unknown,

    [EnumMember(Value = "healthy")]
    Healthy,

    [EnumMember(Value = "degraded")]
    Degraded,

    [EnumMember(Value = "failed")]
    Failed,
}
