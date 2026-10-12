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
        bool knownReason = ChatBotMessageCatalog.IsKnown(record.ReasonCode) &&
            KnownOptionalCode(record.TerminalReason) &&
            KnownOptionalCode(record.FailureReasonCode) &&
            KnownOptionalCode(record.TerminalReasonCode) &&
            KnownOptionalCode(record.WorkflowLastFailureCode);
        if (!knownReason)
        {
            completion = OperationCompletionStatus.Failed;
        }
        CommandSubmissionResponse? previous = record.PriorOutcome;
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(knownReason ? record.ReasonCode : null);
        List<ChatBotMessageNextAction> safeNextActions = knownReason
            ? (record.SafeNextActions ?? [])
                .Where(action => string.Equals(action, entry.NextAction, StringComparison.Ordinal))
                .Select(ParseAction)
                .Distinct()
                .ToList()
            : [ChatBotMessageNextAction.Dismiss];
        return new OperationStatus
        {
            OperationId = record.OperationId,
            CommandId = record.CommandId,
            CorrelationId = record.CorrelationId,
            LifecycleState = knownReason ? record.LifecycleState : LifecycleState.Failed,
            ReasonCode = knownReason ? ParseCode(record.ReasonCode) : ChatBotMessageCode.Generic_denial,
            RetryEligible = safeNextActions.Contains(ChatBotMessageNextAction.RetryLater) &&
                record.NextRetryAt is { } due && due <= now && record.RetryCount < record.MaxAttempts,
            PriorOutcome = !knownReason || previous is null ? null : new PriorCommandOutcome
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
            SafeNextActions = safeNextActions,
            TerminalReason = knownReason ? ParseCodeOrNull(record.TerminalReason) : ChatBotMessageCode.Generic_denial,
            OperationClass = record.OperationClass,
            MaxAttempts = record.MaxAttempts,
            NextRetryAt = knownReason ? record.NextRetryAt?.ToUniversalTime() : null,
            DuplicateSafetyNote = knownReason ? record.DuplicateSafetyNote : null,
            OwnerRole = knownReason ? record.OwnerRole : null,
            FailureReasonCode = knownReason ? ParseCodeOrNull(record.FailureReasonCode) : ChatBotMessageCode.Generic_denial,
            TerminalReasonCode = knownReason ? ParseCodeOrNull(record.TerminalReasonCode) : ChatBotMessageCode.Generic_denial,
            OriginalOperationId = knownReason ? record.OriginalOperationId : null,
            DuplicateAttemptCount = record.DuplicateAttemptCount,
            WorkflowInstanceId = knownReason ? record.WorkflowInstanceId : null,
            WorkflowStatus = knownReason ? record.WorkflowStatus : null,
            WorkflowRetryCount = record.WorkflowRetryCount,
            WorkflowLastFailureCode = knownReason ? ParseCodeOrNull(record.WorkflowLastFailureCode) : null,
            AcceptedAt = record.AcceptedAt.ToUniversalTime(),
            LastUpdatedAt = record.LastUpdatedAt.ToUniversalTime(),
        };
    }

    private static ChatBotMessageCode ParseCode(string code)
        => TryParse(code, out ChatBotMessageCode value) ? value : ChatBotMessageCode.Generic_denial;

    private static ChatBotMessageNextAction ParseAction(string code)
        => TryParse(code, out ChatBotMessageNextAction value) ? value : ChatBotMessageNextAction.Dismiss;

    private static ChatBotMessageCode? ParseCodeOrNull(string? code)
        => string.IsNullOrWhiteSpace(code) ? null : ParseCode(code);

    private static bool KnownOptionalCode(string? code)
        => string.IsNullOrWhiteSpace(code) || ChatBotMessageCatalog.IsKnown(code);

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
