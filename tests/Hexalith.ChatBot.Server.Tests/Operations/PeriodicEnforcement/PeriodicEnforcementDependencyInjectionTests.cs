using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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

    [Fact]
    public void CommandGatewayRegistrationShouldResolveEnabledM2RuntimeAndAllThreeSweepCoordinators()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddChatBotCommandGateway()
            .Configure<PeriodicEnforcementOptions>(options => options.RunM2AuditRecoverySweeps = true)
            .BuildServiceProvider();

        provider.GetRequiredService<IOptions<PeriodicEnforcementOptions>>().Value.RunM2AuditRecoverySweeps.ShouldBeTrue();
        provider.GetRequiredService<AuditChainVerificationCoordinator>().ShouldNotBeNull();
        provider.GetRequiredService<ReplayIsolationProbeCoordinator>().ShouldNotBeNull();
        provider.GetRequiredService<DerivedStoreIsolationProbeCoordinator>().ShouldNotBeNull();
        provider.GetRequiredService<PeriodicEnforcementCoordinator>().ShouldNotBeNull();
    }

    [Fact]
    public async Task HostedBackgroundServiceShouldInvokeAllEnabledM2SweepsOnItsPrimaryPath()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceCollection services = new();
        _ = services
            .AddChatBotCommandGateway()
            .Configure<PeriodicEnforcementOptions>(options =>
            {
                options.UsePeriodicEnforcementRuntime = true;
                options.RunM2AuditRecoverySweeps = true;
                options.Cadence = TimeSpan.FromHours(1);
            })
            .AddChatBotPeriodicEnforcementHostedService();
        await using ServiceProvider provider = services.BuildServiceProvider();
        PeriodicEnforcementCoordinator coordinator = provider.GetRequiredService<PeriodicEnforcementCoordinator>();
        PeriodicEnforcementBackgroundService hostedService = provider
            .GetServices<IHostedService>()
            .OfType<PeriodicEnforcementBackgroundService>()
            .ShouldHaveSingleItem();

        await hostedService.StartAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            while (coordinator.Status.M2SweepStatuses.Count < 3)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token).ConfigureAwait(true);
            }

            coordinator.Status.M2SweepStatuses["worm-audit-chain"].LastSucceededAtUtc.ShouldNotBeNull();
            coordinator.Status.M2SweepStatuses["replay-isolation-probe"].LastSucceededAtUtc.ShouldNotBeNull();
            coordinator.Status.M2SweepStatuses["derived-store-isolation-probe"].LastSucceededAtUtc.ShouldNotBeNull();
            coordinator.Status.M2SweepStatuses["replay-isolation-probe"].LastBreaches.ShouldBe(0);
            coordinator.Status.M2SweepStatuses["derived-store-isolation-probe"].LastBreaches.ShouldBe(0);
        }
        finally
        {
            await hostedService.StopAsync(CancellationToken.None).ConfigureAwait(true);
        }
    }
}
