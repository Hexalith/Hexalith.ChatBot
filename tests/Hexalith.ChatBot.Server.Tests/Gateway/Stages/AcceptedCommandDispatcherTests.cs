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
        AcceptedCommandDispatcher dispatcher = new(gateway, clock);

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
        AcceptedCommandDispatcher dispatcher = new(gateway, new FixedClock());

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
    public async Task DispatchWithoutTaskIdShouldOmitExtensions()
    {
        RecordingEventStoreGatewayClient gateway = new();
        AcceptedCommandDispatcher dispatcher = new(gateway, new FixedClock());

        _ = await dispatcher.DispatchAsync(Context(WireCommand(NoteId), taskId: null), TestContext.Current.CancellationToken);

        gateway.Submitted.ShouldHaveSingleItem().Extensions.ShouldBeNull();
    }

    private static ChatBotGatewayContext Context(JsonElement command, string? taskId = TaskId)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test"));
        ChatBotCommandSubmission submission = new(
            principal,
            new CommandSubmissionRequest
            {
                CommandId = CommandId,
                CommandType = nameof(RecordGovernedNote),
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

    private sealed class FixedClock : ISystemClock
    {
        public static DateTimeOffset FixedUtcNow { get; } = new(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);

        public DateTimeOffset UtcNow => FixedUtcNow;
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
