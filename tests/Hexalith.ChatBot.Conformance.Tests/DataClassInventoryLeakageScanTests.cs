using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Conformance.Tests.Harness;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Story 9.7 (NFR2/NFR42) no-leak floor: the data-class inventory artifact, classification, snapshot metadata, and
/// the governed change command are metadata-only by construction (safe tokens + sha256 fingerprints). Serializing
/// them and routing the rendered JSON through the shared cross-tenant leakage scanner must surface no foreign-tenant
/// (or any other corpus-class) sentinel.
/// </summary>
public sealed class DataClassInventoryLeakageScanTests
{
    [Fact]
    public void InventoryContractSerializationCarriesNoCrossTenantSentinel()
    {
        DataClassInventorySnapshotMetadata snapshot = new(
            "inventory-snapshot-proposed",
            DataClassInventorySchemaVersions.V1,
            "inventory-snapshot-current",
            "inventory-snapshot-next",
            "inventory-change-001",
            "admin-requester",
            AdminScope.Compliance,
            [ComplianceRetentionClassIds.AuditRecords],
            8,
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "data-class-inventory-update",
            "policy-snapshot-admin-v1",
            "sha256:oldinventoryfingerprint001",
            "sha256:newinventoryfingerprint001");
        SubmitDataClassInventoryChange change = new(
            "inventory-change-001",
            "inventory-snapshot-current",
            "inventory-snapshot-proposed",
            8,
            new DataClassInventoryChangeSet(DataClassInventoryCatalog.Published.Classifications),
            "data-class-inventory-update",
            "admin-requester",
            DataClassInventorySchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            "sha256:oldinventoryfingerprint001",
            "sha256:newinventoryfingerprint001",
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero));

        string rendered = JsonSerializer.Serialize(
            new { inventory = DataClassInventoryCatalog.Published, snapshot, change },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Should.NotThrow(() =>
            CrossTenantLeakageScanner.ScanAll("compliance-admin", "data-class-inventory", rendered));
    }
}
