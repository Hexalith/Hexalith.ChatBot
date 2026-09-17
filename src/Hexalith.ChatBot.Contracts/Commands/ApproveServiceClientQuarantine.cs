using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Second-person approval that activates a pending service-client quarantine (FR75d). The approver MUST be a
/// different human from the proposer; this is re-checked in the aggregate as defense-in-depth.
/// </summary>
public sealed record ApproveServiceClientQuarantine(
    string QuarantineChangeId,
    string ServiceClientRef,
    string ReasonCode,
    string PolicySnapshotId,
    ServiceClientControlState OldState,
    ServiceClientControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
