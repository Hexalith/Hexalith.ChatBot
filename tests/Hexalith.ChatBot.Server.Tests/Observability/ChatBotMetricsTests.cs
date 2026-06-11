using System.Diagnostics.Metrics;

using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.ServiceDefaults;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Observability;

/// <summary>
/// Story 8.2 acceptance coverage for the operational metrics seam: instrument registration, bounded dimensions,
/// histogram/counter recording, the audit-projection-lag gauge, non-blocking emission, and the gap-detection
/// meta-counter. Observed deterministically through the BCL <see cref="MeterListener"/> (no exporter required).
/// The whole suite lives in one class so the shared <c>Hexalith.ChatBot</c> meter name is exercised sequentially.
/// </summary>
public sealed class ChatBotMetricsTests
{
    private static readonly string[] AllowedMeasurementTagKeys =
        [ChatBotMetrics.TenantTagName, ChatBotMetrics.OperationClassTagName];

    private static readonly string[] AllowedMetaCounterTagKeys =
        [ChatBotMetrics.OperationClassTagName, ChatBotMetrics.ReasonTagName];

    private static readonly string[] AllowedWorkflowMeasurementTagKeys =
        [ChatBotMetrics.TenantTagName, ChatBotMetrics.OperationClassTagName, ChatBotMetrics.StatusTagName, ChatBotMetrics.ReasonTagName];

    [Fact]
    public void AllOperationalInstrumentsPlusGapCounterAreRegisteredOnTheChatBotMeter()
    {
        using ChatBotMetrics metrics = new(new EmptyLagSource());
        using MetricCapture capture = new();

        capture.PublishedInstrumentNames.ShouldBe(
            new[]
            {
                ChatBotMetrics.IngestionLatencyInstrumentName,
                ChatBotMetrics.AssociationLatencyInstrumentName,
                ChatBotMetrics.ApprovalLatencyInstrumentName,
                ChatBotMetrics.CommandExecutionLatencyInstrumentName,
                ChatBotMetrics.RetryExhaustedInstrumentName,
                ChatBotMetrics.DuplicateSuppressedInstrumentName,
                ChatBotMetrics.WorkflowLifecycleInstrumentName,
                ChatBotMetrics.AuditProjectionLagInstrumentName,
                ChatBotMetrics.AuditCompletenessInstrumentName,
                ChatBotMetrics.EmissionFailuresInstrumentName,
            },
            ignoreOrder: true);
    }

    [Fact]
    public void WorkflowLifecycleCounterCarriesOnlyBoundedWorkflowDimensions()
    {
        using ChatBotMetrics metrics = new(new EmptyLagSource());
        using MetricCapture capture = new();

        metrics.RecordWorkflowLifecycle("tenant-workflow", "started", "none");

        CapturedMeasurement measurement = capture.Single(ChatBotMetrics.WorkflowLifecycleInstrumentName);
        measurement.Value.ShouldBe(1);
        measurement.Tags.Keys.ShouldBe(AllowedWorkflowMeasurementTagKeys, ignoreOrder: true);
        measurement.Tags[ChatBotMetrics.TenantTagName].ShouldBe("tenant-workflow");
        measurement.Tags[ChatBotMetrics.OperationClassTagName].ShouldBe(ChatBotOperationClasses.Workflow);
        measurement.Tags[ChatBotMetrics.StatusTagName].ShouldBe("started");
        measurement.Tags[ChatBotMetrics.ReasonTagName].ShouldBe("none");
    }

    [Theory]
    [InlineData(ChatBotMetrics.IngestionLatencyInstrumentName, ChatBotOperationClasses.MessageIntake)]
    [InlineData(ChatBotMetrics.AssociationLatencyInstrumentName, ChatBotOperationClasses.Association)]
    [InlineData(ChatBotMetrics.ApprovalLatencyInstrumentName, ChatBotOperationClasses.Approval)]
    [InlineData(ChatBotMetrics.CommandExecutionLatencyInstrumentName, ChatBotOperationClasses.CommandExecution)]
    public void LatencyHistogramsRecordDurationsWithBoundedDimensions(string instrumentName, string operationClass)
    {
        using ChatBotMetrics metrics = new(new EmptyLagSource());
        using MetricCapture capture = new();

        switch (instrumentName)
        {
            case ChatBotMetrics.IngestionLatencyInstrumentName: metrics.RecordIngestionLatency("tenant-alpha", 12.5); break;
            case ChatBotMetrics.AssociationLatencyInstrumentName: metrics.RecordAssociationLatency("tenant-alpha", 12.5); break;
            case ChatBotMetrics.ApprovalLatencyInstrumentName: metrics.RecordApprovalLatency("tenant-alpha", 12.5); break;
            default: metrics.RecordCommandExecutionLatency("tenant-alpha", 12.5); break;
        }

        CapturedMeasurement measurement = capture.Single(instrumentName);
        measurement.Value.ShouldBe(12.5);
        measurement.Tags[ChatBotMetrics.TenantTagName].ShouldBe("tenant-alpha");
        measurement.Tags[ChatBotMetrics.OperationClassTagName].ShouldBe(operationClass);
    }

    [Fact]
    public void RetryExhaustedAndDuplicateSuppressedCountersFire()
    {
        using ChatBotMetrics metrics = new(new EmptyLagSource());
        using MetricCapture capture = new();

        metrics.RecordRetryExhausted("tenant-beta");
        metrics.RecordDuplicateSuppressed("tenant-beta");

        CapturedMeasurement retry = capture.Single(ChatBotMetrics.RetryExhaustedInstrumentName);
        retry.Value.ShouldBe(1);
        retry.Tags[ChatBotMetrics.TenantTagName].ShouldBe("tenant-beta");
        retry.Tags[ChatBotMetrics.OperationClassTagName].ShouldBe(ChatBotOperationClasses.Retry);

        CapturedMeasurement duplicate = capture.Single(ChatBotMetrics.DuplicateSuppressedInstrumentName);
        duplicate.Value.ShouldBe(1);
        duplicate.Tags[ChatBotMetrics.OperationClassTagName].ShouldBe(ChatBotOperationClasses.DuplicateHandling);
    }

    [Fact]
    public void EveryOperationalMeasurementCarriesOnlyTenantAndOperationClassDimensions()
    {
        using ChatBotMetrics metrics = new(new EmptyLagSource());
        using MetricCapture capture = new();

        metrics.RecordIngestionLatency("tenant-a", 1);
        metrics.RecordAssociationLatency("tenant-a", 1);
        metrics.RecordApprovalLatency("tenant-a", 1);
        metrics.RecordCommandExecutionLatency("tenant-a", 1);
        metrics.RecordRetryExhausted("tenant-a");
        metrics.RecordDuplicateSuppressed("tenant-a");

        IReadOnlyList<CapturedMeasurement> measurements = capture.Snapshot();
        measurements.Count.ShouldBe(6);
        foreach (CapturedMeasurement measurement in measurements)
        {
            // Dimension-name ban (mirrors OpenTelemetryShouldNotCaptureRequestOrResponseBodies): no correlation/
            // operation/command/project id, payload, evidence, token, secret, or any other key may ever appear.
            measurement.Tags.Keys.ShouldBe(AllowedMeasurementTagKeys, ignoreOrder: true);
        }
    }

    [Fact]
    public void AuditProjectionLagGaugeReflectsEvaluatorOutput()
    {
        // committed (105) is 5 events ahead of projected (100); a fresh snapshot → coarse LagEvents = 5.
        StubLagSource source = new(
            [new AuditProjectionLagReading("tenant-lag", LastProjectedPosition: 100, LatestCommittedPosition: 105, DateTimeOffset.UtcNow)]);
        using ChatBotMetrics metrics = new(source);
        using MetricCapture capture = new();

        capture.RecordObservable();

        CapturedMeasurement gauge = capture.Single(ChatBotMetrics.AuditProjectionLagInstrumentName);
        gauge.Value.ShouldBe(5);
        gauge.Tags[ChatBotMetrics.TenantTagName].ShouldBe("tenant-lag");
        gauge.Tags[ChatBotMetrics.OperationClassTagName].ShouldBe(ChatBotOperationClasses.AuditProjectionLag);
    }

    [Fact]
    public void AuditProjectionLagGaugeEmitsNoMeasurementWhenPositionsAreUnavailable()
    {
        // Fail-safe doctrine: null positions → evaluator Unknown / no LagEvents → no fabricated 0 is published.
        StubLagSource source = new(
            [new AuditProjectionLagReading("tenant-lag", LastProjectedPosition: null, LatestCommittedPosition: null, DateTimeOffset.UtcNow)]);
        using ChatBotMetrics metrics = new(source);
        using MetricCapture capture = new();

        capture.RecordObservable();

        capture.Snapshot().ShouldNotContain(m => m.InstrumentName == ChatBotMetrics.AuditProjectionLagInstrumentName);
    }

    [Fact]
    public void ForcedEmissionFailureIsSwallowedAndIncrementsTheGapCounter()
    {
        using ChatBotMetrics metrics = new(new EmptyLagSource());

        // A listener that throws when the ingestion histogram records — simulates a failing exporter/listener.
        using MeterListener thrower = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == Extensions.ChatBotMeterName &&
                    instrument.Name == ChatBotMetrics.IngestionLatencyInstrumentName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        thrower.SetMeasurementEventCallback<double>((_, _, _, _) => throw new InvalidOperationException("exporter down"));
        thrower.Start();

        using MetricCapture gapCapture = new(instrument => instrument.Name == ChatBotMetrics.EmissionFailuresInstrumentName);

        // Must not throw into the operation path even though the underlying record throws.
        Should.NotThrow(() => metrics.RecordIngestionLatency("tenant-gamma", 9.0));

        CapturedMeasurement failure = gapCapture.Single(ChatBotMetrics.EmissionFailuresInstrumentName);
        failure.Value.ShouldBe(1);
        failure.Tags[ChatBotMetrics.OperationClassTagName].ShouldBe(ChatBotOperationClasses.MessageIntake);
        failure.Tags[ChatBotMetrics.ReasonTagName].ShouldBe("emit-threw");
    }

    [Fact]
    public void MissingBoundTenantIsCountedAsAGapAndSkipsTheMeasurement()
    {
        using ChatBotMetrics metrics = new(new EmptyLagSource());
        using MetricCapture capture = new();

        metrics.RecordIngestionLatency("   ", 4.0);

        capture.Snapshot().ShouldNotContain(m => m.InstrumentName == ChatBotMetrics.IngestionLatencyInstrumentName);
        CapturedMeasurement failure = capture.Single(ChatBotMetrics.EmissionFailuresInstrumentName);
        failure.Tags[ChatBotMetrics.OperationClassTagName].ShouldBe(ChatBotOperationClasses.MessageIntake);
        failure.Tags[ChatBotMetrics.ReasonTagName].ShouldBe("tenant-unavailable");
    }

    [Fact]
    public void GapDetectionMetaCounterCarriesOnlyOperationClassAndReasonAndNeverLeaksTenant()
    {
        // AC4/AC6 dimension-name ban for the gap signal itself: the meta-counter must NOT carry the tenant tag (the
        // failure being counted is often a missing/blank bound tenant) — only operation-class + a stable reason.
        using ChatBotMetrics metrics = new(new EmptyLagSource());
        using MetricCapture capture = new();

        // A blank bound tenant is a gap: it increments the meta-counter with reason `tenant-unavailable`.
        metrics.RecordIngestionLatency(" ", 3.0);

        CapturedMeasurement failure = capture.Single(ChatBotMetrics.EmissionFailuresInstrumentName);
        failure.Tags.Keys.ShouldBe(AllowedMetaCounterTagKeys, ignoreOrder: true);
        failure.Tags.ShouldNotContainKey(ChatBotMetrics.TenantTagName);
    }

    [Fact]
    public void AuditProjectionLagGaugeMeasurementCarriesOnlyTenantAndOperationClassDimensions()
    {
        // AC4/AC9 dimension-name ban extended to the observable gauge (excluded from the push-instrument ban test):
        // the gauge measurement must expose exactly the bounded tenant + operation-class keys, nothing else.
        StubLagSource source = new(
            [new AuditProjectionLagReading("tenant-lag", LastProjectedPosition: 100, LatestCommittedPosition: 105, DateTimeOffset.UtcNow)]);
        using ChatBotMetrics metrics = new(source);
        using MetricCapture capture = new();

        capture.RecordObservable();

        CapturedMeasurement gauge = capture.Single(ChatBotMetrics.AuditProjectionLagInstrumentName);
        gauge.Tags.Keys.ShouldBe(AllowedMeasurementTagKeys, ignoreOrder: true);
    }

    [Fact]
    public void AuditCompletenessGaugeReflectsMeasuredFractionWithTenantTagOnly()
    {
        // Story 9.2 (NFR50a): the gauge surfaces the per-tenant reconstructable fraction as the measurement VALUE,
        // carrying only the low-cardinality tenant tag (the fraction is never a dimension).
        StubCompletenessSource source = new([new AuditCompletenessReading("tenant-c", IsMeasurable: true, Fraction: 0.994)]);
        using ChatBotMetrics metrics = new(new EmptyLagSource(), auditCompletenessSource: source);
        using MetricCapture capture = new();

        capture.RecordObservable();

        CapturedMeasurement gauge = capture.Single(ChatBotMetrics.AuditCompletenessInstrumentName);
        gauge.Value.ShouldBe(0.994);
        gauge.Tags.Keys.ShouldBe(new[] { ChatBotMetrics.TenantTagName });
        gauge.Tags[ChatBotMetrics.TenantTagName].ShouldBe("tenant-c");
    }

    [Fact]
    public void AuditCompletenessGaugeEmitsNoMeasurementWhenUnmeasurable()
    {
        // Fail-safe doctrine: an unmeasurable tenant publishes NO measurement — never a fabricated 1.0.
        StubCompletenessSource source = new([new AuditCompletenessReading("tenant-c", IsMeasurable: false, Fraction: 0.0)]);
        using ChatBotMetrics metrics = new(new EmptyLagSource(), auditCompletenessSource: source);
        using MetricCapture capture = new();

        capture.RecordObservable();

        capture.Snapshot().ShouldNotContain(m => m.InstrumentName == ChatBotMetrics.AuditCompletenessInstrumentName);
    }

    [Fact]
    public void AuditCompletenessSourceFailureIsSwallowedAndCountedAsAGap()
    {
        using ChatBotMetrics metrics = new(new EmptyLagSource(), auditCompletenessSource: new ThrowingCompletenessSource());
        using MetricCapture capture = new();

        Should.NotThrow(capture.RecordObservable);

        capture.Snapshot().ShouldNotContain(m => m.InstrumentName == ChatBotMetrics.AuditCompletenessInstrumentName);
        CapturedMeasurement failure = capture.Single(ChatBotMetrics.EmissionFailuresInstrumentName);
        failure.Tags[ChatBotMetrics.OperationClassTagName].ShouldBe(ChatBotOperationClasses.AuditCompleteness);
        failure.Tags[ChatBotMetrics.ReasonTagName].ShouldBe("completeness-source-threw");
    }

    [Fact]
    public void AuditLagSourceFailureIsSwallowedAndCountedAsAGap()
    {
        using ChatBotMetrics metrics = new(new ThrowingLagSource());
        using MetricCapture capture = new();

        Should.NotThrow(capture.RecordObservable);

        capture.Snapshot().ShouldNotContain(m => m.InstrumentName == ChatBotMetrics.AuditProjectionLagInstrumentName);
        CapturedMeasurement failure = capture.Single(ChatBotMetrics.EmissionFailuresInstrumentName);
        failure.Tags[ChatBotMetrics.OperationClassTagName].ShouldBe(ChatBotOperationClasses.AuditProjectionLag);
        failure.Tags[ChatBotMetrics.ReasonTagName].ShouldBe("lag-source-threw");
    }

    [Fact]
    public void RecordRetryExhaustedSignalsRetrySourceAfterCounter()
    {
        // Story 8.4 (AC2): the retry-exhaustion alert source is signalled with the tenant so the wiring coordinator's
        // ReadAndClear picks it up — the integration that turns the OTel counter into a fired alert.
        RecordingRetrySource retrySource = new();
        using ChatBotMetrics metrics = new(new EmptyLagSource(), retrySource);
        using MetricCapture capture = new();

        metrics.RecordRetryExhausted("tenant-beta");

        capture.Single(ChatBotMetrics.RetryExhaustedInstrumentName).Value.ShouldBe(1);
        retrySource.Signalled.ShouldHaveSingleItem().ShouldBe("tenant-beta");
    }

    [Fact]
    public void RetrySourceSignalExceptionIsSwallowedAndCountedAsAGap()
    {
        // Story 8.4 (AC2/NFR43 non-invasive): a throwing alert source must never surface on the metric-recording path;
        // it is swallowed and gap-counted (same exception-isolation posture as the OTel emissions).
        using ChatBotMetrics metrics = new(new EmptyLagSource(), new ThrowingRetrySource());
        using MetricCapture capture = new();

        Should.NotThrow(() => metrics.RecordRetryExhausted("tenant-beta"));

        // The counter still fired; the swallowed signal failure is recorded as a gap.
        capture.Single(ChatBotMetrics.RetryExhaustedInstrumentName).Value.ShouldBe(1);
        CapturedMeasurement failure = capture.Single(ChatBotMetrics.EmissionFailuresInstrumentName);
        failure.Tags[ChatBotMetrics.OperationClassTagName].ShouldBe(ChatBotOperationClasses.Retry);
        failure.Tags[ChatBotMetrics.ReasonTagName].ShouldBe("retry-alert-signal-threw");
    }

    [Fact]
    public void RecordRetryExhaustedWithBlankTenantDoesNotSignal()
    {
        RecordingRetrySource retrySource = new();
        using ChatBotMetrics metrics = new(new EmptyLagSource(), retrySource);

        metrics.RecordRetryExhausted("   ");

        retrySource.Signalled.ShouldBeEmpty();
    }

    private sealed class RecordingRetrySource : IRetryExhaustionAlertSource
    {
        public List<string> Signalled { get; } = [];

        public void Signal(string tenantId) => Signalled.Add(tenantId);

        public bool ReadAndClear(string tenantId) => Signalled.Remove(tenantId);
    }

    private sealed class ThrowingRetrySource : IRetryExhaustionAlertSource
    {
        public void Signal(string tenantId) => throw new InvalidOperationException("retry alert source down");

        public bool ReadAndClear(string tenantId) => false;
    }

    private sealed class EmptyLagSource : IAuditProjectionLagSource
    {
        public IReadOnlyList<AuditProjectionLagReading> ReadCurrent() => [];
    }

    private sealed class StubLagSource(IReadOnlyList<AuditProjectionLagReading> readings) : IAuditProjectionLagSource
    {
        public IReadOnlyList<AuditProjectionLagReading> ReadCurrent() => readings;
    }

    private sealed class ThrowingLagSource : IAuditProjectionLagSource
    {
        public IReadOnlyList<AuditProjectionLagReading> ReadCurrent() => throw new InvalidOperationException("checkpoint source down");
    }

    private sealed class StubCompletenessSource(IReadOnlyList<AuditCompletenessReading> readings) : IAuditCompletenessSource
    {
        public IReadOnlyList<AuditCompletenessReading> ReadCurrent() => readings;
    }

    private sealed class ThrowingCompletenessSource : IAuditCompletenessSource
    {
        public IReadOnlyList<AuditCompletenessReading> ReadCurrent() => throw new InvalidOperationException("completeness source down");
    }

    private sealed record CapturedMeasurement(string InstrumentName, double Value, IReadOnlyDictionary<string, object?> Tags);

    /// <summary>Captures measurements from the ChatBot meter only, using the BCL MeterListener.</summary>
    private sealed class MetricCapture : IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly List<CapturedMeasurement> _measurements = [];
        private readonly List<string> _published = [];
        private readonly object _sync = new();

        public MetricCapture(Func<Instrument, bool>? enable = null)
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name != Extensions.ChatBotMeterName)
                {
                    return;
                }

                lock (_sync)
                {
                    _published.Add(instrument.Name);
                }

                if (enable is null || enable(instrument))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument.Name, value, tags));
            _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument.Name, value, tags));
            _listener.Start();
        }

        public IReadOnlyList<string> PublishedInstrumentNames
        {
            get
            {
                lock (_sync)
                {
                    return _published.ToArray();
                }
            }
        }

        public void RecordObservable() => _listener.RecordObservableInstruments();

        public IReadOnlyList<CapturedMeasurement> Snapshot()
        {
            lock (_sync)
            {
                return _measurements.ToArray();
            }
        }

        public CapturedMeasurement Single(string instrumentName)
            => Snapshot().Where(m => m.InstrumentName == instrumentName).ShouldHaveSingleItem();

        public void Dispose() => _listener.Dispose();

        private void Record(string instrumentName, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            Dictionary<string, object?> copied = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                copied[tag.Key] = tag.Value;
            }

            lock (_sync)
            {
                _measurements.Add(new CapturedMeasurement(instrumentName, value, copied));
            }
        }
    }
}
