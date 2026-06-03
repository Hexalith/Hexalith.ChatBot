using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Adapters.Mailbox;

/// <summary>
/// Story 9.4 (AC1/AC3) wiring guard for <c>AddChatBotCommandGateway</c>: the registered
/// <see cref="IOutboundMailboxSender"/> the dispatcher resolves is the <see cref="ReplayAwareOutboundMailboxSender"/>
/// selector (NOT the bare production sender) — the single decision point that routes a test tenant to the test-mode
/// adapter and every production tenant to the unchanged production sender. The trace store and the nightly
/// <see cref="ReplayIsolationProbeCoordinator"/> also resolve. Without this guard a registration regression could
/// silently bypass the selector and defeat the whole isolation model — exactly the wiring-drift defect called out as the
/// top recurring Epic 7–9 review fix.
/// </summary>
public sealed class ReplayIsolationDependencyInjectionTests
{
    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        _ = services.AddChatBotCommandGateway();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void OutboundMailboxSenderResolvesToTheReplayAwareSelector()
    {
        using ServiceProvider provider = BuildProvider();

        // The dispatcher resolves IOutboundMailboxSender; it MUST be the tenant-aware selector so isolation is enforced
        // at the single send seam, never the bare production sender.
        provider.GetRequiredService<IOutboundMailboxSender>().ShouldBeOfType<ReplayAwareOutboundMailboxSender>();
    }

    [Fact]
    public void OutboundTraceStoreResolvesToTheInMemoryDefault()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<IOutboundTraceStore>().ShouldBeOfType<InMemoryOutboundTraceStore>();
    }

    [Fact]
    public void TestModeSenderAndIsolationProbeCoordinatorResolve()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<TestModeOutboundMailboxSender>().ShouldNotBeNull();
        provider.GetRequiredService<ReplayIsolationProbeCoordinator>().ShouldNotBeNull();
    }
}
