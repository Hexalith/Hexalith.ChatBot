using System.Runtime.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

public enum ActorType
{
    [EnumMember(Value = "human")]
    Human,

    [EnumMember(Value = "ai")]
    Ai,

    [EnumMember(Value = "service")]
    Service,

    [EnumMember(Value = "system")]
    System,
}
