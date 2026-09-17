using System.Collections.Concurrent;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Notifications;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;

internal static class PeriodicEnforcementServiceCollectionExtensions
{
    public static IServiceCollection AddChatBotPeriodicEnforcement(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // ValidateOnStart: a bad cadence/anchor used to throw ArgumentOutOfRangeException from the partition-key
        // helper on the first tick — outside the per-evaluator fail-isolation wrapper — which escaped the background
        // loop and stopped the whole host minutes after an apparently successful boot. Fail at startup instead.
        _ = services
            .AddOptions<PeriodicEnforcementOptions>()
            .Validate(
                static options => options.Validate() is null,
                "Invalid ChatBot:PeriodicEnforcement configuration.")
            .ValidateOnStart();
        services.TryAddSingleton<IPeriodicEnforcementInputSource, ProjectionBackedPeriodicEnforcementInputSource>();
        services.TryAddSingleton<IPeriodicEnforcementStatusStore, InMemoryPeriodicEnforcementStatusStore>();
        services.TryAddSingleton<IAuditProjectionCheckpointSource, UnavailableAuditProjectionCheckpointSource>();
        services.TryAddSingleton<SweepBackedAuditCompletenessSource>();
        services.TryAddSingleton<CheckpointBackedAuditProjectionLagSource>();
        services.TryAddSingleton<PeriodicEnforcementCoordinator>();
        return services;
    }

    public static IServiceCollection AddChatBotPeriodicEnforcementHostedService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<IAuditProjectionLagSource>();
        services.RemoveAll<IAuditCompletenessSource>();
        services.AddSingleton<IAuditProjectionLagSource>(static provider => provider.GetRequiredService<CheckpointBackedAuditProjectionLagSource>());
        services.AddSingleton<IAuditCompletenessSource>(static provider => provider.GetRequiredService<SweepBackedAuditCompletenessSource>());
        services.AddHostedService<PeriodicEnforcementBackgroundService>();
        return services;
    }
}
