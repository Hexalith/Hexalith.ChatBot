using System.Net.Http.Json;
using System.Text.Json;

namespace Hexalith.ChatBot.Client;

/// <summary>A metadata-only compliance audit query sent to the S9 investigation search endpoint (Story 9.3).</summary>
public sealed record ComplianceAuditQuery(
    string QueryRef,
    IReadOnlyList<ComplianceAuditFilter> Filters,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int Limit);
