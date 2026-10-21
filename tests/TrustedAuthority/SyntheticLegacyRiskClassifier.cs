using System.Security.Claims;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Tests.TrustedAuthority;

/// <summary>Explicit synthetic determinate-classifier input for downstream replay fixtures; no A9a authority.</summary>
internal sealed class SyntheticLegacyRiskClassifier : IRiskClassifier
{
    /// <inheritdoc />
    public async ValueTask<ChatBotRiskClassification> ClassifyAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ClaimsPrincipal principal = new(context.Actor.Principal.Identities.Select(identity => identity.Clone()));
        principal.AddIdentity(new ClaimsIdentity([new Claim("requester_authority_class", "project-contributor")], "synthetic-classifier-input"));
        ChatBotRiskClassification result = await new DeterministicAiActionRiskClassifier().ClassifyAsync(context with { Actor = context.Actor with { Principal = principal } }, cancellationToken).ConfigureAwait(false);
        return result.Record is not null ? result : ChatBotRiskClassification.Classified(AiActionRiskClassifier.Classify(new AiActionRiskInputTuple(
            AiActionCommandMetadataProvider.AppendConversationMessageCommandName, [], null, null, "project-contributor", "synthetic-policy",
            null, null, null, "authorized", context.Submission.CorrelationId)));
    }
}
