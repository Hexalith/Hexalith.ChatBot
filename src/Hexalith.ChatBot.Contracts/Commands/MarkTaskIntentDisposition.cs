namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record MarkTaskIntentDisposition(
    string ProjectId,
    string TaskIntentId,
    string SourceMessageId,
    string Disposition,
    long ExpectedSourceVersion,
    IReadOnlyList<string> EvidenceReferences,
    string? PolicySnapshotId,
    string CorrelationId,
    string TransitionId,
    string? PredecessorTaskIntentId = null,
    string? ReasonCode = null) : IChatBotCommand;
