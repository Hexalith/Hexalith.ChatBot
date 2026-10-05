using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Server.Gateway;

internal static class CommandGatewayHttpResults
{
    public static IResult ToHttpResult(ChatBotGatewayResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Accepted is { } accepted)
        {
            return Results.Text(Newtonsoft.Json.JsonConvert.SerializeObject(accepted), "application/json", statusCode: StatusCodes.Status202Accepted);
        }

        ProblemDetails problem = result.Problem ?? throw new InvalidOperationException("Denied gateway results must include Problem Details.");
        problem.SchemaVersion = ChatBotMessageCatalogVersion.Current;
        problem.Details ??= new ProblemDetailsDetails { Visibility = ProblemDetailsDetailsVisibility.Metadata_only };
        return Results.Text(Newtonsoft.Json.JsonConvert.SerializeObject(problem), "application/problem+json", statusCode: problem.Status);
    }
}
