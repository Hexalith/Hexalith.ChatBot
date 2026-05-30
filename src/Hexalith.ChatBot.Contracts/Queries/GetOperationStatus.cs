using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record GetOperationStatus(string OperationId, string CorrelationId)
{
    public static bool TryCreate(string? operationId, string? correlationId, out GetOperationStatus? query)
    {
        query = null;
        if (!ChatBotIdentity.IsValidUlid(operationId) ||
            !ChatBotCorrelationId.TryParse(correlationId, out ChatBotCorrelationId parsedCorrelationId))
        {
            return false;
        }

        query = new GetOperationStatus(operationId!, parsedCorrelationId.Value);
        return true;
    }
}
