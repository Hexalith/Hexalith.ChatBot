using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Observability;

internal sealed class SweepBackedAuditCompletenessSource : IAuditCompletenessSource
{
    private readonly Lock _gate = new();
    private IReadOnlyList<AuditCompletenessReading> _readings = [];

    public IReadOnlyList<AuditCompletenessReading> ReadCurrent()
    {
        lock (_gate)
        {
            return _readings;
        }
    }

    public void Publish(IReadOnlyList<AuditCompletenessMeasurement> measurements)
    {
        ArgumentNullException.ThrowIfNull(measurements);
        AuditCompletenessReading[] readings = measurements
            .Select(static measurement => new AuditCompletenessReading(
                measurement.TenantRef,
                measurement.IsMeasurable,
                measurement.Fraction))
            .OrderBy(static reading => reading.TenantId, StringComparer.Ordinal)
            .ToArray();

        lock (_gate)
        {
            _readings = readings;
        }
    }
}
