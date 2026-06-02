using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<SenderAuthorityClass>))]
public enum SenderAuthorityClass
{
    [EnumMember(Value = "draft-only")]
    DraftOnly,

    [EnumMember(Value = "authenticated-user send")]
    AuthenticatedUserSend,

    [EnumMember(Value = "shared-mailbox send")]
    SharedMailboxSend,

    [EnumMember(Value = "send-on-behalf")]
    SendOnBehalf,

    [EnumMember(Value = "approved service-send")]
    ApprovedServiceSend,
}
