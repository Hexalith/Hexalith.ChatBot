using System.Runtime.Serialization;

namespace Hexalith.ChatBot.Contracts.Queries;

public enum OperationCompletionStatus
{
    [EnumMember(Value = "accepted-projection-pending")]
    AcceptedProjectionPending,

    [EnumMember(Value = "completed")]
    Completed,

    [EnumMember(Value = "failed")]
    Failed,
}
