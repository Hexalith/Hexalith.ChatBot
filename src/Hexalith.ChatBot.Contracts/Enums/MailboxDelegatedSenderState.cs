using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<MailboxDelegatedSenderState>))]
public enum MailboxDelegatedSenderState
{
    [EnumMember(Value = "not-delegated")]
    NotDelegated,

    [EnumMember(Value = "delegated")]
    Delegated,

    [EnumMember(Value = "ambiguous")]
    Ambiguous,

    [EnumMember(Value = "not-supplied")]
    NotSupplied,
}
