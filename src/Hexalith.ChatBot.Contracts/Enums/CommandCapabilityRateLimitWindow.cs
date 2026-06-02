using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Finite, closed set of trailing-window dimensions a command-capability rate-limit budget may be measured over
/// (Story 7.23, FR75). Deliberately NOT a free-form duration: a tenant cannot introduce a new window dimension or a
/// custom formula — it can only choose a budget within the declared <c>CommandCapabilityRateLimitBounds</c> for the
/// single declared window. Mirrors the closed rolling-hour discipline of the Story 7.20 AI-actor window. Append-only:
/// add new members at the end and update ordering/stability tests deliberately.
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<CommandCapabilityRateLimitWindow>))]
public enum CommandCapabilityRateLimitWindow
{
    /// <summary>A trailing rolling 60-minute window (the single declared command-capability budget dimension).</summary>
    [EnumMember(Value = "rolling-hour")]
    RollingHour,
}
