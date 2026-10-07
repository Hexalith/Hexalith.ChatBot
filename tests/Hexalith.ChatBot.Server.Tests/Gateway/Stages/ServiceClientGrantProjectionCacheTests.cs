using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Tests.TrustedAuthority;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class ServiceClientGrantProjectionCacheTests
{
    [Theory]
    [InlineData(300, 0)]
    [InlineData(0, 60)]
    [InlineData(-1, -1)]
    public async Task CacheCannotExtendOwnerObservationOrRevocationBounds(int observedAge, int revocationAge)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        ServiceClientGrantProjectionCache cache = new(clock);
        ChatBotOwnerAuthorityRequest request = Request("tenant-alpha", "client-alpha", "operation-alpha");
        ChatBotOwnerAuthorityEvidence evidence = (await owner.GetAuthorityAsync(request, TestContext.Current.CancellationToken))!;
        cache.Upsert(evidence with { ObservedAt = clock.UtcNow.AddSeconds(-observedAge), RevocationCheckedAt = clock.UtcNow.AddSeconds(-revocationAge) });
        cache.TryGetEvidence(request).ShouldBeNull();
    }

    [Fact]
    public async Task KnownRevocationImmediatelyDeniesOnlyItsExactGrantScope()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        ServiceClientGrantProjectionCache cache = new(clock);
        ChatBotOwnerAuthorityRequest request = Request("tenant-alpha", "client-alpha", "operation-alpha");
        ChatBotOwnerAuthorityRequest foreign = Request("tenant-beta", "client-alpha", "operation-alpha");
        ChatBotOwnerAuthorityEvidence evidence = (await owner.GetAuthorityAsync(request, TestContext.Current.CancellationToken))!;
        cache.Upsert(evidence);
        cache.Upsert((await owner.GetAuthorityAsync(foreign, TestContext.Current.CancellationToken))!);
        cache.TryGetEvidence(request).ShouldNotBeNull();
        cache.InvalidateRevocation(request.TenantId, request.ResourceId, "api", evidence.ServiceGrant!.GrantId);
        cache.TryGetEvidence(request).ShouldBeNull();
        cache.Upsert(evidence);
        cache.TryGetEvidence(request).ShouldBeNull();
        cache.TryGetEvidence(foreign).ShouldNotBeNull();
        cache.TryGetEvidence(request with { Operation = "operation-beta" }).ShouldBeNull();
        cache.TryGetEvidence(request with { PrincipalId = "actor-other" }).ShouldBeNull();
    }

    private static ChatBotOwnerAuthorityRequest Request(string tenant, string client, string operation)
        => new("ChatBot", "actor-alpha", tenant, client, operation, "service-grant", "service", Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin.Api, false);
}
