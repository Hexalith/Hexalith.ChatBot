using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Acceptance;

internal sealed class Story132AcceptanceRiskClassifier : IRiskClassifier
{
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web);
    private readonly DeterministicAiActionRiskClassifier _productionClassifier = new();

    public ValueTask<ChatBotRiskClassification> ClassifyAsync(
        ChatBotGatewayContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!string.Equals(
            context.Submission.Request.CommandType,
            nameof(ExecuteLowRiskAIAssistance),
            StringComparison.Ordinal))
        {
            return _productionClassifier.ClassifyAsync(context, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        JsonElement payload = context.Submission.Request.Command is JsonElement element
            ? element
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, ReadOptions);
        ExecuteLowRiskAIAssistance command = payload.Deserialize<ExecuteLowRiskAIAssistance>(ReadOptions)
            ?? throw new InvalidOperationException("The Story 13.2 acceptance execution command payload could not be read.");
        AiActionCommandMetadata metadata = AiActionCommandMetadataProvider.TryGet(
            AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName)
            ?? throw new InvalidOperationException("Low-risk assistance metadata is not configured.");

        AiActionRiskClassificationRecord record = AiActionRiskClassifier.Classify(new AiActionRiskInputTuple(
            metadata.CommandName,
            metadata.ActionClasses,
            metadata.EffectSurface,
            metadata.TenantPolicyClassification,
            "project-contributor",
            command.PolicySnapshotId,
            metadata.CommandAllowlistVersion,
            metadata.CommandDefaultRisk,
            "declared",
            "authorized",
            context.Submission.CorrelationId));
        ChatBotRiskClassification classification = ChatBotRiskClassification.Classified(record);
        context.SetRiskClassification(classification);
        return ValueTask.FromResult(classification);
    }
}
