using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.UI.State.AssociationReview;

namespace Hexalith.ChatBot.UI.Services;

/// <summary>Maps transport failures to safe public catalog codes without exposing exception text.</summary>
internal static class ChatBotUiFailureCatalog
{
    /// <summary>Recognizes client timeouts while retaining explicit cancellation behavior.</summary>
    /// <param name="exception">The cancellation received from the client.</param>
    /// <returns>Whether the cancellation represents a timeout.</returns>
    internal static bool IsTimeout(OperationCanceledException exception)
        => exception.InnerException is TimeoutException;

    /// <summary>Resolves a supplied problem code or a safe transport-status fallback.</summary>
    /// <param name="exception">The failure received at the typed-client boundary.</param>
    /// <returns>The stable catalog code for the failure.</returns>
    internal static string Resolve(Exception exception)
        => exception switch
        {
            HexalithChatBotApiException<ProblemDetails> { Result.Code: { } code } when !string.IsNullOrWhiteSpace(code) => AssociationReviewFailureCatalog.CatalogCode(code),
            HexalithChatBotApiException api => api.StatusCode switch
            {
                401 => ChatBotMessageCodes.AuthenticationDenied,
                403 or 404 => ChatBotMessageCodes.AuthorizationDenied,
                400 or 422 => ChatBotMessageCodes.CommandContractInvalid,
                409 => ChatBotMessageCodes.GenericDenial,
                408 or 429 or >= 500 => ChatBotMessageCodes.DependencyDegraded,
                _ => ChatBotMessageCodes.GenericDenial,
            },
            HttpRequestException or TimeoutException => ChatBotMessageCodes.DependencyDegraded,
            OperationCanceledException cancellation when IsTimeout(cancellation) => ChatBotMessageCodes.DependencyDegraded,
            _ => ChatBotMessageCodes.GenericDenial,
        };
}
