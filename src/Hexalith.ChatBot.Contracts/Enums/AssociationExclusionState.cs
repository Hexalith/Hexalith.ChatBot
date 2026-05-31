using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<AssociationExclusionState>))]
public enum AssociationExclusionState
{
    [EnumMember(Value = "not-found")]
    NotFound,

    [EnumMember(Value = "archived")]
    Archived,

    [EnumMember(Value = "stale")]
    Stale,

    [EnumMember(Value = "unavailable")]
    Unavailable,

    [EnumMember(Value = "ambiguous")]
    Ambiguous,

    [EnumMember(Value = "tenant-mismatch")]
    TenantMismatch,

    [EnumMember(Value = "unauthorized")]
    Unauthorized,

    [EnumMember(Value = "conflict")]
    Conflict,

    [EnumMember(Value = "invalid-reference")]
    InvalidReference,
}
