using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.1 (AC3, NFR49a, cross-cutting #13) coverage for GDPR erasure by appended redaction record + KMS key-shred +
/// projection tombstone: redaction grows the chain (never shrinks it), the original is recoverable before shred and
/// unrecoverable after, the projection tombstone yields safe-not-found, and the chain still verifies end-to-end after
/// erasure (AC2 still passes).
/// </summary>
public sealed class AuditRedactionServiceTests
{
    private const string Subject = "subject-1";
    private const string Reason = "gdpr_erasure_request";
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    private static (InMemoryWormAuditStore Store, AuditRedactionService Service, InMemoryRedactionProjectionStore Projection) BuildService()
    {
        InMemoryWormAuditStore store = new();
        InMemoryRedactionProjectionStore projection = new();
        AuditRedactionService service = new(
            store,
            new InMemoryKmsRedactionKeyStore(),
            new InMemoryEncryptedAuditOriginalStore(),
            projection,
            new WormAuditTestData.FixedClock(WormAuditTestData.FixedNow));
        return (store, service, projection);
    }

    [Fact]
    public async Task RedactionAppendsToChainSoTheChainGrowsNeverShrinks()
    {
        (InMemoryWormAuditStore store, AuditRedactionService service, _) = BuildService();
        WormAuditChainRecord original = (await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha"), CancellationToken.None)).Record!;
        int before = store.EnumerateChain("tenant-alpha").Count;

        AuditRedactionRegistration registration = await service.RegisterRedactionAsync(original, Subject, Reason, Correlation, CancellationToken.None);

        store.EnumerateChain("tenant-alpha").Count.ShouldBe(before + 1);
        registration.RedactionRecord.Sequence.ShouldBe(original.Sequence + 1);
        registration.RedactionRecord.PredecessorHash.ShouldBe(original.RecordHash);
        registration.RedactionRecord.Envelope.CommandName.ShouldBe("AuditRecordRedacted");
    }

    [Fact]
    public async Task OriginalRecoverableBeforeShredUnrecoverableAfterKeyShred()
    {
        (InMemoryWormAuditStore store, AuditRedactionService service, _) = BuildService();
        WormAuditChainRecord original = (await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha", resourceId: "secret-resource"), CancellationToken.None)).Record!;
        AuditRedactionRegistration registration = await service.RegisterRedactionAsync(original, Subject, Reason, Correlation, CancellationToken.None);

        // Before shred: lawful access can decrypt the preserved original.
        service.TryRecoverOriginal(registration.KeyHandle, out AuditEnvelope? recovered).ShouldBeTrue();
        recovered!.ResourceId.ShouldBe("secret-resource");

        // Key-shred: the original is now unrecoverable (the key is gone).
        service.Erase(registration);
        service.TryRecoverOriginal(registration.KeyHandle, out AuditEnvelope? afterShred).ShouldBeFalse();
        afterShred.ShouldBeNull();
    }

    [Fact]
    public async Task ErasureTombstonesProjectionYieldingSafeNotFound()
    {
        (InMemoryWormAuditStore store, AuditRedactionService service, InMemoryRedactionProjectionStore projection) = BuildService();
        WormAuditChainRecord original = (await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha"), CancellationToken.None)).Record!;
        AuditRedactionRegistration registration = await service.RegisterRedactionAsync(original, Subject, Reason, Correlation, CancellationToken.None);

        // Before erasure the subject is not tombstoned; Erase tombstones it (tenant-partitioned), yielding safe-not-found.
        projection.IsTombstoned("tenant-alpha", Subject).ShouldBeFalse();

        service.Erase(registration);

        projection.IsTombstoned("tenant-alpha", Subject).ShouldBeTrue();
        projection.IsTombstoned("tenant-beta", Subject).ShouldBeFalse();
    }

    [Fact]
    public async Task ChainStillVerifiesEndToEndAfterErasure()
    {
        (InMemoryWormAuditStore store, AuditRedactionService service, _) = BuildService();
        for (int i = 0; i < 3; i++)
        {
            _ = await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha", resourceId: $"r{i}"), CancellationToken.None);
        }

        WormAuditChainRecord target = store.EnumerateChain("tenant-alpha")[1];
        AuditRedactionRegistration registration = await service.RegisterRedactionAsync(target, Subject, Reason, Correlation, CancellationToken.None);
        service.Erase(registration);

        // The chain bytes were never mutated — only a redaction record was appended and the key shredded.
        WormAuditChainVerificationResult result = WormAuditChainVerifier.Verify("tenant-alpha", store.EnumerateChain("tenant-alpha"));
        result.Status.ShouldBe(WormChainVerificationStatus.Verified);
    }
}
