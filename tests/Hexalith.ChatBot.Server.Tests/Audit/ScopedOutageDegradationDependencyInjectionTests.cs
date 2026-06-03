using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.13 (Task 8, AC1) wiring guard for <c>AddChatBotCommandGateway</c>: the scoped-outage degradation validation
/// seam registers exactly like its 9.11/9.12 twins — the injection driver resolves to the wired-but-inert
/// <see cref="DeferredScopedOutageInjectionDriver"/> (the live M2-deferred fault-injection runtime is NOT yet composed),
/// and the <see cref="ScopedOutageDegradationValidationCoordinator"/> resolves with all its constructor dependencies
/// (<c>IAuditWriter</c>/<c>IOperatorAlertSink</c>/<c>ISystemClock</c>) satisfied. This pre-empts the DI/bookkeeping wiring
/// drift called out as the top recurring Epic 7–9 review defect (mirrors <see cref="WormAuditChainDependencyInjectionTests"/>).
/// </summary>
public sealed class ScopedOutageDegradationDependencyInjectionTests
{
    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        _ = services.AddChatBotCommandGateway();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void InjectionDriverResolvesToTheInertDeferredDefault()
    {
        using ServiceProvider provider = BuildProvider();

        // The seam is wired but unmistakably not live — the inert default throws until the M2 runtime lands.
        provider.GetRequiredService<IScopedOutageInjectionDriver>().ShouldBeOfType<DeferredScopedOutageInjectionDriver>();
    }

    [Fact]
    public void ValidationCoordinatorResolvesWithAllDependenciesSatisfied()
    {
        using ServiceProvider provider = BuildProvider();

        // Resolving the coordinator proves the driver/audit-writer/alert-sink/clock dependencies all wire up.
        provider.GetRequiredService<ScopedOutageDegradationValidationCoordinator>().ShouldNotBeNull();
    }

    [Fact]
    public void ValidationCoordinatorIsRegisteredAsASingleton()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<ScopedOutageDegradationValidationCoordinator>()
            .ShouldBeSameAs(provider.GetRequiredService<ScopedOutageDegradationValidationCoordinator>());
    }
}
