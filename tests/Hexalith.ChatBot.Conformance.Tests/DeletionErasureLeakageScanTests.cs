using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Conformance.Tests.Harness;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Story 9.9 (NFR2/NFR42) no-leak floor: the deletion/erasure request command, run result, per-class result, proof
/// artifact/entry, and snapshot metadata are metadata-only by construction (safe tokens + sha256 fingerprints).
/// Serializing them and routing the rendered JSON through the shared cross-tenant leakage scanner must surface no
/// foreign-tenant (or any other corpus-class) sentinel. Mirrors <c>TenantExportLeakageScanTests</c>. The scope token
/// is the neutral, non-sentinel <c>tenant-deletion-owner</c> (not the Story 1.12 corpus <c>tenant-alpha</c> boundary).
/// </summary>
public sealed class DeletionErasureLeakageScanTests
{
    [Fact]
    public void DeletionContractSerializationCarriesNoCrossTenantSentinel()
    {
        DeletionErasureSnapshotMetadata snapshot = new(
            "deletion-snapshot-001",
            DeletionErasureSchemaVersions.V1,
            "deletion-snapshot-current",
            "deletion-snapshot-next",
            "deletion-run-001",
            "admin-requester",
            AdminScope.Compliance,
            [ComplianceRetentionClassIds.SourceEmailMetadata],
            8,
            new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "deletion-erasure-request",
            "policy-snapshot-admin-v1",
            "sha256:olddeletionfingerprint001",
            "sha256:newdeletionfingerprint001");

        SubmitDeletionErasureRequest request = new(
            "deletion-run-001",
            "inventory-snapshot-current",
            8,
            new DeletionErasureRequestSpec(
                DeletionErasureModes.Erasure,
                [ComplianceRetentionClassIds.SourceEmailMetadata, ComplianceRetentionClassIds.AuditRecords],
                new DeletionErasureScope("tenant-deletion-owner", ["project-authorized-001"])),
            "deletion-erasure-request",
            "admin-requester",
            DeletionErasureSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            "sha256:deletionprooffingerprint001",
            new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.Zero));

        DeletionErasureRunResult run = DeletionErasurePlanner.Plan(
            DataClassInventoryCatalog.Published,
            request.RequestSpec,
            new DeletionErasureAuthorityView(true, new HashSet<string>(["project-authorized-001"], StringComparer.Ordinal)),
            request.DeletionRunId,
            new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.Zero),
            request.CorrelationId);

        string rendered = JsonSerializer.Serialize(
            new { request, run, snapshot, classResult = run.ClassResults[0], proofEntry = run.Proof.Entries[0] },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Should.NotThrow(() =>
            CrossTenantLeakageScanner.ScanAll("compliance-admin", "tenant-deletion", rendered));
    }
}
