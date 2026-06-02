using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<ComplianceAuditRedactionState>))]
public enum ComplianceAuditRedactionState
{
    [EnumMember(Value = "unknown")]
    Unknown,

    [EnumMember(Value = "metadata-only")]
    MetadataOnly,

    [EnumMember(Value = "detail-available")]
    DetailAvailable,

    [EnumMember(Value = "restricted")]
    Restricted,

    [EnumMember(Value = "escalation-required")]
    EscalationRequired,
}
