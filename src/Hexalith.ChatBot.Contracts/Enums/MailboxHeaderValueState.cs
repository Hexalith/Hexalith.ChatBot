using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<MailboxHeaderValueState>))]
public enum MailboxHeaderValueState
{
    [EnumMember(Value = "not-supplied")]
    NotSupplied,

    [EnumMember(Value = "supplied")]
    Supplied,

    [EnumMember(Value = "malformed")]
    Malformed,

    [EnumMember(Value = "ambiguous")]
    Ambiguous,
}
