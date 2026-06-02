using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Identities;

public sealed record SenderAuthorityClassificationResult(
    SenderAuthorityClass AuthorityClass,
    string RequesterRef,
    string? MailboxRef,
    string? ServiceClientRef,
    string? PrincipalForRef,
    string? ApprovalRef,
    string PolicySnapshotRef,
    string EvidenceFreshness,
    IReadOnlyList<string> AuditEvidenceRefs,
    string? DenialReason);
