using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record EscalationPolicyValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors)
{
    public static EscalationPolicyValidationResult Valid { get; } = new(true, []);

    public static EscalationPolicyValidationResult Invalid(params string[] errors)
        => new(false, errors);
}
