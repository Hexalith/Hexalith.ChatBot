using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Server.Gateway.Redaction;

internal sealed class CoarseUserFacingRedactionStage : IUserFacingRedactionStage
{
    public const string MetadataOnlyDecision = "metadata_only";

    public ProblemDetails Apply(ProblemDetails problem)
    {
        ArgumentNullException.ThrowIfNull(problem);
        bool known = ChatBotMessageCatalog.IsKnown(problem.Code) && Enum.IsDefined(problem.Category) &&
            (ChatBotMessageCatalog.Resolve(problem.Code).State is not ("waiting" or "completed") ||
                problem.Code?.StartsWith("idempotency_conflict_", StringComparison.Ordinal) == true);
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(known ? problem.Code : null);
        int status = problem.Status is >= 400 and <= 599
            ? problem.Status == StatusCodes.Status404NotFound
                ? StatusCodes.Status403Forbidden
                : problem.Status
            : StatusCodes.Status403Forbidden;
        ProblemDetailsClientAction catalogAction = entry.NextAction switch
        {
            ChatBotMessageNextActions.Authenticate => ProblemDetailsClientAction.Authenticate,
            ChatBotMessageNextActions.CorrectRequest => ProblemDetailsClientAction.CorrectRequest,
            ChatBotMessageNextActions.RetryLater => ProblemDetailsClientAction.RetryLater,
            ChatBotMessageNextActions.RequestAccess => ProblemDetailsClientAction.RequestAccess,
            ChatBotMessageNextActions.Escalate => ProblemDetailsClientAction.Escalate,
            ChatBotMessageNextActions.Dismiss => ProblemDetailsClientAction.Dismiss,
            _ => ProblemDetailsClientAction.None,
        };

        return new ProblemDetails
        {
            Type = known && entry.Code != ChatBotMessageCodes.GenericDenial
                ? KnownType(problem.Type) : ChatBotProblemTypes.GenericDenial,
            Title = entry.Headline,
            Status = status,
            Detail = null,
            Instance = null,
            Category = known && entry.Code != ChatBotMessageCodes.GenericDenial
                ? problem.Category : CategoryForStatus(status),
            Code = entry.Code,
            Message = entry.Reason,
            CorrelationId = problem.CorrelationId,
            TaskId = problem.TaskId,
            Retryable = known && problem.Retryable && !entry.IsTerminal &&
                catalogAction == ProblemDetailsClientAction.RetryLater &&
                problem.ClientAction == ProblemDetailsClientAction.RetryLater,
            ClientAction = entry.Code == ChatBotMessageCodes.GenericDenial ? ProblemDetailsClientAction.Dismiss
                : problem.ClientAction == catalogAction &&
                    (catalogAction != ProblemDetailsClientAction.RetryLater || problem.Retryable)
                    ? catalogAction
                    : ProblemDetailsClientAction.None,
            Details = new ProblemDetailsDetails
            {
                Visibility = ProblemDetailsDetailsVisibility.Metadata_only,
            },
            SchemaVersion = ChatBotMessageCatalogVersion.Current,
        };
    }

    private static string KnownType(string? type)
        => type switch
        {
            ChatBotProblemTypes.ValidationFailure or
            ChatBotProblemTypes.AuthorizationDenied or
            ChatBotProblemTypes.AuditUnavailable or
            ChatBotProblemTypes.CanonicalExecutionUnavailable or
            ChatBotProblemTypes.DependencyUnavailable or
            ChatBotProblemTypes.DispatchUnavailable or
            ChatBotProblemTypes.IdempotencyConflict or
            ChatBotProblemTypes.InvalidLifecycleTransition or
            ChatBotProblemTypes.CommandNotAllowlisted => type,
            _ => ChatBotProblemTypes.AuthorizationDenied,
        };

    private static ProblemDetailsCategory CategoryForStatus(int status)
        => status switch
        {
            StatusCodes.Status401Unauthorized => ProblemDetailsCategory.Authentication_failure,
            StatusCodes.Status403Forbidden => ProblemDetailsCategory.Authorization_denied,
            StatusCodes.Status400BadRequest or StatusCodes.Status422UnprocessableEntity => ProblemDetailsCategory.Validation_error,
            StatusCodes.Status409Conflict => ProblemDetailsCategory.Conflict,
            _ => ProblemDetailsCategory.Internal_error,
        };
}
