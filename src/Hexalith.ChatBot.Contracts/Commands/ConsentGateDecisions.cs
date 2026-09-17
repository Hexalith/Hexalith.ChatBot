using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed gate-decision dimension (AC4). <see cref="ConsentGate"/> returns <see cref="Satisfied"/> when a
/// governed action may proceed and <see cref="BlockedMissingBasis"/> when it must fail closed pending an active basis.
/// </summary>
public static class ConsentGateDecisions
{
    public const string Satisfied = "satisfied";
    public const string BlockedMissingBasis = "blocked-missing-basis";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Satisfied, BlockedMissingBasis], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
