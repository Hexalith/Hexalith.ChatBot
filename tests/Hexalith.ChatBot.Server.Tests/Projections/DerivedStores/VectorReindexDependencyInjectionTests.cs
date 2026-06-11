using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Projections.DerivedStores;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections.DerivedStores;

/// <summary>
/// Story 9.6 (AC1/AC2) wiring guard for <c>AddChatBotCommandGateway</c>: the version-guard ledger, the
/// <c>ReindexVectors</c> reindexer, and the vector-reindex correction-propagation activity all resolve to their
/// in-memory defaults. The activity catalog is complete for the M2 scope, while the coordinator remains fail-closed
/// until the hosted DAPR workflow runtime is explicitly enabled. Without this guard a registration regression could
/// silently drop the reindex seam — the wiring-drift defect called out as the top recurring Epic 7–9 review fix.
/// Mirrors <c>DerivedStoreIsolationDependencyInjectionTests</c>.
/// </summary>
public sealed class VectorReindexDependencyInjectionTests
{
    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        _ = services.AddChatBotCommandGateway();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void LedgerAndReindexerResolveToTheInMemoryDefaults()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<IVectorReindexLedger>().ShouldBeOfType<InMemoryVectorReindexLedger>();
        provider.GetRequiredService<IVectorReindexer>().ShouldBeOfType<InMemoryVectorReindexer>();
    }

    [Fact]
    public void TheVectorReindexActivityIsRegistered()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetServices<ICorrectionPropagationStoreActivity>()
            .ShouldContain(static activity => activity.StoreKey == CorrectionPropagationStoreKeys.VectorReindex);
    }

    [Fact]
    public void ActivityCatalogIsReadyForTheM2Scope()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<ICorrectionPropagationActivityCatalog>().IsReady.ShouldBeTrue();
    }

    [Fact]
    public void CoordinatorRequiresAnEnabledWorkflowRuntime()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<ICorrectionPropagationCoordinator>().IsReady.ShouldBeFalse();
    }
}
