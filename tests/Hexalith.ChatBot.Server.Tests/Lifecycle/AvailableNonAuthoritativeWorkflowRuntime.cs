using Hexalith.ChatBot.Server.Lifecycle.Workflows;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

/// <summary>Models an available legacy runtime that has no authoritative progress reader.</summary>
internal sealed class AvailableNonAuthoritativeWorkflowRuntime : ICorrectionPropagationWorkflowRuntime
{
    public bool IsAvailable => true;
    public ValueTask ScheduleAsync(CorrectionPropagationRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    public ValueTask<CorrectionPropagationWorkflowRuntimeStatus> CheckAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
}
