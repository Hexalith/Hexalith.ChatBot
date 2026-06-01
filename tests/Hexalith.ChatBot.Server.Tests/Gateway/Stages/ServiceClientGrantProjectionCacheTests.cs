using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class ServiceClientGrantProjectionCacheTests
{
    [Fact]
    public void NormalGrantCacheShouldBoundStalenessToFiveMinutes()
    {
        MutableClock clock = new(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
        ServiceClientGrantProjectionCache cache = new(clock);
        cache.Upsert(Grant("tenant-alpha", "cli-automation-client", ChatBotSurfaceOrigin.Cli, "grant-alpha"));

        cache.TryGet("tenant-alpha", "cli-automation-client", "cli", "grant-alpha").ShouldNotBeNull();

        clock.UtcNow = clock.UtcNow.Add(ServiceClientGrantProjectionCache.NormalGrantStaleness);

        cache.TryGet("tenant-alpha", "cli-automation-client", "cli", "grant-alpha").ShouldBeNull();
    }

    [Fact]
    public void RevocationInvalidationShouldAffectOnlyTargetedTenantServiceClientAndSurfaceWithinSixtySeconds()
    {
        MutableClock clock = new(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
        ServiceClientGrantProjectionCache cache = new(clock);
        cache.Upsert(Grant("tenant-alpha", "cli-automation-client", ChatBotSurfaceOrigin.Cli, "grant-alpha"));
        cache.Upsert(Grant("tenant-alpha", "mcp-tool-client", ChatBotSurfaceOrigin.Mcp, "grant-mcp"));
        cache.Upsert(Grant("tenant-beta", "cli-automation-client", ChatBotSurfaceOrigin.Cli, "grant-beta"));

        cache.InvalidateRevocation("tenant-alpha", "cli-automation-client", "cli", "grant-alpha");
        clock.UtcNow = clock.UtcNow.Add(ServiceClientGrantProjectionCache.RevocationStaleness).AddSeconds(1);

        cache.TryGet("tenant-alpha", "cli-automation-client", "cli", "grant-alpha").ShouldBeNull();
        cache.TryGet("tenant-alpha", "mcp-tool-client", "mcp", "grant-mcp").ShouldNotBeNull();
        cache.TryGet("tenant-beta", "cli-automation-client", "cli", "grant-beta").ShouldNotBeNull();
    }

    private static ServiceClientGrant Grant(string tenantId, string serviceClientId, ChatBotSurfaceOrigin origin, string grantId)
        => new(
            grantId,
            tenantId,
            serviceClientId,
            origin == ChatBotSurfaceOrigin.Mcp ? ServiceClientClass.McpTool : ServiceClientClass.CliAutomation,
            [nameof(Hexalith.ChatBot.Contracts.Commands.RecordGovernedNote)],
            [],
            origin,
            new DateTimeOffset(2026, 6, 1, 13, 0, 0, TimeSpan.Zero),
            false,
            ["notes.write"],
            "command-set-v1");

    private sealed class MutableClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;
    }
}
