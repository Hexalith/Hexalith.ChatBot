using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed record ChatBotGatewayResult(CommandSubmissionResponse? Accepted, ProblemDetails? Problem)
{
    public bool IsAccepted => Accepted is not null;

    public static ChatBotGatewayResult AcceptedResult(CommandSubmissionResponse response)
        => new(response, null);

    public static ChatBotGatewayResult Denied(ProblemDetails problem)
        => new(null, problem);
}
