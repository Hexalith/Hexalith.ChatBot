using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<TenantPolicyApprovalStatus>))]
public enum TenantPolicyApprovalStatus
{
    [EnumMember(Value = "not-required")]
    NotRequired,

    [EnumMember(Value = "pending-second-approval")]
    PendingSecondApproval,

    [EnumMember(Value = "approved")]
    Approved,

    [EnumMember(Value = "rejected")]
    Rejected,
}
