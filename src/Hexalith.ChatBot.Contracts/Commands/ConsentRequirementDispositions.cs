using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed requirement-disposition dimension (AC4). A subject kind either <see cref="Required"/> (a basis must be
/// recorded before a governed action proceeds) or <see cref="NotRequired"/>. An unknown/missing entry biases to
/// <see cref="Required"/> in <see cref="ConsentRequirementPolicy"/> (fail-closed).
/// </summary>
public static class ConsentRequirementDispositions
{
    public const string Required = "required";
    public const string NotRequired = "not-required";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Required, NotRequired], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
