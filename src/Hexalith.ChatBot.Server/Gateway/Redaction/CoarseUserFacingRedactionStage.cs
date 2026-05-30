using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway.Redaction;

internal sealed class CoarseUserFacingRedactionStage : IUserFacingRedactionStage
{
    public const string MetadataOnlyDecision = "metadata_only";

    public ProblemDetails Apply(ProblemDetails problem)
    {
        ArgumentNullException.ThrowIfNull(problem);

        return new ProblemDetails
        {
            Type = problem.Type,
            Title = problem.Title,
            Status = problem.Status,
            Detail = null,
            Instance = null,
            Category = problem.Category,
            Code = problem.Code,
            Message = problem.Message,
            CorrelationId = problem.CorrelationId,
            TaskId = problem.TaskId,
            Retryable = problem.Retryable,
            ClientAction = problem.ClientAction,
            Details = new ProblemDetailsDetails
            {
                Visibility = ProblemDetailsDetailsVisibility.Metadata_only,
            },
        };
    }
}
