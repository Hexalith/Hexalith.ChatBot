using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

/// <summary>How a duplicate command-execution within the idempotency window is resolved. Closed token set.</summary>
internal enum AiActionIdempotencyResolution
{
    /// <summary>Return the prior outcome; do not re-execute (addendum §Idempotency Keys).</summary>
    ReturnPriorOutcome,
}
