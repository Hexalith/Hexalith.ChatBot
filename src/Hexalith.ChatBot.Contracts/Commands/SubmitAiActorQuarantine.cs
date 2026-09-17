using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

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
