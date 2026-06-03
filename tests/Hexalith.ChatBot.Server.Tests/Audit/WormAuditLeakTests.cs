using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Redaction;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.1 (cross-cutting no-leak floor, NFR2/NFR42) coverage: no record, alert, or projection field — and no hash
/// input — carries a banned sensitive marker. The encrypted original is the only place raw content may live, and it is
/// opaque ciphertext, never asserted here.
/// </summary>
public sealed class WormAuditLeakTests
{
    private static readonly string[] BannedMarkers = ["secret", "password", "bearer", "token", "exception", ".txt", ".json", ".xml"];

    [Fact]
    public void BrokenChainAlertEnvelopeCarriesNoBannedMarkers()
    {
        WormAuditChainVerificationResult result = new(
            "tenant-alpha",
            WormChainVerificationStatus.Broken,
            WormAuditChainVerificationResult.RecordHashMismatchReasonCode,
            FirstBreakLocator: "seq:3");

        AuditEnvelope envelope = AuditEnvelopeFactory.AuditChainBroken(result, "01ARZ3NDEKTSV4RRFFQ69G5FAW", WormAuditTestData.FixedNow);

        envelope.Phase.ShouldBe(AuditCommitPhase.PreCommit);
        envelope.RedactionDecision.ShouldBe(CoarseUserFacingRedactionStage.MetadataOnlyDecision);
        envelope.SourceEvidenceRefs.ShouldContain("worm-chain-first-break:seq:3");
        AssertNoBannedMarkers(envelope.SourceEvidenceRefs);
    }

    [Fact]
    public void RedactionRecordEnvelopeCarriesNoBannedMarkers()
    {
        AuditEnvelope envelope = AuditEnvelopeFactory.AuditRecordRedacted(
            "tenant-alpha",
            redactedRecordLocator: "seq:2",
            subjectRef: "subject-1",
            redactionKeyHandle: "rk-abc123",
            reasonCode: "gdpr_erasure_request",
            correlationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            timestamp: WormAuditTestData.FixedNow);

        envelope.SourceEvidenceRefs.ShouldContain("redacted-record:seq:2");
        envelope.SourceEvidenceRefs.ShouldContain("redaction-key:rk-abc123");
        AssertNoBannedMarkers(envelope.SourceEvidenceRefs);
    }

    [Fact]
    public void CanonicalHashInputOfAMetadataOnlyEnvelopeCarriesNoBannedMarkers()
    {
        AuditEnvelope envelope = WormAuditTestData.Envelope("tenant-alpha");

        string canonical = WormAuditChainHasher.CanonicalizeEnvelope(envelope);

        foreach (string banned in BannedMarkers)
        {
            canonical.ShouldNotContain(banned, Case.Insensitive);
        }
    }

    private static void AssertNoBannedMarkers(IReadOnlyList<string> refs)
    {
        foreach (string banned in BannedMarkers)
        {
            refs.ShouldAllBe(reference => !reference.Contains(banned, StringComparison.OrdinalIgnoreCase));
        }
    }
}
