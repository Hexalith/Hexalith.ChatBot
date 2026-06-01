using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<ProjectConversationClassificationKind>))]
public enum ProjectConversationClassificationKind
{
    [EnumMember(Value = "informational")]
    Informational,

    [EnumMember(Value = "actionable")]
    Actionable,
}
