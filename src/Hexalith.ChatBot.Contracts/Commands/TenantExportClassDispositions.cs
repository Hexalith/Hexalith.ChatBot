using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed per-class disposition dimension (AC1). Each value is an <c>AuditMetadata</c>-safe bounded token.
/// Mirrors the <see cref="DataClassExportEligibilities"/> shape line-for-line.
/// </summary>
public static class TenantExportClassDispositions
{
    public const string Included = "included";
    public const string Redacted = "redacted";
    public const string Excluded = "excluded";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Included, Redacted, Excluded], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
