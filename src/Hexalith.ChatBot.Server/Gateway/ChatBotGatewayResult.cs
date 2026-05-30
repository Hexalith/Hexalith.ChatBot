using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed record ChatBotGatewayResult(
    CommandSubmissionResponse? Accepted,
    ProblemDetails? Problem,
    bool AuditReconciliationRequired = false)
{
    public bool IsAccepted => Accepted is not null;

    public static ChatBotGatewayResult AcceptedResult(CommandSubmissionResponse response, bool auditReconciliationRequired = false)
        => new(response, null, auditReconciliationRequired);

    public static ChatBotGatewayResult Denied(ProblemDetails problem)
        => new(null, problem);
}
