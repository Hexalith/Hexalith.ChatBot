using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>
/// Hardcoded M0 spine allowlist containing exactly the single trivial governed command
/// (<see cref="RecordGovernedNote"/>). Every other command type is rejected fail-closed at the
/// gateway. This is deliberately distinct from the addendum's AI-action execution allowlist
/// (<c>Project.AppendConversationMessage</c>, Epic 4) and does not alter it.
/// </summary>
internal sealed class ChatBotSpineCommandAllowlist : ISpineCommandAllowlist
{
    private static readonly HashSet<string> AllowedCommandTypes =
        new(StringComparer.Ordinal) { nameof(RecordGovernedNote) };

    public bool IsAllowed(string? commandType)
        => !string.IsNullOrWhiteSpace(commandType) && AllowedCommandTypes.Contains(commandType);
}
