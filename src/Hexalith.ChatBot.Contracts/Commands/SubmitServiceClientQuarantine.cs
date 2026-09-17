using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// First-person proposal to quarantine (contain for review) a service client under the FR75d two-person rule.
/// Tenant and actor authority are supplied by the authenticated gateway binding, never the command body. Carries
/// only safe, finite, metadata-only tokens — never service-client credentials, OAuth grant fingerprints,
/// delegated-user PII, or addresses. The subject is identified by its safe <c>ServiceClientId</c>. The command
/// shape mirrors <see cref="SubmitServiceClientDisable"/> exactly and reuses
/// <see cref="ServiceClientControlSchemaVersions.V1"/>; the FR74 control state differs (Active→Quarantined).
/// </summary>
public sealed record SubmitServiceClientQuarantine(
    string QuarantineChangeId,
    string ServiceClientRef,
    string ReasonCode,
    string PolicySnapshotId,
    ServiceClientControlState OldState,
    ServiceClientControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
