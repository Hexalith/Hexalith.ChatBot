using Hexalith.ChatBot.Server.Gateway.Redaction;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Pure, deterministic per-operation reconstructability evaluator (Story 9.2, AC1/NFR50a). Given the chained WORM
/// envelope(s) for a single state-mutating operation it decides whether the operation can be rebuilt <b>end-to-end
/// from the chain alone</b> — and produces the metadata-only <see cref="ReconstructedOperationState"/> the AC2 measurer
/// diffs against the live projection.
/// <para>
/// <b>Reconstructability is deliberately STRONGER than field presence — this is the defining distinction of the story.</b>
/// NFR50 (already shipped) tests 100% required-field <em>presence</em> on the validation dataset. NFR50a asks the harder
/// question: can the operation be <em>reconstructed</em>? This evaluator therefore checks field presence only as a
/// <em>precondition</em> (reusing the existing <see cref="AuditMetadata"/> safe-token discipline) and then goes further:
/// it maps the envelope to its NFR15a path and <b>assembles the operation's resulting end-state</b> (resource, decision,
/// transition, outcome, and the structural projection token). Presence alone is necessary but NOT sufficient — an
/// operation with every field present whose end-state cannot be assembled, whose path is unknown, or (in AC2) whose
/// rebuilt state diverges from the projection is <c>NotReconstructable</c>. Do not quietly downgrade this to a
/// presence-only check: the assembled end-state + the AC2 projection diff are what make this NFR50a, not NFR50.
/// </para>
/// <para>
/// Fail-safe: an operation with no envelopes is <c>chain_missing</c>, an unmapped path is <c>unmapped_path</c>, and a
/// result-bearing envelope missing its outcome/required fields is <c>outcome_absent</c>/<c>state_unreconstructable</c>
/// — never a fabricated reconstructable.
/// </para>
/// </summary>
internal static class AuditOperationReconstructor
{
    /// <summary>
    /// Evaluates one operation's chained envelopes (in append order). Replay envelopes must already be excluded by the
    /// caller (FR95a). Returns the reconstruction verdict from the chain alone; the AC2 projection diff is applied
    /// afterwards by the measurer.
    /// </summary>
    public static AuditOperationReconstructionResult Reconstruct(IReadOnlyList<AuditEnvelope> operationEnvelopes)
    {
        ArgumentNullException.ThrowIfNull(operationEnvelopes);

        if (operationEnvelopes.Count == 0)
        {
            return AuditOperationReconstructionResult.NotReconstructable(AuditOperationReconstructionResult.ChainMissingReasonCode);
        }

        // The result-bearing envelope carries the operation's end-state: prefer the post-commit record (the committed
        // outcome), falling back to the last envelope when an operation never reached post-commit.
        AuditEnvelope representative = ResultBearingEnvelope(operationEnvelopes);

        // Path mapping (NFR15a): an envelope that maps to no known state-writing path is itself a completeness gap.
        ChatBotStateWritingPath? path = ChatBotAuditPathMap.Resolve(representative);
        if (path is null)
        {
            return AuditOperationReconstructionResult.NotReconstructable(AuditOperationReconstructionResult.UnmappedPathReasonCode);
        }

        // Field-presence PRECONDITION (NFR50 discipline) — necessary but not sufficient. The outcome is called out
        // separately because an absent outcome specifically means the end-state cannot be established.
        if (!AuditMetadata.IsSafeStableIdentifier(representative.Outcome))
        {
            return AuditOperationReconstructionResult.NotReconstructable(AuditOperationReconstructionResult.OutcomeAbsentReasonCode, path.Code);
        }

        if (!HasAllReconstructionFields(representative))
        {
            return AuditOperationReconstructionResult.NotReconstructable(AuditOperationReconstructionResult.StateUnreconstructableReasonCode, path.Code);
        }

        // Beyond presence: ASSEMBLE the end-state. The resulting-state token is the structural rebuild AC2 will diff
        // against the projection (path + transition + outcome), and the projection redaction token is the structural
        // field the projection must agree on.
        ReconstructedOperationState state = new(
            ResourceId: representative.ResourceId,
            Decision: representative.Decision,
            ReasonCode: representative.ReasonCode,
            PolicySnapshotId: representative.PolicySnapshotId,
            StateTransition: representative.StateTransition,
            Outcome: representative.Outcome,
            ProjectionRedactionState: ProjectionRedactionState(representative),
            ResultingStateToken: $"{path.Code}:{representative.StateTransition}:{representative.Outcome}");

        return new AuditOperationReconstructionResult(
            AuditOperationReconstructability.Reconstructable,
            AuditOperationReconstructionResult.ReconstructableReasonCode,
            path.Code,
            state);
    }

    private static AuditEnvelope ResultBearingEnvelope(IReadOnlyList<AuditEnvelope> envelopes)
    {
        AuditEnvelope? postCommit = null;
        foreach (AuditEnvelope envelope in envelopes)
        {
            if (envelope.Phase == AuditCommitPhase.PostCommit)
            {
                postCommit = envelope;
            }
        }

        return postCommit ?? envelopes[^1];
    }

    // All the reconstruction fields NFR50a names must be present AND safe (the AuditMetadata token class) from the
    // chain alone — except StateTransition, which is a structural "From->To" arrow whose '>' is intentionally outside
    // the safe-token charset; it only has to be present. SourceEvidenceRefs must be non-empty — an operation with no
    // evidence refs cannot be rebuilt.
    private static bool HasAllReconstructionFields(AuditEnvelope envelope)
        => AuditMetadata.IsSafeStableIdentifier(envelope.ActorId)
            && AuditMetadata.IsSafeStableIdentifier(envelope.ActorType)
            && AuditMetadata.IsSafeStableIdentifier(envelope.CommandName)
            && AuditMetadata.IsSafeStableIdentifier(envelope.ResourceId)
            && AuditMetadata.IsSafeStableIdentifier(envelope.Decision)
            && AuditMetadata.IsSafeStableIdentifier(envelope.ReasonCode)
            && AuditMetadata.IsSafeStableIdentifier(envelope.PolicySnapshotId)
            && !string.IsNullOrWhiteSpace(envelope.StateTransition)
            && envelope.SourceEvidenceRefs.Count > 0;

    // Structural redaction token the projection must agree on. The chain's MetadataOnlyDecision corresponds to the
    // governed-operation projection's MetadataOnlyRedactionState (both "metadata_only"); any other decision is passed
    // through as its own safe token so a future redaction class still diffs structurally.
    private static string ProjectionRedactionState(AuditEnvelope envelope)
        => string.Equals(envelope.RedactionDecision, CoarseUserFacingRedactionStage.MetadataOnlyDecision, StringComparison.Ordinal)
            ? Projections.GovernedOperationView.MetadataOnlyRedactionState
            : envelope.RedactionDecision;
}
