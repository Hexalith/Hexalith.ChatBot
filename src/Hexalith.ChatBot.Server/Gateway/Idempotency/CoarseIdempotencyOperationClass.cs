namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal sealed record CoarseIdempotencyOperationClass(
    string Code,
    TimeSpan? ReplayWindow,
    string ConflictCode)
{
    public static CoarseIdempotencyOperationClass CommandExecution { get; } = new(
        "command-execution",
        TimeSpan.FromSeconds(60),
        "idempotency_conflict_command_execution");

    public static IReadOnlyList<CoarseIdempotencyOperationClass> All { get; } =
    [
        new("message-intake", null, "idempotency_conflict_message_intake"),
        new("association-decision", TimeSpan.FromHours(24), "idempotency_conflict_association_decision"),
        new("approval-decision", TimeSpan.FromHours(24), "idempotency_conflict_approval_decision"),
        CommandExecution,
        new("outbound-send", null, "idempotency_conflict_outbound_send"),
        new("ai-action-proposal", TimeSpan.FromMinutes(5), "idempotency_conflict_ai_action_proposal"),
        new("correction", null, "idempotency_conflict_correction"),
        new("retry", null, "idempotency_conflict_retry"),
    ];
}
