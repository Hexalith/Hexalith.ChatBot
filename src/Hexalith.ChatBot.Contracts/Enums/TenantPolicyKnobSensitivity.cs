using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<TenantPolicyKnobSensitivity>))]
public enum TenantPolicyKnobSensitivity
{
    [EnumMember(Value = "standard")]
    Standard,

    [EnumMember(Value = "security-sensitive")]
    SecuritySensitive,
}
