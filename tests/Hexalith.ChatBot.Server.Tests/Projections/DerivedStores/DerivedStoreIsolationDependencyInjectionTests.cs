using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Projections.DerivedStores;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections.DerivedStores;

/// <summary>
/// Story 9.5 (AC1/AC2) wiring guard for <c>AddChatBotCommandGateway</c>: the tenant-partitioned
/// <see cref="IDerivedStore"/> resolves to the in-memory default and the synthetic cross-tenant
/// <see cref="DerivedStoreIsolationProbeCoordinator"/> resolves — the seam + probe a periodic scheduler and the M2
/// release gate depend on. Without this guard a registration regression could silently drop the isolation seam — exactly
/// the wiring-drift defect called out as the top recurring Epic 7–9 review fix. Mirrors
/// <c>ReplayIsolationDependencyInjectionTests</c>.
/// </summary>
public sealed class DerivedStoreIsolationDependencyInjectionTests
{
    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        _ = services.AddChatBotCommandGateway();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void DerivedStoreResolvesToTheInMemoryDefault()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<IDerivedStore>().ShouldBeOfType<InMemoryDerivedStore>();
    }

    [Fact]
    public void IsolationProbeCoordinatorResolves()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<DerivedStoreIsolationProbeCoordinator>().ShouldNotBeNull();
    }
}
