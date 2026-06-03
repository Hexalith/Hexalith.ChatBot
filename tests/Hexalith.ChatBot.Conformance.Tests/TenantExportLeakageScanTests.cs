using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Conformance.Tests.Harness;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Story 9.8 (NFR2/NFR42) no-leak floor: the tenant export request command, run result, per-class result, and
/// snapshot metadata are metadata-only by construction (safe tokens + sha256 fingerprints). Serializing them and
/// routing the rendered JSON through the shared cross-tenant leakage scanner must surface no foreign-tenant (or any
/// other corpus-class) sentinel. Mirrors <c>DataClassInventoryLeakageScanTests</c>.
/// </summary>
public sealed class TenantExportLeakageScanTests
{
    [Fact]
    public void ExportContractSerializationCarriesNoCrossTenantSentinel()
    {
        TenantExportSnapshotMetadata snapshot = new(
            "export-snapshot-001",
            TenantExportSchemaVersions.V1,
            "export-snapshot-current",
            "export-snapshot-next",
            "export-run-001",
            "admin-requester",
            AdminScope.Compliance,
            [ComplianceRetentionClassIds.SourceEmailMetadata],
            8,
            new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "tenant-export-request",
            "policy-snapshot-admin-v1",
            "sha256:oldexportfingerprint001",
            "sha256:newexportfingerprint001");

        SubmitTenantExportRequest request = new(
            "export-run-001",
            "inventory-snapshot-current",
            8,
            new TenantExportRequestSpec(
                [ComplianceRetentionClassIds.SourceEmailMetadata, ComplianceRetentionClassIds.AuditRecords],
                new TenantExportScope("tenant-export-owner", ["project-authorized-001"])),
            "tenant-export-request",
            "admin-requester",
            TenantExportSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            "sha256:exportmanifestfingerprint001",
            new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.Zero));

        TenantExportRunResult run = TenantExportPlanner.Plan(
            DataClassInventoryCatalog.Published,
            request.RequestSpec,
            new TenantExportAuthorityView(true, new HashSet<string>(["project-authorized-001"], StringComparer.Ordinal)),
            request.ExportRunId,
            new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.Zero),
            request.CorrelationId);

        string rendered = JsonSerializer.Serialize(
            new { request, run, snapshot, classResult = run.ClassResults[0] },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Should.NotThrow(() =>
            CrossTenantLeakageScanner.ScanAll("compliance-admin", "tenant-export", rendered));
    }
}
