using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record AiActionApprovalRequested(
    string ApprovalId,
    string ProjectId,
    string ProposalId,
    string TaskIntentId,
    string SourceMessageId,
    string? SourceConversationItemId,
    string RequesterId,
    string RequesterActorType,
    DateTimeOffset RequestedAtUtc,
    string CommandName,
    string CommandAllowlistVersion,
    AiActionRiskClass AiRiskClass,
    IReadOnlyList<string> AiRiskActionClasses,
    string RiskInputTuple,
    string PolicySnapshotId,
    string PolicySnapshotVisibility,
    IReadOnlyList<string> EvidenceReferences,
    IReadOnlyList<ApprovalEvidenceFreshness> EvidenceFreshnessStates,
    IReadOnlyList<string> AffectedResourceReferences,
    IReadOnlyList<string> RecipientReferences,
    string SenderAuthorityClass,
    string ExpectedPostStateRedactionState,
    string ActionSummaryRedactionState,
    long SourceVersion,
    string CorrelationId,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.ai-action-approval-requested.v1") : IEventPayload;
