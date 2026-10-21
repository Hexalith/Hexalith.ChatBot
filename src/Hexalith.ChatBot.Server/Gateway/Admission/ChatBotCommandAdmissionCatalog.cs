using System.Collections.ObjectModel;
using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Gateway.Admission;

/// <summary>Versioned product mapping, including unsupported stable contracts and explicit legacy DTO mappings.</summary>
internal sealed class ChatBotCommandAdmissionCatalog
{
    private static readonly KeyValuePair<string, ChatBotAdmissionProfile>[] ConcreteDefinitions =
    [
        new(nameof(ApproveAiActorDisable), ChatBotAdmissionProfile.Governance),
        new(nameof(ApproveAiActorQuarantine), ChatBotAdmissionProfile.Governance),
        new(nameof(ApproveCommandCapabilityDisable), ChatBotAdmissionProfile.Governance),
        new(nameof(ApproveCommandCapabilityQuarantine), ChatBotAdmissionProfile.Governance),
        new(nameof(ApproveMailboxSourceDisable), ChatBotAdmissionProfile.Governance),
        new(nameof(ApproveMailboxSourceQuarantine), ChatBotAdmissionProfile.Governance),
        new(nameof(ApproveOutboundChannelDisable), ChatBotAdmissionProfile.Governance),
        new(nameof(ApproveOutboundChannelQuarantine), ChatBotAdmissionProfile.Governance),
        new(nameof(ApproveServiceClientDisable), ChatBotAdmissionProfile.Governance),
        new(nameof(ApproveServiceClientQuarantine), ChatBotAdmissionProfile.Governance),
        new(nameof(ApproveTenantPolicyChange), ChatBotAdmissionProfile.Governance),
        new(nameof(AssignTenantAdminRole), ChatBotAdmissionProfile.Governance),
        new(nameof(AssociateEmailToProject), ChatBotAdmissionProfile.Workflow),
        new(nameof(CancelAiResponseGeneration), ChatBotAdmissionProfile.GovernedChat),
        new(nameof(CaptureMailboxMessageIntake), ChatBotAdmissionProfile.MailboxIntake),
        new(nameof(CaptureTaskIntent), ChatBotAdmissionProfile.Workflow),
        new(nameof(CorrectEmailProjectAssociation), ChatBotAdmissionProfile.Workflow),
        new(nameof(CreateOutboundDraft), ChatBotAdmissionProfile.HumanEffect),
        new(nameof(DecideAiActionApproval), ChatBotAdmissionProfile.AiEffect),
        new(nameof(DecideOutboundApproval), ChatBotAdmissionProfile.HumanEffect),
        new(nameof(DeferEmailProjectAssociation), ChatBotAdmissionProfile.Workflow),
        new(nameof(ExecuteAdminQueueOperation), ChatBotAdmissionProfile.Governance),
        new(nameof(ExecuteApprovedAIAction), ChatBotAdmissionProfile.AiEffect),
        new(nameof(ExecuteApprovedOutboundDraft), ChatBotAdmissionProfile.HumanEffect),
        new(nameof(ExecuteLowRiskAIAssistance), ChatBotAdmissionProfile.AiRead),
        new(nameof(GrantChatBotAuthority), ChatBotAdmissionProfile.Governance),
        new(nameof(MarkAiActionProposalInvalidatedByCorrection), ChatBotAdmissionProfile.AiEffect),
        new(nameof(MarkEmailAssociationNeedsReview), ChatBotAdmissionProfile.Workflow),
        new(nameof(MarkTaskIntentDisposition), ChatBotAdmissionProfile.Workflow),
        new(nameof(ProposeAIAction), ChatBotAdmissionProfile.AiEffect),
        new(nameof(RecordGovernedNote), ChatBotAdmissionProfile.CommandExecution),
        new(nameof(RevokeChatBotAuthority), ChatBotAdmissionProfile.Governance),
        new(nameof(RecordMailboxProviderConnection), ChatBotAdmissionProfile.Governance),
        new(nameof(RecordProjectConversationMessage), ChatBotAdmissionProfile.GovernedChat),
        new(nameof(RejectEmailProjectAssociation), ChatBotAdmissionProfile.Workflow),
        new(nameof(RequestComplianceEscalation), ChatBotAdmissionProfile.DataRights),
        new(nameof(RequestComplianceInvestigation), ChatBotAdmissionProfile.DataRights),
        new(nameof(RequestFailedWorkflowRetry), ChatBotAdmissionProfile.CommandExecution),
        new(nameof(RequestOutboundSendApproval), ChatBotAdmissionProfile.HumanEffect),
        new(nameof(ResolveMailboxMessageParticipants), ChatBotAdmissionProfile.Workflow),
        new(nameof(ScoreMailboxMessageAssociation), ChatBotAdmissionProfile.Workflow),
        new(nameof(SetAssociationConfidenceThresholds), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitAiActorDisable), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitAiActorQuarantine), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitAiActorRateLimit), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitCommandCapabilityDisable), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitCommandCapabilityQuarantine), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitCommandCapabilityRateLimit), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitConsentLawfulBasisRecord), ChatBotAdmissionProfile.DataRights),
        new(nameof(SubmitDataClassInventoryChange), ChatBotAdmissionProfile.DataRights),
        new(nameof(SubmitDeletionErasureRequest), ChatBotAdmissionProfile.DataRights),
        new(nameof(SubmitEscalationPolicyChange), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitMailboxConfigurationChange), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitMailboxSourceDisable), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitMailboxSourceQuarantine), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitMailboxSourceRateLimit), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitNotificationRoutingChange), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitOutboundChannelDisable), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitOutboundChannelQuarantine), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitOutboundChannelRateLimit), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitRetentionConfigurationChange), ChatBotAdmissionProfile.DataRights),
        new(nameof(SubmitServiceClientDisable), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitServiceClientQuarantine), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitServiceClientRateLimit), ChatBotAdmissionProfile.Governance),
        new(nameof(SubmitTenantExportRequest), ChatBotAdmissionProfile.DataRights),
        new(nameof(SubmitTenantPolicyChange), ChatBotAdmissionProfile.Governance),
    ];
    private static readonly IReadOnlyDictionary<string, ChatBotAdmissionProfile> StableDefinitions =
        new ReadOnlyDictionary<string, ChatBotAdmissionProfile>(new Dictionary<string, ChatBotAdmissionProfile>(StringComparer.Ordinal)
        {
            ["CaptureMailboxEvent"] = ChatBotAdmissionProfile.MailboxIntake,
            ["ResolveInboundAuthenticityReview"] = ChatBotAdmissionProfile.MailboxIntake,
            ["ReprocessInboundAuthenticity"] = ChatBotAdmissionProfile.MailboxIntake,
            ["RetryMailboxIntake"] = ChatBotAdmissionProfile.MailboxIntake,
            ["ProposeEmailProjectAssociation"] = ChatBotAdmissionProfile.Workflow,
            ["AssociateEmailToProject"] = ChatBotAdmissionProfile.Workflow,
            ["ConfirmEmailProjectAssociation"] = ChatBotAdmissionProfile.Workflow,
            ["RejectEmailProjectAssociation"] = ChatBotAdmissionProfile.Workflow,
            ["DeferEmailProjectAssociation"] = ChatBotAdmissionProfile.Workflow,
            ["MarkEmailAssociationNeedsReview"] = ChatBotAdmissionProfile.Workflow,
            ["SkipEmailAssociation"] = ChatBotAdmissionProfile.Workflow,
            ["CorrectEmailProjectAssociation"] = ChatBotAdmissionProfile.Workflow,
            ["ReprocessEmailAssociation"] = ChatBotAdmissionProfile.Workflow,
            ["ResumeEmailAssociationReview"] = ChatBotAdmissionProfile.Workflow,
            ["MarkEmailAssociationFailed"] = ChatBotAdmissionProfile.Workflow,
            ["AcknowledgeAssociationCorrectionStore"] = ChatBotAdmissionProfile.Workflow,
            ["MarkAssociationCorrectionDelayed"] = ChatBotAdmissionProfile.Workflow,
            ["LinkOrResolveEmailParticipant"] = ChatBotAdmissionProfile.Workflow,
            ["MarkParticipantResolutionDisposition"] = ChatBotAdmissionProfile.Workflow,
            ["ResumeParticipantResolutionReview"] = ChatBotAdmissionProfile.Workflow,
            ["CaptureEmailAttachment"] = ChatBotAdmissionProfile.Workflow,
            ["StoreEmailAttachmentInProjectFolder"] = ChatBotAdmissionProfile.Workflow,
            ["MarkEmailAttachmentOutcome"] = ChatBotAdmissionProfile.Workflow,
            ["RetryAttachmentCapture"] = ChatBotAdmissionProfile.Workflow,
            ["CaptureTaskIntent"] = ChatBotAdmissionProfile.Workflow,
            ["MarkTaskIntentDisposition"] = ChatBotAdmissionProfile.Workflow,
            ["AddWorkflowResolutionAnnotation"] = ChatBotAdmissionProfile.Workflow,
            ["SupersedeWorkflowDecision"] = ChatBotAdmissionProfile.Workflow,
            ["SubmitGovernedChatMessage"] = ChatBotAdmissionProfile.GovernedChat,
            ["CancelGovernedChatRequest"] = ChatBotAdmissionProfile.GovernedChat,
            ["StopGovernedChatResponse"] = ChatBotAdmissionProfile.GovernedChat,
            ["RetryGovernedChatMessage"] = ChatBotAdmissionProfile.GovernedChat,
            ["ExecuteLowRiskAssistance"] = ChatBotAdmissionProfile.AiRead,
            ["ProposeAIAction"] = ChatBotAdmissionProfile.AiEffect,
            ["ApproveAIAction"] = ChatBotAdmissionProfile.AiEffect,
            ["RejectAIAction"] = ChatBotAdmissionProfile.AiEffect,
            ["RequestAIActionRevision"] = ChatBotAdmissionProfile.AiEffect,
            ["CancelAIAction"] = ChatBotAdmissionProfile.AiEffect,
            ["ExecuteApprovedProjectCommand"] = ChatBotAdmissionProfile.AiEffect,
            ["RetryApprovedAIActionExecution"] = ChatBotAdmissionProfile.AiEffect,
            ["CreateOutboundProjectEmailDraft"] = ChatBotAdmissionProfile.HumanEffect,
            ["SendApprovedProjectEmail"] = ChatBotAdmissionProfile.HumanEffect,
            ["ReconcileOutboundSendOutcome"] = ChatBotAdmissionProfile.HumanEffect,
            ["RetryCommandExecution"] = ChatBotAdmissionProfile.CommandExecution,
            ["GrantServiceClientPermission"] = ChatBotAdmissionProfile.Governance,
            ["RevokeServiceClientPermission"] = ChatBotAdmissionProfile.Governance,
            ["ConfigureMailboxSource"] = ChatBotAdmissionProfile.Governance,
            ["PauseMailboxSource"] = ChatBotAdmissionProfile.Governance,
            ["ResumeMailboxSource"] = ChatBotAdmissionProfile.Governance,
            ["DisableMailboxSource"] = ChatBotAdmissionProfile.Governance,
            ["UpdateTenantPolicy"] = ChatBotAdmissionProfile.Governance,
            ["UpdateCommandAllowlist"] = ChatBotAdmissionProfile.Governance,
            ["ConfigureNotificationRouting"] = ChatBotAdmissionProfile.Governance,
            ["UpdateOperationalLimits"] = ChatBotAdmissionProfile.Governance,
            ["ApplySafetyControl"] = ChatBotAdmissionProfile.Governance,
            ["ReleaseSafetyControl"] = ChatBotAdmissionProfile.Governance,
            ["GrantChatBotAdminRole"] = ChatBotAdmissionProfile.Governance,
            ["ChangeChatBotAdminRole"] = ChatBotAdmissionProfile.Governance,
            ["RevokeChatBotAdminRole"] = ChatBotAdmissionProfile.Governance,
            ["PauseQueuePartition"] = ChatBotAdmissionProfile.Governance,
            ["ResumeQueuePartition"] = ChatBotAdmissionProfile.Governance,
            ["ClaimQueueItem"] = ChatBotAdmissionProfile.Governance,
            ["AssignQueueItem"] = ChatBotAdmissionProfile.Governance,
            ["InitiateDataExport"] = ChatBotAdmissionProfile.DataRights,
            ["RetryDataExport"] = ChatBotAdmissionProfile.DataRights,
            ["InitiateDataErasure"] = ChatBotAdmissionProfile.DataRights,
            ["RetryDataErasure"] = ChatBotAdmissionProfile.DataRights,
            ["PlaceLegalHold"] = ChatBotAdmissionProfile.DataRights,
            ["ReleaseLegalHold"] = ChatBotAdmissionProfile.DataRights,
            ["ExecuteRetentionDisposition"] = ChatBotAdmissionProfile.DataRights,
            ["RetryRetentionDisposition"] = ChatBotAdmissionProfile.DataRights,
            ["RebuildAuditProjection"] = ChatBotAdmissionProfile.ProjectionDelivery,
            ["DispatchWorkflowNotification"] = ChatBotAdmissionProfile.ProjectionDelivery,
            ["RetryWorkflowNotification"] = ChatBotAdmissionProfile.ProjectionDelivery,
        });
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> StableConcreteDefinitions =
        new ReadOnlyDictionary<string, IReadOnlyList<string>>(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["CaptureMailboxEvent"] = [nameof(CaptureMailboxMessageIntake)],
            ["ResolveInboundAuthenticityReview"] = [],
            ["ReprocessInboundAuthenticity"] = [],
            ["RetryMailboxIntake"] = [],
            ["ProposeEmailProjectAssociation"] = [nameof(ScoreMailboxMessageAssociation)],
            ["AssociateEmailToProject"] = [nameof(AssociateEmailToProject)],
            ["ConfirmEmailProjectAssociation"] = [nameof(AssociateEmailToProject)],
            ["RejectEmailProjectAssociation"] = [nameof(RejectEmailProjectAssociation)],
            ["DeferEmailProjectAssociation"] = [nameof(DeferEmailProjectAssociation)],
            ["MarkEmailAssociationNeedsReview"] = [nameof(MarkEmailAssociationNeedsReview)],
            ["SkipEmailAssociation"] = [],
            ["CorrectEmailProjectAssociation"] = [nameof(CorrectEmailProjectAssociation)],
            ["ReprocessEmailAssociation"] = [],
            ["ResumeEmailAssociationReview"] = [],
            ["MarkEmailAssociationFailed"] = [],
            ["AcknowledgeAssociationCorrectionStore"] = [],
            ["MarkAssociationCorrectionDelayed"] = [],
            ["LinkOrResolveEmailParticipant"] = [nameof(ResolveMailboxMessageParticipants)],
            ["MarkParticipantResolutionDisposition"] = [],
            ["ResumeParticipantResolutionReview"] = [],
            ["CaptureEmailAttachment"] = [],
            ["StoreEmailAttachmentInProjectFolder"] = [],
            ["MarkEmailAttachmentOutcome"] = [],
            ["RetryAttachmentCapture"] = [],
            ["CaptureTaskIntent"] = [nameof(CaptureTaskIntent)],
            ["MarkTaskIntentDisposition"] = [nameof(MarkTaskIntentDisposition)],
            ["AddWorkflowResolutionAnnotation"] = [],
            ["SupersedeWorkflowDecision"] = [],
            ["SubmitGovernedChatMessage"] = [],
            ["CancelGovernedChatRequest"] = [nameof(CancelAiResponseGeneration)],
            ["StopGovernedChatResponse"] = [nameof(CancelAiResponseGeneration)],
            ["RetryGovernedChatMessage"] = [],
            ["ExecuteLowRiskAssistance"] = [nameof(ExecuteLowRiskAIAssistance)],
            ["ProposeAIAction"] = [nameof(ProposeAIAction)],
            ["ApproveAIAction"] = [nameof(DecideAiActionApproval)],
            ["RejectAIAction"] = [nameof(DecideAiActionApproval)],
            ["RequestAIActionRevision"] = [nameof(DecideAiActionApproval)],
            ["CancelAIAction"] = [],
            ["ExecuteApprovedProjectCommand"] = [nameof(ExecuteApprovedAIAction)],
            ["RetryApprovedAIActionExecution"] = [],
            ["CreateOutboundProjectEmailDraft"] = [nameof(CreateOutboundDraft)],
            ["SendApprovedProjectEmail"] = [nameof(ExecuteApprovedOutboundDraft)],
            ["ReconcileOutboundSendOutcome"] = [],
            ["RetryCommandExecution"] = [nameof(RequestFailedWorkflowRetry)],
            ["GrantServiceClientPermission"] = [nameof(GrantChatBotAuthority)],
            ["RevokeServiceClientPermission"] = [nameof(RevokeChatBotAuthority)],
            ["ConfigureMailboxSource"] = [nameof(SubmitMailboxConfigurationChange), nameof(RecordMailboxProviderConnection)],
            ["PauseMailboxSource"] = [],
            ["ResumeMailboxSource"] = [],
            ["DisableMailboxSource"] = [nameof(SubmitMailboxSourceDisable), nameof(ApproveMailboxSourceDisable)],
            ["UpdateTenantPolicy"] = [nameof(SubmitTenantPolicyChange), nameof(ApproveTenantPolicyChange), nameof(SetAssociationConfidenceThresholds)],
            ["UpdateCommandAllowlist"] = [],
            ["ConfigureNotificationRouting"] = [nameof(SubmitNotificationRoutingChange)],
            ["UpdateOperationalLimits"] = [],
            ["ApplySafetyControl"] = [nameof(ApproveAiActorDisable), nameof(ApproveAiActorQuarantine), nameof(ApproveCommandCapabilityDisable), nameof(ApproveCommandCapabilityQuarantine), nameof(ApproveMailboxSourceQuarantine), nameof(ApproveOutboundChannelDisable), nameof(ApproveOutboundChannelQuarantine), nameof(ApproveServiceClientDisable), nameof(ApproveServiceClientQuarantine), nameof(SubmitAiActorDisable), nameof(SubmitAiActorQuarantine), nameof(SubmitAiActorRateLimit), nameof(SubmitCommandCapabilityDisable), nameof(SubmitCommandCapabilityQuarantine), nameof(SubmitCommandCapabilityRateLimit), nameof(SubmitMailboxSourceQuarantine), nameof(SubmitMailboxSourceRateLimit), nameof(SubmitOutboundChannelDisable), nameof(SubmitOutboundChannelQuarantine), nameof(SubmitOutboundChannelRateLimit), nameof(SubmitServiceClientDisable), nameof(SubmitServiceClientQuarantine), nameof(SubmitServiceClientRateLimit)],
            ["ReleaseSafetyControl"] = [],
            ["GrantChatBotAdminRole"] = [nameof(AssignTenantAdminRole), nameof(GrantChatBotAuthority)],
            ["ChangeChatBotAdminRole"] = [],
            ["RevokeChatBotAdminRole"] = [nameof(RevokeChatBotAuthority)],
            ["PauseQueuePartition"] = [nameof(ExecuteAdminQueueOperation)],
            ["ResumeQueuePartition"] = [nameof(ExecuteAdminQueueOperation)],
            ["ClaimQueueItem"] = [nameof(ExecuteAdminQueueOperation)],
            ["AssignQueueItem"] = [nameof(ExecuteAdminQueueOperation)],
            ["InitiateDataExport"] = [nameof(SubmitTenantExportRequest)],
            ["RetryDataExport"] = [],
            ["InitiateDataErasure"] = [nameof(SubmitDeletionErasureRequest)],
            ["RetryDataErasure"] = [],
            ["PlaceLegalHold"] = [],
            ["ReleaseLegalHold"] = [],
            ["ExecuteRetentionDisposition"] = [nameof(SubmitRetentionConfigurationChange)],
            ["RetryRetentionDisposition"] = [],
            ["RebuildAuditProjection"] = [],
            ["DispatchWorkflowNotification"] = [],
            ["RetryWorkflowNotification"] = [],
        });
    private readonly IReadOnlyDictionary<string, ChatBotAdmissionProfile> _commands;

    /// <summary>Creates the product mapping.</summary>
    public ChatBotCommandAdmissionCatalog() : this(ConcreteDefinitions) { }

    /// <summary>Rejects duplicate, changed, missing, unknown or undefined mappings at construction.</summary>
    internal ChatBotCommandAdmissionCatalog(IEnumerable<KeyValuePair<string, ChatBotAdmissionProfile>> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        Dictionary<string, ChatBotAdmissionProfile> commands = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, ChatBotAdmissionProfile> row in definitions)
        {
            if (!Enum.IsDefined(row.Value) || !ConcreteDefinitions.Contains(row) || !commands.TryAdd(row.Key, row.Value))
            {
                throw new InvalidOperationException("Invalid command admission catalog.");
            }
        }
        if (commands.Count != ConcreteDefinitions.Length)
        {
            throw new InvalidOperationException("Incomplete command admission catalog.");
        }
        _commands = new ReadOnlyDictionary<string, ChatBotAdmissionProfile>(commands);
    }

    /// <summary>Explicit stable-to-concrete compatibility map; empty rows remain unsupported, and shared DTOs keep one profile.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> StableConcreteContracts => StableConcreteDefinitions;

    /// <summary>The immutable versioned product identifier of each closed profile.</summary>
    public static string ProfileId(ChatBotAdmissionProfile profile) => profile switch
    {
        ChatBotAdmissionProfile.MailboxIntake => "mailbox-intake-v1",
        ChatBotAdmissionProfile.Workflow => "workflow-v1",
        ChatBotAdmissionProfile.GovernedChat => "governed-chat-v1",
        ChatBotAdmissionProfile.AiRead => "ai-read-v1",
        ChatBotAdmissionProfile.AiEffect => "ai-effect-v1",
        ChatBotAdmissionProfile.HumanEffect => "human-effect-v1",
        ChatBotAdmissionProfile.CommandExecution => "command-execution-v1",
        ChatBotAdmissionProfile.Governance => "governance-v1",
        ChatBotAdmissionProfile.DataRights => "data-rights-v1",
        ChatBotAdmissionProfile.ProjectionDelivery => "projection-delivery-v1",
        _ => throw new InvalidOperationException("Unknown admission profile."),
    };

    /// <summary>The product-required owner proofs; profiles cannot omit, add or replicate these checks.</summary>
    public static IReadOnlyList<string> RequiredStages(ChatBotAdmissionProfile profile) => profile switch
    {
        ChatBotAdmissionProfile.MailboxIntake => ["provider-identity-deduplication", "authenticity-state", "a6-retention", "attachment-quarantine"],
        ChatBotAdmissionProfile.Workflow => ["family-state", "evidence-freshness", "project-authority", "correction-impact-manifest"],
        ChatBotAdmissionProfile.GovernedChat => ["conversation-authority", "origin-instruction-boundary", "attempt-identity-revision", "stream-race-guard"],
        ChatBotAdmissionProfile.AiRead => ["origin-instruction-boundary", "policy-context-scope", "project-authority"],
        ChatBotAdmissionProfile.AiEffect => ["origin-instruction-boundary", "human-approval-lifetime-drift", "allowlist-effect-target"],
        ChatBotAdmissionProfile.HumanEffect => ["frozen-content-recipients", "sender-authority", "human-approval-lifetime-drift", "provider-evidence", "no-blind-retry"],
        ChatBotAdmissionProfile.CommandExecution => ["exposed-command-effect", "caller-authority", "prior-attempt-state", "target-idempotency-concurrency", "uncertain-effect-denial"],
        ChatBotAdmissionProfile.Governance => ["closed-role-scope", "separation-of-duty", "independent-approval", "safer-state"],
        ChatBotAdmissionProfile.DataRights => ["request-authority", "tenantowner-compliance-approval", "a6-policy", "hold-precedence", "owner-command-acknowledgement"],
        ChatBotAdmissionProfile.ProjectionDelivery => ["source-watermark-or-recipient-redaction-routing"],
        _ => throw new InvalidOperationException("Unknown admission profile."),
    };

    /// <summary>All currently supported concrete command contracts, each mapped exactly once.</summary>
    public IReadOnlyDictionary<string, ChatBotAdmissionProfile> Commands => _commands;

    /// <summary>All product stable contracts. Membership does not enable an unsupported concrete successor.</summary>
    public static IReadOnlyDictionary<string, ChatBotAdmissionProfile> StableCommands => StableDefinitions;

    /// <summary>Resolves exact concrete contracts only; stable aliases cannot dispatch invented DTOs.</summary>
    public ChatBotAdmissionProfile? Find(string? command) => command is not null && _commands.TryGetValue(command, out ChatBotAdmissionProfile profile) ? profile : null;
}
