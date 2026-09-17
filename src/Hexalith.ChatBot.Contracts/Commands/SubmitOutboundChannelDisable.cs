using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// First-person proposal to disable an outbound channel under the FR75d two-person rule — its future approved sends
/// are blocked for the tenant and fail closed at the outbound send seam (before the external adapter call) until the
/// channel is re-enabled. Pending drafts/approvals and all existing records stay inspectable. Tenant authority is
/// supplied by the authenticated gateway binding, never the command body. Carries only safe, finite, metadata-only
/// tokens — never recipient/sender addresses, message content, credentials, OAuth grant fingerprints, model
/// prompts/completions, or delegated-user PII. The subject is identified by its safe channel ref (the
/// <see cref="OutboundChannelRef"/>), a finite stable identifier. Mirrors <see cref="SubmitCommandCapabilityDisable"/>
/// (Story 7.21), retargeted from the command-type subject to the outbound-channel subject.
/// </summary>
public sealed record SubmitOutboundChannelDisable(
    string DisableChangeId,
    string OutboundChannelRef,
    string ReasonCode,
    string PolicySnapshotId,
    OutboundChannelControlState OldState,
    OutboundChannelControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
