using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<MailboxRoutingRuleKind>))]
public enum MailboxRoutingRuleKind
{
    [EnumMember(Value = "unknown")]
    Unknown,

    [EnumMember(Value = "source-context")]
    SourceContext,

    [EnumMember(Value = "mailbox-source")]
    MailboxSource,
}
