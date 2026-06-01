using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<ProjectConversationActorKind>))]
public enum ProjectConversationActorKind
{
    [EnumMember(Value = "mailbox")]
    Mailbox,

    [EnumMember(Value = "system-decision")]
    SystemDecision,

    [EnumMember(Value = "internal-participant")]
    InternalParticipant,

    [EnumMember(Value = "external-participant")]
    ExternalParticipant,

    [EnumMember(Value = "unresolved-participant")]
    UnresolvedParticipant,

    [EnumMember(Value = "restricted-participant")]
    RestrictedParticipant,

    [EnumMember(Value = "mailbox-attachment")]
    MailboxAttachment,
}
