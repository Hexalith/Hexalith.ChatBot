using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Finite, closed set of trailing-window dimensions a mailbox-source intake rate-limit budget may be measured
/// over (Story 7.14, FR75). Deliberately NOT a free-form duration: a tenant cannot introduce a new window
/// dimension or a custom formula — it can only choose a budget within the declared <c>MailboxRateLimitBounds</c>
/// for the single declared window. Mirrors the closed rolling-hour discipline of Story 7.9's hourly ceiling.
/// Append-only: add new members at the end and update ordering/stability tests deliberately.
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<MailboxRateLimitWindow>))]
public enum MailboxRateLimitWindow
{
    /// <summary>A trailing rolling 60-minute window (the single declared mailbox-source intake budget dimension).</summary>
    [EnumMember(Value = "rolling-hour")]
    RollingHour,
}
