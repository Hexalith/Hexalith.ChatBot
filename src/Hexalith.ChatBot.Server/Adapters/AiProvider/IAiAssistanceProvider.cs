using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Adapters.AiProvider;

internal interface IAiAssistanceProvider
{
    ValueTask<LowRiskAiAssistanceExecutionRecord> ExecuteAsync(
        AiAssistanceProviderRequest request,
        CancellationToken cancellationToken);
}
