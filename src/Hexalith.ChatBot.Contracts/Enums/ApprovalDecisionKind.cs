using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<ApprovalDecisionKind>))]
public enum ApprovalDecisionKind
{
    [EnumMember(Value = "approve")]
    Approve,

    [EnumMember(Value = "reject")]
    Reject,

    [EnumMember(Value = "request-revision")]
    RequestRevision,

    [EnumMember(Value = "cancel")]
    Cancel,
}
