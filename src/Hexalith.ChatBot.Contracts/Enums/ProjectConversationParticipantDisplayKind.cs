using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<ProjectConversationParticipantDisplayKind>))]
public enum ProjectConversationParticipantDisplayKind
{
    [EnumMember(Value = "internal-participant")]
    InternalParticipant,

    [EnumMember(Value = "external-participant")]
    ExternalParticipant,

    [EnumMember(Value = "unresolved-participant")]
    UnresolvedParticipant,

    [EnumMember(Value = "restricted-participant")]
    RestrictedParticipant,
}
