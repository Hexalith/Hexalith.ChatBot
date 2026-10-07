using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Adapters.AiProvider;
using Hexalith.ChatBot.Server.Adapters.Projects;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Association.Scoring;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.Conversations;
using Hexalith.ChatBot.Server.Lifecycle.AiExecution;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Queries;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using Shouldly;

using ContractAssociationCandidate = Hexalith.ChatBot.Contracts.Commands.AssociationCandidate;
using ContractAssociationConfidenceInput = Hexalith.ChatBot.Contracts.Commands.AssociationConfidenceInput;
using ContractAssociationDecisionKind = Hexalith.ChatBot.Contracts.Enums.AssociationDecisionKind;
using ContractAssociationEvidenceReference = Hexalith.ChatBot.Contracts.Commands.AssociationEvidenceReference;
using ContractAssociationExclusion = Hexalith.ChatBot.Contracts.Commands.AssociationExclusion;
using ContractAssociationExclusionState = Hexalith.ChatBot.Contracts.Enums.AssociationExclusionState;
using ContractAssociationReasonCode = Hexalith.ChatBot.Contracts.Enums.AssociationReasonCode;
using ContractAssociationScoringOutcome = Hexalith.ChatBot.Contracts.Enums.AssociationScoringOutcome;
using ContractAssociationSignalClass = Hexalith.ChatBot.Contracts.Enums.AssociationSignalClass;
using ContractAssociationThresholdBand = Hexalith.ChatBot.Contracts.Enums.AssociationThresholdBand;
using ContractMailboxAuthenticityStrictness = Hexalith.ChatBot.Contracts.Enums.MailboxAuthenticityStrictness;
using ContractMailboxPartyResolutionState = Hexalith.ChatBot.Contracts.Enums.MailboxPartyResolutionState;
using ContractApprovalEventKind = Hexalith.ChatBot.Contracts.Enums.ApprovalEventKind;
using ContractApprovalStatus = Hexalith.ChatBot.Contracts.Enums.ApprovalStatus;
using ContractLifecycleState = Hexalith.ChatBot.Contracts.Enums.LifecycleState;
using ContractProjectConversationAttachmentStatus = Hexalith.ChatBot.Contracts.Enums.ProjectConversationAttachmentStatus;
using ContractProjectConversationActorKind = Hexalith.ChatBot.Contracts.Enums.ProjectConversationActorKind;
using ContractProjectConversationDetectedActionKind = Hexalith.ChatBot.Contracts.Enums.ProjectConversationDetectedActionKind;
using ContractProjectConversationItemKind = Hexalith.ChatBot.Contracts.Enums.ProjectConversationItemKind;
using ContractScoreMailboxMessageAssociation = Hexalith.ChatBot.Contracts.Commands.ScoreMailboxMessageAssociation;
using ContractTaskIntentRecord = Hexalith.ChatBot.Contracts.Queries.TaskIntentRecord;
using ContractTaskIntentSourceEvidenceOffset = Hexalith.ChatBot.Contracts.Queries.TaskIntentSourceEvidenceOffset;
using ContractTaskIntentState = Hexalith.ChatBot.Contracts.Enums.TaskIntentState;

namespace Hexalith.ChatBot.Server.Tests;

public sealed class ServerBootstrapApiTests
{
    [Fact]
    public async Task DomainQueryDispatcherShouldDispatchRegisteredChatBotReadHandlers()
    {
        InMemoryAssociationProjectionStore associationStore = new();
        await associationStore
            .SaveAsync(AssociationRoutingViewWithEvidence(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.ConfigureServices(
                    services => services.AddSingleton<IAssociationProjectionStore>(associationStore)));

        using IServiceScope scope = factory.Services.CreateScope();
        IEnumerable<IDomainQueryHandler> handlers = scope.ServiceProvider.GetServices<IDomainQueryHandler>();
        handlers.ShouldContain(handler => handler.QueryType == ChatBotReadQueryTypes.AssociationRoutingStatus);
        QueryEnvelope envelope = new(
            "tenant-alpha",
            ChatBotReadQueryTypes.Domain,
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            ChatBotReadQueryTypes.AssociationRoutingStatus,
            JsonSerializer.SerializeToUtf8Bytes(new AssociationRoutingStatusQuery("01ARZ3NDEKTSV4RRFFQ69G5FAV", null), Program.QueryJsonOptions),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "actor-alpha");

        QueryResult result = await DomainQueryDispatcher
            .ExecuteAsync(scope.ServiceProvider, envelope, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Success.ShouldBeTrue();
        using JsonDocument document = JsonDocument.Parse(result.PayloadBytes.ShouldNotBeNull());
        document.RootElement.GetProperty("associationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
    }

    [Fact]
    public async Task DomainProjectionDispatcherShouldDispatchRegisteredChatBotProjectionHandler()
    {
        using WebApplicationFactory<Program> factory = new();
        using IServiceScope scope = factory.Services.CreateScope();

        ProjectionRequest request = new(
            "tenant-alpha",
            "chatbot",
            "note-alpha",
            [
                new ProjectionEventDto(
                    GovernedOperationProjectionTranslator.GovernedNoteRecordedEventType,
                    [],
                    "json",
                    1,
                    new DateTimeOffset(2026, 6, 12, 9, 0, 0, TimeSpan.Zero),
                    "correlation-alpha",
                    "message-alpha"),
            ]);

        ProjectionResponse? response = DomainProjectionDispatcher.Project(scope.ServiceProvider, request);

        response.ShouldNotBeNull();
        response.ProjectionType.ShouldBe("chatbot");
        response.State.GetProperty("appliedEventCount").GetInt32().ShouldBe(1);
        response.State.GetProperty("ignoredEventCount").GetInt32().ShouldBe(0);
        IGovernedOperationProjectionStore store = scope.ServiceProvider.GetRequiredService<IGovernedOperationProjectionStore>();
        GovernedOperationView view = (await store
            .GetAsync("tenant-alpha", "note-alpha", TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ShouldNotBeNull();
        view.SourceVersion.ShouldBe(1);
    }

    [Fact]
    public async Task DomainProjectionDispatcherShouldRequireFlatAiResponseCancellationConfirmationForTerminalState()
    {
        // Regression: AiResponseGenerationCancellationRequested is a flat domain event. The projection handler
        // must materialize it into PublishedTaskIntentEvent.AiResponseCancellation from its raw payload and project
        // the server-verified request and separate confirmation state. A direct TaskIntentProjectionHandler test does not exercise
        // this wire path, which previously left the cancellation branch as dead code.
        using WebApplicationFactory<Program> factory = new();
        using IServiceScope scope = factory.Services.CreateScope();

        Hexalith.ChatBot.Server.Governance.Conversations.AiResponseGenerationCancellationRequested cancellation = new(
            "tenant-alpha",
            "project-alpha",
            "conversation-alpha",
            "response-alpha",
            "generation-alpha",
            "actor-alpha",
            12,
            "correlation-alpha",
            "ai-response-cancel-alpha",
            new DateTimeOffset(2026, 6, 19, 9, 0, 0, TimeSpan.Zero),
            "metadata_only",
            "chatbot.ai-response-cancel.v1",
            13,
            "wait-for-executor");
        Hexalith.ChatBot.Server.Governance.Conversations.AiResponseGenerationCancellationConfirmed confirmation = new(
            "tenant-alpha",
            "project-alpha",
            "conversation-alpha",
            "response-alpha",
            "generation-alpha",
            "ai-response-cancel-alpha",
            "correlation-alpha",
            cancellation.RequestedAtUtc.AddSeconds(1),
            "metadata_only",
            "chatbot.ai-response-cancellation-completion.v1",
            "none");

        ProjectionRequest request = new(
            "tenant-alpha",
            "chatbot",
            "ai-response-cancel-alpha",
            [
                new ProjectionEventDto(
                    typeof(Hexalith.ChatBot.Server.Governance.Conversations.AiResponseGenerationCancellationRequested).FullName!,
                    JsonSerializer.SerializeToUtf8Bytes(cancellation, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    "json",
                    13,
                    cancellation.RequestedAtUtc,
                    "correlation-alpha",
                    "message-cancel-alpha"),
                new ProjectionEventDto(
                    typeof(Hexalith.ChatBot.Server.Governance.Conversations.AiResponseGenerationCancellationConfirmed).FullName!,
                    JsonSerializer.SerializeToUtf8Bytes(confirmation, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    "json",
                    14,
                    confirmation.ConfirmedAtUtc,
                    "correlation-alpha",
                    "message-cancel-confirmed-alpha"),
            ]);

        ProjectionResponse? response = DomainProjectionDispatcher.Project(scope.ServiceProvider, request);

        response.ShouldNotBeNull();
        response.State.GetProperty("appliedEventCount").GetInt32().ShouldBe(2);
        response.State.GetProperty("ignoredEventCount").GetInt32().ShouldBe(0);

        IProjectConversationProjectionStore conversationStore = scope.ServiceProvider.GetRequiredService<IProjectConversationProjectionStore>();
        ProjectConversationItemView item = (await conversationStore
                .ReadPageAsync("tenant-alpha", "project-alpha", null, 25, TestContext.Current.CancellationToken)
                .ConfigureAwait(true))
            .Items
            .OrderByDescending(static candidate => candidate.SourceVersion)
            .First();
        Hexalith.ChatBot.Contracts.Queries.AiResponseProgress progress = item.BuildAiResponseProgress().ShouldNotBeNull();
        progress.State.ShouldBe(Hexalith.ChatBot.Contracts.Enums.AiResponseProgressState.Stopped);
        progress.TerminalReason.ShouldBe(Hexalith.ChatBot.Contracts.Enums.AiResponseTerminalReason.UserStopped);
        progress.IsTerminal.ShouldBeTrue();
    }

    [Fact]
    public async Task DomainProjectionDispatcherShouldMaterializeFlatTaskIntentConversionAsAiProposal()
    {
        // Regression: EventStore sends the concrete conversion payload (`taskIntent` + `proposal`). The public
        // projection adapter envelope uses `record` + `proposal`, so generic JSON deserialization silently left
        // Record null and acknowledged the durable conversion without producing its conversation item.
        using WebApplicationFactory<Program> factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        DateTimeOffset occurredAt = new(2026, 6, 19, 9, 1, 0, TimeSpan.Zero);
        ContractTaskIntentRecord converted = ProjectConversationTaskIntentRecord() with
        {
            State = ContractTaskIntentState.Converted,
            ConvertedProposalId = "proposal-domain-wire",
            ReviewerActorId = "actor-alpha",
            DecidedAtUtc = occurredAt,
            AuditOperationId = "audit-domain-wire",
            TransitionId = "transition-domain-wire",
            ReasonCode = "task_intent_converted",
            SafeNextAction = "review-ai-action",
        };
        Hexalith.ChatBot.Contracts.Queries.AiActionProposalRecord proposal = new(
            "proposal-domain-wire",
            converted.TaskIntentId,
            converted.SourceMessageId,
            converted.SourceMessageId,
            converted.RequesterPartyId,
            "actor-alpha",
            ["message:offset:001"],
            "CreateProjectTask",
            "project-task",
            ["project:project-alpha"],
            [],
            converted.PolicySnapshotId,
            converted.SourceVersion,
            converted.CorrelationId,
            "metadata_only",
            "collaboration_input",
            "chatbot.ai-action-proposal.v1",
            "review-ai-action");
        TaskIntentConvertedToAiActionProposal domainEvent = new(
            converted,
            proposal,
            "actor-alpha",
            occurredAt,
            "audit-domain-wire");
        ProjectionRequest request = new(
            "tenant-alpha",
            "chatbot",
            "project-alpha",
            [
                new ProjectionEventDto(
                    typeof(TaskIntentConvertedToAiActionProposal).FullName!,
                    JsonSerializer.SerializeToUtf8Bytes(domainEvent, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    "json",
                    42,
                    occurredAt,
                    converted.CorrelationId,
                    "message-domain-wire"),
            ]);

        ProjectionResponse response = DomainProjectionDispatcher.Project(scope.ServiceProvider, request).ShouldNotBeNull();

        response.State.GetProperty("appliedEventCount").GetInt32().ShouldBe(1);
        response.State.GetProperty("ignoredEventCount").GetInt32().ShouldBe(0);
        IProjectConversationProjectionStore conversationStore = scope.ServiceProvider.GetRequiredService<IProjectConversationProjectionStore>();
        ProjectConversationItemView projectedProposal = (await conversationStore
                .ReadPageAsync("tenant-alpha", "project-alpha", null, 25, TestContext.Current.CancellationToken)
                .ConfigureAwait(true))
            .Items
            .Single(static item => item.Kind == ContractProjectConversationItemKind.AiOutcome);
        projectedProposal.AiOutcomeKind.ShouldBe(Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.Proposal);
        projectedProposal.AiOutcomeStatus.ShouldBe(Hexalith.ChatBot.Contracts.Enums.AiOutcomeStatus.Proposed);
        projectedProposal.AiProposalId.ShouldBe("proposal-domain-wire");
    }

    [Fact]
    public async Task DomainProjectionDispatcherShouldCaptureAuthoritativeAppliedStartedVersion()
    {
        // This is the production EventStore wire path that makes Stop reachable. The coordinator consumes the
        // durable Started event independently; the projection must also adapt that same flat payload into an
        // executing AI outcome instead of acknowledging it with an empty Published* nested slot.
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(
                services => services.AddSingleton<IAiExecutionCoordinator, NoOpAiExecutionCoordinator>()));
        using IServiceScope scope = factory.Services.CreateScope();
        DateTimeOffset occurredAt = new(2026, 6, 19, 9, 2, 0, TimeSpan.Zero);
        LowRiskAiAssistanceExecutionStarted domainEvent = new(
            "execution-domain-wire",
            "proposal-domain-wire",
            "project-alpha",
            "task-intent:api",
            "graph-message-001",
            "party-001",
            "summarize",
            "context-domain-wire",
            "context-v1",
            "policy-001",
            "low-risk-policy",
            42,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            occurredAt,
            TenantId: "tenant-alpha",
            ConversationId: "project-alpha");
        ProjectionRequest request = new(
            "tenant-alpha",
            "chatbot",
            "project-alpha",
            [
                new ProjectionEventDto(
                    typeof(LowRiskAiAssistanceExecutionStarted).FullName!,
                    JsonSerializer.SerializeToUtf8Bytes(domainEvent, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    "json",
                    43,
                    occurredAt,
                    domainEvent.CorrelationId,
                    "message-execution-domain-wire"),
            ]);

        ProjectionResponse response = DomainProjectionDispatcher.Project(scope.ServiceProvider, request).ShouldNotBeNull();

        response.State.GetProperty("appliedEventCount").GetInt32().ShouldBe(1);
        response.State.GetProperty("ignoredEventCount").GetInt32().ShouldBe(0);
        IProjectConversationProjectionStore conversationStore = scope.ServiceProvider.GetRequiredService<IProjectConversationProjectionStore>();
        ProjectConversationItemView projectedStart = (await conversationStore
                .ReadPageAsync("tenant-alpha", "project-alpha", null, 25, TestContext.Current.CancellationToken)
                .ConfigureAwait(true))
            .Items
            .Single(static item => item.Kind == ContractProjectConversationItemKind.AiOutcome);
        projectedStart.AiOutcomeKind.ShouldBe(Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.ExecutionStarted);
        Hexalith.ChatBot.Contracts.Queries.AiResponseProgress progress = projectedStart.BuildAiResponseProgress().ShouldNotBeNull();
        progress.State.ShouldBe(Hexalith.ChatBot.Contracts.Enums.AiResponseProgressState.Rendering);
        progress.IsTerminal.ShouldBeFalse();
        progress.SourceVersion.ShouldBe(43);
        progress.StartedSourceVersion.ShouldBe(43);
    }

    [Fact]
    public async Task ProjectEndpointShouldDispatchRegisteredChatBotProjectionHandler()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        ProjectionRequest request = new(
            "tenant-alpha",
            "chatbot",
            "note-http",
            [
                new ProjectionEventDto(
                    GovernedOperationProjectionTranslator.GovernedNoteRecordedEventType,
                    [],
                    "json",
                    5,
                    new DateTimeOffset(2026, 6, 12, 9, 5, 0, TimeSpan.Zero),
                    "correlation-http",
                    "message-http"),
            ]);

        using HttpResponseMessage response = await client
            .PostAsJsonAsync("/project", request, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ProjectionResponse projection = (await response.Content
            .ReadFromJsonAsync<ProjectionResponse>(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ShouldNotBeNull();
        projection.ProjectionType.ShouldBe("chatbot");
        projection.State.GetProperty("appliedEventCount").GetInt32().ShouldBe(1);
        projection.State.GetProperty("ignoredEventCount").GetInt32().ShouldBe(0);

        IGovernedOperationProjectionStore store = factory.Services.GetRequiredService<IGovernedOperationProjectionStore>();
        GovernedOperationView view = (await store
            .GetAsync("tenant-alpha", "note-http", TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ShouldNotBeNull();
        view.SourceVersion.ShouldBe(5);
    }

    [Fact]
    public async Task ProjectEndpointShouldAcknowledgeUnsupportedEventsAsNoOp()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        ProjectionRequest request = new(
            "tenant-alpha",
            "chatbot",
            "note-unsupported",
            [
                new ProjectionEventDto(
                    "Hexalith.ChatBot.Unsupported",
                    [],
                    "json",
                    6,
                    new DateTimeOffset(2026, 6, 12, 9, 6, 0, TimeSpan.Zero),
                    "correlation-unsupported",
                    "message-unsupported"),
            ]);

        using HttpResponseMessage response = await client
            .PostAsJsonAsync("/project", request, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ProjectionResponse projection = (await response.Content
            .ReadFromJsonAsync<ProjectionResponse>(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ShouldNotBeNull();
        projection.State.GetProperty("appliedEventCount").GetInt32().ShouldBe(0);
        projection.State.GetProperty("ignoredEventCount").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task ProjectEndpointShouldReturnNotFoundForUnknownDomain()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        ProjectionRequest request = new(
            "tenant-alpha",
            "folders",
            "folder-alpha",
            []);

        using HttpResponseMessage response = await client
            .PostAsJsonAsync("/project", request, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public void ServerHostShouldExposeSdkCanonicalRoutesExactlyOnce()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient _ = factory.CreateClient();

        string[] routes = factory.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(static endpoint => endpoint.RoutePattern.RawText ?? string.Empty)
            .ToArray();

        foreach (string route in new[]
        {
            "/process",
            "/replay-state",
            "/query",
            "/project",
            "/admin/operational-index-metadata",
        })
        {
            routes.Count(candidate => string.Equals(candidate, route, StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
        }

        routes.Count(static candidate => string.Equals(candidate, "/api/v1/commands", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
    }

    [Fact]
    public async Task ProcessEndpointShouldRunChatBotAdmissionBeforeSdkProcessor()
    {
        InMemoryAuditWriter auditWriter = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<IAuditWriter>(auditWriter));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsJsonAsync("/process", DomainServiceRequest("RecordGovernedNote"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DomainServiceWireResult result = (await response.Content
            .ReadFromJsonAsync<DomainServiceWireResult>(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ShouldNotBeNull();
        result.IsRejection.ShouldBeFalse();
        result.Events.ShouldContain(static item => item.EventTypeName.EndsWith("GovernedNoteRecorded", StringComparison.Ordinal));
        auditWriter.Envelopes.ShouldContain(static envelope => envelope.Phase == AuditCommitPhase.PreCommit);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.Phase != AuditCommitPhase.PostCommit);
    }

    [Fact]
    public async Task ProcessEndpointShouldReturnTypedAdmissionRejectionAndSkipProcessorWhenAdmissionRejects()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<ISpineCommandAllowlist>(_ => new DenyAllSpineCommandAllowlist()));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsJsonAsync("/process", DomainServiceRequest("RecordGovernedNote"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DomainServiceWireResult result = (await response.Content
            .ReadFromJsonAsync<DomainServiceWireResult>(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ShouldNotBeNull();
        result.IsRejection.ShouldBeTrue();
        DomainServiceWireEvent rejection = result.Events.ShouldHaveSingleItem();
        rejection.EventTypeName.ShouldBe(typeof(ChatBotDomainServiceAdmissionRejected).FullName);
        using JsonDocument document = JsonDocument.Parse(rejection.Payload);
        JsonElement root = document.RootElement;
        root.GetProperty("ReasonCode").GetString().ShouldBe(ChatBotAuthorizationReasonCodes.CommandNotAllowlisted);
        root.GetProperty("CommandType").GetString().ShouldBe("RecordGovernedNote");
    }

    [Fact]
    public async Task ProcessEndpointShouldRejectUnauthenticatedEnvelopeBeforeSdkProcessor()
    {
        InMemoryAuditWriter auditWriter = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<IAuditWriter>(auditWriter));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsJsonAsync(
                "/process",
                DomainServiceRequest("RecordGovernedNote", userId: "actor alpha"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        DomainServiceWireResult result = await ReadDomainServiceResultAsync(response).ConfigureAwait(true);

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().EventTypeName.ShouldBe(typeof(ChatBotDomainServiceAdmissionRejected).FullName);
        AdmissionReason(result).ShouldBe(ChatBotAuthorizationReasonCodes.AuthenticationDenied);
        result.Events.ShouldNotContain(static item => item.EventTypeName.EndsWith("GovernedNoteRecorded", StringComparison.Ordinal));
        auditWriter.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthenticationDenied);
    }

    [Fact]
    public async Task ProcessEndpointShouldRejectCrossTenantEnvelopeBeforeSdkProcessor()
    {
        InMemoryAuditWriter auditWriter = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<IAuditWriter>(auditWriter));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsJsonAsync(
                "/process",
                DomainServiceRequest(
                    "RecordGovernedNote",
                    new { noteId = "01ARZ3NDEKTSV4RRFFQ69G5FAV", tenantId = "tenant-beta" }),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        DomainServiceWireResult result = await ReadDomainServiceResultAsync(response).ConfigureAwait(true);

        result.IsRejection.ShouldBeTrue();
        AdmissionReason(result).ShouldBe(ChatBotAuthorizationReasonCodes.TenantMismatch);
        result.Events.ShouldNotContain(static item => item.EventTypeName.EndsWith("GovernedNoteRecorded", StringComparison.Ordinal));
        auditWriter.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMismatch);
    }

    [Fact]
    public async Task ProcessEndpointShouldRejectInvalidLifecycleAndAbortAdmissionBeforeSdkProcessor()
    {
        InMemoryAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services =>
            {
                services.AddSingleton<IAuditWriter>(auditWriter);
                services.AddSingleton<IIdempotencyStore>(idempotencyStore);
                services.AddSingleton<ILifecycleTransitionGuard>(
                    new FixedLifecycleTransitionGuard(
                        LifecycleTransitionValidation.Invalid(new LifecycleTransitionDefinition("Received", "Associated"))));
            });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsJsonAsync("/process", DomainServiceRequest("RecordGovernedNote"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        DomainServiceWireResult result = await ReadDomainServiceResultAsync(response).ConfigureAwait(true);

        result.IsRejection.ShouldBeTrue();
        AdmissionReason(result).ShouldBe(LifecycleTransitionReasonCodes.InvalidTransition);
        result.Events.ShouldNotContain(static item => item.EventTypeName.EndsWith("GovernedNoteRecorded", StringComparison.Ordinal));
        auditWriter.Envelopes.ShouldHaveSingleItem().Decision.ShouldBe("reject");
        idempotencyStore.RecordCount.ShouldBe(0);
    }

    [Fact]
    public async Task ProcessEndpointShouldRejectIdempotencyConflictBeforeSdkProcessor()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<IIdempotencyStore>(new ConflictIdempotencyStore()));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsJsonAsync("/process", DomainServiceRequest("RecordGovernedNote"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        DomainServiceWireResult result = await ReadDomainServiceResultAsync(response).ConfigureAwait(true);

        result.IsRejection.ShouldBeTrue();
        AdmissionReason(result).ShouldBe(CoarseIdempotencyOperationClass.CommandExecution.ConflictCode);
        result.Events.ShouldNotContain(static item => item.EventTypeName.EndsWith("GovernedNoteRecorded", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProcessEndpointShouldAbortAcceptedCoarseAdmissionWhenNoSdkPostProcessHookExists()
    {
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<IIdempotencyStore>(idempotencyStore));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage firstResponse = await client
            .PostAsJsonAsync("/process", DomainServiceRequest("RecordGovernedNote"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage duplicateResponse = await client
            .PostAsJsonAsync("/process", DomainServiceRequest("RecordGovernedNote"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        DomainServiceWireResult first = await ReadDomainServiceResultAsync(firstResponse).ConfigureAwait(true);
        DomainServiceWireResult duplicate = await ReadDomainServiceResultAsync(duplicateResponse).ConfigureAwait(true);

        first.IsRejection.ShouldBeFalse();
        first.Events.ShouldContain(static item => item.EventTypeName.EndsWith("GovernedNoteRecorded", StringComparison.Ordinal));
        duplicate.IsRejection.ShouldBeFalse();
        duplicate.Events.ShouldContain(static item => item.EventTypeName.EndsWith("GovernedNoteRecorded", StringComparison.Ordinal));
        idempotencyStore.RecordCount.ShouldBe(0);
    }

    [Fact]
    public async Task ProcessEndpointShouldFailClosedAndAbortAdmissionWhenPreCommitAuditIsUnavailable()
    {
        UnavailableAuditWriter auditWriter = new();
        InMemoryAuditReplayIntentQueue replayQueue = new();
        InMemoryOperatorAlertSink alertSink = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services =>
            {
                services.AddSingleton<IAuditWriter>(auditWriter);
                services.AddSingleton<IAuditReplayIntentQueue>(replayQueue);
                services.AddSingleton<IOperatorAlertSink>(alertSink);
                services.AddSingleton<IIdempotencyStore>(idempotencyStore);
            });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsJsonAsync("/process", DomainServiceRequest("RecordGovernedNote"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        DomainServiceWireResult result = await ReadDomainServiceResultAsync(response).ConfigureAwait(true);

        result.IsRejection.ShouldBeTrue();
        AdmissionReason(result).ShouldBe(AuditFailureReasonCodes.AuditUnavailable);
        result.Events.ShouldNotContain(static item => item.EventTypeName.EndsWith("GovernedNoteRecorded", StringComparison.Ordinal));
        auditWriter.Envelopes.ShouldHaveSingleItem().Phase.ShouldBe(AuditCommitPhase.PreCommit);
        replayQueue.Intents.ShouldHaveSingleItem().Kind.ShouldBe(AuditReplayIntentKind.PreCommitOperationReplay);
        alertSink.Alerts.ShouldHaveSingleItem().Kind.ShouldBe(OperatorAlertKind.AuditUnavailable);
        idempotencyStore.RecordCount.ShouldBe(0);
    }

    [Fact]
    public async Task ProcessEndpointShouldRejectMalformedCommandPayloadWithoutEchoingPayload()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsJsonAsync(
                "/process",
                DomainServiceRequest("RecordGovernedNote", payloadBytes: "{ \"noteId\": \"payload-sentinel\" "u8.ToArray()),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument document = JsonDocument.Parse(body);
        DomainServiceWireResult result = document.Deserialize<DomainServiceWireResult>(Program.QueryJsonOptions).ShouldNotBeNull();

        result.IsRejection.ShouldBeTrue();
        AdmissionReason(result).ShouldBe(ChatBotAuthorizationReasonCodes.InvalidCommandPayload);
        body.ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public void ServerHostShouldRegisterSdkDomainTelemetryAndStateStoreHealthCheck()
    {
        using WebApplicationFactory<Program> factory = new();

        EventStoreDomainDiagnostics diagnostics = factory.Services.GetRequiredService<EventStoreDomainDiagnostics>();
        diagnostics.Domain.ShouldBe("chatbot");
        diagnostics.ActivitySource.Name.ShouldBe(EventStoreDomainTelemetry.ActivitySourceName("chatbot"));
        diagnostics.Meter.Name.ShouldBe(EventStoreDomainTelemetry.MeterName("chatbot"));

        HealthCheckServiceOptions healthOptions = factory.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value;
        HealthCheckRegistration registration = healthOptions.Registrations
            .Where(registration => registration.Name == EventStoreDomainTelemetry.StateStoreHealthCheckName("chatbot"))
            .ShouldHaveSingleItem();
        registration.Tags.ShouldContain("chatbot");
        registration.Tags.ShouldContain("ready");
    }

    [Fact]
    public void ServerHostShouldRegisterDurableIdempotencyStoreWithItsRecoveryInputs()
    {
        using WebApplicationFactory<Program> factory = new();
        IServiceProvider services = factory.Services;

        IIdempotencyStore registered = services.GetRequiredService<IIdempotencyStore>();
        DaprCoarseIdempotencyStore store = registered.ShouldBeOfType<DaprCoarseIdempotencyStore>();
        services.GetRequiredService<IIdempotencyStore>().ShouldBeSameAs(store);

        // Durable recovery needs both the retained audit history and authoritative EventStore command status.
        store.AuditHistory.ShouldNotBeNull().ShouldBeSameAs(services.GetRequiredService<IAuditHistoryReader>());
        store.EventStore.ShouldNotBeNull().ShouldBeOfType<EventStoreGatewayClient>();

        // The store persists through its dedicated DAPR client: EnumMember wire values for generated enums, while
        // the shared host client keeps the DAPR web defaults the EventStore read-model batch seam depends on.
        Dapr.Client.DaprClient idempotencyClient = store.StateClient.ShouldBeOfType<DaprCoarseIdempotencyStateClient>().Client;
        idempotencyClient.ShouldBeSameAs(services.GetRequiredKeyedService<Dapr.Client.DaprClient>(CoarseIdempotencyStateJson.DaprClientKey));
        idempotencyClient.JsonSerializerOptions.ShouldBeSameAs(CoarseIdempotencyStateJson.Options);
        idempotencyClient.JsonSerializerOptions.Converters.ShouldContain(static converter => converter is EnumMemberWireValueJsonConverterFactory);
        Dapr.Client.DaprClient shared = services.GetRequiredService<Dapr.Client.DaprClient>();
        shared.ShouldNotBeSameAs(idempotencyClient);
        shared.JsonSerializerOptions.Converters.ShouldNotContain(static converter => converter is EnumMemberWireValueJsonConverterFactory);
    }

    [Fact]
    public async Task ChatBotCompatibilityHealthEndpointShouldReturnHealthyStatus()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/health/chatbot", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        body.ShouldContain("healthy");
    }

    [Fact]
    public async Task AliveEndpointShouldReturnAliveStatus()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/alive", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        body.ShouldContain("Healthy");
    }

    [Fact]
    public async Task ChatBotHealthEndpointShouldExposeModuleIdentity()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/health/chatbot", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ChatBotHealth? health = await response.Content
            .ReadFromJsonAsync<ChatBotHealth>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        health.ShouldNotBeNull();
        health.ModuleName.ShouldBe(ChatBotModuleInfo.ModuleName);
        health.DaprAppId.ShouldBe(ChatBotModuleInfo.DaprAppId);
        health.Status.ShouldBe("healthy");
    }

    [Fact]
    public async Task PeriodicEnforcementHealthEndpointShouldExposeSchedulerStatus()
    {
        InMemoryPeriodicEnforcementStatusStore statusStore = new();
        DateTimeOffset startedAt = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);
        statusStore.RecordStarted(startedAt, "periodic-api-test");
        statusStore.RecordEvaluatorFailure("audit-projection-lag");
        statusStore.RecordSucceeded(startedAt.AddSeconds(30), TimeSpan.FromSeconds(30));

        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(
                services => services.AddSingleton<IPeriodicEnforcementStatusStore>(statusStore)));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/health/chatbot/periodic-enforcement", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;

        root.GetProperty("isRunning").GetBoolean().ShouldBeFalse();
        DateTimeOffset.Parse(root.GetProperty("lastStartedAtUtc").GetString()!).ShouldBe(startedAt);
        DateTimeOffset.Parse(root.GetProperty("lastSucceededAtUtc").GetString()!).ShouldBe(startedAt.AddSeconds(30));
        root.GetProperty("lastCorrelationId").GetString().ShouldBe("periodic-api-test");
        root.GetProperty("skippedOverlapCount").GetInt64().ShouldBe(0);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("project-alpha", Case.Insensitive);

        // This endpoint is anonymous and the topology publishes it externally, so it must disclose nothing about the
        // governance controls' verdicts — no per-sweep breach bit, no sweep-keyed evaluator failure counts (which name
        // the WORM/isolation jobs), and no stop-ship flag. Those moved to the token-gated M2 release-gate endpoint.
        root.TryGetProperty("m2SweepStatuses", out _).ShouldBeFalse();
        root.TryGetProperty("isStopShip", out _).ShouldBeFalse();
        root.TryGetProperty("evaluatorFailureCounts", out _).ShouldBeFalse();
        body.ShouldNotContain("breach", Case.Insensitive);
        body.ShouldNotContain("worm-audit-chain", Case.Insensitive);
    }

    [Fact]
    public async Task M2ReleaseGateEndpointShouldNotBeMappedWithoutAToken()
    {
        // Fail-closed by construction: with no configured token the endpoint does not exist, so the M2 breach state
        // can never be reached anonymously even by a caller who knows the path.
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/health/chatbot/periodic-enforcement/m2", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnknownEndpointShouldReturnNotFound()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/health/missing", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CommandEndpointShouldRejectUnauthenticatedSubmissionsWithSafeProblemDetails()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "restricted-project-sentinel"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authentication_failure");
        root.GetProperty("code").GetString().ShouldBe("authentication_denied");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("restricted-project-sentinel", Case.Insensitive);
    }

    /// <summary>
    /// Pins the boundary order the live Tier-3 mailbox-bearer probes rely on: the published request contract is
    /// validated before authentication (so a contract-invalid body answers the versioned 400 whatever the bearer), a
    /// contract-valid body is rejected at the authentication boundary (401) without credentials, and the same body with
    /// an admitted principal reaches dispatch, whose deliberately invalid intake identity yields dispatch-unavailable —
    /// the only response that proves admission. A 400 therefore never proves a bearer was admitted.
    /// </summary>
    [Fact]
    public async Task CommandEndpointShouldValidateContractBeforeAuthenticationAndAuthenticateBeforeAdmission()
    {
        const string commandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";

        using WebApplicationFactory<Program> anonymousFactory = new();
        using HttpClient anonymous = anonymousFactory.CreateClient();
        using HttpResponseMessage incomplete = await anonymous
            .SendAsync(MailboxAdmissionProbeRequest(IncompleteMailboxAdmissionProbeBody(commandId)), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        incomplete.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using (JsonDocument problem = JsonDocument.Parse(await incomplete.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)))
        {
            problem.RootElement.GetProperty("type").GetString().ShouldBe(ChatBotProblemTypes.ValidationFailure);
            problem.RootElement.GetProperty("code").GetString().ShouldBe("command_contract_invalid");
        }

        using HttpResponseMessage unauthenticated = await anonymous
            .SendAsync(MailboxAdmissionProbeRequest(MailboxAdmissionProbeBody(commandId)), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        unauthenticated.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        using (JsonDocument problem = JsonDocument.Parse(await unauthenticated.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)))
        {
            problem.RootElement.GetProperty("type").GetString().ShouldBe(ChatBotProblemTypes.AuthorizationDenied);
            problem.RootElement.GetProperty("code").GetString().ShouldBe("authentication_denied");
        }

        using WebApplicationFactory<Program> admittedFactory = AuthenticatedFactory("tenant-alpha");
        using HttpClient admitted = admittedFactory.CreateClient();
        using HttpResponseMessage dispatchUnavailable = await admitted
            .SendAsync(MailboxAdmissionProbeRequest(MailboxAdmissionProbeBody(commandId)), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        dispatchUnavailable.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        using (JsonDocument problem = JsonDocument.Parse(await dispatchUnavailable.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)))
        {
            problem.RootElement.GetProperty("type").GetString().ShouldBe(ChatBotProblemTypes.DispatchUnavailable);
        }
    }

    [Fact]
    public async Task CommandEndpointShouldAcceptAuthenticatedTenantBoundSubmission()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "allowed-resource"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("lifecycleState").GetString().ShouldBe("Proposed");
        response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        response.Headers.GetValues("X-Hexalith-Task-Id").Single().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        body.ShouldNotContain("allowed-resource", Case.Insensitive);
    }

    [Fact]
    public async Task CommandEndpointShouldRoundTripThroughSdkProcessWithoutSecondAdmission()
    {
        InMemoryAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new SystemClock());
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services =>
            {
                services.AddSingleton<IAuditWriter>(auditWriter);
                services.AddSingleton<IIdempotencyStore>(idempotencyStore);
                services.AddSingleton<IEventStoreGatewayClient>(static provider => new RoundTrippingEventStoreGatewayClient(provider));
            });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(RecordGovernedNoteRequest("01ARZ3NDEKTSV4RRFFQ69G5FAZ", origin: "ui"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        auditWriter.Envelopes.Count(static envelope => envelope.Phase == AuditCommitPhase.PreCommit).ShouldBe(1);
        auditWriter.Envelopes.Count(static envelope => envelope.Phase == AuditCommitPhase.PostCommit).ShouldBe(1);
        idempotencyStore.RecordCount.ShouldBe(1);
    }

    [Fact]
    public async Task TypedFacadeShouldSubmitCamelCaseCommandAndReplayStableIdentityOverHttp()
    {
        RecordingDispatcher dispatcher = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<ICommandDispatcher>(dispatcher));
        using HttpClient http = factory.CreateClient();
        Hexalith.ChatBot.Client.ChatBotClient facade = new(new Hexalith.ChatBot.Client.Generated.Client(http));
        const string commandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
        const string correlationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        Hexalith.ChatBot.Contracts.Commands.RecordGovernedNote command = new("note-1");

        CommandSubmissionResponse first = await facade.SubmitWithCommandIdAsync(
            command, commandId, correlationId, cancellationToken: TestContext.Current.CancellationToken);
        CommandSubmissionResponse replay = await facade.SubmitWithCommandIdAsync(
            command, commandId, correlationId, cancellationToken: TestContext.Current.CancellationToken);

        first.CommandId.ShouldBe(commandId);
        first.OperationId.ShouldBe(commandId);
        replay.OperationId.ShouldBe(first.OperationId);
        replay.CommandId.ShouldBe(first.CommandId);
        dispatcher.DispatchCount.ShouldBe(1);
    }

    [Fact]
    public async Task CommandEndpointShouldGenerateAndEchoMissingCorrelationHeader()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = CommandSubmissionRequest("tenant-alpha", "allowed-resource");
        request.Headers.Remove("X-Correlation-Id");
        request.Headers.Remove("X-Hexalith-Task-Id");

        using HttpResponseMessage response = await client
            .SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        string correlationId = accepted.RootElement.GetProperty("correlationId").GetString().ShouldNotBeNull();
        correlationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        string commandId = accepted.RootElement.GetProperty("commandId").GetString().ShouldNotBeNull();
        accepted.RootElement.GetProperty("operationId").GetString().ShouldBe(commandId);
        response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe(correlationId);
        response.Headers.Contains("X-Hexalith-Task-Id").ShouldBeFalse();

        using HttpRequestMessage statusRequest = new(HttpMethod.Get, $"/api/v1/operations/{commandId}");
        using HttpResponseMessage statusResponse = await client.SendAsync(
            statusRequest, TestContext.Current.CancellationToken).ConfigureAwait(true);
        statusResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        statusResponse.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        using JsonDocument status = JsonDocument.Parse(await statusResponse.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
        status.RootElement.GetProperty("operationId").GetString().ShouldBe(commandId);
    }

    [Fact]
    public async Task CommandEndpointShouldRejectInvalidLifecycleTransitionBeforeDispatch()
    {
        RecordingDispatcher dispatcher = new();
        InMemoryAuditWriter auditWriter = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services =>
            {
                services.AddSingleton<ICommandDispatcher>(dispatcher);
                services.AddSingleton<IAuditWriter>(auditWriter);
                services.AddSingleton<ILifecycleTransitionGuard>(
                    new FixedLifecycleTransitionGuard(
                        LifecycleTransitionValidation.Invalid(new LifecycleTransitionDefinition("Received", "Associated"))));
            });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "payload-sentinel"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.Count.ShouldBe(1);
        auditWriter.Envelopes[0].Decision.ShouldBe("reject");
        auditWriter.Envelopes[0].ReasonCode.ShouldBe(LifecycleTransitionReasonCodes.InvalidTransition);
        auditWriter.Envelopes[0].StateTransition.ShouldBe("Received->Associated");

        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("conflict");
        root.GetProperty("code").GetString().ShouldBe(LifecycleTransitionReasonCodes.InvalidTransition);
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task CommandEndpointShouldReturnAuditUnavailableWhenInvalidLifecycleAuditCannotBeWritten()
    {
        RecordingDispatcher dispatcher = new();
        UnavailableAuditWriter auditWriter = new();
        InMemoryAuditReplayIntentQueue replayQueue = new();
        InMemoryOperatorAlertSink alertSink = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services =>
            {
                services.AddSingleton<ICommandDispatcher>(dispatcher);
                services.AddSingleton<IAuditWriter>(auditWriter);
                services.AddSingleton<IAuditReplayIntentQueue>(replayQueue);
                services.AddSingleton<IOperatorAlertSink>(alertSink);
                services.AddSingleton<ILifecycleTransitionGuard>(
                    new FixedLifecycleTransitionGuard(
                        LifecycleTransitionValidation.Invalid(new LifecycleTransitionDefinition("Received", "Associated"))));
            });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "payload-sentinel"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.Count.ShouldBe(1);
        replayQueue.Intents.Count.ShouldBe(1);
        replayQueue.Intents[0].Kind.ShouldBe(AuditReplayIntentKind.PreCommitOperationReplay);
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
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task CommandEndpointShouldReplayEquivalentDuplicateWithoutSecondDispatch()
    {
        RecordingDispatcher dispatcher = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<ICommandDispatcher>(dispatcher));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "allowed-resource"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage second = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "allowed-resource"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        second.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        first.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        second.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        dispatcher.DispatchCount.ShouldBe(1);
        string firstBody = await first.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string secondBody = await second.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        AssertReplayMatchesStoredOutcome(firstBody, secondBody);
        secondBody.ShouldNotContain("allowed-resource", Case.Insensitive);
        secondBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
    }

    [Fact]
    public async Task ReplayWithoutStoredTaskMustClearRetryTaskFromTraceAndMatchResponseIdentity()
    {
        RecordingDispatcher dispatcher = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha", services => services.AddSingleton<ICommandDispatcher>(dispatcher));
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage original = CommandSubmissionRequest("tenant-alpha", "allowed-resource");
        original.Headers.Remove("X-Hexalith-Task-Id");
        using HttpResponseMessage accepted = await client.SendAsync(original, TestContext.Current.CancellationToken);
        accepted.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        string firstBody = await accepted.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        System.Diagnostics.ActivityTraceId traceId = System.Diagnostics.ActivityTraceId.CreateRandom();
        TaskCompletionSource<System.Diagnostics.Activity> stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using System.Diagnostics.ActivityListener listener = new()
        {
            ShouldListenTo = static _ => true,
            Sample = static (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref System.Diagnostics.ActivityCreationOptions<string> _) => System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => { if (activity.Kind == System.Diagnostics.ActivityKind.Server && activity.TraceId == traceId) { stopped.TrySetResult(activity); } },
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);
        using HttpRequestMessage retry = CommandSubmissionRequest("tenant-alpha", "allowed-resource");
        retry.Headers.Remove("X-Correlation-Id");
        retry.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAZ");
        retry.Headers.Add("traceparent", $"00-{traceId}-{System.Diagnostics.ActivitySpanId.CreateRandom()}-01");
        using HttpResponseMessage replay = await client.SendAsync(retry, TestContext.Current.CancellationToken);
        replay.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        string replayBody = await replay.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        AssertReplayMatchesStoredOutcome(firstBody, replayBody);
        using JsonDocument parsed = JsonDocument.Parse(replayBody);
        JsonElement body = parsed.RootElement;
        body.GetProperty("operationId").GetString().ShouldBe(body.GetProperty("commandId").GetString());
        body.TryGetProperty("taskId", out _).ShouldBeFalse();
        replay.Headers.Contains("X-Hexalith-Task-Id").ShouldBeFalse();
        string responseCorrelation = body.GetProperty("correlationId").GetString()!;
        replay.Headers.GetValues("X-Correlation-Id").Single().ShouldBe(responseCorrelation);
        System.Diagnostics.Activity trace = await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        trace.GetTagItem("hexalith.correlation_id").ShouldBe(responseCorrelation);
        trace.GetTagItem("hexalith.task_id").ShouldBeNull();
        dispatcher.DispatchCount.ShouldBe(1);
    }

    [Theory]
    [InlineData("malformed-json")]
    [InlineData("command-type-suffix")]
    [InlineData("command-type-pattern")]
    [InlineData("command-type-trailing-newline")]
    [InlineData("command-type-too-long")]
    [InlineData("wrong-metadata-type")]
    [InlineData("mismatched-casing")]
    [InlineData("unsupported-request-schema-version")]
    [InlineData("undeclared-root-property")]
    [InlineData("malformed-specialized-body")]
    [InlineData("invalid-specialized-enum")]
    [InlineData("fractional-specialized-integer")]
    [InlineData("unsupported-media-type")]
    [InlineData("vendor-json-media-type")]
    [InlineData("unknown-origin")]
    [InlineData("null-scoring-signal")]
    [InlineData("invalid-init-only-member")]
    [InlineData("nested-casing-override")]
    [InlineData("nested-object-casing-override")]
    [InlineData("invalid-dictionary-enum-key")]
    [InlineData("duplicate-dictionary-key")]
    public async Task CommandEndpointShouldRejectInvalidContractBeforeAdmissionWithVersionedSafeProblem(string scenario)
    {
        RecordingDispatcher dispatcher = new();
        InMemoryAuditWriter audit = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha", services =>
        {
            services.AddSingleton<ICommandDispatcher>(dispatcher);
            services.AddSingleton<IAuditWriter>(audit);
        });
        using HttpClient client = factory.CreateClient();
        const string commandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
        string payload = scenario switch
        {
            "malformed-json" => "{",
            "command-type-suffix" => """{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"TenantScopedCommand","command":{},"requestSchemaVersion":"v1"}""",
            "command-type-pattern" => """{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"lowercaseName","command":{},"requestSchemaVersion":"v1"}""",
            "command-type-trailing-newline" => """{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"ProbeCommand\n","command":{},"requestSchemaVersion":"v1"}""",
            "command-type-too-long" => $$"""{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"{{new string('A', 161)}}","command":{},"requestSchemaVersion":"v1"}""",
            "wrong-metadata-type" => """{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"RecordGovernedNote","command":{"noteId":"note-1"},"actorType":42,"requestSchemaVersion":"v1"}""",
            "mismatched-casing" => """{"CommandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"RecordGovernedNote","command":{"noteId":"note-1"},"requestSchemaVersion":"v1"}""",
            "unsupported-request-schema-version" => """{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"RecordGovernedNote","command":{"noteId":"note-1"},"requestSchemaVersion":"v2"}""",
            "undeclared-root-property" => """{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"RecordGovernedNote","command":{"noteId":"note-1"},"requestSchemaVersion":"v1","unexpectedField":true}""",
            "malformed-specialized-body" => """{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"RequestFailedWorkflowRetry","command":{},"requestSchemaVersion":"v1"}""",
            "invalid-specialized-enum" => """{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"AssociateEmailToProject","command":{"associationId":"01ARZ3NDEKTSV4RRFFQ69G5FAV","intakeId":"01ARZ3NDEKTSV4RRFFQ69G5FAZ","projectId":"project-001","decisionKind":"not-a-decision","decisionNote":null,"candidateEvidenceFingerprint":"hash-project","sourceVersion":1,"schemaVersion":"chatbot.association-decision-command.v1"},"requestSchemaVersion":"v1"}""",
            "fractional-specialized-integer" => """{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"AssociateEmailToProject","command":{"associationId":"01ARZ3NDEKTSV4RRFFQ69G5FAV","intakeId":"01ARZ3NDEKTSV4RRFFQ69G5FAZ","projectId":"project-001","decisionKind":"associate","decisionNote":null,"candidateEvidenceFingerprint":"hash-project","sourceVersion":1.5,"schemaVersion":"chatbot.association-decision-command.v1"},"requestSchemaVersion":"v1"}""",
            "unknown-origin" => """{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"RecordGovernedNote","command":{"noteId":"note-1"},"origin":"unknown","requestSchemaVersion":"v1"}""",
            "null-scoring-signal" => """{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"ScoreMailboxMessageAssociation","command":{"associationId":"01ARZ3NDEKTSV4RRFFQ69G5FAV","intakeId":"01ARZ3NDEKTSV4RRFFQ69G5FAZ","sourceMailboxId":"mailbox-1","sourceConversationId":"conversation-1","sourceThreadId":null,"deterministicSignals":[null],"thresholdPolicy":null,"candidates":null,"exclusions":null,"result":null,"scoringKernelVersion":"v1"},"requestSchemaVersion":"v1"}""",
            "invalid-init-only-member" => ProposeActionRequestBody("""{"secret":"value"}"""),
            "nested-casing-override" => """{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"ScoreMailboxMessageAssociation","command":{"associationId":"01ARZ3NDEKTSV4RRFFQ69G5FAV","intakeId":"01ARZ3NDEKTSV4RRFFQ69G5FAZ","sourceMailboxId":"mailbox-1","sourceConversationId":"conversation-1","sourceThreadId":null,"deterministicSignals":[],"DeterministicSignals":[null],"thresholdPolicy":null,"candidates":null,"exclusions":null,"result":null,"scoringKernelVersion":"v1"},"requestSchemaVersion":"v1"}""",
            "nested-object-casing-override" => ProposeActionRequestBody("\"owner-001\"").Replace(
                "\"proposalInputMetadata\":{\"source\":\"trusted\"}",
                "\"proposalInputMetadata\":{\"source\":\"trusted\"},\"ProposalInputMetadata\":42",
                StringComparison.Ordinal),
            "duplicate-dictionary-key" => ProposeActionRequestBody("\"owner-001\"").Replace(
                "\"proposalInputMetadata\":{\"source\":\"trusted\"}",
                "\"proposalInputMetadata\":{\"source\":\"one\",\"source\":\"two\"}",
                StringComparison.Ordinal),
            "invalid-dictionary-enum-key" => """{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"SubmitTenantPolicyChange","command":{"policyChangeId":"change-1","sourcePolicySnapshotId":"source-1","proposedPolicySnapshotId":"next-1","sourceVersion":1,"changedKnobIds":["ai-action.low-risk-allowed"],"changeSet":{"values":[{"knobId":"ai-action.low-risk-allowed","aiActionLowRiskAllowed":{"invalid-action":true}}]},"reasonCode":"test","requesterRef":"actor-1","schemaVersion":"v1","correlationId":"01ARZ3NDEKTSV4RRFFQ69G5FAW","oldValueFingerprint":"old","newValueFingerprint":"new"},"requestSchemaVersion":"v1"}""",
            _ => """{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"RecordGovernedNote","command":{"noteId":"note-1"},"requestSchemaVersion":"v1"}""",
        };
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
        {
            Content = new StringContent(payload, Encoding.UTF8,
                scenario == "unsupported-media-type" ? "text/plain" :
                scenario == "vendor-json-media-type" ? "application/vnd.chatbot+json" : "application/json"),
        };

        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("code").GetString().ShouldBe("command_contract_invalid");
        root.GetProperty("schemaVersion").GetString().ShouldBe("chatbot.message-catalog.v1");
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        if (scenario is "wrong-metadata-type" or "malformed-specialized-body" or "command-type-suffix" or
            "command-type-pattern" or "command-type-trailing-newline" or "command-type-too-long" or
            "unsupported-request-schema-version" or "undeclared-root-property" or
            "invalid-specialized-enum" or "fractional-specialized-integer" or "unknown-origin" or
            "null-scoring-signal" or "invalid-init-only-member" or "nested-casing-override" or "nested-object-casing-override" or
            "invalid-dictionary-enum-key" or "duplicate-dictionary-key")
        {
            root.GetProperty("correlationId").GetString().ShouldBe(commandId);
        }

        dispatcher.DispatchCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        body.ShouldNotContain("note-1", Case.Insensitive);
        body.ShouldNotContain("not-a-decision", Case.Insensitive);
        body.ShouldNotContain("project-001", Case.Insensitive);
    }

    [Fact]
    public async Task CommandEndpointShouldAcceptObjectValuedProposalMetadataAtValidationBoundary()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
        {
            Content = new StringContent(ProposeActionRequestBody("\"owner-001\""), Encoding.UTF8, "application/json"),
        };

        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.StatusCode.ShouldNotBe(HttpStatusCode.BadRequest);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        body.ShouldNotContain("command_contract_invalid", Case.Sensitive);
    }

    [Fact]
    public async Task TypedFacadeShouldPreserveCaseDistinctProposalMetadataKeysAtValidationBoundary()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient http = factory.CreateClient();
        Hexalith.ChatBot.Client.ChatBotClient facade = new(new Hexalith.ChatBot.Client.Generated.Client(http));
        const string correlationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        Hexalith.ChatBot.Contracts.Commands.ProposeAIAction command = new(
            ProjectId: "project-001",
            TaskIntentId: "task-001",
            SourceMessageId: "message-001",
            RequesterId: "requester-001",
            IntendedCommandName: "AppendConversationMessage",
            ActionKind: "summarize",
            ExpectedSourceVersion: 1,
            EvidenceReferences: ["evidence-safe"],
            AffectedResourceReferences: [],
            RecipientReferences: [],
            PolicySnapshotId: null,
            CorrelationId: correlationId,
            TransitionId: "transition-001",
            ProposalInputMetadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["source"] = "one",
                ["Source"] = "two",
            });

        // A valid envelope reaches authentication; malformed typed member aliases still fail before that boundary.
        HexalithChatBotApiException<ProblemDetails> problem = await Should.ThrowAsync<HexalithChatBotApiException<ProblemDetails>>(
            () => facade.SubmitWithCommandIdAsync(command, "01ARZ3NDEKTSV4RRFFQ69G5FAY", correlationId,
                cancellationToken: TestContext.Current.CancellationToken));
        problem.StatusCode.ShouldBe((int)HttpStatusCode.Unauthorized);
        problem.Result.Code.ShouldBe("authentication_denied");
        problem.Result.SchemaVersion.ShouldBe("chatbot.message-catalog.v1");
    }

    private static string ProposeActionRequestBody(string stateOwnerJson)
        => $$"""
        {"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","commandType":"ProposeAIAction","command":{
          "projectId":"project-001","taskIntentId":"task-001","sourceMessageId":"message-001",
          "requesterId":"requester-001","intendedCommandName":"AppendConversationMessage",
          "actionKind":"summarize","expectedSourceVersion":1,"evidenceReferences":[],
          "affectedResourceReferences":[],"recipientReferences":[],"policySnapshotId":null,
          "correlationId":"01ARZ3NDEKTSV4RRFFQ69G5FAW","transitionId":"transition-001",
          "proposalInputMetadata":{"source":"trusted"},"stateOwnerAggregateId":{{stateOwnerJson}}
        },"requestSchemaVersion":"v1"}
        """;

    [Fact]
    public async Task CommandEndpointShouldReturnMetadataOnlyConflictAndSkipDispatch()
    {
        RecordingDispatcher dispatcher = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services =>
            {
                services.AddSingleton<ICommandDispatcher>(dispatcher);
                services.AddSingleton<IIdempotencyStore>(new ConflictIdempotencyStore());
            });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "payload-sentinel"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        dispatcher.DispatchCount.ShouldBe(0);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("conflict");
        root.GetProperty("code").GetString().ShouldBe("idempotency_conflict_command_execution");
        root.GetProperty("retryable").GetBoolean().ShouldBeFalse();
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task CommandEndpointShouldReturnAuditUnavailableAndSkipDispatchWhenPreCommitAuditFails()
    {
        RecordingDispatcher dispatcher = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services =>
            {
                services.AddSingleton<IAuditWriter>(new UnavailableAuditWriter());
                services.AddSingleton<ICommandDispatcher>(dispatcher);
            });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "payload-sentinel"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("internal_error");
        root.GetProperty("code").GetString().ShouldBe("audit_unavailable");
        root.GetProperty("retryable").GetBoolean().ShouldBeTrue();
        root.GetProperty("clientAction").GetString().ShouldBe("retry-later");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        dispatcher.DispatchCount.ShouldBe(0);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task CommandEndpointShouldRejectAuthenticatedCrossTenantSubmissionWithSafeProblemDetails()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-beta", "restricted-project-sentinel"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authorization_denied");
        root.GetProperty("code").GetString().ShouldBe("authorization_denied");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("tenant-beta", Case.Insensitive);
        body.ShouldNotContain("restricted-project-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task CommandEndpointShouldNotEchoInvalidCorrelationMetadataInSafeProblemDetails()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpRequestMessage request = CommandSubmissionRequest("tenant-alpha", "restricted-project-sentinel");
        request.Headers.Remove("X-Correlation-Id");
        request.Headers.Remove("X-Hexalith-Task-Id");
        request.Headers.Add("X-Correlation-Id", "/tmp/sensitive-correlation");
        request.Headers.Add("X-Hexalith-Task-Id", "payload-sentinel-task");

        using HttpResponseMessage response = await client
            .SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.TryGetProperty("taskId", out _).ShouldBeFalse();
        response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        response.Headers.Contains("X-Hexalith-Task-Id").ShouldBeFalse();
        body.ShouldNotContain("/tmp/sensitive-correlation", Case.Insensitive);
        body.ShouldNotContain("payload-sentinel-task", Case.Insensitive);
        body.ShouldNotContain("restricted-project-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task OperationStatusEndpointShouldRequireAuthentication()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = OperationStatusRequest("01ARZ3NDEKTSV4RRFFQ69G5FAX");

        using HttpResponseMessage response = await client
            .SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("code").GetString().ShouldBe("authentication_denied");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
    }

    [Fact]
    public async Task OperationStatusEndpointShouldRejectInvalidOperationIdWithoutEchoingUnsafeCorrelationMetadata()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = OperationStatusRequest("payload-sentinel-raw-operation");
        request.Headers.Remove("X-Correlation-Id");
        request.Headers.Remove("X-Hexalith-Task-Id");
        request.Headers.Add("X-Correlation-Id", "/tmp/sensitive-correlation");
        request.Headers.Add("X-Hexalith-Task-Id", "secret-task-token");

        using HttpResponseMessage response = await client
            .SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        string correlationId = root.GetProperty("correlationId").GetString().ShouldNotBeNull();
        ChatBotCorrelationId.TryParse(correlationId, out _).ShouldBeTrue();
        response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe(correlationId);
        response.Headers.Contains("X-Hexalith-Task-Id").ShouldBeFalse();
        root.GetProperty("code").GetString().ShouldBe("authorization_denied");
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("payload-sentinel", Case.Insensitive);
        body.ShouldNotContain("/tmp/sensitive-correlation", Case.Insensitive);
        body.ShouldNotContain("secret-task-token", Case.Insensitive);
    }

    [Fact]
    public async Task OperationStatusEndpointShouldReturnProjectionPendingMetadataOnlyStatus()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage command = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "payload-sentinel-secret-C:\\\\restricted\\\\item-/tmp/item raw exception"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        command.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        using HttpResponseMessage response = await client
            .SendAsync(OperationStatusRequest("01ARZ3NDEKTSV4RRFFQ69G5FAX"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        response.Headers.GetValues("X-Hexalith-Task-Id").Single().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument status = JsonDocument.Parse(body);
        JsonElement root = status.RootElement;
        root.GetProperty("operationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("lifecycleState").GetString().ShouldBe("Proposed");
        root.GetProperty("retryCount").GetInt32().ShouldBe(0);
        string completionStatus = root.GetProperty("completionStatus").GetString().ShouldNotBeNull();
        completionStatus.ShouldBe("accepted-projection-pending");
        // Never-a-false-Done: the top-level status and the partial-output status must agree on the pending state
        // for a freshly accepted command (distinctness from 'completed' is enforced at the contract enum level in
        // ClientGenerationTests.GeneratedOperationStatusEnumsShouldUseCanonicalWireValuesAndKeepPendingDistinctFromCompleted).
        completionStatus.ShouldBe(root.GetProperty("partialOutputs").GetProperty("completionStatus").GetString());
        root.GetProperty("auditStatus").GetString().ShouldBe("committed");
        root.GetProperty("partialOutputs").GetProperty("completionStatus").GetString().ShouldBe("accepted-projection-pending");
        root.GetProperty("partialOutputs").GetProperty("auditStatus").GetString().ShouldBe("committed");
        root.GetProperty("safeNextActions").EnumerateArray().Single().GetString().ShouldBe("none");
        root.TryGetProperty("terminalReason", out _).ShouldBeFalse();
        root.GetProperty("acceptedAt").GetDateTimeOffset().Offset.ShouldBe(TimeSpan.Zero);
        root.GetProperty("lastUpdatedAt").GetDateTimeOffset().Offset.ShouldBe(TimeSpan.Zero);
        root.GetProperty("partialOutputs").GetProperty("acceptedAt").GetDateTimeOffset().ShouldBe(
            root.GetProperty("acceptedAt").GetDateTimeOffset());
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("payload-sentinel", Case.Insensitive);
        body.ShouldNotContain("secret", Case.Insensitive);
        body.ShouldNotContain("/tmp/item", Case.Insensitive);
        body.ShouldNotContain("C:\\", Case.Insensitive);
        body.ShouldNotContain("raw exception", Case.Insensitive);
    }

    [Fact]
    public async Task OperationStatusGetShouldUseInjectedClockAtRetryDueBoundary()
    {
        DateTimeOffset acceptedAt = new(2026, 8, 9, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset dueAt = acceptedAt.AddSeconds(30);
        MutableStatusClock clock = new(dueAt.AddTicks(-1));
        InMemoryOperationStatusStore store = new();
        CommandSubmissionResponse accepted = new()
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            OperationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            TaskId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            LifecycleState = Hexalith.ChatBot.Client.Generated.LifecycleState.Correcting,
            AcceptedAt = acceptedAt,
        };
        await store.UpsertAsync(OperationStatusRecord.Accepted("tenant-alpha", accepted, false, acceptedAt) with
        {
            RetryCount = 1,
            MaxAttempts = 5,
            NextRetryAt = dueAt,
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha", services =>
        {
            services.AddSingleton<IOperationStatusStore>(store);
            services.AddSingleton<ISystemClock>(clock);
        });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage before = await client.SendAsync(
            OperationStatusRequest(accepted.OperationId), TestContext.Current.CancellationToken).ConfigureAwait(true);
        before.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument beforeBody = JsonDocument.Parse(await before.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken).ConfigureAwait(true));
        beforeBody.RootElement.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();

        clock.UtcNow = dueAt;
        using HttpResponseMessage atDue = await client.SendAsync(
            OperationStatusRequest(accepted.OperationId), TestContext.Current.CancellationToken).ConfigureAwait(true);
        atDue.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument dueBody = JsonDocument.Parse(await atDue.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken).ConfigureAwait(true));
        dueBody.RootElement.GetProperty("retryEligible").GetBoolean().ShouldBeTrue();
        dueBody.RootElement.GetProperty("nextRetryAt").GetDateTimeOffset().ShouldBe(dueAt);

        CorrectionPropagationRequest resumed = new(
            "tenant-alpha", "actor-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY", "correction-1", "wf-1", "project-001", "project-002", 3,
            accepted.CorrelationId, acceptedAt, acceptedAt.AddMinutes(10), OperationId: accepted.OperationId);
        await new OperationStatusWorkflowStatusSink(store, clock).ReportAsync(
            resumed, CorrectionPropagationWorkflowStatuses.Started, 1,
            CorrectionPropagationWorkflowFailureCodes.None, TestContext.Current.CancellationToken).ConfigureAwait(true);
        using HttpResponseMessage activeAttempt = await client.SendAsync(
            OperationStatusRequest(accepted.OperationId), TestContext.Current.CancellationToken).ConfigureAwait(true);
        using JsonDocument activeBody = JsonDocument.Parse(await activeAttempt.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken).ConfigureAwait(true));
        activeBody.RootElement.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        activeBody.RootElement.TryGetProperty("nextRetryAt", out _).ShouldBeFalse();
    }

    private sealed class MutableStatusClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    [Fact]
    public async Task AssociationRoutingStatusEndpointShouldEnrichWhyPanelEvidenceWithoutRawDetails()
    {
        InMemoryAssociationProjectionStore store = new();
        await store
            .SaveAsync(AssociationRoutingViewWithEvidence(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<IAssociationProjectionStore>(store));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(AssociationRoutingStatusRequest("01ARZ3NDEKTSV4RRFFQ69G5FAV"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument status = JsonDocument.Parse(body);
        JsonElement root = status.RootElement;
        root.GetProperty("decisionActorId").GetString().ShouldBe("actor-safe");
        root.GetProperty("decisionActorType").GetString().ShouldBe("human");
        root.GetProperty("thresholdPolicyVersion").GetString().ShouldBe("association-thresholds.m0.default.v1");
        root.GetProperty("kernelVersion").GetString().ShouldBe("association-deterministic.kernel.m0.v1");
        JsonElement evidence = root.GetProperty("evidenceRefs").EnumerateArray().Single();
        evidence.GetProperty("signalClass").GetString().ShouldBe("explicit-project-identifier");
        evidence.GetProperty("matchedValueDisplayToken").GetString().ShouldBe("mailbox:metadata");
        evidence.GetProperty("visibilityState").GetString().ShouldBe("available");
        evidence.GetProperty("redactionState").GetString().ShouldBe("metadata_only");
        evidence.GetProperty("freshnessState").GetString().ShouldBe("fresh");
        evidence.GetProperty("confidenceContribution").GetDouble().ShouldBe(0.42);
        body.ShouldNotContain("\"decisionNote\":", Case.Sensitive);
        body.ShouldNotContain("\"correctionRationale\":", Case.Sensitive);
        body.ShouldNotContain("raw-body", Case.Insensitive);
        body.ShouldNotContain("sourceContext", Case.Insensitive);
        body.ShouldNotContain("providerPayload", Case.Insensitive);
    }

    [Fact]
    public async Task AssociationRoutingStatusEndpointShouldExposeExternalStrictnessPostureSafely()
    {
        InMemoryAssociationProjectionStore store = new();
        await store
            .SaveAsync(
                AssociationRoutingViewWithEvidence() with
                {
                    ExternalSender = new Hexalith.ChatBot.Contracts.Commands.MailboxExternalSenderPosture(
                        ExternalSender: true,
                        ContractMailboxPartyResolutionState.Unresolved,
                        ResolvedPartyRef: null,
                        ["external-sender:true", "party-resolution:unresolved"]),
                    StrictnessPolicy = new Hexalith.ChatBot.Contracts.Commands.MailboxAuthenticityStrictnessPolicySnapshot(
                        ContractMailboxAuthenticityStrictness.Strict,
                        "policy-v1",
                        "configured"),
                    RoutingReason = "external-sender-strict-review",
                },
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<IAssociationProjectionStore>(store));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(AssociationRoutingStatusRequest("01ARZ3NDEKTSV4RRFFQ69G5FAV"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument status = JsonDocument.Parse(body);
        JsonElement root = status.RootElement;
        root.GetProperty("routingReason").GetString().ShouldBe("external-sender-strict-review");
        root.GetProperty("externalSender").GetProperty("externalSender").GetBoolean().ShouldBeTrue();
        root.GetProperty("externalSender").GetProperty("partyResolutionState").GetString().ShouldBe("unresolved");
        root.GetProperty("strictnessPolicy").GetProperty("strictness").GetString().ShouldBe("strict");
        root.GetProperty("strictnessPolicy").GetProperty("policyVersion").GetString().ShouldBe("policy-v1");
        body.ShouldNotContain("raw-body", Case.Insensitive);
        body.ShouldNotContain("providerPayload", Case.Insensitive);
        body.ShouldNotContain("sender@example.test", Case.Insensitive);
    }

    [Fact]
    public async Task OperationStatusEndpointShouldExposeDuplicateMailboxSuppressionMetadataOnly()
    {
        RecordingDispatcher dispatcher = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<ICommandDispatcher>(dispatcher));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client
            .SendAsync(
                MailboxIntakeRequest(
                    commandId: "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    taskId: "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                    intakeId: "01ARZ3NDEKTSV4RRFFQ69G5FAZ"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage duplicate = await client
            .SendAsync(
                MailboxIntakeRequest(
                    commandId: "01ARZ3NDEKTSV4RRFFQ69G5FBA",
                    taskId: "01ARZ3NDEKTSV4RRFFQ69G5FBB",
                    intakeId: "01ARZ3NDEKTSV4RRFFQ69G5FBC"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        duplicate.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        dispatcher.DispatchCount.ShouldBe(1);

        string firstBody = await first.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string duplicateBody = await duplicate.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        AssertReplayMatchesStoredOutcome(firstBody, duplicateBody);

        using HttpResponseMessage response = await client
            .SendAsync(OperationStatusRequest("01ARZ3NDEKTSV4RRFFQ69G5FAX"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument status = JsonDocument.Parse(body);
        JsonElement root = status.RootElement;
        root.GetProperty("operationClass").GetString().ShouldBe("message-intake");
        root.GetProperty("originalOperationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("duplicateAttemptCount").GetInt32().ShouldBe(1);
        root.GetProperty("duplicateSafetyNote").GetString().ShouldBe("duplicate-provider-message-suppressed");
        root.GetProperty("safeNextActions").EnumerateArray().Single().GetString().ShouldBe("none");
        root.GetProperty("partialOutputs").GetProperty("completionStatus").GetString().ShouldBe(
            root.GetProperty("completionStatus").GetString());
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("sender@example.test", Case.Insensitive);
        body.ShouldNotContain("recipient@example.test", Case.Insensitive);
        body.ShouldNotContain("graph-message-sensitive", Case.Insensitive);
        body.ShouldNotContain("raw provider payload", Case.Insensitive);
        body.ShouldNotContain("Secret Project", Case.Insensitive);
        body.ShouldNotContain("raw exception", Case.Insensitive);
    }

    [Fact]
    public async Task CommandEndpointShouldAcceptMailboxAuthenticityMetadataAndAuditOnlySafeRefs()
    {
        InMemoryAuditWriter auditWriter = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<IAuditWriter>(auditWriter));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage command = await client
            .SendAsync(
                MailboxIntakeRequest(
                    commandId: "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    taskId: "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                    intakeId: "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    includeAuthenticity: true),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        command.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        AuditEnvelope preCommit = auditWriter.Envelopes.First(static envelope => envelope.Phase == AuditCommitPhase.PreCommit);
        preCommit.SurfaceOrigin.ShouldBe("mailbox");
        preCommit.SourceEvidenceRefs.ShouldContain("auth-spf:fail");
        preCommit.SourceEvidenceRefs.ShouldContain("auth-dkim:temperror");
        preCommit.SourceEvidenceRefs.ShouldContain("auth-dmarc:not-supplied");
        preCommit.SourceEvidenceRefs.ShouldContain("auth-compauth:unknown");
        preCommit.SourceEvidenceRefs.ShouldContain("auth-compauth-reason:109");
        preCommit.SourceEvidenceRefs.ShouldContain("header-discrepancy:multiple-authentication-results");
        preCommit.SourceEvidenceRefs.ShouldContain("header-discrepancy:from-reply-to-mismatch");
        preCommit.SourceEvidenceRefs.ShouldContain("selected-header:Authentication-Results");
        preCommit.SourceEvidenceRefs.ShouldContain("selected-header:Received");

        string acceptedBody = await command.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        AuditEnvelope postCommit = auditWriter.Envelopes.First(static envelope => envelope.Phase == AuditCommitPhase.PostCommit);
        postCommit.SurfaceOrigin.ShouldBe("mailbox");
        postCommit.RedactionDecision.ShouldBe("metadata_only");

        string serializedAudit = JsonSerializer.Serialize(auditWriter.Envelopes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        foreach (string surface in new[] { acceptedBody, serializedAudit })
        {
            surface.ShouldNotContain("Authentication-Results: spf=fail", Case.Insensitive);
            surface.ShouldNotContain("smtp.mailfrom", Case.Insensitive);
            surface.ShouldNotContain("raw provider payload", Case.Insensitive);
            surface.ShouldNotContain("message body", Case.Insensitive);
            surface.ShouldNotContain("Secret Sender", Case.Insensitive);
            surface.ShouldNotContain("Secret Project", Case.Insensitive);
        }
    }

    [Fact]
    public async Task OperationStatusEndpointShouldExposeRetryReplayMetadataOnly()
    {
        RecordingDispatcher dispatcher = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<ICommandDispatcher>(dispatcher));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client
            .SendAsync(
                RetryFailedWorkflowRequest(
                    commandId: "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    taskId: "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                    retryId: "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    rationale: "operator reviewed safe metadata"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage replay = await client
            .SendAsync(
                RetryFailedWorkflowRequest(
                    commandId: "01ARZ3NDEKTSV4RRFFQ69G5FBA",
                    taskId: "01ARZ3NDEKTSV4RRFFQ69G5FBB",
                    retryId: "01ARZ3NDEKTSV4RRFFQ69G5FBC",
                    rationale: "payload-sentinel raw exception Secret Project"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        replay.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        dispatcher.DispatchCount.ShouldBe(1);

        string firstBody = await first.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string replayBody = await replay.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        AssertReplayMatchesStoredOutcome(firstBody, replayBody);

        using HttpResponseMessage response = await client
            .SendAsync(OperationStatusRequest("01ARZ3NDEKTSV4RRFFQ69G5FAX"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument status = JsonDocument.Parse(body);
        JsonElement root = status.RootElement;
        root.GetProperty("operationClass").GetString().ShouldBe("retry");
        root.GetProperty("retryCount").GetInt32().ShouldBe(1);
        root.GetProperty("maxAttempts").GetInt32().ShouldBe(5);
        root.GetProperty("originalOperationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("duplicateAttemptCount").GetInt32().ShouldBe(0);
        root.GetProperty("safeNextActions").EnumerateArray().Single().GetString().ShouldBe("none");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("operator reviewed", Case.Insensitive);
        body.ShouldNotContain("payload-sentinel", Case.Insensitive);
        body.ShouldNotContain("Secret Project", Case.Insensitive);
        body.ShouldNotContain("raw exception", Case.Insensitive);
    }

    [Fact]
    public async Task ProjectConversationEndpointShouldExposeStatusSummaryMetadataOnly()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        await conversationStore
            .UpsertAsync(ProjectConversationProjectionPendingItem(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using WebApplicationFactory<Program> factory = ProjectConversationFactory(conversationStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/api/v1/projects/project-alpha/conversation", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument document = JsonDocument.Parse(body);

        JsonElement item = document.RootElement.GetProperty("items").EnumerateArray().Single();
        JsonElement facets = item.GetProperty("statusSummary").GetProperty("facets");
        facets.EnumerateArray().Select(static facet => facet.GetProperty("domain").GetString()).ShouldBe(
            ["association", "attachment", "task", "approval", "command", "failure", "retry", "next-action"],
            ignoreOrder: false);

        JsonElement command = facets.EnumerateArray().Single(static facet => facet.GetProperty("domain").GetString() == "command");
        command.GetProperty("health").GetString().ShouldBe("degraded");
        command.GetProperty("completionStatus").GetString().ShouldBe("accepted-projection-pending");
        command.GetProperty("projectionStatus").GetString().ShouldBe("accepted-projection-pending");
        command.GetProperty("auditStatus").GetString().ShouldBe("reconciling");
        command.GetProperty("operationId").GetString().ShouldBe("audit-operation-001");
        command.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        command.GetProperty("safeNextAction").GetString().ShouldBe("wait-for-projection");
        item.GetProperty("classification").GetProperty("kind").GetString().ShouldBe("actionable");
        item.GetProperty("detectedIntent").GetProperty("safeNextAction").GetString().ShouldBe("wait-for-projection");
        item.GetProperty("reviewHistory").EnumerateArray().ShouldHaveSingleItem()
            .GetProperty("actionCode").GetString().ShouldBe("outcome");

        body.ShouldNotContain("commandBody", Case.Insensitive);
        body.ShouldNotContain("providerPayload", Case.Insensitive);
        body.ShouldNotContain("auditEnvelope", Case.Insensitive);
        body.ShouldNotContain("raw provider payload", Case.Insensitive);
        body.ShouldNotContain("/home/", Case.Insensitive);
    }

    [Fact]
    public async Task ProjectConversationEndpointShouldExposeCapturedTaskIntentMetadataOnly()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        await conversationStore
            .UpsertAsync(ProjectConversationTaskIntentSourceItem(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await conversationStore
            .UpsertTaskIntentAsync(ProjectConversationTaskIntentRecord(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using WebApplicationFactory<Program> factory = ProjectConversationFactory(conversationStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/api/v1/projects/project-alpha/conversation", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument document = JsonDocument.Parse(body);

        JsonElement item = document.RootElement.GetProperty("items").EnumerateArray().Single();
        JsonElement detectedIntent = item.GetProperty("detectedIntent");
        detectedIntent.GetProperty("summary").GetString().ShouldBe("authorized conversation item requests action");
        detectedIntent.GetProperty("actionKind").GetString().ShouldBe("request-action");
        detectedIntent.GetProperty("sourceEvidenceIds")
            .EnumerateArray()
            .Select(static evidence => evidence.GetString())
            .ShouldBe(["message:offset:001", "message:offset:002"], ignoreOrder: false);
        detectedIntent.GetProperty("safeNextAction").GetString().ShouldBe("review-task-intent-action");
        detectedIntent.GetProperty("messageCode").GetString().ShouldBe("task_intent_captured");
        detectedIntent.GetProperty("redactionState").GetString().ShouldBe("metadata_only");

        body.ShouldContain("task-intent:api", Case.Insensitive);
        body.ShouldNotContain("safe-token", Case.Insensitive);
        body.ShouldNotContain("raw mail body", Case.Insensitive);
        body.ShouldNotContain("providerPayload", Case.Insensitive);
        body.ShouldNotContain("prompt", Case.Insensitive);
        body.ShouldNotContain("toolArgs", Case.Insensitive);
    }

    [Fact]
    public async Task ProjectConversationEndpointShouldUseSdkProtectedCursorScope()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        await conversationStore
            .UpsertAsync(ProjectConversationProjectionPendingItem() with
            {
                ItemId = "conversation:item-a",
                OccurredAt = new DateTimeOffset(2026, 6, 1, 8, 11, 0, TimeSpan.Zero),
            }, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await conversationStore
            .UpsertAsync(ProjectConversationProjectionPendingItem() with
            {
                ItemId = "conversation:item-b",
                OccurredAt = new DateTimeOffset(2026, 6, 1, 8, 12, 0, TimeSpan.Zero),
            }, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using WebApplicationFactory<Program> factory = ProjectConversationFactory(conversationStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage firstResponse = await client
            .GetAsync("/api/v1/projects/project-alpha/conversation?pageSize=1", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        string firstBody = await firstResponse.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument first = JsonDocument.Parse(firstBody);
        first.RootElement.GetProperty("items").EnumerateArray().Single().GetProperty("itemId").GetString()
            .ShouldBe("conversation:item-b");
        string cursor = first.RootElement.GetProperty("page").GetProperty("nextCursor").GetString().ShouldNotBeNull();
        cursor.ShouldNotContain("tenant-alpha", Case.Sensitive);
        cursor.ShouldNotContain("project-alpha", Case.Sensitive);
        cursor.ShouldNotContain("conversation:item-b", Case.Sensitive);

        using HttpResponseMessage secondResponse = await client
            .GetAsync($"/api/v1/projects/project-alpha/conversation?pageSize=1&cursor={Uri.EscapeDataString(cursor)}", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        string secondBody = await secondResponse.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument second = JsonDocument.Parse(secondBody);
        second.RootElement.GetProperty("items").EnumerateArray().Single().GetProperty("itemId").GetString().ShouldBe("conversation:item-a");

        using HttpResponseMessage tamperedResponse = await client
            .GetAsync($"/api/v1/projects/project-alpha/conversation?pageSize=1&cursor={Uri.EscapeDataString(cursor + "tampered")}", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        tamperedResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string tamperedBody = await tamperedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        tamperedBody.ShouldContain("\"code\":\"authorization_denied\"");
        tamperedBody.ShouldNotContain(cursor, Case.Sensitive);
        tamperedBody.ShouldNotContain("tampered", Case.Insensitive);
    }

    [Fact]
    public async Task ProjectConversationEndpointShouldReturnAuthenticationDeniedForUnauthenticatedReads()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/api/v1/projects/project-alpha/conversation", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        // An unauthenticated read must surface AuthenticationDenied (401), not SafeNotFound (403): resolving tenant
        // before the project-scope check is what keeps that signal intact across the SDK query migration.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authentication_failure");
        root.GetProperty("code").GetString().ShouldBe("authentication_denied");
        body.ShouldNotContain("project-alpha", Case.Insensitive);
    }

    [Fact]
    public async Task TaskIntentReviewEndpointShouldReturnAuthenticationDeniedForUnauthenticatedReads()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/api/v1/projects/project-alpha/task-intents/task-intent-001", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authentication_failure");
        root.GetProperty("code").GetString().ShouldBe("authentication_denied");
        body.ShouldNotContain("project-alpha", Case.Insensitive);
        body.ShouldNotContain("task-intent-001", Case.Insensitive);
    }

    [Fact]
    public async Task ProjectConversationEndpointShouldRejectCursorMintedForDifferentScope()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        await conversationStore
            .UpsertAsync(ProjectConversationProjectionPendingItem(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using WebApplicationFactory<Program> factory = ProjectConversationFactory(conversationStore);
        using HttpClient client = factory.CreateClient();

        IQueryCursorCodec cursorCodec = factory.Services.GetRequiredService<IQueryCursorCodec>();
        string position = new ProjectConversationCursorPosition(
            new DateTimeOffset(2026, 6, 1, 8, 11, 0, TimeSpan.Zero),
            "conversation:item-a").ToProtectedPosition();

        // A cursor minted for a different tenant, project, or query discriminator must collapse to the safe
        // not-found denial and never leak the foreign scope or decoded position (AC3).
        (string Label, string Scope)[] mismatchedScopes =
        [
            ("wrong-tenant", QueryCursorScope.Create().Add("tenant", "tenant-other").Add("project", "project-alpha").Add("query", ChatBotReadQueryTypes.ProjectConversation).Build()),
            ("wrong-project", QueryCursorScope.Create().Add("tenant", "tenant-alpha").Add("project", "project-other").Add("query", ChatBotReadQueryTypes.ProjectConversation).Build()),
            ("wrong-query", QueryCursorScope.Create().Add("tenant", "tenant-alpha").Add("project", "project-alpha").Add("query", "some-other-query").Build()),
        ];

        foreach ((string label, string scope) in mismatchedScopes)
        {
            string foreignCursor = cursorCodec.Encode(ChatBotReadQueryTypes.ProjectConversation, scope, position);
            using HttpResponseMessage response = await client
                .GetAsync(
                    $"/api/v1/projects/project-alpha/conversation?pageSize=1&cursor={Uri.EscapeDataString(foreignCursor)}",
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, $"scope mismatch '{label}' should collapse to safe not-found");
            string body = await response.Content
                .ReadAsStringAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            body.ShouldContain("\"code\":\"authorization_denied\"");
            body.ShouldNotContain(foreignCursor, Case.Sensitive);
            body.ShouldNotContain("conversation:item-a", Case.Sensitive);
            body.ShouldNotContain("tenant-other", Case.Insensitive);
            body.ShouldNotContain("project-other", Case.Insensitive);
        }
    }

    [Fact]
    public async Task ProjectConversationEndpointShouldOmitDetectedIntentWhenTaskIntentCaptureFailsClosed()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        await conversationStore
            .UpsertAsync(ProjectConversationRedactedNonActionableSourceItem(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using WebApplicationFactory<Program> factory = ProjectConversationFactory(conversationStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/api/v1/projects/project-alpha/conversation", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument document = JsonDocument.Parse(body);

        JsonElement item = document.RootElement.GetProperty("items").EnumerateArray().Single();
        item.GetProperty("classification").GetProperty("kind").GetString().ShouldBe("informational");
        item.GetProperty("classification").GetProperty("messageCode").GetString().ShouldBe("conversation_item_informational");
        item.GetProperty("classification").GetProperty("redactionState").GetString().ShouldBe("redacted");
        if (item.TryGetProperty("detectedIntent", out JsonElement detectedIntent))
        {
            detectedIntent.ValueKind.ShouldBe(JsonValueKind.Null);
        }

        body.ShouldNotContain("task-intent:", Case.Insensitive);
        body.ShouldNotContain("restricted-resource", Case.Insensitive);
        body.ShouldNotContain("raw mail body", Case.Insensitive);
        body.ShouldNotContain("providerPayload", Case.Insensitive);
        body.ShouldNotContain("prompt", Case.Insensitive);
        body.ShouldNotContain("toolArgs", Case.Insensitive);
    }

    [Fact]
    public async Task ProjectConversationEndpointShouldExposeAttachmentStatusesAndUnsafeActionsMetadataOnly()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        ContractProjectConversationAttachmentStatus[] statuses =
        [
            ContractProjectConversationAttachmentStatus.Captured,
            ContractProjectConversationAttachmentStatus.Pending,
            ContractProjectConversationAttachmentStatus.Unavailable,
            ContractProjectConversationAttachmentStatus.Rejected,
            ContractProjectConversationAttachmentStatus.Unsafe,
            ContractProjectConversationAttachmentStatus.Failed,
            ContractProjectConversationAttachmentStatus.Retryable,
        ];
        for (int index = 0; index < statuses.Length; index++)
        {
            await conversationStore
                .UpsertAsync(ProjectConversationAttachmentApiItem(statuses[index], index), TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        using WebApplicationFactory<Program> factory = ProjectConversationFactory(conversationStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/api/v1/projects/project-alpha/conversation", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement[] items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();

        items.Select(static item => item.GetProperty("attachmentScanStatus").GetString()).ShouldBe(
            ["captured", "pending", "unavailable", "rejected", "unsafe", "failed", "retryable"],
            ignoreOrder: true);
        JsonElement captured = items.Single(static item => item.GetProperty("attachmentScanStatus").GetString() == "captured");
        captured.GetProperty("attachmentAiContextEligibility").GetString().ShouldBe("eligible");
        captured.GetProperty("attachmentAllowedActions").EnumerateArray().Select(static action => action.GetString()).ShouldBe(
            ["open-governed-file", "add-to-ai-context"],
            ignoreOrder: false);
        captured.GetProperty("attachmentFolderId").GetString().ShouldBe("folder-reference-api");
        captured.GetProperty("attachmentFileId").GetString().ShouldBe("file-reference-api");

        foreach (JsonElement restricted in items.Where(static item => item.GetProperty("attachmentScanStatus").GetString() is not "captured"))
        {
            restricted.GetProperty("attachmentAiContextEligibility").GetString().ShouldBe("not-eligible");
            restricted.GetProperty("attachmentAllowedActions").GetArrayLength().ShouldBe(0);
            restricted.GetProperty("attachmentFolderId").ValueKind.ShouldBe(JsonValueKind.Null);
            restricted.GetProperty("attachmentFileId").ValueKind.ShouldBe(JsonValueKind.Null);
        }

        body.ShouldContain("\"safeNextAction\":\"quarantine-review\"");
        body.ShouldContain("\"safeNextAction\":\"retry-scan\"");
        body.ShouldNotContain("unsafe-malware-sample.exe", Case.Insensitive);
        body.ShouldNotContain("raw attachment bytes", Case.Insensitive);
        body.ShouldNotContain("raw scanner payload", Case.Insensitive);
        body.ShouldNotContain("malware family", Case.Insensitive);
        body.ShouldNotContain("C:\\", Case.Insensitive);
        body.ShouldNotContain("/tmp/", Case.Insensitive);
    }

    [Fact]
    public async Task ProjectConversationEndpointShouldExposeAiContextPackageManifestMetadataOnly()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        await conversationStore
            .UpsertAsync(ProjectConversationAiContextPolicyCarrier(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await conversationStore
            .UpsertAsync(ProjectConversationAttachmentApiItem(ContractProjectConversationAttachmentStatus.Captured, 0) with
            {
                ItemId = "attachment:ai-context:included",
                SourceVersion = 31,
                SourceProviderAttachmentId = "provider-ai-context-included",
                AttachmentFolderId = "folder-ai-context",
                AttachmentFileId = "file-ai-context",
                EvidenceReferenceSummary = ["attachment:evidence:included"],
            }, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await conversationStore
            .UpsertAsync(ProjectConversationAttachmentApiItem(ContractProjectConversationAttachmentStatus.Pending, 1) with
            {
                ItemId = "attachment:ai-context:pending",
                SourceVersion = 32,
                SourceProviderAttachmentId = "provider-ai-context-pending",
                EvidenceReferenceSummary = ["attachment:evidence:pending"],
            }, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using WebApplicationFactory<Program> factory = ProjectConversationFactory(conversationStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/api/v1/projects/project-alpha/conversation", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement package = document.RootElement.GetProperty("aiContextPackage");
        string tenantReference = package.GetProperty("tenantId").GetString()
            ?? throw new InvalidOperationException("AI-context package tenant reference is required.");

        tenantReference.ShouldStartWith("tenant:");
        tenantReference.ShouldNotContain("tenant-alpha", Case.Sensitive);
        package.GetProperty("projectId").GetString().ShouldBe("project-alpha");
        package.GetProperty("policySnapshotId").GetString().ShouldBe("policy-snapshot-ai-context-v1");
        package.GetProperty("redactionDecision").GetString().ShouldBe("metadata_only");
        package.GetProperty("retentionClass").GetString().ShouldBe("collaboration_input");
        package.GetProperty("providerReuseSetting").GetString().ShouldBe("disabled");
        package.GetProperty("packageId").GetString().ShouldNotBeNullOrWhiteSpace();
        package.GetProperty("packageVersion").GetString().ShouldBe("v1");
        package.GetProperty("schemaVersion").GetString().ShouldBe("chatbot.project-ai-context-package.v1");
        package.GetProperty("sourceVersion").GetInt64().ShouldBe(32);
        package.GetProperty("correlationId").GetString().ShouldNotBeNullOrWhiteSpace();
        package.GetProperty("sourceProvenance").GetString().ShouldBe("m365-mailbox-intake");
        package.GetProperty("derivationKernelVersion").GetString().ShouldBe("chatbot.project-ai-context-package.kernel.v1");

        JsonElement included = package.GetProperty("includedFiles").EnumerateArray().ShouldHaveSingleItem();
        included.GetProperty("folderId").GetString().ShouldBe("folder-ai-context");
        included.GetProperty("fileId").GetString().ShouldBe("file-ai-context");
        included.GetProperty("sourceProviderAttachmentId").GetString().ShouldBe("provider-ai-context-included");
        included.GetProperty("redactionState").GetString().ShouldBe("metadata_only");
        included.GetProperty("retentionClass").GetString().ShouldBe("collaboration_input");
        included.GetProperty("sourceEvidenceReference").GetString().ShouldBe("graph-conversation-001");

        JsonElement excluded = package.GetProperty("excludedFiles").EnumerateArray().ShouldHaveSingleItem();
        string excludedReferenceToken = excluded.GetProperty("referenceToken").GetString()
            ?? throw new InvalidOperationException("AI-context package exclusion reference token is required.");
        excluded.GetProperty("reasonCode").GetString().ShouldBe("pending-scan");
        excludedReferenceToken.ShouldNotContain("provider-ai-context-pending", Case.Sensitive);
        excluded.GetProperty("sourceEvidenceReference").GetString().ShouldBe("graph-conversation-001");

        package.GetProperty("sourceEvidenceReferences")
            .EnumerateArray()
            .Select(static reference => reference.GetString())
            .ShouldContain("mailbox:evidence:001");
        string packageBody = package.GetRawText();
        packageBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
        packageBody.ShouldNotContain("displayName", Case.Insensitive);
        packageBody.ShouldNotContain("contentType", Case.Insensitive);
        packageBody.ShouldNotContain("raw attachment bytes", Case.Insensitive);
        packageBody.ShouldNotContain("raw scanner payload", Case.Insensitive);
        packageBody.ShouldNotContain("malware family", Case.Insensitive);
        packageBody.ShouldNotContain("C:\\", Case.Insensitive);
        packageBody.ShouldNotContain("/tmp/", Case.Insensitive);
    }

    [Fact]
    public async Task ProjectConversationEndpointShouldPartitionAiContextPackageWithStableExclusionReasons()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        await conversationStore
            .UpsertAsync(ProjectConversationAiContextPolicyCarrier(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await conversationStore
            .UpsertAsync(ProjectConversationAttachmentApiItem(ContractProjectConversationAttachmentStatus.Captured, 10) with
            {
                ItemId = "attachment:ai-context:captured",
                SourceVersion = 41,
                SourceProviderAttachmentId = "provider-ai-context-captured",
                AttachmentFolderId = "folder-ai-context-captured",
                AttachmentFileId = "file-ai-context-captured",
                EvidenceReferenceSummary = ["attachment:evidence:captured"],
            }, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await conversationStore
            .UpsertAsync(ProjectConversationAttachmentApiItem(ContractProjectConversationAttachmentStatus.Pending, 11) with
            {
                ItemId = "attachment:ai-context:pending",
                SourceVersion = 42,
                SourceProviderAttachmentId = "provider-ai-context-pending",
                EvidenceReferenceSummary = ["attachment:evidence:pending"],
            }, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await conversationStore
            .UpsertAsync(ProjectConversationAttachmentApiItem(ContractProjectConversationAttachmentStatus.Unsafe, 12) with
            {
                ItemId = "attachment:ai-context:unsafe",
                SourceVersion = 43,
                SourceProviderAttachmentId = "provider-ai-context-unsafe",
                EvidenceReferenceSummary = ["attachment:evidence:unsafe"],
            }, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await conversationStore
            .UpsertAsync(ProjectConversationAttachmentApiItem(ContractProjectConversationAttachmentStatus.Captured, 13) with
            {
                ItemId = "attachment:ai-context:policy-denied",
                SourceVersion = 44,
                SourceProviderAttachmentId = "provider-ai-context-policy-denied",
                AttachmentFolderId = "folder-ai-context-policy-denied",
                AttachmentFileId = "file-ai-context-policy-denied",
                AttachmentAllowedActions = [],
                EvidenceReferenceSummary = ["attachment:evidence:policy-denied"],
            }, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await conversationStore
            .UpsertAsync(ProjectConversationAttachmentApiItem(ContractProjectConversationAttachmentStatus.Captured, 14) with
            {
                ItemId = "attachment:ai-context:redacted",
                SourceVersion = 45,
                SourceProviderAttachmentId = "provider-ai-context-redacted-secret",
                AttachmentFolderId = "folder-ai-context-redacted-secret",
                AttachmentFileId = "file-ai-context-redacted-secret",
                RedactionState = "redacted",
                AttachmentRedactionState = "redacted",
                AttachmentAiContextEligibility = "redacted",
                EvidenceReferenceSummary = ["secret-evidence-redacted"],
            }, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using WebApplicationFactory<Program> factory = ProjectConversationFactory(conversationStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(ProjectConversationRequest("project-alpha"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull();
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement package = document.RootElement.GetProperty("aiContextPackage");

        package.GetProperty("policySnapshotId").GetString().ShouldBe("policy-snapshot-ai-context-v1");
        package.GetProperty("sourceVersion").GetInt64().ShouldBe(45);
        package.GetProperty("redactionDecision").GetString().ShouldBe("metadata_only");
        package.GetProperty("retentionClass").GetString().ShouldBe("collaboration_input");
        package.GetProperty("providerReuseSetting").GetString().ShouldBe("disabled");

        JsonElement included = package.GetProperty("includedFiles").EnumerateArray().ShouldHaveSingleItem();
        included.GetProperty("folderId").GetString().ShouldBe("folder-ai-context-captured");
        included.GetProperty("fileId").GetString().ShouldBe("file-ai-context-captured");
        included.GetProperty("sourceProviderAttachmentId").GetString().ShouldBe("provider-ai-context-captured");
        included.GetProperty("sourceEvidenceReference").GetString().ShouldBe("graph-conversation-001");

        JsonElement[] excluded = package.GetProperty("excludedFiles").EnumerateArray().ToArray();
        excluded.Select(static item => item.GetProperty("reasonCode").GetString()).ShouldBe(
            ["pending-scan", "policy-denied", "redacted", "unsafe"],
            ignoreOrder: true);
        excluded.Single(static item => item.GetProperty("reasonCode").GetString() == "pending-scan")
            .GetProperty("sourceEvidenceReference")
            .GetString()
            .ShouldBe("graph-conversation-001");
        excluded.Single(static item => item.GetProperty("reasonCode").GetString() == "unsafe")
            .GetProperty("sourceEvidenceReference")
            .GetString()
            .ShouldBe("graph-conversation-001");
        excluded.Single(static item => item.GetProperty("reasonCode").GetString() == "policy-denied")
            .GetProperty("sourceEvidenceReference")
            .GetString()
            .ShouldBe("graph-conversation-001");
        JsonElement redacted = excluded.Single(static item => item.GetProperty("reasonCode").GetString() == "redacted");
        redacted.GetProperty("sourceEvidenceReference").ValueKind.ShouldBe(JsonValueKind.Null);
        string redactedReferenceToken = redacted.GetProperty("referenceToken").GetString()
            ?? throw new InvalidOperationException("AI-context redacted exclusion reference token is required.");
        redactedReferenceToken.ShouldStartWith("attachment:redacted:");
        redactedReferenceToken.ShouldNotContain("provider-ai-context-redacted-secret", Case.Sensitive);
        redactedReferenceToken.ShouldNotContain("folder-ai-context-redacted-secret", Case.Sensitive);
        redactedReferenceToken.ShouldNotContain("file-ai-context-redacted-secret", Case.Sensitive);

        package.GetProperty("sourceEvidenceReferences")
            .EnumerateArray()
            .Select(static reference => reference.GetString())
            .ShouldContain("mailbox:evidence:001");
        string packageBody = package.GetRawText();
        packageBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
        packageBody.ShouldNotContain("secret-evidence-redacted", Case.Sensitive);
        packageBody.ShouldNotContain("provider-ai-context-redacted-secret", Case.Sensitive);
        packageBody.ShouldNotContain("folder-ai-context-redacted-secret", Case.Sensitive);
        packageBody.ShouldNotContain("file-ai-context-redacted-secret", Case.Sensitive);
        packageBody.ShouldNotContain("displayName", Case.Insensitive);
        packageBody.ShouldNotContain("contentType", Case.Insensitive);
        packageBody.ShouldNotContain("raw attachment bytes", Case.Insensitive);
        packageBody.ShouldNotContain("raw scanner payload", Case.Insensitive);
        packageBody.ShouldNotContain("malware family", Case.Insensitive);
        packageBody.ShouldNotContain("C:\\", Case.Insensitive);
        packageBody.ShouldNotContain("/tmp/", Case.Insensitive);
    }

    [Fact]
    public async Task ProjectConversationEndpointShouldReturnNotModifiedForMatchingEtag()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        await conversationStore
            .UpsertAsync(ProjectConversationAiContextPolicyCarrier(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using WebApplicationFactory<Program> factory = ProjectConversationFactory(conversationStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client
            .GetAsync("/api/v1/projects/project-alpha/conversation", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Headers.ETag.ShouldNotBeNull();

        using HttpRequestMessage secondRequest = ProjectConversationRequest("project-alpha");
        secondRequest.Headers.IfNoneMatch.Add(first.Headers.ETag);
        using HttpResponseMessage second = await client
            .SendAsync(secondRequest, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        second.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        string body = await second.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        body.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProjectConversationEndpointShouldOmitAiContextPackageFromRedactedDenials()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        await conversationStore
            .UpsertAsync(ProjectConversationAiContextPolicyCarrier(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using WebApplicationFactory<Program> factory = ProjectConversationFactory(conversationStore);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage foreignProjectResponse = await client
            .SendAsync(ProjectConversationRequest("project-beta"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage unknownProjectResponse = await client
            .SendAsync(ProjectConversationRequest("project-missing"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        foreignProjectResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        unknownProjectResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string foreignProjectBody = await foreignProjectResponse.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string unknownProjectBody = await unknownProjectResponse.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        foreignProjectBody.ShouldBe(unknownProjectBody);
        foreignProjectBody.ShouldNotContain("aiContextPackage", Case.Insensitive);
        foreignProjectBody.ShouldNotContain("project-alpha", Case.Insensitive);
        foreignProjectBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
        foreignProjectBody.ShouldNotContain("folder-ai-context", Case.Insensitive);
        foreignProjectBody.ShouldNotContain("file-ai-context", Case.Insensitive);
        foreignProjectBody.ShouldNotContain("provider-ai-context", Case.Insensitive);
    }

    [Fact]
    public async Task ProjectConversationEndpointShouldDenyAuthenticatedActorWithoutProjectScope()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        await conversationStore
            .UpsertAsync(ProjectConversationAiContextPolicyCarrier(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<IProjectConversationProjectionStore>(conversationStore));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(ProjectConversationRequest("project-alpha"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        body.ShouldNotContain("aiContextPackage", Case.Insensitive);
        body.ShouldNotContain("project-alpha", Case.Insensitive);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
    }

    [Fact]
    public async Task OperationStatusEndpointShouldCollapseCrossTenantAndUnknownOperations()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage command = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "allowed-resource"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        command.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        using HttpRequestMessage crossTenant = OperationStatusRequest("01ARZ3NDEKTSV4RRFFQ69G5FAX", "tenant-beta");
        using HttpRequestMessage unknown = OperationStatusRequest("01ARZ3NDEKTSV4RRFFQ69G5FAV", "tenant-beta");
        using HttpResponseMessage crossTenantResponse = await client
            .SendAsync(crossTenant, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage unknownResponse = await client
            .SendAsync(unknown, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        crossTenantResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        unknownResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string crossTenantBody = await crossTenantResponse.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string unknownBody = await unknownResponse.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument crossTenantProblem = JsonDocument.Parse(crossTenantBody);
        using JsonDocument unknownProblem = JsonDocument.Parse(unknownBody);
        crossTenantProblem.RootElement.GetProperty("code").GetString().ShouldBe("authorization_denied");
        unknownProblem.RootElement.GetProperty("code").GetString().ShouldBe("authorization_denied");
        // Tenant isolation by construction: a cross-tenant id and an unknown id must be byte-for-byte
        // indistinguishable to the caller (same status, same headers, same body) so neither confirms existence.
        crossTenantResponse.StatusCode.ShouldBe(unknownResponse.StatusCode);
        crossTenantResponse.Headers.GetValues("X-Correlation-Id").Single()
            .ShouldBe(unknownResponse.Headers.GetValues("X-Correlation-Id").Single());
        crossTenantBody.ShouldBe(unknownBody);
        crossTenantBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
        crossTenantBody.ShouldNotContain("tenant-beta", Case.Insensitive);
        unknownBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
        unknownBody.ShouldNotContain("tenant-beta", Case.Insensitive);
    }

    [Fact]
    public async Task AuditHistoryEndpointShouldReturnMetadataOnlyPostCommitSummaryForTheOperation()
    {
        // Story 1.9 M3: the UI's audit-history surface is a REAL tenant-scoped, metadata-only read of the
        // operation's post-commit audit envelope summary through the spine — not a client-side fabrication.
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage command = await client
            .SendAsync(RecordGovernedNoteRequest("01ARZ3NDEKTSV4RRFFQ69G5FAZ", origin: "ui"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        command.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        using HttpResponseMessage response = await client
            .SendAsync(AuditHistoryRequest("01ARZ3NDEKTSV4RRFFQ69G5FAX"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument history = JsonDocument.Parse(body);
        JsonElement root = history.RootElement;
        root.GetProperty("operationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("auditStatus").GetString().ShouldBe("committed");
        JsonElement entries = root.GetProperty("entries");
        entries.GetArrayLength().ShouldBe(1);
        JsonElement entry = entries[0];
        entry.GetProperty("phase").GetString().ShouldBe("post-commit");
        entry.GetProperty("decision").GetString().ShouldBe("allow");
        entry.GetProperty("reasonCode").GetString().ShouldBe("eventstore_dispatch_accepted");
        entry.GetProperty("outcome").GetString().ShouldBe("proposed");
        entry.GetProperty("redactionDecision").GetString().ShouldBe("metadata_only");
        entry.GetProperty("surfaceOrigin").GetString().ShouldBe("ui");
        entry.GetProperty("resourceId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAZ");
        entry.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        // Metadata-only: the tenant id is the read scope, never echoed into the body.
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
    }

    [Fact]
    public async Task AuditHistoryEndpointShouldCollapseCrossTenantUnknownAndInvalidToSafeNotFound()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage command = await client
            .SendAsync(RecordGovernedNoteRequest("01ARZ3NDEKTSV4RRFFQ69G5FAZ", origin: "ui"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        command.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        // A foreign tenant reading the very same operation id, an unknown operation, and a malformed id all
        // collapse to the identical safe-not-found (403) so the read never confirms existence across the boundary.
        using HttpResponseMessage crossTenant = await client
            .SendAsync(AuditHistoryRequest("01ARZ3NDEKTSV4RRFFQ69G5FAX", "tenant-beta"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage unknown = await client
            .SendAsync(AuditHistoryRequest("01ARZ3NDEKTSV4RRFFQ69G5FAV", "tenant-beta"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage invalid = await client
            .SendAsync(AuditHistoryRequest("payload-sentinel-raw-operation"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        crossTenant.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        unknown.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        invalid.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string crossTenantBody = await crossTenant.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        string unknownBody = await unknown.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        crossTenantBody.ShouldBe(unknownBody);
        using JsonDocument problem = JsonDocument.Parse(crossTenantBody);
        problem.RootElement.GetProperty("code").GetString().ShouldBe("authorization_denied");
        crossTenantBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
        crossTenantBody.ShouldNotContain("tenant-beta", Case.Insensitive);
        crossTenantBody.ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task AuditHistoryEndpointShouldRequireAuthentication()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(AuditHistoryRequest("01ARZ3NDEKTSV4RRFFQ69G5FAX"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("code").GetString().ShouldBe("authentication_denied");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
    }

    [Fact]
    public async Task ChatBotCompatibilityHealthEndpointShouldRejectUnsupportedMethods()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsync("/health/chatbot", null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task CommandEndpointShouldRejectNonAllowlistedCommandFailClosed()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            static services => services.AddSingleton<ISpineCommandAllowlist, ChatBotSpineCommandAllowlist>());
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "payload-sentinel"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        body.ShouldContain("refusal_blocked_action");
        body.ShouldNotContain("payload-sentinel", Case.Insensitive);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);

        // Fail-closed means no durable end-state: querying the operation status for the rejected submission's
        // task id collapses to the indistinguishable safe-not-found (403), proving the rejection created no
        // operation-status record — not just that the HTTP response was a 403.
        using HttpResponseMessage status = await client
            .SendAsync(OperationStatusRequest("01ARZ3NDEKTSV4RRFFQ69G5FAX"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        status.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string statusBody = await status.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument statusProblem = JsonDocument.Parse(statusBody);
        statusProblem.RootElement.GetProperty("code").GetString().ShouldBe("authorization_denied");
    }

    [Fact]
    public async Task CommandEndpointShouldAdmitAllowlistedRecordGovernedNote()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            static services => services.AddSingleton<ISpineCommandAllowlist, ChatBotSpineCommandAllowlist>());
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(RecordGovernedNoteRequest("01ARZ3NDEKTSV4RRFFQ69G5FAZ", origin: "ui"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        accepted.RootElement.GetProperty("lifecycleState").GetString().ShouldBe("Proposed");
    }

    [Fact]
    public async Task CommandEndpointShouldAdmitAllowedLowRiskAiAssistanceOnceAndReplayDuplicate()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAiAssistanceProvider provider = new("success");
        InMemoryAuditWriter auditWriter = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services =>
            {
                services.AddSingleton<ISpineCommandAllowlist, ChatBotSpineCommandAllowlist>();
                services.AddSingleton<IEventStoreGatewayClient>(eventStore);
                services.AddSingleton<IAiAssistanceProvider>(provider);
                services.AddSingleton<IAuditWriter>(auditWriter);
                services.AddSingleton<ITenantAiPolicySnapshotProvider>(new FixedTenantAiPolicySnapshotProvider(lowRiskAllowed: true));
            });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client
            .SendAsync(LowRiskAiAssistanceRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage replay = await client
            .SendAsync(LowRiskAiAssistanceRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        replay.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        string firstBody = await first.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        string replayBody = await replay.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        AssertReplayMatchesStoredOutcome(firstBody, replayBody);

        provider.ExecuteCount.ShouldBe(0);
        provider.LastRequest.ShouldBeNull();

        SubmitCommandRequest submitted = eventStore.Submitted.ShouldHaveSingleItem();
        submitted.CommandType.ShouldBe("ExecuteLowRiskAIAssistance");
        submitted.AggregateId.ShouldBe("project-001");
        submitted.Payload.GetProperty("ExecutionRecord").ValueKind.ShouldBe(JsonValueKind.Null);
        string submittedPayload = submitted.Payload.GetRawText();
        submittedPayload.ShouldNotContain("raw prompt", Case.Insensitive);
        submittedPayload.ShouldNotContain("raw provider payload", Case.Insensitive);
        submittedPayload.ShouldNotContain("/home/administrator", Case.Insensitive);
        submittedPayload.ShouldNotContain("secret", Case.Insensitive);

        auditWriter.Envelopes.Count.ShouldBe(2);
        auditWriter.Envelopes.ShouldAllBe(static envelope =>
            envelope.SourceEvidenceRefs.Contains("low-risk-policy-reason:low-risk-execute-allowed") &&
            envelope.SourceEvidenceRefs.Contains("context-package:context-package-001") &&
            envelope.SourceEvidenceRefs.Contains("execution:ai-execution-001"));
    }

    [Fact]
    public async Task CommandEndpointShouldRoutePolicyFalseLowRiskAiAssistanceToApprovalWithoutProviderCall()
    {
        RecordingEventStoreGatewayClient eventStore = new();
        RecordingAiAssistanceProvider provider = new("success");
        InMemoryAuditWriter auditWriter = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services =>
            {
                services.AddSingleton<ISpineCommandAllowlist, ChatBotSpineCommandAllowlist>();
                services.AddSingleton<IEventStoreGatewayClient>(eventStore);
                services.AddSingleton<IAiAssistanceProvider>(provider);
                services.AddSingleton<IAuditWriter>(auditWriter);
                services.AddSingleton<ITenantAiPolicySnapshotProvider>(new FixedTenantAiPolicySnapshotProvider(lowRiskAllowed: false));
            });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(LowRiskAiAssistanceRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        provider.ExecuteCount.ShouldBe(0);
        SubmitCommandRequest submitted = eventStore.Submitted.ShouldHaveSingleItem();
        submitted.Payload.GetProperty("ExecutionRecord").GetProperty("Outcome").GetString().ShouldBe("pending-approval");
        submitted.Payload.GetProperty("ExecutionRecord").GetProperty("PolicyReasonCode").GetString().ShouldBe("low_risk_policy_false");
        submitted.Payload.GetProperty("ExecutionRecord").GetProperty("SafeNextAction").GetString().ShouldBe("review-ai-action");
        auditWriter.Envelopes.Count.ShouldBe(2);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("raw provider payload", Case.Insensitive);
        body.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task CommandEndpointShouldRouteAmbiguousAssociationToNeedsReviewThroughCommandSpine()
    {
        RoutingEventStoreGatewayClient eventStore = new();
        RecordingProjectDirectory projectDirectory = new(
            new ProjectDirectoryAssociationResult(
                true,
                [new ProjectAssociationCandidateEvidence("project-001", "Project One", [AssociationSignal(0.75)])],
                []));
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services =>
            {
                services.AddSingleton<ISpineCommandAllowlist, ChatBotSpineCommandAllowlist>();
                services.AddSingleton<IProjectDirectory>(projectDirectory);
                services.AddSingleton<IEventStoreGatewayClient>(eventStore);
            });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(AssociationScoringRequest(signalWeight: 0.75), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        body.ShouldNotContain("Project One", Case.Sensitive);
        body.ShouldNotContain("project-001", Case.Insensitive);

        projectDirectory.Request.ShouldNotBeNull();
        projectDirectory.Request.TenantId.ShouldBe("tenant-alpha");
        projectDirectory.Request.CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");

        SubmitCommandRequest submitted = eventStore.Submitted.ShouldHaveSingleItem();
        submitted.Tenant.ShouldBe("tenant-alpha");
        submitted.Domain.ShouldBe(ChatBotEventStore.DomainName);
        submitted.AggregateId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAB");
        submitted.CommandType.ShouldBe("ScoreMailboxMessageAssociation");

        MailboxAssociationCandidatesGenerated routed = eventStore.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxAssociationCandidatesGenerated>();
        routed.LifecycleState.ShouldBe(ContractLifecycleState.NeedsReview);
        routed.Outcome.ShouldBe(ContractAssociationScoringOutcome.CandidatesGenerated);
        routed.ThresholdBand.ShouldBe(ContractAssociationThresholdBand.Ambiguous);
        routed.Candidates.ShouldHaveSingleItem().ProjectId.ShouldBe("project-001");
        routed.CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
    }

    [Fact]
    public async Task CommandEndpointShouldFailClosedToNeedsReviewWhenProjectEvidenceIsUnavailable()
    {
        RoutingEventStoreGatewayClient eventStore = new();
        RecordingProjectDirectory projectDirectory = new(ProjectDirectoryAssociationResult.Unavailable(
            [
                new ContractAssociationExclusion(
                    "project-001",
                    ContractAssociationExclusionState.Unavailable,
                    ContractAssociationReasonCode.AuthorizationEvidenceUnavailable,
                    "mailbox:project-id",
                    "hash-project"),
            ]));
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services =>
            {
                services.AddSingleton<ISpineCommandAllowlist, ChatBotSpineCommandAllowlist>();
                services.AddSingleton<IProjectDirectory>(projectDirectory);
                services.AddSingleton<IEventStoreGatewayClient>(eventStore);
            });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(AssociationScoringRequest(signalWeight: 0.9), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        body.ShouldNotContain("project-001", Case.Insensitive);

        MailboxAssociationScoringFailedClosed routed = eventStore.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxAssociationScoringFailedClosed>();
        routed.LifecycleState.ShouldBe(ContractLifecycleState.NeedsReview);
        routed.ThresholdBand.ShouldBe(ContractAssociationThresholdBand.FailClosed);
        routed.ConfidenceScore.ShouldBe(0.0);
        routed.ReasonCodes.ShouldBe([ContractAssociationReasonCode.AuthorizationEvidenceUnavailable]);
        routed.Exclusions.ShouldHaveSingleItem().State.ShouldBe(ContractAssociationExclusionState.Unavailable);
    }

    [Fact]
    public async Task CommandEndpointShouldCaptureBodyDeclaredSurfaceOriginImmutablyIntoEveryAuditEnvelope()
    {
        // AC2: the surface origin declared in the request body is captured once at the adapter boundary and
        // travels into the audit envelope; the pre- and post-commit envelopes carry the identical origin
        // because no downstream stage can rewrite the immutable submission.
        InMemoryAuditWriter auditWriter = await SubmitGovernedNoteCapturingAudit(bodyOrigin: "ui").ConfigureAwait(true);

        auditWriter.Envelopes.Count.ShouldBe(2);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SurfaceOrigin == "ui");
        auditWriter.Envelopes.Select(static envelope => envelope.SurfaceOrigin).Distinct(StringComparer.Ordinal).Count().ShouldBe(1);
    }

    [Fact]
    public async Task CommandEndpointShouldFallBackToSurfaceOriginHeaderWhenBodyOriginIsAbsent()
    {
        // AC2: with no body origin, the X-Hexalith-Surface-Origin header is the declared provenance.
        InMemoryAuditWriter auditWriter = await SubmitGovernedNoteCapturingAudit(headerOrigin: "ui").ConfigureAwait(true);

        auditWriter.Envelopes.ShouldNotBeEmpty();
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SurfaceOrigin == "ui");
    }

    [Fact]
    public async Task CommandEndpointBodyDeclaredSurfaceOriginShouldTakePrecedenceOverHeader()
    {
        // AC2: the body declaration wins over the header when both are present.
        InMemoryAuditWriter auditWriter = await SubmitGovernedNoteCapturingAudit(bodyOrigin: "ui", headerOrigin: "api").ConfigureAwait(true);

        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SurfaceOrigin == "ui");
    }

    [Fact]
    public async Task CommandEndpointShouldDefaultSurfaceOriginToApiWhenNeitherBodyNorHeaderDeclareIt()
    {
        // AC2: an absent declaration collapses to the safe default and is still audited (never rejected).
        InMemoryAuditWriter auditWriter = await SubmitGovernedNoteCapturingAudit().ConfigureAwait(true);

        auditWriter.Envelopes.ShouldNotBeEmpty();
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SurfaceOrigin == "api");
    }

    [Fact]
    public async Task CommandEndpointShouldRejectUnknownSurfaceOriginWithSafeProblem()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.SendAsync(
            RecordGovernedNoteRequest("01ARZ3NDEKTSV4RRFFQ69G5FAZ", "totally-unknown-surface"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    private static async Task<InMemoryAuditWriter> SubmitGovernedNoteCapturingAudit(string? bodyOrigin = null, string? headerOrigin = null)
    {
        InMemoryAuditWriter auditWriter = new();
        using WebApplicationFactory<Program> factory = AuthenticatedFactory(
            "tenant-alpha",
            services => services.AddSingleton<IAuditWriter>(auditWriter));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(
                RecordGovernedNoteRequest("01ARZ3NDEKTSV4RRFFQ69G5FAZ", bodyOrigin, headerOrigin),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        // The note must be admitted and dispatched so both pre- and post-commit envelopes are written.
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        return auditWriter;
    }

    private static WebApplicationFactory<Program> AuthenticatedFactory(
        string tenantId,
        Action<IServiceCollection>? configureServices = null)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.ConfigureServices(
                    services =>
                    {
                        services.AddSingleton<IStartupFilter>(new TestPrincipalStartupFilter(tenantId));
                        services.AddSingleton<IIdempotencyStore>(_ => new InMemoryCoarseIdempotencyStore(new SystemClock()));

                        // The real AcceptedCommandDispatcher submits to EventStore over HTTP; there is no
                        // EventStore gateway running in these in-process tests, so substitute an accepting fake.
                        // Tests that override ICommandDispatcher never reach it; tests that exercise real dispatch
                        // (accept/replay/operation-status/allowlisted-note) rely on this accepting fake.
                        services.AddSingleton<IEventStoreGatewayClient>(_ => new AcceptingEventStoreGatewayClient());

                        // These bootstrap tests exercise admission/redaction/idempotency/status paths with a
                        // generic command, so they default to a permissive spine allowlist. Allowlist
                        // enforcement is covered by the dedicated tests below, which re-register the real
                        // ChatBotSpineCommandAllowlist via configureServices (last registration wins).
                        services.AddSingleton<ISpineCommandAllowlist>(_ => new AllowAllSpineCommandAllowlist());
                        configureServices?.Invoke(services);
                    }));

    private static WebApplicationFactory<Program> ProjectConversationFactory(InMemoryProjectConversationProjectionStore conversationStore)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.ConfigureServices(
                    services =>
                    {
                        services.AddSingleton<IProjectConversationProjectionStore>(conversationStore);
                        services.AddSingleton<IStartupFilter>(new ProjectConversationPrincipalStartupFilter());
                    }));

    private static ProjectConversationItemView ProjectConversationProjectionPendingItem()
        => new(
            "tenant-alpha",
            "project-alpha",
            "Authorized Project",
            "approval:approval-001:outcome:12",
            "intake-001",
            ContractProjectConversationItemKind.ApprovalEvent,
            ContractProjectConversationActorKind.ApprovalSystem,
            "Approval event",
            new DateTimeOffset(2026, 6, 1, 8, 11, 0, TimeSpan.Zero),
            ContractLifecycleState.Associated,
            ContractAssociationThresholdBand.Auto,
            0.91,
            "01HZXASSOC000000000000001",
            "controlled-mailbox-001",
            "graph-message-001",
            "<internet-message-001@example.test>",
            "graph-conversation-001",
            "graph-thread-001",
            new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 7, 59, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 8, 0, 30, TimeSpan.Zero),
            "UTC",
            "mailbox:metadata",
            "microsoft-graph",
            "metadata_only",
            "collaboration_input",
            ProjectConversationItemView.CurrentSchemaVersion,
            12,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            SafeNextAction: "wait-for-projection",
            ApprovalId: "approval-001",
            ApprovalEventKind: ContractApprovalEventKind.Outcome,
            ApprovalStatus: ContractApprovalStatus.Approved,
            ApprovalCommandName: "SendExternalReply",
            ApprovalAuditOperationId: "audit-operation-001",
            ApprovalAuditStatus: "reconciling",
            ApprovalCommandOutcomeStatus: "accepted-projection-pending",
            ResponsibleOwnerRole: "operations",
            DuplicateSafetyState: "duplicate-safe");

    private static ProjectConversationItemView ProjectConversationAiContextPolicyCarrier()
        => ProjectConversationProjectionPendingItem() with
        {
            ItemId = "conversation:ai-context:policy",
            Kind = ContractProjectConversationItemKind.EmailDerived,
            ActorKind = ContractProjectConversationActorKind.Mailbox,
            ActorLabel = "Mailbox event",
            OccurredAt = new DateTimeOffset(2026, 6, 1, 8, 29, 0, TimeSpan.Zero),
            SourceVersion = 30,
            PolicySnapshotVersion = "policy-snapshot-ai-context-v1",
            EvidenceReferenceSummary = ["mailbox:evidence:001"],
            SafeNextAction = "none",
        };

    private static ProjectConversationItemView ProjectConversationTaskIntentSourceItem()
        => ProjectConversationProjectionPendingItem() with
        {
            ItemId = "01HZXMAILBOX000000000000021",
            Kind = ContractProjectConversationItemKind.EmailDerived,
            ActorKind = ContractProjectConversationActorKind.Mailbox,
            ActorLabel = "Mailbox intake",
            OccurredAt = new DateTimeOffset(2026, 6, 1, 8, 34, 0, TimeSpan.Zero),
            SourceVersion = 40,
            SafeNextAction = "review-association",
            EvidenceReferenceSummary = ["placeholder:evidence"],
        };

    private static ProjectConversationItemView ProjectConversationRedactedNonActionableSourceItem()
        => ProjectConversationTaskIntentSourceItem() with
        {
            ItemId = "01HZXMAILBOX000000000000022",
            SourceProviderMessageId = null,
            InternetMessageId = null,
            SourceThreadId = null,
            SourceProvenanceDisplayToken = null,
            RedactionState = "redacted",
            SourceVersion = 42,
            SafeNextAction = "none",
            EvidenceReferenceSummary = [],
        };

    private static ContractTaskIntentRecord ProjectConversationTaskIntentRecord()
        => new(
            "task-intent:api",
            "tenant-alpha",
            "project-alpha",
            "graph-message-001",
            "party-001",
            "authorized conversation item requests action",
            ContractProjectConversationDetectedActionKind.RequestAction,
            [
                new ContractTaskIntentSourceEvidenceOffset("message:offset:002", 40, 80, "safe-token-2"),
                new ContractTaskIntentSourceEvidenceOffset("message:offset:001", 10, 30, "safe-token-1"),
            ],
            "chatbot.task-intent.kernel.m0.v1",
            0.82,
            new DateTimeOffset(2026, 6, 1, 8, 34, 30, TimeSpan.Zero),
            ContractTaskIntentState.Captured,
            "chatbot.task-intent-record.v1",
            "task_intent_captured",
            "authorized-project-conversation",
            "metadata_only",
            "collaboration_input",
            41,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-001",
            ConversionReadinessBlocked: false,
            SafeNextAction: "review-task-intent-action");

    private sealed class NoOpAiExecutionCoordinator : IAiExecutionCoordinator
    {
        public ValueTask RecordStartedAsync(
            string tenantId,
            string conversationId,
            long sourceVersion,
            LowRiskAiAssistanceExecutionStarted started,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask RecordCancellationRequestedAsync(
            AiResponseGenerationCancellationRequested request,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask RecordTerminalObservedAsync(
            string tenantId,
            string stateOwnerAggregateId,
            string projectId,
            string responseId,
            string generationId,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask RecordCancellationFailedAsync(
            AiResponseGenerationCancellationFailed failure,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private static ProjectConversationItemView ProjectConversationAttachmentApiItem(
        ContractProjectConversationAttachmentStatus status,
        int index)
    {
        bool captured = status is ContractProjectConversationAttachmentStatus.Captured;
        string statusToken = status.ToString().ToLowerInvariant();
        return new ProjectConversationItemView(
            "tenant-alpha",
            "project-alpha",
            "Authorized Project",
            $"attachment:api:{index}",
            "intake-attachment-api",
            ContractProjectConversationItemKind.Attachment,
            ContractProjectConversationActorKind.MailboxAttachment,
            "Mailbox attachment",
            new DateTimeOffset(2026, 6, 1, 8, 30, index, TimeSpan.Zero),
            ContractLifecycleState.Associated,
            ContractAssociationThresholdBand.Auto,
            0.91,
            "01HZXASSOC000000000000001",
            "controlled-mailbox-001",
            null,
            null,
            "graph-conversation-001",
            "graph-thread-001",
            null,
            null,
            null,
            null,
            "Microsoft 365 mailbox",
            "m365-mailbox-intake",
            "metadata_only",
            "collaboration_input",
            ProjectConversationItemView.CurrentSchemaVersion,
            20 + index,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            SafeNextAction: status switch
            {
                ContractProjectConversationAttachmentStatus.Captured => "none",
                ContractProjectConversationAttachmentStatus.Retryable => "retry-scan",
                ContractProjectConversationAttachmentStatus.Unsafe => "quarantine-review",
                ContractProjectConversationAttachmentStatus.Rejected => "review-source-evidence",
                _ => "inspect-later",
            },
            SourceProviderAttachmentId: $"graph-attachment-api-{index}",
            AttachmentDisplayName: status is ContractProjectConversationAttachmentStatus.Unsafe ? null : $"attachment-{statusToken}.pdf",
            AttachmentContentType: status is ContractProjectConversationAttachmentStatus.Unsafe ? null : "application/pdf",
            AttachmentSizeInBytes: status is ContractProjectConversationAttachmentStatus.Unsafe ? null : 4096 + index,
            AttachmentCaptureStatus: ContractProjectConversationAttachmentStatus.Captured,
            AttachmentStorageStatus: captured ? ContractProjectConversationAttachmentStatus.Captured : ContractProjectConversationAttachmentStatus.Pending,
            AttachmentScanStatus: status,
            AttachmentFolderId: captured ? "folder-reference-api" : null,
            AttachmentFileId: captured ? "file-reference-api" : null,
            AttachmentDuplicateState: "unique",
            AttachmentRetryState: status is ContractProjectConversationAttachmentStatus.Retryable ? "retryable" : "not-retryable",
            AttachmentAiContextEligibility: captured ? "eligible" : "not-eligible",
            AttachmentAllowedActions: captured ? ["open-governed-file", "add-to-ai-context"] : [],
            AttachmentRedactionState: "metadata_only");
    }

    private static void AssertReplayMatchesStoredOutcome(string acceptedBody, string replayBody)
    {
        using JsonDocument accepted = JsonDocument.Parse(acceptedBody);
        using JsonDocument replay = JsonDocument.Parse(replayBody);
        JsonElement original = accepted.RootElement;
        JsonElement actual = replay.RootElement;
        JsonElement prior = actual.GetProperty("priorOutcome");
        foreach (string name in new[] { "commandId", "correlationId", "operationId", "taskId", "lifecycleState", "acceptedAt", "reasonCode", "retryEligible" })
        {
            bool present = original.TryGetProperty(name, out JsonElement expected);
            actual.TryGetProperty(name, out JsonElement current).ShouldBe(present);
            prior.TryGetProperty(name, out JsonElement stored).ShouldBe(present);
            if (present)
            {
                current.ToString().ShouldBe(expected.ToString());
                stored.ToString().ShouldBe(expected.ToString());
            }
        }
    }

    private static HttpRequestMessage CommandSubmissionRequest(string tenantId, string resourceName)
    {
        string payload =
            $$"""
            {
              "commandId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "commandType": "TenantScopedAction",
              "command": {
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

    private static async Task<DomainServiceWireResult> ReadDomainServiceResultAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content
            .ReadFromJsonAsync<DomainServiceWireResult>(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ShouldNotBeNull();
    }

    private static string AdmissionReason(DomainServiceWireResult result)
    {
        DomainServiceWireEvent rejection = result.Events.ShouldHaveSingleItem();
        using JsonDocument document = JsonDocument.Parse(rejection.Payload);
        return document.RootElement.GetProperty("ReasonCode").GetString().ShouldNotBeNull();
    }

    private static DomainServiceRequest DomainServiceRequest(
        string commandType,
        object? payload = null,
        string tenantId = "tenant-alpha",
        string aggregateId = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
        string userId = "actor-alpha",
        byte[]? payloadBytes = null)
    {
        byte[] commandPayload = payloadBytes ?? JsonSerializer.SerializeToUtf8Bytes(
            payload ?? new { noteId = "01ARZ3NDEKTSV4RRFFQ69G5FAV" });
        return new DomainServiceRequest(
            new CommandEnvelope(
                "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                tenantId,
                ChatBotEventStore.DomainName,
                aggregateId,
                commandType,
                commandPayload,
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                null,
                userId,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["surfaceOrigin"] = "api",
                    ["taskId"] = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                }),
            CurrentState: null);
    }

    private static HttpRequestMessage RecordGovernedNoteRequest(string noteId, string? origin = null, string? surfaceOriginHeader = null)
    {
        string originLine = origin is null ? string.Empty : $"\n  \"origin\": \"{origin}\",";
        string payload =
            $$"""
            {
              "commandId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "commandType": "RecordGovernedNote",
              "command": {
                "noteId": "{{noteId}}"
              },{{originLine}}
              "requestSchemaVersion": "v1"
            }
            """;

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        if (surfaceOriginHeader is not null)
        {
            request.Headers.Add("X-Hexalith-Surface-Origin", surfaceOriginHeader);
        }

        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        return request;
    }

    private static HttpRequestMessage LowRiskAiAssistanceRequest()
    {
        string payload =
            """
            {
              "commandId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "commandType": "ExecuteLowRiskAIAssistance",
              "command": {
                "projectId": "project-001",
                "proposalId": "proposal-001",
                "taskIntentId": "task-intent-001",
                "sourceMessageId": "graph-message-001",
                "requesterId": "actor-alpha",
                "assistanceKind": "summarize-visible-context",
                "contextPackageId": "context-package-001",
                "contextPackageVersion": "v1",
                "contextPackageRedactionState": "metadata_only",
                "retentionClass": "collaboration_input",
                "providerReuseSetting": "disabled",
                "sourceEvidenceReferences": ["evidence-message-001"],
                "authorizedContextReferences": ["evidence-message-001"],
                "excludedContextReasons": ["redacted", "policy-denied"],
                "expectedProposalSourceVersion": 8,
                "policySnapshotId": "policy-snap-001",
                "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                "executionId": "ai-execution-001",
                "transitionId": "transition-001"
              },
              "origin": "api",
              "requestSchemaVersion": "v1"
            }
            """;

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        return request;
    }

    private static HttpRequestMessage MailboxIntakeRequest(
        string commandId,
        string taskId,
        string intakeId,
        bool includeAuthenticity = false)
    {
        string authenticity = includeAuthenticity
            ? """
                ,
                "authenticity": {
                  "authenticationResults": {
                    "spf": "fail",
                    "dkim": "temperror",
                    "dmarc": "not-supplied",
                    "compositeAuthentication": "unknown",
                    "compositeAuthenticationReason": "109",
                    "authenticationResultsHeaders": [
                      {
                        "name": "Authentication-Results",
                        "ordinal": 0,
                        "valueState": "supplied"
                      },
                      {
                        "name": "Authentication-Results",
                        "ordinal": 1,
                        "valueState": "malformed"
                      }
                    ]
                  },
                  "headerInspection": {
                    "receivedHeaders": [
                      {
                        "name": "Received",
                        "ordinal": 0,
                        "valueState": "supplied"
                      }
                    ],
                    "authenticationResultsHeaders": [
                      {
                        "name": "Authentication-Results",
                        "ordinal": 0,
                        "valueState": "supplied"
                      },
                      {
                        "name": "Authentication-Results",
                        "ordinal": 1,
                        "valueState": "malformed"
                      }
                    ],
                    "from": "supplied",
                    "replyTo": "supplied",
                    "sender": "not-supplied",
                    "xOriginalSender": "not-supplied",
                    "discrepancies": [
                      "multiple-authentication-results",
                      "from-reply-to-mismatch"
                    ]
                  }
                }
                """
            : string.Empty;
        string payload =
            $$"""
            {
              "commandId": "{{commandId}}",
              "commandType": "CaptureMailboxMessageIntake",
              "command": {
                "intakeId": "{{intakeId}}",
                "source": {
                  "providerMessageId": "graph-message-sensitive",
                  "internetMessageId": "<message@example.test>",
                  "conversationId": "graph-conversation-sensitive",
                  "threadId": "graph-thread-sensitive",
                  "mailboxId": "controlled-mailbox-001",
                  "sender": {
                    "address": "sender@example.test",
                    "displayName": "Secret Sender"
                  },
                  "receivedAt": "2026-05-31T09:00:00Z",
                  "sentAt": "2026-05-31T08:59:00Z",
                  "createdAt": "2026-05-31T08:58:00Z",
                  "sourceTimezone": "UTC",
                  "sourceContext": "raw provider payload",
                  "sourceSchemaVersion": 1
                },
                "recipients": [
                  {
                    "address": "recipient@example.test",
                    "displayName": "Secret Project",
                    "kind": "to"
                  }
                ],
                "attachments": []{{authenticity}}
              },
              "origin": "mailbox",
              "requestSchemaVersion": "v1"
            }
            """;

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", taskId);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        return request;
    }

    /// <summary>
    /// The contract-valid mailbox admission probe body used by the live Tier-3 lane: every published member is present
    /// with its declared type, while the intake identity is deliberately not a ULID so an admitted request fails in
    /// dispatch planning before any external write.
    /// </summary>
    private static string MailboxAdmissionProbeBody(string commandId)
        => $$"""
            {"commandId":"{{commandId}}","commandType":"CaptureMailboxMessageIntake","command":{"intakeId":"not-a-ulid","source":{"providerMessageId":"recovery-admission-probe-{{commandId}}","internetMessageId":"<recovery-admission-probe-{{commandId}}@example.test>","conversationId":"recovery-admission-probe-{{commandId}}","threadId":null,"mailboxId":"recovery-admission-probe","sender":{"address":"probe@example.test","displayName":null},"receivedAt":"2026-08-01T00:00:00Z","sentAt":null,"createdAt":null,"sourceTimezone":null,"sourceContext":"mailbox:metadata","sourceSchemaVersion":1},"recipients":[],"attachments":[]},"origin":"mailbox","requestSchemaVersion":"v1"}
            """;

    /// <summary>The former live probe body: its source identity misses required members, so it never passes the contract.</summary>
    private static string IncompleteMailboxAdmissionProbeBody(string commandId)
        => $$"""
            {"commandId":"{{commandId}}","commandType":"CaptureMailboxMessageIntake","command":{"intakeId":"not-a-ulid","source":{"providerMessageId":"probe","mailboxId":"probe","receivedAtUtc":"2026-08-01T00:00:00Z"},"recipients":[],"attachments":[]},"origin":"mailbox","requestSchemaVersion":"v1"}
            """;

    private static HttpRequestMessage MailboxAdmissionProbeRequest(string body)
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-real-token");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        return request;
    }

    private static HttpRequestMessage RetryFailedWorkflowRequest(
        string commandId,
        string taskId,
        string retryId,
        string rationale)
    {
        string payload =
            $$"""
            {
              "commandId": "{{commandId}}",
              "commandType": "RequestFailedWorkflowRetry",
              "command": {
                "retryId": "{{retryId}}",
                "failedEventId": "01ARZ3NDEKTSV4RRFFQ69G5FAB",
                "failedOperationClass": "message-intake",
                "failureReasonCode": "graph_throttled",
                "expectedFailedSourceVersion": 7,
                "rationale": "{{rationale}}"
              },
              "origin": "ui",
              "requestSchemaVersion": "v1"
            }
            """;

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", taskId);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        return request;
    }

    private static HttpRequestMessage AssociationScoringRequest(double signalWeight)
    {
        string payload =
            $$"""
            {
              "commandId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "commandType": "ScoreMailboxMessageAssociation",
              "command": {
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
                    "weight": {{signalWeight.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
                    "requiredForAutoAssociation": true
                  }
                ],
                "thresholdPolicy": null,
                "candidates": [],
                "exclusions": [],
                "result": null,
                "scoringKernelVersion": ""
              },
              "origin": "api",
              "requestSchemaVersion": "v1"
            }
            """;

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        return request;
    }

    private static Hexalith.ChatBot.Contracts.Commands.AssociationDeterministicSignal AssociationSignal(double weight)
        => new(
            ContractAssociationSignalClass.ExplicitProjectIdentifier,
            "project-001",
            "mailbox:project-id",
            "hash-project",
            weight,
            RequiredForAutoAssociation: true);

    private static HttpRequestMessage ProjectConversationRequest(string projectId)
    {
        HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{projectId}/conversation");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        return request;
    }

    private static AssociationCandidateView AssociationRoutingViewWithEvidence()
    {
        ContractAssociationEvidenceReference evidence = new(
            "mailbox:project-id",
            "evidence-sha256-project",
            "project-identifier");
        ContractAssociationCandidate candidate = new(
            "project-001",
            null,
            0.91,
            1,
            [ContractAssociationReasonCode.ExplicitProjectIdentifierMatched],
            [evidence],
            [
                new ContractAssociationConfidenceInput(
                    ContractAssociationSignalClass.ExplicitProjectIdentifier,
                    ContractAssociationReasonCode.ExplicitProjectIdentifierMatched,
                    0.42,
                    evidence.EvidenceReference,
                    evidence.EvidenceFingerprint),
            ],
            false);

        return new AssociationCandidateView(
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "controlled-mailbox-001",
            "conversation-001",
            "thread-001",
            "project-001",
            null,
            ContractLifecycleState.Associated,
            ContractAssociationScoringOutcome.AutoAssociated,
            ContractAssociationThresholdBand.Auto,
            0.91,
            [candidate],
            [],
            "association-thresholds.m0.default.v1",
            AssociationCandidateView.CurrentSchemaVersion,
            AssociationCandidateView.MailboxSourceProvenance,
            "association-deterministic.kernel.m0.v1",
            "metadata_only",
            "collaboration_input",
            7,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 8, 1, 0, TimeSpan.Zero),
            ContractAssociationDecisionKind.Associate,
            "actor-safe",
            "human",
            new DateTimeOffset(2026, 6, 1, 8, 2, 0, TimeSpan.Zero),
            DecisionNote: "raw-body decision note must not leave projection",
            DecisionNoteRedactionState: "redacted",
            SurfaceOrigin: "ui",
            PolicySnapshotVersion: "association-thresholds.m0.default.v1",
            SafeNextAction: "none");
    }

    private static HttpRequestMessage OperationStatusRequest(string operationId, string? tenantId = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/operations/{operationId}");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        if (tenantId is not null)
        {
            request.Headers.Add("X-Test-Tenant", tenantId);
        }

        return request;
    }

    private static HttpRequestMessage AssociationRoutingStatusRequest(string associationId)
    {
        HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/associations/{associationId}/routing-status");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        return request;
    }

    private static HttpRequestMessage AuditHistoryRequest(string operationId, string? tenantId = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/operations/{operationId}/audit-history");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        if (tenantId is not null)
        {
            request.Headers.Add("X-Test-Tenant", tenantId);
        }

        return request;
    }

    private sealed class AllowAllSpineCommandAllowlist : ISpineCommandAllowlist
    {
        public bool IsAllowed(string? commandType) => true;
    }

    private sealed class DenyAllSpineCommandAllowlist : ISpineCommandAllowlist
    {
        public bool IsAllowed(string? commandType) => false;
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

    private sealed class RoutingEventStoreGatewayClient : IEventStoreGatewayClient
    {
        private readonly List<IEventPayload> _events = [];
        private readonly List<SubmitCommandRequest> _submitted = [];

        public IReadOnlyList<IEventPayload> Events => _events;

        public IReadOnlyList<SubmitCommandRequest> Submitted => _submitted;

        public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            _submitted.Add(request);

            ContractScoreMailboxMessageAssociation command = request.Payload.Deserialize<ContractScoreMailboxMessageAssociation>()
                ?? throw new InvalidOperationException("Association command payload could not be deserialized.");
            CommandEnvelope envelope = new(
                request.MessageId,
                request.Tenant,
                request.Domain,
                request.AggregateId,
                request.CommandType,
                [],
                request.CorrelationId ?? request.MessageId,
                null,
                "actor-alpha",
                request.Extensions is null ? null : new Dictionary<string, string>(request.Extensions, StringComparer.Ordinal));
            DomainResult result = GovernedOperationAggregate.Handle(command, null, envelope);

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException("Association routing unexpectedly rejected the scored command.");
            }

            _events.AddRange(result.Events);
            return Task.FromResult(new SubmitCommandResponse(request.CorrelationId ?? request.MessageId));
        }

        public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    // Stand-in for the EventStore gateway: accepts every command submission (no real EventStore in-process).
    private sealed class AcceptingEventStoreGatewayClient : IEventStoreGatewayClient
    {
        public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Task.FromResult(new SubmitCommandResponse(request.CorrelationId ?? request.MessageId));
        }

        public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class RoundTrippingEventStoreGatewayClient(IServiceProvider serviceProvider) : IEventStoreGatewayClient
    {
        public async Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            CommandEnvelope envelope = new(
                request.MessageId,
                request.Tenant,
                request.Domain,
                request.AggregateId,
                request.CommandType,
                JsonSerializer.SerializeToUtf8Bytes(request.Payload),
                request.CorrelationId ?? request.MessageId,
                CausationId: null,
                UserId: "actor-alpha",
                request.Extensions is null ? null : new Dictionary<string, string>(request.Extensions, StringComparer.Ordinal));

            using IServiceScope scope = serviceProvider.CreateScope();
            DomainServiceWireResult result = await DomainServiceRequestRouter
                .ProcessAsync(scope.ServiceProvider, new DomainServiceRequest(envelope, CurrentState: null), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsRejection)
            {
                throw new InvalidOperationException("The in-process EventStore round trip returned a domain rejection.");
            }

            return new SubmitCommandResponse(request.CorrelationId ?? request.MessageId);
        }

        public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingEventStoreGatewayClient : IEventStoreGatewayClient
    {
        private readonly List<SubmitCommandRequest> _submitted = [];

        public IReadOnlyList<SubmitCommandRequest> Submitted => _submitted;

        public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
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

    private sealed class FixedTenantAiPolicySnapshotProvider(bool lowRiskAllowed) : ITenantAiPolicySnapshotProvider
    {
        public ValueTask<TenantAiPolicySnapshot?> TryGetAsync(
            string tenantId,
            string projectId,
            string? requestedPolicySnapshotId,
            CancellationToken cancellationToken)
            => ValueTask.FromResult<TenantAiPolicySnapshot?>(new TenantAiPolicySnapshot(
                requestedPolicySnapshotId ?? "policy-snap-001",
                lowRiskAllowed,
                "read-only",
                ["summarize-visible-context"],
                IsFresh: true,
                IsValid: true));
    }

    private sealed class RecordingAiAssistanceProvider(string outcome) : IAiAssistanceProvider
    {
        public int ExecuteCount { get; private set; }

        public AiAssistanceProviderRequest? LastRequest { get; private set; }

        public ValueTask<Hexalith.ChatBot.Contracts.Queries.LowRiskAiAssistanceExecutionRecord> ExecuteAsync(
            AiAssistanceProviderRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            ExecuteCount++;
            LastRequest = request;
            bool success = string.Equals(outcome, "success", StringComparison.Ordinal);
            return ValueTask.FromResult(new Hexalith.ChatBot.Contracts.Queries.LowRiskAiAssistanceExecutionRecord(
                request.ExecutionId,
                request.ProposalId,
                request.AssistanceKind,
                outcome,
                "deterministic-test",
                "test-model-v1",
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
                "metadata_only",
                success ? "none" : "review-ai-action",
                FailureCode: success ? null : "ai_provider_disabled",
                Retryability: success ? null : "retryable"));
        }
    }

    private sealed class TestPrincipalStartupFilter(string tenantId) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(
                    async (context, continuation) =>
                    {
                        string effectiveTenantId = context.Request.Headers.TryGetValue("X-Test-Tenant", out Microsoft.Extensions.Primitives.StringValues values) &&
                            values.Count == 1
                                ? values[0]!
                                : tenantId;
                        Claim[] claims =
                        [
                            new("sub", "actor-alpha"),
                            new("eventstore:tenant", effectiveTenantId),
                            new("requester_authority_class", "project-contributor"),
                            new(ParticipantAuthorizationStage.ProjectOwnerClaim, "project-001"),
                        ];
                        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
                        await continuation().ConfigureAwait(false);
                    });
                next(app);
            };
    }

    private sealed class ProjectConversationPrincipalStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(
                    async (context, continuation) =>
                    {
                        context.User = new ClaimsPrincipal(new ClaimsIdentity(
                            [
                                new Claim("sub", "actor-alpha"),
                                new Claim("eventstore:tenant", "tenant-alpha"),
                                new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, "project-alpha"),
                            ],
                            "test"));
                        await continuation().ConfigureAwait(false);
                    });
                next(app);
            };
    }

    private sealed class UnavailableAuditWriter : IAuditWriter
    {
        private readonly List<AuditEnvelope> _envelopes = [];

        public IReadOnlyList<AuditEnvelope> Envelopes => _envelopes;

        public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            _envelopes.Add(envelope);
            return ValueTask.FromResult(AuditWriteResult.Unavailable());
        }

        public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            _envelopes.Add(envelope);
            return ValueTask.FromResult(AuditWriteResult.Success);
        }
    }

    private sealed class FixedLifecycleTransitionGuard(LifecycleTransitionValidation result) : ILifecycleTransitionGuard
    {
        public LifecycleTransitionValidation ValidateCommandSubmission(ChatBotGatewayContext context)
            => result;

        public LifecycleTransitionValidation ResolveSkipTransition(LifecycleSkipTrigger trigger)
            => LifecycleTransitionValidation.Valid(new LifecycleTransitionDefinition("Received", "Skipped"));
    }

    private sealed class RecordingDispatcher : ICommandDispatcher
    {
        public int DispatchCount { get; private set; }

        public ValueTask<ChatBotDispatchResult> DispatchAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            DispatchCount++;
            return ValueTask.FromResult(new ChatBotDispatchResult(DateTimeOffset.UtcNow));
        }
    }

    private sealed class ConflictIdempotencyStore : IIdempotencyStore
    {
        public ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            CoarseIdempotencyMetadata metadata = CoarseIdempotencyMetadata.UnsafeCreateForTesting(
                "command-execution",
                "conflict-key",
                "conflict-equivalence",
                DateTimeOffset.UtcNow.AddSeconds(60));
            context.SetIdempotency(metadata);
            return ValueTask.FromResult(CoarseIdempotencyDecision.Conflict(metadata));
        }

        public ValueTask RecordOutcomeAsync(
            CoarseIdempotencyMetadata metadata,
            CommandSubmissionResponse outcome,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
