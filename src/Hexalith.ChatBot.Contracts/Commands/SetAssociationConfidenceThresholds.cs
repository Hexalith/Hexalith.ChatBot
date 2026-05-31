namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Security-sensitive tenant policy update for association confidence thresholds.
/// </summary>
public sealed record SetAssociationConfidenceThresholds(
    string PolicyId,
    double THigh,
    double TLow,
    string PolicyVersion,
    string? EvaluationRunReference,
    DateTimeOffset? ChangedAt) : IChatBotCommand;
