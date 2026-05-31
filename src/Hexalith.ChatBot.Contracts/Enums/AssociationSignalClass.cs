using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<AssociationSignalClass>))]
public enum AssociationSignalClass
{
    [EnumMember(Value = "explicit-project-identifier")]
    ExplicitProjectIdentifier,

    [EnumMember(Value = "mailbox-routing-rule")]
    MailboxRoutingRule,

    [EnumMember(Value = "conversation-thread-identifier")]
    ConversationThreadIdentifier,
}
