using System.Runtime.Serialization;

namespace Hexalith.ChatBot.Contracts.Queries;

public enum OperationAuditStatus
{
    [EnumMember(Value = "committed")]
    Committed,

    [EnumMember(Value = "reconciling")]
    Reconciling,
}
