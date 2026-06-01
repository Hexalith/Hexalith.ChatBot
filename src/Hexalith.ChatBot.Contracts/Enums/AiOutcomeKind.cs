using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<AiOutcomeKind>))]
public enum AiOutcomeKind
{
    [EnumMember(Value = "proposal")]
    Proposal,

    [EnumMember(Value = "denial")]
    Denial,

    [EnumMember(Value = "refusal")]
    Refusal,

    [EnumMember(Value = "approval-linked")]
    ApprovalLinked,

    [EnumMember(Value = "execution-started")]
    ExecutionStarted,

    [EnumMember(Value = "execution-succeeded")]
    ExecutionSucceeded,

    [EnumMember(Value = "execution-failed")]
    ExecutionFailed,

    [EnumMember(Value = "outcome-recorded")]
    OutcomeRecorded,

    [EnumMember(Value = "corrected-context-invalidated")]
    CorrectedContextInvalidated,
}
