using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<OperationalQueueSortKey>))]
public enum OperationalQueueSortKey
{
    [EnumMember(Value = "priority")]
    Priority,

    [EnumMember(Value = "age")]
    Age,

    [EnumMember(Value = "risk")]
    Risk,

    [EnumMember(Value = "confidence")]
    Confidence,

    [EnumMember(Value = "freshness")]
    Freshness,

    [EnumMember(Value = "retry-count")]
    RetryCount,
}
