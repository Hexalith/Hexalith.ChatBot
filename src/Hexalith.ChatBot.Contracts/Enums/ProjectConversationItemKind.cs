using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<ProjectConversationItemKind>))]
public enum ProjectConversationItemKind
{
    [EnumMember(Value = "email-derived")]
    EmailDerived,

    [EnumMember(Value = "system-decision")]
    SystemDecision,

    [EnumMember(Value = "participant")]
    Participant,
}
