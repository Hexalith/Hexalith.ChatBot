using System.Text.Json.Serialization;

using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway;

internal static class CommandGatewayHttpResults
{
    public static IResult ToHttpResult(ChatBotGatewayResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Accepted is not null)
        {
            return Results.Json(ToWireAccepted(result.Accepted), statusCode: StatusCodes.Status202Accepted);
        }

        ProblemDetails problem = result.Problem ?? throw new InvalidOperationException("Denied gateway results must include Problem Details.");
        return Results.Json(ToWireProblem(problem), statusCode: problem.Status);
    }

    private static ChatBotCommandSubmissionResponseWireModel ToWireAccepted(CommandSubmissionResponse response)
        => new(
            response.CommandId,
            response.CorrelationId,
            response.TaskId,
            Lifecycle(response.LifecycleState),
            response.AcceptedAt);

    private static ChatBotProblemDetailsWireModel ToWireProblem(ProblemDetails problem)
        => new(
            problem.Type,
            problem.Title,
            problem.Status,
            problem.Detail,
            problem.Instance,
            Category(problem.Category),
            problem.Code,
            problem.Message,
            problem.CorrelationId,
            problem.TaskId,
            problem.Retryable,
            ClientAction(problem.ClientAction),
            new ChatBotProblemDetailsDetailsWireModel(Visibility(problem.Details?.Visibility)));

    private static string Category(ProblemDetailsCategory category)
        => category switch
        {
            ProblemDetailsCategory.Authentication_failure => "authentication_failure",
            ProblemDetailsCategory.Authorization_denied => "authorization_denied",
            ProblemDetailsCategory.Validation_error => "validation_error",
            ProblemDetailsCategory.Conflict => "conflict",
            _ => "internal_error",
        };

    private static string ClientAction(ProblemDetailsClientAction action)
        => action switch
        {
            ProblemDetailsClientAction.Authenticate => "authenticate",
            ProblemDetailsClientAction.CorrectRequest => "correct-request",
            ProblemDetailsClientAction.RetryLater => "retry-later",
            ProblemDetailsClientAction.RequestAccess => "request-access",
            ProblemDetailsClientAction.Escalate => "escalate",
            ProblemDetailsClientAction.Dismiss => "dismiss",
            _ => "none",
        };

    private static string Visibility(ProblemDetailsDetailsVisibility? visibility)
        => visibility switch
        {
            ProblemDetailsDetailsVisibility.Metadata_only => "metadata_only",
            _ => "metadata_only",
        };

    private static string Lifecycle(LifecycleState state)
        => state switch
        {
            LifecycleState.Received => "Received",
            LifecycleState.Proposed => "Proposed",
            LifecycleState.Associated => "Associated",
            LifecycleState.Rejected => "Rejected",
            LifecycleState.Deferred => "Deferred",
            LifecycleState.NeedsReview => "NeedsReview",
            LifecycleState.Failed => "Failed",
            LifecycleState.Skipped => "Skipped",
            LifecycleState.Corrected => "Corrected",
            LifecycleState.Correcting => "Correcting",
            LifecycleState.CorrectionDelayed => "Correction-delayed",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported lifecycle state."),
        };

    private sealed record ChatBotCommandSubmissionResponseWireModel(
        string CommandId,
        string CorrelationId,
        string? TaskId,
        string LifecycleState,
        DateTimeOffset AcceptedAt);

    private sealed record ChatBotProblemDetailsWireModel(
        string Type,
        string Title,
        int Status,
        string? Detail,
        string? Instance,
        string Category,
        string Code,
        string Message,
        string CorrelationId,
        string? TaskId,
        bool Retryable,
        string ClientAction,
        ChatBotProblemDetailsDetailsWireModel Details);

    private sealed record ChatBotProblemDetailsDetailsWireModel(
        [property: JsonPropertyName("visibility")] string Visibility);
}
