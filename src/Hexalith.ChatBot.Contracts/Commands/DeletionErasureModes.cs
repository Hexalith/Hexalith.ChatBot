using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed request-mode dimension (AC1/AC3). <see cref="Erasure"/> additionally runs audit-chain erasure through
/// the existing Story 9.1 <c>AuditRedactionService</c> seam. Mirrors <see cref="TenantExportClassDispositions"/>.
/// </summary>
public static class DeletionErasureModes
{
    public const string Deletion = "deletion";
    public const string Erasure = "erasure";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Deletion, Erasure], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
