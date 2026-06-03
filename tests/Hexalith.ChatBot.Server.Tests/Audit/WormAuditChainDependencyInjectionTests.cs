using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.1 (AC1/AC2/AC3) wiring guard for <c>AddChatBotCommandGateway</c>: the post-commit audit seam resolves to the
/// <see cref="ChainedAuditWriter"/> decorator over the in-process WORM store (so chaining is actually composed behind the
/// commit path), the Story 1.9 audit-history surface still resolves to the inner writer, and the verification +
/// redaction services and their separate-KMS / encrypted-original / projection-tombstone seams all resolve. This
/// pre-empts the bookkeeping/wiring drift called out as the top recurring Epic 7/8 defect.
/// </summary>
public sealed class WormAuditChainDependencyInjectionTests
{
    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        _ = services.AddChatBotCommandGateway();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void PostCommitAuditWriterResolvesToTheChainedWormDecorator()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<IAuditWriter>().ShouldBeOfType<ChainedAuditWriter>();
    }

    [Fact]
    public void AuditHistoryReaderStillResolvesToTheInnerInMemoryWriter()
    {
        using ServiceProvider provider = BuildProvider();

        // The Story 1.9 audit-history surface must stay backed by the inner writer, not the chaining decorator.
        provider.GetRequiredService<IAuditHistoryReader>().ShouldBeOfType<InMemoryAuditWriter>();
    }

    [Fact]
    public void WormStoreAndErasureSeamsResolve()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<IWormAuditStore>().ShouldBeOfType<InMemoryWormAuditStore>();
        provider.GetRequiredService<IKmsRedactionKeyStore>().ShouldBeOfType<InMemoryKmsRedactionKeyStore>();
        provider.GetRequiredService<IEncryptedAuditOriginalStore>().ShouldBeOfType<InMemoryEncryptedAuditOriginalStore>();
        provider.GetRequiredService<IRedactionProjectionStore>().ShouldBeOfType<InMemoryRedactionProjectionStore>();
    }

    [Fact]
    public void VerificationAndRedactionServicesResolve()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<AuditChainVerificationCoordinator>().ShouldNotBeNull();
        provider.GetRequiredService<AuditRedactionService>().ShouldNotBeNull();
    }

    [Fact]
    public void CompletenessObservableSeamsResolve()
    {
        using ServiceProvider provider = BuildProvider();

        // Story 9.2 (NFR50a): the completeness measurer, the audit-then-deliver alert coordinator, and the fail-safe
        // gauge source all resolve — pre-empting the wiring-drift defect called out across Epics 7–9.
        provider.GetRequiredService<AuditCompletenessMeasurer>().ShouldNotBeNull();
        provider.GetRequiredService<AuditCompletenessAlertCoordinator>().ShouldNotBeNull();
        provider.GetRequiredService<Hexalith.ChatBot.Server.Observability.IAuditCompletenessSource>()
            .ShouldBeOfType<Hexalith.ChatBot.Server.Observability.UnavailableAuditCompletenessSource>();
    }
}
