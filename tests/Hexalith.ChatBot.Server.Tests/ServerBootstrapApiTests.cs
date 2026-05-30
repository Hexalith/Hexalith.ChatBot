using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

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
    public async Task HealthEndpointShouldRejectUnsupportedMethods()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsync("/health", null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
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
                        configureServices?.Invoke(services);
                    }));

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
                        Claim[] claims = [new("sub", "actor-alpha"), new("eventstore:tenant", effectiveTenantId)];
                        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
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
