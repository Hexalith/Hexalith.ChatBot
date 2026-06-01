using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<ProjectConversationDetectedActionKind>))]
public enum ProjectConversationDetectedActionKind
{
    [EnumMember(Value = "request-information")]
    RequestInformation,

    [EnumMember(Value = "request-action")]
    RequestAction,

    [EnumMember(Value = "request-decision")]
    RequestDecision,

    [EnumMember(Value = "inform-only")]
    InformOnly,
}
