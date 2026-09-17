using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Second-person approval that activates a pending AI-actor disable (FR75d). The approver MUST be a different
/// human from the proposer; this is re-checked in the aggregate as defense-in-depth.
/// </summary>
public sealed record ApproveAiActorDisable(
    string DisableChangeId,
    string AiActorRef,
    string ReasonCode,
    string PolicySnapshotId,
    AiActorControlState OldState,
    AiActorControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
