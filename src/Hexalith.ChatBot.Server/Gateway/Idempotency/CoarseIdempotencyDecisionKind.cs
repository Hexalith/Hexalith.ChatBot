using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal enum CoarseIdempotencyDecisionKind
{
    Proceed,
    ReplayPriorOutcome,
    Conflict,
}
