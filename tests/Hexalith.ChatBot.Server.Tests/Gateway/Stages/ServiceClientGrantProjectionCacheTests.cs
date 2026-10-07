using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Queries;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NewGrantCannotReviveAnotherOperationsPreRevocationEvidence(bool delayedOldUpsert)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        ServiceClientGrantProjectionCache cache = new(clock);
        ChatBotOwnerAuthorityRequest first = Request("tenant-alpha", "client-alpha", "operation-alpha");
        ChatBotOwnerAuthorityRequest second = first with { Operation = "operation-beta" };
        ChatBotOwnerAuthorityEvidence oldFirst = (await owner.GetAuthorityAsync(first, TestContext.Current.CancellationToken))!;
        ChatBotOwnerAuthorityEvidence oldSecond = (await owner.GetAuthorityAsync(second, TestContext.Current.CancellationToken))!;
        oldSecond = oldSecond with { ServiceGrant = oldSecond.ServiceGrant! with { GrantId = "old-second-grant" } };
        cache.Upsert(oldFirst);
        cache.Upsert(oldSecond);
        cache.TryGetEvidence(second).ShouldNotBeNull();
        cache.InvalidateRevocation(first.TenantId, first.ResourceId, "api", string.Empty);
        cache.TryGetEvidence(first).ShouldBeNull();
        cache.TryGetEvidence(second).ShouldBeNull();
        clock.UtcNow += TimeSpan.FromSeconds(1);
        ChatBotOwnerAuthorityEvidence newFirst = (await owner.GetAuthorityAsync(first, TestContext.Current.CancellationToken))!;
        newFirst = newFirst with { ServiceGrant = newFirst.ServiceGrant! with { GrantId = "new-first-grant" } };
        if (delayedOldUpsert) { cache.Upsert(oldSecond); }
        cache.ObserveAllowedEvidence(newFirst);
        cache.Upsert(newFirst);
        cache.TryGetEvidence(first).ShouldBe(newFirst);
        cache.TryGetEvidence(second).ShouldBeNull();
        // A provider request which began before revocation may finish after the tombstone was lifted.
        cache.Upsert(oldSecond);
        cache.TryGetEvidence(second).ShouldBeNull();
        cache.PredatesClientRevocation(oldSecond).ShouldBeTrue();
        cache.PredatesClientRevocation(newFirst).ShouldBeFalse();
        cache.InvalidateRevocation(first.TenantId, first.ResourceId, "api", newFirst.ServiceGrant!.GrantId);
        clock.UtcNow += TimeSpan.FromSeconds(1);
        cache.ObserveAllowedEvidence(newFirst with { RevocationCheckedAt = clock.UtcNow });
        cache.IsRevoked(first, newFirst.ServiceGrant.GrantId).ShouldBeTrue();
    }

    [Fact]
    public async Task OlderBroaderResponseCannotReplaceNewerExactObservation()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        ServiceClientGrantProjectionCache cache = new(clock);
        ChatBotOwnerAuthorityRequest request = Request("tenant-alpha", "client-alpha", ChatBotReadQueryTypes.GovernedOperation);
        ChatBotOwnerAuthorityEvidence older = (await owner.GetAuthorityAsync(request, TestContext.Current.CancellationToken))!;
        older = older with { Version = "999999", ServiceGrant = older.ServiceGrant! with { Scopes = ["old-broad-scope", "new-exact-scope"], AllowedQueryNames = [ChatBotReadQueryTypes.GovernedOperation, ChatBotReadQueryTypes.OperationStatus] } };
        clock.UtcNow += TimeSpan.FromSeconds(1);
        ChatBotOwnerAuthorityEvidence newer = (await owner.GetAuthorityAsync(request, TestContext.Current.CancellationToken))!;
        newer = newer with { Version = "opaque-current-version", ServiceGrant = newer.ServiceGrant! with { Scopes = ["new-exact-scope"] } };
        cache.Upsert(newer);
        clock.UtcNow += TimeSpan.FromSeconds(1);
        cache.Upsert(older with { RevocationCheckedAt = clock.UtcNow });
        cache.TryGetEvidence(request).ShouldBe(newer);
        cache.TryGetEvidence(request)!.ServiceGrant!.Scopes.ShouldBe(["new-exact-scope"]);
        cache.TryGetEvidence(request)!.ServiceGrant!.AllowedQueryNames.ShouldBe([ChatBotReadQueryTypes.GovernedOperation]);
    }

    private static ChatBotOwnerAuthorityRequest Request(string tenant, string client, string operation)
        => new("ChatBot", "actor-alpha", tenant, client, operation, "service-grant", "service", Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin.Api, false);
}
