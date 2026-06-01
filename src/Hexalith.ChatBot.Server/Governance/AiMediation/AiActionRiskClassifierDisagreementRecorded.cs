using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record AiActionRiskClassifierDisagreementRecorded(
    string ProposalId,
    string ReviewerActorId,
    string ReviewerDecision,
    string Resolution,
    AiActionRiskClass Classification,
    string ClassifierVersion,
    AiActionRiskInputTuple InputTuple,
    string CorrelationId,
    string? PolicySnapshotId,
    DateTimeOffset RecordedAtUtc,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.ai-action-risk-classifier-disagreement.v1") : IEventPayload;
