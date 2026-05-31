using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Real EventStore dispatch behind the <see cref="ICommandDispatcher"/> seam. It routes an admitted command
/// into EventStore through the public gateway client — the durable segment of the spine
/// (<c>fine-idempotency → execute → persist → publish → project</c>) runs inside EventStore — and forwards
/// correlation + task provenance. <see cref="CommandGateway"/> remains the single caller of <see cref="DispatchAsync"/>.
/// </summary>
internal sealed class AcceptedCommandDispatcher(
    IEventStoreGatewayClient eventStore,
    ISystemClock clock) : ICommandDispatcher
{
    // The EventStoreAggregate base deserializes the command payload with default (case-sensitive, PascalCase)
    // JsonSerializer options. The inbound wire body is camelCase, so we read it case-insensitively (web options)
    // and re-serialize PascalCase (default options) — otherwise the engine would fail to bind the payload.
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<ChatBotDispatchResult> DispatchAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        EventStoreDispatchPlan plan = BuildPlan(context);
        SubmitCommandRequest request = new(
            MessageId: context.Submission.Request.CommandId,
            Tenant: context.TenantBinding.TenantId,
            Domain: ChatBotEventStore.DomainName,
            AggregateId: plan.AggregateId,
            CommandType: plan.CommandType,
            Payload: plan.Payload,
            CorrelationId: context.Submission.CorrelationId,
            Extensions: BuildExtensions(context.Submission.TaskId));

        _ = await eventStore.SubmitCommandAsync(request, cancellationToken).ConfigureAwait(false);

        return new ChatBotDispatchResult(clock.UtcNow, plan.AggregateId);
    }

    private static EventStoreDispatchPlan BuildPlan(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        JsonElement command = ToElement(context.Submission.Request.Command);

        if (string.Equals(commandType, nameof(RecordGovernedNote), StringComparison.Ordinal))
        {
            RecordGovernedNote note = command.Deserialize<RecordGovernedNote>(ReadOptions)
                ?? throw new InvalidOperationException("The governed note command payload could not be read.");
            if (string.IsNullOrWhiteSpace(note.NoteId))
            {
                throw new InvalidOperationException("The governed note command is missing its aggregate identity.");
            }

            // PascalCase payload (default options) so the case-sensitive aggregate engine round-trips it.
            JsonElement payload = JsonSerializer.SerializeToElement(note);
            return new EventStoreDispatchPlan(note.NoteId, commandType, payload);
        }

        // Defensive fallback: the spine allowlist admits only RecordGovernedNote in production, so this branch
        // is reached only by bootstrap tests that submit a generic command through a permissive allowlist.
        return new EventStoreDispatchPlan(context.Submission.Request.CommandId, commandType, command);
    }

    private static JsonElement ToElement(object? command)
        => command is JsonElement element
            ? element
            : JsonSerializer.SerializeToElement(command, ReadOptions);

    private static Dictionary<string, string>? BuildExtensions(string? taskId)
        => string.IsNullOrWhiteSpace(taskId)
            ? null
            : new Dictionary<string, string>(StringComparer.Ordinal) { ["taskId"] = taskId };

    private sealed record EventStoreDispatchPlan(string AggregateId, string CommandType, JsonElement Payload);
}
