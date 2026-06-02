using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// The closed set of declared delivery channels a notification routing entry may target (FR73). Channels are
/// finite tokens; tenants cannot introduce new channels, and channel secrets are never carried by the routing map.
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<NotificationChannel>))]
public enum NotificationChannel
{
    [EnumMember(Value = "in-app")]
    InApp,

    [EnumMember(Value = "email")]
    Email,

    [EnumMember(Value = "webhook")]
    Webhook,

    [EnumMember(Value = "operator-alert")]
    OperatorAlert,
}
