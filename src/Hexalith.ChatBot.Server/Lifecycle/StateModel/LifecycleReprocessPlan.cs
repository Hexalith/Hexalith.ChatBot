namespace Hexalith.ChatBot.Server.Lifecycle.StateModel;

internal sealed record LifecycleReprocessPlan(
    string SupersededWorkflowId,
    string NewWorkflowId,
    string SupersededByAuditLinkName,
    string SupersedesAuditLinkName);
