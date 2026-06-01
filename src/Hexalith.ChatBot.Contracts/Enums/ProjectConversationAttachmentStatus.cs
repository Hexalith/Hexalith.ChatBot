using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<ProjectConversationAttachmentStatus>))]
public enum ProjectConversationAttachmentStatus
{
    [EnumMember(Value = "captured")]
    Captured,

    [EnumMember(Value = "pending")]
    Pending,

    [EnumMember(Value = "unavailable")]
    Unavailable,

    [EnumMember(Value = "rejected")]
    Rejected,

    [EnumMember(Value = "unsafe")]
    Unsafe,

    [EnumMember(Value = "failed")]
    Failed,

    [EnumMember(Value = "retryable")]
    Retryable,
}
