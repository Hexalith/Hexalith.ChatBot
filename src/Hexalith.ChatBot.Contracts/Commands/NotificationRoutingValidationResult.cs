using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record NotificationRoutingValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors)
{
    public static NotificationRoutingValidationResult Valid { get; } = new(true, []);

    public static NotificationRoutingValidationResult Invalid(params string[] errors)
        => new(false, errors);
}
