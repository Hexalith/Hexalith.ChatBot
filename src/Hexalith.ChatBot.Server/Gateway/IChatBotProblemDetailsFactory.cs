using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway;

internal interface IChatBotProblemDetailsFactory
{
    ProblemDetails CreateAuthorizationProblem(string reasonCode, string correlationId, string? taskId);

    ProblemDetails CreateAuditUnavailable(string correlationId, string? taskId);

    ProblemDetails CreateIdempotencyConflict(string correlationId, string? taskId);

    ProblemDetails CreateInvalidLifecycleTransition(string correlationId, string? taskId);
}
