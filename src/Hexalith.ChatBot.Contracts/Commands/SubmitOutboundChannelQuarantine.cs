using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// First-person proposal to quarantine an outbound channel under the FR75d two-person rule — its future approved
/// sends are held for manual review for the tenant and fail closed at the outbound send seam (before the external
/// adapter call) until a policy administrator reviews and releases it. Pending drafts/approvals and all existing
/// records stay inspectable. Tenant authority is supplied by the authenticated gateway binding, never the command
/// body. Carries only safe, finite, metadata-only tokens — never recipient/sender addresses, message content,
/// credentials, OAuth grant fingerprints, model prompts/completions, or delegated-user PII. The subject is identified
/// by its safe channel ref (the <see cref="OutboundChannelRef"/>), a finite stable identifier. Mirrors
/// <see cref="SubmitOutboundChannelDisable"/> (Story 7.24), substituting <c>Quarantine</c>/<c>Quarantined</c> for
/// <c>Disable</c>/<c>Disabled</c> and reusing <see cref="OutboundChannelControlSchemaVersions.V1"/> — the command
/// shape is identical to disable (the Story 7.22 disable→quarantine substitution precedent).
/// </summary>
public sealed record SubmitOutboundChannelQuarantine(
    string QuarantineChangeId,
    string OutboundChannelRef,
    string ReasonCode,
    string PolicySnapshotId,
    OutboundChannelControlState OldState,
    OutboundChannelControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
