using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Second-person approval that activates a pending outbound-channel quarantine (FR75d). The approver MUST be a
/// different human from the proposer; this is re-checked in the aggregate as defense-in-depth. Mirrors
/// <see cref="ApproveOutboundChannelDisable"/>.
/// </summary>
public sealed record ApproveOutboundChannelQuarantine(
    string QuarantineChangeId,
    string OutboundChannelRef,
    string ReasonCode,
    string PolicySnapshotId,
    OutboundChannelControlState OldState,
    OutboundChannelControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
