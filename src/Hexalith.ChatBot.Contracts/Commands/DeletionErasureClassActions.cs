using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed per-class action dimension (AC1). Each value is an <c>AuditMetadata</c>-safe bounded token. Mirrors
/// the <see cref="TenantExportClassDispositions"/> shape line-for-line. The action is keyed off the class's
/// <see cref="DataClassDeletionBehaviors"/>: <c>key-shred</c>⇒<see cref="CryptoShredded"/>;
/// <c>projection-tombstone</c>⇒<see cref="Tombstoned"/>; <c>hard-delete</c>⇒<see cref="HardDeleted"/>;
/// <c>retain-immutable</c> (and every fail-closed/unauthorized case)⇒<see cref="Retained"/>.
/// </summary>
public static class DeletionErasureClassActions
{
    public const string CryptoShredded = "crypto-shredded";
    public const string Tombstoned = "tombstoned";
    public const string HardDeleted = "hard-deleted";
    public const string Retained = "retained";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([CryptoShredded, Tombstoned, HardDeleted, Retained], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
