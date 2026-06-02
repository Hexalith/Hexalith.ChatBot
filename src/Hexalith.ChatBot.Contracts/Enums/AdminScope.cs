using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<AdminScope>))]
public enum AdminScope
{
    [EnumMember(Value = "see-only")]
    SeeOnly,

    [EnumMember(Value = "operate")]
    Operate,

    [EnumMember(Value = "policy")]
    Policy,

    [EnumMember(Value = "mailbox")]
    Mailbox,

    [EnumMember(Value = "compliance")]
    Compliance,

    [EnumMember(Value = "audit-obligation")]
    AuditObligation,
}
