using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<MailboxAuthenticationVerdictKind>))]
public enum MailboxAuthenticationVerdictKind
{
    [EnumMember(Value = "not-supplied")]
    NotSupplied,

    [EnumMember(Value = "pass")]
    Pass,

    [EnumMember(Value = "fail")]
    Fail,

    [EnumMember(Value = "softfail")]
    SoftFail,

    [EnumMember(Value = "neutral")]
    Neutral,

    [EnumMember(Value = "none")]
    None,

    [EnumMember(Value = "temperror")]
    TempError,

    [EnumMember(Value = "permerror")]
    PermError,

    [EnumMember(Value = "bestguesspass")]
    BestGuessPass,

    [EnumMember(Value = "malformed")]
    Malformed,

    [EnumMember(Value = "ambiguous")]
    Ambiguous,

    [EnumMember(Value = "unknown")]
    Unknown,
}
