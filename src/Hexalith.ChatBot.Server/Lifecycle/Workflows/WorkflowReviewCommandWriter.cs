using System.Text.Json;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Operations;
using Ulid = ByteAether.Ulid.Ulid;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Dispatches a deterministic internal transition through the signed, bound EventStore workload route.</summary>
internal sealed class WorkflowReviewCommandWriter(IEventStoreGatewayClient gateway, IChatBotAdmissionMarker marker, IServiceScopeFactory scopes) : IWorkflowReviewCommandWriter
{
    internal const string Actor = "review-lifecycle-workflow";

    public async Task RecordAsync(string tenant, string correlation, RecordWorkflowReviewTransition command, CancellationToken token)
    {
        string aggregate = command.StreamId(tenant);
        string type = nameof(RecordWorkflowReviewTransition);
        byte[] digest = System.Security.Cryptography.SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new object[]
            { aggregate, command.Generation, command.Attempt, command.State }));
        string message = Ulid.New(digest.AsSpan(0, 16)).ToString();
        JsonElement payload = JsonSerializer.SerializeToElement(command);
        var request = new SubmitCommandRequest(message, tenant, ChatBotEventStore.DomainName, aggregate, type, payload,
            CorrelationId: correlation, IdempotencyKey: message, Extensions: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["actorId"] = Actor, ["actorType"] = "system", ["surfaceOrigin"] = "workflow",
                [DataProtectionChatBotAdmissionMarker.ExtensionKey] = marker.Create(message, tenant, aggregate, type, payload,
                    correlation, Actor, "workflow", null),
            });
        using var scope = scopes.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<Hexalith.EventStore.Client.Streams.IAuthoritativeEventStreamReader>();
        var identity = new Hexalith.EventStore.Contracts.Identity.AggregateIdentity(tenant, request.Domain, aggregate);
        async Task<bool> CommittedAsync()
        {
            var result = await reader.ReadAsync(identity, token).ConfigureAwait(false);
            if (!result.IsAuthoritative || result.Stream is not { } stream || stream.Identity != identity) { return false; }
            foreach (var item in stream.Events.Reverse())
            {
                if (item.EventTypeName.Split(',', 2)[0] != typeof(WorkflowReviewTransitionRecorded).FullName) { continue; }
                var previous = JsonSerializer.Deserialize<WorkflowReviewTransitionRecorded>(item.Payload, JsonSerializerOptions.Web)?.Transition;
                if (previous is null || previous.StreamId(tenant) != aggregate) { return false; }
                // A replayed start may resume only its own in-flight attempt. A newer owner result
                // must fence external work even when this command would be an aggregate no-op.
                if (command.State == "retrying" && (previous.Generation > command.Generation ||
                    previous.Generation == command.Generation && (previous.State is "recovered" or "exhausted" or "nonretryable" ||
                    (previous.Attempt, previous.Stage).CompareTo((command.Attempt, command.Stage)) > 0)))
                { throw new InvalidOperationException("workflow_review_attempt_superseded"); }
                return previous.Generation > command.Generation || previous.Generation == command.Generation &&
                    (previous.State == "recovered" || previous.State is ("exhausted" or "nonretryable") && command.State != "recovered" ||
                    (previous.Attempt, previous.Stage).CompareTo((command.Attempt, command.Stage)) >= 0);
            }
            return false;
        }
        if (await CommittedAsync().ConfigureAwait(false)) { return; }
        try
        {
            var receipt = await gateway.SubmitCommandAsync(request, token).ConfigureAwait(false);
            if (receipt.MessageId != message || receipt.CorrelationId != correlation)
            { throw new InvalidOperationException("workflow_review_receipt_mismatch"); }
        }
        catch (EventStoreGatewayException ex) when (ex.StatusCode == 409)
        {
            // A concurrent submission may already own this deterministic message. Only the stream can confirm it.
        }
        for (int poll = 0; poll < 8; poll++)
        {
            if (await CommittedAsync().ConfigureAwait(false)) { return; }
            await Task.Delay(TimeSpan.FromMilliseconds(250), token).ConfigureAwait(false);
        }
        throw new InvalidOperationException("workflow_review_commit_unconfirmed");
    }
}
