using Hexalith.ChatBot.Server.Adapters.AiProvider;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.ChatBot.Server.Acceptance;

internal static class Story132AcceptanceServiceCollectionExtensions
{
    internal const string EnabledConfigurationKey = "ChatBot:Story132Acceptance:Enabled";

    public static IServiceCollection AddStory132AcceptanceFixture(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        if (!configuration.GetValue<bool>(EnabledConfigurationKey))
        {
            return services;
        }

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            throw new InvalidOperationException(
                $"{EnabledConfigurationKey}=true is an acceptance-only fixture and may run only in Development or Testing; "
                + $"the current environment is '{environment.EnvironmentName}'.");
        }

        services.RemoveAll<ITenantAiPolicySnapshotProvider>();
        services.RemoveAll<IAiAssistanceProvider>();
        services.RemoveAll<IRiskClassifier>();
        services.AddSingleton<ITenantAiPolicySnapshotProvider, Story132AcceptanceTenantAiPolicySnapshotProvider>();
        services.AddSingleton<IAiAssistanceProvider, Story132AcceptanceAiAssistanceProvider>();
        services.AddScoped<IRiskClassifier, Story132AcceptanceRiskClassifier>();
        return services;
    }
}
