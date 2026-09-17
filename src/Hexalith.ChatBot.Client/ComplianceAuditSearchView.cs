using System.Net.Http.Json;
using System.Text.Json;

namespace Hexalith.ChatBot.Client;

/// <summary>The metadata-only result of a compliance audit search — bounded safe tokens only.</summary>
public sealed record ComplianceAuditSearchView(
    string QueryRef,
    IReadOnlyList<ComplianceAuditRowView> Rows,
    string ResultFingerprint,
    DateTimeOffset GeneratedAtUtc,
    string CorrelationId)
{
    public static ComplianceAuditSearchView Denied { get; } = new("denied", [], "sha256:denied", default, "denied");
}
