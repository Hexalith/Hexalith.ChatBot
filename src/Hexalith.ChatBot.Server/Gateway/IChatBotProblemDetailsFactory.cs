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
    /// <summary>Returns the terminal classifier failure; no proposal or approval can remediate this operation.</summary>
    ProblemDetails CreateClassifierIndeterminate(string correlationId, string? taskId)
    {
        Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCatalogEntry entry =
            Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCatalog.Resolve(Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCodes.ClassifierIndeterminate);
        return new()
        {
            Type = ChatBotProblemTypes.ClassifierIndeterminate,
            Title = entry.Headline,
            Status = StatusCodes.Status422UnprocessableEntity,
            Category = ProblemDetailsCategory.Validation_error,
            Code = entry.Code,
            Message = entry.Reason,
            CorrelationId = correlationId,
            TaskId = taskId,
            Retryable = false,
            ClientAction = ProblemDetailsClientAction.CorrectRequest,
            Details = new ProblemDetailsDetails { Visibility = ProblemDetailsDetailsVisibility.Metadata_only },
            SchemaVersion = Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCatalogVersion.Current,
        };
    }

    /// <summary>Returns the distinct-command approval disposition without creating proposal state.</summary>
    ProblemDetails CreateApprovalRequired(string correlationId, string? taskId)
    {
        Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCatalogEntry entry = Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCatalog.Resolve(Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCodes.ApprovalRequired);
        return new()
        {
            Type = ChatBotProblemTypes.ApprovalRequired, Title = entry.Headline,
            Status = StatusCodes.Status409Conflict, Category = ProblemDetailsCategory.Conflict,
            Code = entry.Code, Message = entry.Reason, CorrelationId = correlationId, TaskId = taskId,
            Retryable = false, ClientAction = ProblemDetailsClientAction.Escalate,
            Details = new ProblemDetailsDetails { Visibility = ProblemDetailsDetailsVisibility.Metadata_only },
            SchemaVersion = Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCatalogVersion.Current,
        };
    }

    ProblemDetails CreateAuthorizationProblem(string reasonCode, string correlationId, string? taskId);

    ProblemDetails CreateAuditUnavailable(string correlationId, string? taskId);

    /// <summary>Returns a safe transient dependency failure without attributing it to audit or conflicting content.</summary>
    ProblemDetails CreateDependencyUnavailable(string correlationId, string? taskId) => CreateDispatchUnavailable(correlationId, taskId);

    /// <summary>Returns a non-retryable refusal while canonical execution acceptance gates remain open.</summary>
    ProblemDetails CreateCanonicalExecutionUnavailable(string correlationId, string? taskId);

    ProblemDetails CreateDispatchUnavailable(string correlationId, string? taskId);

    ProblemDetails CreateIdempotencyConflict(string correlationId, string? taskId, string? catalogCode = null);

    ProblemDetails CreateInvalidLifecycleTransition(string correlationId, string? taskId);

    ProblemDetails CreateCommandNotAllowlisted(string correlationId, string? taskId);
}
