using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

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
