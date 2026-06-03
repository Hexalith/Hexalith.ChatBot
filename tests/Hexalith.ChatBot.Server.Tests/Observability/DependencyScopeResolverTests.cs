using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Observability;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Observability;

/// <summary>
/// Story 8.5 AC1: the dependency scope resolver picks the <b>narrowest</b> present safe scope token at every
/// precedence boundary (workflow-item &lt; operation &lt; command-surface &lt; service-client &lt; project &lt;
/// mailbox &lt; tenant) and fails closed to <see cref="DependencyScopeKind.Unknown"/> when no token is present —
/// never a fabricated broader scope.
/// </summary>
public sealed class DependencyScopeResolverTests
{
    [Fact]
    public void WorkflowItemBeatsEveryBroaderScope()
    {
        (DependencyScopeKind kind, string scope) = DependencyScopeResolver.Resolve(
            "wi-1", "op-1", "cs-1", "sc-1", "pr-1", "mb-1", "tn-1");

        kind.ShouldBe(DependencyScopeKind.WorkflowItem);
        scope.ShouldBe("workflow-item:wi-1");
    }

    [Fact]
    public void OperationBeatsCommandSurfaceWhenNoWorkflowItem()
    {
        (DependencyScopeKind kind, string scope) = DependencyScopeResolver.Resolve(
            null, "op-1", "cs-1", "sc-1", "pr-1", "mb-1", "tn-1");

        kind.ShouldBe(DependencyScopeKind.Operation);
        scope.ShouldBe("operation:op-1");
    }

    [Fact]
    public void CommandSurfaceBeatsServiceClientWhenNoNarrower()
    {
        (DependencyScopeKind kind, string scope) = DependencyScopeResolver.Resolve(
            null, null, "cs-1", "sc-1", "pr-1", "mb-1", "tn-1");

        kind.ShouldBe(DependencyScopeKind.CommandSurface);
        scope.ShouldBe("command-surface:cs-1");
    }

    [Fact]
    public void ServiceClientBeatsProjectWhenNoNarrower()
    {
        (DependencyScopeKind kind, string scope) = DependencyScopeResolver.Resolve(
            null, null, null, "sc-1", "pr-1", "mb-1", "tn-1");

        kind.ShouldBe(DependencyScopeKind.ServiceClient);
        scope.ShouldBe("service-client:sc-1");
    }

    [Fact]
    public void ProjectBeatsMailboxWhenNoNarrower()
    {
        (DependencyScopeKind kind, string scope) = DependencyScopeResolver.Resolve(
            null, null, null, null, "pr-1", "mb-1", "tn-1");

        kind.ShouldBe(DependencyScopeKind.Project);
        scope.ShouldBe("project:pr-1");
    }

    [Fact]
    public void MailboxBeatsTenantWhenNoNarrower()
    {
        (DependencyScopeKind kind, string scope) = DependencyScopeResolver.Resolve(
            null, null, null, null, null, "mb-1", "tn-1");

        kind.ShouldBe(DependencyScopeKind.Mailbox);
        scope.ShouldBe("mailbox:mb-1");
    }

    [Fact]
    public void TenantIsTheBroadestFallbackBeforeUnknown()
    {
        (DependencyScopeKind kind, string scope) = DependencyScopeResolver.Resolve(
            null, null, null, null, null, null, "tn-1");

        kind.ShouldBe(DependencyScopeKind.Tenant);
        scope.ShouldBe("tenant:tn-1");
    }

    [Fact]
    public void AllEmptyResolvesToUnknownFailClosed()
    {
        (DependencyScopeKind kind, string scope) = DependencyScopeResolver.Resolve(
            null, "   ", null, null, null, null, null);

        kind.ShouldBe(DependencyScopeKind.Unknown);
        scope.ShouldBe("scope:unknown");
    }

    [Fact]
    public void NonSafeTokenIsSkippedInFavorOfTheNextNarrowestSafeToken()
    {
        // The narrowest candidate carries a banned marker; the resolver skips it and falls through to the mailbox.
        (DependencyScopeKind kind, string scope) = DependencyScopeResolver.Resolve(
            "secret-workflow", null, null, null, null, "mb-1", "tn-1");

        kind.ShouldBe(DependencyScopeKind.Mailbox);
        scope.ShouldBe("mailbox:mb-1");
    }

    [Fact]
    public void AlreadyNamespacedTokenIsNotDoublePrefixed()
    {
        (DependencyScopeKind kind, string scope) = DependencyScopeResolver.Resolve(
            null, null, null, null, null, "mailbox:ops", null);

        kind.ShouldBe(DependencyScopeKind.Mailbox);
        scope.ShouldBe("mailbox:ops");
    }
}
