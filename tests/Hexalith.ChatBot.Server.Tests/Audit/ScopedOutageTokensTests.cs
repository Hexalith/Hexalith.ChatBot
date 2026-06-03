using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.13 (Task 1, AC1/AC2/AC4) coverage for the three closed scoped-outage token vocabularies.
/// <see cref="ScopedOutageDependencies"/> (the six NFR59 dependencies the sweep runs), <see cref="ScopedOutageScopes"/>
/// (the six NFR41/NFR58 narrowest-scope axes), and <see cref="ScopedOutageDegradationVerdicts"/>
/// (contained/breached/unmeasurable) are fixed, bounded sets whose null-safe <c>Contains</c> biases an unknown/null token
/// to "not a member" so the coordinator fails safe to <c>unmeasurable</c> rather than fabricating a <c>contained</c>.
/// These pin the set contents, the exact literal values (which deliberately avoid the legacy-lifecycle tokens so no
/// scaffold allowlist entry is needed), and the null-safe membership contract directly. Mirrors
/// <see cref="ContinuityDrillTokensTests"/>.
/// </summary>
public sealed class ScopedOutageTokensTests
{
    private static readonly string[] LegacyLifecycle = ["pending", "accepted", "running", "succeeded", "cancelled"];

    [Fact]
    public void DependenciesClosedSetIsExactlyTheSixNfr59Dependencies()
    {
        ScopedOutageDependencies.All.ShouldBe(
            new[]
            {
                ScopedOutageDependencies.Graph,
                ScopedOutageDependencies.Identity,
                ScopedOutageDependencies.AiProvider,
                ScopedOutageDependencies.CommandExecution,
                ScopedOutageDependencies.AuditStore,
                ScopedOutageDependencies.AttachmentProcessing,
            },
            ignoreOrder: true);
        ScopedOutageDependencies.Graph.ShouldBe("graph");
        ScopedOutageDependencies.Identity.ShouldBe("identity");
        ScopedOutageDependencies.AiProvider.ShouldBe("ai-provider");
        ScopedOutageDependencies.CommandExecution.ShouldBe("command-execution");
        ScopedOutageDependencies.AuditStore.ShouldBe("audit-store");
        ScopedOutageDependencies.AttachmentProcessing.ShouldBe("attachment-processing");
    }

    [Fact]
    public void DependenciesHaveNoSeventhSubscriptionExpiryToken()
    {
        // graph covers both degraded Graph access AND the expired-subscription lapse — there is no separate token.
        ScopedOutageDependencies.All.Count.ShouldBe(6);
        ScopedOutageDependencies.Contains("subscription-expiry").ShouldBeFalse();
    }

    [Theory]
    [InlineData(ScopedOutageDependencies.Graph, true)]
    [InlineData(ScopedOutageDependencies.AttachmentProcessing, true)]
    [InlineData("unknown-dependency", false)]
    [InlineData("", false)]
    public void DependenciesContainsRecognizesOnlyKnownTokens(string dependency, bool expected)
        => ScopedOutageDependencies.Contains(dependency).ShouldBe(expected);

    [Fact]
    public void DependenciesContainsIsNullSafe()
        => ScopedOutageDependencies.Contains(null).ShouldBeFalse();

    [Fact]
    public void ScopesClosedSetIsExactlyTheSixNarrowestScopeAxes()
    {
        ScopedOutageScopes.All.ShouldBe(
            new[]
            {
                ScopedOutageScopes.Tenant,
                ScopedOutageScopes.Mailbox,
                ScopedOutageScopes.Operation,
                ScopedOutageScopes.ServiceClient,
                ScopedOutageScopes.CommandSurface,
                ScopedOutageScopes.WorkflowItem,
            },
            ignoreOrder: true);
        ScopedOutageScopes.Tenant.ShouldBe("tenant");
        ScopedOutageScopes.Mailbox.ShouldBe("mailbox");
        ScopedOutageScopes.Operation.ShouldBe("operation");
        ScopedOutageScopes.ServiceClient.ShouldBe("service-client");
        ScopedOutageScopes.CommandSurface.ShouldBe("command-surface");
        ScopedOutageScopes.WorkflowItem.ShouldBe("workflow-item");
    }

    [Theory]
    [InlineData(ScopedOutageScopes.Mailbox, true)]
    [InlineData(ScopedOutageScopes.WorkflowItem, true)]
    [InlineData("global", false)]
    [InlineData("", false)]
    public void ScopesContainsRecognizesOnlyKnownTokens(string scope, bool expected)
        => ScopedOutageScopes.Contains(scope).ShouldBe(expected);

    [Fact]
    public void ScopesContainsIsNullSafe()
        => ScopedOutageScopes.Contains(null).ShouldBeFalse();

    [Fact]
    public void VerdictsClosedSetIsExactlyContainedBreachedUnmeasurable()
    {
        ScopedOutageDegradationVerdicts.All.ShouldBe(
            new[] { ScopedOutageDegradationVerdicts.Contained, ScopedOutageDegradationVerdicts.Breached, ScopedOutageDegradationVerdicts.Unmeasurable },
            ignoreOrder: true);
        ScopedOutageDegradationVerdicts.Contained.ShouldBe("contained");
        ScopedOutageDegradationVerdicts.Breached.ShouldBe("breached");
        ScopedOutageDegradationVerdicts.Unmeasurable.ShouldBe("unmeasurable");
    }

    [Theory]
    [InlineData(ScopedOutageDegradationVerdicts.Contained, true)]
    [InlineData(ScopedOutageDegradationVerdicts.Breached, true)]
    [InlineData(ScopedOutageDegradationVerdicts.Unmeasurable, true)]
    [InlineData("passed", false)]
    public void VerdictsContainsRecognizesOnlyKnownTokens(string verdict, bool expected)
        => ScopedOutageDegradationVerdicts.Contains(verdict).ShouldBe(expected);

    [Fact]
    public void VerdictsContainsIsNullSafe()
        => ScopedOutageDegradationVerdicts.Contains(null).ShouldBeFalse();

    [Fact]
    public void AllTokensAvoidTheLegacyLifecycleLiterals()
    {
        // The dependency/scope/verdict tokens deliberately avoid pending/accepted/running/succeeded/cancelled so
        // ScaffoldArchitectureTests does not flag them and no allowlist entry is needed (Task 1) — guard that here.
        foreach (string token in ScopedOutageDependencies.All.Concat(ScopedOutageScopes.All).Concat(ScopedOutageDegradationVerdicts.All))
        {
            LegacyLifecycle.ShouldNotContain(token);
        }
    }
}
