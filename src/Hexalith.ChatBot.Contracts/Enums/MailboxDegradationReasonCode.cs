using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<MailboxDegradationReasonCode>))]
public enum MailboxDegradationReasonCode
{
    [EnumMember(Value = "unknown")]
    Unknown,

    [EnumMember(Value = "healthy")]
    Healthy,

    [EnumMember(Value = "graph-permission-revoked")]
    GraphPermissionRevoked,

    [EnumMember(Value = "graph-token-expired")]
    GraphTokenExpired,

    [EnumMember(Value = "graph-throttled")]
    GraphThrottled,

    [EnumMember(Value = "graph-backoff")]
    GraphBackoff,

    [EnumMember(Value = "graph-partial-access")]
    GraphPartialAccess,

    [EnumMember(Value = "graph-delayed-delivery")]
    GraphDelayedDelivery,

    [EnumMember(Value = "graph-subscription-expired")]
    GraphSubscriptionExpired,

    [EnumMember(Value = "graph-permission-drift")]
    GraphPermissionDrift,

    [EnumMember(Value = "mailbox-scope-mismatch")]
    MailboxScopeMismatch,
}
