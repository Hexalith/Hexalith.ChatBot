using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>The closed, side-effect-free target mappings established by AcceptedCommandDispatcher.</summary>
internal static class ChatBotCanonicalDispatchTarget
{
    /// <summary>Resolves only an established dispatch target without reading protected state.</summary>
    public static bool TryResolve(string operation, JsonElement payload, out string? target)
    {
        string? property = operation switch
        {
            nameof(RecordGovernedNote) => "NoteId",
            nameof(CaptureMailboxMessageIntake) => "IntakeId",
            nameof(ResolveMailboxMessageParticipants) => "ResolutionId",
            nameof(ScoreMailboxMessageAssociation) or nameof(AssociateEmailToProject) or nameof(RejectEmailProjectAssociation)
                or nameof(DeferEmailProjectAssociation) or nameof(MarkEmailAssociationNeedsReview) or nameof(CorrectEmailProjectAssociation) => "AssociationId",
            nameof(SetAssociationConfidenceThresholds) => "PolicyId",
            nameof(SubmitTenantPolicyChange) or nameof(ApproveTenantPolicyChange) => "PolicyChangeId",
            nameof(SubmitNotificationRoutingChange) => "RoutingChangeId",
            nameof(SubmitAiActorDisable) or nameof(ApproveAiActorDisable) or nameof(SubmitCommandCapabilityDisable) or nameof(ApproveCommandCapabilityDisable)
                or nameof(SubmitMailboxSourceDisable) or nameof(ApproveMailboxSourceDisable) or nameof(SubmitOutboundChannelDisable) or nameof(ApproveOutboundChannelDisable)
                or nameof(SubmitServiceClientDisable) or nameof(ApproveServiceClientDisable) => "DisableChangeId",
            nameof(SubmitAiActorQuarantine) or nameof(ApproveAiActorQuarantine) or nameof(SubmitCommandCapabilityQuarantine) or nameof(ApproveCommandCapabilityQuarantine)
                or nameof(SubmitMailboxSourceQuarantine) or nameof(ApproveMailboxSourceQuarantine) or nameof(SubmitOutboundChannelQuarantine) or nameof(ApproveOutboundChannelQuarantine)
                or nameof(SubmitServiceClientQuarantine) or nameof(ApproveServiceClientQuarantine) => "QuarantineChangeId",
            nameof(RequestFailedWorkflowRetry) => "RetryId",
            nameof(RecordProjectConversationMessage) or nameof(ProposeAIAction) or nameof(ExecuteLowRiskAIAssistance) or nameof(DecideAiActionApproval)
                or nameof(ExecuteApprovedAIAction) or nameof(MarkAiActionProposalInvalidatedByCorrection) => "ProjectId",
            nameof(CancelAiResponseGeneration) => "ConversationId",
            nameof(CreateOutboundDraft) or nameof(RequestOutboundSendApproval) or nameof(DecideOutboundApproval) or nameof(ExecuteApprovedOutboundDraft) => "DraftId",
            _ => null,
        };
        target = null;
        if (property is null || payload.ValueKind != JsonValueKind.Object || !TryRead(payload, property, false, out target))
        {
            return false;
        }

        if (operation is nameof(ProposeAIAction) or nameof(ExecuteLowRiskAIAssistance) or nameof(DecideAiActionApproval)
            or nameof(ExecuteApprovedAIAction) or nameof(MarkAiActionProposalInvalidatedByCorrection))
        {
            if (!TryRead(payload, "StateOwnerAggregateId", true, out string? stateOwner)) { return false; }
            target = stateOwner ?? target;
        }

        return AuditMetadata.IsSafeStableIdentifier(target) && target != "*";
    }

    private static bool TryRead(JsonElement payload, string name, bool optional, out string? value)
    {
        JsonProperty[] properties = payload.EnumerateObject().Where(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)).ToArray();
        value = null;
        if (properties.Length == 0) { return optional; }
        if (properties.Length != 1) { return false; }
        if (optional && properties[0].Value.ValueKind == JsonValueKind.Null) { return true; }
        if (properties[0].Value.ValueKind != JsonValueKind.String) { return false; }
        value = properties[0].Value.GetString();
        return AuditMetadata.IsSafeStableIdentifier(value) && value != "*";
    }
}
