using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>
/// Hardcoded M0 spine allowlist containing only first-party governed commands. Every other command type is
/// rejected fail-closed at the gateway. This is deliberately distinct from the addendum's AI-action execution
/// allowlist (<c>Project.AppendConversationMessage</c>, Epic 4) and does not alter it.
/// </summary>
internal sealed class ChatBotSpineCommandAllowlist : ISpineCommandAllowlist
{
    private static readonly HashSet<string> AllowedCommandTypes =
        new(StringComparer.Ordinal)
        {
            nameof(RecordGovernedNote),
            nameof(CaptureMailboxMessageIntake),
            nameof(ResolveMailboxMessageParticipants),
            nameof(ScoreMailboxMessageAssociation),
            nameof(AssociateEmailToProject),
            nameof(RejectEmailProjectAssociation),
            nameof(DeferEmailProjectAssociation),
            nameof(MarkEmailAssociationNeedsReview),
            nameof(CorrectEmailProjectAssociation),
            nameof(SetAssociationConfidenceThresholds),
            nameof(SubmitTenantPolicyChange),
            nameof(ApproveTenantPolicyChange),
            nameof(SubmitMailboxConfigurationChange),
            nameof(SubmitMailboxSourceDisable),
            nameof(ApproveMailboxSourceDisable),
            nameof(SubmitMailboxSourceQuarantine),
            nameof(ApproveMailboxSourceQuarantine),
            nameof(SubmitMailboxSourceRateLimit),
            nameof(SubmitServiceClientDisable),
            nameof(ApproveServiceClientDisable),
            nameof(SubmitAiActorDisable),
            nameof(ApproveAiActorDisable),
            nameof(SubmitCommandCapabilityDisable),
            nameof(ApproveCommandCapabilityDisable),
            nameof(SubmitCommandCapabilityQuarantine),
            nameof(ApproveCommandCapabilityQuarantine),
            nameof(SubmitAiActorQuarantine),
            nameof(ApproveAiActorQuarantine),
            nameof(SubmitServiceClientQuarantine),
            nameof(ApproveServiceClientQuarantine),
            nameof(SubmitServiceClientRateLimit),
            nameof(SubmitAiActorRateLimit),
            nameof(SubmitCommandCapabilityRateLimit),
            nameof(SubmitNotificationRoutingChange),
            nameof(SubmitEscalationPolicyChange),
            nameof(RecordMailboxProviderConnection),
            nameof(RequestComplianceInvestigation),
            nameof(RequestComplianceEscalation),
            nameof(SubmitRetentionConfigurationChange),
            nameof(AssignTenantAdminRole),
            nameof(ExecuteAdminQueueOperation),
            nameof(RequestFailedWorkflowRetry),
            nameof(ProposeAIAction),
            nameof(ExecuteLowRiskAIAssistance),
            nameof(DecideAiActionApproval),
            nameof(ExecuteApprovedAIAction),
            nameof(MarkAiActionProposalInvalidatedByCorrection),
            nameof(CreateOutboundDraft),
            nameof(RequestOutboundSendApproval),
            nameof(DecideOutboundApproval),
            nameof(ExecuteApprovedOutboundDraft),
        };

    public bool IsAllowed(string? commandType)
        => !string.IsNullOrWhiteSpace(commandType) && AllowedCommandTypes.Contains(commandType);
}
