using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Operations.PeriodicEnforcement;

public sealed class PeriodicEnforcementDependencyInjectionTests
{
    [Fact]
    public void CommandGatewayRegistrationShouldExposeCoordinatorWithoutHostedTimerByDefault()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddChatBotCommandGateway()
            .BuildServiceProvider();

        provider.GetRequiredService<PeriodicEnforcementCoordinator>().ShouldNotBeNull();
        provider.GetServices<IHostedService>().ShouldBeEmpty();
        provider.GetRequiredService<IAuditProjectionLagSource>().ShouldBeOfType<UnavailableAuditProjectionLagSource>();
        provider.GetRequiredService<IAuditCompletenessSource>().ShouldBeOfType<UnavailableAuditCompletenessSource>();
    }

    [Fact]
    public void HostedRegistrationShouldResolvePeriodicRuntimeAndMeasuredSourcesExactlyOnce()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddChatBotCommandGateway()
            .AddChatBotPeriodicEnforcementHostedService()
            .BuildServiceProvider();

        provider.GetServices<PeriodicEnforcementCoordinator>().Count().ShouldBe(1);
        provider.GetServices<IHostedService>().OfType<PeriodicEnforcementBackgroundService>().Count().ShouldBe(1);
        provider.GetServices<IAuditProjectionLagSource>().ShouldHaveSingleItem().ShouldBeOfType<CheckpointBackedAuditProjectionLagSource>();
        provider.GetServices<IAuditCompletenessSource>().ShouldHaveSingleItem().ShouldBeOfType<SweepBackedAuditCompletenessSource>();
    }
}
