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

    /// <summary>The UTC acceptance identity prepared before external dispatch.</summary>
    public DateTimeOffset? PreparedAcceptedAt { get; private set; }

    /// <summary>Whether dispatch has attempted an external write whose outcome may be uncertain.</summary>
    public bool ExternalEffectAttempted { get; private set; }

    /// <summary>Marks the boundary before invoking an external writer or EventStore submission.</summary>
    public void MarkExternalEffectAttempted() => ExternalEffectAttempted = true;

    /// <summary>Uses the same acceptance instant in dispatch evidence and the durable response.</summary>
    public void SetPreparedAcceptedAt(DateTimeOffset acceptedAt) => PreparedAcceptedAt = acceptedAt.ToUniversalTime();

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
