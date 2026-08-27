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

    ValueTask RecordTerminalObservedAsync(
        string tenantId,
        string stateOwnerAggregateId,
        string projectId,
        string responseId,
        string generationId,
        CancellationToken cancellationToken);

    ValueTask RecordCancellationFailedAsync(
        AiResponseGenerationCancellationFailed failure,
        CancellationToken cancellationToken);
}
