namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The FR95a replay-isolation predicate (Story 9.2): a record is a replay/simulation event iff it carries a
/// <see cref="AuditEnvelope.ReplayRunId"/>. The completeness measure removes replay events from <b>both</b> the
/// numerator (reconstructable operations) and the denominator (total state-mutating operations) before computing the
/// fraction, so a replay run can never inflate or deflate the measured completeness (NFR50a).
/// <para>
/// Today there are zero replay events in production — replay <em>execution</em> lands in Story 9.4 — so the exclusion
/// is satisfied by construction. This predicate makes it <b>real and testable now</b>: the distinguishing marker and
/// the exclusion test exist, so when Story 9.4 begins emitting replay records the measure stays correct without a
/// retrofit. The marker is tamper-evident (covered by the canonical hash from v2; see <see cref="WormAuditChainHasher"/>).
/// </para>
/// </summary>
internal static class AuditReplayExclusion
{
    /// <summary>Returns <see langword="true"/> when the envelope is a replay/simulation event (carries a replay run id).</summary>
    public static bool IsReplayEnvelope(AuditEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return envelope.ReplayRunId is not null;
    }
}
