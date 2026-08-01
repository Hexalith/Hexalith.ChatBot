using System.Net;
using System.Text;

using Hexalith.ChatBot.Client.Generated;

using Newtonsoft.Json;

using Shouldly;

using ContractMailboxDelegatedSenderState = Hexalith.ChatBot.Contracts.Enums.MailboxDelegatedSenderState;
using GeneratedClient = Hexalith.ChatBot.Client.Generated.Client;

namespace Hexalith.ChatBot.Client.Tests;

public sealed class CommandSubmissionTransportTests
{
    private const string CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TaskId = "01ARZ3NDEKTSV4RRFFQ69G5FAX";

    [Fact]
    public async Task SubmitCommandShouldPostTheTypedCommandRequestAndParseAcceptedResponse()
    {
        const string responseBody =
            """
            {
              "commandId": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW",
              "taskId": "01ARZ3NDEKTSV4RRFFQ69G5FAX",
              "lifecycleState": "Proposed",
              "acceptedAt": "2026-06-10T00:00:00+00:00"
            }
            """;
        CapturingHandler handler = new(HttpStatusCode.Accepted, responseBody);
        GeneratedClient client = NewClient(handler);

        CommandSubmissionResponse response = await client.SubmitCommandAsync(
            CorrelationId,
            TaskId,
            Request(),
            TestContext.Current.CancellationToken);

        handler.LastMethod.ShouldBe(HttpMethod.Post);
        handler.LastPath.ShouldBe("/api/v1/commands");
        handler.LastCorrelationId.ShouldBe(CorrelationId);
        handler.LastTaskId.ShouldBe(TaskId);
        handler.LastRequestBody.ShouldContain("\"commandId\":\"01ARZ3NDEKTSV4RRFFQ69G5FAV\"");
        handler.LastRequestBody.ShouldContain("\"commandType\":\"CaptureTaskIntent\"");
        handler.LastRequestBody.ShouldContain("\"requestSchemaVersion\":\"v1\"");
        handler.LastRequestBody.ShouldContain("\"origin\":\"ui\"");
        handler.LastRequestBody.ShouldNotContain("tenantId", Case.Insensitive);
        handler.LastRequestBody.ShouldNotContain("EventStore", Case.Insensitive);
        handler.LastRequestBody.ShouldNotContain("Dapr", Case.Insensitive);

        response.CommandId.ShouldBe(CommandId);
        response.CorrelationId.ShouldBe(CorrelationId);
        response.TaskId.ShouldBe(TaskId);
        response.LifecycleState.ShouldBe(LifecycleState.Proposed);
    }

    [Fact]
    public async Task SubmitCommandShouldSerializeContractEnumsByTheirEnumMemberWireValues()
    {
        CapturingHandler handler = new(HttpStatusCode.Accepted,
            """
            {"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAV","correlationId":"01ARZ3NDEKTSV4RRFFQ69G5FAW","lifecycleState":"Proposed","acceptedAt":"2026-06-10T00:00:00Z"}
            """);
        GeneratedClient client = NewClient(handler);
        CommandSubmissionRequest request = Request();
        request.Command = new { delegatedSenderState = ContractMailboxDelegatedSenderState.NotDelegated };

        _ = await client.SubmitCommandAsync(
            CorrelationId,
            TaskId,
            request,
            TestContext.Current.CancellationToken);

        handler.LastRequestBody.ShouldContain("\"delegatedSenderState\":\"not-delegated\"");
        handler.LastRequestBody.ShouldNotContain("\"delegatedSenderState\":0");
    }

    [Fact]
    public async Task SubmitCommandShouldRejectNumericEnumsDuringResponseDeserialization()
    {
        CapturingHandler handler = new(
            HttpStatusCode.Accepted,
            """
            {"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAV","correlationId":"01ARZ3NDEKTSV4RRFFQ69G5FAW","lifecycleState":0,"acceptedAt":"2026-06-10T00:00:00Z"}
            """);
        GeneratedClient client = NewClient(handler);

        HexalithChatBotApiException exception = await Should.ThrowAsync<HexalithChatBotApiException>(
            () => client.SubmitCommandAsync(
                CorrelationId,
                TaskId,
                Request(),
                TestContext.Current.CancellationToken));

        exception.InnerException.ShouldBeOfType<JsonSerializationException>();
    }

    [Fact]
    public async Task GetOperationStatusShouldRejectNumericEnumsInsideEnumCollections()
    {
        // NSwag emits ItemConverterType = StringEnumConverter (AllowIntegerValues = true) on every
        // collection-of-enum property, and Newtonsoft prefers containerProperty.ItemConverter over the settings
        // converter list. Overriding only the scalar property converter left every enum ARRAY accepting ordinals.
        CapturingHandler handler = new(
            HttpStatusCode.OK,
            """
            {"operationId":"01ARZ3NDEKTSV4RRFFQ69G5FAV","commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAV","correlationId":"01ARZ3NDEKTSV4RRFFQ69G5FAW","safeNextActions":[1]}
            """);
        GeneratedClient client = NewClient(handler);

        HexalithChatBotApiException exception = await Should.ThrowAsync<HexalithChatBotApiException>(
            () => client.GetOperationStatusAsync(
                CommandId,
                CorrelationId,
                TaskId,
                TestContext.Current.CancellationToken));

        exception.InnerException.ShouldBeOfType<JsonSerializationException>();
    }

    [Fact]
    public async Task GetOperationStatusShouldAcceptNamedEnumWireValuesInsideEnumCollections()
    {
        CapturingHandler handler = new(
            HttpStatusCode.OK,
            """
            {"operationId":"01ARZ3NDEKTSV4RRFFQ69G5FAV","commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAV","correlationId":"01ARZ3NDEKTSV4RRFFQ69G5FAW","safeNextActions":["retry-later","escalate"]}
            """);
        GeneratedClient client = NewClient(handler);

        OperationStatus status = await client.GetOperationStatusAsync(
            CommandId,
            CorrelationId,
            TaskId,
            TestContext.Current.CancellationToken);

        status.SafeNextActions.ShouldBe([ChatBotMessageNextAction.RetryLater, ChatBotMessageNextAction.Escalate]);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, 400, ProblemDetailsCategory.Validation_error, ProblemDetailsClientAction.CorrectRequest)]
    [InlineData(HttpStatusCode.Unauthorized, 401, ProblemDetailsCategory.Authentication_failure, ProblemDetailsClientAction.Authenticate)]
    [InlineData(HttpStatusCode.Forbidden, 403, ProblemDetailsCategory.Authorization_denied, ProblemDetailsClientAction.RequestAccess)]
    [InlineData(HttpStatusCode.Conflict, 409, ProblemDetailsCategory.Conflict, ProblemDetailsClientAction.None)]
    [InlineData(HttpStatusCode.InternalServerError, 500, ProblemDetailsCategory.Internal_error, ProblemDetailsClientAction.RetryLater)]
    [InlineData(HttpStatusCode.ServiceUnavailable, 503, ProblemDetailsCategory.Internal_error, ProblemDetailsClientAction.RetryLater)]
    public async Task SubmitCommandShouldParseDeclaredProblemResponsesAsMetadataOnlyTypedExceptions(
        HttpStatusCode httpStatus,
        int expectedStatus,
        ProblemDetailsCategory expectedCategory,
        ProblemDetailsClientAction expectedClientAction)
    {
        GeneratedClient client = NewClient(new CapturingHandler(httpStatus, ProblemBody(expectedStatus, expectedCategory, expectedClientAction)));

        HexalithChatBotApiException<ProblemDetails> exception = await Should.ThrowAsync<HexalithChatBotApiException<ProblemDetails>>(
            () => client.SubmitCommandAsync(CorrelationId, TaskId, Request(), TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(expectedStatus);
        exception.Result.Status.ShouldBe(expectedStatus);
        exception.Result.Category.ShouldBe(expectedCategory);
        exception.Result.CorrelationId.ShouldBe(CorrelationId);
        exception.Result.TaskId.ShouldBe(TaskId);
        exception.Result.ClientAction.ShouldBe(expectedClientAction);
        exception.Result.Details.Visibility.ShouldBe(ProblemDetailsDetailsVisibility.Metadata_only);
        string response = exception.Response.ShouldNotBeNull();
        response.ShouldNotContain("tenant-alpha", Case.Insensitive);
        response.ShouldNotContain("payload-sentinel", Case.Insensitive);
        response.ShouldNotContain("/tmp/", Case.Insensitive);
        response.ShouldNotContain("secret", Case.Insensitive);
    }

    private static CommandSubmissionRequest Request()
        => new()
        {
            CommandId = CommandId,
            CommandType = "CaptureTaskIntent",
            Command = new { taskIntentId = "task-intent-alpha" },
            RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            Origin = SurfaceOrigin.Ui,
        };

    private static string ProblemBody(
        int status,
        ProblemDetailsCategory category,
        ProblemDetailsClientAction clientAction)
        => $$"""
            {
              "type": "https://problems.hexalith.local/chatbot/{{status}}",
              "title": "Synthetic metadata-only problem",
              "status": {{status}},
              "category": "{{WireValue(category)}}",
              "code": "synthetic_metadata_only_problem",
              "message": "Synthetic safe failure message.",
              "correlationId": "{{CorrelationId}}",
              "taskId": "{{TaskId}}",
              "retryable": false,
              "clientAction": "{{WireValue(clientAction)}}",
              "details": {
                "visibility": "metadata_only"
              }
            }
            """;

    private static string WireValue<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => value switch
        {
            ProblemDetailsCategory.Authentication_failure => "authentication_failure",
            ProblemDetailsCategory.Authorization_denied => "authorization_denied",
            ProblemDetailsCategory.Validation_error => "validation_error",
            ProblemDetailsCategory.Conflict => "conflict",
            ProblemDetailsCategory.Internal_error => "internal_error",
            ProblemDetailsClientAction.Authenticate => "authenticate",
            ProblemDetailsClientAction.RetryLater => "retry-later",
            ProblemDetailsClientAction.RequestAccess => "request-access",
            ProblemDetailsClientAction.Escalate => "escalate",
            ProblemDetailsClientAction.Dismiss => "dismiss",
            ProblemDetailsClientAction.CorrectRequest => "correct-request",
            ProblemDetailsClientAction.None => "none",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unexpected enum value."),
        };

    private static GeneratedClient NewClient(CapturingHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

    private sealed class CapturingHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
    {
        public HttpMethod? LastMethod { get; private set; }

        public string? LastPath { get; private set; }

        public string? LastCorrelationId { get; private set; }

        public string? LastTaskId { get; private set; }

        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastMethod = request.Method;
            LastPath = request.RequestUri?.AbsolutePath;
            LastCorrelationId = request.Headers.TryGetValues("X-Correlation-Id", out IEnumerable<string>? correlationValues)
                ? correlationValues.SingleOrDefault()
                : null;
            LastTaskId = request.Headers.TryGetValues("X-Hexalith-Task-Id", out IEnumerable<string>? taskValues)
                ? taskValues.SingleOrDefault()
                : null;

            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }
}
