using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<FailureStateKind>))]
public enum FailureStateKind
{
    [EnumMember(Value = "failure")]
    Failure,

    [EnumMember(Value = "retry-queued")]
    RetryQueued,

    [EnumMember(Value = "retry-accepted")]
    RetryAccepted,

    [EnumMember(Value = "retry-exhausted")]
    RetryExhausted,

    [EnumMember(Value = "blocked")]
    Blocked,

    [EnumMember(Value = "duplicate-suppressed")]
    DuplicateSuppressed,

    [EnumMember(Value = "dependency-degraded")]
    DependencyDegraded,

    [EnumMember(Value = "projection-retryable")]
    ProjectionRetryable,

    [EnumMember(Value = "terminal-failure")]
    TerminalFailure,

    [EnumMember(Value = "reprocess-created")]
    ReprocessCreated,
}
