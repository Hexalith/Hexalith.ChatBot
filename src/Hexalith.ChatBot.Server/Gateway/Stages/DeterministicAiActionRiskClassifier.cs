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

        if (!string.Equals(context.Submission.Request.CommandType, nameof(ProposeAIAction), StringComparison.Ordinal) &&
            !string.Equals(context.Submission.Request.CommandType, nameof(ExecuteLowRiskAIAssistance), StringComparison.Ordinal) &&
            !string.Equals(context.Submission.Request.CommandType, nameof(ExecuteApprovedAIAction), StringComparison.Ordinal))
        {
            context.SetRiskClassification(ChatBotRiskClassification.PassThrough);
            return ValueTask.FromResult(ChatBotRiskClassification.PassThrough);
        }

        AiActionRiskInputTuple input = context.Submission.Request.CommandType switch
        {
            nameof(ExecuteLowRiskAIAssistance) => BuildExecutionInput(context),
            nameof(ExecuteApprovedAIAction) => BuildApprovedExecutionInput(context),
            _ => BuildProposalInput(context),
        };

        ChatBotRiskClassification classification = ChatBotRiskClassification.Classified(AiActionRiskClassifier.Classify(input));
        context.SetRiskClassification(classification);
        return ValueTask.FromResult(classification);
    }

    private static AiActionRiskInputTuple BuildProposalInput(ChatBotGatewayContext context)
    {
        ProposeAIAction command = ReadProposalCommand(context);
        AiActionCommandMetadata? knownMetadata = AiActionCommandMetadataProvider.TryGet(command.IntendedCommandName);
        bool metadataSupported = knownMetadata is not null;
        return new AiActionRiskInputTuple(
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
    }

    private static AiActionRiskInputTuple BuildExecutionInput(ChatBotGatewayContext context)
    {
        ExecuteLowRiskAIAssistance command = ReadExecutionCommand(context);
        AiActionCommandMetadata metadata = AiActionCommandMetadataProvider.TryGet(AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName)
            ?? throw new InvalidOperationException("Low-risk assistance metadata is not configured.");

        return new AiActionRiskInputTuple(
            metadata.CommandName,
            metadata.ActionClasses,
            metadata.EffectSurface,
            metadata.TenantPolicyClassification,
            RequesterAuthorityClass(context.Actor.Principal),
            command.PolicySnapshotId,
            metadata.CommandAllowlistVersion,
            metadata.CommandDefaultRisk,
            "declared",
            "authorized",
            context.Submission.CorrelationId);
    }

    private static AiActionRiskInputTuple BuildApprovedExecutionInput(ChatBotGatewayContext context)
    {
        ExecuteApprovedAIAction command = ReadApprovedExecutionCommand(context);
        AiActionCommandMetadata? metadata = AiActionCommandMetadataProvider.TryGet(command.CommandName);
        bool metadataSupported = metadata is not null;

        return new AiActionRiskInputTuple(
            command.CommandName,
            metadata?.ActionClasses ?? [],
            metadata?.EffectSurface,
            metadata?.TenantPolicyClassification,
            RequesterAuthorityClass(context.Actor.Principal),
            command.PolicySnapshotId,
            command.CommandAllowlistVersion,
            metadata?.CommandDefaultRisk,
            metadataSupported ? "declared" : "unsupported",
            "authorized",
            context.Submission.CorrelationId);
    }

    private static ProposeAIAction ReadProposalCommand(ChatBotGatewayContext context)
    {
        JsonElement command = context.Submission.Request.Command is JsonElement element
            ? element
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, ReadOptions);

        return command.Deserialize<ProposeAIAction>(ReadOptions)
            ?? throw new InvalidOperationException("The AI action proposal command payload could not be read.");
    }

    private static ExecuteLowRiskAIAssistance ReadExecutionCommand(ChatBotGatewayContext context)
    {
        JsonElement command = context.Submission.Request.Command is JsonElement element
            ? element
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, ReadOptions);

        return command.Deserialize<ExecuteLowRiskAIAssistance>(ReadOptions)
            ?? throw new InvalidOperationException("The low-risk AI assistance execution command payload could not be read.");
    }

    private static ExecuteApprovedAIAction ReadApprovedExecutionCommand(ChatBotGatewayContext context)
    {
        JsonElement command = context.Submission.Request.Command is JsonElement element
            ? element
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, ReadOptions);

        return command.Deserialize<ExecuteApprovedAIAction>(ReadOptions)
            ?? throw new InvalidOperationException("The approved AI action execution command payload could not be read.");
    }

    private static string? RequesterAuthorityClass(ClaimsPrincipal principal)
        => principal.Claims
            .FirstOrDefault(static claim =>
                string.Equals(claim.Type, "requester_authority_class", StringComparison.Ordinal) ||
                string.Equals(claim.Type, "requesterAuthorityClass", StringComparison.Ordinal))?
            .Value;
}
