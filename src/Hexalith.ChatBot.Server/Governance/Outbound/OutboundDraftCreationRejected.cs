using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

public sealed record OutboundDraftCreationRejected(
    string DraftId,
    string ProjectId,
    string RequesterId,
    string ReasonCode,
    string CorrelationId,
    string? PolicySnapshotId = null,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input") : IRejectionEvent;
