using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74 command-capability control (disable) commands. A dedicated constant —
/// not <see cref="AiActorControlSchemaVersions.V1"/> — because the subject diverges from the AI-actor control
/// plane: the command capability is identified by its safe command <em>type name</em>
/// (<c>CommandCapabilityRef</c>), not an actor id. Mirrors <see cref="AiActorControlSchemaVersions"/>.
/// </summary>
public static class CommandCapabilityControlSchemaVersions
{
    public const string V1 = "command-capability-control-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// First-person proposal to disable a command capability under the FR75d two-person rule — its future
/// submissions are removed from admitted execution for the tenant and fail closed at the command-admission
/// pipeline until the capability is re-enabled. Tenant authority is supplied by the authenticated gateway
/// binding, never the command body. Carries only safe, finite, metadata-only tokens — never credentials, OAuth
/// grant fingerprints, model prompts/completions, delegated-user PII, or addresses. The subject is identified by
/// its safe command type name (the <see cref="CommandCapabilityRef"/>), a finite stable identifier.
/// </summary>
public sealed record SubmitCommandCapabilityDisable(
    string DisableChangeId,
    string CommandCapabilityRef,
    string ReasonCode,
    string PolicySnapshotId,
    CommandCapabilityControlState OldState,
    CommandCapabilityControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Second-person approval that activates a pending command-capability disable (FR75d). The approver MUST be a
/// different human from the proposer; this is re-checked in the aggregate as defense-in-depth.
/// </summary>
public sealed record ApproveCommandCapabilityDisable(
    string DisableChangeId,
    string CommandCapabilityRef,
    string ReasonCode,
    string PolicySnapshotId,
    CommandCapabilityControlState OldState,
    CommandCapabilityControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
