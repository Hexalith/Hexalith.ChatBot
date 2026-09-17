using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The correlation-stamped result of an export run (AC1/AC3). <see cref="ManifestFingerprint"/> seals exactly the
/// <c>succeeded</c> includable classes — a failed/excluded class contributes no artifact.
/// </summary>
public sealed record TenantExportRunResult(
    string ExportRunId,
    string RunStatus,
    string ManifestFingerprint,
    IReadOnlyList<TenantExportClassResult> ClassResults,
    DateTimeOffset GeneratedAtUtc,
    string CorrelationId);
