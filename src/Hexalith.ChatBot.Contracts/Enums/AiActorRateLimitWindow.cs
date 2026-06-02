using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Finite, closed set of trailing-window dimensions an AI-actor proposal rate-limit budget may be measured
/// over (Story 7.20, FR75). Deliberately NOT a free-form duration: a tenant cannot introduce a new window
/// dimension or a custom formula — it can only choose a budget within the declared <c>AiActorRateLimitBounds</c>
/// for the single declared window. Mirrors the closed rolling-hour discipline of Story 7.17's service-client window.
/// Append-only: add new members at the end and update ordering/stability tests deliberately.
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<AiActorRateLimitWindow>))]
public enum AiActorRateLimitWindow
{
    /// <summary>A trailing rolling 60-minute window (the single declared AI-actor proposal budget dimension).</summary>
    [EnumMember(Value = "rolling-hour")]
    RollingHour,
}
