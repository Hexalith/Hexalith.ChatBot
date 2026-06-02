using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<ComplianceEscalationStatus>))]
public enum ComplianceEscalationStatus
{
    [EnumMember(Value = "unknown")]
    Unknown,

    [EnumMember(Value = "not-requested")]
    NotRequested,

    [EnumMember(Value = "requested")]
    Requested,

    [EnumMember(Value = "approved")]
    Approved,

    [EnumMember(Value = "denied")]
    Denied,
}
