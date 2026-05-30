using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;

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
            .AddSingleton(static _ => new Dapr.Client.DaprClientBuilder().Build())
            .AddSingleton<IIdempotencyStore, DaprCoarseIdempotencyStore>()
            .AddSingleton<InMemoryAuditWriter>()
            .AddSingleton<IAuditWriter>(static services => services.GetRequiredService<InMemoryAuditWriter>())
            .AddSingleton<InMemoryAuditReplayIntentQueue>()
            .AddSingleton<IAuditReplayIntentQueue>(static services => services.GetRequiredService<InMemoryAuditReplayIntentQueue>())
            .AddSingleton<InMemoryOperatorAlertSink>()
            .AddSingleton<IOperatorAlertSink>(static services => services.GetRequiredService<InMemoryOperatorAlertSink>())
            .AddSingleton<ISystemClock, SystemClock>()
            .AddScoped<ILifecycleTransitionGuard, CommandSubmissionLifecycleTransitionGuard>()
            .AddScoped<ICommandDispatcher, AcceptedCommandDispatcher>()
            .AddScoped<CommandGateway>();
    }
}
