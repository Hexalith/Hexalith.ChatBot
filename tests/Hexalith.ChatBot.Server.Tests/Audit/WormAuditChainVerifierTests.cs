using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.1 (AC2, NFR49a) coverage for the pure <see cref="WormAuditChainVerifier"/>: an intact chain verifies; a
/// mutated record, a broken predecessor link, and a sequence discontinuity each report <c>Broken</c> with the correct
/// reason code and first-break locator.
/// </summary>
public sealed class WormAuditChainVerifierTests
{
    private static async Task<(InMemoryWormAuditStore Store, IReadOnlyList<WormAuditChainRecord> Chain)> BuildChainAsync(int length)
    {
        InMemoryWormAuditStore store = new();
        for (int i = 0; i < length; i++)
        {
            _ = await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha", resourceId: $"r{i}"), CancellationToken.None).ConfigureAwait(false);
        }

        return (store, store.EnumerateChain("tenant-alpha"));
    }

    [Fact]
    public void EmptyChainVerifies()
    {
        WormAuditChainVerificationResult result = WormAuditChainVerifier.Verify("tenant-alpha", []);

        result.Status.ShouldBe(WormChainVerificationStatus.Verified);
        result.IsBreach.ShouldBeFalse();
        result.FirstBreakLocator.ShouldBeNull();
    }

    [Fact]
    public async Task IntactChainVerifies()
    {
        (_, IReadOnlyList<WormAuditChainRecord> chain) = await BuildChainAsync(4);

        WormAuditChainVerificationResult result = WormAuditChainVerifier.Verify("tenant-alpha", chain);

        result.Status.ShouldBe(WormChainVerificationStatus.Verified);
        result.ReasonCode.ShouldBe(WormAuditChainVerificationResult.VerifiedReasonCode);
    }

    [Fact]
    public async Task MutatedRecordIsDetectedWithHashMismatchAndLocator()
    {
        (_, IReadOnlyList<WormAuditChainRecord> chain) = await BuildChainAsync(4);
        List<WormAuditChainRecord> tampered = [.. chain];
        // Mutate the body of record at sequence 2 while leaving its stored RecordHash unchanged.
        tampered[2] = tampered[2] with { Envelope = tampered[2].Envelope with { Outcome = "tampered" } };

        WormAuditChainVerificationResult result = WormAuditChainVerifier.Verify("tenant-alpha", tampered);

        result.Status.ShouldBe(WormChainVerificationStatus.Broken);
        result.ReasonCode.ShouldBe(WormAuditChainVerificationResult.RecordHashMismatchReasonCode);
        result.FirstBreakLocator.ShouldBe("seq:2");
    }

    [Fact]
    public async Task BrokenPredecessorLinkIsDetected()
    {
        (_, IReadOnlyList<WormAuditChainRecord> chain) = await BuildChainAsync(3);
        List<WormAuditChainRecord> tampered = [.. chain];
        // Re-point record 1's predecessor to a foreign hash, then recompute its own hash so only the link is wrong.
        string forgedPredecessor = new('b', 64);
        AuditEnvelope envelope = tampered[1].Envelope with { PredecessorHash = forgedPredecessor };
        string rehashed = WormAuditChainHasher.ComputeRecordHash(envelope, forgedPredecessor, 1);
        tampered[1] = tampered[1] with { Envelope = envelope, PredecessorHash = forgedPredecessor, RecordHash = rehashed };

        WormAuditChainVerificationResult result = WormAuditChainVerifier.Verify("tenant-alpha", tampered);

        result.Status.ShouldBe(WormChainVerificationStatus.Broken);
        result.ReasonCode.ShouldBe(WormAuditChainVerificationResult.PredecessorLinkBrokenReasonCode);
        result.FirstBreakLocator.ShouldBe("seq:1");
    }

    [Fact]
    public async Task ForgedGenesisPredecessorIsDetectedAtSequenceZero()
    {
        (_, IReadOnlyList<WormAuditChainRecord> chain) = await BuildChainAsync(3);
        List<WormAuditChainRecord> tampered = [.. chain];
        // Re-point the genesis record's predecessor away from the genesis sentinel: the first record must chain off
        // the sentinel, so a forged genesis is a broken link at seq:0 (a forged-origin / chain-truncation attack).
        tampered[0] = tampered[0] with { PredecessorHash = new string('c', 64) };

        WormAuditChainVerificationResult result = WormAuditChainVerifier.Verify("tenant-alpha", tampered);

        result.Status.ShouldBe(WormChainVerificationStatus.Broken);
        result.ReasonCode.ShouldBe(WormAuditChainVerificationResult.PredecessorLinkBrokenReasonCode);
        result.FirstBreakLocator.ShouldBe("seq:0");
    }

    [Fact]
    public async Task SequenceDiscontinuityIsDetected()
    {
        (_, IReadOnlyList<WormAuditChainRecord> chain) = await BuildChainAsync(3);
        List<WormAuditChainRecord> tampered = [.. chain];
        // Drop the middle record: the survivor at index 1 now carries sequence 2 → discontinuity at index 1.
        tampered.RemoveAt(1);

        WormAuditChainVerificationResult result = WormAuditChainVerifier.Verify("tenant-alpha", tampered);

        result.Status.ShouldBe(WormChainVerificationStatus.Broken);
        result.ReasonCode.ShouldBe(WormAuditChainVerificationResult.SequenceDiscontinuityReasonCode);
        result.FirstBreakLocator.ShouldBe("seq:1");
    }
}
