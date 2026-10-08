using System.Reflection;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Queries;
using Hexalith.ChatBot.Server.Lifecycle.AiExecution;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests;

public sealed class TrustedAuthorityBoundaryTests
{
    [Fact]
    public void CatalogCoversEveryCommandAndRegisteredQueryExactlyOnce()
    {
        ChatBotAuthorityCatalog catalog = new();
        string[] commands = typeof(IChatBotCommand).Assembly.GetTypes().Where(static type => type.IsClass && !type.IsAbstract && typeof(IChatBotCommand).IsAssignableFrom(type)).Select(static type => type.Name).Order(StringComparer.Ordinal).ToArray();
        catalog.Requirements.Where(static row => !row.IsQuery && row.Operation != AiExecutionRecoveryOperations.Recover).Select(static row => row.Operation).Order(StringComparer.Ordinal).ShouldBe(commands);
        string[] queries = typeof(ChatBotRequestAuthorizer).Assembly.GetTypes().Where(static type => type.IsClass && !type.IsAbstract && typeof(IDomainQueryHandler).IsAssignableFrom(type)).Select(type => (string)type.GetProperty("QueryType")!.GetValue(System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type))!).Order(StringComparer.Ordinal).ToArray();
        catalog.Requirements.Where(static row => row.IsQuery && row.Operation != AiExecutionRecoveryOperations.List).Select(static row => row.Operation).Order(StringComparer.Ordinal).ShouldBe(queries);
        catalog.Find(AiExecutionRecoveryOperations.List, true)!.AdminScope.ShouldBe("operate");
        catalog.Find(AiExecutionRecoveryOperations.Recover, false)!.AdminScope.ShouldBe("operate");
        catalog.Find("unknown-command", false).ShouldBeNull();
        Should.Throw<InvalidOperationException>(() => new ChatBotAuthorityCatalog(catalog.Requirements.Concat([catalog.Requirements.First()])));
        Should.Throw<InvalidOperationException>(() => new ChatBotAuthorityCatalog(catalog.Requirements.Skip(1)));
        Should.Throw<InvalidOperationException>(() => new ChatBotAuthorityCatalog(catalog.Requirements.Append(new("unknown", false, null, null, false, false))));
    }

    [Fact]
    public void MandatoryRegistrationsFailClosedWithoutOwnerMappings()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddChatBotCommandGateway();
        services.ShouldContain(static descriptor => descriptor.ServiceType == typeof(ChatBotRequestAuthorizer));
        services.ShouldContain(static descriptor => descriptor.ServiceType == typeof(ChatBotRequestContextResolver));
        services.ShouldContain(static descriptor => descriptor.ServiceType == typeof(IChatBotOwnerAuthorityProvider) && descriptor.ImplementationType == typeof(UnavailableChatBotOwnerAuthorityProvider));
        typeof(ChatBotCommandAdmissionPipeline).GetConstructors().Single().GetParameters().Single(static parameter => parameter.ParameterType == typeof(ChatBotRequestAuthorizer)).IsOptional.ShouldBeFalse();
        typeof(ChatBotReadQueryHandler<>).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single().GetParameters().ShouldContain(static parameter => parameter.ParameterType == typeof(ChatBotRequestAuthorizer) && !parameter.IsOptional);
    }

    /// <summary>Every concrete SDK query must inherit the mandatory trusted-read boundary.</summary>
    [Fact]
    public void EveryConcreteSdkQueryHandlerInheritsTheMandatoryAuthorityBoundary()
    {
        Type[] handlers = typeof(ChatBotRequestAuthorizer).Assembly.GetTypes()
            .Where(static type => type.IsClass && !type.IsAbstract && typeof(IDomainQueryHandler).IsAssignableFrom(type)).ToArray();
        handlers.ShouldNotBeEmpty();
        foreach (Type handler in handlers)
        {
            Type? parent = handler.BaseType;
            while (parent is not null && (!parent.IsGenericType || parent.GetGenericTypeDefinition() != typeof(ChatBotReadQueryHandler<>)))
            {
                parent = parent.BaseType;
            }
            parent.ShouldNotBeNull($"{handler.FullName} must inherit the mandatory trusted-read boundary.");
        }
    }

    [Fact]
    public void AuthoritySeamsRemainInternalAndAdaptersDependOnlyOnThePublicClient()
    {
        Type[] seams = typeof(ChatBotRequestAuthorizer).Assembly.GetTypes().Where(static type => type.Namespace is "Hexalith.ChatBot.Server.Authorization" or "Hexalith.ChatBot.Server.Authentication").ToArray();
        seams.ShouldAllBe(static type => !type.IsPublic);
        Assembly[] adapters = [typeof(Hexalith.ChatBot.Cli.ChatBotCliService).Assembly, typeof(Hexalith.ChatBot.Mcp.ChatBotMcpService).Assembly];
        foreach (Assembly adapter in adapters)
        {
            adapter.GetReferencedAssemblies().ShouldNotContain(static name => name.Name == "Hexalith.ChatBot.Server");
        }
    }
}
