using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public static class TenantPolicyKnobIds
{
    public const string AssociationTHigh = "association.t-high";
    public const string AssociationTLow = "association.t-low";
    public const string AttachmentsUnsafeHandling = "attachments.unsafe-handling";
    public const string AiActionLowRiskAllowed = "ai-action.low-risk-allowed";
    public const string MailboxRoutingRules = "mailbox.routing-rules";
    public const string ApprovalRouting = "approval.routing";
    public const string AdminPermissionScopes = "admin.permission-scopes";
    public const string AllowlistVersionPin = "allowlist.version-pin";
    public const string ClassifierExplanationLayerEnabled = "classifier.explanation-layer-enabled";
    public const string InboundAuthenticityStrictness = "inbound-authenticity.strictness";
    public const string ApprovalPriorityWeights = "approval.priority-weights";
    public const string NotificationThrottleCeilings = "notification.throttle-ceilings";
    public const string ReviewerBacklogThreshold = "notification.reviewer-backlog-threshold";
}
