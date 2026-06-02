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
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Streams;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task HealthEndpointShouldReturnHealthyStatus()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/health", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        body.ShouldContain("Healthy");
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
        body.ShouldContain("Alive");
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

    [Fact]
    public async Task CommandEndpointShouldAcceptAuthenticatedTenantBoundSubmission()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "allowed-resource"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
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
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        string correlationId = accepted.RootElement.GetProperty("correlationId").GetString().ShouldNotBeNull();
        correlationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe(correlationId);
        response.Headers.Contains("X-Hexalith-Task-Id").ShouldBeFalse();
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
        dispatcher.DispatchCount.ShouldBe(1);
        string firstBody = await first.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string secondBody = await second.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        secondBody.ShouldBe(firstBody);
        secondBody.ShouldNotContain("allowed-resource", Case.Insensitive);
        secondBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
    }

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
        root.GetProperty("taskId").ValueKind.ShouldBe(JsonValueKind.Null);
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
        root.GetProperty("terminalReason").ValueKind.ShouldBe(JsonValueKind.Null);
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
        duplicateBody.ShouldBe(firstBody);

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
        replayBody.ShouldBe(firstBody);

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
    public async Task HealthEndpointShouldRejectUnsupportedMethods()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsync("/health", null, TestContext.Current.CancellationToken)
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
    public async Task CommandEndpointShouldExecuteAllowedLowRiskAiAssistanceOnceAndReplayDuplicate()
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
        replayBody.ShouldBe(firstBody);

        provider.ExecuteCount.ShouldBe(1);
        provider.LastRequest.ShouldNotBeNull();
        provider.LastRequest.TenantId.ShouldBe("tenant-alpha");
        provider.LastRequest.PolicySnapshotId.ShouldBe("policy-snap-001");
        provider.LastRequest.PolicyReasonCode.ShouldBe("low-risk-execute-allowed");
        provider.LastRequest.AuthorizedContextReferences.ShouldBe(["evidence-message-001"]);
        provider.LastRequest.ExcludedContextReasons.ShouldBe(["redacted", "policy-denied"]);

        SubmitCommandRequest submitted = eventStore.Submitted.ShouldHaveSingleItem();
        submitted.CommandType.ShouldBe("ExecuteLowRiskAIAssistance");
        submitted.AggregateId.ShouldBe("graph-message-001");
        submitted.Payload.GetProperty("ExecutionRecord").GetProperty("Outcome").GetString().ShouldBe("success");
        submitted.Payload.GetProperty("ExecutionRecord").GetProperty("SafeNextAction").GetString().ShouldBe("none");
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
    public async Task CommandEndpointShouldCollapseUnknownSurfaceOriginToTheSafeApiDefault()
    {
        // AC2: an unknown/unattributed origin is never trusted as an arbitrary value — it collapses to api.
        InMemoryAuditWriter auditWriter = await SubmitGovernedNoteCapturingAudit(bodyOrigin: "totally-unknown-surface").ConfigureAwait(true);

        auditWriter.Envelopes.ShouldNotBeEmpty();
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SurfaceOrigin == "api");
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

    private static HttpRequestMessage CommandSubmissionRequest(string tenantId, string resourceName)
    {
        string payload =
            $$"""
            {
              "commandId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "commandType": "TenantScopedCommand",
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
