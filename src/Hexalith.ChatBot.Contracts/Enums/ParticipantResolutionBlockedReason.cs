using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ParticipantResolutionBlockedReason>))]
public enum ParticipantResolutionBlockedReason
{
    [EnumMember(Value = "not-found")]
    NotFound,

    [EnumMember(Value = "ambiguous-match")]
    AmbiguousMatch,

    [EnumMember(Value = "restricted-party")]
    RestrictedParty,

    [EnumMember(Value = "erased-party")]
    ErasedParty,

    [EnumMember(Value = "tenant-mismatch")]
    TenantMismatch,

    [EnumMember(Value = "directory-degraded")]
    DirectoryDegraded,

    [EnumMember(Value = "directory-unavailable")]
    DirectoryUnavailable,

    [EnumMember(Value = "invalid-evidence")]
    InvalidEvidence,

    [EnumMember(Value = "unauthorized-actor")]
    UnauthorizedActor,

    [EnumMember(Value = "unresolved-participant")]
    UnresolvedParticipant,
}
