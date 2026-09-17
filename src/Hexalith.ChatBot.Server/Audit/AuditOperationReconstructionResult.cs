using Hexalith.ChatBot.Server.Gateway.Redaction;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The per-operation reconstruction result: the verdict, a metadata-only reason code, the resolved path, and the rebuilt state (when reconstructable from the chain).</summary>
internal sealed record AuditOperationReconstructionResult(
    AuditOperationReconstructability Status,
    string ReasonCode,
    string? PathCode,
    ReconstructedOperationState? State)
{
    /// <summary>Reason code for a fully reconstructable operation.</summary>
    public const string ReconstructableReasonCode = "reconstructable";

    /// <summary>No envelopes for the operation — the chain is missing or shorter than the operation requires.</summary>
    public const string ChainMissingReasonCode = "chain_missing";

    /// <summary>The result-bearing envelope carries no outcome, so the operation's end-state cannot be established.</summary>
    public const string OutcomeAbsentReasonCode = "outcome_absent";

    /// <summary>A required reconstruction field is absent/unsafe, so the end-state cannot be assembled from the chain.</summary>
    public const string StateUnreconstructableReasonCode = "state_unreconstructable";

    /// <summary>The envelope maps to no known NFR15a state-writing path — itself a completeness gap, never silently dropped.</summary>
    public const string UnmappedPathReasonCode = "unmapped_path";

    /// <summary>AC2 verdict (set by the measurer): the rebuilt state diverged from — or is absent in — the live projection.</summary>
    public const string ProjectionDivergedReasonCode = "projection_diverged";

    public bool IsReconstructable => Status == AuditOperationReconstructability.Reconstructable;

    public static AuditOperationReconstructionResult NotReconstructable(string reasonCode, string? pathCode = null)
        => new(AuditOperationReconstructability.NotReconstructable, reasonCode, pathCode, State: null);
}
