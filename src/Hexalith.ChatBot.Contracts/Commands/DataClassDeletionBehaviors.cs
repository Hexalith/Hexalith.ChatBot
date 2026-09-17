using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed deletion-behavior dimension (architecture cross-cutting #13, WORM-vs-erasure). <c>audit-records</c>
/// must never be <see cref="HardDelete"/> — erasure over the immutable chain is projection-tombstone + key-shred.
/// </summary>
public static class DataClassDeletionBehaviors
{
    public const string KeyShred = "key-shred";
    public const string ProjectionTombstone = "projection-tombstone";
    public const string HardDelete = "hard-delete";
    public const string RetainImmutable = "retain-immutable";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([KeyShred, ProjectionTombstone, HardDelete, RetainImmutable], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
