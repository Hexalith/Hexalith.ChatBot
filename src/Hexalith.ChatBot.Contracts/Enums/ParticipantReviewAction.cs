using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ParticipantReviewAction>))]
public enum ParticipantReviewAction
{
    [EnumMember(Value = "link")]
    Link,

    [EnumMember(Value = "create-pending")]
    CreatePending,

    [EnumMember(Value = "reject")]
    Reject,

    [EnumMember(Value = "quarantine")]
    Quarantine,
}
