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

            // Poll the post-condition, not the dictionary's cardinality. The runtime records "ran" before the sweep
            // executes and "succeeded" after, under two separate lock acquisitions, so Count == 3 is reached while the
            // third sweep is still in flight — the earlier spelling of this test could observe three entries whose
            // LastSucceededAtUtc was still null and fail intermittently. This is the story's only primary-path
            // evidence that the hosted service really drives the sweeps, so it must not be flaky.
            string[] jobNames = M2SweepJobs.All;
            while (!jobNames.All(jobName =>
                coordinator.Status.M2SweepStatuses.TryGetValue(jobName, out PeriodicEnforcementM2SweepStatus? sweep) &&
                sweep.LastSucceededAtUtc is not null))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token).ConfigureAwait(true);
            }

            coordinator.Status.M2SweepStatuses[M2SweepJobs.WormAuditChain].LastSucceededAtUtc.ShouldNotBeNull();
            coordinator.Status.M2SweepStatuses[M2SweepJobs.ReplayIsolationProbe].LastSucceededAtUtc.ShouldNotBeNull();
            coordinator.Status.M2SweepStatuses[M2SweepJobs.DerivedStoreIsolationProbe].LastSucceededAtUtc.ShouldNotBeNull();
            coordinator.Status.M2SweepStatuses[M2SweepJobs.ReplayIsolationProbe].LastBreaches.ShouldBe(0);
            coordinator.Status.M2SweepStatuses[M2SweepJobs.DerivedStoreIsolationProbe].LastBreaches.ShouldBe(0);

            // Coverage, not just breach count. On this host every store is an empty in-memory default, so all three
            // sweeps enumerate zero tenants and report zero breaches — this assertion states plainly that the hosted
            // proof demonstrates *invocation*, not enforcement. The release gate reads the same field and treats
            // zero coverage as stop-ship precisely so a vacuous run cannot be mistaken for a verified clean one.
            coordinator.Status.M2SweepStatuses[M2SweepJobs.WormAuditChain].LastCoverage.ShouldBe(0);
            coordinator.Status.M2SweepStatuses[M2SweepJobs.ReplayIsolationProbe].LastCoverage.ShouldBe(0);
            coordinator.Status.M2SweepStatuses[M2SweepJobs.DerivedStoreIsolationProbe].LastCoverage.ShouldBe(0);
            PeriodicEnforcementM2ReleaseGateResponse gate = coordinator.M2ReleaseGateStatus;
            gate.IsStopShip.ShouldBeTrue();
        }
        finally
        {
            await hostedService.StopAsync(CancellationToken.None).ConfigureAwait(true);
        }
    }
}
