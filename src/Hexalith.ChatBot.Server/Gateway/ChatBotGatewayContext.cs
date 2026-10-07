using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed record ChatBotGatewayContext(
    ChatBotCommandSubmission Submission,
    ChatBotAuthenticatedActor Actor,
    ChatBotTenantBinding TenantBinding,
    ServiceClientGrantEvidence? ServiceClientGrantEvidence = null,
    IReadOnlyList<string>? AuthorityEvidenceReferences = null)
{
    private Func<string, CancellationToken, ValueTask<bool>>? _dispatchTargetBinding;

    public CoarseIdempotencyMetadata? Idempotency { get; private set; }

    public ChatBotRiskClassification? RiskClassification { get; private set; }

    public ChatBotApprovalResult? ApprovalResult { get; private set; }

    /// <summary>The UTC acceptance identity prepared before external dispatch.</summary>
    public DateTimeOffset? PreparedAcceptedAt { get; private set; }

    /// <summary>The aggregate target durably bound to this prepared dispatch ownership.</summary>
    public string? PreparedAggregateId { get; private set; }

    /// <summary>Whether the actual SDK submission returned successfully for this bound dispatch.</summary>
    public bool SdkSubmissionAccepted { get; private set; }

    /// <summary>Observes SDK acceptance only after the bound command's submission has returned successfully.</summary>
    public void MarkSdkSubmissionAccepted()
    {
        if (string.IsNullOrWhiteSpace(PreparedAggregateId))
        {
            throw new InvalidOperationException("SDK acceptance requires a bound prepared dispatch target.");
        }

        SdkSubmissionAccepted = true;
    }

    /// <summary>Installs the gateway's prepared-outcome and owner-fenced target binding.</summary>
    public void SetDispatchTargetBinding(Func<string, CancellationToken, ValueTask<bool>> binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        _dispatchTargetBinding = binding;
    }

    /// <summary>Requires durable target binding before the dispatch plan can be submitted to EventStore.</summary>
    public async ValueTask<bool> BindDispatchTargetAsync(string aggregateId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(aggregateId) || _dispatchTargetBinding is null ||
            !await _dispatchTargetBinding(aggregateId, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        PreparedAggregateId = aggregateId;
        return true;
    }

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
