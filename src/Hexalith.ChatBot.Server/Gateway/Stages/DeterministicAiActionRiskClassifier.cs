using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class DeterministicAiActionRiskClassifier : IRiskClassifier
{
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web);

    public ValueTask<ChatBotRiskClassification> ClassifyAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(context.Submission.Request.CommandType, nameof(ProposeAIAction), StringComparison.Ordinal))
        {
            context.SetRiskClassification(ChatBotRiskClassification.PassThrough);
            return ValueTask.FromResult(ChatBotRiskClassification.PassThrough);
        }

        ProposeAIAction command = ReadCommand(context);
        AiActionCommandMetadata? knownMetadata = AiActionCommandMetadataProvider.TryGet(command.IntendedCommandName);
        bool metadataSupported = knownMetadata is not null;
        AiActionRiskInputTuple input = new(
            command.IntendedCommandName,
            command.ProposedActionClasses ?? knownMetadata?.ActionClasses ?? [],
            command.EffectSurface ?? knownMetadata?.EffectSurface,
            command.TenantPolicyClassification ?? knownMetadata?.TenantPolicyClassification,
            RequesterAuthorityClass(context.Actor.Principal),
            command.PolicySnapshotId,
            command.CommandAllowlistVersion ?? knownMetadata?.CommandAllowlistVersion,
            command.CommandDefaultRisk ?? knownMetadata?.CommandDefaultRisk,
            metadataSupported ? "declared" : "unsupported",
            "authorized",
            context.Submission.CorrelationId);

        ChatBotRiskClassification classification = ChatBotRiskClassification.Classified(AiActionRiskClassifier.Classify(input));
        context.SetRiskClassification(classification);
        return ValueTask.FromResult(classification);
    }

    private static ProposeAIAction ReadCommand(ChatBotGatewayContext context)
    {
        JsonElement command = context.Submission.Request.Command is JsonElement element
            ? element
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, ReadOptions);

        return command.Deserialize<ProposeAIAction>(ReadOptions)
            ?? throw new InvalidOperationException("The AI action proposal command payload could not be read.");
    }

    private static string? RequesterAuthorityClass(ClaimsPrincipal principal)
        => principal.Claims
            .FirstOrDefault(static claim =>
                string.Equals(claim.Type, "requester_authority_class", StringComparison.Ordinal) ||
                string.Equals(claim.Type, "requesterAuthorityClass", StringComparison.Ordinal))?
            .Value;
}
