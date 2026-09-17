using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal sealed record AiActionPolicyEvaluationRequest(
    string TenantId,
    string ProjectId,
    string ProposalId,
    string ContextPackageId,
    string ContextPackageVersion,
    string? RequestedPolicySnapshotId,
    AiActionRiskClass RiskClass,
    IReadOnlyList<string> RiskActionClasses,
    string EffectSurface,
    string AssistanceKind,
    bool HasProjectAuthorization);
