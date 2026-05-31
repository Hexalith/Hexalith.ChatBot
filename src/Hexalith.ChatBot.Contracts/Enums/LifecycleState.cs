using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<LifecycleState>))]
public enum LifecycleState
{
    [EnumMember(Value = "Received")]
    Received,

    [EnumMember(Value = "Proposed")]
    Proposed,

    [EnumMember(Value = "Associated")]
    Associated,

    [EnumMember(Value = "Rejected")]
    Rejected,

    [EnumMember(Value = "Deferred")]
    Deferred,

    [EnumMember(Value = "NeedsReview")]
    NeedsReview,

    [EnumMember(Value = "Failed")]
    Failed,

    [EnumMember(Value = "Skipped")]
    Skipped,

    [EnumMember(Value = "Corrected")]
    Corrected,

    [EnumMember(Value = "Correcting")]
    Correcting,

    [EnumMember(Value = "Correction-delayed")]
    CorrectionDelayed,
}
