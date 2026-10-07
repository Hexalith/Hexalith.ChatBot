using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Queries;
using Hexalith.ChatBot.Server.Lifecycle.AiExecution;

namespace Hexalith.ChatBot.Server.Authorization;

/// <summary>Exhaustive, closed command and query authority requirements.</summary>
internal sealed class ChatBotAuthorityCatalog
{
    private static readonly ChatBotAuthorityRequirement[] Definitions =
    [
        new(nameof(ApproveAiActorDisable), false, "policy", "DisableChangeId", false, false),
        new(nameof(ApproveAiActorQuarantine), false, "policy", "QuarantineChangeId", false, false),
        new(nameof(ApproveCommandCapabilityDisable), false, "policy", "DisableChangeId", false, false),
        new(nameof(ApproveCommandCapabilityQuarantine), false, "policy", "QuarantineChangeId", false, false),
        new(nameof(ApproveMailboxSourceDisable), false, "mailbox", "DisableChangeId", false, false),
        new(nameof(ApproveMailboxSourceQuarantine), false, "mailbox", "QuarantineChangeId", false, false),
        new(nameof(ApproveOutboundChannelDisable), false, "policy", "DisableChangeId", false, false),
        new(nameof(ApproveOutboundChannelQuarantine), false, "policy", "QuarantineChangeId", false, false),
        new(nameof(ApproveServiceClientDisable), false, "tenant", "DisableChangeId", false, false),
        new(nameof(ApproveServiceClientQuarantine), false, "tenant", "QuarantineChangeId", false, false),
        new(nameof(ApproveTenantPolicyChange), false, "policy", "PolicyChangeId", false, false),
        new(nameof(AssignTenantAdminRole), false, "tenant", "AssignmentId", false, true),
        new(nameof(AssociateEmailToProject), false, null, "AssociationId", true, false),
        new(nameof(CancelAiResponseGeneration), false, null, "ProjectId", true, false),
        new(nameof(CaptureMailboxMessageIntake), false, null, "IntakeId", false, false),
        new(nameof(CaptureTaskIntent), false, null, "ProjectId", true, true),
        new(nameof(CorrectEmailProjectAssociation), false, null, "AssociationId", true, false),
        new(nameof(CreateOutboundDraft), false, null, "DraftId", true, true),
        new(nameof(DecideAiActionApproval), false, null, "ProjectId", true, false),
        new(nameof(DecideOutboundApproval), false, null, "ApprovalId", true, false),
        new(nameof(DeferEmailProjectAssociation), false, null, "AssociationId", false, false),
        new(nameof(ExecuteAdminQueueOperation), false, "operate", "OperationId", false, false),
        new(nameof(ExecuteApprovedAIAction), false, null, "ProjectId", true, false),
        new(nameof(ExecuteApprovedOutboundDraft), false, null, "SendId", true, false),
        new(nameof(ExecuteLowRiskAIAssistance), false, null, "ProjectId", true, false),
        new(nameof(MarkAiActionProposalInvalidatedByCorrection), false, null, "ProjectId", true, false),
        new(nameof(MarkEmailAssociationNeedsReview), false, null, "AssociationId", false, false),
        new(nameof(MarkTaskIntentDisposition), false, null, "ProjectId", true, false),
        new(nameof(ProposeAIAction), false, null, "ProjectId", true, false),
        new(nameof(RecordGovernedNote), false, null, "NoteId", false, false),
        new(nameof(RecordMailboxProviderConnection), false, "mailbox", "ProviderConnectionChangeId", false, false),
        new(nameof(RecordProjectConversationMessage), false, null, "ProjectId", true, false),
        new(nameof(RejectEmailProjectAssociation), false, null, "AssociationId", false, false),
        new(nameof(RequestComplianceEscalation), false, "compliance", "EscalationId", false, false),
        new(nameof(RequestComplianceInvestigation), false, "compliance", "InvestigationId", false, false),
        new(nameof(RequestFailedWorkflowRetry), false, null, "RetryId", false, false),
        new(nameof(RequestOutboundSendApproval), false, null, "ApprovalId", true, false),
        new(nameof(ResolveMailboxMessageParticipants), false, null, "ResolutionId", false, true),
        new(nameof(ScoreMailboxMessageAssociation), false, null, "AssociationId", true, false),
        new(nameof(SetAssociationConfidenceThresholds), false, "policy", "PolicyId", false, false),
        new(nameof(SubmitAiActorDisable), false, "policy", "DisableChangeId", false, false),
        new(nameof(SubmitAiActorQuarantine), false, "policy", "QuarantineChangeId", false, false),
        new(nameof(SubmitAiActorRateLimit), false, "policy", "RateLimitChangeId", false, false),
        new(nameof(SubmitCommandCapabilityDisable), false, "policy", "DisableChangeId", false, false),
        new(nameof(SubmitCommandCapabilityQuarantine), false, "policy", "QuarantineChangeId", false, false),
        new(nameof(SubmitCommandCapabilityRateLimit), false, "policy", "RateLimitChangeId", false, false),
        new(nameof(SubmitConsentLawfulBasisRecord), false, "compliance", "RecordId", true, false),
        new(nameof(SubmitDataClassInventoryChange), false, "compliance", "InventoryChangeId", false, false),
        new(nameof(SubmitDeletionErasureRequest), false, "compliance", "DeletionRunId", false, false),
        new(nameof(SubmitEscalationPolicyChange), false, "policy", "EscalationPolicyChangeId", false, false),
        new(nameof(SubmitMailboxConfigurationChange), false, "mailbox", "ConfigurationChangeId", false, false),
        new(nameof(SubmitMailboxSourceDisable), false, "mailbox", "DisableChangeId", false, false),
        new(nameof(SubmitMailboxSourceQuarantine), false, "mailbox", "QuarantineChangeId", false, false),
        new(nameof(SubmitMailboxSourceRateLimit), false, "mailbox", "RateLimitChangeId", false, false),
        new(nameof(SubmitNotificationRoutingChange), false, "policy", "RoutingChangeId", false, false),
        new(nameof(SubmitOutboundChannelDisable), false, "policy", "DisableChangeId", false, false),
        new(nameof(SubmitOutboundChannelQuarantine), false, "policy", "QuarantineChangeId", false, false),
        new(nameof(SubmitOutboundChannelRateLimit), false, "policy", "RateLimitChangeId", false, false),
        new(nameof(SubmitRetentionConfigurationChange), false, "compliance", "RetentionChangeId", false, false),
        new(nameof(SubmitServiceClientDisable), false, "tenant", "DisableChangeId", false, false),
        new(nameof(SubmitServiceClientQuarantine), false, "tenant", "QuarantineChangeId", false, false),
        new(nameof(SubmitServiceClientRateLimit), false, "tenant", "RateLimitChangeId", false, false),
        new(nameof(SubmitTenantExportRequest), false, "compliance", "ExportRunId", false, false),
        new(nameof(SubmitTenantPolicyChange), false, "policy", "PolicyChangeId", false, false),
        new(ChatBotReadQueryTypes.AssociationRoutingStatus, true, null, "AssociationId", false, false),
        new(ChatBotReadQueryTypes.ProjectConversation, true, null, "ProjectId", true, false),
        new(ChatBotReadQueryTypes.TaskIntentReview, true, null, "ProjectId", true, false),
        new(ChatBotReadQueryTypes.OperationStatus, true, null, "OperationId", false, false),
        new(ChatBotReadQueryTypes.OperationAuditHistory, true, null, "OperationId", false, false),
        new(ChatBotReadQueryTypes.GovernedOperation, true, null, "NoteId", false, false),
        new(ChatBotReadQueryTypes.ComplianceAuditSearch, true, "compliance", null, false, false),
        new(ChatBotReadQueryTypes.ComplianceAuditDetail, true, "compliance", "AuditRecordRef", false, false),
        new(AiExecutionRecoveryOperations.List, true, "operate", null, false, false),
        new(AiExecutionRecoveryOperations.Recover, false, "operate", null, false, false),
    ];
    private readonly IReadOnlyDictionary<(bool, string), ChatBotAuthorityRequirement> _rows;

    /// <summary>Creates the canonical closed catalog.</summary>
    public ChatBotAuthorityCatalog() : this(Definitions) { }

    /// <summary>Validates all rows; duplicate, unknown, altered or missing rows fail closed.</summary>
    internal ChatBotAuthorityCatalog(IEnumerable<ChatBotAuthorityRequirement> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        Dictionary<(bool, string), ChatBotAuthorityRequirement> validated = [];
        foreach (ChatBotAuthorityRequirement row in rows)
        {
            if (!Definitions.Contains(row) || !validated.TryAdd((row.IsQuery, row.Operation), row))
            {
                throw new InvalidOperationException("Invalid authority catalog.");
            }
        }

        if (validated.Count != Definitions.Length)
        {
            throw new InvalidOperationException("Incomplete authority catalog.");
        }

        _rows = validated;
    }

    /// <summary>The immutable operation rows.</summary>
    public IReadOnlyCollection<ChatBotAuthorityRequirement> Requirements => _rows.Values.ToArray();
    /// <summary>Resolves only known exact operations.</summary>
    public ChatBotAuthorityRequirement? Find(string operation, bool isQuery)
        => _rows.GetValueOrDefault((isQuery, operation));
}
