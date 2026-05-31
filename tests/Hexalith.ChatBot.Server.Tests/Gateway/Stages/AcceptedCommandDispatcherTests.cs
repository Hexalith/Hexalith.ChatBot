using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
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
}
