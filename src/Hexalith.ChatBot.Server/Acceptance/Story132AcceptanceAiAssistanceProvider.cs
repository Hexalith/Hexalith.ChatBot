using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Adapters.AiProvider;

namespace Hexalith.ChatBot.Server.Acceptance;

/// <summary>
/// Acceptance-only provider that holds a real persisted execution open until the lifecycle coordinator cancels it.
/// </summary>
internal sealed class Story132AcceptanceAiAssistanceProvider : IAiAssistanceProvider
{
    public async ValueTask<LowRiskAiAssistanceExecutionRecord> ExecuteAsync(
        AiAssistanceProviderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        throw new InvalidOperationException("The Story 13.2 acceptance provider may complete only by cancellation.");
    }
}
