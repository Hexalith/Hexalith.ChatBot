using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Server.Gateway.Redaction;

internal sealed class CoarseUserFacingRedactionStage : IUserFacingRedactionStage
{
    public const string MetadataOnlyDecision = "metadata_only";

    public ProblemDetails Apply(ProblemDetails problem)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(problem.Code);
        bool known = ChatBotMessageCatalog.IsKnown(problem.Code);
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
            Type = known ? KnownType(problem.Type) : ChatBotProblemTypes.AuthorizationDenied,
            Title = entry.Headline,
            Status = known && problem.Status is >= 400 and <= 599
                ? problem.Status
                : StatusCodes.Status403Forbidden,
            Detail = null,
            Instance = null,
            Category = known ? problem.Category : ProblemDetailsCategory.Authorization_denied,
            Code = entry.Code,
            Message = entry.Reason,
            CorrelationId = problem.CorrelationId,
            TaskId = problem.TaskId,
            Retryable = known && problem.Retryable && !entry.IsTerminal &&
                catalogAction == ProblemDetailsClientAction.RetryLater &&
                problem.ClientAction == ProblemDetailsClientAction.RetryLater,
            ClientAction = !known ? ProblemDetailsClientAction.Dismiss
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
            ChatBotProblemTypes.DependencyUnavailable or
            ChatBotProblemTypes.DispatchUnavailable or
            ChatBotProblemTypes.IdempotencyConflict or
            ChatBotProblemTypes.InvalidLifecycleTransition or
            ChatBotProblemTypes.CommandNotAllowlisted => type,
            _ => ChatBotProblemTypes.AuthorizationDenied,
        };
}
