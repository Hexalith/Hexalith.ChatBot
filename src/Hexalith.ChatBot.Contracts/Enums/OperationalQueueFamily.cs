using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<OperationalQueueFamily>))]
public enum OperationalQueueFamily
{
    [EnumMember(Value = "ambiguous-association")]
    AmbiguousAssociation,

    [EnumMember(Value = "unresolved-participant")]
    UnresolvedParticipant,

    [EnumMember(Value = "pending-approval")]
    PendingApproval,

    [EnumMember(Value = "failed-ingestion")]
    FailedIngestion,

    [EnumMember(Value = "failed-attachment")]
    FailedAttachment,

    [EnumMember(Value = "retryable-operation")]
    RetryableOperation,
}
