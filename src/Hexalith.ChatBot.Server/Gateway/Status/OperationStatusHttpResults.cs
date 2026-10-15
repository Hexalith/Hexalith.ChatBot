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
        bool validCanonicalMetadata = record.CanonicalRevision is null ||
            (record.ResourceIds is not null && record.ResourceIds.All(Hexalith.ChatBot.Server.Operations.CommandOperationMetadataValidation.IsValidResourceId) &&
                record.LastTransition is not null && Hexalith.ChatBot.Server.Operations.CommandOperationMetadataValidation.IsValidActorId(record.LastTransition.ActorId));
        bool knownReason = validCanonicalMetadata && KnownCode(record.ReasonCode) && Enum.IsDefined(record.LifecycleState) &&
            (record.ExecutionState is null || Enum.IsDefined(record.ExecutionState.Value)) &&
            (record.ProjectionState is null || Enum.IsDefined(record.ProjectionState.Value)) &&
            (record.LastTransition is null || (KnownCode(record.LastTransition.ReasonCode) &&
                Enum.IsDefined(record.LastTransition.State) && Enum.IsDefined(record.LastTransition.ProjectionState))) &&
            KnownOptionalCode(record.TerminalReason) &&
            KnownOptionalCode(record.FailureReasonCode) &&
            KnownOptionalCode(record.TerminalReasonCode) &&
            KnownOptionalCode(record.WorkflowLastFailureCode) &&
            (previous is null || (Enum.IsDefined(previous.LifecycleState) && KnownCode(CodeValue(previous.ReasonCode))));
        if (!knownReason)
        {
            completion = OperationCompletionStatus.Failed;
        }
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(knownReason ? record.ReasonCode : null);
        ChatBotMessageCatalogEntry? previousEntry = knownReason && previous is not null
            ? ChatBotMessageCatalog.Resolve(CodeValue(previous.ReasonCode)) : null;
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
            RetryEligible = knownReason && !entry.IsTerminal &&
                (record.CanonicalRevision is null || (record.ExecutionState == Hexalith.ChatBot.Contracts.Enums.CommandExecutionState.Failed &&
                    record.LastTransition is { PreEffectFailure: true } && record.CommittedRevision is null)) &&
                safeNextActions.Contains(ChatBotMessageNextAction.RetryLater) &&
                record.NextRetryAt is { } due && due <= now &&
                (record.CanonicalRevision is null ? record.RetryCount < record.MaxAttempts : (long)record.RetryCount + 1 < record.MaxAttempts),
            PriorOutcome = !knownReason || previous is null ? null : new PriorCommandOutcome
            {
                CommandId = previous.CommandId,
                CorrelationId = previous.CorrelationId,
                OperationId = string.IsNullOrWhiteSpace(previous.OperationId) ? previous.TaskId ?? previous.CommandId : previous.OperationId,
                TaskId = previous.TaskId,
                LifecycleState = previous.LifecycleState,
                AcceptedAt = previous.AcceptedAt.ToUniversalTime(),
                ReasonCode = previous.ReasonCode,
                RetryEligible = previous.RetryEligible && previousEntry is { IsTerminal: false, NextAction: ChatBotMessageNextActions.RetryLater },
            },
            RetryCount = knownReason ? record.RetryCount : 0,
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
            MaxAttempts = knownReason ? record.MaxAttempts : 1,
            NextRetryAt = knownReason ? record.NextRetryAt?.ToUniversalTime() : null,
            DuplicateSafetyNote = knownReason ? record.DuplicateSafetyNote : null,
            OwnerRole = knownReason ? record.OwnerRole : null,
            FailureReasonCode = knownReason ? ParseCodeOrNull(record.FailureReasonCode) : ChatBotMessageCode.Generic_denial,
            TerminalReasonCode = knownReason ? ParseCodeOrNull(record.TerminalReasonCode) : ChatBotMessageCode.Generic_denial,
            OriginalOperationId = knownReason ? record.OriginalOperationId : null,
            DuplicateAttemptCount = knownReason ? record.DuplicateAttemptCount : 0,
            WorkflowInstanceId = knownReason ? record.WorkflowInstanceId : null,
            WorkflowStatus = knownReason ? record.WorkflowStatus : null,
            WorkflowRetryCount = knownReason ? record.WorkflowRetryCount : 0,
            WorkflowLastFailureCode = knownReason ? ParseCodeOrNull(record.WorkflowLastFailureCode) : null,
            AcceptedAt = record.AcceptedAt.ToUniversalTime(),
            LastUpdatedAt = record.LastUpdatedAt.ToUniversalTime(),
            ExecutionState = knownReason && record.ExecutionState is { } execution ? Enum.Parse<CommandExecutionState>(execution.ToString()) : null,
            ProjectionState = knownReason && record.ProjectionState is { } projection ? Enum.Parse<CommandProjectionState>(projection.ToString()) : null,
            CanonicalRevision = knownReason ? record.CanonicalRevision : null,
            ExpectedRevision = knownReason ? record.ExpectedRevision : null,
            CommittedRevision = knownReason ? record.CommittedRevision : null,
            Origin = knownReason && record.Origin is { } origin && TryParse(origin, out SurfaceOrigin surface) ? surface : null,
            ResourceIds = knownReason ? record.ResourceIds : null,
            CreatedAt = knownReason ? record.CreatedAt?.ToUniversalTime() : null,
            EquivalenceVersion = knownReason && record.EquivalenceVersion == "v1" ? OperationStatusEquivalenceVersion.V1 : null,
            PredecessorOperationId = knownReason ? record.PredecessorOperationId : null,
            HasPartialOutputs = knownReason && record.PartialOutputCodes is { Length: > 0 },
            LastTransition = knownReason && record.LastTransition is { } fact && KnownCode(fact.ReasonCode) ? new CommandOperationTransition
            {
                Revision = fact.Revision,
                State = Enum.Parse<CommandExecutionState>(fact.State.ToString()),
                ProjectionState = Enum.Parse<CommandProjectionState>(fact.ProjectionState.ToString()),
                ActorId = fact.ActorId,
                OccurredAt = fact.OccurredAt.ToUniversalTime(),
                ReasonCode = ParseCode(fact.ReasonCode),
                CorrelationId = fact.CorrelationId,
            } : null,
        };
    }

    private static ChatBotMessageCode ParseCode(string code)
        => TryParse(code, out ChatBotMessageCode value) ? value : ChatBotMessageCode.Generic_denial;

    private static string? CodeValue(ChatBotMessageCode code)
        => typeof(ChatBotMessageCode).GetField(code.ToString())?.GetCustomAttribute<EnumMemberAttribute>()?.Value;

    private static ChatBotMessageNextAction ParseAction(string code)
        => TryParse(code, out ChatBotMessageNextAction value) ? value : ChatBotMessageNextAction.Dismiss;

    private static ChatBotMessageCode? ParseCodeOrNull(string? code)
        => string.IsNullOrWhiteSpace(code) ? null : ParseCode(code);

    private static bool KnownOptionalCode(string? code)
        => string.IsNullOrWhiteSpace(code) || KnownCode(code);

    private static bool KnownCode(string? code)
        => code != ChatBotMessageCodes.GenericDenial && ChatBotMessageCatalog.IsKnown(code);

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
