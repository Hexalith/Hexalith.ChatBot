using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<MailboxProviderKind>))]
public enum MailboxProviderKind
{
    [EnumMember(Value = "unknown")]
    Unknown,

    [EnumMember(Value = "microsoft-graph")]
    MicrosoftGraph,
}
