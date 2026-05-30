using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Gateway;

internal static class CommandGatewayServiceCollectionExtensions
{
    public static IServiceCollection AddChatBotCommandGateway(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddScoped<IAuthenticationStage, ClaimsAuthenticationStage>()
            .AddScoped<ITenantBindingStage, ClaimsTenantBindingStage>()
            .AddScoped<IAuthorizationStage, PassThroughAuthorizationStage>()
            .AddScoped<IRiskClassifier, PassThroughRiskClassifier>()
            .AddScoped<IApprovalGate, PassThroughApprovalGate>()
            .AddScoped<IIdempotencyStore, PassThroughIdempotencyStore>()
            .AddScoped<IAuditWriter, InMemoryAuditWriter>()
            .AddScoped<ICommandDispatcher, AcceptedCommandDispatcher>()
            .AddScoped<CommandGateway>();
    }
}
