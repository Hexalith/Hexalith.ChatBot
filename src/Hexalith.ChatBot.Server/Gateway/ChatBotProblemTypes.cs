namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>
/// Canonical problem type URIs emitted by the gateway.
/// </summary>
/// <remarks>
/// The Tier-3 mailbox admission probe proves admission by recognising the dispatch-unavailable type, which is
/// emitted only from the accepted branch of the command gateway. Keeping the complete URI vocabulary together
/// prevents producers, tests, and any external discriminator from drifting through duplicated literals.
/// </remarks>
internal static class ChatBotProblemTypes
{
    /// <summary>Authorization or authentication was denied.</summary>
    public const string AuthorizationDenied = "https://hexalith.dev/errors/chatbot/authorization-denied";

    /// <summary>A required audit write was unavailable.</summary>
    public const string AuditUnavailable = "https://hexalith.dev/errors/chatbot/audit-unavailable";

    /// <summary>Emitted only after admission accepted the caller, when dispatch could not be completed.</summary>
    public const string DispatchUnavailable = "https://hexalith.dev/errors/chatbot/dispatch-unavailable";

    /// <summary>A command identifier was reused for different content.</summary>
    public const string IdempotencyConflict = "https://hexalith.dev/errors/chatbot/idempotency-conflict";

    /// <summary>A requested lifecycle transition is invalid.</summary>
    public const string InvalidLifecycleTransition = "https://hexalith.dev/errors/chatbot/invalid-lifecycle-transition";

    /// <summary>The command type is outside the caller's allowlist.</summary>
    public const string CommandNotAllowlisted = "https://hexalith.dev/errors/chatbot/command-not-allowlisted";
}
