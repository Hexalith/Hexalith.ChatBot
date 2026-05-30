using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway;

internal static class ChatBotProblemDetailsFactory
{
    public static ProblemDetails Create(string reasonCode, string correlationId, string? taskId)
    {
        (int status, ProblemDetailsCategory category, ProblemDetailsClientAction action) = reasonCode switch
        {
            ChatBotAuthorizationReasonCodes.AuthenticationDenied =>
                (StatusCodes.Status401Unauthorized, ProblemDetailsCategory.Authentication_failure, ProblemDetailsClientAction.Authenticate),
            _ => (StatusCodes.Status403Forbidden, ProblemDetailsCategory.Authorization_denied, ProblemDetailsClientAction.None),
        };

        return new ProblemDetails
        {
            Type = "https://hexalith.dev/errors/chatbot/authorization-denied",
            Title = status == StatusCodes.Status401Unauthorized ? "Authentication required." : "Authorization denied.",
            Status = status,
            Category = category,
            Code = reasonCode == ChatBotAuthorizationReasonCodes.AuthenticationDenied
                ? ChatBotAuthorizationReasonCodes.AuthenticationDenied
                : ChatBotAuthorizationReasonCodes.AuthorizationDenied,
            Message = status == StatusCodes.Status401Unauthorized
                ? "Authentication is required to submit this command."
                : "Access is denied. The caller is not authorized for this operation or resource.",
            CorrelationId = correlationId,
            TaskId = taskId,
            Retryable = false,
            ClientAction = action,
            Details = new ProblemDetailsDetails
            {
                Visibility = ProblemDetailsDetailsVisibility.Metadata_only,
            },
        };
    }
}
