using System.Net;
using System.Text;

using Hexalith.ChatBot.Client.Generated;

using Shouldly;

using GeneratedClient = Hexalith.ChatBot.Client.Generated.Client;

namespace Hexalith.ChatBot.Client.Tests;

public sealed class AssociationRoutingStatusTransportTests
{
    private const string AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TaskId = "01ARZ3NDEKTSV4RRFFQ69G5FAX";

    [Fact]
    public async Task GetAssociationRoutingStatusShouldReadMetadataOnlyStatusWithCorrelationHeaders()
    {
        CapturingHandler handler = new(HttpStatusCode.OK, SuccessBody());
        GeneratedClient client = NewClient(handler);

        AssociationRoutingStatus status = await client.GetAssociationRoutingStatusAsync(
            AssociationId,
            CorrelationId,
            TaskId,
            TestContext.Current.CancellationToken);

        handler.LastMethod.ShouldBe(HttpMethod.Get);
        handler.LastPath.ShouldBe($"/api/v1/associations/{AssociationId}/routing-status");
        handler.LastCorrelationId.ShouldBe(CorrelationId);
        handler.LastTaskId.ShouldBe(TaskId);
        status.AssociationId.ShouldBe(AssociationId);
        status.LifecycleState.ShouldBe(LifecycleState.NeedsReview);
        status.Outcome.ShouldBe(AssociationScoringOutcome.CandidatesGenerated);
        status.ThresholdBand.ShouldBe(AssociationThresholdBand.Ambiguous);
        status.Candidates.ShouldHaveSingleItem().ProjectId.ShouldBe("project-001");
        status.EvidenceRefs.ShouldHaveSingleItem().RedactionState.ShouldBe(AssociationEvidenceReferenceRedactionState.Metadata_only);
        status.DisabledActionReasonCodes.ShouldContain("projection-pending");
        status.NextActionReasonCodes.ShouldContain(ChatBotMessageCode.Association_ambiguous_routed);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, 401, ProblemDetailsCategory.Authentication_failure, ProblemDetailsClientAction.Authenticate)]
    [InlineData(HttpStatusCode.Forbidden, 403, ProblemDetailsCategory.Authorization_denied, ProblemDetailsClientAction.RequestAccess)]
    [InlineData(HttpStatusCode.InternalServerError, 500, ProblemDetailsCategory.Internal_error, ProblemDetailsClientAction.RetryLater)]
    public async Task GetAssociationRoutingStatusShouldParseDeclaredProblemResponsesAsMetadataOnlyTypedExceptions(
        HttpStatusCode httpStatus,
        int expectedStatus,
        ProblemDetailsCategory expectedCategory,
        ProblemDetailsClientAction expectedClientAction)
    {
        GeneratedClient client = NewClient(new CapturingHandler(httpStatus, ProblemBody(expectedStatus, expectedCategory, expectedClientAction)));

        HexalithChatBotApiException<ProblemDetails> exception = await Should.ThrowAsync<HexalithChatBotApiException<ProblemDetails>>(
            () => client.GetAssociationRoutingStatusAsync(AssociationId, CorrelationId, TaskId, TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(expectedStatus);
        exception.Result.Status.ShouldBe(expectedStatus);
        exception.Result.Category.ShouldBe(expectedCategory);
        exception.Result.CorrelationId.ShouldBe(CorrelationId);
        exception.Result.TaskId.ShouldBe(TaskId);
        exception.Result.ClientAction.ShouldBe(expectedClientAction);
        exception.Result.Details.Visibility.ShouldBe(ProblemDetailsDetailsVisibility.Metadata_only);
        string response = exception.Response.ShouldNotBeNull();
        response.ShouldNotContain("tenant-alpha", Case.Insensitive);
        response.ShouldNotContain("restricted@example.com", Case.Insensitive);
        response.ShouldNotContain("Secret Project", Case.Insensitive);
        response.ShouldNotContain("raw provider payload", Case.Insensitive);
        response.ShouldNotContain("/tmp/", Case.Insensitive);
    }

    private static string SuccessBody()
        => $$"""
            {
              "associationId": "{{AssociationId}}",
              "intakeId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "sourceMailboxId": "controlled-mailbox-001",
              "sourceConversationId": "conversation-001",
              "sourceThreadId": "thread-001",
              "lifecycleState": "NeedsReview",
              "outcome": "candidates-generated",
              "thresholdBand": "ambiguous",
              "confidenceScore": 0.72,
              "reasonCodes": ["multiple-authorized-candidates"],
              "candidates": [
                {
                  "projectId": "project-001",
                  "displayName": "Authorized candidate",
                  "confidenceScore": 0.72,
                  "rank": 1,
                  "reasonCodes": ["explicit-project-identifier-matched"],
                  "evidenceRefs": [
                    {
                      "evidenceReference": "mailbox:project-id",
                      "evidenceFingerprint": "hash-project",
                      "evidenceKind": "ExplicitProjectIdentifier",
                      "redactionState": "metadata_only",
                      "visibilityState": "available",
                      "freshnessState": "fresh"
                    }
                  ],
                  "confidenceInputs": [],
                  "requiredEvidenceComplete": true
                }
              ],
              "exclusions": [],
              "thresholdPolicyVersion": "association-thresholds.m0.default.v1",
              "evidenceRefs": [
                {
                  "evidenceReference": "mailbox:project-id",
                  "evidenceFingerprint": "hash-project",
                  "evidenceKind": "ExplicitProjectIdentifier",
                  "redactionState": "metadata_only",
                  "visibilityState": "available",
                  "freshnessState": "fresh"
                }
              ],
              "kernelVersion": "association-deterministic.kernel.m0.v1",
              "detectedAt": "2026-05-31T09:00:00+00:00",
              "sourceProvenance": "m365-mailbox-intake",
              "redactionState": "metadata_only",
              "retentionClass": "collaboration_input",
              "schemaVersion": "chatbot.association-routing-status.v1",
              "sourceVersion": 7,
              "correlationId": "{{CorrelationId}}",
              "disabledActionReasonCodes": ["projection-pending"],
              "nextActionReasonCodes": ["association_ambiguous_routed"]
            }
            """;

    private static string ProblemBody(
        int status,
        ProblemDetailsCategory category,
        ProblemDetailsClientAction clientAction)
        => $$"""
            {
              "type": "https://problems.hexalith.local/chatbot/{{status}}",
              "title": "Synthetic metadata-only association routing problem",
              "status": {{status}},
              "category": "{{WireValue(category)}}",
              "code": "synthetic_association_routing_problem",
              "message": "Synthetic safe routing-status failure.",
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
            ProblemDetailsCategory.Internal_error => "internal_error",
            ProblemDetailsClientAction.Authenticate => "authenticate",
            ProblemDetailsClientAction.RetryLater => "retry-later",
            ProblemDetailsClientAction.RequestAccess => "request-access",
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

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastMethod = request.Method;
            LastPath = request.RequestUri?.AbsolutePath;
            LastCorrelationId = request.Headers.TryGetValues("X-Correlation-Id", out IEnumerable<string>? correlationValues)
                ? correlationValues.SingleOrDefault()
                : null;
            LastTaskId = request.Headers.TryGetValues("X-Hexalith-Task-Id", out IEnumerable<string>? taskValues)
                ? taskValues.SingleOrDefault()
                : null;

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            });
        }
    }
}
