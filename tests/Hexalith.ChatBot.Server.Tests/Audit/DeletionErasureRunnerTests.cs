using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.9 (AC3/AC5) coverage for the <see cref="DeletionErasureRunner"/> wiring the audit-chain erasure portion of
/// an erasure run to the EXISTING Story 9.1 <see cref="AuditRedactionService"/> seam. An erasure over an audited
/// subject appends a redaction record, crypto-shreds the key (original unrecoverable), tombstones the subject
/// (safe-not-found), produces a populated <see cref="ErasureProofEntry"/>, and — the AC3 guarantee — leaves the WORM
/// chain still verifiable end-to-end. Reuses the Story 9.1 in-memory WORM harness. Also covers the per-class failure
/// classification reusing the ONE retry taxonomy (AC4, NFR17/NFR18).
/// </summary>
public sealed class DeletionErasureRunnerTests
{
    private const string DataClassId = ComplianceRetentionClassIds.AiPromptsOutputsContext;
    private const string Subject = "subject-1";
    private const string Reason = "gdpr_erasure_request";
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly DateTimeOffset ObservedAt = new(2026, 6, 3, 8, 0, 0, TimeSpan.Zero);

    private static (InMemoryWormAuditStore Store, AuditRedactionService Service, InMemoryKmsRedactionKeyStore Kms, InMemoryRedactionProjectionStore Projection) BuildService()
    {
        InMemoryWormAuditStore store = new();
        InMemoryRedactionProjectionStore projection = new();
        InMemoryKmsRedactionKeyStore kms = new();
        AuditRedactionService service = new(
            store,
            kms,
            new InMemoryEncryptedAuditOriginalStore(),
            projection,
            new WormAuditTestData.FixedClock(WormAuditTestData.FixedNow));
        return (store, service, kms, projection);
    }

    [Fact]
    public async Task ErasureAppendsRedactionShredsKeyTombstonesAndKeepsChainVerifiable()
    {
        (InMemoryWormAuditStore store, AuditRedactionService service, InMemoryKmsRedactionKeyStore kms, InMemoryRedactionProjectionStore projection) = BuildService();
        for (int i = 0; i < 3; i++)
        {
            _ = await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha", resourceId: $"r{i}"), CancellationToken.None);
        }

        DeletionErasureRunner runner = new(service);
        WormAuditChainRecord target = store.EnumerateChain("tenant-alpha")[1];
        int before = store.EnumerateChain("tenant-alpha").Count;

        ErasureProofEntry entry = await runner.EraseAuditSubjectAsync(
            target, DataClassId, Subject, Reason, Correlation, CancellationToken.None);

        // AC5: a populated, metadata-only proof entry — tombstone + key-shred confirmations.
        entry.DataClassId.ShouldBe(DataClassId);
        entry.SubjectLocator.ShouldBe($"tenant-alpha:{Subject}");
        entry.Tombstoned.ShouldBeTrue();
        entry.KeyShredded.ShouldBeTrue();
        entry.KeyHandle.ShouldNotBeNullOrWhiteSpace();

        // The chain grew by one (the appended redaction record) — it never shrank.
        store.EnumerateChain("tenant-alpha").Count.ShouldBe(before + 1);

        // The key is shredded (original unrecoverable) and the subject is tombstoned (safe-not-found).
        kms.HasKey(entry.KeyHandle).ShouldBeFalse();
        projection.IsTombstoned("tenant-alpha", Subject).ShouldBeTrue();

        // AC3: the chain bytes were never mutated — verification still passes end-to-end.
        WormAuditChainVerificationResult result = WormAuditChainVerifier.Verify("tenant-alpha", store.EnumerateChain("tenant-alpha"));
        result.Status.ShouldBe(WormChainVerificationStatus.Verified);
    }

    [Theory]
    [InlineData("graph_throttled", 1, DeletionErasureClassStatuses.FailedRetryable)]
    [InlineData("projection_retryable", 0, DeletionErasureClassStatuses.FailedRetryable)]
    [InlineData("graph_permission_revoked", 1, DeletionErasureClassStatuses.FailedTerminal)]
    [InlineData("graph_throttled", 5, DeletionErasureClassStatuses.FailedTerminal)] // exhausted ⇒ terminal
    public void ClassifyClassStatusShouldReuseTheRetryTaxonomy(string reasonCode, int retryCount, string expected)
        => DeletionErasureFailureClassifier.ClassifyClassStatus(reasonCode, retryCount, ObservedAt)
            .ShouldBe(expected);
}
