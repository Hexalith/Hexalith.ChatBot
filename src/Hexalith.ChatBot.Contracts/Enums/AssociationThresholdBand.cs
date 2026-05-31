using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<AssociationThresholdBand>))]
public enum AssociationThresholdBand
{
    [EnumMember(Value = "auto")]
    Auto,

    [EnumMember(Value = "ambiguous")]
    Ambiguous,

    [EnumMember(Value = "fail-closed")]
    FailClosed,
}
