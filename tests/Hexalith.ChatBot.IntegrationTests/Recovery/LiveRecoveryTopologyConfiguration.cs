using Aspire.Hosting.ApplicationModel;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Applies the bounded resource settings shared by the live-recovery topology and its model tests.</summary>
internal static class LiveRecoveryTopologyConfiguration
{
    /// <summary>Bounds graceful shutdown inside the Aspire command deadline and removes test-only rate bottlenecks.</summary>
    public static void ConfigureEventStore(IResource eventStore)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        eventStore.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["DOTNET_SHUTDOWNTIMEOUTSECONDS"] = "5";
            context.EnvironmentVariables["EventStore__RateLimiting__PermitLimit"] = "100000";
            context.EnvironmentVariables["EventStore__RateLimiting__ConsumerPermitLimit"] = "10000";
        }));
    }
}
