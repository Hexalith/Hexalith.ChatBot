using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record MailboxConfigurationValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors)
{
    public static MailboxConfigurationValidationResult Valid { get; } = new(true, []);

    public static MailboxConfigurationValidationResult Invalid(params string[] errors)
        => new(false, errors);
}
