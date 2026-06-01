using System.Text.Json;

using Hexalith.ChatBot.Contracts.Queries;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class ProjectAiContextPackageContractTests
{
    [Fact]
    public static void PackageShouldSerializeMetadataOnlyManifestWithoutRawAttachmentFields()
    {
        ProjectAiContextPackage package = new(
            "tenant-alpha",
            "project-001",
            "policy-snapshot-001",
            "metadata_only",
            "collaboration_input",
            "disabled",
            "ai-context:project-001:12",
            "v1",
            ProjectAiContextPackage.SchemaVersionValue,
            12,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            [
                new ProjectAiContextPackageFile(
                    "attachment:stable-ref",
                    "folder-001",
                    "file-001",
                    "provider-attachment-001",
                    "metadata_only",
                    "collaboration_input",
                    "conversation-001"),
            ],
            [
                new ProjectAiContextPackageExclusion("attachment:redacted:stable-ref", "redacted"),
                new ProjectAiContextPackageExclusion("attachment:pending-ref", "pending-scan", "conversation-001"),
            ],
            ["conversation-001"],
            "m365-mailbox-intake",
            ProjectAiContextPackage.DerivationKernelVersionValue);

        string json = JsonSerializer.Serialize(package, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"policySnapshotId\":\"policy-snapshot-001\"");
        json.ShouldContain("\"providerReuseSetting\":\"disabled\"");
        json.ShouldContain("\"includedFiles\"");
        json.ShouldContain("\"excludedFiles\"");
        json.ShouldContain("\"reasonCode\":\"pending-scan\"");
        json.ShouldNotContain("displayName", Case.Insensitive);
        json.ShouldNotContain("contentType", Case.Insensitive);
        json.ShouldNotContain("byte", Case.Insensitive);
        json.ShouldNotContain("base64", Case.Insensitive);
        json.ShouldNotContain("path", Case.Insensitive);
        json.ShouldNotContain("scanner", Case.Insensitive);
        json.ShouldNotContain("payload", Case.Insensitive);
    }
}
