using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Queries;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Serialization;
using Hexalith.EventStore.DomainService;

[assembly: RouteCatalogDeclarationSource(typeof(Hexalith.ChatBot.Server.Routing.ChatBotCatalogDeclarations))]

namespace Hexalith.ChatBot.Server.Routing;

/// <summary>Owner-authored inventory of ChatBot's existing SDK receivers; metadata grants no runtime authority.</summary>
public static class ChatBotCatalogDeclarations
{
    /// <summary>Exact command payloads handled by the aggregate, including the four refusal-only policy fallbacks.</summary>
    public static IReadOnlyList<Type> CommandTypes { get; } = Array.AsReadOnly<Type>([
        typeof(GrantChatBotAuthority), typeof(RevokeChatBotAuthority), typeof(RecordGovernedNote),
        typeof(SubmitTenantPolicyChange), typeof(ApproveTenantPolicyChange), typeof(SubmitNotificationRoutingChange),
        typeof(SubmitMailboxSourceDisable), typeof(ApproveMailboxSourceDisable), typeof(SubmitServiceClientDisable),
        typeof(ApproveServiceClientDisable), typeof(SubmitAiActorDisable), typeof(ApproveAiActorDisable),
        typeof(SubmitCommandCapabilityDisable), typeof(ApproveCommandCapabilityDisable), typeof(SubmitOutboundChannelDisable),
        typeof(ApproveOutboundChannelDisable), typeof(SubmitOutboundChannelQuarantine), typeof(ApproveOutboundChannelQuarantine),
        typeof(SubmitCommandCapabilityQuarantine), typeof(ApproveCommandCapabilityQuarantine), typeof(SubmitAiActorQuarantine),
        typeof(ApproveAiActorQuarantine), typeof(SubmitServiceClientQuarantine), typeof(ApproveServiceClientQuarantine),
        typeof(SubmitMailboxSourceQuarantine), typeof(ApproveMailboxSourceQuarantine), typeof(SubmitMailboxSourceRateLimit),
        typeof(SubmitServiceClientRateLimit), typeof(SubmitAiActorRateLimit), typeof(SubmitCommandCapabilityRateLimit),
        typeof(SubmitOutboundChannelRateLimit), typeof(CreateOutboundDraft), typeof(RequestOutboundSendApproval),
        typeof(DecideOutboundApproval), typeof(ExecuteApprovedOutboundDraft), typeof(CaptureMailboxMessageIntake),
        typeof(RequestFailedWorkflowRetry), typeof(RecordProjectConversationMessage), typeof(CancelAiResponseGeneration),
        typeof(CompleteAiResponseGenerationCancellation), typeof(CaptureTaskIntent), typeof(ProposeAIAction),
        typeof(MarkAiActionProposalInvalidatedByCorrection), typeof(ExecuteLowRiskAIAssistance), typeof(CompleteLowRiskAiAssistance),
        typeof(DecideAiActionApproval), typeof(ExecuteApprovedAIAction), typeof(MarkTaskIntentDisposition),
        typeof(ResolveMailboxMessageParticipants), typeof(ScoreMailboxMessageAssociation), typeof(SetAssociationConfidenceThresholds),
        typeof(AssociateEmailToProject), typeof(RejectEmailProjectAssociation), typeof(DeferEmailProjectAssociation),
        typeof(MarkEmailAssociationNeedsReview), typeof(CorrectEmailProjectAssociation), typeof(StartMailboxAssociationCorrectionPropagation),
        typeof(AcknowledgeMailboxAssociationCorrectionStoreInvalidated), typeof(CompleteMailboxAssociationCorrectionPropagation), typeof(DelayMailboxAssociationCorrectionPropagation),
        typeof(SubmitEscalationPolicyChange), typeof(SubmitMailboxConfigurationChange), typeof(SubmitRetentionConfigurationChange),
        typeof(SubmitDataClassInventoryChange),
    ]);

    /// <summary>SDK mutation admission is independent of ChatBot's existing coarse reservations and retention windows.</summary>
    public static IReadOnlyList<CommandIntentDeclaration> Commands { get; } = Array.AsReadOnly(
        CommandTypes.Select(Command).ToArray());

    /// <summary>Declarations attached to all eight actual query handlers.</summary>
    public static IReadOnlyList<RouteContractDeclaration> Queries => Array.AsReadOnly<RouteContractDeclaration>([
        AssociationRoutingStatusQueryHandler.CatalogDeclaration,
        ProjectConversationQueryHandler.CatalogDeclaration,
        TaskIntentReviewQueryHandler.CatalogDeclaration,
        OperationStatusQueryHandler.CatalogDeclaration,
        OperationAuditHistoryQueryHandler.CatalogDeclaration,
        GovernedOperationQueryHandler.CatalogDeclaration,
        ComplianceAuditSearchQueryHandler.CatalogDeclaration,
        ComplianceAuditDetailQueryHandler.CatalogDeclaration,
    ]);

    /// <summary>The actual legacy dispatcher returns one chatbot summary; its outputs are not callable handlers.</summary>
    public static IReadOnlyList<RouteContractDeclaration> Projections { get; } = Array.AsReadOnly<RouteContractDeclaration>([
        new("chatbot-legacy-projection", "projection", "chatbot", null, "chatbot", "v1", null,
            "workload", EventStoreOperationIdentifiers.DomainServiceProject, null, [],
            new(typeof(ChatBotDomainProjectionHandler).FullName!, "exact", ["chatbot"], "domain-service", null, ["chatbot"])),
    ]);

    /// <summary>Complete finite owner inventory, independent of deployment settings.</summary>
    public static IReadOnlyList<RouteContractDeclaration> Routes => Array.AsReadOnly<RouteContractDeclaration>([
        .. Commands.Select(command => command.Route), .. Queries, .. Projections,
    ]);

    /// <summary>Actual workload-only receiver contracts. Enrollment does not introduce a machine gateway surface.</summary>
    public static IReadOnlyList<RouteOperationContract> Operations => Array.AsReadOnly(Routes.Select(route =>
    {
        (Type request, Type result) = route.Kind switch
        {
            "command" => (CommandTypes.Single(type => type.FullName == route.Execution!.HandlerType), typeof(DomainServiceWireResult)),
            "query" => (QueryPayloadType(route.MessageType!), typeof(QueryResult)),
            _ => (typeof(ProjectionRequest), typeof(ProjectionResponse)),
        };
        return RouteOperationSchema.Create(route, request, result, "internal", "UI-only",
            route.Kind == "command" ? EventStorePayloadSerialization.Options : new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }).ToArray());

    /// <summary>Declares a computed domain-service query; its existing response tags are not SDK projection-actor names.</summary>
    internal static RouteContractDeclaration Query(string wire, Type handler) => new(
        "chatbot-query-" + wire, "query", "chatbot", wire, null, "v1", null, "workload",
        EventStoreOperationIdentifiers.DomainServiceQuery, null, [],
        new(handler.FullName!, "exact", [wire], "domain-service", "computed", []));

    private static CommandIntentDeclaration Command(Type type)
    {
        string id = string.Concat(type.Name.Select((character, index) => char.IsUpper(character) && index > 0
            ? "-" + char.ToLowerInvariant(character) : char.ToLowerInvariant(character).ToString()));
        return new(new("chatbot-command-" + id, "command", "chatbot", type.Name, null, "v1", 1, "workload",
            EventStoreOperationIdentifiers.DomainServiceProcess, null, [],
            new(type.FullName!, "clr-short-name-v1", [type.Name], "domain-service", null, [])),
            "hexalith:chatbot:" + id + ":intent-v1", "chatbot:" + id, "chatbot:" + id + ":local-policy-v1",
            new("hexalith:chatbot:" + id + ":descriptor-v1", 1,
                [new("target", "string"), new("payload-sha256", "string"), new("acting-subject", "string")]),
            IdempotencyReplayRetentionTier.Mutation, true);
    }

    private static Type QueryPayloadType(string wire) => wire switch
    {
        ChatBotReadQueryTypes.AssociationRoutingStatus => typeof(AssociationRoutingStatusQuery),
        ChatBotReadQueryTypes.ProjectConversation => typeof(ProjectConversationQuery),
        ChatBotReadQueryTypes.TaskIntentReview => typeof(TaskIntentReviewQuery),
        ChatBotReadQueryTypes.OperationStatus => typeof(OperationStatusQuery),
        ChatBotReadQueryTypes.OperationAuditHistory => typeof(OperationAuditHistoryQuery),
        ChatBotReadQueryTypes.GovernedOperation => typeof(GovernedOperationQuery),
        ChatBotReadQueryTypes.ComplianceAuditSearch => typeof(ComplianceAuditSearchQuery),
        ChatBotReadQueryTypes.ComplianceAuditDetail => typeof(ComplianceAuditDetailQuery),
        _ => throw new InvalidOperationException("The query has no owned payload contract."),
    };
}
