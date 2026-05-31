namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>
/// Mechanical M0 spine guardrail: decides whether a declared command type may traverse the command
/// spine at all. Enforced fail-closed by <see cref="CommandGateway"/> before any durable-state work.
/// This is the spine allowlist (orthogonal to the AI-action execution allowlist of the addendum).
/// </summary>
internal interface ISpineCommandAllowlist
{
    /// <summary>
    /// Returns <see langword="true"/> only when the declared command type is admitted to the spine.
    /// </summary>
    /// <param name="commandType">The adapter-declared command type token.</param>
    bool IsAllowed(string? commandType);
}
