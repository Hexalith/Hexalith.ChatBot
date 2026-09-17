using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

/// <summary>
/// The per-command idempotency contract for AI-action command execution (addendum §Idempotency Keys). The key
/// template, window, and duplicate-resolution behaviour are fixed safe tokens — never tenant-supplied.
/// </summary>
/// <param name="KeyTemplate">The idempotency-key composition for command execution.</param>
/// <param name="WindowSeconds">The dedup window in whole seconds (integer, never a float).</param>
/// <param name="OnDuplicate">The resolution applied to a duplicate within the window.</param>
internal sealed record AiActionIdempotencyContract(
    string KeyTemplate,
    int WindowSeconds,
    AiActionIdempotencyResolution OnDuplicate)
{
    /// <summary>
    /// Command-execution idempotency contract: <c>tenant_id + command_name + command_input_hash + requester_id</c>,
    /// 60-second window, "return prior outcome; do not re-execute" (addendum §Idempotency Keys).
    /// </summary>
    public static AiActionIdempotencyContract CommandExecution { get; } = new(
        "tenant_id+command_name+command_input_hash+requester_id",
        60,
        AiActionIdempotencyResolution.ReturnPriorOutcome);
}
