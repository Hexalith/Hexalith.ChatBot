using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal static class CoarseIdempotencyComposer
{
    public static CoarseIdempotencyRecord ComposeCommandExecutionRecord(
        ChatBotGatewayContext context,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(context);

        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.CommandExecution;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string commandInputHash = HashCommandInput(context.Submission.Request.Command);
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            operation.Code,
            commandName,
            commandInputHash,
            context.Actor.ActorId);
        DateTimeOffset expiresAt = operation.ReplayWindow is { } replayWindow
            ? now.Add(replayWindow)
            : DateTimeOffset.MaxValue;

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            commandInputHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            expiresAt,
            PriorOutcome: null);
    }

    private static string HashCommandInput(object? command)
    {
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(command, JsonOptions));
        return CoarseIdempotencyCanonicalizer.HashCanonicalJson(document.RootElement);
    }

    private static string HashParts(params string[] parts)
    {
        string value = string.Join('\u001f', parts.Select(static part => part.Normalize(NormalizationForm.FormC)));
        return CoarseIdempotencyCanonicalizer.HashUtf8(value);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
