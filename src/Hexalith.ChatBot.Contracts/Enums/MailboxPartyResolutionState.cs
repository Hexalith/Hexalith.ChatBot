using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<MailboxPartyResolutionState>))]
public enum MailboxPartyResolutionState
{
    [EnumMember(Value = "resolved-internal")]
    ResolvedInternal,

    [EnumMember(Value = "resolved-external")]
    ResolvedExternal,

    [EnumMember(Value = "unresolved")]
    Unresolved,

    [EnumMember(Value = "ambiguous")]
    Ambiguous,

    [EnumMember(Value = "unavailable")]
    Unavailable,
}
