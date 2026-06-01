using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<FailureStatus>))]
public enum FailureStatus
{
    [EnumMember(Value = "retryable")]
    Retryable,

    [EnumMember(Value = "terminal")]
    Terminal,

    [EnumMember(Value = "blocked")]
    Blocked,

    [EnumMember(Value = "degraded")]
    Degraded,

    [EnumMember(Value = "resolved")]
    Resolved,

    [EnumMember(Value = "unknown")]
    Unknown,
}
