using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Adapters.AiProvider;
using Hexalith.ChatBot.Server.Adapters.Conversations;
using Hexalith.ChatBot.Server.Adapters.Parties;
using Hexalith.ChatBot.Server.Adapters.Projects;
using Hexalith.ChatBot.Server.Association.Scoring;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.Outbound;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using ContractApproveCommandCapabilityDisable = Hexalith.ChatBot.Contracts.Commands.ApproveCommandCapabilityDisable;
using ContractApproveMailboxSourceDisable = Hexalith.ChatBot.Contracts.Commands.ApproveMailboxSourceDisable;
using ContractApproveMailboxSourceQuarantine = Hexalith.ChatBot.Contracts.Commands.ApproveMailboxSourceQuarantine;
using ContractCommandCapabilityControlState = Hexalith.ChatBot.Contracts.Enums.CommandCapabilityControlState;
using ContractMailboxSourceControlState = Hexalith.ChatBot.Contracts.Enums.MailboxSourceControlState;
using ContractApproveAiActorQuarantine = Hexalith.ChatBot.Contracts.Commands.ApproveAiActorQuarantine;
using ContractAiActorControlState = Hexalith.ChatBot.Contracts.Enums.AiActorControlState;
using ContractApproveServiceClientDisable = Hexalith.ChatBot.Contracts.Commands.ApproveServiceClientDisable;
using ContractApproveServiceClientQuarantine = Hexalith.ChatBot.Contracts.Commands.ApproveServiceClientQuarantine;
using ContractServiceClientControlState = Hexalith.ChatBot.Contracts.Enums.ServiceClientControlState;
using ContractSubmitAiActorRateLimit = Hexalith.ChatBot.Contracts.Commands.SubmitAiActorRateLimit;
using ContractSubmitAiActorQuarantine = Hexalith.ChatBot.Contracts.Commands.SubmitAiActorQuarantine;
using ContractSubmitCommandCapabilityDisable = Hexalith.ChatBot.Contracts.Commands.SubmitCommandCapabilityDisable;
using ContractSubmitCommandCapabilityRateLimit = Hexalith.ChatBot.Contracts.Commands.SubmitCommandCapabilityRateLimit;
using ContractSubmitOutboundChannelRateLimit = Hexalith.ChatBot.Contracts.Commands.SubmitOutboundChannelRateLimit;
using ContractSubmitServiceClientDisable = Hexalith.ChatBot.Contracts.Commands.SubmitServiceClientDisable;
using ContractSubmitServiceClientQuarantine = Hexalith.ChatBot.Contracts.Commands.SubmitServiceClientQuarantine;
using ContractSubmitMailboxSourceQuarantine = Hexalith.ChatBot.Contracts.Commands.SubmitMailboxSourceQuarantine;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed class CommandGatewayAdmissionApiE2ETests
{
    [Fact]
    public async Task CommandGatewayApi_ShouldResolveMailboxParticipantsBeforeEventStoreSubmission()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        RecordingParticipantDirectory directory = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = ParticipantResolutionGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            directory,
            idempotencyStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(ParticipantResolutionSubmissionRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        directory.Lookups.Select(static lookup => lookup.SourceParticipantId).ShouldBe(
            ["01ARZ3NDEKTSV4RRFFQ69G5FAZ", "01ARZ3NDEKTSV4RRFFQ69G5FBA"]);
        directory.Lookups.ShouldAllBe(static lookup => lookup.TenantId == "tenant-alpha");

        SubmitCommandRequest submitted = eventStore.Submitted.ShouldHaveSingleItem();
        submitted.Tenant.ShouldBe("tenant-alpha");
        submitted.Domain.ShouldBe("chatbot");
        submitted.AggregateId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        submitted.CommandType.ShouldBe(nameof(ResolveMailboxMessageParticipants));
        submitted.Extensions.ShouldNotBeNull();
        submitted.Extensions["surfaceOrigin"].ShouldBe("mailbox");

        JsonElement payload = submitted.Payload;
        payload.GetProperty("ResolvedParticipants").EnumerateArray().ShouldHaveSingleItem()
            .GetProperty("PartyId").GetString().ShouldBe("tenant-alpha:parties:party-001");
        JsonElement unresolved = payload.GetProperty("UnresolvedParticipants").EnumerateArray().ShouldHaveSingleItem();
        unresolved.GetProperty("Reason").GetString().ShouldBe(nameof(ParticipantResolutionBlockedReason.NotFound));
        unresolved.GetProperty("AllowedReviewActions").EnumerateArray().Select(static item => item.GetString()).ShouldBe(
            [
                nameof(ParticipantReviewAction.Link),
                nameof(ParticipantReviewAction.CreatePending),
                nameof(ParticipantReviewAction.Reject),
                nameof(ParticipantReviewAction.Quarantine),
            ]);

        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.CommandName == nameof(ResolveMailboxMessageParticipants));
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(
            CoarseIdempotencyOperationClass.ParticipantResolution.Code);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("lifecycleState").GetString().ShouldBe("Proposed");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("sender@example.test", Case.Insensitive);
        body.ShouldNotContain("unresolved@example.test", Case.Insensitive);
        body.ShouldNotContain("Sender Raw", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldScoreMailboxAssociationBeforeEventStoreSubmission()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        AssociationDeterministicSignal signal = AssociationSignal("project-001", 0.9);
        RecordingProjectDirectory directory = new(new ProjectDirectoryAssociationResult(
            true,
            [new ProjectAssociationCandidateEvidence("project-001", "Roadmap", [signal])],
            []));
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = AssociationScoringGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            directory,
            idempotencyStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(AssociationScoringSubmissionRequest(signal), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        directory.Request.ShouldNotBeNull();
        directory.Request.TenantId.ShouldBe("tenant-alpha");
        directory.Request.CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        directory.Request.SourceConversationId.ShouldBe("conversation-001");
        directory.Request.SourceThreadId.ShouldBe("thread-001");

        SubmitCommandRequest submitted = eventStore.Submitted.ShouldHaveSingleItem();
        submitted.Tenant.ShouldBe("tenant-alpha");
        submitted.Domain.ShouldBe("chatbot");
        submitted.AggregateId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAB");
        submitted.CommandType.ShouldBe(nameof(ScoreMailboxMessageAssociation));
        submitted.Extensions.ShouldNotBeNull();
        submitted.Extensions["surfaceOrigin"].ShouldBe("mailbox");

        JsonElement payload = submitted.Payload;
        payload.TryGetProperty("Result", out JsonElement result).ShouldBeTrue();
        result.GetProperty("Outcome").GetString().ShouldBe("auto-associated");
        result.GetProperty("ThresholdBand").GetString().ShouldBe("auto");
        result.GetProperty("ConfidenceScore").GetDouble().ShouldBe(0.9);
        result.GetProperty("CorrelationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        result.GetProperty("RedactionState").GetString().ShouldBe("metadata_only");
        payload.GetProperty("ThresholdPolicy").GetProperty("PolicyVersion").GetString().ShouldBe("association-thresholds.m0.default.v1");

        JsonElement candidate = payload.GetProperty("Candidates").EnumerateArray().ShouldHaveSingleItem();
        candidate.GetProperty("ProjectId").GetString().ShouldBe("project-001");
        candidate.GetProperty("Rank").GetInt32().ShouldBe(1);
        candidate.GetProperty("RequiredEvidenceComplete").GetBoolean().ShouldBeTrue();
        candidate.GetProperty("EvidenceRefs").EnumerateArray().ShouldHaveSingleItem()
            .GetProperty("EvidenceFingerprint").GetString().ShouldBe("hash-project");
        payload.TryGetProperty("result", out _).ShouldBeFalse();

        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.CommandName == nameof(ScoreMailboxMessageAssociation));
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(
            CoarseIdempotencyOperationClass.AssociationScoring.Code);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("Roadmap", Case.Insensitive);
        body.ShouldNotContain("project-001", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldFailClosedWhenAssociationAuthorizationEvidenceIsUnavailable()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        AssociationDeterministicSignal signal = AssociationSignal("project-001", 0.9);
        RecordingProjectDirectory directory = new(ProjectDirectoryAssociationResult.Unavailable(
            [
                new AssociationExclusion(
                    "suppressed",
                    AssociationExclusionState.Unavailable,
                    AssociationReasonCode.AuthorizationEvidenceUnavailable,
                    "suppressed",
                    "suppressed"),
            ]));
        using WebApplicationFactory<Program> factory = AssociationScoringGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            directory,
            new InMemoryCoarseIdempotencyStore(new SystemClock()));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(AssociationScoringSubmissionRequest(signal), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        SubmitCommandRequest submitted = eventStore.Submitted.ShouldHaveSingleItem();
        JsonElement payload = submitted.Payload;
        payload.GetProperty("Candidates").EnumerateArray().ShouldBeEmpty();
        JsonElement exclusion = payload.GetProperty("Exclusions").EnumerateArray().ShouldHaveSingleItem();
        exclusion.GetProperty("ProjectId").GetString().ShouldBe("suppressed");
        exclusion.GetProperty("ReasonCode").GetString().ShouldBe("authorization-evidence-unavailable");

        JsonElement result = payload.GetProperty("Result");
        result.GetProperty("Outcome").GetString().ShouldBe("failed-closed");
        result.GetProperty("ThresholdBand").GetString().ShouldBe("fail-closed");
        result.GetProperty("ConfidenceScore").GetDouble().ShouldBe(0.0);
        result.GetProperty("ReasonCodes").EnumerateArray().ShouldHaveSingleItem().GetString()
            .ShouldBe("authorization-evidence-unavailable");

        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);

        string acceptedBody = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        acceptedBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
        acceptedBody.ShouldNotContain("project-001", Case.Insensitive);
        acceptedBody.ShouldNotContain("Secret Project", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptAssociationCorrectionThroughUiSpineAndForwardMetadataOnlyPayload()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        RecordingWorkflowRuntime workflowRuntime = new();
        using WebApplicationFactory<Program> factory = AssociationCorrectionGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            idempotencyStore,
            AssociationCorrectionDependencyReadinessStatus.Ready,
            workflowRuntime);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(AssociationCorrectionSubmissionRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        eventStore.Submitted.Count.ShouldBe(1);
        SubmitCommandRequest submitted = eventStore.Submitted[0];
        submitted.Tenant.ShouldBe("tenant-alpha");
        submitted.Domain.ShouldBe("chatbot");
        submitted.AggregateId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        submitted.CommandType.ShouldBe(nameof(CorrectEmailProjectAssociation));
        submitted.Extensions.ShouldNotBeNull();
        submitted.Extensions["surfaceOrigin"].ShouldBe("ui");
        submitted.Extensions["taskId"].ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");

        JsonElement payload = submitted.Payload;
        payload.GetProperty("AssociationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        payload.GetProperty("IntakeId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FBZ");
        payload.GetProperty("PriorProjectId").GetString().ShouldBe("project-alpha");
        payload.GetProperty("TargetProjectId").GetString().ShouldBe("project-beta");
        payload.GetProperty("CorrectionKind").GetString().ShouldBe("project-reassignment");
        payload.GetProperty("CorrectionRationale").GetString().ShouldBe("Safe metadata-only correction rationale.");
        payload.GetProperty("PredecessorAssociationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAA");
        payload.GetProperty("CandidateEvidenceFingerprint").GetString().ShouldBe("evidence:subject-match:sha256");
        payload.GetProperty("SourceVersion").GetInt64().ShouldBe(9);
        payload.GetProperty("SchemaVersion").GetString().ShouldBe("chatbot.association-correction-command.v1");
        payload.TryGetProperty("associationId", out _).ShouldBeFalse();
        payload.TryGetProperty("actorId", out _).ShouldBeFalse();
        payload.TryGetProperty("tenantId", out _).ShouldBeFalse();
        CorrectionPropagationRequest scheduled = workflowRuntime.Scheduled.ShouldHaveSingleItem();
        scheduled.WorkflowInstanceId.ShouldBe(
            DaprCorrectionPropagationCoordinator.WorkflowInstanceIdFor(
                "tenant-alpha",
                "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                DaprCorrectionPropagationCoordinator.CorrectionIdFor("01ARZ3NDEKTSV4RRFFQ69G5FAV", 10),
                10));
        scheduled.CorrectedProjectId.ShouldBe("project-beta");
        scheduled.CorrectedCaseId.ShouldBeEmpty();
        foreach (string submittedPayload in eventStore.Submitted.Select(static request => request.Payload.GetRawText()))
        {
            submittedPayload.ShouldNotContain("sender@example.test", Case.Insensitive);
            submittedPayload.ShouldNotContain("rawBody", Case.Insensitive);
            submittedPayload.ShouldNotContain("Authorization:", Case.Insensitive);
            submittedPayload.ShouldNotContain("Bearer ", Case.Insensitive);
        }

        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.CommandName == nameof(CorrectEmailProjectAssociation));
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(
            CoarseIdempotencyOperationClass.Correction.Code);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("lifecycleState").GetString().ShouldBe("Proposed");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("project-alpha", Case.Insensitive);
        body.ShouldNotContain("project-beta", Case.Insensitive);
        body.ShouldNotContain("Safe metadata-only correction rationale.", Case.Insensitive);
        body.ShouldNotContain("rawBody", Case.Insensitive);
        body.ShouldNotContain("sender@example.test", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldFailClosedWhenAssociationCorrectionProjectionDependencyIsUnavailable()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = AssociationCorrectionGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            idempotencyStore,
            new AssociationCorrectionDependencyReadinessStatus(
                IsWorkflowRuntimeReady: true,
                IsProjectionInvalidationReady: false,
                IsAuditWriterReady: true,
                IsIdempotencyStoreReady: true));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(AssociationCorrectionSubmissionRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        eventStore.Submitted.ShouldBeEmpty();
        idempotencyStore.RecordCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        auditWriter.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(
            ChatBotAuthorizationReasonCodes.AssociationCorrectionProjectionUnavailable);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authorization_denied");
        root.GetProperty("code").GetString().ShouldBe(ChatBotMessageCodes.AssociationCorrectionProjectionUnavailable);
        root.GetProperty("clientAction").GetString().ShouldBe(ChatBotMessageNextActions.RetryLater);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("project-alpha", Case.Insensitive);
        body.ShouldNotContain("project-beta", Case.Insensitive);
        body.ShouldNotContain("Safe metadata-only correction rationale.", Case.Insensitive);
        body.ShouldNotContain("System.InvalidOperationException", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldFailClosedWhenAssociationCorrectionWorkflowRuntimeIsUnavailable()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        RecordingWorkflowRuntime workflowRuntime = new();
        using WebApplicationFactory<Program> factory = AssociationCorrectionGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            idempotencyStore,
            new AssociationCorrectionDependencyReadinessStatus(
                IsWorkflowRuntimeReady: false,
                IsProjectionInvalidationReady: true,
                IsAuditWriterReady: true,
                IsIdempotencyStoreReady: true),
            workflowRuntime);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(AssociationCorrectionSubmissionRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        eventStore.Submitted.ShouldBeEmpty();
        workflowRuntime.Scheduled.ShouldBeEmpty();
        idempotencyStore.RecordCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        auditWriter.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(
            ChatBotAuthorizationReasonCodes.AssociationCorrectionWorkflowUnavailable);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authorization_denied");
        root.GetProperty("code").GetString().ShouldBe(ChatBotMessageCodes.AssociationCorrectionWorkflowUnavailable);
        root.GetProperty("clientAction").GetString().ShouldBe(ChatBotMessageNextActions.RetryLater);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("project-alpha", Case.Insensitive);
        body.ShouldNotContain("project-beta", Case.Insensitive);
        body.ShouldNotContain("Safe metadata-only correction rationale.", Case.Insensitive);
        body.ShouldNotContain("System.InvalidOperationException", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldClassifyAiActionProposalBeforeEventStoreSubmission()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = AiActionProposalGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            idempotencyStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(AiActionProposalSubmissionRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        SubmitCommandRequest submitted = eventStore.Submitted.ShouldHaveSingleItem();
        submitted.Tenant.ShouldBe("tenant-alpha");
        submitted.Domain.ShouldBe("chatbot");
        submitted.AggregateId.ShouldBe("project-001");
        submitted.CommandType.ShouldBe(nameof(ProposeAIAction));

        JsonElement payload = submitted.Payload;
        payload.GetProperty("ProjectId").GetString().ShouldBe("project-001");
        JsonElement risk = payload.GetProperty("RiskClassification");
        risk.GetProperty("RiskClass").GetString().ShouldBe("approval-required");
        risk.GetProperty("RiskActionClasses").EnumerateArray().Select(static value => value.GetString()).ShouldBe(
            [
                "modifies-state",
                "exposes-files",
                "sends-external",
                "creates-tasks",
                "invokes-tools",
                "acts-on-behalf",
            ],
            ignoreOrder: false);
        risk.GetProperty("ClassifierVersion").GetString().ShouldBe("chatbot.ai-action-risk-classifier.m0.v1");
        risk.GetProperty("ReasonCode").GetString().ShouldBe("risky_action_class");
        risk.GetProperty("RequesterAuthorityClass").GetString().ShouldBe("project-contributor");
        risk.GetProperty("CommandAllowlistVersion").GetString().ShouldBe("ai-action-command-allowlist.m0");
        risk.GetProperty("InputTuple").GetProperty("TenantPolicyClassification").GetString().ShouldBe("approval-required");

        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.CommandName == nameof(ProposeAIAction));
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SourceEvidenceRefs.Contains("risk-class:approval-required"));
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SourceEvidenceRefs.Contains("reason:risky_action_class"));
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SourceEvidenceRefs.Contains("risk-action:modifies-state"));
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SourceEvidenceRefs.Contains("risk-action:acts-on-behalf"));
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(
            CoarseIdempotencyOperationClass.CommandExecution.Code);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("project-001", Case.Insensitive);
        body.ShouldNotContain("raw prompt", Case.Insensitive);
        body.ShouldNotContain("provider payload", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAdmitAllowedLowRiskAiAssistanceWithoutStartingProvider()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        RecordingAiAssistanceProvider provider = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = LowRiskAiExecutionGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            idempotencyStore,
            provider,
            lowRiskAllowed: true);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(LowRiskAiExecutionSubmissionRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        provider.ExecuteCount.ShouldBe(0);
        provider.LastRequest.ShouldBeNull();

        SubmitCommandRequest submitted = eventStore.Submitted.ShouldHaveSingleItem();
        submitted.Tenant.ShouldBe("tenant-alpha");
        submitted.Domain.ShouldBe("chatbot");
        submitted.AggregateId.ShouldBe("project-001");
        submitted.CommandType.ShouldBe(nameof(ExecuteLowRiskAIAssistance));
        JsonElement payload = submitted.Payload;
        payload.GetProperty("ExecutionRecord").ValueKind.ShouldBe(JsonValueKind.Null);
        payload.GetProperty("RiskClassification").GetProperty("RiskClass").GetString().ShouldBe("low-risk");
        payload.GetRawText().ShouldNotContain("prompt", Case.Insensitive);
        payload.GetRawText().ShouldNotContain("completion", Case.Insensitive);
        payload.GetRawText().ShouldNotContain("raw provider payload", Case.Insensitive);
        payload.GetRawText().ShouldNotContain("/home/administrator", Case.Insensitive);

        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope =>
            envelope.SourceEvidenceRefs.Contains("low-risk-policy-reason:low-risk-execute-allowed") &&
            envelope.SourceEvidenceRefs.Contains("context-package:context-package-001") &&
            envelope.SourceEvidenceRefs.Contains("execution:ai-execution-001"));
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(
            CoarseIdempotencyOperationClass.LowRiskAiAssistance.Code);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        accepted.RootElement.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        accepted.RootElement.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        accepted.RootElement.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("provider payload", Case.Insensitive);
        body.ShouldNotContain("prompt", Case.Insensitive);
        body.ShouldNotContain("completion", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldRoutePolicyFalseLowRiskAiAssistanceToApprovalWithoutProviderCall()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        RecordingAiAssistanceProvider provider = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = LowRiskAiExecutionGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            idempotencyStore,
            provider,
            lowRiskAllowed: false);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(LowRiskAiExecutionSubmissionRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        provider.ExecuteCount.ShouldBe(0);
        SubmitCommandRequest submitted = eventStore.Submitted.ShouldHaveSingleItem();
        submitted.CommandType.ShouldBe(nameof(ExecuteLowRiskAIAssistance));
        JsonElement record = submitted.Payload.GetProperty("ExecutionRecord");
        record.GetProperty("Outcome").GetString().ShouldBe("pending-approval");
        record.GetProperty("ProviderName").GetString().ShouldBe("not-invoked");
        record.GetProperty("PolicyReasonCode").GetString().ShouldBe("low_risk_policy_false");
        record.GetProperty("SafeNextAction").GetString().ShouldBe("review-ai-action");
        submitted.Payload.GetRawText().ShouldNotContain("provider payload", Case.Insensitive);
        submitted.Payload.GetRawText().ShouldNotContain("prompt", Case.Insensitive);
        submitted.Payload.GetRawText().ShouldNotContain("completion", Case.Insensitive);

        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope =>
            envelope.SourceEvidenceRefs.Contains("low-risk-policy-reason:low_risk_policy_false") &&
            envelope.SourceEvidenceRefs.Contains("context-package:context-package-001") &&
            envelope.SourceEvidenceRefs.Contains("execution:ai-execution-001"));
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(
            CoarseIdempotencyOperationClass.LowRiskAiAssistance.Code);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("provider payload", Case.Insensitive);
        body.ShouldNotContain("raw-body", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldExecuteApprovedAiActionThroughAllowlistedConversationAppend()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        RecordingConversationWriter conversationWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = ApprovedAiExecutionGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            idempotencyStore,
            conversationWriter);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(ApprovedAiExecutionSubmissionRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        conversationWriter.PrepareCount.ShouldBe(1);
        conversationWriter.LastRequest.ShouldNotBeNull();
        conversationWriter.LastRequest.TenantId.ShouldBe("tenant-alpha");
        conversationWriter.LastRequest.ProjectId.ShouldBe("project-001");
        conversationWriter.LastRequest.CommandName.ShouldBe("Project.AppendConversationMessage");
        conversationWriter.LastRequest.CommandAllowlistVersion.ShouldBe("ai-action-command-allowlist.m0");
        conversationWriter.LastRequest.ApprovalId.ShouldBe("approval:ai-proposal-001");
        conversationWriter.LastRequest.ProposalId.ShouldBe("ai-proposal-001");

        SubmitCommandRequest submitted = eventStore.Submitted.ShouldHaveSingleItem();
        submitted.Tenant.ShouldBe("tenant-alpha");
        submitted.Domain.ShouldBe("chatbot");
        submitted.AggregateId.ShouldBe("project-001");
        submitted.CommandType.ShouldBe(nameof(ExecuteApprovedAIAction));
        submitted.Extensions.ShouldNotBeNull();
        submitted.Extensions["surfaceOrigin"].ShouldBe("ui");

        JsonElement payload = submitted.Payload;
        payload.GetProperty("CommandName").GetString().ShouldBe("Project.AppendConversationMessage");
        payload.GetProperty("CommandAllowlistVersion").GetString().ShouldBe("ai-action-command-allowlist.m0");
        JsonElement record = payload.GetProperty("ExecutionRecord");
        record.GetProperty("Outcome").GetString().ShouldBe("success");
        record.GetProperty("CommandName").GetString().ShouldBe("Project.AppendConversationMessage");
        record.GetProperty("CommandAllowlistVersion").GetString().ShouldBe("ai-action-command-allowlist.m0");
        record.GetProperty("ApprovalId").GetString().ShouldBe("approval:ai-proposal-001");
        record.GetProperty("ProposalId").GetString().ShouldBe("ai-proposal-001");
        record.GetProperty("AuditStatus").GetString().ShouldBe("available");
        record.GetProperty("GeneratedContentVisibility").GetString().ShouldBe("metadata_only");
        record.GetProperty("SafeNextAction").GetString().ShouldBe("none");
        payload.GetRawText().ShouldNotContain("raw prompt", Case.Insensitive);
        payload.GetRawText().ShouldNotContain("provider payload", Case.Insensitive);
        payload.GetRawText().ShouldNotContain("raw-body", Case.Insensitive);

        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.CommandName == nameof(ExecuteApprovedAIAction));
        auditWriter.Envelopes.ShouldAllBe(static envelope =>
            envelope.SourceEvidenceRefs.Contains("approved-ai-command:Project.AppendConversationMessage") &&
            envelope.SourceEvidenceRefs.Contains("ai-action-command-allowlist:ai-action-command-allowlist.m0") &&
            envelope.SourceEvidenceRefs.Contains("approval:approval:ai-proposal-001") &&
            envelope.SourceEvidenceRefs.Contains("proposal:ai-proposal-001"));
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(
            CoarseIdempotencyOperationClass.ApprovedAiActionExecution.Code);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        accepted.RootElement.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        accepted.RootElement.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        accepted.RootElement.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("raw prompt", Case.Insensitive);
        body.ShouldNotContain("provider payload", Case.Insensitive);
        body.ShouldNotContain("raw-body", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldExecuteApprovedAiActionForV1OnlyCommandWhenPinnedToV1()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        RecordingConversationWriter conversationWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = ApprovedAiExecutionGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            idempotencyStore,
            conversationWriter);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                ApprovedAiExecutionSubmissionRequest(
                    AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName,
                    AiActionCommandMetadataProvider.V1AllowlistVersion),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        conversationWriter.PrepareCount.ShouldBe(1);
        conversationWriter.LastRequest.ShouldNotBeNull();
        conversationWriter.LastRequest.CommandName.ShouldBe(AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName);
        conversationWriter.LastRequest.CommandAllowlistVersion.ShouldBe(AiActionCommandMetadataProvider.V1AllowlistVersion);

        SubmitCommandRequest submitted = eventStore.Submitted.ShouldHaveSingleItem();
        submitted.CommandType.ShouldBe(nameof(ExecuteApprovedAIAction));
        JsonElement payload = submitted.Payload;
        payload.GetProperty("CommandName").GetString().ShouldBe(AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName);
        payload.GetProperty("CommandAllowlistVersion").GetString().ShouldBe(AiActionCommandMetadataProvider.V1AllowlistVersion);
        JsonElement record = payload.GetProperty("ExecutionRecord");
        record.GetProperty("CommandName").GetString().ShouldBe(AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName);
        record.GetProperty("CommandAllowlistVersion").GetString().ShouldBe(AiActionCommandMetadataProvider.V1AllowlistVersion);
        record.GetProperty("Outcome").GetString().ShouldBe("success");

        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope =>
            envelope.SourceEvidenceRefs.Contains($"approved-ai-command:{AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName}") &&
            envelope.SourceEvidenceRefs.Contains($"ai-action-command-allowlist:{AiActionCommandMetadataProvider.V1AllowlistVersion}"));
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(
            CoarseIdempotencyOperationClass.ApprovedAiActionExecution.Code);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("raw prompt", Case.Insensitive);
        body.ShouldNotContain("provider payload", Case.Insensitive);
        body.ShouldNotContain("raw-body", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldFailClosedApprovedAiActionForNonAllowlistedCommandBeforeMutation()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        RecordingConversationWriter conversationWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = ApprovedAiExecutionGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            idempotencyStore,
            conversationWriter);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(ApprovedAiExecutionSubmissionRequest("Project.SendEmail"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        conversationWriter.PrepareCount.ShouldBe(0);
        eventStore.Submitted.ShouldBeEmpty();
        auditWriter.Envelopes.ShouldBeEmpty();
        idempotencyStore.RecordCount.ShouldBe(0);
        idempotencyStore.Records.ShouldBeEmpty();

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authorization_denied");
        root.GetProperty("code").GetString().ShouldBe(ChatBotMessageCodes.RefusalBlockedAction);
        root.GetProperty("clientAction").GetString().ShouldBe(ChatBotMessageNextActions.Escalate);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("Project.SendEmail", Case.Insensitive);
        body.ShouldNotContain("raw prompt", Case.Insensitive);
        body.ShouldNotContain("provider payload", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldCreateOutboundDraftThroughSpineWithoutExternalSend()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = OutboundDraftGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            idempotencyStore,
            OutboundDraftAuthorityClaims());
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(OutboundDraftSubmissionRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        SubmitCommandRequest submitted = eventStore.Submitted.ShouldHaveSingleItem();
        submitted.Tenant.ShouldBe("tenant-alpha");
        submitted.Domain.ShouldBe("chatbot");
        submitted.AggregateId.ShouldBe("draft-001");
        submitted.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.CreateOutboundDraft));
        submitted.Payload.GetProperty("DraftId").GetString().ShouldBe("draft-001");
        submitted.Payload.GetProperty("ProjectId").GetString().ShouldBe("project-001");
        submitted.Payload.GetProperty("SenderAuthorityClass").GetString().ShouldBe("draft-only");
        submitted.Payload.GetProperty("HasM365SendPosture").GetBoolean().ShouldBeFalse();
        submitted.Payload.TryGetProperty("AdapterMode", out _).ShouldBeFalse();
        submitted.Payload.TryGetProperty("ProviderPayload", out _).ShouldBeFalse();

        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(
            CoarseIdempotencyOperationClass.OutboundDraftCreation.Code);
        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope =>
            envelope.CommandName == nameof(Hexalith.ChatBot.Contracts.Commands.CreateOutboundDraft) &&
            envelope.SourceEvidenceRefs.Contains("outbound-draft:draft-001") &&
            envelope.SourceEvidenceRefs.Contains("sender-authority:draft-only") &&
            envelope.SourceEvidenceRefs.Contains("requester:actor-alpha") &&
            envelope.SourceEvidenceRefs.Contains("project:project-001") &&
            envelope.SourceEvidenceRefs.Contains("policy-snapshot:policy-snap-001") &&
            envelope.SourceEvidenceRefs.Contains("recipient:party-001"));
        JsonSerializer.Serialize(auditWriter.Envelopes, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            .ShouldNotContain("Governed draft content.", Case.Insensitive);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("lifecycleState").GetString().ShouldBe("Proposed");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("project-001", Case.Insensitive);
        body.ShouldNotContain("recipient:party-001", Case.Insensitive);
        body.ShouldNotContain("Governed draft content.", Case.Insensitive);
        body.ShouldNotContain("Graph", Case.Insensitive);
        body.ShouldNotContain("SMTP", Case.Insensitive);
    }

    [Theory]
    [InlineData("missing-project-authority", false, true, true, false, ChatBotDisabledActionReasons.InsufficientAuthority)]
    [InlineData("missing-outbound-draft-scope", true, false, true, false, ChatBotDisabledActionReasons.InsufficientAuthority)]
    [InlineData("m365-send-posture-present", true, true, true, true, ChatBotDisabledActionReasons.PolicyBlocked)]
    [InlineData("tenant-policy-disables-draft-only", true, true, false, false, ChatBotDisabledActionReasons.PolicyBlocked)]
    public async Task CommandGatewayApi_ShouldDenyOutboundDraftAuthorityGapsBeforeDurableMutation(
        string caseName,
        bool includeProjectAuthority,
        bool includeOutboundDraftScope,
        bool includeTenantPolicy,
        bool hasM365SendPosture,
        string expectedAuditReason)
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = OutboundDraftGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            idempotencyStore,
            OutboundDraftAuthorityClaims(includeProjectAuthority, includeOutboundDraftScope, includeTenantPolicy));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                OutboundDraftSubmissionRequest(
                    OutboundDraftCommand() with { HasM365SendPosture = hasM365SendPosture }),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, caseName);
        eventStore.Submitted.ShouldBeEmpty();
        idempotencyStore.RecordCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        auditWriter.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(expectedAuditReason);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authorization_denied");
        root.GetProperty("code").GetString().ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        root.GetProperty("clientAction").GetString().ShouldBe(ChatBotMessageNextActions.RequestAccess);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("project-001", Case.Insensitive);
        body.ShouldNotContain("recipient:party-001", Case.Insensitive);
        body.ShouldNotContain("Governed draft content.", Case.Insensitive);
        body.ShouldNotContain("policy-snap-001", Case.Insensitive);
        body.ShouldNotContain("m365", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldReplayEquivalentOutboundDraftAndRejectConflictingDuplicate()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = OutboundDraftGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            idempotencyStore,
            OutboundDraftAuthorityClaims());
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client
            .SendAsync(OutboundDraftSubmissionRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string firstBody = await first.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage replay = await client
            .SendAsync(OutboundDraftSubmissionRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string replayBody = await replay.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage conflict = await client
            .SendAsync(
                OutboundDraftSubmissionRequest(
                    OutboundDraftCommand() with
                    {
                        GovernedContent = new Hexalith.ChatBot.Contracts.Commands.OutboundDraftContent(
                            "Changed status",
                            "Changed governed draft content with sender@example.test and Project Alpha.",
                            "text/plain"),
                    },
                    commandId: "01ARZ3NDEKTSV4RRFFQ69G5FBY"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string conflictBody = await conflict.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        replay.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        replayBody.ShouldBe(firstBody);
        conflict.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        eventStore.Submitted.Count.ShouldBe(1);
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(
            CoarseIdempotencyOperationClass.OutboundDraftCreation.Code);
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);

        using JsonDocument problem = JsonDocument.Parse(conflictBody);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("conflict");
        root.GetProperty("code").GetString().ShouldBe(ChatBotMessageCodes.IdempotencyConflictOutboundDraftCreation);
        root.GetProperty("retryable").GetBoolean().ShouldBeFalse();
        root.GetProperty("clientAction").GetString().ShouldBe(ChatBotMessageNextActions.None);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        conflictBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
        conflictBody.ShouldNotContain("project-001", Case.Insensitive);
        conflictBody.ShouldNotContain("recipient:party-001", Case.Insensitive);
        conflictBody.ShouldNotContain("Changed governed draft content", Case.Insensitive);
        conflictBody.ShouldNotContain("sender@example.test", Case.Insensitive);
        conflictBody.ShouldNotContain("Project Alpha", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldPauseOutboundSendForApprovalThenSubmitApprovedSendOnceWithDefaultAdapterFailClosed()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = OutboundDraftGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            idempotencyStore,
            OutboundSendAuthorityClaims());
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage approvalRequest = await client
            .SendAsync(OutboundApprovalSubmissionRequest(OutboundApprovalRequestCommand(), "01ARZ3NDEKTSV4RRFFQ69G5FAY"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage approvalDecision = await client
            .SendAsync(OutboundApprovalSubmissionRequest(OutboundApprovalDecisionCommand(), "01ARZ3NDEKTSV4RRFFQ69G5FBY"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage send = await client
            .SendAsync(OutboundApprovalSubmissionRequest(OutboundSendCommand("send-001"), "01ARZ3NDEKTSV4RRFFQ69G5FCY"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage replay = await client
            .SendAsync(OutboundApprovalSubmissionRequest(OutboundSendCommand("send-002"), "01ARZ3NDEKTSV4RRFFQ69G5FDY"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        approvalRequest.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        approvalDecision.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        send.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        replay.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await replay.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))
            .ShouldBe(await send.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));

        eventStore.Submitted.Select(static request => request.CommandType).ShouldBe(
            [
                nameof(RequestOutboundSendApproval),
                nameof(DecideOutboundApproval),
                nameof(ExecuteApprovedOutboundDraft),
            ]);
        eventStore.Submitted.Select(static request => request.AggregateId).ShouldBe(["draft-001", "draft-001", "draft-001"]);
        eventStore.Submitted[0].Payload.GetProperty("CommandName").GetString().ShouldBe(nameof(ExecuteApprovedOutboundDraft));
        eventStore.Submitted[0].Payload.GetProperty("RecipientRefs").EnumerateArray()
            .Select(static item => item.GetString()).ShouldBe(["recipient:party-001"]);
        eventStore.Submitted[1].Payload.GetProperty("Decision").GetString().ShouldBe("approve");
        eventStore.Submitted[2].Payload.GetProperty("AdapterMode").GetString().ShouldBe("approved");
        eventStore.Submitted[2].Payload.GetProperty("AdapterStatus").GetString().ShouldBe("unavailable");

        idempotencyStore.Records.Select(static record => record.OperationClass).Order(StringComparer.Ordinal).ShouldBe(
            [
                CoarseIdempotencyOperationClass.ApprovalDecision.Code,
                CoarseIdempotencyOperationClass.CommandExecution.Code,
                CoarseIdempotencyOperationClass.OutboundSend.Code,
            ]);
        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Count.ShouldBe(6);
        auditWriter.Envelopes.ShouldContain(envelope =>
            envelope.CommandName == nameof(RequestOutboundSendApproval) &&
            envelope.SourceEvidenceRefs.Contains("approval:approval-001") &&
            envelope.SourceEvidenceRefs.Contains("outbound-draft:draft-001") &&
            envelope.SourceEvidenceRefs.Contains("requester:actor-alpha") &&
            envelope.SourceEvidenceRefs.Contains("project:project-001") &&
            envelope.SourceEvidenceRefs.Contains("policy-snapshot:policy-snap-001") &&
            envelope.SourceEvidenceRefs.Contains("recipient:party-001"));
        auditWriter.Envelopes.ShouldContain(envelope =>
            envelope.CommandName == nameof(DecideOutboundApproval) &&
            envelope.SourceEvidenceRefs.Contains("approval:approval-001") &&
            envelope.SourceEvidenceRefs.Contains("approval-decision:approve"));
        auditWriter.Envelopes.ShouldContain(envelope =>
            envelope.CommandName == nameof(ExecuteApprovedOutboundDraft) &&
            envelope.SourceEvidenceRefs.Contains("outbound-send:send-001") &&
            envelope.SourceEvidenceRefs.Contains("approval:approval-001") &&
            envelope.SourceEvidenceRefs.Contains("outbound-draft:draft-001") &&
            envelope.SourceEvidenceRefs.Contains("sender-authority:authenticated-user-send") &&
            envelope.SourceEvidenceRefs.Contains("send-actor:actor-alpha") &&
            envelope.SourceEvidenceRefs.Contains("adapter-mode:approved") &&
            envelope.SourceEvidenceRefs.Contains("recipient:party-001"));

        string publicArtifacts = JsonSerializer.Serialize(
            new { auditWriter.Envelopes, eventStore.Submitted },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        publicArtifacts.ShouldNotContain("provider payload", Case.Insensitive);
        publicArtifacts.ShouldNotContain("Graph", Case.Insensitive);
        publicArtifacts.ShouldNotContain("SMTP", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldRejectConflictingApprovedOutboundSendWithoutSecondDurableSubmission()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = OutboundDraftGatewayFactory(
            "tenant-alpha",
            eventStore,
            auditWriter,
            idempotencyStore,
            OutboundSendAuthorityClaims());
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client
            .SendAsync(OutboundApprovalSubmissionRequest(OutboundSendCommand("send-001"), "01ARZ3NDEKTSV4RRFFQ69G5FCY"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage conflict = await client
            .SendAsync(
                OutboundApprovalSubmissionRequest(
                    OutboundSendCommand("send-002") with { ApprovalId = "approval-other" },
                    "01ARZ3NDEKTSV4RRFFQ69G5FDY"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        conflict.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        eventStore.Submitted.ShouldHaveSingleItem().CommandType.ShouldBe(nameof(ExecuteApprovedOutboundDraft));
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(CoarseIdempotencyOperationClass.OutboundSend.Code);
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe([AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);

        string body = await conflict.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("conflict");
        root.GetProperty("code").GetString().ShouldBe(CoarseIdempotencyOperationClass.OutboundSend.ConflictCode);
        root.GetProperty("retryable").GetBoolean().ShouldBeFalse();
        root.GetProperty("clientAction").GetString().ShouldBe(ChatBotMessageNextActions.None);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("Governed draft content.", Case.Insensitive);
        body.ShouldNotContain("Approved governed content.", Case.Insensitive);
        body.ShouldNotContain("recipient:party-001", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldRecordMetadataOnlyDenialFactForSpineRefusalAcrossSurface()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new DenyAllSpineCommandAllowlist());
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                CommandSubmissionRequest(
                    "tenant-alpha",
                    "restricted-project-sentinel-C:\\\\secret\\\\item-/tmp/raw-exception",
                    origin: "mcp"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        idempotencyStore.RecordCount.ShouldBe(0);
        idempotencyStore.Records.ShouldBeEmpty();

        ChatBotAuthorizationFailureAuditFact fact = auditWriter.AuthorizationFailures.ShouldHaveSingleItem();
        fact.TenantId.ShouldBe("tenant-alpha");
        fact.ActorId.ShouldBe("actor-alpha");
        fact.CommandType.ShouldBe("TenantScopedCommand");
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.CommandNotAllowlisted);
        fact.CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        fact.TaskId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        fact.SurfaceOrigin.ShouldBe("mcp");

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authorization_denied");
        root.GetProperty("code").GetString().ShouldBe(ChatBotMessageCodes.RefusalBlockedAction);
        root.GetProperty("retryable").GetBoolean().ShouldBeFalse();
        root.GetProperty("clientAction").GetString().ShouldBe(ChatBotMessageNextActions.Escalate);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("restricted-project-sentinel", Case.Insensitive);
        body.ShouldNotContain("/tmp/raw-exception", Case.Insensitive);
        body.ShouldNotContain("C:\\", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptMailboxSourceDisableApprovalThroughUiSpine()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist(),
            additionalClaims:
            [
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, "mailbox-admin"),
            ]);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                MailboxSourceDisableSubmissionRequest(MailboxSourceDisableApprovalCommand()),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        dispatcher.DispatchCount.ShouldBe(1);
        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.CommandName == typeof(ContractApproveMailboxSourceDisable).Name);
        foreach (AuditEnvelope envelope in auditWriter.Envelopes)
        {
            envelope.ActorType.ShouldBe("human");
            envelope.StateTransition.ShouldBe("Active->Disabled");
            envelope.SourceEvidenceRefs.ShouldContain("admin-operation:mailbox-source-disable-approve");
            envelope.SourceEvidenceRefs.ShouldContain("admin-scope:mailbox");
            envelope.SourceEvidenceRefs.ShouldContain("mailbox-source:controlled-mailbox-001");
            envelope.SourceEvidenceRefs.ShouldContain("reason:mailbox-source-unsafe-activity");
        }

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("controlled-mailbox-001", Case.Insensitive);
        body.ShouldNotContain("@", Case.Insensitive);
        body.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptMailboxSourceQuarantineTwoPersonFlowThroughUiSpine()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist(),
            additionalClaims:
            [
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, "mailbox-admin"),
            ]);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage proposal = await client
            .SendAsync(
                MailboxSourceControlSubmissionRequest(
                    MailboxSourceQuarantineSubmitCommand(),
                    "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage approval = await client
            .SendAsync(
                MailboxSourceControlSubmissionRequest(
                    MailboxSourceQuarantineApprovalCommand(),
                    "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    "01ARZ3NDEKTSV4RRFFQ69G5FBA"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        proposal.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        approval.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        dispatcher.DispatchCount.ShouldBe(2);
        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [
                AuditCommitPhase.PreCommit,
                AuditCommitPhase.PostCommit,
                AuditCommitPhase.PreCommit,
                AuditCommitPhase.PostCommit,
            ]);
        auditWriter.Envelopes.Take(2).ShouldAllBe(static envelope =>
            envelope.CommandName == typeof(ContractSubmitMailboxSourceQuarantine).Name);
        auditWriter.Envelopes.Skip(2).ShouldAllBe(static envelope =>
            envelope.CommandName == typeof(ContractApproveMailboxSourceQuarantine).Name);
        auditWriter.Envelopes.Take(2).ShouldAllBe(static envelope =>
            envelope.StateTransition == "Received->Proposed" &&
            envelope.SourceEvidenceRefs.Contains("admin-operation:mailbox-source-quarantine") &&
            envelope.SourceEvidenceRefs.Contains("admin-scope:mailbox") &&
            envelope.SourceEvidenceRefs.Contains("mailbox-source:controlled-mailbox-001") &&
            envelope.SourceEvidenceRefs.Contains("reason:mailbox-source-unsafe-activity"));
        auditWriter.Envelopes.Skip(2).ShouldAllBe(static envelope =>
            envelope.ActorType == "human" &&
            envelope.StateTransition == "Active->Quarantined" &&
            envelope.SourceEvidenceRefs.Contains("admin-operation:mailbox-source-quarantine-approve") &&
            envelope.SourceEvidenceRefs.Contains("admin-scope:mailbox") &&
            envelope.SourceEvidenceRefs.Contains("mailbox-source:controlled-mailbox-001") &&
            envelope.SourceEvidenceRefs.Contains("reason:mailbox-source-unsafe-activity") &&
            envelope.SourceEvidenceRefs.Contains("admin-subject:admin-approver"));
        idempotencyStore.Records.Select(static record => record.OperationClass).ShouldBe(
            [CoarseIdempotencyOperationClass.CommandExecution.Code, CoarseIdempotencyOperationClass.CommandExecution.Code]);

        foreach (HttpResponseMessage response in new[] { proposal, approval })
        {
            string body = await response.Content
                .ReadAsStringAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            using JsonDocument accepted = JsonDocument.Parse(body);
            JsonElement root = accepted.RootElement;
            root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
            body.ShouldNotContain("tenant-alpha", Case.Insensitive);
            body.ShouldNotContain("controlled-mailbox-001", Case.Insensitive);
            body.ShouldNotContain("@", Case.Insensitive);
            body.ShouldNotContain("secret", Case.Insensitive);
        }
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptServiceClientDisableFlowThenFailClosedForDisabledServiceClient()
    {
        RecordingDispatcher adminDispatcher = new();
        RecordingAuditWriter adminAuditWriter = new();
        InMemoryCoarseIdempotencyStore adminIdempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> adminFactory = GatewayFactory(
            tenantId: "tenant-alpha",
            adminDispatcher,
            adminAuditWriter,
            idempotencyStore: adminIdempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist(),
            additionalClaims:
            [
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, "tenant-admin"),
            ]);
        using HttpClient adminClient = adminFactory.CreateClient();

        using HttpResponseMessage proposal = await adminClient
            .SendAsync(
                ServiceClientControlSubmissionRequest(
                    ServiceClientDisableSubmitCommand(),
                    "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage approval = await adminClient
            .SendAsync(
                ServiceClientControlSubmissionRequest(
                    ServiceClientDisableApprovalCommand(),
                    "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    "01ARZ3NDEKTSV4RRFFQ69G5FBA"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        proposal.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        approval.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        adminDispatcher.DispatchCount.ShouldBe(2);
        adminAuditWriter.AuthorizationFailures.ShouldBeEmpty();
        adminAuditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [
                AuditCommitPhase.PreCommit,
                AuditCommitPhase.PostCommit,
                AuditCommitPhase.PreCommit,
                AuditCommitPhase.PostCommit,
            ]);
        adminAuditWriter.Envelopes.Take(2).ShouldAllBe(static envelope =>
            envelope.CommandName == typeof(ContractSubmitServiceClientDisable).Name);
        adminAuditWriter.Envelopes.Skip(2).ShouldAllBe(static envelope =>
            envelope.CommandName == typeof(ContractApproveServiceClientDisable).Name);
        adminAuditWriter.Envelopes.Take(2).ShouldAllBe(static envelope =>
            envelope.StateTransition == "Received->Proposed" &&
            envelope.SourceEvidenceRefs.Contains("admin-operation:service-client-disable") &&
            envelope.SourceEvidenceRefs.Contains("admin-scope:tenant-admin") &&
            envelope.SourceEvidenceRefs.Contains("service-client:cli-automation-client") &&
            envelope.SourceEvidenceRefs.Contains("reason:service-client-unsafe-activity"));
        adminAuditWriter.Envelopes.Skip(2).ShouldAllBe(static envelope =>
            envelope.ActorType == "human" &&
            envelope.StateTransition == "Active->Disabled" &&
            envelope.SourceEvidenceRefs.Contains("admin-operation:service-client-disable-approve") &&
            envelope.SourceEvidenceRefs.Contains("admin-scope:tenant-admin") &&
            envelope.SourceEvidenceRefs.Contains("service-client:cli-automation-client") &&
            envelope.SourceEvidenceRefs.Contains("reason:service-client-unsafe-activity") &&
            envelope.SourceEvidenceRefs.Contains("admin-subject:admin-approver"));
        adminIdempotencyStore.Records.Select(static record => record.OperationClass).ShouldBe(
            [CoarseIdempotencyOperationClass.CommandExecution.Code, CoarseIdempotencyOperationClass.CommandExecution.Code]);

        foreach (HttpResponseMessage response in new[] { proposal, approval })
        {
            string body = await response.Content
                .ReadAsStringAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            using JsonDocument accepted = JsonDocument.Parse(body);
            JsonElement root = accepted.RootElement;
            root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
            body.ShouldNotContain("tenant-alpha", Case.Insensitive);
            body.ShouldNotContain("cli-automation-client", Case.Insensitive);
            body.ShouldNotContain("@", Case.Insensitive);
            body.ShouldNotContain("oauth", Case.Insensitive);
            body.ShouldNotContain("secret", Case.Insensitive);
        }

        RecordingDispatcher disabledDispatcher = new();
        RecordingAuditWriter disabledAuditWriter = new();
        InMemoryCoarseIdempotencyStore disabledIdempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> disabledFactory = GatewayFactory(
            tenantId: "tenant-alpha",
            disabledDispatcher,
            disabledAuditWriter,
            idempotencyStore: disabledIdempotencyStore,
            commandAllowlist: new AllowAllSpineCommandAllowlist(),
            serviceClientControlStateProvider: new FixedServiceClientControlStateProvider(ContractServiceClientControlState.Disabled),
            principalSubject: "service-account-cli-automation-client",
            additionalClaims: ServiceClientGrantClaims("ui", "TenantScopedCommand"));
        using HttpClient disabledClient = disabledFactory.CreateClient();

        using HttpResponseMessage disabledResponse = await disabledClient
            .SendAsync(
                CommandSubmissionRequest("tenant-alpha", "payload-sentinel-disabled-service-client", origin: "ui"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        disabledResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        disabledDispatcher.DispatchCount.ShouldBe(0);
        disabledAuditWriter.Envelopes.ShouldBeEmpty();
        disabledIdempotencyStore.RecordCount.ShouldBe(0);
        ChatBotAuthorizationFailureAuditFact fact = disabledAuditWriter.AuthorizationFailures.ShouldHaveSingleItem();
        fact.CommandType.ShouldBe("TenantScopedCommand");
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientDisabled);

        string disabledBody = await disabledResponse.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(disabledBody);
        JsonElement problemRoot = problem.RootElement;
        problemRoot.GetProperty("category").GetString().ShouldBe("authorization_denied");
        problemRoot.GetProperty("code").GetString().ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        problemRoot.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        disabledBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
        disabledBody.ShouldNotContain("cli-automation-client", Case.Insensitive);
        disabledBody.ShouldNotContain("payload-sentinel", Case.Insensitive);
        disabledBody.ShouldNotContain("oauth", Case.Insensitive);
        disabledBody.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptCommandCapabilityDisableFlowThenFailClosedForDisabledCapability()
    {
        RecordingDispatcher adminDispatcher = new();
        RecordingAuditWriter adminAuditWriter = new();
        InMemoryCoarseIdempotencyStore adminIdempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> adminFactory = GatewayFactory(
            tenantId: "tenant-alpha",
            adminDispatcher,
            adminAuditWriter,
            idempotencyStore: adminIdempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist(),
            additionalClaims:
            [
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, "policy-admin"),
            ]);
        using HttpClient adminClient = adminFactory.CreateClient();

        using HttpResponseMessage proposal = await adminClient
            .SendAsync(
                CommandCapabilityControlSubmissionRequest(
                    CommandCapabilityDisableSubmitCommand(),
                    "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage approval = await adminClient
            .SendAsync(
                CommandCapabilityControlSubmissionRequest(
                    CommandCapabilityDisableApprovalCommand(),
                    "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    "01ARZ3NDEKTSV4RRFFQ69G5FBA"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        proposal.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        approval.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        adminDispatcher.DispatchCount.ShouldBe(2);
        adminAuditWriter.AuthorizationFailures.ShouldBeEmpty();
        adminAuditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [
                AuditCommitPhase.PreCommit,
                AuditCommitPhase.PostCommit,
                AuditCommitPhase.PreCommit,
                AuditCommitPhase.PostCommit,
            ]);
        adminAuditWriter.Envelopes.Take(2).ShouldAllBe(static envelope =>
            envelope.CommandName == typeof(ContractSubmitCommandCapabilityDisable).Name);
        adminAuditWriter.Envelopes.Skip(2).ShouldAllBe(static envelope =>
            envelope.CommandName == typeof(ContractApproveCommandCapabilityDisable).Name);
        adminAuditWriter.Envelopes.Take(2).ShouldAllBe(static envelope =>
            envelope.StateTransition == "Received->Proposed" &&
            envelope.SourceEvidenceRefs.Contains("admin-operation:command-capability-disable") &&
            envelope.SourceEvidenceRefs.Contains("admin-scope:policy") &&
            envelope.SourceEvidenceRefs.Contains("command-capability:TenantScopedCommand") &&
            envelope.SourceEvidenceRefs.Contains("reason:command-capability-unsafe-execution"));
        adminAuditWriter.Envelopes.Skip(2).ShouldAllBe(static envelope =>
            envelope.ActorType == "human" &&
            envelope.StateTransition == "Active->Disabled" &&
            envelope.SourceEvidenceRefs.Contains("admin-operation:command-capability-disable-approve") &&
            envelope.SourceEvidenceRefs.Contains("admin-scope:policy") &&
            envelope.SourceEvidenceRefs.Contains("command-capability:TenantScopedCommand") &&
            envelope.SourceEvidenceRefs.Contains("reason:command-capability-unsafe-execution") &&
            envelope.SourceEvidenceRefs.Contains("admin-subject:admin-approver"));
        adminIdempotencyStore.Records.Select(static record => record.OperationClass).ShouldBe(
            [CoarseIdempotencyOperationClass.CommandExecution.Code, CoarseIdempotencyOperationClass.CommandExecution.Code]);

        foreach (HttpResponseMessage response in new[] { proposal, approval })
        {
            string body = await response.Content
                .ReadAsStringAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            using JsonDocument accepted = JsonDocument.Parse(body);
            JsonElement root = accepted.RootElement;
            root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
            body.ShouldNotContain("tenant-alpha", Case.Insensitive);
            body.ShouldNotContain("payload-sentinel", Case.Insensitive);
            body.ShouldNotContain("@", Case.Insensitive);
            body.ShouldNotContain("oauth", Case.Insensitive);
            body.ShouldNotContain("secret", Case.Insensitive);
        }

        RecordingDispatcher disabledDispatcher = new();
        RecordingAuditWriter disabledAuditWriter = new();
        InMemoryCoarseIdempotencyStore disabledIdempotencyStore = new(new SystemClock());
        FixedCommandCapabilityControlStateProvider disabledCapabilityProvider = new("tenant-alpha", "TenantScopedCommand");
        using WebApplicationFactory<Program> disabledFactory = GatewayFactory(
            tenantId: "tenant-alpha",
            disabledDispatcher,
            disabledAuditWriter,
            idempotencyStore: disabledIdempotencyStore,
            commandAllowlist: new AllowAllSpineCommandAllowlist(),
            commandCapabilityControlStateProvider: disabledCapabilityProvider);
        using HttpClient disabledClient = disabledFactory.CreateClient();

        using HttpResponseMessage disabledResponse = await disabledClient
            .SendAsync(
                CommandSubmissionRequest("tenant-alpha", "payload-sentinel-disabled-command-capability", origin: "ui"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        disabledResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        disabledDispatcher.DispatchCount.ShouldBe(0);
        disabledAuditWriter.Envelopes.ShouldBeEmpty();
        disabledIdempotencyStore.RecordCount.ShouldBe(0);
        disabledCapabilityProvider.Requests.ShouldBe([("tenant-alpha", "TenantScopedCommand")]);
        ChatBotAuthorizationFailureAuditFact fact = disabledAuditWriter.AuthorizationFailures.ShouldHaveSingleItem();
        fact.TenantId.ShouldBe("tenant-alpha");
        fact.ActorId.ShouldBe("actor-alpha");
        fact.CommandType.ShouldBe("TenantScopedCommand");
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.CommandCapabilityDisabled);
        fact.SurfaceOrigin.ShouldBe("ui");

        string disabledBody = await disabledResponse.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(disabledBody);
        JsonElement problemRoot = problem.RootElement;
        problemRoot.GetProperty("category").GetString().ShouldBe("authorization_denied");
        problemRoot.GetProperty("retryable").GetBoolean().ShouldBeFalse();
        problemRoot.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        disabledBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
        disabledBody.ShouldNotContain("payload-sentinel", Case.Insensitive);
        disabledBody.ShouldNotContain("@", Case.Insensitive);
        disabledBody.ShouldNotContain("oauth", Case.Insensitive);
        disabledBody.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptCommandCapabilityRateLimitAsSinglePolicyAdminMutationThroughUiSpine()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist(),
            additionalClaims:
            [
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, "policy-admin"),
            ]);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                CommandCapabilityControlSubmissionRequest(
                    CommandCapabilityRateLimitCommand(),
                    "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        dispatcher.DispatchCount.ShouldBe(1);
        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope =>
            envelope.ActorType == "human" &&
            envelope.CommandName == typeof(ContractSubmitCommandCapabilityRateLimit).Name &&
            envelope.StateTransition == "Received->Proposed" &&
            envelope.SourceEvidenceRefs.Contains("admin-operation:command-capability-rate-limit") &&
            envelope.SourceEvidenceRefs.Contains("admin-scope:policy") &&
            envelope.SourceEvidenceRefs.Contains("command-capability:TenantScopedCommand") &&
            envelope.SourceEvidenceRefs.Contains("reason:command-capability-noisy-submissions") &&
            envelope.SourceEvidenceRefs.Contains("command-capability-rate-limit-old:0") &&
            envelope.SourceEvidenceRefs.Contains("command-capability-rate-limit-new:2") &&
            envelope.SourceEvidenceRefs.Contains("command-capability-rate-limit-window:rolling-hour") &&
            !envelope.SourceEvidenceRefs.Contains("command-capability-new-state:rate-limited"));
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(CoarseIdempotencyOperationClass.CommandExecution.Code);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("TenantScopedCommand", Case.Insensitive);
        body.ShouldNotContain("@", Case.Insensitive);
        body.ShouldNotContain("oauth", Case.Insensitive);
        body.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptOutboundChannelRateLimitAsSinglePolicyAdminMutationThroughUiSpine()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist(),
            additionalClaims:
            [
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, "policy-admin"),
            ]);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                OutboundChannelRateLimitSubmissionRequest(
                    OutboundChannelRateLimitCommand(),
                    "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        dispatcher.DispatchCount.ShouldBe(1);
        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope =>
            envelope.ActorType == "human" &&
            envelope.CommandName == typeof(ContractSubmitOutboundChannelRateLimit).Name &&
            envelope.StateTransition == "Received->Proposed" &&
            envelope.SourceEvidenceRefs.Contains("admin-operation:outbound-channel-rate-limit") &&
            envelope.SourceEvidenceRefs.Contains("admin-scope:policy") &&
            envelope.SourceEvidenceRefs.Contains("outbound-channel:adapter:mailbox-outbound") &&
            envelope.SourceEvidenceRefs.Contains("reason:outbound-channel-noisy-sends") &&
            envelope.SourceEvidenceRefs.Contains("outbound-channel-rate-limit-old:0") &&
            envelope.SourceEvidenceRefs.Contains("outbound-channel-rate-limit-new:200") &&
            envelope.SourceEvidenceRefs.Contains("outbound-channel-rate-limit-window:rolling-hour") &&
            envelope.SourceEvidenceRefs.Contains("outbound-channel-rate-limit-source-version:4") &&
            !envelope.SourceEvidenceRefs.Contains("outbound-channel-new-state:rate-limited"));
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(CoarseIdempotencyOperationClass.CommandExecution.Code);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("adapter:mailbox-outbound", Case.Insensitive);
        body.ShouldNotContain("@", Case.Insensitive);
        body.ShouldNotContain("oauth", Case.Insensitive);
        body.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldReturnTypedRedactedRetryLaterResponseForRateLimitedCommandCapability()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new AllowAllSpineCommandAllowlist(),
            commandCapabilityRateLimitProvider: new FixedCommandCapabilityRateLimitProvider(
                "tenant-alpha",
                "TenantScopedCommand",
                new CommandCapabilityRateLimitState(2, CommandCapabilityRateLimitWindow.RollingHour)),
            commandCapabilityCommandHistory: new FixedCommandCapabilityCommandHistory(
            [
                DateTimeOffset.UtcNow.AddMinutes(-10),
                DateTimeOffset.UtcNow.AddMinutes(-20),
            ]),
            additionalClaims:
            [
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, "tenant-admin"),
            ]);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                CommandSubmissionRequest("tenant-alpha", "payload-sentinel-rate-limited-command-capability", origin: "ui"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        idempotencyStore.RecordCount.ShouldBe(0);
        ChatBotAuthorizationFailureAuditFact fact = auditWriter.AuthorizationFailures.ShouldHaveSingleItem();
        fact.TenantId.ShouldBe("tenant-alpha");
        fact.ActorId.ShouldBe("actor-alpha");
        fact.CommandType.ShouldBe("TenantScopedCommand");
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.CommandCapabilityRateLimited);
        fact.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.CommandCapabilityDisabled);
        fact.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.CommandCapabilityQuarantined);
        fact.SurfaceOrigin.ShouldBe("ui");

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement problemRoot = problem.RootElement;
        problemRoot.GetProperty("category").GetString().ShouldBe("authorization_denied");
        problemRoot.GetProperty("code").GetString().ShouldBe(ChatBotMessageCodes.CommandCapabilityRateLimited);
        problemRoot.GetProperty("retryable").GetBoolean().ShouldBeTrue();
        problemRoot.GetProperty("clientAction").GetString().ShouldBe(ChatBotMessageNextActions.RetryLater);
        problemRoot.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("TenantScopedCommand", Case.Insensitive);
        body.ShouldNotContain("payload-sentinel", Case.Insensitive);
        body.ShouldNotContain("oauth", Case.Insensitive);
        body.ShouldNotContain("fingerprint", Case.Insensitive);
        body.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptServiceClientQuarantineFlowThenFailClosedForQuarantinedServiceClient()
    {
        RecordingDispatcher adminDispatcher = new();
        RecordingAuditWriter adminAuditWriter = new();
        InMemoryCoarseIdempotencyStore adminIdempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> adminFactory = GatewayFactory(
            tenantId: "tenant-alpha",
            adminDispatcher,
            adminAuditWriter,
            idempotencyStore: adminIdempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist(),
            additionalClaims:
            [
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, "tenant-admin"),
            ]);
        using HttpClient adminClient = adminFactory.CreateClient();

        using HttpResponseMessage proposal = await adminClient
            .SendAsync(
                ServiceClientControlSubmissionRequest(
                    ServiceClientQuarantineSubmitCommand(),
                    "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage approval = await adminClient
            .SendAsync(
                ServiceClientControlSubmissionRequest(
                    ServiceClientQuarantineApprovalCommand(),
                    "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    "01ARZ3NDEKTSV4RRFFQ69G5FBA"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        proposal.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        approval.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        adminDispatcher.DispatchCount.ShouldBe(2);
        adminAuditWriter.AuthorizationFailures.ShouldBeEmpty();
        adminAuditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [
                AuditCommitPhase.PreCommit,
                AuditCommitPhase.PostCommit,
                AuditCommitPhase.PreCommit,
                AuditCommitPhase.PostCommit,
            ]);
        adminAuditWriter.Envelopes.Take(2).ShouldAllBe(static envelope =>
            envelope.CommandName == typeof(ContractSubmitServiceClientQuarantine).Name);
        adminAuditWriter.Envelopes.Skip(2).ShouldAllBe(static envelope =>
            envelope.CommandName == typeof(ContractApproveServiceClientQuarantine).Name);
        adminAuditWriter.Envelopes.Take(2).ShouldAllBe(static envelope =>
            envelope.StateTransition == "Received->Proposed" &&
            envelope.SourceEvidenceRefs.Contains("admin-operation:service-client-quarantine") &&
            envelope.SourceEvidenceRefs.Contains("admin-scope:tenant-admin") &&
            envelope.SourceEvidenceRefs.Contains("service-client:cli-automation-client") &&
            envelope.SourceEvidenceRefs.Contains("reason:service-client-unsafe-activity"));
        adminAuditWriter.Envelopes.Skip(2).ShouldAllBe(static envelope =>
            envelope.ActorType == "human" &&
            envelope.StateTransition == "Active->Quarantined" &&
            envelope.SourceEvidenceRefs.Contains("admin-operation:service-client-quarantine-approve") &&
            envelope.SourceEvidenceRefs.Contains("admin-scope:tenant-admin") &&
            envelope.SourceEvidenceRefs.Contains("service-client:cli-automation-client") &&
            envelope.SourceEvidenceRefs.Contains("reason:service-client-unsafe-activity") &&
            envelope.SourceEvidenceRefs.Contains("admin-subject:admin-approver"));
        adminIdempotencyStore.Records.Select(static record => record.OperationClass).ShouldBe(
            [CoarseIdempotencyOperationClass.CommandExecution.Code, CoarseIdempotencyOperationClass.CommandExecution.Code]);

        foreach (HttpResponseMessage response in new[] { proposal, approval })
        {
            string body = await response.Content
                .ReadAsStringAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            using JsonDocument accepted = JsonDocument.Parse(body);
            JsonElement root = accepted.RootElement;
            root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
            body.ShouldNotContain("tenant-alpha", Case.Insensitive);
            body.ShouldNotContain("cli-automation-client", Case.Insensitive);
            body.ShouldNotContain("@", Case.Insensitive);
            body.ShouldNotContain("oauth", Case.Insensitive);
            body.ShouldNotContain("secret", Case.Insensitive);
        }

        RecordingDispatcher quarantinedDispatcher = new();
        RecordingAuditWriter quarantinedAuditWriter = new();
        InMemoryCoarseIdempotencyStore quarantinedIdempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> quarantinedFactory = GatewayFactory(
            tenantId: "tenant-alpha",
            quarantinedDispatcher,
            quarantinedAuditWriter,
            idempotencyStore: quarantinedIdempotencyStore,
            commandAllowlist: new AllowAllSpineCommandAllowlist(),
            serviceClientControlStateProvider: new FixedServiceClientControlStateProvider(ContractServiceClientControlState.Quarantined),
            principalSubject: "service-account-cli-automation-client",
            additionalClaims: ServiceClientGrantClaims("ui", "TenantScopedCommand"));
        using HttpClient quarantinedClient = quarantinedFactory.CreateClient();

        using HttpResponseMessage quarantinedResponse = await quarantinedClient
            .SendAsync(
                CommandSubmissionRequest("tenant-alpha", "payload-sentinel-quarantined-service-client", origin: "ui"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        quarantinedResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        quarantinedDispatcher.DispatchCount.ShouldBe(0);
        quarantinedAuditWriter.Envelopes.ShouldBeEmpty();
        quarantinedIdempotencyStore.RecordCount.ShouldBe(0);
        ChatBotAuthorizationFailureAuditFact fact = quarantinedAuditWriter.AuthorizationFailures.ShouldHaveSingleItem();
        fact.CommandType.ShouldBe("TenantScopedCommand");
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientQuarantined);

        string quarantinedBody = await quarantinedResponse.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(quarantinedBody);
        JsonElement problemRoot = problem.RootElement;
        problemRoot.GetProperty("category").GetString().ShouldBe("authorization_denied");
        problemRoot.GetProperty("code").GetString().ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        problemRoot.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        quarantinedBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
        quarantinedBody.ShouldNotContain("cli-automation-client", Case.Insensitive);
        quarantinedBody.ShouldNotContain("payload-sentinel", Case.Insensitive);
        quarantinedBody.ShouldNotContain("oauth", Case.Insensitive);
        quarantinedBody.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptAiActorQuarantineFlowThenFailClosedForQuarantinedAiActor()
    {
        RecordingDispatcher adminDispatcher = new();
        RecordingAuditWriter adminAuditWriter = new();
        InMemoryCoarseIdempotencyStore adminIdempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> adminFactory = GatewayFactory(
            tenantId: "tenant-alpha",
            adminDispatcher,
            adminAuditWriter,
            idempotencyStore: adminIdempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist(),
            additionalClaims:
            [
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, "policy-admin"),
            ]);
        using HttpClient adminClient = adminFactory.CreateClient();

        using HttpResponseMessage proposal = await adminClient
            .SendAsync(
                AiActorControlSubmissionRequest(
                    AiActorQuarantineSubmitCommand(),
                    "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage approval = await adminClient
            .SendAsync(
                AiActorControlSubmissionRequest(
                    AiActorQuarantineApprovalCommand(),
                    "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    "01ARZ3NDEKTSV4RRFFQ69G5FBA"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        proposal.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        approval.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        adminDispatcher.DispatchCount.ShouldBe(2);
        adminAuditWriter.AuthorizationFailures.ShouldBeEmpty();
        adminAuditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [
                AuditCommitPhase.PreCommit,
                AuditCommitPhase.PostCommit,
                AuditCommitPhase.PreCommit,
                AuditCommitPhase.PostCommit,
            ]);
        adminAuditWriter.Envelopes.Take(2).ShouldAllBe(static envelope =>
            envelope.CommandName == typeof(ContractSubmitAiActorQuarantine).Name);
        adminAuditWriter.Envelopes.Skip(2).ShouldAllBe(static envelope =>
            envelope.CommandName == typeof(ContractApproveAiActorQuarantine).Name);
        adminAuditWriter.Envelopes.Take(2).ShouldAllBe(static envelope =>
            envelope.StateTransition == "Received->Proposed" &&
            envelope.SourceEvidenceRefs.Contains("admin-operation:ai-actor-quarantine") &&
            envelope.SourceEvidenceRefs.Contains("admin-scope:policy") &&
            envelope.SourceEvidenceRefs.Contains("ai-actor:gpt-mediation-actor") &&
            envelope.SourceEvidenceRefs.Contains("reason:ai-actor-unsafe-proposals"));
        adminAuditWriter.Envelopes.Skip(2).ShouldAllBe(static envelope =>
            envelope.ActorType == "human" &&
            envelope.StateTransition == "Active->Quarantined" &&
            envelope.SourceEvidenceRefs.Contains("admin-operation:ai-actor-quarantine-approve") &&
            envelope.SourceEvidenceRefs.Contains("admin-scope:policy") &&
            envelope.SourceEvidenceRefs.Contains("ai-actor:gpt-mediation-actor") &&
            envelope.SourceEvidenceRefs.Contains("reason:ai-actor-unsafe-proposals") &&
            envelope.SourceEvidenceRefs.Contains("admin-subject:admin-approver"));
        adminIdempotencyStore.Records.Select(static record => record.OperationClass).ShouldBe(
            [CoarseIdempotencyOperationClass.CommandExecution.Code, CoarseIdempotencyOperationClass.CommandExecution.Code]);

        foreach (HttpResponseMessage response in new[] { proposal, approval })
        {
            string body = await response.Content
                .ReadAsStringAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            using JsonDocument accepted = JsonDocument.Parse(body);
            JsonElement root = accepted.RootElement;
            root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
            body.ShouldNotContain("tenant-alpha", Case.Insensitive);
            body.ShouldNotContain("gpt-mediation-actor", Case.Insensitive);
            body.ShouldNotContain("@", Case.Insensitive);
            body.ShouldNotContain("oauth", Case.Insensitive);
            body.ShouldNotContain("secret", Case.Insensitive);
        }

        RecordingDispatcher quarantinedDispatcher = new();
        RecordingAuditWriter quarantinedAuditWriter = new();
        InMemoryCoarseIdempotencyStore quarantinedIdempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> quarantinedFactory = GatewayFactory(
            tenantId: "tenant-alpha",
            quarantinedDispatcher,
            quarantinedAuditWriter,
            idempotencyStore: quarantinedIdempotencyStore,
            commandAllowlist: new AllowAllSpineCommandAllowlist(),
            aiActorControlStateProvider: new FixedAiActorControlStateProvider(ContractAiActorControlState.Quarantined),
            principalSubject: "ai-gpt-mediation-actor",
            additionalClaims: AiActorGrantClaims("ui", "TenantScopedCommand"));
        using HttpClient quarantinedClient = quarantinedFactory.CreateClient();

        using HttpResponseMessage quarantinedResponse = await quarantinedClient
            .SendAsync(
                CommandSubmissionRequest("tenant-alpha", "payload-sentinel-quarantined-ai-actor", origin: "ui"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        quarantinedResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        quarantinedDispatcher.DispatchCount.ShouldBe(0);
        quarantinedAuditWriter.Envelopes.ShouldBeEmpty();
        quarantinedIdempotencyStore.RecordCount.ShouldBe(0);
        ChatBotAuthorizationFailureAuditFact fact = quarantinedAuditWriter.AuthorizationFailures.ShouldHaveSingleItem();
        fact.CommandType.ShouldBe("TenantScopedCommand");
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AiActorQuarantined);
        fact.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.AiActorDisabled);
        fact.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientQuarantined);
        fact.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantRevoked);

        string quarantinedBody = await quarantinedResponse.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(quarantinedBody);
        JsonElement problemRoot = problem.RootElement;
        problemRoot.GetProperty("category").GetString().ShouldBe("authorization_denied");
        problemRoot.GetProperty("code").GetString().ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        problemRoot.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        quarantinedBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
        quarantinedBody.ShouldNotContain("gpt-mediation-actor", Case.Insensitive);
        quarantinedBody.ShouldNotContain("payload-sentinel", Case.Insensitive);
        quarantinedBody.ShouldNotContain("oauth", Case.Insensitive);
        quarantinedBody.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptAiActorRateLimitAsSinglePolicyAdminMutationThroughUiSpine()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist(),
            additionalClaims:
            [
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, "policy-admin"),
            ]);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                AiActorRateLimitSubmissionRequest(
                    AiActorRateLimitCommand(),
                    "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        dispatcher.DispatchCount.ShouldBe(1);
        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope =>
            envelope.ActorType == "human" &&
            envelope.CommandName == typeof(ContractSubmitAiActorRateLimit).Name &&
            envelope.StateTransition == "Received->Proposed" &&
            envelope.SourceEvidenceRefs.Contains("admin-operation:ai-actor-rate-limit") &&
            envelope.SourceEvidenceRefs.Contains("admin-scope:policy") &&
            envelope.SourceEvidenceRefs.Contains("ai-actor:gpt-mediation-actor") &&
            envelope.SourceEvidenceRefs.Contains("reason:ai-actor-noisy-proposals") &&
            envelope.SourceEvidenceRefs.Contains("ai-actor-rate-limit-old:0") &&
            envelope.SourceEvidenceRefs.Contains("ai-actor-rate-limit-new:2") &&
            envelope.SourceEvidenceRefs.Contains("ai-actor-rate-limit-window:rolling-hour") &&
            !envelope.SourceEvidenceRefs.Contains("ai-actor-new-state:rate-limited"));
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(CoarseIdempotencyOperationClass.CommandExecution.Code);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("gpt-mediation-actor", Case.Insensitive);
        body.ShouldNotContain("@", Case.Insensitive);
        body.ShouldNotContain("oauth", Case.Insensitive);
        body.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldReturnTypedRedactedRetryLaterResponseForRateLimitedAiActor()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new AllowAllSpineCommandAllowlist(),
            aiActorRateLimitProvider: new FixedAiActorRateLimitProvider(new AiActorRateLimitState(2, AiActorRateLimitWindow.RollingHour)),
            aiActorProposalHistory: new FixedAiActorProposalHistory(
            [
                DateTimeOffset.UtcNow.AddMinutes(-10),
                DateTimeOffset.UtcNow.AddMinutes(-20),
            ]),
            principalSubject: "ai-gpt-mediation-actor",
            additionalClaims: AiActorGrantClaims("ui", "TenantScopedCommand"));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                CommandSubmissionRequest("tenant-alpha", "payload-sentinel-rate-limited-ai-actor", origin: "ui"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        idempotencyStore.RecordCount.ShouldBe(0);
        ChatBotAuthorizationFailureAuditFact fact = auditWriter.AuthorizationFailures.ShouldHaveSingleItem();
        fact.CommandType.ShouldBe("TenantScopedCommand");
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AiActorRateLimited);
        fact.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.AiActorDisabled);
        fact.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.AiActorQuarantined);
        fact.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientRateLimited);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement problemRoot = problem.RootElement;
        problemRoot.GetProperty("category").GetString().ShouldBe("authorization_denied");
        problemRoot.GetProperty("code").GetString().ShouldBe(ChatBotMessageCodes.AiActorRateLimited);
        problemRoot.GetProperty("retryable").GetBoolean().ShouldBeTrue();
        problemRoot.GetProperty("clientAction").GetString().ShouldBe(ChatBotMessageNextActions.RetryLater);
        problemRoot.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("gpt-mediation-actor", Case.Insensitive);
        body.ShouldNotContain("payload-sentinel", Case.Insensitive);
        body.ShouldNotContain("oauth", Case.Insensitive);
        body.ShouldNotContain("fingerprint", Case.Insensitive);
        body.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldDenyMailboxSourceDisableApprovalFromServiceActorWithTenantAdminClaim()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist(),
            additionalClaims:
            [
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.ServiceActorValue),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, "tenant-admin"),
            ]);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                MailboxSourceDisableSubmissionRequest(MailboxSourceDisableApprovalCommand()),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        idempotencyStore.RecordCount.ShouldBe(0);
        ChatBotAuthorizationFailureAuditFact fact = auditWriter.AuthorizationFailures.ShouldHaveSingleItem();
        fact.ActorId.ShouldBe("actor-alpha");
        fact.CommandType.ShouldBe(typeof(ContractApproveMailboxSourceDisable).Name);
        fact.ReasonCode.ShouldBeOneOf(
            ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing,
            ChatBotAuthorizationReasonCodes.AuthorizationDenied);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authorization_denied");
        root.GetProperty("code").GetString().ShouldBeOneOf(
            ChatBotMessageCodes.RefusalBlockedAction,
            ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("controlled-mailbox-001", Case.Insensitive);
        body.ShouldNotContain("@", Case.Insensitive);
        body.ShouldNotContain("secret", Case.Insensitive);
    }

    [Theory]
    [InlineData(
        ParticipantAuthorizationStage.UnresolvedValue,
        ChatBotMessageCodes.UnresolvedParticipant,
        ChatBotAuthorizationReasonCodes.UnresolvedParticipant,
        ChatBotMessageNextActions.RequestAccess)]
    [InlineData(
        ParticipantAuthorizationStage.EmailOnlyValue,
        ChatBotMessageCodes.UnauthorizedParticipant,
        ChatBotAuthorizationReasonCodes.UnauthorizedParticipant,
        ChatBotMessageNextActions.RequestAccess)]
    [InlineData(
        ParticipantAuthorizationStage.UnauthorizedValue,
        ChatBotMessageCodes.UnauthorizedParticipant,
        ChatBotAuthorizationReasonCodes.UnauthorizedParticipant,
        ChatBotMessageNextActions.RequestAccess)]
    [InlineData(
        ParticipantAuthorizationStage.DirectoryDegradedValue,
        ChatBotMessageCodes.ParticipantDirectoryDegraded,
        ChatBotAuthorizationReasonCodes.ParticipantDirectoryDegraded,
        ChatBotMessageNextActions.RetryLater)]
    public async Task CommandGatewayApi_ShouldBlockUnsafeParticipantAuthoritiesBeforeDispatch(
        string authority,
        string expectedMessageCode,
        string expectedAuditReason,
        string expectedClientAction)
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            idempotencyStore: idempotencyStore,
            participantAuthority: authority);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "restricted-project-sentinel"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        idempotencyStore.RecordCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(expectedAuditReason);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authorization_denied");
        root.GetProperty("code").GetString().ShouldBe(expectedMessageCode);
        root.GetProperty("clientAction").GetString().ShouldBe(expectedClientAction);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("restricted-project-sentinel", Case.Insensitive);
        body.ShouldNotContain("sender@example.test", Case.Insensitive);
        body.ShouldNotContain("party-alpha", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptTenantBoundSubmissionAfterAdmissionStages()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            idempotencyStore: idempotencyStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "allowed-resource"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        dispatcher.DispatchCount.ShouldBe(1);
        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.Select(static envelope => envelope.StateTransition).ShouldBe(
            ["Received->Proposed", "Received->Proposed"]);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.Decision == "allow");
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.TenantId == "tenant-alpha");
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.ActorId == "actor-alpha");
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.CommandName == "TenantScopedCommand");

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("lifecycleState").GetString().ShouldBe("Proposed");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("allowed-resource", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptServiceClientGrantThroughSharedCommandSpine()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            idempotencyStore: idempotencyStore,
            principalSubject: "service-account-cli-automation-client",
            additionalClaims: ServiceClientGrantClaims("cli", "TenantScopedCommand"));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                CommandSubmissionRequest(
                    "tenant-alpha",
                    "allowed-resource-service-client",
                    origin: "cli"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        dispatcher.DispatchCount.ShouldBe(1);
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(
            CoarseIdempotencyOperationClass.CommandExecution.Code);
        auditWriter.AuthorizationFailures.ShouldBeEmpty();
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.ActorId == "service-account-cli-automation-client");
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.ActorType == "service");
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SurfaceOrigin == "cli");
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SourceEvidenceRefs.Contains("service-client:cli-automation-client"));
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SourceEvidenceRefs.Contains("grant:01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SourceEvidenceRefs.Contains("grant-scope:notes.write"));
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SourceEvidenceRefs.Contains("delegated-user:actor-alpha"));
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SourceEvidenceRefs.Contains("oauth-evidence:oauth-proof-01ARZ3NDEKTSV4RRFFQ69G5FAV"));

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("allowed-resource-service-client", Case.Insensitive);
        body.ShouldNotContain("oauth-proof", Case.Insensitive);
        body.ShouldNotContain("bearer", Case.Insensitive);
        body.ShouldNotContain("secret", Case.Insensitive);
    }

    [Theory]
    [InlineData("mcp", "cli", "TenantScopedCommand", ChatBotAuthorizationReasonCodes.ServiceClientWrongSurface)]
    [InlineData("cli", "cli", "CaptureMailboxMessageIntake", ChatBotAuthorizationReasonCodes.ServiceClientGrantUnderScoped)]
    public async Task CommandGatewayApi_ShouldFailClosedServiceClientGrantErrorsBeforeDurableWork(
        string requestOrigin,
        string grantSurface,
        string grantCommand,
        string expectedReason)
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            idempotencyStore: idempotencyStore,
            principalSubject: "service-account-cli-automation-client",
            additionalClaims: ServiceClientGrantClaims(grantSurface, grantCommand));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                CommandSubmissionRequest(
                    "tenant-alpha",
                    "restricted-service-client-resource-C:\\\\secret\\\\item-/tmp/raw-exception",
                    origin: requestOrigin),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        idempotencyStore.RecordCount.ShouldBe(0);
        idempotencyStore.Records.ShouldBeEmpty();
        ChatBotAuthorizationFailureAuditFact fact = auditWriter.AuthorizationFailures.ShouldHaveSingleItem();
        fact.TenantId.ShouldBe("tenant-alpha");
        fact.ActorId.ShouldBe("service-account-cli-automation-client");
        fact.CommandType.ShouldBe("TenantScopedCommand");
        fact.ReasonCode.ShouldBe(expectedReason);
        fact.CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        fact.TaskId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        fact.SurfaceOrigin.ShouldBe(requestOrigin);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authorization_denied");
        root.GetProperty("code").GetString().ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        root.GetProperty("clientAction").GetString().ShouldBe(ChatBotMessageNextActions.RequestAccess);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("restricted-service-client-resource", Case.Insensitive);
        body.ShouldNotContain("oauth-proof", Case.Insensitive);
        body.ShouldNotContain("/tmp/raw-exception", Case.Insensitive);
        body.ShouldNotContain("C:\\", Case.Insensitive);
        body.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldReturnCatalogBackedRedactedProblemsForStory17States()
    {
        await AssertCatalogBackedProblemAsync(
            GatewayFactory(
                tenantId: null,
                new RecordingDispatcher(),
                new RecordingAuditWriter()),
            CommandSubmissionRequest(
                "tenant-alpha",
                "payload-sentinel-restricted-project-C:\\\\secret\\\\item-/tmp/raw-exception"),
            HttpStatusCode.Unauthorized,
            "authentication_failure",
            ChatBotMessageCodes.AuthenticationDenied,
            expectedRetryable: false,
            ChatBotMessageNextActions.Authenticate);

        await AssertCatalogBackedProblemAsync(
            GatewayFactory(
                tenantId: "tenant-alpha",
                new RecordingDispatcher(),
                new RecordingAuditWriter()),
            CommandSubmissionRequest(
                "tenant-beta",
                "payload-sentinel-restricted-project-C:\\\\secret\\\\item-/tmp/raw-exception"),
            HttpStatusCode.Forbidden,
            "authorization_denied",
            ChatBotMessageCodes.AuthorizationDenied,
            expectedRetryable: false,
            ChatBotMessageNextActions.RequestAccess);

        await AssertCatalogBackedProblemAsync(
            GatewayFactory(
                tenantId: "tenant-alpha",
                new RecordingDispatcher(),
                new RecordingAuditWriter(),
                commandAllowlist: new DenyAllSpineCommandAllowlist()),
            CommandSubmissionRequest(
                "tenant-alpha",
                "payload-sentinel-restricted-project-C:\\\\secret\\\\item-/tmp/raw-exception"),
            HttpStatusCode.Forbidden,
            "authorization_denied",
            ChatBotMessageCodes.RefusalBlockedAction,
            expectedRetryable: false,
            ChatBotMessageNextActions.Escalate);

        await AssertCatalogBackedProblemAsync(
            GatewayFactory(
                tenantId: "tenant-alpha",
                new RecordingDispatcher(),
                new RecordingAuditWriter(),
                idempotencyStore: new AlwaysConflictingIdempotencyStore()),
            CommandSubmissionRequest(
                "tenant-alpha",
                "payload-sentinel-restricted-project-C:\\\\secret\\\\item-/tmp/raw-exception"),
            HttpStatusCode.Conflict,
            "conflict",
            ChatBotMessageCodes.IdempotencyConflictCommandExecution,
            expectedRetryable: false,
            ChatBotMessageNextActions.None);

        await AssertCatalogBackedProblemAsync(
            GatewayFactory(
                tenantId: "tenant-alpha",
                new RecordingDispatcher(),
                new RecordingAuditWriter(),
                lifecycleTransitionGuard: new FixedLifecycleTransitionGuard(
                    LifecycleTransitionValidation.Invalid(new LifecycleTransitionDefinition("Received", "Associated")))),
            CommandSubmissionRequest(
                "tenant-alpha",
                "payload-sentinel-restricted-project-C:\\\\secret\\\\item-/tmp/raw-exception"),
            HttpStatusCode.Conflict,
            "conflict",
            ChatBotMessageCodes.InvalidLifecycleTransition,
            expectedRetryable: false,
            ChatBotMessageNextActions.None);

        await AssertCatalogBackedProblemAsync(
            GatewayFactory(
                tenantId: "tenant-alpha",
                new RecordingDispatcher(),
                new RecordingAuditWriter { PreCommitResult = AuditWriteResult.Unavailable() }),
            CommandSubmissionRequest(
                "tenant-alpha",
                "payload-sentinel-restricted-project-C:\\\\secret\\\\item-/tmp/raw-exception"),
            HttpStatusCode.ServiceUnavailable,
            "internal_error",
            ChatBotMessageCodes.AuditUnavailable,
            expectedRetryable: true,
            ChatBotMessageNextActions.RetryLater);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldReplayEquivalentDuplicateWithoutRedispatchOrAudit()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryAuditReplayIntentQueue replayQueue = new();
        InMemoryOperatorAlertSink alertSink = new();
        InMemoryOperationStatusStore operationStatusStore = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            replayQueue,
            alertSink,
            operationStatusStore,
            idempotencyStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "allowed-resource"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string firstBody = await first.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage second = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "allowed-resource"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string secondBody = await second.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        second.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        secondBody.ShouldBe(firstBody);
        dispatcher.DispatchCount.ShouldBe(1);
        idempotencyStore.RecordCount.ShouldBe(1);
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        replayQueue.Intents.ShouldBeEmpty();
        alertSink.Alerts.ShouldBeEmpty();

        OperationStatusRecord? status = await operationStatusStore
            .TryGetAsync("tenant-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAX", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        status.ShouldNotBeNull();
        status.AuditStatus.ShouldBe(OperationStatusRecord.AuditCommitted);
        secondBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
        secondBody.ShouldNotContain("allowed-resource", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldSuppressDuplicateMailboxProviderDeliveryThroughMessageIntakeIdempotency()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client
            .SendAsync(
                MailboxIntakeSubmissionRequest(
                    "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    "01ARZ3NDEKTSV4RRFFQ69G5FAZ"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string firstBody = await first.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using HttpResponseMessage duplicate = await client
            .SendAsync(
                MailboxIntakeSubmissionRequest(
                    "01ARZ3NDEKTSV4RRFFQ69G5FBA",
                    "01ARZ3NDEKTSV4RRFFQ69G5FBB"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string duplicateBody = await duplicate.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        duplicate.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        duplicateBody.ShouldBe(firstBody);
        dispatcher.DispatchCount.ShouldBe(1);
        CoarseIdempotencyRecord record = idempotencyStore.Records.Single();
        record.OperationClass.ShouldBe(CoarseIdempotencyOperationClass.MessageIntake.Code);
        record.CommandType.ShouldBe(nameof(CaptureMailboxMessageIntake));
        auditWriter.Envelopes.ShouldContain(static envelope =>
            envelope.ReasonCode == "duplicate_provider_message" &&
            envelope.Outcome == "duplicate_suppressed" &&
            envelope.SurfaceOrigin == "mailbox");

        duplicateBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
        duplicateBody.ShouldNotContain("controlled-mailbox-001", Case.Insensitive);
        duplicateBody.ShouldNotContain("graph-message-001", Case.Insensitive);
        duplicateBody.ShouldNotContain("sender@example.test", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldReturnMetadataOnlyConflictForDuplicateIdempotencyConflict()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryAuditReplayIntentQueue replayQueue = new();
        InMemoryOperatorAlertSink alertSink = new();
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            replayQueue,
            alertSink,
            idempotencyStore: new AlwaysConflictingIdempotencyStore());
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                CommandSubmissionRequest(
                    "tenant-alpha",
                    "payload-sentinel-C:\\\\secret\\\\item-/tmp/raw-exception"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        replayQueue.Intents.ShouldBeEmpty();
        alertSink.Alerts.ShouldBeEmpty();

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("conflict");
        root.GetProperty("code").GetString().ShouldBe("idempotency_conflict_command_execution");
        root.GetProperty("retryable").GetBoolean().ShouldBeFalse();
        root.GetProperty("clientAction").GetString().ShouldBe("none");
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("payload-sentinel", Case.Insensitive);
        body.ShouldNotContain("/tmp/raw-exception", Case.Insensitive);
        body.ShouldNotContain("C:\\", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldRejectUnauthenticatedSubmissionBeforeDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: null,
            dispatcher,
            auditWriter);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "restricted-project-sentinel"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        ChatBotAuthorizationFailureAuditFact fact = auditWriter.AuthorizationFailures.Single();
        fact.TenantId.ShouldBe("unavailable");
        fact.ActorId.ShouldBe("anonymous");
        fact.CommandType.ShouldBe("TenantScopedCommand");
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthenticationDenied);
        fact.CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        fact.TaskId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authentication_failure");
        root.GetProperty("code").GetString().ShouldBe(ChatBotAuthorizationReasonCodes.AuthenticationDenied);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("restricted-project-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldRejectCrossTenantTargetBeforeDispatchAndRecordMetadataOnlyAuditFact()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                CommandSubmissionRequest(
                    "tenant-beta",
                    "restricted-project-sentinel-C:\\\\secret\\\\item-/tmp/raw-exception"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        ChatBotAuthorizationFailureAuditFact fact = auditWriter.AuthorizationFailures.Single();
        fact.TenantId.ShouldBe("tenant-alpha");
        fact.ActorId.ShouldBe("actor-alpha");
        fact.CommandType.ShouldBe("TenantScopedCommand");
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMismatch);
        fact.CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        fact.TaskId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authorization_denied");
        root.GetProperty("code").GetString().ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("tenant-beta", Case.Insensitive);
        body.ShouldNotContain("restricted-project-sentinel", Case.Insensitive);
        body.ShouldNotContain("/tmp/raw-exception", Case.Insensitive);
        body.ShouldNotContain("C:\\", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldReturnAuditUnavailableWhenRejectedLifecycleTransitionCannotBeAudited()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new() { PreCommitResult = AuditWriteResult.Unavailable() };
        InMemoryAuditReplayIntentQueue replayQueue = new();
        InMemoryOperatorAlertSink alertSink = new();
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            replayQueue,
            alertSink,
            lifecycleTransitionGuard: new FixedLifecycleTransitionGuard(
                LifecycleTransitionValidation.Invalid(new LifecycleTransitionDefinition("Received", "Associated"))));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                CommandSubmissionRequest(
                    "tenant-alpha",
                    "payload-sentinel-C:\\\\secret\\\\item-/tmp/raw-exception"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.Count.ShouldBe(1);
        auditWriter.Envelopes[0].Decision.ShouldBe("reject");
        auditWriter.Envelopes[0].ReasonCode.ShouldBe(LifecycleTransitionReasonCodes.InvalidTransition);
        auditWriter.Envelopes[0].StateTransition.ShouldBe("Received->Associated");
        replayQueue.Intents.Count.ShouldBe(1);
        replayQueue.Intents[0].Kind.ShouldBe(AuditReplayIntentKind.PreCommitOperationReplay);
        replayQueue.Intents[0].ReasonCode.ShouldBe(AuditFailureReasonCodes.AuditUnavailable);
        alertSink.Alerts.Count.ShouldBe(1);
        alertSink.Alerts[0].Kind.ShouldBe(OperatorAlertKind.AuditUnavailable);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("internal_error");
        root.GetProperty("code").GetString().ShouldBe(AuditFailureReasonCodes.AuditUnavailable);
        root.GetProperty("retryable").GetBoolean().ShouldBeTrue();
        root.GetProperty("clientAction").GetString().ShouldBe("retry-later");
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("payload-sentinel", Case.Insensitive);
        body.ShouldNotContain("/tmp/raw-exception", Case.Insensitive);
        body.ShouldNotContain("C:\\", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldFailClosedWhenPreCommitAuditIsUnavailable()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new() { PreCommitResult = AuditWriteResult.Unavailable() };
        InMemoryAuditReplayIntentQueue replayQueue = new();
        InMemoryOperatorAlertSink alertSink = new();
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            replayQueue,
            alertSink);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                CommandSubmissionRequest("tenant-alpha", "allowed-resource-C:\\\\secret\\\\item-/tmp/raw-exception"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.Count.ShouldBe(1);
        auditWriter.Envelopes[0].Phase.ShouldBe(AuditCommitPhase.PreCommit);
        replayQueue.Intents.Count.ShouldBe(1);
        replayQueue.Intents[0].Kind.ShouldBe(AuditReplayIntentKind.PreCommitOperationReplay);
        replayQueue.Intents[0].ReasonCode.ShouldBe(AuditFailureReasonCodes.AuditUnavailable);
        alertSink.Alerts.Count.ShouldBe(1);
        alertSink.Alerts[0].Kind.ShouldBe(OperatorAlertKind.AuditUnavailable);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("internal_error");
        root.GetProperty("code").GetString().ShouldBe(AuditFailureReasonCodes.AuditUnavailable);
        root.GetProperty("retryable").GetBoolean().ShouldBeTrue();
        root.GetProperty("clientAction").GetString().ShouldBe("retry-later");
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("allowed-resource", Case.Insensitive);
        body.ShouldNotContain("/tmp/raw-exception", Case.Insensitive);
        body.ShouldNotContain("C:\\", Case.Insensitive);
    }

    [Fact]
    public async Task CommandGatewayApi_ShouldAcceptAndQueueReconciliationWhenPostCommitAuditFails()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new() { PostCommitResult = AuditWriteResult.Unavailable("post_commit_sink_unavailable") };
        InMemoryAuditReplayIntentQueue replayQueue = new();
        InMemoryOperatorAlertSink alertSink = new();
        InMemoryOperationStatusStore operationStatusStore = new();
        using WebApplicationFactory<Program> factory = GatewayFactory(
            tenantId: "tenant-alpha",
            dispatcher,
            auditWriter,
            replayQueue,
            alertSink,
            operationStatusStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "allowed-resource"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        dispatcher.DispatchCount.ShouldBe(1);
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        replayQueue.Intents.Count.ShouldBe(1);
        replayQueue.Intents[0].Kind.ShouldBe(AuditReplayIntentKind.PostCommitAuditReconciliation);
        replayQueue.Intents[0].ReasonCode.ShouldBe("post_commit_sink_unavailable");
        alertSink.Alerts.Count.ShouldBe(1);
        alertSink.Alerts[0].Kind.ShouldBe(OperatorAlertKind.PostCommitAuditReconciliationRequired);
        alertSink.Alerts[0].ReasonCode.ShouldBe("post_commit_sink_unavailable");

        OperationStatusRecord? status = await operationStatusStore
            .TryGetAsync("tenant-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAX", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        status.ShouldNotBeNull();
        status.AuditStatus.ShouldBe(OperationStatusRecord.AuditReconciling);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("allowed-resource", Case.Insensitive);
    }

    private static WebApplicationFactory<Program> GatewayFactory(
        string? tenantId,
        RecordingDispatcher dispatcher,
        RecordingAuditWriter auditWriter,
        InMemoryAuditReplayIntentQueue? replayQueue = null,
        InMemoryOperatorAlertSink? alertSink = null,
        InMemoryOperationStatusStore? operationStatusStore = null,
        IIdempotencyStore? idempotencyStore = null,
        ILifecycleTransitionGuard? lifecycleTransitionGuard = null,
        ISpineCommandAllowlist? commandAllowlist = null,
        string? participantAuthority = null,
        IReadOnlyCollection<string>? projectOwners = null,
        AssociationCorrectionDependencyReadinessStatus? correctionDependencyReadiness = null,
        IServiceClientControlStateProvider? serviceClientControlStateProvider = null,
        IAiActorControlStateProvider? aiActorControlStateProvider = null,
        ICommandCapabilityControlStateProvider? commandCapabilityControlStateProvider = null,
        IAiActorRateLimitProvider? aiActorRateLimitProvider = null,
        IAiActorProposalHistory? aiActorProposalHistory = null,
        ICommandCapabilityRateLimitProvider? commandCapabilityRateLimitProvider = null,
        ICommandCapabilityCommandHistory? commandCapabilityCommandHistory = null,
        string? principalSubject = null,
        IReadOnlyCollection<Claim>? additionalClaims = null)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.ConfigureServices(
                    services =>
                    {
                        if (tenantId is not null)
                        {
                            services.AddSingleton<IStartupFilter>(
                                new TestPrincipalStartupFilter(
                                    tenantId,
                                    participantAuthority,
                                    projectOwners,
                                    principalSubject,
                                    additionalClaims));
                        }

                        services.AddSingleton<ICommandDispatcher>(dispatcher);
                        services.AddSingleton<IAuditWriter>(auditWriter);
                        if (replayQueue is not null)
                        {
                            services.AddSingleton<IAuditReplayIntentQueue>(replayQueue);
                        }

                        if (alertSink is not null)
                        {
                            services.AddSingleton<IOperatorAlertSink>(alertSink);
                        }

                        if (operationStatusStore is not null)
                        {
                            services.AddSingleton<IOperationStatusStore>(operationStatusStore);
                        }

                        services.AddSingleton<IIdempotencyStore>(
                            idempotencyStore ?? new InMemoryCoarseIdempotencyStore(new SystemClock()));
                        if (lifecycleTransitionGuard is not null)
                        {
                            services.AddSingleton(lifecycleTransitionGuard);
                        }

                        services.AddSingleton<ISpineCommandAllowlist>(_ => commandAllowlist ?? new AllowAllSpineCommandAllowlist());
                        if (correctionDependencyReadiness is not null)
                        {
                            services.AddSingleton<IAssociationCorrectionDependencyReadiness>(
                                new FixedAssociationCorrectionDependencyReadiness(correctionDependencyReadiness));
                        }

                        if (serviceClientControlStateProvider is not null)
                        {
                            services.AddSingleton(serviceClientControlStateProvider);
                        }

                        if (aiActorControlStateProvider is not null)
                        {
                            services.AddSingleton(aiActorControlStateProvider);
                        }

                        if (commandCapabilityControlStateProvider is not null)
                        {
                            services.AddSingleton(commandCapabilityControlStateProvider);
                        }

                        if (aiActorRateLimitProvider is not null)
                        {
                            services.AddSingleton(aiActorRateLimitProvider);
                        }

                        if (aiActorProposalHistory is not null)
                        {
                            services.AddSingleton(aiActorProposalHistory);
                        }

                        if (commandCapabilityRateLimitProvider is not null)
                        {
                            services.AddSingleton(commandCapabilityRateLimitProvider);
                        }

                        if (commandCapabilityCommandHistory is not null)
                        {
                            services.AddSingleton(commandCapabilityCommandHistory);
                        }
                    }));

    private static WebApplicationFactory<Program> ParticipantResolutionGatewayFactory(
        string tenantId,
        RecordingEventStoreGatewayClient eventStore,
        RecordingAuditWriter auditWriter,
        RecordingParticipantDirectory directory,
        IIdempotencyStore idempotencyStore)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.ConfigureServices(
                    services =>
                    {
                        services.AddSingleton<IStartupFilter>(new TestPrincipalStartupFilter(tenantId));
                        services.AddSingleton<IEventStoreGatewayClient>(eventStore);
                        services.AddSingleton<IAuditWriter>(auditWriter);
                        services.AddSingleton<IParticipantDirectory>(directory);
                        services.AddSingleton<IIdempotencyStore>(idempotencyStore);
                        services.AddSingleton<ISpineCommandAllowlist>(new ChatBotSpineCommandAllowlist());
                    }));

    private static WebApplicationFactory<Program> AssociationScoringGatewayFactory(
        string tenantId,
        RecordingEventStoreGatewayClient eventStore,
        RecordingAuditWriter auditWriter,
        RecordingProjectDirectory directory,
        IIdempotencyStore idempotencyStore)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.ConfigureServices(
                    services =>
                    {
                        services.AddSingleton<IStartupFilter>(new TestPrincipalStartupFilter(tenantId));
                        services.AddSingleton<IEventStoreGatewayClient>(eventStore);
                        services.AddSingleton<IAuditWriter>(auditWriter);
                        services.AddSingleton<IProjectDirectory>(directory);
                        services.AddSingleton<IIdempotencyStore>(idempotencyStore);
                        services.AddSingleton<ISpineCommandAllowlist>(new ChatBotSpineCommandAllowlist());
                    }));

    private static WebApplicationFactory<Program> AssociationCorrectionGatewayFactory(
        string tenantId,
        RecordingEventStoreGatewayClient eventStore,
        RecordingAuditWriter auditWriter,
        IIdempotencyStore idempotencyStore,
        AssociationCorrectionDependencyReadinessStatus readiness,
        RecordingWorkflowRuntime? workflowRuntime = null)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.ConfigureServices(
                    services =>
                    {
                        services.AddSingleton<IStartupFilter>(
                            new TestPrincipalStartupFilter(tenantId, projectOwners: ["project-alpha", "project-beta"]));
                        services.AddSingleton<IEventStoreGatewayClient>(eventStore);
                        services.AddSingleton<IAuditWriter>(auditWriter);
                        services.AddSingleton<IIdempotencyStore>(idempotencyStore);
                        services.AddSingleton<ISpineCommandAllowlist>(new ChatBotSpineCommandAllowlist());
                        services.AddSingleton<ICorrectionPropagationWorkflowRuntime>(workflowRuntime ?? new RecordingWorkflowRuntime());
                        services.AddSingleton<IAssociationCorrectionDependencyReadiness>(
                            new FixedAssociationCorrectionDependencyReadiness(readiness));
                    }));

    private static WebApplicationFactory<Program> AiActionProposalGatewayFactory(
        string tenantId,
        RecordingEventStoreGatewayClient eventStore,
        RecordingAuditWriter auditWriter,
        IIdempotencyStore idempotencyStore)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.ConfigureServices(
                    services =>
                    {
                        services.AddSingleton<IStartupFilter>(
                            new TestPrincipalStartupFilter(tenantId, projectOwners: ["project-001"]));
                        services.AddSingleton<IEventStoreGatewayClient>(eventStore);
                        services.AddSingleton<IAuditWriter>(auditWriter);
                        services.AddSingleton<IIdempotencyStore>(idempotencyStore);
                        services.AddSingleton<ISpineCommandAllowlist>(new ChatBotSpineCommandAllowlist());
                    }));

    private static WebApplicationFactory<Program> LowRiskAiExecutionGatewayFactory(
        string tenantId,
        RecordingEventStoreGatewayClient eventStore,
        RecordingAuditWriter auditWriter,
        IIdempotencyStore idempotencyStore,
        RecordingAiAssistanceProvider provider,
        bool lowRiskAllowed)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.ConfigureServices(
                    services =>
                    {
                        services.AddSingleton<IStartupFilter>(
                            new TestPrincipalStartupFilter(tenantId, projectOwners: ["project-001"]));
                        services.AddSingleton<IEventStoreGatewayClient>(eventStore);
                        services.AddSingleton<IAuditWriter>(auditWriter);
                        services.AddSingleton<IIdempotencyStore>(idempotencyStore);
                        services.AddSingleton<ITenantAiPolicySnapshotProvider>(
                            new FixedTenantAiPolicySnapshotProvider(lowRiskAllowed));
                        services.AddSingleton<IAiAssistanceProvider>(provider);
                        services.AddSingleton<ISpineCommandAllowlist>(new ChatBotSpineCommandAllowlist());
                    }));

    private static WebApplicationFactory<Program> ApprovedAiExecutionGatewayFactory(
        string tenantId,
        RecordingEventStoreGatewayClient eventStore,
        RecordingAuditWriter auditWriter,
        IIdempotencyStore idempotencyStore,
        RecordingConversationWriter conversationWriter)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.ConfigureServices(
                    services =>
                    {
                        services.AddSingleton<IStartupFilter>(
                            new TestPrincipalStartupFilter(tenantId, projectOwners: ["project-001"]));
                        services.AddSingleton<IEventStoreGatewayClient>(eventStore);
                        services.AddSingleton<IAuditWriter>(auditWriter);
                        services.AddSingleton<IIdempotencyStore>(idempotencyStore);
                        services.AddSingleton<IConversationWriter>(conversationWriter);
                        services.AddSingleton<ISpineCommandAllowlist>(new ChatBotSpineCommandAllowlist());
                    }));

    private static WebApplicationFactory<Program> OutboundDraftGatewayFactory(
        string tenantId,
        RecordingEventStoreGatewayClient eventStore,
        RecordingAuditWriter auditWriter,
        IIdempotencyStore idempotencyStore,
        IReadOnlyCollection<Claim> authorityClaims)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.ConfigureServices(
                    services =>
                    {
                        services.AddSingleton<IStartupFilter>(
                            new TestPrincipalStartupFilter(
                                tenantId,
                                projectOwners: ["project-alpha"],
                                additionalClaims: authorityClaims));
                        services.AddSingleton<IEventStoreGatewayClient>(eventStore);
                        services.AddSingleton<IAuditWriter>(auditWriter);
                        services.AddSingleton<IIdempotencyStore>(idempotencyStore);
                        services.AddSingleton<ISpineCommandAllowlist>(new ChatBotSpineCommandAllowlist());
                    }));

    private static async Task AssertCatalogBackedProblemAsync(
        WebApplicationFactory<Program> factory,
        HttpRequestMessage request,
        HttpStatusCode expectedStatus,
        string expectedCategory,
        string expectedCode,
        bool expectedRetryable,
        string expectedClientAction)
    {
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(expectedCode);

        using (factory)
        using (request)
        using (HttpClient client = factory.CreateClient())
        using (HttpResponseMessage response = await client
            .SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(true))
        {
            response.StatusCode.ShouldBe(expectedStatus);

            string body = await response.Content
                .ReadAsStringAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            using JsonDocument problem = JsonDocument.Parse(body);
            JsonElement root = problem.RootElement;
            root.GetProperty("title").GetString().ShouldBe(entry.Headline);
            root.GetProperty("message").GetString().ShouldBe(entry.Reason);
            root.GetProperty("category").GetString().ShouldBe(expectedCategory);
            root.GetProperty("code").GetString().ShouldBe(entry.Code);
            root.GetProperty("retryable").GetBoolean().ShouldBe(expectedRetryable);
            root.GetProperty("clientAction").GetString().ShouldBe(expectedClientAction);
            root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe(ChatBotDetailVisibility.MetadataOnly);

            body.ShouldNotContain("tenant-alpha", Case.Insensitive);
            body.ShouldNotContain("tenant-beta", Case.Insensitive);
            body.ShouldNotContain("payload-sentinel", Case.Insensitive);
            body.ShouldNotContain("restricted-project", Case.Insensitive);
            body.ShouldNotContain("/tmp/raw-exception", Case.Insensitive);
            body.ShouldNotContain("C:\\", Case.Insensitive);
        }
    }

    private static HttpRequestMessage CommandSubmissionRequest(string tenantId, string resourceName, string? origin = null)
    {
        string originProperty = string.IsNullOrWhiteSpace(origin)
            ? string.Empty
            : $"              \"origin\": {JsonSerializer.Serialize(origin)},\n";
        string payload =
            $$"""
            {
              "commandId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "commandType": "TenantScopedCommand",
              {{originProperty}}"command": {
                "tenantId": "{{tenantId}}",
                "resourceName": "{{resourceName}}"
              },
              "requestSchemaVersion": "v1"
            }
            """;

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        return request;
    }

    private static HttpRequestMessage OutboundDraftSubmissionRequest(
        Hexalith.ChatBot.Contracts.Commands.CreateOutboundDraft? command = null,
        string commandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY")
    {
        object payload = new
        {
            commandId,
            commandType = nameof(Hexalith.ChatBot.Contracts.Commands.CreateOutboundDraft),
            origin = "ui",
            command = command ?? OutboundDraftCommand(),
            requestSchemaVersion = "v1",
        };

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static Hexalith.ChatBot.Contracts.Commands.CreateOutboundDraft OutboundDraftCommand()
        => new(
            "draft-001",
            "project-001",
            "actor-alpha",
            "actor-alpha",
            "conversation:conv-001",
            "source-message:msg-001",
            "item-001",
            ["recipient:party-001"],
            ["conversation:conv-001", "source-message:msg-001", "file:file-001"],
            "policy-snap-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            new Hexalith.ChatBot.Contracts.Commands.OutboundDraftContent(
                "Status update",
                "Governed draft content.",
                "text/plain"));

    private static HttpRequestMessage OutboundApprovalSubmissionRequest<TCommand>(
        TCommand command,
        string commandId)
        where TCommand : IChatBotCommand
    {
        object payload = new
        {
            commandId,
            commandType = command.GetType().Name,
            origin = "ui",
            command,
            requestSchemaVersion = "v1",
        };

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static HttpRequestMessage MailboxSourceDisableSubmissionRequest<TCommand>(TCommand command)
        where TCommand : IChatBotCommand
        => MailboxSourceControlSubmissionRequest(command, "01ARZ3NDEKTSV4RRFFQ69G5FAY", "01ARZ3NDEKTSV4RRFFQ69G5FAX");

    private static HttpRequestMessage MailboxSourceControlSubmissionRequest<TCommand>(
        TCommand command,
        string commandId,
        string taskId)
        where TCommand : IChatBotCommand
    {
        object payload = new
        {
            commandId,
            commandType = command.GetType().Name,
            origin = "ui",
            command,
            requestSchemaVersion = "v1",
        };

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", taskId);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static ContractApproveMailboxSourceDisable MailboxSourceDisableApprovalCommand()
        => new(
            "mailbox-disable-001",
            "controlled-mailbox-001",
            "mailbox-source-unsafe-activity",
            "policy-snapshot-mailbox-v1",
            ContractMailboxSourceControlState.Active,
            ContractMailboxSourceControlState.Disabled,
            5,
            "admin-requester",
            "admin-approver",
            MailboxSourceControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ContractSubmitMailboxSourceQuarantine MailboxSourceQuarantineSubmitCommand()
        => new(
            "mailbox-quarantine-001",
            "controlled-mailbox-001",
            "mailbox-source-unsafe-activity",
            "policy-snapshot-mailbox-v1",
            ContractMailboxSourceControlState.Active,
            ContractMailboxSourceControlState.Quarantined,
            4,
            "admin-requester",
            MailboxSourceControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ContractApproveMailboxSourceQuarantine MailboxSourceQuarantineApprovalCommand()
        => new(
            "mailbox-quarantine-001",
            "controlled-mailbox-001",
            "mailbox-source-unsafe-activity",
            "policy-snapshot-mailbox-v1",
            ContractMailboxSourceControlState.Active,
            ContractMailboxSourceControlState.Quarantined,
            5,
            "admin-requester",
            "admin-approver",
            MailboxSourceControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static HttpRequestMessage ServiceClientControlSubmissionRequest<TCommand>(
        TCommand command,
        string commandId,
        string taskId)
        where TCommand : IChatBotCommand
    {
        object payload = new
        {
            commandId,
            commandType = command.GetType().Name,
            origin = "ui",
            command,
            requestSchemaVersion = "v1",
        };

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", taskId);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static ContractSubmitServiceClientDisable ServiceClientDisableSubmitCommand()
        => new(
            "service-client-disable-001",
            "cli-automation-client",
            "service-client-unsafe-activity",
            "policy-snapshot-service-client-v1",
            ContractServiceClientControlState.Active,
            ContractServiceClientControlState.Disabled,
            4,
            "admin-requester",
            ServiceClientControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ContractApproveServiceClientDisable ServiceClientDisableApprovalCommand()
        => new(
            "service-client-disable-001",
            "cli-automation-client",
            "service-client-unsafe-activity",
            "policy-snapshot-service-client-v1",
            ContractServiceClientControlState.Active,
            ContractServiceClientControlState.Disabled,
            5,
            "admin-requester",
            "admin-approver",
            ServiceClientControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ContractSubmitServiceClientQuarantine ServiceClientQuarantineSubmitCommand()
        => new(
            "service-client-quarantine-001",
            "cli-automation-client",
            "service-client-unsafe-activity",
            "policy-snapshot-service-client-v1",
            ContractServiceClientControlState.Active,
            ContractServiceClientControlState.Quarantined,
            4,
            "admin-requester",
            ServiceClientControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ContractApproveServiceClientQuarantine ServiceClientQuarantineApprovalCommand()
        => new(
            "service-client-quarantine-001",
            "cli-automation-client",
            "service-client-unsafe-activity",
            "policy-snapshot-service-client-v1",
            ContractServiceClientControlState.Active,
            ContractServiceClientControlState.Quarantined,
            5,
            "admin-requester",
            "admin-approver",
            ServiceClientControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static HttpRequestMessage CommandCapabilityControlSubmissionRequest<TCommand>(
        TCommand command,
        string commandId,
        string taskId)
        where TCommand : IChatBotCommand
    {
        object payload = new
        {
            commandId,
            commandType = command.GetType().Name,
            origin = "ui",
            command,
            requestSchemaVersion = "v1",
        };

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", taskId);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static ContractSubmitCommandCapabilityDisable CommandCapabilityDisableSubmitCommand()
        => new(
            "command-capability-disable-001",
            "TenantScopedCommand",
            "command-capability-unsafe-execution",
            "policy-snapshot-policy-admin-v1",
            ContractCommandCapabilityControlState.Active,
            ContractCommandCapabilityControlState.Disabled,
            4,
            "admin-requester",
            CommandCapabilityControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ContractApproveCommandCapabilityDisable CommandCapabilityDisableApprovalCommand()
        => new(
            "command-capability-disable-001",
            "TenantScopedCommand",
            "command-capability-unsafe-execution",
            "policy-snapshot-policy-admin-v1",
            ContractCommandCapabilityControlState.Active,
            ContractCommandCapabilityControlState.Disabled,
            5,
            "admin-requester",
            "admin-approver",
            CommandCapabilityControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ContractSubmitCommandCapabilityRateLimit CommandCapabilityRateLimitCommand()
        => new(
            "command-capability-rate-limit-001",
            "TenantScopedCommand",
            "command-capability-noisy-submissions",
            "policy-snapshot-policy-admin-v1",
            OldBudget: 0,
            NewBudget: 2,
            CommandCapabilityRateLimitWindow.RollingHour,
            4,
            "admin-requester",
            CommandCapabilityRateLimitSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static HttpRequestMessage OutboundChannelRateLimitSubmissionRequest(
        ContractSubmitOutboundChannelRateLimit command,
        string commandId,
        string taskId)
        => CommandCapabilityControlSubmissionRequest(command, commandId, taskId);

    private static ContractSubmitOutboundChannelRateLimit OutboundChannelRateLimitCommand()
        => new(
            "outbound-channel-rate-limit-001",
            "adapter:mailbox-outbound",
            "outbound-channel-noisy-sends",
            "policy-snapshot-policy-admin-v1",
            OldBudget: 0,
            NewBudget: 200,
            OutboundChannelRateLimitWindow.RollingHour,
            4,
            "admin-requester",
            OutboundChannelRateLimitSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static HttpRequestMessage AiActorControlSubmissionRequest<TCommand>(
        TCommand command,
        string commandId,
        string taskId)
        where TCommand : IChatBotCommand
    {
        object payload = new
        {
            commandId,
            commandType = command.GetType().Name,
            origin = "ui",
            command,
            requestSchemaVersion = "v1",
        };

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", taskId);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static HttpRequestMessage AiActorRateLimitSubmissionRequest(
        ContractSubmitAiActorRateLimit command,
        string commandId,
        string taskId)
        => AiActorControlSubmissionRequest(command, commandId, taskId);

    private static ContractSubmitAiActorRateLimit AiActorRateLimitCommand()
        => new(
            "ai-actor-rate-limit-001",
            "gpt-mediation-actor",
            "ai-actor-noisy-proposals",
            "policy-snapshot-policy-admin-v1",
            OldBudget: 0,
            NewBudget: 2,
            AiActorRateLimitWindow.RollingHour,
            4,
            "admin-requester",
            AiActorRateLimitSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ContractSubmitAiActorQuarantine AiActorQuarantineSubmitCommand()
        => new(
            "ai-actor-quarantine-001",
            "gpt-mediation-actor",
            "ai-actor-unsafe-proposals",
            "policy-snapshot-policy-admin-v1",
            ContractAiActorControlState.Active,
            ContractAiActorControlState.Quarantined,
            4,
            "admin-requester",
            AiActorControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ContractApproveAiActorQuarantine AiActorQuarantineApprovalCommand()
        => new(
            "ai-actor-quarantine-001",
            "gpt-mediation-actor",
            "ai-actor-unsafe-proposals",
            "policy-snapshot-policy-admin-v1",
            ContractAiActorControlState.Active,
            ContractAiActorControlState.Quarantined,
            5,
            "admin-requester",
            "admin-approver",
            AiActorControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static RequestOutboundSendApproval OutboundApprovalRequestCommand()
        => new(
            "approval-001",
            "draft-001",
            "project-001",
            "actor-alpha",
            "conversation:conv-001",
            "source-message:msg-001",
            "item-001",
            ["recipient:party-001"],
            ["conversation:conv-001", "source-message:msg-001", "file:file-001"],
            "policy-snap-001",
            "metadata_only",
            nameof(ExecuteApprovedOutboundDraft),
            "chatbot-spine.v1",
            "outbound-send-approved-pending-adapter",
            new OutboundApprovalContentSnapshot(
                new OutboundDraftContent("Status update", "Governed draft content.", "text/plain"),
                null,
                "metadata_only",
                null),
            SenderAuthorityClass.AuthenticatedUserSend,
            ApprovalEvidenceFreshness.Fresh,
            1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static DecideOutboundApproval OutboundApprovalDecisionCommand()
        => new(
            "approval-001",
            "draft-001",
            "project-001",
            ApprovalDecisionKind.Approve,
            "decision-001",
            2,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            new OutboundDraftContent("Status update", "Approved governed content.", "text/plain"));

    private static ExecuteApprovedOutboundDraft OutboundSendCommand(string sendId)
        => new(
            sendId,
            "approval-001",
            "draft-001",
            "project-001",
            "actor-alpha",
            "actor-alpha",
            "conversation:conv-001",
            "source-message:msg-001",
            "item-001",
            ["recipient:party-001"],
            ["conversation:conv-001", "source-message:msg-001", "file:file-001"],
            "policy-snap-001",
            nameof(ExecuteApprovedOutboundDraft),
            "chatbot-spine.v1",
            SenderAuthorityClass.AuthenticatedUserSend,
            ApprovalEvidenceFreshness.Fresh,
            3,
            1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static HttpRequestMessage AssociationCorrectionSubmissionRequest()
    {
        CorrectEmailProjectAssociation command = new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FBZ",
            "project-alpha",
            "project-beta",
            AssociationCorrectionKind.ProjectReassignment,
            "Safe metadata-only correction rationale.",
            "01ARZ3NDEKTSV4RRFFQ69G5FAA",
            "evidence:subject-match:sha256",
            9,
            "chatbot.association-correction-command.v1");
        object payload = new
        {
            commandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            commandType = nameof(CorrectEmailProjectAssociation),
            origin = "ui",
            command,
            requestSchemaVersion = "v1",
        };

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static HttpRequestMessage MailboxIntakeSubmissionRequest(string commandId, string intakeId)
    {
        string payload =
            $$"""
            {
              "commandId": "{{commandId}}",
              "commandType": "CaptureMailboxMessageIntake",
              "origin": "mailbox",
              "command": {
                "intakeId": "{{intakeId}}",
                "source": {
                  "providerMessageId": "graph-message-001",
                  "internetMessageId": "<message-001@example.test>",
                  "conversationId": "graph-conversation-001",
                  "threadId": "graph-thread-001",
                  "mailboxId": "controlled-mailbox-001",
                  "sender": {
                    "address": "sender@example.test",
                    "displayName": "Sender"
                  },
                  "receivedAt": "2026-05-30T10:15:00+00:00",
                  "sentAt": "2026-05-30T10:10:00+00:00",
                  "createdAt": "2026-05-30T10:05:00+00:00",
                  "sourceTimezone": "UTC",
                  "sourceContext": "graph-message-v1",
                  "sourceSchemaVersion": 1
                },
                "recipients": [
                  {
                    "address": "project@example.test",
                    "displayName": "Project",
                    "kind": "to"
                  }
                ],
                "attachments": [
                  {
                    "providerAttachmentId": "attachment-001",
                    "name": "evidence.pdf",
                    "contentType": "application/pdf",
                    "sizeInBytes": 1024
                  }
                ]
              },
              "requestSchemaVersion": "v1"
            }
            """;

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        return request;
    }

    private static HttpRequestMessage AiActionProposalSubmissionRequest()
    {
        ProposeAIAction command = new(
            "project-001",
            "task-intent-001",
            "graph-message-001",
            "requester-001",
            "Project.AppendConversationMessage",
            "project-conversation",
            8,
            ["message:offset:001"],
            ["project:project-001"],
            [],
            "policy-snapshot-4-3",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "transition-001",
            SourceConversationItemId: "conversation-item-001",
            ProposedActionClasses:
            [
                AiActionRiskActionClass.InvokesTools,
                AiActionRiskActionClass.ExposesFiles,
                AiActionRiskActionClass.ModifiesState,
                AiActionRiskActionClass.ActsOnBehalf,
                AiActionRiskActionClass.CreatesTasks,
                AiActionRiskActionClass.SendsExternal,
            ]);
        object payload = new
        {
            commandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            commandType = nameof(ProposeAIAction),
            origin = "ui",
            command,
            requestSchemaVersion = "v1",
        };

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static HttpRequestMessage LowRiskAiExecutionSubmissionRequest()
    {
        ExecuteLowRiskAIAssistance command = new(
            "project-001",
            "ai-proposal-001",
            "task-intent-001",
            "graph-message-001",
            "party-001",
            LowRiskAiAssistanceKind.SummarizeVisibleContext,
            "context-package-001",
            "v1",
            "metadata_only",
            "collaboration_input",
            "disabled",
            ["evidence-message-001", "evidence-attachment-001"],
            ["evidence-message-001", "evidence-attachment-001"],
            ["redacted", "policy-denied"],
            8,
            "policy-snap-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "ai-execution-001",
            "transition-001",
            SourceConversationItemId: "conversation-item-001");
        object payload = new
        {
            commandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            commandType = nameof(ExecuteLowRiskAIAssistance),
            origin = "ui",
            command,
            requestSchemaVersion = "v1",
        };

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static HttpRequestMessage ApprovedAiExecutionSubmissionRequest(
        string commandName = "Project.AppendConversationMessage",
        string commandAllowlistVersion = "ai-action-command-allowlist.m0")
    {
        ExecuteApprovedAIAction command = new(
            "project-001",
            "ai-proposal-001",
            "approval:ai-proposal-001",
            "task-intent-001",
            "graph-message-001",
            "party-001",
            commandName,
            commandAllowlistVersion,
            10,
            9,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "ai-approved-execution-001",
            "approved-execution-transition-001",
            ["evidence-message-001"],
            ["project:project-001"],
            ["party-001"],
            SourceConversationItemId: "conversation-item-001",
            PolicySnapshotId: "policy-snap-001");
        object payload = new
        {
            commandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            commandType = nameof(ExecuteApprovedAIAction),
            origin = "ui",
            command,
            requestSchemaVersion = "v1",
        };

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static HttpRequestMessage ParticipantResolutionSubmissionRequest()
    {
        const string payload =
            """
            {
              "commandId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "commandType": "ResolveMailboxMessageParticipants",
              "origin": "mailbox",
              "command": {
                "resolutionId": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                "intakeId": "01ARZ3NDEKTSV4RRFFQ69G5FBZ",
                "sourceMailboxId": "controlled-mailbox-001",
                "sourceParticipants": [
                  {
                    "sourceParticipantId": "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    "role": "sender",
                    "evidenceReference": "mailbox:intake:sender",
                    "evidenceFingerprint": "evidence-sha256-sender",
                    "addressEvidence": "sender@example.test",
                    "displayNameEvidence": "Sender Raw"
                  },
                  {
                    "sourceParticipantId": "01ARZ3NDEKTSV4RRFFQ69G5FBA",
                    "role": "to",
                    "evidenceReference": "mailbox:intake:recipient:0",
                    "evidenceFingerprint": "evidence-sha256-recipient",
                    "addressEvidence": "unresolved@example.test",
                    "displayNameEvidence": "Unresolved Raw"
                  }
                ],
                "resolvedParticipants": [],
                "unresolvedParticipants": [],
                "resolutionKernelVersion": "participant-resolution.kernel.v1"
              },
              "requestSchemaVersion": "v1"
            }
            """;

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        return request;
    }

    private static HttpRequestMessage AssociationScoringSubmissionRequest(AssociationDeterministicSignal signal)
    {
        ScoreMailboxMessageAssociation command = new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAB",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "controlled-mailbox-001",
            "conversation-001",
            "thread-001",
            [signal],
            null,
            [],
            [],
            null,
            string.Empty,
            ExternalSender: null,
            StrictnessPolicy: new MailboxAuthenticityStrictnessPolicySnapshot(
                MailboxAuthenticityStrictness.Permissive,
                "mailbox-authenticity-strictness.m0.v1",
                "tenant-policy"),
            Authenticity: null);
        object payload = new
        {
            commandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            commandType = nameof(ScoreMailboxMessageAssociation),
            origin = "mailbox",
            command,
            requestSchemaVersion = "v1",
        };

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static IReadOnlyList<Claim> ServiceClientGrantClaims(string surface, string allowedCommand)
        =>
        [
            new Claim("preferred_username", "service-account-cli-automation-client"),
            new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.ServiceActorValue),
            new Claim(ClaimsServiceClientGrantResolver.ServiceClientIdClaim, "cli-automation-client"),
            new Claim(ClaimsServiceClientGrantResolver.ServiceClientClassClaim, "cli-automation"),
            new Claim(ClaimsServiceClientGrantResolver.GrantIdClaim, "01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new Claim(ClaimsServiceClientGrantResolver.GrantTenantClaim, "tenant-alpha"),
            new Claim(ClaimsServiceClientGrantResolver.GrantExpiryClaim, "2099-06-01T13:00:00Z"),
            new Claim(ClaimsServiceClientGrantResolver.GrantScopeClaim, "notes.write"),
            new Claim(ClaimsServiceClientGrantResolver.GrantSurfaceClaim, surface),
            new Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, allowedCommand),
            new Claim(ClaimsServiceClientGrantResolver.CommandSetVersionClaim, "command-set-v1"),
            new Claim(ClaimsServiceClientGrantResolver.DelegatedUserIdClaim, "actor-alpha"),
            new Claim(ClaimsServiceClientGrantResolver.OAuthGrantEvidenceFingerprintClaim, "oauth-proof-01ARZ3NDEKTSV4RRFFQ69G5FAV"),
        ];

    private static IReadOnlyList<Claim> AiActorGrantClaims(string surface, string allowedCommand)
        =>
        [
            new Claim("preferred_username", "ai-gpt-mediation-actor"),
            new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.AiActorValue),
            new Claim(ClaimsServiceClientGrantResolver.ServiceClientIdClaim, "gpt-mediation-actor"),
            new Claim(ClaimsServiceClientGrantResolver.ServiceClientClassClaim, "ai-action-execution"),
            new Claim(ClaimsServiceClientGrantResolver.GrantIdClaim, "01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new Claim(ClaimsServiceClientGrantResolver.GrantTenantClaim, "tenant-alpha"),
            new Claim(ClaimsServiceClientGrantResolver.GrantExpiryClaim, "2099-06-01T13:00:00Z"),
            new Claim(ClaimsServiceClientGrantResolver.GrantScopeClaim, "notes.write"),
            new Claim(ClaimsServiceClientGrantResolver.GrantSurfaceClaim, surface),
            new Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, allowedCommand),
            new Claim(ClaimsServiceClientGrantResolver.CommandSetVersionClaim, "command-set-v1"),
            new Claim(ClaimsServiceClientGrantResolver.DelegatedUserIdClaim, "actor-alpha"),
            new Claim(ClaimsServiceClientGrantResolver.OAuthGrantEvidenceFingerprintClaim, "oauth-proof-01ARZ3NDEKTSV4RRFFQ69G5FAV"),
        ];

    private static IReadOnlyList<Claim> OutboundDraftAuthorityClaims(
        bool includeProjectAuthority = true,
        bool includeOutboundDraftScope = true,
        bool includeTenantPolicy = true)
    {
        List<Claim> claims = [];
        if (includeProjectAuthority)
        {
            claims.Add(new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, "project-001"));
        }

        if (includeOutboundDraftScope)
        {
            claims.Add(new Claim(OutboundDraftAuthorityEvaluator.ProjectScopeClaim, "project-001:outbound-draft"));
        }

        if (includeTenantPolicy)
        {
            claims.Add(new Claim(OutboundDraftAuthorityEvaluator.TenantOutboundPolicyClaim, "draft-only"));
        }

        return claims;
    }

    private static IReadOnlyList<Claim> OutboundSendAuthorityClaims()
        =>
        [
            new Claim("requester_authority_class", "project-approver"),
            new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, "project-001"),
            new Claim(OutboundDraftAuthorityEvaluator.ProjectScopeClaim, "project-001:outbound-send"),
            new Claim(OutboundDraftAuthorityEvaluator.TenantOutboundPolicyClaim, "authenticated-user-send"),
            new Claim(OutboundSendAuthorityEvaluator.MailboxIdClaim, "mailbox-001"),
            new Claim(OutboundSendAuthorityEvaluator.MailboxOwnerClaim, "mailbox-001"),
            new Claim(OutboundSendAuthorityEvaluator.OwnMailboxMailSendClaim, "true"),
        ];

    private static AssociationDeterministicSignal AssociationSignal(string projectId, double weight)
        => new(
            AssociationSignalClass.ExplicitProjectIdentifier,
            projectId,
            "mailbox:project-id",
            "hash-project",
            weight,
            RequiredForAutoAssociation: true);

    private sealed class TestPrincipalStartupFilter(
        string tenantId,
        string? participantAuthority = null,
        IReadOnlyCollection<string>? projectOwners = null,
        string? principalSubject = null,
        IReadOnlyCollection<Claim>? additionalClaims = null) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(
                    async (context, continuation) =>
                    {
                        List<Claim> claims =
                        [
                            new Claim("sub", string.IsNullOrWhiteSpace(principalSubject) ? "actor-alpha" : principalSubject),
                            new Claim("eventstore:tenant", tenantId),
                            new Claim("party", "party-alpha"),
                            new Claim("email", "sender@example.test"),
                        ];
                        if (additionalClaims?.Any(static claim =>
                            string.Equals(claim.Type, "requester_authority_class", StringComparison.Ordinal)) != true)
                        {
                            claims.Add(new Claim("requester_authority_class", "project-contributor"));
                        }

                        if (additionalClaims?.Any(static claim =>
                            string.Equals(claim.Type, ParticipantAuthorizationStage.ActorTypeClaim, StringComparison.Ordinal)) != true)
                        {
                            claims.Add(new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue));
                        }

                        foreach (string projectOwner in projectOwners is { Count: > 0 } ? projectOwners : ["project-alpha"])
                        {
                            claims.Add(new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, projectOwner));
                        }

                        if (!string.IsNullOrWhiteSpace(participantAuthority))
                        {
                            claims.Add(new Claim(ParticipantAuthorizationStage.ParticipantAuthorityClaim, participantAuthority));
                        }

                        if (additionalClaims is { Count: > 0 })
                        {
                            claims.AddRange(additionalClaims);
                        }

                        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
                        await continuation().ConfigureAwait(false);
                    });
                next(app);
            };
    }

    private sealed class FixedAssociationCorrectionDependencyReadiness(AssociationCorrectionDependencyReadinessStatus status)
        : IAssociationCorrectionDependencyReadiness
    {
        public AssociationCorrectionDependencyReadinessStatus Status { get; } = status;

        public bool IsProjectionInvalidationReady => Status.IsProjectionInvalidationReady;
    }

    private sealed class RecordingWorkflowRuntime : ICorrectionPropagationWorkflowRuntime
    {
        public List<CorrectionPropagationRequest> Scheduled { get; } = [];

        public bool IsAvailable => true;

        public ValueTask ScheduleAsync(CorrectionPropagationRequest request, CancellationToken cancellationToken)
        {
            Scheduled.Add(request);
            return ValueTask.CompletedTask;
        }

        public ValueTask<CorrectionPropagationWorkflowRuntimeStatus> CheckAsync(CancellationToken cancellationToken)
            => ValueTask.FromResult(new CorrectionPropagationWorkflowRuntimeStatus(
                true,
                "available",
                CorrectionPropagationWorkflowFailureCodes.None,
                DateTimeOffset.UtcNow));
    }

    private sealed class RecordingParticipantDirectory : IParticipantDirectory
    {
        private readonly List<ParticipantDirectoryLookup> _lookups = [];

        public IReadOnlyList<ParticipantDirectoryLookup> Lookups => _lookups;

        public ValueTask<ParticipantDirectoryResolution> ResolveEmailEvidenceAsync(
            ParticipantDirectoryLookup lookup,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _lookups.Add(lookup);
            if (string.Equals(lookup.SourceParticipantId, "01ARZ3NDEKTSV4RRFFQ69G5FAZ", StringComparison.Ordinal))
            {
                return ValueTask.FromResult(ParticipantDirectoryResolution.FromResolved(
                    new ResolvedMailboxParticipantReference(
                        lookup.SourceParticipantId,
                        "tenant-alpha:parties:party-001",
                        "tenant-alpha",
                        lookup.EvidenceReference,
                        lookup.EvidenceFingerprint,
                        ParticipantResolutionStatus.Resolved)));
            }

            return ValueTask.FromResult(ParticipantDirectoryResolution.FromUnresolved(
                lookup,
                ParticipantResolutionBlockedReason.NotFound));
        }
    }

    private sealed class RecordingProjectDirectory(ProjectDirectoryAssociationResult result) : IProjectDirectory
    {
        public ProjectDirectoryAssociationRequest? Request { get; private set; }

        public ValueTask<ProjectDirectoryAssociationResult> FindAuthorizedCandidatesAsync(
            ProjectDirectoryAssociationRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Request = request;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FixedTenantAiPolicySnapshotProvider(bool lowRiskAllowed) : ITenantAiPolicySnapshotProvider
    {
        public ValueTask<TenantAiPolicySnapshot?> TryGetAsync(
            string tenantId,
            string projectId,
            string? requestedPolicySnapshotId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TenantAiPolicySnapshot snapshot = new(
                string.IsNullOrWhiteSpace(requestedPolicySnapshotId) ? "policy-snap-001" : requestedPolicySnapshotId,
                lowRiskAllowed,
                "read-only",
                ["summarize-visible-context", "explain-visible-evidence"],
                IsFresh: true,
                IsValid: true);
            return ValueTask.FromResult<TenantAiPolicySnapshot?>(snapshot);
        }
    }

    private sealed class RecordingAiAssistanceProvider : IAiAssistanceProvider
    {
        public int ExecuteCount { get; private set; }

        public AiAssistanceProviderRequest? LastRequest { get; private set; }

        public ValueTask<LowRiskAiAssistanceExecutionRecord> ExecuteAsync(
            AiAssistanceProviderRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExecuteCount++;
            LastRequest = request;
            return ValueTask.FromResult(new LowRiskAiAssistanceExecutionRecord(
                request.ExecutionId,
                request.ProposalId,
                request.AssistanceKind,
                "success",
                "deterministic-test",
                "deterministic-test.v1",
                new DateTimeOffset(2026, 6, 1, 8, 20, 40, TimeSpan.Zero),
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
                "summary_available",
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
            cancellationToken.ThrowIfCancellationRequested();
            PrepareCount++;
            LastRequest = request;
            return ValueTask.FromResult(new ConversationAppendResult(
                "success",
                "available",
                "metadata_only",
                "none"));
        }
    }

    private sealed class RecordingEventStoreGatewayClient : IEventStoreGatewayClient
    {
        private readonly List<SubmitCommandRequest> _submitted = [];

        public IReadOnlyList<SubmitCommandRequest> Submitted => _submitted;

        public Task<SubmitCommandResponse> SubmitCommandAsync(
            SubmitCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            _submitted.Add(request);
            return Task.FromResult(new SubmitCommandResponse(request.CorrelationId ?? request.MessageId));
        }

        public Task<EventStoreQueryResult> SubmitQueryAsync(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StreamReadPage> ReadStreamAsync(
            StreamReadRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingDispatcher : ICommandDispatcher
    {
        public int DispatchCount { get; private set; }

        public ValueTask<ChatBotDispatchResult> DispatchAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DispatchCount++;
            return ValueTask.FromResult(new ChatBotDispatchResult(new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero)));
        }
    }

    private sealed class RecordingAuditWriter : IAuditWriter
    {
        private readonly List<ChatBotAuthorizationFailureAuditFact> _authorizationFailures = [];
        private readonly List<AuditEnvelope> _envelopes = [];

        public IReadOnlyList<ChatBotAuthorizationFailureAuditFact> AuthorizationFailures => _authorizationFailures;

        public IReadOnlyList<AuditEnvelope> Envelopes => _envelopes;

        public AuditWriteResult PreCommitResult { get; init; } = AuditWriteResult.Success;

        public AuditWriteResult PostCommitResult { get; init; } = AuditWriteResult.Success;

        public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _authorizationFailures.Add(fact);
            return ValueTask.CompletedTask;
        }

        public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _envelopes.Add(envelope);
            return ValueTask.FromResult(PreCommitResult);
        }

        public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _envelopes.Add(envelope);
            return ValueTask.FromResult(PostCommitResult);
        }
    }

    private sealed class AllowAllSpineCommandAllowlist : ISpineCommandAllowlist
    {
        public bool IsAllowed(string? commandType) => true;
    }

    private sealed class DenyAllSpineCommandAllowlist : ISpineCommandAllowlist
    {
        public bool IsAllowed(string? commandType) => false;
    }

    private sealed class FixedLifecycleTransitionGuard(LifecycleTransitionValidation result) : ILifecycleTransitionGuard
    {
        public LifecycleTransitionValidation ValidateCommandSubmission(ChatBotGatewayContext context)
            => result;

        public LifecycleTransitionValidation ResolveSkipTransition(LifecycleSkipTrigger trigger)
            => LifecycleTransitionValidation.Valid(new LifecycleTransitionDefinition("Received", "Skipped"));
    }

    private sealed class FixedServiceClientControlStateProvider(ContractServiceClientControlState state)
        : IServiceClientControlStateProvider
    {
        public ValueTask<ContractServiceClientControlState> GetControlStateAsync(
            string tenantId,
            string serviceClientId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(state);
        }
    }

    private sealed class FixedAiActorControlStateProvider(ContractAiActorControlState state)
        : IAiActorControlStateProvider
    {
        public ValueTask<ContractAiActorControlState> GetControlStateAsync(
            string tenantId,
            string aiActorRef,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(state);
        }
    }

    private sealed class FixedCommandCapabilityControlStateProvider(
        string disabledTenantId,
        string disabledCommandCapabilityRef) : ICommandCapabilityControlStateProvider
    {
        private readonly List<(string TenantId, string CommandCapabilityRef)> _requests = [];

        public IReadOnlyList<(string TenantId, string CommandCapabilityRef)> Requests => _requests;

        public ValueTask<ContractCommandCapabilityControlState> GetControlStateAsync(
            string tenantId,
            string commandCapabilityRef,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _requests.Add((tenantId, commandCapabilityRef));
            ContractCommandCapabilityControlState state =
                string.Equals(tenantId, disabledTenantId, StringComparison.Ordinal) &&
                string.Equals(commandCapabilityRef, disabledCommandCapabilityRef, StringComparison.Ordinal)
                    ? ContractCommandCapabilityControlState.Disabled
                    : ContractCommandCapabilityControlState.Active;
            return ValueTask.FromResult(state);
        }
    }

    private sealed class FixedAiActorRateLimitProvider(AiActorRateLimitState state) : IAiActorRateLimitProvider
    {
        public ValueTask<AiActorRateLimitState?> GetRateLimitAsync(
            string tenantId,
            string aiActorId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<AiActorRateLimitState?>(state);
        }
    }

    private sealed class FixedAiActorProposalHistory(IReadOnlyList<DateTimeOffset> recentAdmitted)
        : IAiActorProposalHistory
    {
        public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
            string tenantId,
            string aiActorId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(recentAdmitted);
        }
    }

    private sealed class FixedCommandCapabilityRateLimitProvider(
        string configuredTenantId,
        string configuredCommandCapabilityRef,
        CommandCapabilityRateLimitState state) : ICommandCapabilityRateLimitProvider
    {
        public ValueTask<CommandCapabilityRateLimitState?> GetRateLimitAsync(
            string tenantId,
            string commandCapabilityRef,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CommandCapabilityRateLimitState? configured =
                string.Equals(tenantId, configuredTenantId, StringComparison.Ordinal) &&
                string.Equals(commandCapabilityRef, configuredCommandCapabilityRef, StringComparison.Ordinal)
                    ? state
                    : null;
            return ValueTask.FromResult(configured);
        }
    }

    private sealed class FixedCommandCapabilityCommandHistory(IReadOnlyList<DateTimeOffset> recentAdmitted)
        : ICommandCapabilityCommandHistory
    {
        public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
            string tenantId,
            string commandCapabilityRef,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(recentAdmitted);
        }
    }

    private sealed class AlwaysConflictingIdempotencyStore : IIdempotencyStore
    {
        private static readonly DateTimeOffset ExpiresAt = new(2026, 6, 1, 8, 1, 0, TimeSpan.Zero);

        public ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(
            ChatBotGatewayContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CoarseIdempotencyMetadata metadata = new(
                CoarseIdempotencyOperationClass.CommandExecution.Code,
                new string('a', 64),
                new string('b', 64),
                ExpiresAt);
            context.SetIdempotency(metadata);
            return ValueTask.FromResult(CoarseIdempotencyDecision.Conflict(metadata));
        }

        public ValueTask RecordOutcomeAsync(
            CoarseIdempotencyMetadata metadata,
            Hexalith.ChatBot.Client.Generated.CommandSubmissionResponse outcome,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
