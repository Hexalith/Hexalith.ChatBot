using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Adapters.AiProvider;
using Hexalith.ChatBot.Server.Adapters.Conversations;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Tests.Observability;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;

using Shouldly;

using ContractAssociationReasonCode = Hexalith.ChatBot.Contracts.Enums.AssociationReasonCode;
using ContractAssociationScoringOutcome = Hexalith.ChatBot.Contracts.Enums.AssociationScoringOutcome;
using ContractAssociationThresholdBand = Hexalith.ChatBot.Contracts.Enums.AssociationThresholdBand;
using ContractAssociationScoringResult = Hexalith.ChatBot.Contracts.Commands.AssociationScoringResult;
using ContractAssociationThresholdPolicySnapshot = Hexalith.ChatBot.Contracts.Commands.AssociationThresholdPolicySnapshot;
using ContractAiActionRiskClass = Hexalith.ChatBot.Contracts.Enums.AiActionRiskClass;
using ContractAiActionRiskInputTuple = Hexalith.ChatBot.Contracts.Queries.AiActionRiskInputTuple;
using OutboundChannelRateLimitWindow = Hexalith.ChatBot.Contracts.Enums.OutboundChannelRateLimitWindow;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class AcceptedCommandDispatcherTests
{
    private const string CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
    private const string Tenant = "tenant-alpha";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TaskId = "01ARZ3NDEKTSV4RRFFQ69G5FAX";

    [Fact]
    public async Task DispatchShouldSubmitGovernedNoteWithTenantDomainAggregateAndProvenance()
    {
        RecordingEventStoreGatewayClient gateway = new();
        FixedClock clock = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), clock);

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(WireCommand(NoteId)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.MessageId.ShouldBe(CommandId);
        request.Tenant.ShouldBe(Tenant);
        request.Domain.ShouldBe("chatbot");
        request.AggregateId.ShouldBe(NoteId);
        request.CommandType.ShouldBe(nameof(RecordGovernedNote));
        request.CorrelationId.ShouldBe(CorrelationId);
        request.Extensions.ShouldNotBeNull();
        request.Extensions!["taskId"].ShouldBe(TaskId);

        // Accepted timestamp comes from the clock; resource id is the aggregate id for downstream audit/status.
        result.AcceptedAt.ShouldBe(FixedClock.FixedUtcNow);
        result.AcceptedAt.Offset.ShouldBe(TimeSpan.Zero);
        result.ResourceId.ShouldBe(NoteId);
    }

    [Fact]
    public async Task DispatchShouldRecordCommandExecutionLatencyForTheBoundTenant()
    {
        RecordingChatBotMetrics metrics = new();
        AcceptedCommandDispatcher dispatcher = new(
            new RecordingEventStoreGatewayClient(),
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            metrics: metrics);

        _ = await dispatcher.DispatchAsync(Context(WireCommand(NoteId)), TestContext.Current.CancellationToken);

        (string operationClass, string tenantId, double milliseconds) = metrics.Latencies.ShouldHaveSingleItem();
        operationClass.ShouldBe(ChatBotOperationClasses.CommandExecution);
        tenantId.ShouldBe(Tenant);
        milliseconds.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task DispatchShouldRecordCommandExecutionLatencyEvenWhenTheDispatchThrows()
    {
        // AC1/AC5: latency is recorded on every completion path via `finally`, so a throwing dispatch still records
        // command-execution latency for the bound tenant while the exception propagates unchanged — emission never
        // alters the operation's control flow or result.
        RecordingChatBotMetrics metrics = new();
        AcceptedCommandDispatcher dispatcher = new(
            new ThrowingEventStoreGatewayClient(),
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            metrics: metrics);

        await Should.ThrowAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(Context(WireCommand(NoteId)), TestContext.Current.CancellationToken).AsTask());

        (string operationClass, string tenantId, _) = metrics.Latencies.ShouldHaveSingleItem();
        operationClass.ShouldBe(ChatBotOperationClasses.CommandExecution);
        tenantId.ShouldBe(Tenant);
    }

    [Fact]
    public async Task DispatchShouldRecordIngestionLatencyForAMailboxIntakeCommand()
    {
        RecordingChatBotMetrics metrics = new();
        AcceptedCommandDispatcher dispatcher = new(
            new RecordingEventStoreGatewayClient(),
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            metrics: metrics);

        JsonElement intake = JsonSerializer.SerializeToElement(MailboxIntake(), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        _ = await dispatcher.DispatchAsync(
            Context(intake, commandType: nameof(Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake)),
            TestContext.Current.CancellationToken);

        // A mailbox intake dispatch is the in-bounds ingestion completion point (the .Workers lane cannot see the
        // .Server seam), so it is tagged operation-class `message-intake`, not `command-execution`.
        (string operationClass, string tenantId, _) = metrics.Latencies.ShouldHaveSingleItem();
        operationClass.ShouldBe(ChatBotOperationClasses.MessageIntake);
        tenantId.ShouldBe(Tenant);
    }

    private static Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake MailboxIntake()
        => new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
            new Hexalith.ChatBot.Contracts.Commands.MailboxMessageSourceIdentity(
                "graph-message-001",
                "<message-001@example.test>",
                "graph-conversation-001",
                "graph-thread-001",
                "controlled-mailbox-001",
                new Hexalith.ChatBot.Contracts.Commands.MailboxParticipantIdentity("sender@example.test", "Sender"),
                new DateTimeOffset(2026, 5, 30, 10, 15, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 5, 30, 10, 10, 0, TimeSpan.Zero),
                null,
                "UTC",
                "graph-message-v1",
                1),
            [new Hexalith.ChatBot.Contracts.Commands.MailboxRecipientIdentity("project@example.test", "Project", "to")],
            [new Hexalith.ChatBot.Contracts.Commands.MailboxAttachmentReference("attachment-001", "evidence.pdf", "application/pdf", 1024)],
            null);

    [Fact]
    public async Task DispatchShouldForwardPascalCasePayloadThatTheAggregateEngineCanDeserialize()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        _ = await dispatcher.DispatchAsync(Context(WireCommand(NoteId)), TestContext.Current.CancellationToken);

        JsonElement payload = gateway.Submitted.ShouldHaveSingleItem().Payload;

        // The EventStoreAggregate base deserializes payloads with default (case-sensitive, PascalCase) options.
        // The forwarded payload must therefore be PascalCase: 'NoteId' present, camelCase 'noteId' absent.
        payload.TryGetProperty("NoteId", out JsonElement noteId).ShouldBeTrue();
        noteId.GetString().ShouldBe(NoteId);
        payload.TryGetProperty("noteId", out _).ShouldBeFalse();

        // Round-trips through the exact mechanism the aggregate engine uses (default options, case-sensitive).
        RecordGovernedNote? roundTripped = JsonSerializer.Deserialize<RecordGovernedNote>(payload.GetRawText());
        roundTripped.ShouldNotBeNull();
        roundTripped.NoteId.ShouldBe(NoteId);
    }

    [Fact]
    public async Task DispatchWithoutTaskIdShouldStillCarryDecisionProvenanceExtensions()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        _ = await dispatcher.DispatchAsync(Context(WireCommand(NoteId), taskId: null), TestContext.Current.CancellationToken);

        Dictionary<string, string> extensions = gateway.Submitted.ShouldHaveSingleItem().Extensions.ShouldNotBeNull();
        extensions.ShouldNotContainKey("taskId");
        extensions["surfaceOrigin"].ShouldBe("ui");
        extensions["decidedAt"].ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DispatchShouldResolveMailboxParticipantsBeforeSubmittingToEventStore()
    {
        RecordingEventStoreGatewayClient gateway = new();
        RecordingParticipantResolutionOrchestrator orchestrator = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, orchestrator, new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireParticipantResolutionCommand(),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ResolveMailboxMessageParticipants)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.MessageId.ShouldBe(CommandId);
        request.Tenant.ShouldBe(Tenant);
        request.Domain.ShouldBe("chatbot");
        request.AggregateId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ResolveMailboxMessageParticipants));
        result.ResourceId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");

        orchestrator.ResolveCount.ShouldBe(1);
        orchestrator.TenantId.ShouldBe(Tenant);

        JsonElement payload = request.Payload;
        payload.TryGetProperty("ResolvedParticipants", out JsonElement resolved).ShouldBeTrue();
        resolved.GetArrayLength().ShouldBe(1);
        resolved[0].GetProperty("PartyId").GetString().ShouldBe("tenant-alpha:parties:party-001");
        payload.TryGetProperty("UnresolvedParticipants", out JsonElement unresolved).ShouldBeTrue();
        unresolved.GetArrayLength().ShouldBe(1);
        unresolved[0].GetProperty("Reason").GetString().ShouldBe("NotFound");
        payload.TryGetProperty("resolvedParticipants", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchShouldRejectMalformedParticipantResolutionBeforeEventStoreSubmit()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(
                Context(
                    MalformedParticipantResolutionCommand(),
                    commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ResolveMailboxMessageParticipants)),
                TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldBe("The participant-resolution command is missing its source identity.");
        gateway.Submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchShouldScoreAssociationBeforeSubmittingToEventStore()
    {
        RecordingEventStoreGatewayClient gateway = new();
        RecordingAssociationScoringOrchestrator orchestrator = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), orchestrator, new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireAssociationScoringCommand(),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ScoreMailboxMessageAssociation)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAB");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ScoreMailboxMessageAssociation));
        result.ResourceId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAB");
        orchestrator.ScoreCount.ShouldBe(1);
        orchestrator.TenantId.ShouldBe(Tenant);
        request.Payload.TryGetProperty("Result", out JsonElement resultPayload).ShouldBeTrue();
        resultPayload.GetProperty("Outcome").GetString().ShouldBe("candidates-generated");
        request.Payload.TryGetProperty("result", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchShouldRouteAssociationDecisionToAssociationAggregateWithPascalCaseMetadataOnlyPayload()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireAssociationDecisionCommand(),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.MessageId.ShouldBe(CommandId);
        request.Tenant.ShouldBe(Tenant);
        request.Domain.ShouldBe("chatbot");
        request.AggregateId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject));
        request.CorrelationId.ShouldBe(CorrelationId);
        request.Extensions.ShouldNotBeNull();
        request.Extensions!["surfaceOrigin"].ShouldBe("ui");
        request.Extensions["decidedAt"].ShouldBe(FixedClock.FixedUtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        result.ResourceId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");

        JsonElement payload = request.Payload;
        payload.TryGetProperty("AssociationId", out JsonElement associationId).ShouldBeTrue();
        associationId.GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        payload.TryGetProperty("DecisionNote", out JsonElement decisionNote).ShouldBeTrue();
        decisionNote.GetString().ShouldBe("Reviewed safe metadata.");
        payload.TryGetProperty("associationId", out _).ShouldBeFalse();
        payload.GetRawText().ShouldNotContain("sender@example.test", Case.Insensitive);
        payload.GetRawText().ShouldNotContain("raw-body", Case.Insensitive);
    }

    [Fact]
    public async Task DispatchShouldRouteAssociationCorrectionToAssociationAggregateWithPascalCaseMetadataOnlyPayload()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireAssociationCorrectionCommand(),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation));
        request.Extensions.ShouldNotBeNull();
        request.Extensions!["surfaceOrigin"].ShouldBe("ui");
        result.ResourceId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");

        JsonElement payload = request.Payload;
        payload.TryGetProperty("AssociationId", out JsonElement associationId).ShouldBeTrue();
        associationId.GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        payload.TryGetProperty("PriorProjectId", out JsonElement priorProjectId).ShouldBeTrue();
        priorProjectId.GetString().ShouldBe("project-001");
        payload.TryGetProperty("TargetProjectId", out JsonElement targetProjectId).ShouldBeTrue();
        targetProjectId.GetString().ShouldBe("project-002");
        payload.TryGetProperty("CorrectionRationale", out JsonElement rationale).ShouldBeTrue();
        rationale.GetString().ShouldBe("Wrong project selected from safe metadata.");
        payload.TryGetProperty("associationId", out _).ShouldBeFalse();
        payload.GetRawText().ShouldNotContain("sender@example.test", Case.Insensitive);
        payload.GetRawText().ShouldNotContain("raw-body", Case.Insensitive);
    }

    [Fact]
    public async Task DispatchShouldRouteWorkflowRetryWithPascalCaseMetadataOnlyPayload()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireWorkflowRetryCommand(),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.RequestFailedWorkflowRetry)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAZ");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.RequestFailedWorkflowRetry));
        result.ResourceId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAZ");

        JsonElement payload = request.Payload;
        payload.TryGetProperty("RetryId", out JsonElement retryId).ShouldBeTrue();
        retryId.GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAZ");
        payload.GetProperty("FailedEventId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        payload.GetProperty("FailedOperationClass").GetString().ShouldBe("message-intake");
        payload.GetProperty("FailureReasonCode").GetString().ShouldBe("graph_throttled");
        payload.TryGetProperty("retryId", out _).ShouldBeFalse();
        payload.GetRawText().ShouldNotContain("raw exception", Case.Insensitive);
    }

    [Fact]
    public async Task DispatchShouldInvokeLowRiskProviderOnceAndSubmitMetadataOnlyExecutionPayload()
    {
        RecordingEventStoreGatewayClient gateway = new();
        RecordingAiAssistanceProvider provider = new();
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            provider);
        ChatBotGatewayContext context = Context(
            WireLowRiskExecutionCommand(),
            commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteLowRiskAIAssistance));
        context.SetRiskClassification(ChatBotRiskClassification.Classified(LowRiskClassification()));
        context.SetApprovalResult(ChatBotApprovalResult.AllowedLowRiskExecution("policy-snap-001", "low-risk-execute-allowed"));

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(context, TestContext.Current.CancellationToken);

        provider.ExecuteCount.ShouldBe(1);
        provider.LastRequest.ShouldNotBeNull();
        provider.LastRequest.TenantId.ShouldBe(Tenant);
        provider.LastRequest.AuthorizedContextReferences.ShouldBe(["evidence-001"]);
        provider.LastRequest.ExcludedContextReasons.ShouldBe(["redacted"]);
        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("graph-message-001");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteLowRiskAIAssistance));
        result.ResourceId.ShouldBe("graph-message-001");

        JsonElement payload = request.Payload;
        payload.TryGetProperty("ExecutionRecord", out JsonElement record).ShouldBeTrue();
        record.GetProperty("Outcome").GetString().ShouldBe("success");
        record.GetProperty("ProviderName").GetString().ShouldBe("deterministic-test");
        payload.GetRawText().ShouldNotContain("prompt", Case.Insensitive);
        payload.GetRawText().ShouldNotContain("completion", Case.Insensitive);
        payload.GetRawText().ShouldNotContain("/home/administrator", Case.Insensitive);
    }

    [Fact]
    public async Task DispatchShouldRoutePolicyFalseLowRiskAssistanceWithoutProviderCall()
    {
        RecordingEventStoreGatewayClient gateway = new();
        RecordingAiAssistanceProvider provider = new();
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            provider);
        ChatBotGatewayContext context = Context(
            WireLowRiskExecutionCommand(),
            commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteLowRiskAIAssistance));
        context.SetRiskClassification(ChatBotRiskClassification.Classified(LowRiskClassification()));
        context.SetApprovalResult(ChatBotApprovalResult.RoutedToApproval("policy-snap-001", "low_risk_policy_false"));

        _ = await dispatcher.DispatchAsync(context, TestContext.Current.CancellationToken);

        provider.ExecuteCount.ShouldBe(0);
        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        JsonElement record = request.Payload.GetProperty("ExecutionRecord");
        record.GetProperty("Outcome").GetString().ShouldBe("pending-approval");
        record.GetProperty("ProviderName").GetString().ShouldBe("not-invoked");
        record.GetProperty("PolicyReasonCode").GetString().ShouldBe("low_risk_policy_false");
        record.GetProperty("SafeNextAction").GetString().ShouldBe("review-ai-action");
    }

    [Fact]
    public async Task DispatchShouldPrepareApprovedAiActionAppendMetadataThenSubmitEventStorePayload()
    {
        RecordingEventStoreGatewayClient gateway = new();
        RecordingConversationWriter conversationWriter = new();
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            conversationWriter: conversationWriter);

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireApprovedAiExecutionCommand(),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedAIAction)),
            TestContext.Current.CancellationToken);

        conversationWriter.PrepareCount.ShouldBe(1);
        conversationWriter.LastRequest.ShouldNotBeNull();
        conversationWriter.LastRequest.TenantId.ShouldBe(Tenant);
        conversationWriter.LastRequest.CommandName.ShouldBe("Project.AppendConversationMessage");

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("graph-message-001");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedAIAction));
        result.ResourceId.ShouldBe("graph-message-001");

        JsonElement payload = request.Payload;
        payload.TryGetProperty("ExecutionRecord", out JsonElement record).ShouldBeTrue();
        record.GetProperty("Outcome").GetString().ShouldBe("success");
        payload.GetProperty("CommandName").GetString().ShouldBe("Project.AppendConversationMessage");
        payload.TryGetProperty("commandName", out _).ShouldBeFalse();
        payload.GetRawText().ShouldNotContain("raw-body", Case.Insensitive);
        payload.GetRawText().ShouldNotContain("provider payload", Case.Insensitive);
    }

    [Fact]
    public async Task DispatchShouldRejectApprovedAiActionWhenCommandIsNotOnAiActionAllowlistBeforeAppendPreparation()
    {
        RecordingEventStoreGatewayClient gateway = new();
        RecordingConversationWriter conversationWriter = new();
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            conversationWriter: conversationWriter);

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(
                Context(
                    WireApprovedAiExecutionCommand("Project.SendEmail"),
                    commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedAIAction)),
                TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldBe("The approved AI action execution command is missing trusted allowlist metadata.");
        conversationWriter.PrepareCount.ShouldBe(0);
        gateway.Submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchShouldRouteAiProposalInvalidationToSourceMessageAggregate()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireProposalInvalidationCommand(),
                commandType: nameof(MarkAiActionProposalInvalidatedByCorrection)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("graph-message-001");
        request.CommandType.ShouldBe(nameof(MarkAiActionProposalInvalidatedByCorrection));
        result.ResourceId.ShouldBe("graph-message-001");
        request.Payload.TryGetProperty("ProposalId", out JsonElement proposalId).ShouldBeTrue();
        proposalId.GetString().ShouldBe("ai-proposal-001");
        request.Payload.TryGetProperty("proposalId", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchShouldRouteNotificationRoutingChangeToRoutingChangeAggregate()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                JsonSerializer.SerializeToElement(NotificationRoutingChange(), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                commandType: nameof(SubmitNotificationRoutingChange)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("routing-change-001");
        request.CommandType.ShouldBe(nameof(SubmitNotificationRoutingChange));
        result.ResourceId.ShouldBe("routing-change-001");

        request.Payload.TryGetProperty("RoutingChangeId", out JsonElement routingChangeId).ShouldBeTrue();
        routingChangeId.GetString().ShouldBe("routing-change-001");
        request.Payload.TryGetProperty("routingChangeId", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchShouldRejectNotificationRoutingChangeWithInvalidMap()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        SubmitNotificationRoutingChange invalid = NotificationRoutingChange() with
        {
            ChangeSet = new NotificationRoutingChangeSet([]),
        };

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(
                Context(
                    JsonSerializer.SerializeToElement(invalid, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    commandType: nameof(SubmitNotificationRoutingChange)),
                TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldBe("The notification-routing change command is missing valid routing metadata.");
        gateway.Submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchShouldRouteMailboxSourceQuarantineApprovalToQuarantineChangeAggregateForDistinctApprover()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireApproveMailboxSourceQuarantineCommand("admin-requester", "admin-approver"),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveMailboxSourceQuarantine)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("mailbox-quarantine-001");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ApproveMailboxSourceQuarantine));
        result.ResourceId.ShouldBe("mailbox-quarantine-001");

        // The forwarded payload is PascalCase so the aggregate engine can deserialize it (matches the disable/policy flow).
        request.Payload.TryGetProperty("QuarantineChangeId", out JsonElement changeId).ShouldBeTrue();
        changeId.GetString().ShouldBe("mailbox-quarantine-001");
        request.Payload.TryGetProperty("quarantineChangeId", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchShouldRejectMailboxSourceQuarantineApprovalWhenApproverEqualsRequester()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        // Third enforcement layer (dispatcher) of the FR75d two-person rule: a single actor cannot both request
        // and approve the quarantine. This guards even if the gateway-validation and aggregate checks were bypassed,
        // mirroring the disable/tenant-policy distinct-approver dispatcher guard. Nothing is submitted to the spine.
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(
                Context(
                    WireApproveMailboxSourceQuarantineCommand("admin-requester", "admin-requester"),
                    commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveMailboxSourceQuarantine)),
                TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldBe("The mailbox-source quarantine approval command is missing valid approval metadata.");
        gateway.Submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchShouldRouteServiceClientDisableApprovalToDisableChangeAggregateForDistinctApprover()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireApproveServiceClientDisableCommand("admin-requester", "admin-approver"),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveServiceClientDisable)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("service-client-disable-001");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ApproveServiceClientDisable));
        result.ResourceId.ShouldBe("service-client-disable-001");

        // The forwarded payload is PascalCase so the aggregate engine can deserialize it (matches the disable/policy flow).
        request.Payload.TryGetProperty("DisableChangeId", out JsonElement changeId).ShouldBeTrue();
        changeId.GetString().ShouldBe("service-client-disable-001");
        request.Payload.TryGetProperty("disableChangeId", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchShouldRejectServiceClientDisableApprovalWhenApproverEqualsRequester()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        // Third enforcement layer (dispatcher) of the FR75d two-person rule for the service-client disable: a single
        // actor cannot both request and approve. This guards even if the gateway-validation and aggregate checks were
        // bypassed, mirroring the mailbox-source disable/quarantine distinct-approver dispatcher guard. Nothing is
        // submitted to the spine.
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(
                Context(
                    WireApproveServiceClientDisableCommand("admin-requester", "admin-requester"),
                    commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveServiceClientDisable)),
                TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldBe("The service-client disable approval command is missing valid approval metadata.");
        gateway.Submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchShouldRouteAiActorDisableApprovalToDisableChangeAggregateForDistinctApprover()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireApproveAiActorDisableCommand("admin-requester", "admin-approver"),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveAiActorDisable)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("ai-actor-disable-001");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ApproveAiActorDisable));
        result.ResourceId.ShouldBe("ai-actor-disable-001");

        // The forwarded payload is PascalCase so the aggregate engine can deserialize it (matches the disable/policy flow).
        request.Payload.TryGetProperty("DisableChangeId", out JsonElement changeId).ShouldBeTrue();
        changeId.GetString().ShouldBe("ai-actor-disable-001");
        request.Payload.TryGetProperty("disableChangeId", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchShouldRejectAiActorDisableApprovalWhenApproverEqualsRequester()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        // Third enforcement layer (dispatcher) of the FR75d two-person rule for the AI-actor disable: a single actor
        // cannot both request and approve. This guards even if the gateway-validation and aggregate checks were
        // bypassed, mirroring the service-client disable distinct-approver dispatcher guard. Nothing is submitted to
        // the spine.
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(
                Context(
                    WireApproveAiActorDisableCommand("admin-requester", "admin-requester"),
                    commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveAiActorDisable)),
                TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldBe("The AI-actor disable approval command is missing valid approval metadata.");
        gateway.Submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchShouldRouteCommandCapabilityDisableApprovalToDisableChangeAggregateForDistinctApprover()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireApproveCommandCapabilityDisableCommand("admin-requester", "admin-approver"),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveCommandCapabilityDisable)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("command-capability-disable-001");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ApproveCommandCapabilityDisable));
        result.ResourceId.ShouldBe("command-capability-disable-001");

        // The forwarded payload is PascalCase so the aggregate engine can deserialize it (matches the disable/policy flow).
        request.Payload.TryGetProperty("DisableChangeId", out JsonElement changeId).ShouldBeTrue();
        changeId.GetString().ShouldBe("command-capability-disable-001");
        request.Payload.TryGetProperty("disableChangeId", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchShouldRejectCommandCapabilityDisableApprovalWhenApproverEqualsRequester()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        // Third enforcement layer (dispatcher) of the FR75d two-person rule for the command-capability disable: a
        // single actor cannot both request and approve. This guards even if the gateway-validation and aggregate
        // checks were bypassed, mirroring the AI-actor/service-client disable distinct-approver dispatcher guard.
        // Nothing is submitted to the spine.
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(
                Context(
                    WireApproveCommandCapabilityDisableCommand("admin-requester", "admin-requester"),
                    commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveCommandCapabilityDisable)),
                TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldBe("The command-capability disable approval command is missing valid approval metadata.");
        gateway.Submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchShouldRouteOutboundChannelDisableApprovalToDisableChangeAggregateForDistinctApprover()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireApproveOutboundChannelDisableCommand("admin-requester", "admin-approver"),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveOutboundChannelDisable)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("outbound-channel-disable-001");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ApproveOutboundChannelDisable));
        result.ResourceId.ShouldBe("outbound-channel-disable-001");

        // The forwarded payload is PascalCase so the aggregate engine can deserialize it (matches the disable/policy flow).
        request.Payload.TryGetProperty("DisableChangeId", out JsonElement changeId).ShouldBeTrue();
        changeId.GetString().ShouldBe("outbound-channel-disable-001");
        request.Payload.TryGetProperty("disableChangeId", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchShouldRejectOutboundChannelDisableApprovalWhenApproverEqualsRequester()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        // Third enforcement layer (dispatcher) of the FR75d two-person rule for the outbound-channel disable: a single
        // actor cannot both request and approve. Mirrors the command-capability disable distinct-approver dispatcher
        // guard. Nothing is submitted to the spine.
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(
                Context(
                    WireApproveOutboundChannelDisableCommand("admin-requester", "admin-requester"),
                    commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveOutboundChannelDisable)),
                TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldBe("The outbound-channel disable approval command is missing valid approval metadata.");
        gateway.Submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchShouldFailClosedAtSendSeamBeforeAdapterWhenOutboundChannelDisabled()
    {
        // The key Story 7.24 divergence: a Disabled outbound channel makes ExecuteApprovedOutboundDraft fail closed at
        // the send seam BEFORE IOutboundMailboxSender.SendAsync — the adapter is never invoked, no external message
        // leaves the boundary, and the send is marked "blocked" so the aggregate records a rejected-send outcome.
        RecordingEventStoreGatewayClient gateway = new();
        SpyOutboundMailboxSender sender = new();
        FakeOutboundChannelControlStateProvider provider = new();
        provider.Disable(Tenant, "adapter:mailbox-outbound");
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: sender,
            outboundChannelControlStateProvider: provider);

        _ = await dispatcher.DispatchAsync(
            OutboundSendContext(OutboundSend("send-001")),
            TestContext.Current.CancellationToken);

        // The adapter spy was never invoked — enforcement is provably local to the send path.
        sender.SendCount.ShouldBe(0);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedOutboundDraft));
        // The dispatched payload carries a non-"sent" adapter status so the aggregate's AdapterStatus != "sent" path
        // records the fail-closed rejected-send outcome (mapped to outbound_channel_disabled, distinct from
        // outbound_adapter_unavailable).
        request.Payload.TryGetProperty("AdapterStatus", out JsonElement status).ShouldBeTrue();
        status.GetString().ShouldBe("blocked");

        // The provider only ever receives the safe tenant id and channel ref — never any credential/recipient/PII.
        provider.ObservedRequests.ShouldContain((Tenant, "adapter:mailbox-outbound"));
    }

    [Fact]
    public async Task DispatchShouldSendNormallyWhenChannelActiveOrUnderADifferentTenant()
    {
        // Isolation: a sibling Active channel for the same tenant, and the SAME channel under a DIFFERENT tenant, are
        // both unaffected — the adapter IS invoked and the send dispatches normally.
        FakeOutboundChannelControlStateProvider provider = new();
        provider.Disable("tenant-other", "adapter:mailbox-outbound");

        // Same tenant, channel still Active → sent normally.
        RecordingEventStoreGatewayClient activeGateway = new();
        SpyOutboundMailboxSender activeSender = new();
        AcceptedCommandDispatcher activeDispatcher = new(
            activeGateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: activeSender,
            outboundChannelControlStateProvider: provider);
        _ = await activeDispatcher.DispatchAsync(
            OutboundSendContext(OutboundSend("send-001")),
            TestContext.Current.CancellationToken);
        activeSender.SendCount.ShouldBe(1);
        activeGateway.Submitted.ShouldHaveSingleItem()
            .Payload.TryGetProperty("AdapterStatus", out JsonElement activeStatus).ShouldBeTrue();
        activeStatus.GetString().ShouldBe("sent");

        // The disabled tenant ("tenant-other") does not affect this tenant ("tenant-alpha") for the same channel ref.
        provider.ObservedRequests.ShouldContain((Tenant, "adapter:mailbox-outbound"));
    }

    [Fact]
    public async Task DispatchUnderATestTenantRoutesToTheTestModeAdapterRecordsTheMarkerAndNeverSendsExternally()
    {
        // Story 9.4 (AC1) E2E send isolation: a replay run (submission with ReplayRunId under a TEST tenant) drives the
        // SAME ExecuteApprovedOutboundDraft through the dispatcher. The tenant-aware selector resolves the test-mode
        // adapter, which records the would-have-sent envelope (carrying the run id) to the test tenant's trace store and
        // returns "sent" so the aggregate's AdapterStatus == "sent" path runs identically to production — but the
        // production sender is NEVER invoked and no external message leaves the boundary.
        const string testTenant = "replay-test:tenant-alpha";
        const string replayRunId = "replay-run-001";
        Hexalith.ChatBot.Server.Adapters.Mailbox.InMemoryOutboundTraceStore traceStore = new();
        SpyOutboundMailboxSender productionSender = new();
        Hexalith.ChatBot.Server.Adapters.Mailbox.ReplayAwareOutboundMailboxSender selector = new(
            productionSender,
            new Hexalith.ChatBot.Server.Adapters.Mailbox.TestModeOutboundMailboxSender(traceStore, new FixedClock()));
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: selector);

        _ = await dispatcher.DispatchAsync(
            OutboundSendContextWithReplay(OutboundSend("send-001"), testTenant, replayRunId),
            TestContext.Current.CancellationToken);

        // No external send — the production sender was never reached for a test tenant.
        productionSender.SendCount.ShouldBe(0);

        // The would-have-sent envelope was recorded to the TEST tenant's partition, carrying the replay marker.
        Hexalith.ChatBot.Server.Adapters.Mailbox.OutboundTraceRecord record =
            traceStore.EnumerateForTenant(testTenant).ShouldHaveSingleItem();
        record.SendId.ShouldBe("send-001");
        record.ReplayRunId.ShouldBe(replayRunId);

        // No production tenant's trace store grew.
        traceStore.EnumerateForTenant(Tenant).ShouldBeEmpty();

        // The aggregate sees the identical-to-production "sent" status (via the test-mode adapter ref).
        gateway.Submitted.ShouldHaveSingleItem()
            .Payload.TryGetProperty("AdapterStatus", out JsonElement status).ShouldBeTrue();
        status.GetString().ShouldBe("sent");
    }

    [Fact]
    public async Task DispatchUnderAProductionTenantIsByteForByteUnchangedAndWritesNoTrace()
    {
        // Story 9.4 (AC1): for every PRODUCTION tenant the existing IOutboundMailboxSender resolution is unchanged — the
        // selector routes to the production sender, the production send path runs as before, and NO trace record is
        // written. Production tenants are never reachable to the test-mode adapter.
        Hexalith.ChatBot.Server.Adapters.Mailbox.InMemoryOutboundTraceStore traceStore = new();
        SpyOutboundMailboxSender productionSender = new();
        Hexalith.ChatBot.Server.Adapters.Mailbox.ReplayAwareOutboundMailboxSender selector = new(
            productionSender,
            new Hexalith.ChatBot.Server.Adapters.Mailbox.TestModeOutboundMailboxSender(traceStore, new FixedClock()));
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: selector);

        _ = await dispatcher.DispatchAsync(
            OutboundSendContext(OutboundSend("send-001")),
            TestContext.Current.CancellationToken);

        // The production sender ran exactly once; the test-mode trace store stayed empty.
        productionSender.SendCount.ShouldBe(1);
        traceStore.EnumerateTenants().ShouldBeEmpty();

        gateway.Submitted.ShouldHaveSingleItem()
            .Payload.TryGetProperty("AdapterStatus", out JsonElement status).ShouldBeTrue();
        status.GetString().ShouldBe("sent");
    }

    [Fact]
    public async Task DispatchShouldLeaveOutboundDraftCreationInspectableWhenChannelDisabledAndNeverConsultTheChannelControl()
    {
        // AC5/AC13: disabling an outbound channel blocks ONLY the send/execute step. CreateOutboundDraft (and the
        // RequestOutboundSendApproval / DecideOutboundApproval steps that share this seam) must stay inspectable — the
        // disabled-channel check is wired ONLY into the ExecuteApprovedOutboundDraft branch, so a draft for a Disabled
        // channel dispatches normally and the channel-control provider is never even consulted (block is local to send).
        RecordingEventStoreGatewayClient gateway = new();
        FakeOutboundChannelControlStateProvider provider = new();
        provider.Disable(Tenant, "adapter:mailbox-outbound");
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: new SpyOutboundMailboxSender(),
            outboundChannelControlStateProvider: provider);

        _ = await dispatcher.DispatchAsync(
            OutboundDraftContext(OutboundDraft("draft-001")),
            TestContext.Current.CancellationToken);

        // The draft was accepted and dispatched normally — pending drafts remain inspectable even though the channel
        // is Disabled for this tenant.
        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.CreateOutboundDraft));

        // The channel-disable control is local to the send step — it is never consulted for the draft step.
        provider.ObservedRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchShouldLeaveOutboundApprovalRequestAndDecisionInspectableWhenChannelDisabled()
    {
        // Story 7.24 AC5 names all THREE pre-send steps that must remain inspectable for a Disabled channel:
        // CreateOutboundDraft, RequestOutboundSendApproval, and DecideOutboundApproval. The draft step is covered above;
        // these two approval steps must dispatch normally, never call the outbound adapter, and never consult the
        // disabled-channel provider because the fail-closed gate lives only in ExecuteApprovedOutboundDraft.
        JsonSerializerOptions webOptions = new(JsonSerializerDefaults.Web);
        foreach ((string CommandType, JsonElement Payload) step in new[]
                 {
                     (nameof(Hexalith.ChatBot.Contracts.Commands.RequestOutboundSendApproval),
                         JsonSerializer.SerializeToElement(OutboundApprovalRequest("approval-001"), webOptions)),
                     (nameof(Hexalith.ChatBot.Contracts.Commands.DecideOutboundApproval),
                         JsonSerializer.SerializeToElement(OutboundApprovalDecision("decision-001"), webOptions)),
                 })
        {
            RecordingEventStoreGatewayClient gateway = new();
            SpyOutboundMailboxSender sender = new();
            FakeOutboundChannelControlStateProvider provider = new();
            provider.Disable(Tenant, "adapter:mailbox-outbound");
            AcceptedCommandDispatcher dispatcher = new(
                gateway,
                new NoOpParticipantResolutionOrchestrator(),
                new NoOpAssociationScoringOrchestrator(),
                new FixedClock(),
                outboundMailboxSender: sender,
                outboundChannelControlStateProvider: provider);

            _ = await dispatcher.DispatchAsync(
                Context(step.Payload, commandType: step.CommandType),
                TestContext.Current.CancellationToken);

            gateway.Submitted.ShouldHaveSingleItem().CommandType.ShouldBe(step.CommandType);
            sender.SendCount.ShouldBe(0);
            provider.ObservedRequests.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task DispatchShouldRouteOutboundChannelQuarantineApprovalToQuarantineChangeAggregateForDistinctApprover()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireApproveOutboundChannelQuarantineCommand("admin-requester", "admin-approver"),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveOutboundChannelQuarantine)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("outbound-channel-quarantine-001");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ApproveOutboundChannelQuarantine));
        result.ResourceId.ShouldBe("outbound-channel-quarantine-001");

        // The forwarded payload is PascalCase so the aggregate engine can deserialize it (matches the disable/policy flow).
        request.Payload.TryGetProperty("QuarantineChangeId", out JsonElement changeId).ShouldBeTrue();
        changeId.GetString().ShouldBe("outbound-channel-quarantine-001");
        request.Payload.TryGetProperty("quarantineChangeId", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchShouldRejectOutboundChannelQuarantineApprovalWhenApproverEqualsRequester()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        // Third enforcement layer (dispatcher) of the FR75d two-person rule for the outbound-channel quarantine: a
        // single actor cannot both request and approve. Mirrors the disable distinct-approver dispatcher guard. Nothing
        // is submitted to the spine.
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(
                Context(
                    WireApproveOutboundChannelQuarantineCommand("admin-requester", "admin-requester"),
                    commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveOutboundChannelQuarantine)),
                TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldBe("The outbound-channel quarantine approval command is missing valid approval metadata.");
        gateway.Submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchShouldFailClosedAtSendSeamBeforeAdapterWhenOutboundChannelQuarantined()
    {
        // The key Story 7.25 divergence: a Quarantined outbound channel makes ExecuteApprovedOutboundDraft fail closed
        // at the send seam BEFORE IOutboundMailboxSender.SendAsync — the adapter is never invoked, no external message
        // leaves the boundary, and the send is marked "quarantined" so the aggregate records a rejected-send outcome.
        RecordingEventStoreGatewayClient gateway = new();
        SpyOutboundMailboxSender sender = new();
        FakeOutboundChannelControlStateProvider provider = new();
        provider.Quarantine(Tenant, "adapter:mailbox-outbound");
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: sender,
            outboundChannelControlStateProvider: provider);

        _ = await dispatcher.DispatchAsync(
            OutboundSendContext(OutboundSend("send-001")),
            TestContext.Current.CancellationToken);

        // The adapter spy was never invoked — enforcement is provably local to the send path.
        sender.SendCount.ShouldBe(0);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedOutboundDraft));
        // The dispatched payload carries the distinct non-"sent" "quarantined" adapter status so the aggregate's
        // AdapterStatus != "sent" path records the fail-closed rejected-send outcome (mapped to
        // outbound_channel_quarantined, distinct from outbound_channel_disabled / outbound_adapter_unavailable).
        request.Payload.TryGetProperty("AdapterStatus", out JsonElement status).ShouldBeTrue();
        status.GetString().ShouldBe("quarantined");

        // The provider only ever receives the safe tenant id and channel ref — never any credential/recipient/PII.
        provider.ObservedRequests.ShouldContain((Tenant, "adapter:mailbox-outbound"));
    }

    [Fact]
    public async Task DispatchShouldStillBlockDisabledChannelWithBlockedStatusAlongsideQuarantineBranch()
    {
        // Regression: both control-state branches coexist off one provider read. A Disabled channel still yields the
        // "blocked" adapter status (→ outbound_channel_disabled) even though the Quarantined branch now exists beside it.
        RecordingEventStoreGatewayClient gateway = new();
        SpyOutboundMailboxSender sender = new();
        FakeOutboundChannelControlStateProvider provider = new();
        provider.Disable(Tenant, "adapter:mailbox-outbound");
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: sender,
            outboundChannelControlStateProvider: provider);

        _ = await dispatcher.DispatchAsync(
            OutboundSendContext(OutboundSend("send-001")),
            TestContext.Current.CancellationToken);

        sender.SendCount.ShouldBe(0);
        gateway.Submitted.ShouldHaveSingleItem()
            .Payload.TryGetProperty("AdapterStatus", out JsonElement status).ShouldBeTrue();
        status.GetString().ShouldBe("blocked");
    }

    [Fact]
    public async Task DispatchShouldSendNormallyWhenChannelActiveOrUnderADifferentTenantForQuarantine()
    {
        // Isolation: a sibling Active channel for the same tenant, and the SAME channel under a DIFFERENT tenant, are
        // both unaffected by a quarantine — the adapter IS invoked and the send dispatches normally.
        FakeOutboundChannelControlStateProvider provider = new();
        provider.Quarantine("tenant-other", "adapter:mailbox-outbound");

        RecordingEventStoreGatewayClient activeGateway = new();
        SpyOutboundMailboxSender activeSender = new();
        AcceptedCommandDispatcher activeDispatcher = new(
            activeGateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: activeSender,
            outboundChannelControlStateProvider: provider);
        _ = await activeDispatcher.DispatchAsync(
            OutboundSendContext(OutboundSend("send-001")),
            TestContext.Current.CancellationToken);
        activeSender.SendCount.ShouldBe(1);
        activeGateway.Submitted.ShouldHaveSingleItem()
            .Payload.TryGetProperty("AdapterStatus", out JsonElement activeStatus).ShouldBeTrue();
        activeStatus.GetString().ShouldBe("sent");

        // The quarantined tenant ("tenant-other") does not affect this tenant ("tenant-alpha") for the same channel ref.
        provider.ObservedRequests.ShouldContain((Tenant, "adapter:mailbox-outbound"));
    }

    [Fact]
    public async Task DispatchShouldLeaveOutboundDraftCreationInspectableWhenChannelQuarantinedAndNeverConsultTheChannelControl()
    {
        // AC5/AC9: quarantining an outbound channel blocks ONLY the send/execute step. CreateOutboundDraft (and the
        // RequestOutboundSendApproval / DecideOutboundApproval steps that share this seam) must stay inspectable — the
        // channel-control check is wired ONLY into the ExecuteApprovedOutboundDraft branch, so a draft for a Quarantined
        // channel dispatches normally and the channel-control provider is never even consulted (block is local to send).
        RecordingEventStoreGatewayClient gateway = new();
        FakeOutboundChannelControlStateProvider provider = new();
        provider.Quarantine(Tenant, "adapter:mailbox-outbound");
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: new SpyOutboundMailboxSender(),
            outboundChannelControlStateProvider: provider);

        _ = await dispatcher.DispatchAsync(
            OutboundDraftContext(OutboundDraft("draft-001")),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.CreateOutboundDraft));

        // The channel-quarantine control is local to the send step — it is never consulted for the draft step.
        provider.ObservedRequests.ShouldBeEmpty();
    }

    // ----- Story 7.26: outbound-channel rate-limit send-seam enforcement -----

    [Fact]
    public async Task DispatchShouldFailClosedAtSendSeamBeforeAdapterWhenOutboundChannelAtRateLimitBudget()
    {
        // The key Story 7.26 divergence: a rate-limited outbound channel at budget makes ExecuteApprovedOutboundDraft
        // fail closed at the send seam BEFORE IOutboundMailboxSender.SendAsync — the adapter is never invoked, no
        // external message leaves the boundary, and the send is marked "rate-limited" (distinct from "blocked" /
        // "quarantined" / "sent") so the aggregate records a rejected-send outcome (→ outbound_channel_rate_limited).
        RecordingEventStoreGatewayClient gateway = new();
        SpyOutboundMailboxSender sender = new();
        FakeOutboundChannelRateLimitProvider rateLimits = new();
        rateLimits.Configure(Tenant, "adapter:mailbox-outbound", budget: 3);
        FakeOutboundChannelSendHistory history = new();
        history.Seed(
            Tenant,
            "adapter:mailbox-outbound",
            FixedClock.FixedUtcNow.AddMinutes(-1),
            FixedClock.FixedUtcNow.AddMinutes(-2),
            FixedClock.FixedUtcNow.AddMinutes(-3));
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: sender,
            outboundChannelRateLimitProvider: rateLimits,
            outboundChannelSendHistory: history);

        _ = await dispatcher.DispatchAsync(
            OutboundSendContext(OutboundSend("send-001")),
            TestContext.Current.CancellationToken);

        // The adapter spy was never invoked — enforcement is provably local to the send path.
        sender.SendCount.ShouldBe(0);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedOutboundDraft));
        request.Payload.TryGetProperty("AdapterStatus", out JsonElement status).ShouldBeTrue();
        status.GetString().ShouldBe("rate-limited");
        // The rate-limited token is DISTINCT from the disable/quarantine/sent tokens.
        status.GetString().ShouldNotBe("blocked");
        status.GetString().ShouldNotBe("quarantined");
        status.GetString().ShouldNotBe("sent");

        // The seams only ever receive the safe tenant id + channel ref — never any credential/recipient/PII.
        rateLimits.ObservedRequests.ShouldContain((Tenant, "adapter:mailbox-outbound"));
        history.ObservedRequests.ShouldContain((Tenant, "adapter:mailbox-outbound"));
    }

    [Fact]
    public async Task DispatchShouldSendNormallyWhenOutboundChannelUnderRateLimitBudget()
    {
        // Under budget (count < budget) the send proceeds normally to the adapter.
        RecordingEventStoreGatewayClient gateway = new();
        SpyOutboundMailboxSender sender = new();
        FakeOutboundChannelRateLimitProvider rateLimits = new();
        rateLimits.Configure(Tenant, "adapter:mailbox-outbound", budget: 3);
        FakeOutboundChannelSendHistory history = new();
        history.Seed(
            Tenant,
            "adapter:mailbox-outbound",
            FixedClock.FixedUtcNow.AddMinutes(-1),
            FixedClock.FixedUtcNow.AddMinutes(-2));
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: sender,
            outboundChannelRateLimitProvider: rateLimits,
            outboundChannelSendHistory: history);

        _ = await dispatcher.DispatchAsync(
            OutboundSendContext(OutboundSend("send-001")),
            TestContext.Current.CancellationToken);

        sender.SendCount.ShouldBe(1);
        gateway.Submitted.ShouldHaveSingleItem()
            .Payload.TryGetProperty("AdapterStatus", out JsonElement status).ShouldBeTrue();
        status.GetString().ShouldBe("sent");
    }

    [Fact]
    public async Task DispatchShouldIsolateSiblingChannelsAndOtherTenantsForRateLimit()
    {
        // Isolation (NFR30): a budget at-or-over the limit on a SIBLING channel ref, or on the SAME channel for a
        // DIFFERENT tenant, must NOT throttle this tenant's send through its own channel — the adapter IS invoked.
        FakeOutboundChannelRateLimitProvider rateLimits = new();
        // A different channel ref is at budget (0) — must not affect "adapter:mailbox-outbound".
        rateLimits.Configure(Tenant, "adapter:other-outbound", budget: 0);
        // The same channel under a different tenant is at budget (0) — must not affect this tenant.
        rateLimits.Configure("tenant-other", "adapter:mailbox-outbound", budget: 0);
        FakeOutboundChannelSendHistory history = new();
        history.Seed(Tenant, "adapter:other-outbound", FixedClock.FixedUtcNow.AddMinutes(-1));
        history.Seed("tenant-other", "adapter:mailbox-outbound", FixedClock.FixedUtcNow.AddMinutes(-1));

        RecordingEventStoreGatewayClient gateway = new();
        SpyOutboundMailboxSender sender = new();
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: sender,
            outboundChannelRateLimitProvider: rateLimits,
            outboundChannelSendHistory: history);

        _ = await dispatcher.DispatchAsync(
            OutboundSendContext(OutboundSend("send-001")),
            TestContext.Current.CancellationToken);

        sender.SendCount.ShouldBe(1);
        gateway.Submitted.ShouldHaveSingleItem()
            .Payload.TryGetProperty("AdapterStatus", out JsonElement status).ShouldBeTrue();
        status.GetString().ShouldBe("sent");
    }

    [Fact]
    public async Task DispatchShouldKeepControlStateReasonOverRateLimitGate()
    {
        // Regression: the Disabled/Quarantined control-state switch precedes the rate-limit gate, so a controlled
        // channel keeps its precise control-state reason ("blocked" / "quarantined") even when a rate-limit budget at 0
        // is ALSO configured — rate-limit never masks a control-state denial, and the rate-limit seams are not consulted.
        FakeOutboundChannelRateLimitProvider rateLimits = new();
        rateLimits.Configure(Tenant, "adapter:mailbox-outbound", budget: 0);
        FakeOutboundChannelSendHistory history = new();
        history.Seed(Tenant, "adapter:mailbox-outbound", FixedClock.FixedUtcNow.AddMinutes(-1));

        FakeOutboundChannelControlStateProvider disabled = new();
        disabled.Disable(Tenant, "adapter:mailbox-outbound");
        RecordingEventStoreGatewayClient disabledGateway = new();
        SpyOutboundMailboxSender disabledSender = new();
        AcceptedCommandDispatcher disabledDispatcher = new(
            disabledGateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: disabledSender,
            outboundChannelControlStateProvider: disabled,
            outboundChannelRateLimitProvider: rateLimits,
            outboundChannelSendHistory: history);
        _ = await disabledDispatcher.DispatchAsync(
            OutboundSendContext(OutboundSend("send-001")),
            TestContext.Current.CancellationToken);
        disabledSender.SendCount.ShouldBe(0);
        disabledGateway.Submitted.ShouldHaveSingleItem()
            .Payload.TryGetProperty("AdapterStatus", out JsonElement disabledStatus).ShouldBeTrue();
        disabledStatus.GetString().ShouldBe("blocked");
        // The rate-limit gate runs only on the Active path, so a Disabled channel never consults the rate-limit seams.
        rateLimits.ObservedRequests.ShouldBeEmpty();

        FakeOutboundChannelControlStateProvider quarantined = new();
        quarantined.Quarantine(Tenant, "adapter:mailbox-outbound");
        RecordingEventStoreGatewayClient quarantinedGateway = new();
        SpyOutboundMailboxSender quarantinedSender = new();
        AcceptedCommandDispatcher quarantinedDispatcher = new(
            quarantinedGateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: quarantinedSender,
            outboundChannelControlStateProvider: quarantined,
            outboundChannelRateLimitProvider: rateLimits,
            outboundChannelSendHistory: history);
        _ = await quarantinedDispatcher.DispatchAsync(
            OutboundSendContext(OutboundSend("send-001")),
            TestContext.Current.CancellationToken);
        quarantinedSender.SendCount.ShouldBe(0);
        quarantinedGateway.Submitted.ShouldHaveSingleItem()
            .Payload.TryGetProperty("AdapterStatus", out JsonElement quarantinedStatus).ShouldBeTrue();
        quarantinedStatus.GetString().ShouldBe("quarantined");
    }

    [Fact]
    public async Task DispatchShouldLeaveOutboundDraftInspectableWhenChannelRateLimitedAndNeverConsultRateLimitSeams()
    {
        // Rate-limit throttles ONLY the send/execute step. CreateOutboundDraft (and the approval steps that share this
        // seam) stay inspectable — the rate-limit gate is wired ONLY into the ExecuteApprovedOutboundDraft branch, so a
        // draft for a rate-limited channel dispatches normally and the rate-limit seams are never consulted.
        RecordingEventStoreGatewayClient gateway = new();
        FakeOutboundChannelRateLimitProvider rateLimits = new();
        rateLimits.Configure(Tenant, "adapter:mailbox-outbound", budget: 0);
        FakeOutboundChannelSendHistory history = new();
        history.Seed(Tenant, "adapter:mailbox-outbound", FixedClock.FixedUtcNow.AddMinutes(-1));
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: new SpyOutboundMailboxSender(),
            outboundChannelRateLimitProvider: rateLimits,
            outboundChannelSendHistory: history);

        _ = await dispatcher.DispatchAsync(
            OutboundDraftContext(OutboundDraft("draft-001")),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.CreateOutboundDraft));
        rateLimits.ObservedRequests.ShouldBeEmpty();
        history.ObservedRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchShouldLeaveOutboundApprovalRequestAndDecisionInspectableWhenChannelRateLimited()
    {
        // AC10 enumerates THREE pre-send steps that must stay inspectable for a rate-limited channel —
        // CreateOutboundDraft, RequestOutboundSendApproval, and DecideOutboundApproval — because the rate-limit gate is
        // wired ONLY into the ExecuteApprovedOutboundDraft branch. The draft step is covered by
        // DispatchShouldLeaveOutboundDraftInspectableWhenChannelRateLimited...; this closes the remaining two approval
        // steps that the AC names (the existing tests only asserted them in a comment). Each must dispatch normally, the
        // send adapter must never be invoked, and the rate-limit seams must never be consulted (the gate is unreachable
        // off the send branch) even when an at-budget (0) limit is configured for the channel.
        JsonSerializerOptions webOptions = new(JsonSerializerDefaults.Web);
        foreach ((string CommandType, JsonElement Payload) step in new[]
                 {
                     (nameof(Hexalith.ChatBot.Contracts.Commands.RequestOutboundSendApproval),
                         JsonSerializer.SerializeToElement(OutboundApprovalRequest("approval-001"), webOptions)),
                     (nameof(Hexalith.ChatBot.Contracts.Commands.DecideOutboundApproval),
                         JsonSerializer.SerializeToElement(OutboundApprovalDecision("decision-001"), webOptions)),
                 })
        {
            RecordingEventStoreGatewayClient gateway = new();
            SpyOutboundMailboxSender sender = new();
            FakeOutboundChannelRateLimitProvider rateLimits = new();
            rateLimits.Configure(Tenant, "adapter:mailbox-outbound", budget: 0);
            FakeOutboundChannelSendHistory history = new();
            history.Seed(Tenant, "adapter:mailbox-outbound", FixedClock.FixedUtcNow.AddMinutes(-1));
            AcceptedCommandDispatcher dispatcher = new(
                gateway,
                new NoOpParticipantResolutionOrchestrator(),
                new NoOpAssociationScoringOrchestrator(),
                new FixedClock(),
                outboundMailboxSender: sender,
                outboundChannelRateLimitProvider: rateLimits,
                outboundChannelSendHistory: history);

            _ = await dispatcher.DispatchAsync(
                Context(step.Payload, commandType: step.CommandType),
                TestContext.Current.CancellationToken);

            gateway.Submitted.ShouldHaveSingleItem().CommandType.ShouldBe(step.CommandType);
            sender.SendCount.ShouldBe(0);
            rateLimits.ObservedRequests.ShouldBeEmpty();
            history.ObservedRequests.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task DispatchShouldCountOnlyAdmittedSendsInsideTheTrailingWindowForRateLimit()
    {
        // AC5: the count is the server-measured UTC age against the injected clock over the rolling-hour window —
        // sends that have aged OUT of the window (and any future-dated timestamps) must NOT count. budget = 3, six
        // seeded timestamps but only TWO inside the trailing hour → admitted (2 < 3). A naive total count (6) would
        // wrongly deny, so this proves the WindowDuration + CountInTrailingWindow wiring is exercised.
        FakeOutboundChannelRateLimitProvider rateLimits = new();
        rateLimits.Configure(Tenant, "adapter:mailbox-outbound", budget: 3);
        FakeOutboundChannelSendHistory history = new();
        history.Seed(
            Tenant,
            "adapter:mailbox-outbound",
            FixedClock.FixedUtcNow.AddMinutes(-10),   // inside the window
            FixedClock.FixedUtcNow.AddMinutes(-59),   // inside the window (just under the hour)
            FixedClock.FixedUtcNow.AddHours(-1),      // exactly one hour old → aged out (age == window is OUTSIDE)
            FixedClock.FixedUtcNow.AddMinutes(-61),   // aged out
            FixedClock.FixedUtcNow.AddHours(-3),      // aged out
            FixedClock.FixedUtcNow.AddMinutes(30));   // future → ignored (negative age)

        RecordingEventStoreGatewayClient gateway = new();
        SpyOutboundMailboxSender sender = new();
        AcceptedCommandDispatcher dispatcher = new(
            gateway,
            new NoOpParticipantResolutionOrchestrator(),
            new NoOpAssociationScoringOrchestrator(),
            new FixedClock(),
            outboundMailboxSender: sender,
            outboundChannelRateLimitProvider: rateLimits,
            outboundChannelSendHistory: history);

        _ = await dispatcher.DispatchAsync(
            OutboundSendContext(OutboundSend("send-001")),
            TestContext.Current.CancellationToken);
        sender.SendCount.ShouldBe(1);
    }

    [Fact]
    public void OutOfBoundsConfiguredOutboundBudgetShouldFallBackToSafeDefaultNeverRaisingTheCap()
    {
        // An out-of-bounds configured budget falls back to the safe default (= maximum) at the enforcement seam — it
        // can never silently raise the cap above the declared maximum.
        new OutboundChannelRateLimitState(OutboundChannelRateLimitBounds.Maximum + 5_000, OutboundChannelRateLimitWindow.RollingHour)
            .EffectiveBudget.ShouldBe(OutboundChannelRateLimitBounds.Maximum);
        new OutboundChannelRateLimitState(-10, OutboundChannelRateLimitWindow.RollingHour)
            .EffectiveBudget.ShouldBe(OutboundChannelRateLimitBounds.Maximum);
        new OutboundChannelRateLimitState(42, OutboundChannelRateLimitWindow.RollingHour)
            .EffectiveBudget.ShouldBe(42);
    }

    [Fact]
    public void OutboundChannelCapacityImpactObservationShouldCarryFiniteIntegerBudgetCountAndThrottledFlag()
    {
        // AC6: the capacity-impact surface is carried as safe, finite integer tokens — the effective budget, the
        // observed trailing-window admitted-send count, and whether this send was throttled — never floats.
        OutboundChannelRateLimitObservation throttled = new(Budget: 3, ObservedWindowCount: 3, Throttled: true);
        throttled.Budget.ShouldBe(3);
        throttled.ObservedWindowCount.ShouldBe(3);
        throttled.Throttled.ShouldBeTrue();

        OutboundChannelRateLimitObservation admitted = new(Budget: 10, ObservedWindowCount: 4, Throttled: false);
        admitted.Throttled.ShouldBeFalse();
        admitted.ObservedWindowCount.ShouldBeLessThan(admitted.Budget);

        typeof(OutboundChannelRateLimitObservation)
            .GetProperties()
            .Select(property => property.PropertyType)
            .ShouldBe([typeof(int), typeof(int), typeof(bool)]);
    }

    [Fact]
    public async Task DispatchShouldRouteCommandCapabilityQuarantineApprovalToQuarantineChangeAggregateForDistinctApprover()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireApproveCommandCapabilityQuarantineCommand("admin-requester", "admin-approver"),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveCommandCapabilityQuarantine)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("command-capability-quarantine-001");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ApproveCommandCapabilityQuarantine));
        result.ResourceId.ShouldBe("command-capability-quarantine-001");

        // The forwarded payload is PascalCase so the aggregate engine can deserialize it (matches the disable/policy flow).
        request.Payload.TryGetProperty("QuarantineChangeId", out JsonElement changeId).ShouldBeTrue();
        changeId.GetString().ShouldBe("command-capability-quarantine-001");
        request.Payload.TryGetProperty("quarantineChangeId", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchShouldRejectCommandCapabilityQuarantineApprovalWhenApproverEqualsRequester()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        // Third enforcement layer (dispatcher) of the FR75d two-person rule for the command-capability quarantine: a
        // single actor cannot both request and approve. This guards even if the gateway-validation and aggregate
        // checks were bypassed, mirroring the command-capability disable distinct-approver dispatcher guard.
        // Nothing is submitted to the spine.
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(
                Context(
                    WireApproveCommandCapabilityQuarantineCommand("admin-requester", "admin-requester"),
                    commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveCommandCapabilityQuarantine)),
                TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldBe("The command-capability quarantine approval command is missing valid approval metadata.");
        gateway.Submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchShouldRouteAiActorQuarantineApprovalToQuarantineChangeAggregateForDistinctApprover()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireApproveAiActorQuarantineCommand("admin-requester", "admin-approver"),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveAiActorQuarantine)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("ai-actor-quarantine-001");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ApproveAiActorQuarantine));
        result.ResourceId.ShouldBe("ai-actor-quarantine-001");

        // The forwarded payload is PascalCase so the aggregate engine can deserialize it (matches the disable/policy flow).
        request.Payload.TryGetProperty("QuarantineChangeId", out JsonElement changeId).ShouldBeTrue();
        changeId.GetString().ShouldBe("ai-actor-quarantine-001");
        request.Payload.TryGetProperty("quarantineChangeId", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchShouldRejectAiActorQuarantineApprovalWhenApproverEqualsRequester()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        // Third enforcement layer (dispatcher) of the FR75d two-person rule for the AI-actor quarantine: a single actor
        // cannot both request and approve. This guards even if the gateway-validation and aggregate checks were
        // bypassed, mirroring the AI-actor disable distinct-approver dispatcher guard. Nothing is submitted to the spine.
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(
                Context(
                    WireApproveAiActorQuarantineCommand("admin-requester", "admin-requester"),
                    commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveAiActorQuarantine)),
                TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldBe("The AI-actor quarantine approval command is missing valid approval metadata.");
        gateway.Submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchShouldRouteServiceClientQuarantineApprovalToQuarantineChangeAggregateForDistinctApprover()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        ChatBotDispatchResult result = await dispatcher.DispatchAsync(
            Context(
                WireApproveServiceClientQuarantineCommand("admin-requester", "admin-approver"),
                commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveServiceClientQuarantine)),
            TestContext.Current.CancellationToken);

        SubmitCommandRequest request = gateway.Submitted.ShouldHaveSingleItem();
        request.AggregateId.ShouldBe("service-client-quarantine-001");
        request.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.ApproveServiceClientQuarantine));
        result.ResourceId.ShouldBe("service-client-quarantine-001");

        // The forwarded payload is PascalCase so the aggregate engine can deserialize it (matches the disable/policy flow).
        request.Payload.TryGetProperty("QuarantineChangeId", out JsonElement changeId).ShouldBeTrue();
        changeId.GetString().ShouldBe("service-client-quarantine-001");
        request.Payload.TryGetProperty("quarantineChangeId", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchShouldRejectServiceClientQuarantineApprovalWhenApproverEqualsRequester()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new NoOpParticipantResolutionOrchestrator(), new NoOpAssociationScoringOrchestrator(), new FixedClock());

        // Third enforcement layer (dispatcher) of the FR75d two-person rule for the service-client quarantine: a
        // single actor cannot both request and approve. This guards even if the gateway-validation and aggregate
        // checks were bypassed, mirroring the service-client disable distinct-approver dispatcher guard. Nothing is
        // submitted to the spine.
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(
                Context(
                    WireApproveServiceClientQuarantineCommand("admin-requester", "admin-requester"),
                    commandType: nameof(Hexalith.ChatBot.Contracts.Commands.ApproveServiceClientQuarantine)),
                TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldBe("The service-client quarantine approval command is missing valid approval metadata.");
        gateway.Submitted.ShouldBeEmpty();
    }

    private static ChatBotGatewayContext Context(
        JsonElement command,
        string? taskId = TaskId,
        string commandType = nameof(RecordGovernedNote))
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test"));
        ChatBotCommandSubmission submission = new(
            principal,
            new CommandSubmissionRequest
            {
                CommandId = CommandId,
                CommandType = commandType,
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            CorrelationId,
            taskId,
            ChatBotSurfaceOrigin.Ui);
        return new ChatBotGatewayContext(
            submission,
            new ChatBotAuthenticatedActor("actor-alpha", principal),
            new ChatBotTenantBinding(Tenant));
    }

    // The inbound wire body is camelCase, mirroring what the adapter posts to /api/v1/commands.
    private static JsonElement WireCommand(string noteId)
        => JsonDocument.Parse($$"""{"noteId":"{{noteId}}"}""").RootElement.Clone();

    private static SubmitNotificationRoutingChange NotificationRoutingChange()
        => new(
            "routing-change-001",
            "routing-snapshot-current",
            "routing-snapshot-proposed",
            4,
            new NotificationRoutingChangeSet(
            [
                new NotificationRoutingEntry(NotificationStateClass.ReviewNeeded, AdminScope.SeeOnly, AdminRole.OperationsAdmin, NotificationChannel.InApp),
                new NotificationRoutingEntry(NotificationStateClass.ApprovalPending, AdminScope.Policy, AdminRole.PolicyAdmin, NotificationChannel.Email),
                new NotificationRoutingEntry(NotificationStateClass.Failure, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
            ]),
            "routing-update",
            "admin-requester",
            NotificationRoutingSchemaVersions.V1,
            CorrelationId,
            "sha256:routingold",
            "sha256:routingnew");

    // camelCase wire body for the mailbox-source quarantine approval, mirroring what the adapter posts.
    private static JsonElement WireApproveMailboxSourceQuarantineCommand(string requesterRef, string approverRef)
        => JsonDocument.Parse(
            $$"""
            {
              "quarantineChangeId": "mailbox-quarantine-001",
              "mailboxSourceRef": "mailbox-source:controlled-mailbox-001",
              "reasonCode": "mailbox-source-unsafe-activity",
              "policySnapshotId": "policy-snapshot-mailbox-v1",
              "oldState": "active",
              "newState": "quarantined",
              "sourceVersion": 5,
              "requesterRef": "{{requesterRef}}",
              "approverRef": "{{approverRef}}",
              "schemaVersion": "mailbox-source-control-schema.v1",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW"
            }
            """).RootElement.Clone();

    // camelCase wire body for the service-client disable approval, mirroring what the adapter posts.
    private static JsonElement WireApproveServiceClientDisableCommand(string requesterRef, string approverRef)
        => JsonDocument.Parse(
            $$"""
            {
              "disableChangeId": "service-client-disable-001",
              "serviceClientRef": "service-client:cli-automation-client",
              "reasonCode": "service-client-unsafe-activity",
              "policySnapshotId": "policy-snapshot-tenant-admin-v1",
              "oldState": "active",
              "newState": "disabled",
              "sourceVersion": 5,
              "requesterRef": "{{requesterRef}}",
              "approverRef": "{{approverRef}}",
              "schemaVersion": "service-client-control-schema.v1",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW"
            }
            """).RootElement.Clone();

    // camelCase wire body for the AI-actor disable approval, mirroring what the adapter posts.
    private static JsonElement WireApproveAiActorDisableCommand(string requesterRef, string approverRef)
        => JsonDocument.Parse(
            $$"""
            {
              "disableChangeId": "ai-actor-disable-001",
              "aiActorRef": "ai-actor:gpt-mediation-actor",
              "reasonCode": "ai-actor-unsafe-proposals",
              "policySnapshotId": "policy-snapshot-policy-admin-v1",
              "oldState": "active",
              "newState": "disabled",
              "sourceVersion": 5,
              "requesterRef": "{{requesterRef}}",
              "approverRef": "{{approverRef}}",
              "schemaVersion": "ai-actor-control-schema.v1",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW"
            }
            """).RootElement.Clone();

    // camelCase wire body for the command-capability disable approval, mirroring what the adapter posts. The
    // subject is the safe command TYPE name (commandCapabilityRef), not an actor id — the FR74 divergence.
    private static JsonElement WireApproveCommandCapabilityDisableCommand(string requesterRef, string approverRef)
        => JsonDocument.Parse(
            $$"""
            {
              "disableChangeId": "command-capability-disable-001",
              "commandCapabilityRef": "AssociateEmailToProject",
              "reasonCode": "command-capability-unsafe-execution",
              "policySnapshotId": "policy-snapshot-policy-admin-v1",
              "oldState": "active",
              "newState": "disabled",
              "sourceVersion": 5,
              "requesterRef": "{{requesterRef}}",
              "approverRef": "{{approverRef}}",
              "schemaVersion": "command-capability-control-schema.v1",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW"
            }
            """).RootElement.Clone();

    // camelCase wire body for the outbound-channel disable approval, mirroring what the adapter posts. The subject is
    // the safe outbound-channel ref (the AdapterRef token), not an actor id or command type — the Story 7.24 subject.
    private static JsonElement WireApproveOutboundChannelDisableCommand(string requesterRef, string approverRef)
        => JsonDocument.Parse(
            $$"""
            {
              "disableChangeId": "outbound-channel-disable-001",
              "outboundChannelRef": "adapter:mailbox-outbound",
              "reasonCode": "outbound-channel-policy-violation",
              "policySnapshotId": "policy-snapshot-policy-admin-v1",
              "oldState": "active",
              "newState": "disabled",
              "sourceVersion": 5,
              "requesterRef": "{{requesterRef}}",
              "approverRef": "{{approverRef}}",
              "schemaVersion": "outbound-channel-control-schema.v1",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW"
            }
            """).RootElement.Clone();

    // camelCase wire body for the outbound-channel quarantine approval, mirroring what the adapter posts. The subject
    // is the safe outbound-channel ref (the AdapterRef token), not an actor id or command type — the Story 7.25 subject.
    private static JsonElement WireApproveOutboundChannelQuarantineCommand(string requesterRef, string approverRef)
        => JsonDocument.Parse(
            $$"""
            {
              "quarantineChangeId": "outbound-channel-quarantine-001",
              "outboundChannelRef": "adapter:mailbox-outbound",
              "reasonCode": "outbound-channel-policy-violation",
              "policySnapshotId": "policy-snapshot-policy-admin-v1",
              "oldState": "active",
              "newState": "quarantined",
              "sourceVersion": 5,
              "requesterRef": "{{requesterRef}}",
              "approverRef": "{{approverRef}}",
              "schemaVersion": "outbound-channel-control-schema.v1",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW"
            }
            """).RootElement.Clone();

    private static Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedOutboundDraft OutboundSend(string sendId)
        => new(
            sendId,
            "approval-001",
            "draft-001",
            "project-001",
            "requester-001",
            "actor-alpha",
            "conv-001",
            "msg-001",
            "item-001",
            ["recipient:party-001"],
            ["conversation:conv-001", "source-message:msg-001"],
            "policy-snap-001",
            nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedOutboundDraft),
            "chatbot-spine.v1",
            Hexalith.ChatBot.Contracts.Enums.SenderAuthorityClass.AuthenticatedUserSend,
            Hexalith.ChatBot.Contracts.Enums.ApprovalEvidenceFreshness.Fresh,
            3,
            1,
            CorrelationId);

    // A context whose authenticated principal carries the outbound-send authority claims so the dispatcher's
    // OutboundSendAuthorityEvaluator.Classify passes (the disabled-channel check runs AFTER Classify).
    private static ChatBotGatewayContext OutboundSendContext(
        Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedOutboundDraft command,
        string tenant = Tenant)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
                new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, "project-001"),
                new Claim(Hexalith.ChatBot.Server.Governance.Outbound.OutboundDraftAuthorityEvaluator.ProjectScopeClaim, "project-001:outbound-send"),
                new Claim(Hexalith.ChatBot.Server.Governance.Outbound.OutboundDraftAuthorityEvaluator.TenantOutboundPolicyClaim, "authenticated-user-send"),
                new Claim(Hexalith.ChatBot.Server.Governance.Outbound.OutboundSendAuthorityEvaluator.MailboxIdClaim, "mailbox-001"),
                new Claim(Hexalith.ChatBot.Server.Governance.Outbound.OutboundSendAuthorityEvaluator.MailboxOwnerClaim, "mailbox-001"),
                new Claim(Hexalith.ChatBot.Server.Governance.Outbound.OutboundSendAuthorityEvaluator.OwnMailboxMailSendClaim, "true"),
            ],
            "test"));
        ChatBotCommandSubmission submission = new(
            principal,
            new CommandSubmissionRequest
            {
                CommandId = CommandId,
                CommandType = nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedOutboundDraft),
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            CorrelationId,
            TaskId,
            ChatBotSurfaceOrigin.Ui);
        return new ChatBotGatewayContext(
            submission,
            new ChatBotAuthenticatedActor("actor-alpha", principal),
            new ChatBotTenantBinding(tenant));
    }

    // Story 9.4: a replay-run variant of OutboundSendContext — same outbound-send authority claims, but bound to a TEST
    // tenant and carrying a ReplayRunId on the immutable submission so the marker threads into the send request → trace.
    private static ChatBotGatewayContext OutboundSendContextWithReplay(
        Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedOutboundDraft command,
        string tenant,
        string replayRunId)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
                new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, "project-001"),
                new Claim(Hexalith.ChatBot.Server.Governance.Outbound.OutboundDraftAuthorityEvaluator.ProjectScopeClaim, "project-001:outbound-send"),
                new Claim(Hexalith.ChatBot.Server.Governance.Outbound.OutboundDraftAuthorityEvaluator.TenantOutboundPolicyClaim, "authenticated-user-send"),
                new Claim(Hexalith.ChatBot.Server.Governance.Outbound.OutboundSendAuthorityEvaluator.MailboxIdClaim, "mailbox-001"),
                new Claim(Hexalith.ChatBot.Server.Governance.Outbound.OutboundSendAuthorityEvaluator.MailboxOwnerClaim, "mailbox-001"),
                new Claim(Hexalith.ChatBot.Server.Governance.Outbound.OutboundSendAuthorityEvaluator.OwnMailboxMailSendClaim, "true"),
            ],
            "test"));
        ChatBotCommandSubmission submission = new(
            principal,
            new CommandSubmissionRequest
            {
                CommandId = CommandId,
                CommandType = nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedOutboundDraft),
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            CorrelationId,
            TaskId,
            ChatBotSurfaceOrigin.Ui,
            replayRunId);
        return new ChatBotGatewayContext(
            submission,
            new ChatBotAuthenticatedActor("actor-alpha", principal),
            new ChatBotTenantBinding(tenant));
    }

    private static Hexalith.ChatBot.Contracts.Commands.CreateOutboundDraft OutboundDraft(string draftId)
        => new(
            draftId,
            "project-001",
            "requester-001",
            "actor-alpha",
            "conv-001",
            "msg-001",
            "item-001",
            ["recipient:party-001"],
            ["conversation:conv-001", "source-message:msg-001"],
            "policy-snap-001",
            CorrelationId,
            new Hexalith.ChatBot.Contracts.Commands.OutboundDraftContent("Status update", "Governed draft content.", "text/plain"));

    // A context whose authenticated principal carries draft-only outbound authority (project ownership + the
    // outbound-draft project scope + the tenant draft-only policy) so the dispatcher's
    // OutboundDraftAuthorityEvaluator.Classify passes for the default SenderAuthorityClass.DraftOnly. This is the
    // proven draft-creation claim recipe (CommandGatewayTests outbound-draft path).
    private static ChatBotGatewayContext OutboundDraftContext(
        Hexalith.ChatBot.Contracts.Commands.CreateOutboundDraft command,
        string tenant = Tenant)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
                new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, "project-001"),
                new Claim(Hexalith.ChatBot.Server.Governance.Outbound.OutboundDraftAuthorityEvaluator.ProjectScopeClaim, "project-001:outbound-draft"),
                new Claim(Hexalith.ChatBot.Server.Governance.Outbound.OutboundDraftAuthorityEvaluator.TenantOutboundPolicyClaim, "draft-only"),
            ],
            "test"));
        ChatBotCommandSubmission submission = new(
            principal,
            new CommandSubmissionRequest
            {
                CommandId = CommandId,
                CommandType = nameof(Hexalith.ChatBot.Contracts.Commands.CreateOutboundDraft),
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            CorrelationId,
            TaskId,
            ChatBotSurfaceOrigin.Ui);
        return new ChatBotGatewayContext(
            submission,
            new ChatBotAuthenticatedActor("actor-alpha", principal),
            new ChatBotTenantBinding(tenant));
    }

    // A RequestOutboundSendApproval carrying the trusted approval metadata the dispatcher's approval-request branch
    // requires (non-empty approval/draft/project ids + a content snapshot). This step is dispatched off the send branch,
    // so the outbound-channel rate-limit gate never applies to it (AC10 inspectability).
    private static Hexalith.ChatBot.Contracts.Commands.RequestOutboundSendApproval OutboundApprovalRequest(string approvalId)
        => new(
            approvalId,
            "draft-001",
            "project-001",
            "requester-001",
            "conv-001",
            "msg-001",
            "item-001",
            ["recipient:party-001"],
            ["conversation:conv-001", "source-message:msg-001"],
            "policy-snap-001",
            "metadata_only",
            nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedOutboundDraft),
            "chatbot-spine.v1",
            "metadata_only",
            new Hexalith.ChatBot.Contracts.Commands.OutboundApprovalContentSnapshot(
                new Hexalith.ChatBot.Contracts.Commands.OutboundDraftContent("Status update", "Governed draft content.", "text/plain"),
                null,
                "metadata_only",
                null),
            Hexalith.ChatBot.Contracts.Enums.SenderAuthorityClass.AuthenticatedUserSend,
            Hexalith.ChatBot.Contracts.Enums.ApprovalEvidenceFreshness.Fresh,
            3,
            CorrelationId);

    // A DecideOutboundApproval carrying the trusted decision metadata the dispatcher's approval-decision branch requires
    // (non-empty approval/draft/project/decision ids + a positive expected approval source version). Like the approval
    // request, this step is dispatched off the send branch, so the rate-limit gate never applies to it.
    private static Hexalith.ChatBot.Contracts.Commands.DecideOutboundApproval OutboundApprovalDecision(string decisionId)
        => new(
            "approval-001",
            "draft-001",
            "project-001",
            Hexalith.ChatBot.Contracts.Enums.ApprovalDecisionKind.Approve,
            decisionId,
            3,
            CorrelationId);

    // camelCase wire body for the command-capability quarantine approval, mirroring what the adapter posts. The
    // subject is the safe command TYPE name (commandCapabilityRef), not an actor id — the FR74 divergence.
    private static JsonElement WireApproveCommandCapabilityQuarantineCommand(string requesterRef, string approverRef)
        => JsonDocument.Parse(
            $$"""
            {
              "quarantineChangeId": "command-capability-quarantine-001",
              "commandCapabilityRef": "AssociateEmailToProject",
              "reasonCode": "command-capability-unsafe-execution",
              "policySnapshotId": "policy-snapshot-policy-admin-v1",
              "oldState": "active",
              "newState": "quarantined",
              "sourceVersion": 5,
              "requesterRef": "{{requesterRef}}",
              "approverRef": "{{approverRef}}",
              "schemaVersion": "command-capability-control-schema.v1",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW"
            }
            """).RootElement.Clone();

    // camelCase wire body for the AI-actor quarantine approval, mirroring what the adapter posts.
    private static JsonElement WireApproveAiActorQuarantineCommand(string requesterRef, string approverRef)
        => JsonDocument.Parse(
            $$"""
            {
              "quarantineChangeId": "ai-actor-quarantine-001",
              "aiActorRef": "ai-actor:gpt-mediation-actor",
              "reasonCode": "ai-actor-unsafe-proposals",
              "policySnapshotId": "policy-snapshot-policy-admin-v1",
              "oldState": "active",
              "newState": "quarantined",
              "sourceVersion": 5,
              "requesterRef": "{{requesterRef}}",
              "approverRef": "{{approverRef}}",
              "schemaVersion": "ai-actor-control-schema.v1",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW"
            }
            """).RootElement.Clone();

    // camelCase wire body for the service-client quarantine approval, mirroring what the adapter posts.
    private static JsonElement WireApproveServiceClientQuarantineCommand(string requesterRef, string approverRef)
        => JsonDocument.Parse(
            $$"""
            {
              "quarantineChangeId": "service-client-quarantine-001",
              "serviceClientRef": "service-client:cli-automation-client",
              "reasonCode": "service-client-unsafe-activity",
              "policySnapshotId": "policy-snapshot-tenant-admin-v1",
              "oldState": "active",
              "newState": "quarantined",
              "sourceVersion": 5,
              "requesterRef": "{{requesterRef}}",
              "approverRef": "{{approverRef}}",
              "schemaVersion": "service-client-control-schema.v1",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW"
            }
            """).RootElement.Clone();

    private static JsonElement WireParticipantResolutionCommand()
        => JsonDocument.Parse(
            """
            {
              "resolutionId": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
              "intakeId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "sourceMailboxId": "controlled-mailbox-001",
              "sourceParticipants": [
                {
                  "sourceParticipantId": "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                  "role": "sender",
                  "evidenceReference": "mailbox:intake:sender",
                  "evidenceFingerprint": "evidence-sha256",
                  "addressEvidence": "sender@example.test",
                  "displayNameEvidence": "Sender"
                }
              ],
              "resolvedParticipants": [],
              "unresolvedParticipants": [],
              "resolutionKernelVersion": "participant-resolution.kernel.v1"
            }
            """).RootElement.Clone();

    private static JsonElement MalformedParticipantResolutionCommand()
        => JsonDocument.Parse(
            """
            {
              "resolutionId": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
              "intakeId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "sourceMailboxId": "",
              "sourceParticipants": null,
              "resolvedParticipants": [],
              "unresolvedParticipants": [],
              "resolutionKernelVersion": "participant-resolution.kernel.v1"
            }
            """).RootElement.Clone();

    private static JsonElement WireAssociationScoringCommand()
        => JsonDocument.Parse(
            """
            {
              "associationId": "01ARZ3NDEKTSV4RRFFQ69G5FAB",
              "intakeId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "sourceMailboxId": "controlled-mailbox-001",
              "sourceConversationId": "conversation-001",
              "sourceThreadId": "thread-001",
              "deterministicSignals": [
                {
                  "signalClass": "ExplicitProjectIdentifier",
                  "projectId": "project-001",
                  "evidenceReference": "mailbox:project-id",
                  "evidenceFingerprint": "hash-project",
                  "weight": 0.9,
                  "requiredForAutoAssociation": true
                }
              ],
              "thresholdPolicy": null,
              "candidates": [],
              "exclusions": [],
              "result": null,
              "scoringKernelVersion": "association-deterministic.kernel.m0.v1"
            }
            """).RootElement.Clone();

    private static JsonElement WireAssociationDecisionCommand()
        => JsonDocument.Parse(
            """
            {
              "associationId": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
              "intakeId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "projectId": "project-001",
              "decisionKind": "associate",
              "decisionNote": "Reviewed safe metadata.",
              "candidateEvidenceFingerprint": "hash-project",
              "sourceVersion": 1,
              "schemaVersion": "chatbot.association-decision-command.v1"
            }
            """).RootElement.Clone();

    private static JsonElement WireAssociationCorrectionCommand()
        => JsonDocument.Parse(
            """
            {
              "associationId": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
              "intakeId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "priorProjectId": "project-001",
              "targetProjectId": "project-002",
              "correctionKind": "project-reassignment",
              "correctionRationale": "Wrong project selected from safe metadata.",
              "predecessorAssociationId": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
              "candidateEvidenceFingerprint": "hash-project-002",
              "sourceVersion": 2,
              "schemaVersion": "chatbot.association-correction-command.v1"
            }
            """).RootElement.Clone();

    private static JsonElement WireWorkflowRetryCommand()
        => JsonDocument.Parse(
            """
            {
              "retryId": "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
              "failedEventId": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
              "failedOperationClass": "message-intake",
              "failureReasonCode": "graph_throttled",
              "expectedFailedSourceVersion": 7,
              "rationale": "safe metadata retry"
            }
            """).RootElement.Clone();

    private static JsonElement WireLowRiskExecutionCommand()
        => JsonDocument.Parse(
            """
            {
              "projectId": "project-001",
              "proposalId": "ai-proposal-001",
              "taskIntentId": "task-intent-001",
              "sourceMessageId": "graph-message-001",
              "requesterId": "party-001",
              "assistanceKind": "summarize-visible-context",
              "contextPackageId": "context-package-001",
              "contextPackageVersion": "v1",
              "contextPackageRedactionState": "metadata_only",
              "retentionClass": "collaboration_input",
              "providerReuseSetting": "disabled",
              "sourceEvidenceReferences": ["evidence-001"],
              "authorizedContextReferences": ["evidence-001"],
              "excludedContextReasons": ["redacted"],
              "expectedProposalSourceVersion": 8,
              "policySnapshotId": "policy-snap-001",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW",
              "executionId": "ai-execution-001",
              "transitionId": "transition-001",
              "sourceConversationItemId": "conversation-item-001"
            }
            """).RootElement.Clone();

    private static JsonElement WireApprovedAiExecutionCommand(string commandName = "Project.AppendConversationMessage")
        => JsonDocument.Parse(
            $$"""
            {
              "projectId": "project-001",
              "proposalId": "ai-proposal-001",
              "approvalId": "approval:ai-proposal-001",
              "taskIntentId": "task-intent-001",
              "sourceMessageId": "graph-message-001",
              "requesterId": "party-001",
              "commandName": "{{commandName}}",
              "commandAllowlistVersion": "ai-action-command-allowlist.m0",
              "expectedApprovalSourceVersion": 10,
              "expectedProposalSourceVersion": 9,
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW",
              "executionId": "ai-approved-execution-001",
              "transitionId": "approved-execution-transition-001",
              "sourceEvidenceReferences": ["evidence-001"],
              "affectedResourceReferences": ["project:project-001"],
              "recipientReferences": ["party-001"],
              "sourceConversationItemId": "conversation-item-001",
              "policySnapshotId": "policy-snap-001"
            }
            """).RootElement.Clone();

    private static JsonElement WireProposalInvalidationCommand()
        => JsonDocument.Parse(
            """
            {
              "projectId": "project-001",
              "proposalId": "ai-proposal-001",
              "approvalId": null,
              "taskIntentId": "task-intent-001",
              "sourceMessageId": "graph-message-001",
              "sourceConversationItemId": "conversation-item-001",
              "requesterId": "party-001",
              "associationId": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
              "correctionId": "01ARZ3NDEKTSV4RRFFQ69G5FAV:correction:11",
              "correctedEvidenceState": "corrected",
              "evidenceSnapshotSourceVersion": 11,
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW"
            }
            """).RootElement.Clone();

    private static Hexalith.ChatBot.Contracts.Queries.AiActionRiskClassificationRecord LowRiskClassification()
        => new(
            ContractAiActionRiskClass.LowRisk,
            [],
            "chatbot.ai-action-risk-classifier.m0.v1",
            new ContractAiActionRiskInputTuple(
                "ChatBot.ExecuteLowRiskAssistance",
                [],
                "read-only",
                "low-risk",
                "project-contributor",
                "policy-snap-001",
                "ai-action-command-allowlist.m0",
                ContractAiActionRiskClass.LowRisk,
                "declared",
                "authorized",
                CorrelationId),
            "policy-snap-001",
            "ai-action-command-allowlist.m0",
            ContractAiActionRiskClass.LowRisk,
            "project-contributor",
            "low_risk_tuple",
            "metadata_only",
            "collaboration_input",
            "chatbot.ai-action-risk-classification.v1",
            CorrelationId,
            FixedClock.FixedUtcNow);

    private sealed class FixedClock : ISystemClock
    {
        public static DateTimeOffset FixedUtcNow { get; } = new(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);

        public DateTimeOffset UtcNow => FixedUtcNow;
    }

    private sealed class NoOpParticipantResolutionOrchestrator : IParticipantResolutionOrchestrator
    {
        public ValueTask<Hexalith.ChatBot.Contracts.Commands.ResolveMailboxMessageParticipants> ResolveAsync(
            Hexalith.ChatBot.Contracts.Commands.ResolveMailboxMessageParticipants command,
            ChatBotGatewayContext context,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(command);
    }

    private sealed class NoOpAssociationScoringOrchestrator : IAssociationScoringOrchestrator
    {
        public ValueTask<Hexalith.ChatBot.Contracts.Commands.ScoreMailboxMessageAssociation> ScoreAsync(
            Hexalith.ChatBot.Contracts.Commands.ScoreMailboxMessageAssociation command,
            ChatBotGatewayContext context,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(command);
    }

    private sealed class RecordingAssociationScoringOrchestrator : IAssociationScoringOrchestrator
    {
        public int ScoreCount { get; private set; }

        public string? TenantId { get; private set; }

        public ValueTask<Hexalith.ChatBot.Contracts.Commands.ScoreMailboxMessageAssociation> ScoreAsync(
            Hexalith.ChatBot.Contracts.Commands.ScoreMailboxMessageAssociation command,
            ChatBotGatewayContext context,
            CancellationToken cancellationToken)
        {
            ScoreCount++;
            TenantId = context.TenantBinding.TenantId;
            ContractAssociationScoringResult result = new(
                0.9,
                ContractAssociationThresholdBand.Auto,
                ContractAssociationScoringOutcome.CandidatesGenerated,
                [ContractAssociationReasonCode.MissingRequiredEvidence],
                command.ScoringKernelVersion,
                FixedClock.FixedUtcNow,
                command.SourceMailboxId,
                command.IntakeId,
                command.SourceConversationId,
                command.SourceThreadId,
                context.Submission.CorrelationId,
                "metadata_only",
                "collaboration_input",
                "chatbot.association-scoring-result.v1");

            return ValueTask.FromResult(command with
            {
                ThresholdPolicy = ContractAssociationThresholdPolicySnapshot.DefaultM0,
                Candidates = [],
                Exclusions = [],
                Result = result,
            });
        }
    }

    private sealed class RecordingParticipantResolutionOrchestrator : IParticipantResolutionOrchestrator
    {
        public int ResolveCount { get; private set; }

        public string? TenantId { get; private set; }

        public ValueTask<Hexalith.ChatBot.Contracts.Commands.ResolveMailboxMessageParticipants> ResolveAsync(
            Hexalith.ChatBot.Contracts.Commands.ResolveMailboxMessageParticipants command,
            ChatBotGatewayContext context,
            CancellationToken cancellationToken)
        {
            ResolveCount++;
            TenantId = context.TenantBinding.TenantId;

            return ValueTask.FromResult(command with
            {
                ResolvedParticipants =
                [
                    new Hexalith.ChatBot.Contracts.Commands.ResolvedMailboxParticipantReference(
                        "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                        "tenant-alpha:parties:party-001",
                        "tenant-alpha",
                        "mailbox:intake:sender",
                        "evidence-sha256",
                        Hexalith.ChatBot.Contracts.Enums.ParticipantResolutionStatus.Resolved),
                ],
                UnresolvedParticipants =
                [
                    new Hexalith.ChatBot.Contracts.Commands.UnresolvedMailboxParticipantEvidence(
                        "01ARZ3NDEKTSV4RRFFQ69G5FAA",
                        "mailbox:intake:recipient:0",
                        "recipient-evidence-sha256",
                        Hexalith.ChatBot.Contracts.Enums.ParticipantResolutionBlockedReason.NotFound,
                        [
                            Hexalith.ChatBot.Contracts.Enums.ParticipantReviewAction.Link,
                            Hexalith.ChatBot.Contracts.Enums.ParticipantReviewAction.CreatePending,
                            Hexalith.ChatBot.Contracts.Enums.ParticipantReviewAction.Reject,
                            Hexalith.ChatBot.Contracts.Enums.ParticipantReviewAction.Quarantine,
                        ]),
                ],
            });
        }
    }

    private sealed class RecordingEventStoreGatewayClient : IEventStoreGatewayClient
    {
        private readonly List<SubmitCommandRequest> _submitted = [];

        public IReadOnlyList<SubmitCommandRequest> Submitted => _submitted;

        public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            _submitted.Add(request);
            return Task.FromResult(new SubmitCommandResponse(request.CorrelationId ?? request.MessageId));
        }

        public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    // Simulates a failing dispatch so the latency-on-failure path (AC1/AC5) can be asserted.
    private sealed class ThrowingEventStoreGatewayClient : IEventStoreGatewayClient
    {
        public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("event store unavailable");

        public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingAiAssistanceProvider : IAiAssistanceProvider
    {
        public int ExecuteCount { get; private set; }

        public AiAssistanceProviderRequest? LastRequest { get; private set; }

        public ValueTask<Hexalith.ChatBot.Contracts.Queries.LowRiskAiAssistanceExecutionRecord> ExecuteAsync(
            AiAssistanceProviderRequest request,
            CancellationToken cancellationToken)
        {
            ExecuteCount++;
            LastRequest = request;
            return ValueTask.FromResult(new Hexalith.ChatBot.Contracts.Queries.LowRiskAiAssistanceExecutionRecord(
                request.ExecutionId,
                request.ProposalId,
                request.AssistanceKind,
                "success",
                "deterministic-test",
                "test-model-v1",
                FixedClock.FixedUtcNow,
                request.SourceEvidenceReferences,
                request.ContextPackageId,
                request.ContextPackageVersion,
                request.ContextRedactionState,
                request.PolicySnapshotId,
                request.PolicyReasonCode,
                request.AuditOperationId,
                "available",
                request.CorrelationId,
                "metadata_only",
                "metadata_only",
                "none"));
        }
    }

    private sealed class RecordingConversationWriter : IConversationWriter
    {
        public int PrepareCount { get; private set; }

        public ApprovedAiConversationAppendRequest? LastRequest { get; private set; }

        public ValueTask<ConversationAppendResult> PrepareAppendConversationMessageAsync(
            ApprovedAiConversationAppendRequest request,
            CancellationToken cancellationToken)
        {
            PrepareCount++;
            LastRequest = request;
            return ValueTask.FromResult(new ConversationAppendResult(
                "success",
                "available",
                "metadata_only",
                "none"));
        }
    }

    private sealed class SpyOutboundMailboxSender : Hexalith.ChatBot.Server.Adapters.Mailbox.IOutboundMailboxSender
    {
        public int SendCount { get; private set; }

        public ValueTask<Hexalith.ChatBot.Server.Adapters.Mailbox.OutboundMailboxSendResult> SendAsync(
            Hexalith.ChatBot.Server.Adapters.Mailbox.OutboundMailboxSendRequest request,
            CancellationToken cancellationToken = default)
        {
            SendCount++;
            return ValueTask.FromResult(Hexalith.ChatBot.Server.Adapters.Mailbox.OutboundMailboxSendResult.Sent("adapter:mailbox-outbound"));
        }
    }

    private sealed class FakeOutboundChannelControlStateProvider : IOutboundChannelControlStateProvider
    {
        private readonly HashSet<string> _disabled = new(StringComparer.Ordinal);
        private readonly HashSet<string> _quarantined = new(StringComparer.Ordinal);

        public List<(string TenantId, string OutboundChannelRef)> ObservedRequests { get; } = [];

        public void Disable(string tenantId, string outboundChannelRef)
            => _disabled.Add($"{tenantId}|{outboundChannelRef}");

        public void Quarantine(string tenantId, string outboundChannelRef)
            => _quarantined.Add($"{tenantId}|{outboundChannelRef}");

        public ValueTask<Hexalith.ChatBot.Contracts.Enums.OutboundChannelControlState> GetControlStateAsync(
            string tenantId,
            string outboundChannelRef,
            CancellationToken cancellationToken)
        {
            ObservedRequests.Add((tenantId, outboundChannelRef));
            string key = $"{tenantId}|{outboundChannelRef}";
            if (_quarantined.Contains(key))
            {
                return ValueTask.FromResult(Hexalith.ChatBot.Contracts.Enums.OutboundChannelControlState.Quarantined);
            }

            return ValueTask.FromResult(_disabled.Contains(key)
                ? Hexalith.ChatBot.Contracts.Enums.OutboundChannelControlState.Disabled
                : Hexalith.ChatBot.Contracts.Enums.OutboundChannelControlState.Active);
        }
    }

    private sealed class FakeOutboundChannelRateLimitProvider : IOutboundChannelRateLimitProvider
    {
        private readonly Dictionary<string, OutboundChannelRateLimitState> _budgets = new(StringComparer.Ordinal);

        public List<(string TenantId, string OutboundChannelRef)> ObservedRequests { get; } = [];

        public void Configure(string tenantId, string outboundChannelRef, int budget)
            => _budgets[$"{tenantId}|{outboundChannelRef}"] =
                new OutboundChannelRateLimitState(budget, OutboundChannelRateLimitWindow.RollingHour);

        public ValueTask<OutboundChannelRateLimitState?> GetRateLimitAsync(
            string tenantId,
            string outboundChannelRef,
            CancellationToken cancellationToken)
        {
            ObservedRequests.Add((tenantId, outboundChannelRef));
            return ValueTask.FromResult(_budgets.TryGetValue($"{tenantId}|{outboundChannelRef}", out OutboundChannelRateLimitState? state)
                ? state
                : null);
        }
    }

    private sealed class FakeOutboundChannelSendHistory : IOutboundChannelSendHistory
    {
        private readonly Dictionary<string, IReadOnlyList<DateTimeOffset>> _history = new(StringComparer.Ordinal);

        public List<(string TenantId, string OutboundChannelRef)> ObservedRequests { get; } = [];

        public void Seed(string tenantId, string outboundChannelRef, params DateTimeOffset[] timestamps)
            => _history[$"{tenantId}|{outboundChannelRef}"] = timestamps;

        public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentSendsAsync(
            string tenantId,
            string outboundChannelRef,
            CancellationToken cancellationToken)
        {
            ObservedRequests.Add((tenantId, outboundChannelRef));
            return ValueTask.FromResult(_history.TryGetValue($"{tenantId}|{outboundChannelRef}", out IReadOnlyList<DateTimeOffset>? timestamps)
                ? timestamps
                : []);
        }
    }
}
