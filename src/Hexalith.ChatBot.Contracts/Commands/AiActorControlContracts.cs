using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74 AI-actor control (disable, quarantine) commands.
/// </summary>
public static class AiActorControlSchemaVersions
{
    public const string V1 = "ai-actor-control-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// First-person proposal to disable an AI actor under the FR75d two-person rule. Tenant and actor authority are
/// supplied by the authenticated gateway binding, never the command body. Carries only safe, finite,
/// metadata-only tokens — never service-client/AI credentials, OAuth grant fingerprints, model
/// prompts/completions, delegated-user PII, or addresses. The subject is identified by its safe
/// <c>ServiceClientId</c> (the AI actor's <see cref="AiActorRef"/>).
/// </summary>
public sealed record SubmitAiActorDisable(
    string DisableChangeId,
    string AiActorRef,
    string ReasonCode,
    string PolicySnapshotId,
    AiActorControlState OldState,
    AiActorControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

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

/// <summary>
/// First-person proposal to quarantine an AI actor under the FR75d two-person rule — its new proposals and
/// commands enter a review-only/contained state and fail closed at the command-admission pipeline until the
/// quarantine is cleared. Tenant and actor authority are supplied by the authenticated gateway binding, never
/// the command body. Carries only safe, finite, metadata-only tokens — never service-client/AI credentials,
/// OAuth grant fingerprints, model prompts/completions, delegated-user PII, or addresses. The subject is
/// identified by its safe <c>ServiceClientId</c> (the AI actor's <see cref="AiActorRef"/>). Reuses
/// <see cref="AiActorControlSchemaVersions.V1"/> — the command shape is identical to the disable pair.
/// </summary>
public sealed record SubmitAiActorQuarantine(
    string QuarantineChangeId,
    string AiActorRef,
    string ReasonCode,
    string PolicySnapshotId,
    AiActorControlState OldState,
    AiActorControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Second-person approval that activates a pending AI-actor quarantine (FR75d). The approver MUST be a different
/// human from the proposer; this is re-checked in the aggregate as defense-in-depth.
/// </summary>
public sealed record ApproveAiActorQuarantine(
    string QuarantineChangeId,
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
