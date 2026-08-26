using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.Conversations;

namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal interface IAiExecutionWorkStore
{
    ValueTask UpsertStartedAsync(AiExecutionWorkItem item, CancellationToken cancellationToken);

    ValueTask MarkCancellationRequestedAsync(
        AiResponseGenerationCancellationRequested request,
        CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<AiExecutionWorkItem>> ListRunnableAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken);

    ValueTask<AiExecutionWorkItem?> TryClaimAsync(
        string key,
        string owner,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    ValueTask<AiExecutionWorkItem?> TryRenewLeaseAsync(
        string key,
        string owner,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    ValueTask MarkCompletionPendingAsync(
        string key,
        string owner,
        LowRiskAiAssistanceExecutionRecord record,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    ValueTask MarkTerminalAsync(
        string key,
        string owner,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    ValueTask ReleaseAsync(
        string key,
        string owner,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
