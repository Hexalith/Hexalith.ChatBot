using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Idempotency;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed record ChatBotGatewayContext(
    ChatBotCommandSubmission Submission,
    ChatBotAuthenticatedActor Actor,
    ChatBotTenantBinding TenantBinding)
{
    public CoarseIdempotencyMetadata? Idempotency { get; private set; }

    public void SetIdempotency(CoarseIdempotencyMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        Idempotency = metadata;
    }
}
