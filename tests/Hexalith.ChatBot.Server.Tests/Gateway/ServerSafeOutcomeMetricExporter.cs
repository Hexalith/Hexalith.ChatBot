using Hexalith.ChatBot.Contracts.Messages;

using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Captures the server host's exported safe-message fallback measurement.</summary>
internal sealed class ServerSafeOutcomeMetricExporter : BaseExporter<Metric>
{
    /// <summary>Gets the latest cumulative fallback count.</summary>
    public long Count { get; private set; }

    /// <summary>Gets whether a fallback measurement contained a tag.</summary>
    public bool HasTags { get; private set; }

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<Metric> batch)
    {
        foreach (Metric metric in batch)
        {
            if (metric.Name != ChatBotMessageCatalog.FallbackCounterName)
            {
                continue;
            }

            foreach (ref readonly MetricPoint point in metric.GetMetricPoints())
            {
                Count = point.GetSumLong();
                HasTags |= point.Tags.Count != 0;
            }
        }

        return ExportResult.Success;
    }
}
