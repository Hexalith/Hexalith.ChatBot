using System.Diagnostics;

using OpenTelemetry;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Simulates an exporter stuck waiting for a collector.</summary>
internal sealed class BlockingUiTelemetryExporter : BaseExporter<Activity>
{
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes when the exporter has received a batch and blocked.</summary>
    public Task Entered => _entered.Task;

    /// <summary>Allows the exporter to finish after the host shutdown assertion.</summary>
    public void Release() => _released.TrySetResult();

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<Activity> batch)
    {
        _entered.TrySetResult();
        _released.Task.GetAwaiter().GetResult();
        return ExportResult.Success;
    }
}
