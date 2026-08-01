using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

public sealed class RecoveryValidationDatasetTests
{
    [Fact]
    public void LoadMaterializesEveryDeclaredDatasetRecord()
    {
        RecoveryValidationDataset dataset = RecoveryValidationDataset.Load(
            DatasetPath(),
            RecoveryValidationTopology.LogicalTenantRef);

        dataset.Descriptor.TotalVolume.ShouldBe(6);
        dataset.Records.Count.ShouldBe(6);
        dataset.Records.Select(static record => record.Kind).ShouldBe(
            ["source", "worm-audit", "governed-command", "approval", "policy-snapshot", "attachment-metadata"]);
        dataset.SourceRecords.Single().IntakeId.ShouldBe("source-email-001");
        dataset.AuditEnvelopes.Single().ResourceId.ShouldBe("worm-record-001");
    }

    private static string DatasetPath()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(
            directory?.FullName ?? throw new InvalidOperationException("Repository root was not found."),
            "tests",
            "Hexalith.ChatBot.IntegrationTests",
            "Recovery",
            "Datasets",
            "recovery-baseline-v1.json");
    }
}
