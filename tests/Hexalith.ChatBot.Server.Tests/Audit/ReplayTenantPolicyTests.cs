using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.4 (AC1/AC3) coverage for the single authoritative test-tenant predicate. A test-tenant id (the reserved
/// prefix) resolves true; representative production ids resolve false; empty/unsafe ids resolve false (fail-closed →
/// treated as production for the probe sweep). The convention is stable so adapter selection and the nightly probe agree.
/// </summary>
public sealed class ReplayTenantPolicyTests
{
    [Fact]
    public void TestTenantPrefixedIdIsATestTenant()
    {
        ReplayTenantPolicy.IsTestTenant($"{ReplayTenantPolicy.ReplayTestTenantPrefix}tenant-alpha").ShouldBeTrue();
        ReplayTenantPolicy.IsTestTenant($"{ReplayTenantPolicy.ReplayTestTenantPrefix}qa-001").ShouldBeTrue();
    }

    [Theory]
    [InlineData("tenant-alpha")]
    [InlineData("tenant-beta")]
    [InlineData("acme-prod")]
    [InlineData("replay-test")] // the bare prefix word WITHOUT the trailing colon is not the reserved prefix
    [InlineData("not-replay-test:tenant")] // the prefix must be at the START
    public void ProductionTenantIdsAreNotTestTenants(string tenantId)
        => ReplayTenantPolicy.IsTestTenant(tenantId).ShouldBeFalse();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("replay-test:has space")] // whitespace ⇒ unsafe token
    [InlineData("replay-test:secret-leak")] // banned sensitive marker ⇒ unsafe token
    public void EmptyOrUnsafeTenantIdsAreNotTestTenantsFailClosed(string? tenantId)
        => ReplayTenantPolicy.IsTestTenant(tenantId).ShouldBeFalse();

    [Fact]
    public void TheReservedPrefixIsStable()
        => ReplayTenantPolicy.ReplayTestTenantPrefix.ShouldBe("replay-test:");
}
