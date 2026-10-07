using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Tests.TrustedAuthority;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.EventStore.Contracts.Commands;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

/// <summary>Checks canonical targets against real dispatcher submissions and post-effect continuation.</summary>
public sealed partial class AcceptedCommandDispatcherTests
{
    private static readonly string[] UnsupportedSdkCommands =
    [
        nameof(AssignTenantAdminRole), nameof(CaptureTaskIntent), nameof(ExecuteAdminQueueOperation),
        nameof(MarkTaskIntentDisposition), nameof(RecordMailboxProviderConnection), nameof(RequestComplianceEscalation),
        nameof(RequestComplianceInvestigation), nameof(SubmitAiActorRateLimit), nameof(SubmitCommandCapabilityRateLimit),
        nameof(SubmitConsentLawfulBasisRecord), nameof(SubmitDataClassInventoryChange), nameof(SubmitDeletionErasureRequest),
        nameof(SubmitEscalationPolicyChange), nameof(SubmitMailboxConfigurationChange), nameof(SubmitMailboxSourceRateLimit),
        nameof(SubmitOutboundChannelRateLimit), nameof(SubmitRetentionConfigurationChange), nameof(SubmitServiceClientRateLimit),
        nameof(SubmitTenantExportRequest),
    ];

    /// <summary>Includes every supported operation and each explicit state-owner override.</summary>
    public static IEnumerable<object[]> CanonicalTargets()
    {
        foreach (ChatBotAuthorityRequirement row in new ChatBotAuthorityCatalog().Requirements.Where(static row => !row.IsQuery && typeof(IChatBotCommand).Assembly.GetType($"Hexalith.ChatBot.Contracts.Commands.{row.Operation}") is not null))
        {
            if (UnsupportedSdkCommands.Contains(row.Operation, StringComparer.Ordinal)) { continue; }
            yield return [row.Operation, false];
            if (row.Operation is nameof(ProposeAIAction) or nameof(ExecuteLowRiskAIAssistance) or nameof(DecideAiActionApproval)
                or nameof(ExecuteApprovedAIAction) or nameof(MarkAiActionProposalInvalidatedByCorrection))
            {
                yield return [row.Operation, true];
            }
        }
    }

    /// <summary>Pins every operation intentionally unsupported by unmarked SDK admission.</summary>
    [Fact]
    public void UnmarkedSdkUnsupportedCommandsAreAnExplicitClosedList()
    {
        string[] unsupported = new ChatBotAuthorityCatalog().Requirements.Where(static row => !row.IsQuery && typeof(IChatBotCommand).Assembly.GetType($"Hexalith.ChatBot.Contracts.Commands.{row.Operation}") is not null)
            .Where(static row => !ChatBotCanonicalDispatchTarget.IsSupported(row.Operation)).Select(static row => row.Operation).Order(StringComparer.Ordinal).ToArray();
        unsupported.ShouldBe(UnsupportedSdkCommands.Order(StringComparer.Ordinal).ToArray());
        foreach (string operation in UnsupportedSdkCommands)
        {
            ChatBotCanonicalDispatchTarget.TryResolve(operation, GenericTargetPayload(), out _).ShouldBeFalse();
        }
    }

    /// <summary>Each canonical target equals the actual SDK aggregate after plan enrichment.</summary>
    [Theory]
    [MemberData(nameof(CanonicalTargets))]
    public async Task CanonicalTargetEqualsEveryActualSdkAggregate(string operation, bool stateOwnerOverride)
    {
        JsonElement payload = TargetPayload(operation);
        if (stateOwnerOverride)
        {
            Dictionary<string, JsonElement> fields = payload.EnumerateObject().ToDictionary(static field => field.Name, static field => field.Value.Clone(), StringComparer.OrdinalIgnoreCase);
            fields["StateOwnerAggregateId"] = JsonSerializer.SerializeToElement("conversation-owner-target");
            payload = JsonSerializer.SerializeToElement(fields);
        }
        ChatBotCanonicalDispatchTarget.TryResolve(operation, payload, out string? canonical).ShouldBeTrue(operation);
        ChatBotGatewayContext context = operation switch
        {
            nameof(CreateOutboundDraft) => OutboundDraftContext(OutboundDraft("draft-target")),
            nameof(ExecuteApprovedOutboundDraft) => OutboundSendContext(OutboundSend("send-target")),
            _ => Context(payload, commandType: operation),
        };
        context.SetRiskClassification(ChatBotRiskClassification.Classified(LowRiskClassification()));
        context.SetApprovalResult(ChatBotApprovalResult.AllowedLowRiskExecution("policy-snap-001", "low-risk-execute-allowed"));
        RecordingEventStoreGatewayClient sdk = new();
        AcceptedCommandDispatcher dispatcher = new(sdk, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock(),
            conversationWriter: new RecordingConversationWriter(), outboundMailboxSender: new SpyOutboundMailboxSender());
        ChatBotDispatchResult result = await dispatcher.DispatchAsync(context, TestContext.Current.CancellationToken);
        SubmitCommandRequest submitted = sdk.Submitted.ShouldHaveSingleItem();
        submitted.CommandType.ShouldBe(operation);
        submitted.AggregateId.ShouldBe(canonical);
        result.ResourceId.ShouldBe(canonical);
        if (stateOwnerOverride) { canonical.ShouldBe("conversation-owner-target"); }
    }

    /// <summary>Expiry after an external effect does not prevent its receipt or workflow startup.</summary>
    [Theory]
    [InlineData("sdk-correction")]
    [InlineData("sdk-ingestion")]
    [InlineData("conversation-writer")]
    public async Task AuthorityLapseAfterIrreversibleEffectStillCompletesSubmissionAndWorkflow(string effect)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => evidence with { ExpiresAt = clock.UtcNow.AddSeconds(1) } };
        string operation = effect switch
        {
            "sdk-correction" => nameof(CorrectEmailProjectAssociation),
            "sdk-ingestion" => nameof(AssociateEmailToProject),
            _ => nameof(ExecuteApprovedAIAction),
        };
        JsonElement payload = TargetPayload(operation);
        ChatBotRequestContext bound = TrustedAuthorityFixture.Context(origin: ChatBotSurfaceOrigin.Ui);
        ChatBotAuthorityDecision decision = await TrustedAuthorityFixture.Authorizer(clock, owner).AuthorizeAsync(bound, operation, false, payload, TestContext.Current.CancellationToken);
        decision.IsAllowed.ShouldBeTrue();
        ChatBotGatewayContext context = Context(payload, commandType: operation) with { Actor = new(bound.SubjectId, decision.Principal!, bound.ActorClass, bound.ServiceClientId, bound) };
        RecordingEventStoreGatewayClient sdk = new() { OnSubmission = effect.StartsWith("sdk-", StringComparison.Ordinal) ? () => clock.UtcNow += TimeSpan.FromSeconds(2) : null };
        RecordingConversationWriter writer = new() { OnPrepared = effect == "conversation-writer" ? () => clock.UtcNow += TimeSpan.FromSeconds(2) : null };
        RecordingCorrectionPropagationCoordinator correction = new();
        RecordingAuthorityIngestionCoordinator ingestion = new();
        AcceptedCommandDispatcher dispatcher = new(sdk, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), clock,
            correctionPropagation: correction, ingestionBinding: ingestion, conversationWriter: writer);
        await dispatcher.DispatchAsync(context, TestContext.Current.CancellationToken);
        decision.Principal!.IsCurrent(clock.UtcNow).ShouldBeFalse();
        context.ExternalEffectAttempted.ShouldBeTrue();
        context.SdkSubmissionAccepted.ShouldBeTrue();
        sdk.Submitted.ShouldHaveSingleItem();
        if (effect == "sdk-correction") { correction.Requests.ShouldHaveSingleItem(); }
        if (effect == "sdk-ingestion") { ingestion.Requests.ShouldHaveSingleItem(); }
        if (effect == "conversation-writer") { writer.PrepareCount.ShouldBe(1); }
    }

    private static JsonElement TargetPayload(string operation) => operation switch
    {
        nameof(RecordGovernedNote) => WireCommand(NoteId),
        nameof(CaptureMailboxMessageIntake) => JsonSerializer.SerializeToElement(MailboxIntake()),
        nameof(ResolveMailboxMessageParticipants) => WireParticipantResolutionCommand(),
        nameof(ScoreMailboxMessageAssociation) => WireAssociationScoringCommand(),
        nameof(AssociateEmailToProject) or nameof(RejectEmailProjectAssociation) or nameof(DeferEmailProjectAssociation) or nameof(MarkEmailAssociationNeedsReview) => WireAssociationDecisionCommand(),
        nameof(CorrectEmailProjectAssociation) => WireAssociationCorrectionCommand(),
        nameof(RequestFailedWorkflowRetry) => WireWorkflowRetryCommand(),
        nameof(ExecuteLowRiskAIAssistance) => WireLowRiskExecutionCommand(),
        nameof(ExecuteApprovedAIAction) => WireApprovedAiExecutionCommand(),
        nameof(MarkAiActionProposalInvalidatedByCorrection) => WireProposalInvalidationCommand(),
        nameof(SubmitNotificationRoutingChange) => JsonSerializer.SerializeToElement(NotificationRoutingChange()),
        nameof(CreateOutboundDraft) => JsonSerializer.SerializeToElement(OutboundDraft("draft-target")),
        nameof(RequestOutboundSendApproval) => JsonSerializer.SerializeToElement(OutboundApprovalRequest("approval-target")),
        nameof(DecideOutboundApproval) => JsonSerializer.SerializeToElement(OutboundApprovalDecision("decision-target")),
        nameof(ExecuteApprovedOutboundDraft) => JsonSerializer.SerializeToElement(OutboundSend("send-target")),
        _ => GenericTargetPayload(),
    };

    private static JsonElement GenericTargetPayload() => JsonSerializer.SerializeToElement(new
    {
        ProjectId = "project-001", PolicyId = "policy-target", PolicyVersion = "v1", PolicyChangeId = "policy-change-target",
        DisableChangeId = "disable-target", QuarantineChangeId = "quarantine-target", MailboxSourceRef = "mailbox-target",
        ServiceClientRef = "client-target", AiActorRef = "ai-target", CommandCapabilityRef = "command-target", OutboundChannelRef = "channel-target",
        RequesterRef = "requester", ApproverRef = "approver", RequesterId = "requester", MessageId = "message-target", TextFingerprint = "safe-fingerprint", TextLength = 1,
        CorrelationId, TaskIntentId = "task-intent", SourceMessageId = "source-message", ProposalId = "proposal", ApprovalId = "approval",
        DecisionId = "decision", ExpectedApprovalSourceVersion = 1, RationaleRedactionState = "metadata_only", SchemaVersion = "v1",
        ConversationId = "conversation-target", ResponseId = "response", GenerationId = "generation", CancellationId = "cancellation",
        ChangeSet = new TenantPolicyChangeSet([new TenantPolicyValue(TenantPolicyKnobIds.MailboxRoutingRules, StringListValue: [])]),
    });
}
