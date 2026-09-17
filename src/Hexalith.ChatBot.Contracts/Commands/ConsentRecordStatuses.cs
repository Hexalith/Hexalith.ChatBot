using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed consent-record status dimension (AC1/AC4). Only <see cref="Active"/> satisfies a <c>required</c> gate;
/// <see cref="Withdrawn"/>/<see cref="Expired"/>/<see cref="Superseded"/> never do (the AC4 fail-closed invariant).
/// </summary>
public static class ConsentRecordStatuses
{
    public const string Active = "active";
    public const string Withdrawn = "withdrawn";
    public const string Expired = "expired";
    public const string Superseded = "superseded";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Active, Withdrawn, Expired, Superseded], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
