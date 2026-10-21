using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Returns a synthetic well-formed classification to observe pipeline sequencing only.</summary>
internal sealed class SyntheticObservedAdmissionRiskClassifier(List<string> calls) : IRiskClassifier
{
    /// <inheritdoc />
    public ValueTask<ChatBotRiskClassification> ClassifyAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        context.RiskClassification.ShouldBeNull();
        context.ApprovalResult.ShouldBeNull();
        context.AdmissionEnvelope.ShouldBeNull();
        calls.Add("classify");
        return ValueTask.FromResult(ChatBotRiskClassification.Classified(AiActionRiskClassifier.Classify(new(
            AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName, [], null, null, "project-contributor", "synthetic-policy",
            null, null, null, "authorized", context.Submission.CorrelationId))));
    }
}
