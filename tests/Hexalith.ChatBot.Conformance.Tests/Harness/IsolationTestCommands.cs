using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Conformance.Tests.Harness;

/// <summary>
/// A tenant-mismatched mutating command shape used only by the cross-tenant isolation harness. Its
/// <see cref="TenantId"/> property is what <c>ClaimsTenantBindingStage</c> reads to detect a body-target tenant
/// mismatch (it ends with "TenantId"); the remaining string fields carry the leakage sentinels so the
/// metadata-only denial can be proven NOT to echo candidate/evidence/file/cursor/path/exception payload. None of
/// the sentinel fields are named like a tenant or scoped identifier, so they never affect tenant binding. This
/// command exists in tests only and is never registered in the production spine allowlist.
/// </summary>
/// <param name="TenantId">The foreign target tenant carried in the command body.</param>
/// <param name="ForeignCandidate">Candidate-channel sentinel.</param>
/// <param name="ForeignEvidence">Evidence-channel sentinel.</param>
/// <param name="ForeignFile">File-channel sentinel.</param>
/// <param name="ForeignCursor">Cursor-channel sentinel.</param>
/// <param name="RawPath">Path-fragment sentinel.</param>
/// <param name="RawProviderSnippet">Provider-snippet sentinel.</param>
/// <param name="RawException">Exception-text sentinel.</param>
internal sealed record CrossTenantProbeCommand(
    string TenantId,
    string ForeignCandidate,
    string ForeignEvidence,
    string ForeignFile,
    string ForeignCursor,
    string RawPath,
    string RawProviderSnippet,
    string RawException) : IChatBotCommand;

/// <summary>
/// A tenant-mismatched command whose scoped identifier (<c>tenant:resource:id</c>) carries the foreign tenant in
/// its prefix, exercising <c>ClaimsTenantBindingStage</c>'s scoped-identifier mismatch path. Test-only.
/// </summary>
/// <param name="Id">The tenant-scoped identifier whose tenant prefix is foreign.</param>
internal sealed record CrossTenantScopedIdentifierProbeCommand(string Id) : IChatBotCommand;
