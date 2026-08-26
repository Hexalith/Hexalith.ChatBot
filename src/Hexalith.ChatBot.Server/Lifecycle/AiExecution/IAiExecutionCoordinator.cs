using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.Conversations;

namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal interface IAiExecutionCoordinator
{
    ValueTask RecordStartedAsync(
        string tenantId,
        string conversationId,
        long sourceVersion,
        LowRiskAiAssistanceExecutionStarted started,
        CancellationToken cancellationToken);

    ValueTask RecordCancellationRequestedAsync(
        AiResponseGenerationCancellationRequested request,
        CancellationToken cancellationToken);
}
