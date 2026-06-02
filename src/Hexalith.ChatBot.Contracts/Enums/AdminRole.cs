using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<AdminRole>))]
public enum AdminRole
{
    [EnumMember(Value = "tenant-admin")]
    TenantAdmin,

    [EnumMember(Value = "mailbox-admin")]
    MailboxAdmin,

    [EnumMember(Value = "policy-admin")]
    PolicyAdmin,

    [EnumMember(Value = "compliance-admin")]
    ComplianceAdmin,

    [EnumMember(Value = "operations-admin")]
    OperationsAdmin,
}
