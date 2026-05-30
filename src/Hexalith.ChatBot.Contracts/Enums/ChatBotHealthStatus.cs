using System.Runtime.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

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
