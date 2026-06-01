using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed record ChatBotGatewayContext(
    ChatBotCommandSubmission Submission,
    ChatBotAuthenticatedActor Actor,
    ChatBotTenantBinding TenantBinding,
    ServiceClientGrantEvidence? ServiceClientGrantEvidence = null)
{
    public CoarseIdempotencyMetadata? Idempotency { get; private set; }

    public ChatBotRiskClassification? RiskClassification { get; private set; }

    public ChatBotApprovalResult? ApprovalResult { get; private set; }

    public void SetIdempotency(CoarseIdempotencyMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        Idempotency = metadata;
    }

    public void SetRiskClassification(ChatBotRiskClassification classification)
    {
        ArgumentNullException.ThrowIfNull(classification);
        RiskClassification = classification;
    }

    public void SetApprovalResult(ChatBotApprovalResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ApprovalResult = result;
    }
}
