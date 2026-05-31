using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ParticipantResolutionStatus>))]
public enum ParticipantResolutionStatus
{
    [EnumMember(Value = "resolved")]
    Resolved,

    [EnumMember(Value = "unresolved")]
    Unresolved,

    [EnumMember(Value = "rejected")]
    Rejected,

    [EnumMember(Value = "quarantined")]
    Quarantined,

    [EnumMember(Value = "blocked")]
    Blocked,
}
