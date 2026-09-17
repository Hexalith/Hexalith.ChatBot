using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record TenantPolicyValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors)
{
    public static TenantPolicyValidationResult Valid { get; } = new(true, []);

    public static TenantPolicyValidationResult Invalid(params string[] errors)
        => new(false, errors);
}
