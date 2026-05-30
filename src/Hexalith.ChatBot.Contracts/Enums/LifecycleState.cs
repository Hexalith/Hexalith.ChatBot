using System.Runtime.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

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
