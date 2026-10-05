using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway;

internal interface IChatBotProblemDetailsFactory
{
    ProblemDetails CreateValidationProblem(string correlationId, string? taskId)
    {
        Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCatalogEntry entry =
            Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCatalog.Resolve(
                Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCodes.CommandContractInvalid);
        return new()
        {
            Type = ChatBotProblemTypes.ValidationFailure,
            Title = entry.Headline,
            Status = StatusCodes.Status400BadRequest,
            Category = ProblemDetailsCategory.Validation_error,
            Code = Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCodes.CommandContractInvalid,
            Message = entry.Reason,
            CorrelationId = correlationId,
            TaskId = taskId,
            Retryable = false,
            ClientAction = ProblemDetailsClientAction.CorrectRequest,
            Details = new ProblemDetailsDetails { Visibility = ProblemDetailsDetailsVisibility.Metadata_only },
            SchemaVersion = Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCatalogVersion.Current,
        };
    }
    ProblemDetails CreateAuthorizationProblem(string reasonCode, string correlationId, string? taskId);

    ProblemDetails CreateAuditUnavailable(string correlationId, string? taskId);

    ProblemDetails CreateDispatchUnavailable(string correlationId, string? taskId);

    ProblemDetails CreateIdempotencyConflict(string correlationId, string? taskId, string? catalogCode = null);

    ProblemDetails CreateInvalidLifecycleTransition(string correlationId, string? taskId);

    ProblemDetails CreateCommandNotAllowlisted(string correlationId, string? taskId);
}
