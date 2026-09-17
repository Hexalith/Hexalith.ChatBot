using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// A single underlying approval item in a batch approve/reject request, paired with the upstream per-item authority
/// resolution (<see cref="ReviewerHasAuthority"/>) drawn from the same scope/gate signal the per-item
/// <see cref="AiActionApprovalGate"/> enforces. All fields are safe refs/metadata — never approval content.
/// </summary>
internal sealed record BatchDecisionItem(
    BatchDecisionItemKind Kind,
    string ApprovalId,
    string ProjectId,
    long ExpectedApprovalSourceVersion,
    bool ReviewerHasAuthority,
    string CorrelationId,
    string DecisionId,
    string? ProposalId = null,
    string? SourceMessageId = null,
    string? DraftId = null);
