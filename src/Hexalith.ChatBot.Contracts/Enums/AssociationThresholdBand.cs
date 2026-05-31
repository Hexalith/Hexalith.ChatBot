using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<AssociationThresholdBand>))]
public enum AssociationThresholdBand
{
    [EnumMember(Value = "auto")]
    Auto,

    [EnumMember(Value = "ambiguous")]
    Ambiguous,

    [EnumMember(Value = "fail-closed")]
    FailClosed,
}
