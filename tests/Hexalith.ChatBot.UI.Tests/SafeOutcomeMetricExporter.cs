using Hexalith.ChatBot.Contracts.Messages;

using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Captures exported fallback measurements without retaining any caller input.</summary>
internal sealed class SafeOutcomeMetricExporter : BaseExporter<Metric>
{
    /// <summary>Gets the latest cumulative fallback count exported by the host.</summary>
    public long Count { get; private set; }

    /// <summary>Gets whether any exported fallback point includes tags.</summary>
    public bool HasTags { get; private set; }

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<Metric> batch)
    {
        foreach (Metric metric in batch)
        {
            if (metric.Name != ChatBotMessageCatalog.FallbackCounterName) continue;
            foreach (ref readonly MetricPoint point in metric.GetMetricPoints())
            {
                Count = point.GetSumLong();
                HasTags |= point.Tags.Count != 0;
            }
        }
        return ExportResult.Success;
    }
}
