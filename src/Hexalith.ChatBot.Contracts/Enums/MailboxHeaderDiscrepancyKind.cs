using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<MailboxHeaderDiscrepancyKind>))]
public enum MailboxHeaderDiscrepancyKind
{
    [EnumMember(Value = "multiple-authentication-results")]
    MultipleAuthenticationResults,

    [EnumMember(Value = "from-sender-mismatch")]
    FromSenderMismatch,

    [EnumMember(Value = "from-reply-to-mismatch")]
    FromReplyToMismatch,

    [EnumMember(Value = "sender-reply-to-mismatch")]
    SenderReplyToMismatch,

    [EnumMember(Value = "from-x-original-sender-mismatch")]
    FromXOriginalSenderMismatch,

    [EnumMember(Value = "malformed-from")]
    MalformedFrom,

    [EnumMember(Value = "malformed-sender")]
    MalformedSender,

    [EnumMember(Value = "malformed-reply-to")]
    MalformedReplyTo,

    [EnumMember(Value = "malformed-x-original-sender")]
    MalformedXOriginalSender,
}
