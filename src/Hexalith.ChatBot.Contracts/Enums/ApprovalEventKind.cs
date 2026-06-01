using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<ApprovalEventKind>))]
public enum ApprovalEventKind
{
    [EnumMember(Value = "request")]
    Request,

    [EnumMember(Value = "decision")]
    Decision,

    [EnumMember(Value = "outcome")]
    Outcome,
}
