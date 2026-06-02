using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Finite FR74 governance control state for a mailbox source. Distinct from the Story 7.3
/// <see cref="Commands.MonitoredMailboxPattern.IsEnabled"/> configuration flag: this state is set only
/// through the security-sensitive two-person submit→approve disable path, never the mailbox-config path.
/// Shaped so Story 7.13 can add <c>Quarantined</c> and 7.15–7.26 can reuse the per-subject control pattern.
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<MailboxSourceControlState>))]
public enum MailboxSourceControlState
{
    /// <summary>The mailbox source processes intake normally.</summary>
    [EnumMember(Value = "active")]
    Active,

    /// <summary>The mailbox source is disabled; future intake is blocked while existing records stay auditable.</summary>
    [EnumMember(Value = "disabled")]
    Disabled,
}
