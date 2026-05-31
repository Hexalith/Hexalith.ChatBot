using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<AssociationDecisionKind>))]
public enum AssociationDecisionKind
{
    [EnumMember(Value = "associate")]
    Associate,

    [EnumMember(Value = "reject")]
    Reject,

    [EnumMember(Value = "defer")]
    Defer,

    [EnumMember(Value = "needs-review")]
    NeedsReview,
}
