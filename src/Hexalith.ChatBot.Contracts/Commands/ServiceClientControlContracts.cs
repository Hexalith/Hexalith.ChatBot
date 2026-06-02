using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74 service-client control (disable/quarantine) commands.
/// </summary>
public static class ServiceClientControlSchemaVersions
{
    public const string V1 = "service-client-control-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// First-person proposal to disable a service client under the FR75d two-person rule. Tenant and actor
/// authority are supplied by the authenticated gateway binding, never the command body. Carries only safe,
/// finite, metadata-only tokens — never service-client credentials, OAuth grant fingerprints, delegated-user
/// PII, or addresses. The subject is identified by its safe <c>ServiceClientId</c>.
/// </summary>
public sealed record SubmitServiceClientDisable(
    string DisableChangeId,
    string ServiceClientRef,
    string ReasonCode,
    string PolicySnapshotId,
    ServiceClientControlState OldState,
    ServiceClientControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Second-person approval that activates a pending service-client disable (FR75d). The approver MUST be a
/// different human from the proposer; this is re-checked in the aggregate as defense-in-depth.
/// </summary>
public sealed record ApproveServiceClientDisable(
    string DisableChangeId,
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
