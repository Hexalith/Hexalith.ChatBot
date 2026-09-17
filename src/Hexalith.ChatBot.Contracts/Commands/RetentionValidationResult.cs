using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record RetentionValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors)
{
    public static RetentionValidationResult Valid { get; } = new(true, []);

    public static RetentionValidationResult Invalid(params string[] errors)
        => new(false, errors);
}
