namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>
/// Problem type URIs that something outside the gateway matches on.
/// </summary>
/// <remarks>
/// The Tier-3 mailbox admission probe proves admission by recognising the dispatch-unavailable type, which is
/// emitted only from the accepted branch of the command gateway. Holding that URI as a second literal in the test
/// harness meant changing it here would silently stop the probe recognising admission, with no failing test.
/// </remarks>
internal static class ChatBotProblemTypes
{
    /// <summary>Emitted only after admission accepted the caller, when dispatch could not be completed.</summary>
    public const string DispatchUnavailable = "https://hexalith.dev/errors/chatbot/dispatch-unavailable";
}
