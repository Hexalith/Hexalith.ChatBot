using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<AssociationScoringOutcome>))]
public enum AssociationScoringOutcome
{
    [EnumMember(Value = "auto-associated")]
    AutoAssociated,

    [EnumMember(Value = "candidates-generated")]
    CandidatesGenerated,

    [EnumMember(Value = "failed-closed")]
    FailedClosed,
}
