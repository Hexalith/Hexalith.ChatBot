using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed record ChatBotGatewayContext(
    ChatBotCommandSubmission Submission,
    ChatBotAuthenticatedActor Actor,
    ChatBotTenantBinding TenantBinding);
