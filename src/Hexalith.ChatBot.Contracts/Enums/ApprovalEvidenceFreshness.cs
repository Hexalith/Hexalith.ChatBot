using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<ApprovalEvidenceFreshness>))]
public enum ApprovalEvidenceFreshness
{
    [EnumMember(Value = "fresh")]
    Fresh,

    [EnumMember(Value = "stale")]
    Stale,

    [EnumMember(Value = "expired")]
    Expired,
}
