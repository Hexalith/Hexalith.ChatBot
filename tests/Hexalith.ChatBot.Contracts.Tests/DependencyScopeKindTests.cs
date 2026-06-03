using Hexalith.ChatBot.Contracts.Enums;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

/// <summary>
/// Story 8.5 AC1: the <see cref="DependencyScopeKind"/> wire-token surface mirrors the sibling enum convention —
/// every kind maps to its stable kebab/underscore token, an undefined kind throws, and <see cref="DependencyScopeKinds.All"/>
/// is ordered narrowest→broadest (the exact precedence the resolver applies) and excludes the fail-closed
/// <see cref="DependencyScopeKind.Unknown"/>.
/// </summary>
public static class DependencyScopeKindTests
{
    [Theory]
    [InlineData(DependencyScopeKind.WorkflowItem, "workflow-item")]
    [InlineData(DependencyScopeKind.Operation, "operation")]
    [InlineData(DependencyScopeKind.CommandSurface, "command-surface")]
    [InlineData(DependencyScopeKind.ServiceClient, "service-client")]
    [InlineData(DependencyScopeKind.Project, "project")]
    [InlineData(DependencyScopeKind.Mailbox, "mailbox")]
    [InlineData(DependencyScopeKind.Tenant, "tenant")]
    [InlineData(DependencyScopeKind.Unknown, "unknown")]
    public static void ToWireValueShouldMapEveryKindToItsStableToken(DependencyScopeKind kind, string token)
        => DependencyScopeKinds.ToWireValue(kind).ShouldBe(token);

    [Fact]
    public static void ToWireValueShouldThrowForAnUndefinedKind()
        => Should.Throw<ArgumentOutOfRangeException>(() => DependencyScopeKinds.ToWireValue((DependencyScopeKind)999));

    [Fact]
    public static void AllShouldBeOrderedNarrowestToBroadestAndExcludeUnknown()
    {
        DependencyScopeKinds.All.ShouldBe(
        [
            DependencyScopeKind.WorkflowItem,
            DependencyScopeKind.Operation,
            DependencyScopeKind.CommandSurface,
            DependencyScopeKind.ServiceClient,
            DependencyScopeKind.Project,
            DependencyScopeKind.Mailbox,
            DependencyScopeKind.Tenant,
        ]);

        DependencyScopeKinds.All.ShouldNotContain(DependencyScopeKind.Unknown);
    }
}
