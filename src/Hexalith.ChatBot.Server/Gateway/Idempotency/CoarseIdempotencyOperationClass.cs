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

    public static CoarseIdempotencyOperationClass MessageIntake { get; } = new(
        "message-intake",
        null,
        "idempotency_conflict_message_intake");

    public static CoarseIdempotencyOperationClass ParticipantResolution { get; } = new(
        "participant-resolution",
        null,
        "idempotency_conflict_participant_resolution");

    public static CoarseIdempotencyOperationClass AssociationScoring { get; } = new(
        "association-scoring",
        null,
        "idempotency_conflict_association_scoring");

    public static CoarseIdempotencyOperationClass AssociationThresholdPolicy { get; } = new(
        "association-threshold-policy",
        null,
        "idempotency_conflict_association_threshold_policy");

    public static CoarseIdempotencyOperationClass AssociationDecision { get; } = new(
        "association-decision",
        TimeSpan.FromHours(24),
        "idempotency_conflict_association_decision");

    public static CoarseIdempotencyOperationClass Correction { get; } = new(
        "correction",
        null,
        "idempotency_conflict_correction");

    public static IReadOnlyList<CoarseIdempotencyOperationClass> All { get; } =
    [
        MessageIntake,
        ParticipantResolution,
        AssociationScoring,
        AssociationThresholdPolicy,
        AssociationDecision,
        new("approval-decision", TimeSpan.FromHours(24), "idempotency_conflict_approval_decision"),
        CommandExecution,
        new("outbound-send", null, "idempotency_conflict_outbound_send"),
        new("ai-action-proposal", TimeSpan.FromMinutes(5), "idempotency_conflict_ai_action_proposal"),
        Correction,
        new("retry", null, "idempotency_conflict_retry"),
    ];

    public static string ConflictCodeFor(string operationClass)
        => All.FirstOrDefault(candidate => string.Equals(candidate.Code, operationClass, StringComparison.Ordinal))?.ConflictCode
            ?? CommandExecution.ConflictCode;
}
