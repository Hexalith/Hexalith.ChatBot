using System.Diagnostics.Metrics;

using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.ServiceDefaults;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// The single dedicated ChatBot OpenTelemetry meter and the FR94 operational instruments (Story 8.2). It owns the
/// always-on <see cref="Meter"/> (named <see cref="Extensions.ChatBotMeterName"/>, registered on the MeterProvider
/// via <c>AddMeter</c>) and records:
/// <list type="bullet">
///   <item>four duration histograms (ingestion / association / approval / command-execution latency, milliseconds) so percentile distributions are derivable (NFR28);</item>
///   <item>two counters (retry exhaustion, duplicate suppression);</item>
///   <item>an observable gauge for the coarse audit-projection lag, derived read-only from <see cref="AuditProjectionLagEvaluator"/>;</item>
///   <item>a gap-detection meta-counter (<c>chatbot.telemetry.emission_failures</c>) that increments whenever an emission is swallowed.</item>
/// </list>
/// Every emission is exception-isolated: a failing instrument/listener/exporter can never throw into the operation
/// path (AC5); the swallowed failure is itself observable through the meta-counter (AC6). The only dimensions ever
/// attached are the bounded, low-cardinality <c>tenant</c> and <c>operation-class</c> tags (plus a stable
/// <c>reason</c> on the meta-counter) — never payloads, ids, correlation, or any high-cardinality/secret value (AC3/AC4).
/// </summary>
internal sealed class ChatBotMetrics : IChatBotMetrics, IDisposable
{
    public const string IngestionLatencyInstrumentName = "chatbot.ingestion.latency";
    public const string AssociationLatencyInstrumentName = "chatbot.association.latency";
    public const string ApprovalLatencyInstrumentName = "chatbot.approval.latency";
    public const string CommandExecutionLatencyInstrumentName = "chatbot.command.execution.latency";
    public const string RetryExhaustedInstrumentName = "chatbot.retry.exhausted";
    public const string DuplicateSuppressedInstrumentName = "chatbot.duplicate.suppressed";
    public const string AuditProjectionLagInstrumentName = "chatbot.audit.projection.lag";
    public const string EmissionFailuresInstrumentName = "chatbot.telemetry.emission_failures";

    public const string TenantTagName = "tenant";
    public const string OperationClassTagName = "operation-class";
    public const string ReasonTagName = "reason";

    private const string LatencyUnit = "ms";
    private const string EventsUnit = "{events}";

    private readonly IAuditProjectionLagSource _auditProjectionLagSource;
    private readonly IRetryExhaustionAlertSource? _retryExhaustionSource;
    private readonly Meter _meter;
    private readonly Histogram<double> _ingestionLatency;
    private readonly Histogram<double> _associationLatency;
    private readonly Histogram<double> _approvalLatency;
    private readonly Histogram<double> _commandExecutionLatency;
    private readonly Counter<long> _retryExhausted;
    private readonly Counter<long> _duplicateSuppressed;
    private readonly Counter<long> _emissionFailures;

    public ChatBotMetrics(
        IAuditProjectionLagSource auditProjectionLagSource,
        IRetryExhaustionAlertSource? retryExhaustionSource = null)
    {
        _auditProjectionLagSource = auditProjectionLagSource ?? throw new ArgumentNullException(nameof(auditProjectionLagSource));
        _retryExhaustionSource = retryExhaustionSource;
        _meter = new Meter(Extensions.ChatBotMeterName);

        _ingestionLatency = _meter.CreateHistogram<double>(IngestionLatencyInstrumentName, LatencyUnit, "Mailbox-intake (ingestion) latency.");
        _associationLatency = _meter.CreateHistogram<double>(AssociationLatencyInstrumentName, LatencyUnit, "Association-scoring latency.");
        _approvalLatency = _meter.CreateHistogram<double>(ApprovalLatencyInstrumentName, LatencyUnit, "Approval-decision latency.");
        _commandExecutionLatency = _meter.CreateHistogram<double>(CommandExecutionLatencyInstrumentName, LatencyUnit, "Command-execution dispatch latency.");
        _retryExhausted = _meter.CreateCounter<long>(RetryExhaustedInstrumentName, EventsUnit, "Workflow items that reached the retry-exhausted terminal state.");
        _duplicateSuppressed = _meter.CreateCounter<long>(DuplicateSuppressedInstrumentName, EventsUnit, "Duplicate provider messages suppressed.");
        _emissionFailures = _meter.CreateCounter<long>(EmissionFailuresInstrumentName, EventsUnit, "Swallowed metric-emission failures (gap-detection signal).");

        // Observable gauge: derive the coarse audit-projection lag read-only at collection time. Emits no
        // measurement when positions are unavailable (fail-safe), and swallows + gap-counts any source failure.
        _ = _meter.CreateObservableGauge(AuditProjectionLagInstrumentName, ObserveAuditProjectionLag, EventsUnit, "Coarse audit-projection lag (events behind), per tenant.");
    }

    public void RecordIngestionLatency(string tenantId, double milliseconds)
        => RecordLatency(_ingestionLatency, ChatBotOperationClasses.MessageIntake, tenantId, milliseconds);

    public void RecordAssociationLatency(string tenantId, double milliseconds)
        => RecordLatency(_associationLatency, ChatBotOperationClasses.Association, tenantId, milliseconds);

    public void RecordApprovalLatency(string tenantId, double milliseconds)
        => RecordLatency(_approvalLatency, ChatBotOperationClasses.Approval, tenantId, milliseconds);

    public void RecordCommandExecutionLatency(string tenantId, double milliseconds)
        => RecordLatency(_commandExecutionLatency, ChatBotOperationClasses.CommandExecution, tenantId, milliseconds);

    public void RecordRetryExhausted(string tenantId)
    {
        RecordCount(_retryExhausted, ChatBotOperationClasses.Retry, tenantId);

        // Story 8.4: signal the in-process retry-exhaustion alert source AFTER the OTel counter increment, preserving
        // the existing ordering invariant. Non-throwing and fire-and-forget: a failing signal can never surface as an
        // exception on the metric-recording path (same exception-isolation posture as the OTel emissions).
        if (_retryExhaustionSource is null || string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        try
        {
            _retryExhaustionSource.Signal(tenantId);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            RecordEmissionFailure(ChatBotOperationClasses.Retry, "retry-alert-signal-threw");
        }
    }

    public void RecordDuplicateSuppressed(string tenantId)
        => RecordCount(_duplicateSuppressed, ChatBotOperationClasses.DuplicateHandling, tenantId);

    public void Dispose() => _meter.Dispose();

    private void RecordLatency(Histogram<double> instrument, string operationClass, string tenantId, double milliseconds)
    {
        if (!TryResolveTenant(tenantId, operationClass, out string tenant))
        {
            return;
        }

        SafeEmit(
            operationClass,
            () => instrument.Record(
                milliseconds,
                new KeyValuePair<string, object?>(TenantTagName, tenant),
                new KeyValuePair<string, object?>(OperationClassTagName, operationClass)));
    }

    private void RecordCount(Counter<long> instrument, string operationClass, string tenantId)
    {
        if (!TryResolveTenant(tenantId, operationClass, out string tenant))
        {
            return;
        }

        SafeEmit(
            operationClass,
            () => instrument.Add(
                1,
                new KeyValuePair<string, object?>(TenantTagName, tenant),
                new KeyValuePair<string, object?>(OperationClassTagName, operationClass)));
    }

    // A missing/blank bound tenant is never fabricated into an identity. It is an emission gap: count it on the
    // meta-counter (so the loss is observable) and skip the measurement rather than tag it with a placeholder id.
    private bool TryResolveTenant(string tenantId, string operationClass, out string tenant)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            RecordEmissionFailure(operationClass, "tenant-unavailable");
            tenant = string.Empty;
            return false;
        }

        tenant = tenantId;
        return true;
    }

    private void SafeEmit(string operationClass, Action emit)
    {
        try
        {
            emit();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            RecordEmissionFailure(operationClass, "emit-threw");
        }
    }

    // Best-effort gap signal: the meta-counter increment is itself exception-isolated so a failing exporter on the
    // meta-counter can never resurface as a thrown exception on the operation path.
    private void RecordEmissionFailure(string operationClass, string reason)
    {
        try
        {
            _emissionFailures.Add(
                1,
                new KeyValuePair<string, object?>(OperationClassTagName, operationClass),
                new KeyValuePair<string, object?>(ReasonTagName, reason));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            // The loss of the loss-signal is unrecoverable here; swallow so the operation path stays unaffected.
        }
    }

    private IEnumerable<Measurement<long>> ObserveAuditProjectionLag()
    {
        IReadOnlyList<AuditProjectionLagReading> readings;
        try
        {
            readings = _auditProjectionLagSource.ReadCurrent();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            RecordEmissionFailure(ChatBotOperationClasses.AuditProjectionLag, "lag-source-threw");
            yield break;
        }

        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        foreach (AuditProjectionLagReading reading in readings)
        {
            if (string.IsNullOrWhiteSpace(reading.TenantId))
            {
                RecordEmissionFailure(ChatBotOperationClasses.AuditProjectionLag, "tenant-unavailable");
                continue;
            }

            AuditProjectionLagStatus status = AuditProjectionLagEvaluator.Evaluate(
                reading.LastProjectedPosition,
                reading.LatestCommittedPosition,
                reading.SnapshotUtc,
                nowUtc);

            // Fail-safe: only a trustworthy, coarse lag value is surfaced. When the evaluator yields no-data
            // (Unknown / null LagEvents) we report no measurement instead of fabricating a 0.
            if (status.LagEvents is not { } lagEvents)
            {
                continue;
            }

            yield return new Measurement<long>(
                lagEvents,
                new KeyValuePair<string, object?>(TenantTagName, reading.TenantId),
                new KeyValuePair<string, object?>(OperationClassTagName, ChatBotOperationClasses.AuditProjectionLag));
        }
    }
}
