using Hexalith.EventStore.DomainService;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Operations;

[assembly: RouteCatalogAdapterSource(typeof(Hexalith.ChatBot.Server.Routing.ChatBotCatalogAdapters))]

namespace Hexalith.ChatBot.Server.Routing;

/// <summary>Typed owner intent adapters bind complete existing payloads without publishing payload values.</summary>
public static class ChatBotCatalogAdapters
{
    /// <summary>Creates offline adapters; the actual gateway must install and observe these with its own options and key provider.</summary>
    public static IReadOnlyList<IDeclaredIdempotencyIntentAdapter> Create() => Array.AsReadOnly<IDeclaredIdempotencyIntentAdapter>([
        Adapter<GrantChatBotAuthority>(), Adapter<RevokeChatBotAuthority>(), Adapter<RecordGovernedNote>(),
        Adapter<SubmitTenantPolicyChange>(), Adapter<ApproveTenantPolicyChange>(), Adapter<SubmitNotificationRoutingChange>(),
        Adapter<SubmitMailboxSourceDisable>(), Adapter<ApproveMailboxSourceDisable>(), Adapter<SubmitServiceClientDisable>(),
        Adapter<ApproveServiceClientDisable>(), Adapter<SubmitAiActorDisable>(), Adapter<ApproveAiActorDisable>(),
        Adapter<SubmitCommandCapabilityDisable>(), Adapter<ApproveCommandCapabilityDisable>(), Adapter<SubmitOutboundChannelDisable>(),
        Adapter<ApproveOutboundChannelDisable>(), Adapter<SubmitOutboundChannelQuarantine>(), Adapter<ApproveOutboundChannelQuarantine>(),
        Adapter<SubmitCommandCapabilityQuarantine>(), Adapter<ApproveCommandCapabilityQuarantine>(), Adapter<SubmitAiActorQuarantine>(),
        Adapter<ApproveAiActorQuarantine>(), Adapter<SubmitServiceClientQuarantine>(), Adapter<ApproveServiceClientQuarantine>(),
        Adapter<SubmitMailboxSourceQuarantine>(), Adapter<ApproveMailboxSourceQuarantine>(), Adapter<SubmitMailboxSourceRateLimit>(),
        Adapter<SubmitServiceClientRateLimit>(), Adapter<SubmitAiActorRateLimit>(), Adapter<SubmitCommandCapabilityRateLimit>(),
        Adapter<SubmitOutboundChannelRateLimit>(), Adapter<CreateOutboundDraft>(), Adapter<RequestOutboundSendApproval>(),
        Adapter<DecideOutboundApproval>(), Adapter<ExecuteApprovedOutboundDraft>(), Adapter<CaptureMailboxMessageIntake>(),
        Adapter<RequestFailedWorkflowRetry>(), Adapter<RecordProjectConversationMessage>(), Adapter<CancelAiResponseGeneration>(),
        Adapter<CompleteAiResponseGenerationCancellation>(), Adapter<CaptureTaskIntent>(), Adapter<ProposeAIAction>(),
        Adapter<MarkAiActionProposalInvalidatedByCorrection>(), Adapter<ExecuteLowRiskAIAssistance>(), Adapter<CompleteLowRiskAiAssistance>(),
        Adapter<DecideAiActionApproval>(), Adapter<ExecuteApprovedAIAction>(), Adapter<MarkTaskIntentDisposition>(),
        Adapter<ResolveMailboxMessageParticipants>(), Adapter<ScoreMailboxMessageAssociation>(), Adapter<SetAssociationConfidenceThresholds>(),
        Adapter<AssociateEmailToProject>(), Adapter<RejectEmailProjectAssociation>(), Adapter<DeferEmailProjectAssociation>(),
        Adapter<MarkEmailAssociationNeedsReview>(), Adapter<CorrectEmailProjectAssociation>(), Adapter<StartMailboxAssociationCorrectionPropagation>(),
        Adapter<AcknowledgeMailboxAssociationCorrectionStoreInvalidated>(), Adapter<CompleteMailboxAssociationCorrectionPropagation>(), Adapter<DelayMailboxAssociationCorrectionPropagation>(),
        Adapter<SubmitEscalationPolicyChange>(), Adapter<SubmitMailboxConfigurationChange>(), Adapter<SubmitRetentionConfigurationChange>(),
        Adapter<SubmitDataClassInventoryChange>(),
    ]);

    private static DeclaredIdempotencyIntentAdapter<T> Adapter<T>() => new(
        ChatBotCatalogDeclarations.Commands.Single(command => command.Route.Execution!.HandlerType == typeof(T).FullName),
        payload => new Dictionary<string, object?> { ["payload-sha256"] = CanonicalIntentPayload.Sha256(payload) },
        completePayloadFields: payload => new Dictionary<string, object?> { ["payload-sha256"] = CanonicalIntentPayload.Sha256CanonicalJson(payload) });
}
