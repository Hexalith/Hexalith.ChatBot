using System.Runtime.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

public enum ThresholdBand
{
    [EnumMember(Value = "below")]
    Below,

    [EnumMember(Value = "within")]
    Within,

    [EnumMember(Value = "above")]
    Above,

    [EnumMember(Value = "critical")]
    Critical,
}
