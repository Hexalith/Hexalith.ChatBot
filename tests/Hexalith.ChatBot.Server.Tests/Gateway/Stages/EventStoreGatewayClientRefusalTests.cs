using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

/// <summary>
/// Drives the production <see cref="EventStoreGatewayClient"/> over EventStore's exact <c>application/problem+json</c>
/// refusal bodies, proving the client surfaces the <c>type</c> and <c>extensions.rejectionType</c> evidence that
/// <see cref="EventStoreDefinitiveRefusal"/> reads. The bodies mirror the EventStore host's
/// <c>DomainCommandRejectedExceptionHandler</c> (with <c>DomainRejectionProblemCatalog</c>),
/// <c>BackpressureExceptionHandler</c>, and rate-limiter <c>OnRejected</c> problem documents.
/// </summary>
public sealed class EventStoreGatewayClientRefusalTests
{
    private const string CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string Tenant = "tenant-alpha";

    [Theory]
    [InlineData("domain-rejection", 422, true)]
    [InlineData("backpressure", 429, true)]
    [InlineData("rate-limit", 429, false)]
    public async Task ProductionClientShouldSurfaceTheRefusalEvidenceTheClassifierReads(string kind, int status, bool definitive)
    {
        EventStoreGatewayClient client = Client(kind, out StubHandler handler);

        EventStoreGatewayException refusal = await Should.ThrowAsync<EventStoreGatewayException>(() => client.SubmitCommandAsync(
            new SubmitCommandRequest(CommandId, Tenant, "chatbot", "01ARZ3NDEKTSV4RRFFQ69G5FAZ", nameof(RecordGovernedNote),
                JsonSerializer.SerializeToElement(new { NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ" }), CorrelationId),
            TestContext.Current.CancellationToken));

        handler.Requests.ShouldBe(1);
        refusal.StatusCode.ShouldBe(status);
        EventStoreDefinitiveRefusal.IsDefinitive(refusal).ShouldBe(definitive);
        if (kind == "domain-rejection")
        {
            refusal.Type.ShouldBe("https://hexalith.io/problems/domain-rejections/governed-note-rejected");
            refusal.Extensions["rejectionType"].GetString().ShouldBe("Hexalith.ChatBot.Server.GovernedNoteRejected");
        }
    }

    [Theory]
    [InlineData("domain-rejection", true)]
    [InlineData("backpressure", true)]
    [InlineData("rate-limit", false)]
    public async Task DispatcherShouldRaiseDefinitiveRefusalOnlyForProvenNonCommitResponses(string kind, bool definitive)
    {
        EventStoreGatewayClient client = Client(kind, out StubHandler handler);
        AcceptedCommandDispatcher dispatcher = new(client, null!, null!, new FixedClock());
        ChatBotGatewayContext context = Context();

        Exception failure = await Should.ThrowAsync<Exception>(() => dispatcher.DispatchAsync(context, TestContext.Current.CancellationToken).AsTask());

        handler.Requests.ShouldBe(1);
        context.ExternalEffectAttempted.ShouldBeTrue();
        if (definitive)
        {
            failure.ShouldBeOfType<CommandDefinitivelyRefusedException>().InnerException.ShouldBeOfType<EventStoreGatewayException>();
        }
        else
        {
            // Not proven: the raw gateway failure stays an uncertain outcome that keeps its dispatch ownership.
            failure.ShouldBeOfType<EventStoreGatewayException>();
        }
    }

    private static EventStoreGatewayClient Client(string kind, out StubHandler handler)
    {
        handler = new StubHandler(kind);
        HttpClient http = new(handler) { BaseAddress = new Uri("http://eventstore.test/") };
        return new EventStoreGatewayClient(http, Options.Create(new EventStoreGatewayClientOptions()));
    }

    private static ChatBotGatewayContext Context()
    {
        ClaimsPrincipal principal = Hexalith.ChatBot.Tests.TrustedAuthority.RegressionAuthorityFixture.Principal(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test")));
        ChatBotCommandSubmission submission = new(
            principal,
            new CommandSubmissionRequest
            {
                CommandId = CommandId,
                CommandType = nameof(RecordGovernedNote),
                Command = JsonDocument.Parse("""{"noteId":"01ARZ3NDEKTSV4RRFFQ69G5FAZ"}""").RootElement.Clone(),
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            CorrelationId,
            null,
            ChatBotSurfaceOrigin.Ui);
        ChatBotGatewayContext context = new(submission, new ChatBotAuthenticatedActor("actor-alpha", principal), new ChatBotTenantBinding(Tenant));
        // This standalone transport test supplies the gateway's binding explicitly; ownership fencing has separate tests.
        context.SetDispatchTargetBinding(static (_, _) => ValueTask.FromResult(true));
        return context;
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 10, 6, 8, 30, 0, TimeSpan.Zero);
    }

    /// <summary>Returns EventStore's exact refusal response for every request.</summary>
    private sealed class StubHandler(string kind) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            request.RequestUri.ShouldNotBeNull().AbsolutePath.ShouldBe("/api/v1/commands");
            (HttpStatusCode status, string body) = kind switch
            {
                // DomainCommandRejectedExceptionHandler + DomainRejectionProblemCatalog for rejection type
                // "Hexalith.ChatBot.Server.GovernedNoteRejected" (reason "governed-note-rejected" maps to 422).
                "domain-rejection" => ((HttpStatusCode)422, """
                    {"type":"https://hexalith.io/problems/domain-rejections/governed-note-rejected","title":"Governed Note Rejected","status":422,"detail":"The command was syntactically valid but rejected by domain business rules or validation.","instance":"/api/v1/commands","correlationId":"01ARZ3NDEKTSV4RRFFQ69G5FAW","tenantId":"tenant-alpha","reasonCode":"governed-note-rejected","rejectionType":"Hexalith.ChatBot.Server.GovernedNoteRejected","correctiveAction":"Review the rejection detail, correct the request, and retry when appropriate."}
                    """),

                // BackpressureExceptionHandler.
                "backpressure" => (HttpStatusCode.TooManyRequests, """
                    {"type":"https://hexalith.io/problems/backpressure-exceeded","title":"Too Many Requests","status":429,"detail":"The target aggregate is under backpressure due to excessive pending commands. Please retry after the specified interval.","instance":"/api/v1/commands","correlationId":"01ARZ3NDEKTSV4RRFFQ69G5FAW","tenantId":"tenant-alpha","domain":"chatbot","aggregateId":"01ARZ3NDEKTSV4RRFFQ69G5FAZ"}
                    """),

                // Rate-limiter OnRejected: also 429, but a gateway-level throttle, not a classified refusal.
                "rate-limit" => (HttpStatusCode.TooManyRequests, """
                    {"type":"https://hexalith.io/problems/rate-limit-exceeded","title":"Too Many Requests","status":429,"detail":"Rate limit exceeded. Please retry after the specified interval.","instance":"/api/v1/commands","correlationId":"01ARZ3NDEKTSV4RRFFQ69G5FAW","tenantId":"tenant-alpha","consumerId":"chatbot"}
                    """),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
            HttpResponseMessage response = new(status)
            {
                Content = new StringContent(body.Trim(), Encoding.UTF8, "application/problem+json"),
            };
            if (status == HttpStatusCode.TooManyRequests)
            {
                response.Headers.Add("Retry-After", "5");
            }

            return Task.FromResult(response);
        }
    }
}
