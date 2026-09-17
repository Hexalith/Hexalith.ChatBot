using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// First-person proposal to quarantine a command capability under the FR75d two-person rule — its future
/// submissions are paused (contained for review) for the tenant and fail closed at the command-admission pipeline
/// until the quarantine is cleared. Mirrors <see cref="SubmitCommandCapabilityDisable"/> (Story 7.21) with the
/// quarantine control-state substituted for disable (the Story 7.19 disable→quarantine precedent), reusing
/// <see cref="CommandCapabilityControlSchemaVersions.V1"/> because the command shape is identical. Tenant
/// authority is supplied by the authenticated gateway binding, never the command body. Carries only safe, finite,
/// metadata-only tokens — never credentials, OAuth grant fingerprints, model prompts/completions, delegated-user
/// PII, or addresses. The subject is identified by its safe command type name (the
/// <see cref="CommandCapabilityRef"/>), a finite stable identifier.
/// </summary>
public sealed record SubmitCommandCapabilityQuarantine(
    string QuarantineChangeId,
    string CommandCapabilityRef,
    string ReasonCode,
    string PolicySnapshotId,
    CommandCapabilityControlState OldState,
    CommandCapabilityControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
