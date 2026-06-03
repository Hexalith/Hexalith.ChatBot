using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Freshness classification for a surfaced evidence/health reference (NFR48), derived from the bounded-staleness
/// window for its class. Stable machine tokens, never translated. <see cref="Stale"/> is a permitted-but-flagged
/// state, never an error; <see cref="Expired"/> means the snapshot is past its trustworthy window.
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<ChatBotFreshnessState>))]
public enum ChatBotFreshnessState
{
    [EnumMember(Value = "fresh")]
    Fresh,

    [EnumMember(Value = "stale")]
    Stale,

    [EnumMember(Value = "expired")]
    Expired,
}
