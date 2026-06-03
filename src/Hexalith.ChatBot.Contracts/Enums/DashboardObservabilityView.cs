using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Closed set of read-only M2 operational-dashboard observability views (FR67). Each value is a stable machine
/// token and never translated; the human-facing view name is localized at the presentation boundary. The set is
/// the six FR67 views plus the audit-projection-lag status surfaced at M0/M1 fidelity.
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<DashboardObservabilityView>))]
public enum DashboardObservabilityView
{
    [EnumMember(Value = "mailbox-processing")]
    MailboxProcessing,

    [EnumMember(Value = "failed-associations")]
    FailedAssociations,

    [EnumMember(Value = "approval-queues")]
    ApprovalQueues,

    [EnumMember(Value = "duplicate-handling")]
    DuplicateHandling,

    [EnumMember(Value = "ai-action-outcomes")]
    AiActionOutcomes,

    [EnumMember(Value = "audit-projection-lag")]
    AuditProjectionLag,
}
