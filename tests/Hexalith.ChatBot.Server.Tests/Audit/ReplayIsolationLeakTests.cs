using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Redaction;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.4 (cross-cutting no-leak floor, NFR2/NFR42) coverage: the replay-isolation breach audit envelope, the probe
/// result, and the probe outcome carry no banned sensitive marker and no record content — only safe bounded tokens.
/// </summary>
public sealed class ReplayIsolationLeakTests
{
    [Fact]
    public void ReplayIsolationBreachEnvelopeIsMetadataOnlyWithNoBannedMarkers()
    {
        ReplayIsolationVerificationResult result = new(
            "tenant-alpha",
            ReplayIsolationStatus.Breach,
            ReplayIsolationVerificationResult.TraceBreachReasonCode,
            FirstOffenderLocator: "trace-send:send-007");

        AuditEnvelope envelope = AuditEnvelopeFactory.ReplayIsolationBreach(result, "01ARZ3NDEKTSV4RRFFQ69G5FAW", WormAuditTestData.FixedNow);

        envelope.Phase.ShouldBe(AuditCommitPhase.PreCommit);
        envelope.RedactionDecision.ShouldBe(CoarseUserFacingRedactionStage.MetadataOnlyDecision);
        envelope.ReplayRunId.ShouldBeNull(); // the system breach record is itself production
        envelope.SourceEvidenceRefs.ShouldContain("replay-isolation-severity:stop-ship");
        envelope.SourceEvidenceRefs.ShouldContain("replay-isolation-first-offender:trace-send:send-007");

        foreach (string banned in ReplayIsolationTestData.BannedMarkers)
        {
            envelope.SourceEvidenceRefs.ShouldAllBe(reference => !reference.Contains(banned, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void UnknownBreachEnvelopeOmitsTheLocatorAndStaysMetadataOnly()
    {
        ReplayIsolationVerificationResult result = new(
            "tenant-alpha",
            ReplayIsolationStatus.Unknown,
            ReplayIsolationVerificationResult.SweepIncompleteReasonCode,
            FirstOffenderLocator: null);

        AuditEnvelope envelope = AuditEnvelopeFactory.ReplayIsolationBreach(result, "01ARZ3NDEKTSV4RRFFQ69G5FAW", WormAuditTestData.FixedNow);

        envelope.SourceEvidenceRefs.ShouldContain("replay-isolation-status:unknown");
        envelope.SourceEvidenceRefs.ShouldNotContain(static reference => reference.StartsWith("replay-isolation-first-offender:", StringComparison.Ordinal));
        foreach (string banned in ReplayIsolationTestData.BannedMarkers)
        {
            envelope.SourceEvidenceRefs.ShouldAllBe(reference => !reference.Contains(banned, StringComparison.OrdinalIgnoreCase));
        }
    }
}
