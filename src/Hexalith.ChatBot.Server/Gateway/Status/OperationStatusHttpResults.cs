using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Server.Gateway.Status;

internal static class OperationStatusHttpResults
{
    public static IResult Ok(OperationStatusRecord record, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(record);
        return Results.Text(Newtonsoft.Json.JsonConvert.SerializeObject(ToWire(record, now ?? DateTimeOffset.UtcNow)),
            "application/json", statusCode: StatusCodes.Status200OK);
    }

    public static JsonElement ToJsonElement(OperationStatusRecord record, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(record);
        using JsonDocument document = JsonDocument.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(
            ToWire(record, now ?? DateTimeOffset.UtcNow)));
        return document.RootElement.Clone();
    }

    private static OperationStatus ToWire(OperationStatusRecord record, DateTimeOffset now)
    {
        OperationCompletionStatus completion = Parse<OperationCompletionStatus>(record.CompletionStatus);
        OperationAuditStatus audit = Parse<OperationAuditStatus>(record.AuditStatus);
        CommandSubmissionResponse? previous = record.PriorOutcome;
        return new OperationStatus
        {
            OperationId = record.OperationId,
            CommandId = record.CommandId,
            CorrelationId = record.CorrelationId,
            LifecycleState = record.LifecycleState,
            ReasonCode = ParseCode(record.ReasonCode),
            RetryEligible = record.NextRetryAt is { } due && due <= now && record.RetryCount < record.MaxAttempts,
            PriorOutcome = previous is null ? null : new PriorCommandOutcome
            {
                CommandId = previous.CommandId,
                CorrelationId = previous.CorrelationId,
                OperationId = string.IsNullOrWhiteSpace(previous.OperationId) ? previous.TaskId ?? previous.CommandId : previous.OperationId,
                TaskId = previous.TaskId,
                LifecycleState = previous.LifecycleState,
                AcceptedAt = previous.AcceptedAt.ToUniversalTime(),
                ReasonCode = previous.ReasonCode,
                RetryEligible = previous.RetryEligible,
            },
            RetryCount = record.RetryCount,
            CompletionStatus = completion,
            AuditStatus = audit,
            PartialOutputs = new OperationStatusPartialOutputs
            {
                AcceptedAt = record.AcceptedAt.ToUniversalTime(),
                CompletionStatus = completion,
                AuditStatus = audit,
            },
            SafeNextActions = record.SafeNextActions.Select(Parse<ChatBotMessageNextAction>).ToList(),
            TerminalReason = ParseCodeOrNull(record.TerminalReason),
            OperationClass = record.OperationClass,
            MaxAttempts = record.MaxAttempts,
            NextRetryAt = record.NextRetryAt?.ToUniversalTime(),
            DuplicateSafetyNote = record.DuplicateSafetyNote,
            OwnerRole = record.OwnerRole,
            FailureReasonCode = ParseCodeOrNull(record.FailureReasonCode),
            TerminalReasonCode = ParseCodeOrNull(record.TerminalReasonCode),
            OriginalOperationId = record.OriginalOperationId,
            DuplicateAttemptCount = record.DuplicateAttemptCount,
            WorkflowInstanceId = record.WorkflowInstanceId,
            WorkflowStatus = record.WorkflowStatus,
            WorkflowRetryCount = record.WorkflowRetryCount,
            WorkflowLastFailureCode = ParseCodeOrNull(record.WorkflowLastFailureCode),
            AcceptedAt = record.AcceptedAt.ToUniversalTime(),
            LastUpdatedAt = record.LastUpdatedAt.ToUniversalTime(),
        };
    }

    private static ChatBotMessageCode ParseCode(string code)
        // External derived stores may return a safe but unrecognized failure token.
        // Keep GET available and report a bounded dependency cause instead of a fabricated command failure.
        => TryParse(code, out ChatBotMessageCode value) ? value : ChatBotMessageCode.Dependency_degraded;

    private static ChatBotMessageCode? ParseCodeOrNull(string? code)
        => string.IsNullOrWhiteSpace(code) ? null : ParseCode(code);

    private static T Parse<T>(string code) where T : struct, Enum
        => TryParse(code, out T value) ? value : throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown contract enum value.");

    private static bool TryParse<T>(string code, out T value) where T : struct, Enum
    {
        foreach (T candidate in Enum.GetValues<T>())
        {
            FieldInfo field = typeof(T).GetField(candidate.ToString())!;
            if (string.Equals(field.GetCustomAttribute<EnumMemberAttribute>()?.Value, code, StringComparison.Ordinal))
            {
                value = candidate;
                return true;
            }
        }

        value = default;
        return false;
    }
}
