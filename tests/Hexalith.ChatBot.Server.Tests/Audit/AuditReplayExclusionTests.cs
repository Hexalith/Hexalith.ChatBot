using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.2 (AC3, FR95a) coverage for the replay marker, its tamper-evident hash coverage, and the canonical version
/// bump. A record is a replay event iff it carries a <see cref="AuditEnvelope.ReplayRunId"/>; the marker is part of the
/// v2 canonical hash (two envelopes differing only in the marker hash differently); and pre-9.2 (v1) chains stay
/// verifiable byte-for-byte under their own stamped version.
/// </summary>
public sealed class AuditReplayExclusionTests
{
    [Fact]
    public void IsReplayEnvelopeIsTrueOnlyWhenReplayRunIdIsPresent()
    {
        AuditEnvelope production = WormAuditTestData.Envelope("tenant-alpha");
        AuditEnvelope replay = production with { ReplayRunId = "replay-run-1" };

        AuditReplayExclusion.IsReplayEnvelope(production).ShouldBeFalse();
        AuditReplayExclusion.IsReplayEnvelope(replay).ShouldBeTrue();
    }

    [Fact]
    public void CanonicalSerializationVersionWasBumpedToV2()
    {
        WormAuditChainHasher.CanonicalSerializationVersion.ShouldBe("chatbot.worm-chain.v2");
        WormAuditChainHasher.CanonicalSerializationVersionV1.ShouldBe("chatbot.worm-chain.v1");
    }

    [Fact]
    public void ReplayRunIdIsCoveredByTheCurrentCanonicalHash()
    {
        AuditEnvelope production = WormAuditTestData.Envelope("tenant-alpha");
        AuditEnvelope replay = production with { ReplayRunId = "replay-run-1" };

        string productionHash = WormAuditChainHasher.ComputeRecordHash(production, WormAuditChainHasher.GenesisPredecessorHash, 0);
        string replayHash = WormAuditChainHasher.ComputeRecordHash(replay, WormAuditChainHasher.GenesisPredecessorHash, 0);

        // A replay record masquerading as production (or vice-versa) must change the digest — tamper-evident.
        replayHash.ShouldNotBe(productionHash);
    }

    [Fact]
    public void V1CanonicalFormIgnoresReplayRunIdSoPre92ChainsStayStable()
    {
        AuditEnvelope production = WormAuditTestData.Envelope("tenant-alpha");
        AuditEnvelope replay = production with { ReplayRunId = "replay-run-1" };

        // Under v1 the marker is not part of the canonical form, so the two are byte-identical (back-compat).
        string v1Production = WormAuditChainHasher.CanonicalizeEnvelope(production, WormAuditChainHasher.CanonicalSerializationVersionV1);
        string v1Replay = WormAuditChainHasher.CanonicalizeEnvelope(replay, WormAuditChainHasher.CanonicalSerializationVersionV1);
        v1Production.ShouldBe(v1Replay);

        // Under v2 they differ.
        WormAuditChainHasher.CanonicalizeEnvelope(production)
            .ShouldNotBe(WormAuditChainHasher.CanonicalizeEnvelope(replay));
    }

    [Fact]
    public void AV1StampedRecordStillVerifiesAfterTheBump()
    {
        // A record written before Story 9.2 (stamped v1, hashed under v1) must re-verify even though the current
        // canonical form (v2) folds in the replay marker — the verifier re-hashes under the record's stamped version.
        AuditEnvelope envelope = WormAuditTestData.Envelope("tenant-alpha") with { PredecessorHash = WormAuditChainHasher.GenesisPredecessorHash };
        string v1Hash = WormAuditChainHasher.ComputeRecordHash(
            envelope,
            WormAuditChainHasher.GenesisPredecessorHash,
            0,
            WormAuditChainHasher.CanonicalSerializationVersionV1);

        WormAuditChainRecord v1Record = new(
            envelope,
            Sequence: 0,
            WormAuditChainHasher.GenesisPredecessorHash,
            v1Hash,
            WormAuditChainHasher.CanonicalSerializationVersionV1);

        WormAuditChainVerificationResult result = WormAuditChainVerifier.Verify("tenant-alpha", [v1Record]);

        result.Status.ShouldBe(WormChainVerificationStatus.Verified);
    }
}
