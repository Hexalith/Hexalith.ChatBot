namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Defines stable story-evidence gate reason codes.
/// </summary>
public static class GateReason
{
    /// <summary>Indicates that status identities or transitions disagree.</summary>
    public const string StatusMismatch = "status_mismatch";

    /// <summary>Indicates that the File List and scoped diff disagree.</summary>
    public const string FileListDiffMismatch = "file_list_diff_mismatch";

    /// <summary>Indicates that a root gitlink and submodule scope disagree.</summary>
    public const string GitlinkScopeMismatch = "gitlink_scope_mismatch";

    /// <summary>Indicates that revisions, paths, or the deterministic digest disagree.</summary>
    public const string ScopeDigestMismatch = "scope_digest_mismatch";

    /// <summary>Indicates missing, malformed, failed, zero, or forbidden-skipped machine results.</summary>
    public const string MachineResultsInvalid = "machine_results_invalid";

    /// <summary>Indicates stale results or provenance that is not bound to the exact scope.</summary>
    public const string EvidenceStaleOrUnbound = "evidence_stale_or_unbound";

    /// <summary>Indicates that a triggered primary path has no successful primary execution.</summary>
    public const string PrimaryPathNotExecuted = "primary_path_not_executed";

    /// <summary>Indicates incomplete or contradicted task or acceptance evidence.</summary>
    public const string CheckedItemEvidenceMismatch = "checked_item_evidence_mismatch";

    /// <summary>Indicates forbidden payload or secret-shaped evidence.</summary>
    public const string EvidencePayloadForbidden = "evidence_payload_forbidden";
}
