using Hexalith.ChatBot.Server.Lifecycle.Workflows;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

/// <summary>Retains post-effect workflow startup for continuation assertions.</summary>
internal sealed class RecordingAuthorityIngestionCoordinator : IIngestionBindingCoordinator
{
    /// <inheritdoc/>
    public bool IsReady => true;
    /// <summary>The workflow requests started after the accepted effect.</summary>
    public List<IngestionBindingRequest> Requests { get; } = [];
    /// <inheritdoc/>
    public ValueTask StartAsync(IngestionBindingRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return ValueTask.CompletedTask;
    }
}
