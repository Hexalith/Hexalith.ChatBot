using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// The closed, ordered severity ladder used by escalation thresholds (FR73). Severities are finite tokens; the
/// escalation evaluator never compares free-form risk strings after the trust boundary. Tenants cannot introduce
/// new severities.
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<EscalationSeverity>))]
public enum EscalationSeverity
{
    [EnumMember(Value = "low")]
    Low,

    [EnumMember(Value = "medium")]
    Medium,

    [EnumMember(Value = "high")]
    High,
}
