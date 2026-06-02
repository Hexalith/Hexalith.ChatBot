using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<MailboxPermissionFreshnessState>))]
public enum MailboxPermissionFreshnessState
{
    [EnumMember(Value = "unknown")]
    Unknown,

    [EnumMember(Value = "fresh")]
    Fresh,

    [EnumMember(Value = "stale")]
    Stale,

    [EnumMember(Value = "expired")]
    Expired,
}
