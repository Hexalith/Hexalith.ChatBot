using System.Diagnostics.Metrics;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Status;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Measures fallback resolutions at the server's public boundaries.</summary>
public sealed class SafeOutcomeFallbackMetricTests
{
    /// <summary>Checks that boundary fallbacks count only the initiating call and contain no input tags.</summary>
    /// <param name="boundary">The public boundary under test.</param>
    [Theory]
    [InlineData("redaction")]
    [InlineData("status")]
    public void PublicBoundaryShouldCountFallbackWithoutInputTags(string boundary)
    {
        long count = 0;
        int initiatingThread = Environment.CurrentManagedThreadId;
        using MeterListener listener = new();
        listener.InstrumentPublished = (instrument, observer) =>
        {
            if (instrument.Meter.Name == ChatBotMessageCatalog.FallbackMeterName &&
                instrument.Name == ChatBotMessageCatalog.FallbackCounterName)
            {
                observer.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            if (Environment.CurrentManagedThreadId != initiatingThread) return;
            count += value;
            tags.Length.ShouldBe(0);
        });
        listener.Start();
        if (boundary == "redaction")
        {
            new CoarseUserFacingRedactionStage().Apply(new ProblemDetails
            {
                Code = "restricted-boundary-secret",
            }).Code.ShouldBe(ChatBotMessageCodes.GenericDenial);
        }
        else
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            OperationStatusRecord record = OperationStatusRecord.Accepted("tenant", new CommandSubmissionResponse
            {
                CommandId = "command", OperationId = "operation", CorrelationId = "correlation", AcceptedAt = now,
            }, false, now) with { ReasonCode = "restricted-boundary-secret" };
            OperationStatusHttpResults.ToJsonElement(record, now).GetRawText().ShouldNotContain("restricted-boundary-secret");
        }
        count.ShouldBe(1);
    }
}
