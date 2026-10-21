using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Supplies a synthetic low-risk tuple so admission tests exercise policy resolution.</summary>
internal sealed class SyntheticLowRiskPolicyRiskClassifier : IRiskClassifier
{
    /// <summary>Classifies fixture authority without modifying the production trusted-principal path.</summary>
    public ValueTask<ChatBotRiskClassification> ClassifyAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ExecuteLowRiskAIAssistance command = (ExecuteLowRiskAIAssistance)context.Submission.Request.Command;
        AiActionRiskInputTuple input = new(AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName, [], "read-only", "low-risk", "project-contributor",
            command.PolicySnapshotId, "synthetic-allowlist-v1", AiActionRiskClass.LowRisk, "declared", "authorized", context.Submission.CorrelationId);
        return ValueTask.FromResult(ChatBotRiskClassification.Classified(AiActionRiskClassifier.Classify(input)));
    }
}
