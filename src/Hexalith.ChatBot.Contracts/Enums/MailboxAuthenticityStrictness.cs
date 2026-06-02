using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<MailboxAuthenticityStrictness>))]
public enum MailboxAuthenticityStrictness
{
    [EnumMember(Value = "permissive")]
    Permissive,

    [EnumMember(Value = "strict")]
    Strict,

    [EnumMember(Value = "paranoid")]
    Paranoid,
}
