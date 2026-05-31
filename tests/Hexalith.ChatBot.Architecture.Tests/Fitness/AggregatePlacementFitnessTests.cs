using System.Reflection;

using Hexalith.ChatBot.Architecture.Tests.Fitness;

using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Handlers;

using NetArchTest.Rules;

using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests;

/// <summary>
/// Assembly/IL-level (NetArchTest/reflection) aggregate &amp; projection PLACEMENT fitness tests (AC4).
/// Aggregates/projections/domain processors live in <c>Hexalith.ChatBot.Server</c> only. Across every loaded
/// ChatBot assembly EXCEPT Server, no type may implement <c>IDomainProcessor</c>, derive from
/// <see cref="EventStoreAggregate{TState}"/>, or reside in a <c>…Operations</c> / <c>…Projections</c>
/// namespace — a violation fails the test and names the forbidden type. (EventStore.Client itself is excluded
/// from the scan set: its base aggregate implements the processor contract by design.)
/// </summary>
public static class AggregatePlacementFitnessTests
{
    [Fact]
    public static void DomainProcessorsLiveOnlyInServer()
    {
        // NetArchTest predicate over the compiled IL — names any non-Server type implementing IDomainProcessor.
        string?[] offenders = Types.InAssemblies(FitnessAssemblies.NonServerChatBotAssemblies)
            .That()
            .ImplementInterface(typeof(IDomainProcessor))
            .GetTypes()
            .Select(static type => type.FullName)
            .ToArray();

        offenders.ShouldBeEmpty(
            "IDomainProcessor implementations must live only in Hexalith.ChatBot.Server, found: "
            + string.Join(", ", offenders));
    }

    [Fact]
    public static void EventSourcedAggregatesLiveOnlyInServer()
    {
        // EventStoreAggregate<> is an OPEN generic — a reflection base-walk is clearer than the fluent API here.
        string[] offenders = FitnessAssemblies.NonServerChatBotAssemblies
            .SelectMany(LoadableTypes)
            .Where(static type => DerivesFromOpenGeneric(type, typeof(EventStoreAggregate<>)))
            .Select(static type => type.FullName ?? type.Name)
            .ToArray();

        offenders.ShouldBeEmpty(
            "EventStoreAggregate<> derivations must live only in Hexalith.ChatBot.Server, found: "
            + string.Join(", ", offenders));
    }

    [Fact]
    public static void OperationsAndProjectionsNamespacesLiveOnlyInServer()
    {
        string[] forbiddenNamespaceSuffixes = [".Operations", ".Projections"];

        string[] offenders = FitnessAssemblies.NonServerChatBotAssemblies
            .SelectMany(LoadableTypes)
            .Where(type => type.Namespace is { } ns
                && forbiddenNamespaceSuffixes.Any(suffix => ns.EndsWith(suffix, StringComparison.Ordinal)))
            .Select(static type => type.FullName ?? type.Name)
            .ToArray();

        offenders.ShouldBeEmpty(
            "*.Operations / *.Projections types must live only in Hexalith.ChatBot.Server, found: "
            + string.Join(", ", offenders));
    }

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(static type => type is not null)!;
        }
    }

    private static bool DerivesFromOpenGeneric(Type type, Type openGenericBase)
    {
        for (Type? current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openGenericBase)
            {
                return true;
            }
        }

        return false;
    }
}
