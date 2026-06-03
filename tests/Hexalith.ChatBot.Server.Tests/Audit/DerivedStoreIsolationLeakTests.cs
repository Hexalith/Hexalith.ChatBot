using System.Text.Json;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Projections.DerivedStores;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.5 (cross-cutting no-leak floor, NFR2/NFR42) coverage: the derived-store entry, the probe verification result,
/// the probe outcome, and the breach audit envelope carry no banned sensitive marker and no derived-store content
/// (vectors, embeddings, prompt text, candidate payloads) — only safe bounded tokens. Derived stores hold the most
/// sensitive material in the system, so every serialized type is scanned. Mirrors the Story 9.4 replay-isolation suite.
/// </summary>
public sealed class DerivedStoreIsolationLeakTests
{
    [Fact]
    public void DerivedStoreEntryCreateSanitizesContentToSafeTokens()
    {
        // A sensitive-marker-bearing digest (here a secret) collapses to the safe fallback — never stored verbatim.
        DerivedStoreEntry entry = DerivedStoreEntry.Create("res-001", "embedding-secret-vector");

        entry.ResourceId.ShouldBe("res-001");
        entry.ContentDigest.ShouldBe("redacted-ref");
        AssertNoBannedMarkers(JsonSerializer.Serialize(entry));
    }

    [Fact]
    public void DerivedStoreEntryCreateWithNullDigestFallsBackToASafeToken()
    {
        // A null/absent digest never leaves a null on the record — it collapses to the safe fallback so the entry is
        // always a fully-populated, metadata-only token set.
        DerivedStoreEntry entry = DerivedStoreEntry.Create("res-003", contentDigest: null);

        entry.ResourceId.ShouldBe("res-003");
        entry.ContentDigest.ShouldBe("redacted-ref");
        AssertNoBannedMarkers(JsonSerializer.Serialize(entry));
    }

    [Fact]
    public void DerivedStoreEntryCreateSanitizesAnUnsafeResourceIdToTheSafeFallback()
    {
        // An unsafe resource id (here carrying a banned ".json" marker) cannot smuggle content into the entry — it
        // collapses to the safe fallback rather than being stored verbatim.
        DerivedStoreEntry entry = DerivedStoreEntry.Create("payload.json", "digest-ok");

        entry.ResourceId.ShouldBe("redacted-ref");
        AssertNoBannedMarkers(JsonSerializer.Serialize(entry));
    }

    [Fact]
    public void DerivedStoreEntryWithRawFloatsLikeTextStaysMetadataOnly()
    {
        // A "vector floats" looking digest is just a token; it has no banned marker, but the point is that the entry
        // type can ONLY hold tokens — there is no field for raw vector/embedding/prompt content.
        DerivedStoreEntry entry = DerivedStoreEntry.Create("res-002", "digest-abc123");
        AssertNoBannedMarkers(JsonSerializer.Serialize(entry));
    }

    [Fact]
    public void VerificationResultIsMetadataOnly()
    {
        DerivedStoreIsolationVerificationResult result = new(
            "tenant-alpha",
            "tenant-beta",
            DerivedStoreIsolationStatus.Breach,
            DerivedStoreIsolationVerificationResult.BreachReasonCode,
            FirstOffenderLocator: "derived-store-sentinel:iso-probe:vector-index:tenant-alpha:corr");

        AssertNoBannedMarkers(JsonSerializer.Serialize(result));
    }

    [Fact]
    public void ProbeOutcomeIsMetadataOnly()
        => AssertNoBannedMarkers(JsonSerializer.Serialize(new DerivedStoreIsolationProbeOutcome(4, 1, 1)));

    [Fact]
    public void BreachEnvelopeIsMetadataOnlyWithNoBannedMarkers()
    {
        DerivedStoreIsolationVerificationResult result = new(
            "tenant-alpha",
            "tenant-beta",
            DerivedStoreIsolationStatus.Breach,
            DerivedStoreIsolationVerificationResult.BreachReasonCode,
            FirstOffenderLocator: "derived-store-sentinel:iso-probe:vector-index:tenant-alpha:corr");

        AuditEnvelope envelope = AuditEnvelopeFactory.DerivedStoreIsolationBreach(result, "01ARZ3NDEKTSV4RRFFQ69G5FAW", WormAuditTestData.FixedNow);

        envelope.Phase.ShouldBe(AuditCommitPhase.PreCommit);
        envelope.RedactionDecision.ShouldBe(CoarseUserFacingRedactionStage.MetadataOnlyDecision);
        envelope.ReplayRunId.ShouldBeNull(); // the system breach record is itself production
        envelope.SourceEvidenceRefs.ShouldContain("derived-store-isolation-severity:stop-ship");
        envelope.SourceEvidenceRefs.ShouldContain("derived-store-isolation-intruder:tenant-beta");
        envelope.SourceEvidenceRefs.ShouldContain("derived-store-isolation-first-offender:derived-store-sentinel:iso-probe:vector-index:tenant-alpha:corr");

        foreach (string banned in ReplayIsolationTestData.BannedMarkers)
        {
            envelope.SourceEvidenceRefs.ShouldAllBe(reference => !reference.Contains(banned, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void UnknownBreachEnvelopeOmitsTheLocatorAndStaysMetadataOnly()
    {
        DerivedStoreIsolationVerificationResult result = new(
            "tenant-alpha",
            "tenant-beta",
            DerivedStoreIsolationStatus.Unknown,
            DerivedStoreIsolationVerificationResult.ProbeIncompleteReasonCode,
            FirstOffenderLocator: null);

        AuditEnvelope envelope = AuditEnvelopeFactory.DerivedStoreIsolationBreach(result, "01ARZ3NDEKTSV4RRFFQ69G5FAW", WormAuditTestData.FixedNow);

        envelope.SourceEvidenceRefs.ShouldContain("derived-store-isolation-status:unknown");
        envelope.SourceEvidenceRefs.ShouldNotContain(static reference => reference.StartsWith("derived-store-isolation-first-offender:", StringComparison.Ordinal));
        foreach (string banned in ReplayIsolationTestData.BannedMarkers)
        {
            envelope.SourceEvidenceRefs.ShouldAllBe(reference => !reference.Contains(banned, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static void AssertNoBannedMarkers(string serialized)
    {
        foreach (string banned in ReplayIsolationTestData.BannedMarkers)
        {
            serialized.ShouldNotContain(banned, Case.Insensitive);
        }
    }
}
