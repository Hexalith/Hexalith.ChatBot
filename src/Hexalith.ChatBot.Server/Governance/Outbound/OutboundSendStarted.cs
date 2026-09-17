using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

public sealed record OutboundSendStarted(
    string SendId,
    string SendKey,
    string ApprovalId,
    string DraftId,
    string ProjectId,
    string RequesterId,
    string SendActorId,
    SenderAuthorityClass SenderAuthorityClass,
    SenderAuthorityClassificationResult AuthorityResult,
    string AdapterMode,
    string AdapterRef,
    long ExpectedApprovalSourceVersion,
    long ExpectedDraftSourceVersion,
    DateTimeOffset StartedAtUtc,
    string CorrelationId,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.outbound-send-started.v1") : IEventPayload;
