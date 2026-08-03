using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.ChatBot.Server.Projections;

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
        dataset.Descriptor.UsesIsolatedValidationStore.ShouldBeTrue();
        dataset.Descriptor.ValidationPartitionRef.ShouldBe("recovery-partition-v1");
        dataset.Descriptor.ProjectionSchemaVersion.ShouldBe(ProjectConversationSourceEmailView.CurrentSchemaVersion);
        dataset.Records.Count.ShouldBe(6);
        dataset.Records.Select(static record => record.Kind).ShouldBe(
            ["source", "worm-audit", "governed-command", "approval", "policy-snapshot", "attachment-metadata"]);
        dataset.Records.Single(static record => record.Kind == "governed-command").CommandKind.ShouldBe("RecordGovernedNote");
        dataset.SourceRecords.Single().IntakeId.ShouldBe("source-email-001");
        dataset.SourceRecords.Single().SchemaVersion.ShouldBe(ProjectConversationSourceEmailView.CurrentSchemaVersion);
        dataset.AuditEnvelopes.Single().ResourceId.ShouldBe("worm-record-001");
    }

    [Fact]
    public void LoadRejectsProjectionSchemaVersionMismatch()
    {
        string path = WriteTempDataset(root =>
        {
            root["projectionSchemaVersion"] = "project-conversation-v1";
        });

        InvalidDataException thrown = Should.Throw<InvalidDataException>(() =>
            RecoveryValidationDataset.Load(path, RecoveryValidationTopology.LogicalTenantRef));
        thrown.Message.ShouldContain(ProjectConversationSourceEmailView.CurrentSchemaVersion);
    }

    [Fact]
    public void LoadRejectsEmptyCategory()
    {
        string path = WriteTempDataset(root =>
        {
            root["approvals"] = new JsonArray();
            root["volume"] = 5;
        });

        InvalidDataException thrown = Should.Throw<InvalidDataException>(() =>
            RecoveryValidationDataset.Load(path, RecoveryValidationTopology.LogicalTenantRef));
        thrown.Message.ShouldContain("approvals");
    }

    [Fact]
    public void LoadRejectsVolumeMismatch()
    {
        string path = WriteTempDataset(root =>
        {
            root["volume"] = 99;
        });

        InvalidDataException thrown = Should.Throw<InvalidDataException>(() =>
            RecoveryValidationDataset.Load(path, RecoveryValidationTopology.LogicalTenantRef));
        thrown.Message.ShouldContain("volume");
    }

    [Fact]
    public void LoadRejectsMissingGovernedCommandKind()
    {
        string path = WriteTempDataset(root =>
        {
            root["governedCommands"] = new JsonArray(
                new JsonObject
                {
                    ["commandRef"] = "governed-command-001",
                    ["structuralState"] = "accepted-v1",
                });
        });

        _ = Should.Throw<KeyNotFoundException>(() =>
            RecoveryValidationDataset.Load(path, RecoveryValidationTopology.LogicalTenantRef));
    }

    private static string DatasetPath()
    {
        string outputPath = Path.Combine(
            AppContext.BaseDirectory,
            "Recovery",
            "Datasets",
            "recovery-baseline-v1.json");
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

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

    private static string WriteTempDataset(Action<JsonObject> mutate)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(DatasetPath()));
        JsonObject root = JsonNode.Parse(document.RootElement.GetRawText())!.AsObject();
        mutate(root);
        string path = Path.Combine(Path.GetTempPath(), $"recovery-baseline-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }
}
