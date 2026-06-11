using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Observability;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Observability;

public sealed class MeasuredAuditSourceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CheckpointBackedAuditProjectionLagSourceShouldNotFabricateHealthyLagForUnmeasurableCheckpoint()
    {
        CheckpointBackedAuditProjectionLagSource source = new();

        source.Publish(
        [
            new AuditProjectionCheckpoint("tenant-alpha", LastProjectedPosition: null, LatestCommittedPosition: 10, Now),
            new AuditProjectionCheckpoint("tenant-beta", LastProjectedPosition: 9, LatestCommittedPosition: 12, Now),
        ]);

        AuditProjectionLagReading reading = source.ReadCurrent().ShouldHaveSingleItem();
        reading.TenantId.ShouldBe("tenant-beta");
        reading.LastProjectedPosition.ShouldBe(9);
        reading.LatestCommittedPosition.ShouldBe(12);
    }

    [Fact]
    public void SweepBackedAuditCompletenessSourceShouldExposeUnmeasurableReadingInsteadOfFabricatingPerfectCompleteness()
    {
        SweepBackedAuditCompletenessSource source = new();

        source.Publish(
        [
            AuditCompletenessMeasurement.Unmeasurable("tenant-alpha", Now.AddDays(-7), Now),
        ]);

        AuditCompletenessReading reading = source.ReadCurrent().ShouldHaveSingleItem();
        reading.TenantId.ShouldBe("tenant-alpha");
        reading.IsMeasurable.ShouldBeFalse();
        reading.Fraction.ShouldBe(0.0);
    }
}
