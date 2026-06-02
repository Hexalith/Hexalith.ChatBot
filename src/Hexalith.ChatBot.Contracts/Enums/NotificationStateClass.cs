using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// The six closed notify-worthy workflow state classes that the notification routing engine evaluates (FR72).
/// Tenants cannot introduce new state classes.
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<NotificationStateClass>))]
public enum NotificationStateClass
{
    [EnumMember(Value = "review-needed")]
    ReviewNeeded,

    [EnumMember(Value = "approval-pending")]
    ApprovalPending,

    [EnumMember(Value = "failure")]
    Failure,

    [EnumMember(Value = "degraded")]
    Degraded,

    [EnumMember(Value = "quarantine")]
    Quarantine,

    [EnumMember(Value = "retry")]
    Retry,
}
