using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Projections.DerivedStores;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections.DerivedStores;

/// <summary>Verifies fail-closed production selection of the distinct Memories live adapters.</summary>
public sealed class MemoriesDerivedStoreDependencyInjectionTests
{
    [Fact]
    public void ProductionResolvesBothLiveAdaptersAndRemovesTheProcessLocalLedger()
    {
        IConfiguration configuration = Configuration(
            (MemoriesDerivedStoreServiceCollectionExtensions.EndpointConfigurationKey, "http://memories/"),
            (MemoriesDerivedStoreServiceCollectionExtensions.ProjectsEndpointConfigurationKey, "http://projects/"),
            (MemoriesDerivedStoreServiceCollectionExtensions.ProjectsApiTokenConfigurationKey, "test-projects-token"),
            (MemoriesDerivedStoreServiceCollectionExtensions.WorkflowRuntimeConfigurationKey, "true"));
        ServiceCollection services = new();
        _ = services.AddChatBotCommandGateway();
        _ = services.AddChatBotMemoriesDerivedStores(configuration, Environment(Environments.Production));

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IDerivedStore>().ShouldBeOfType<MemoriesDerivedStore>();
        provider.GetRequiredService<IVectorReindexer>().ShouldBeOfType<MemoriesVectorReindexer>();
        provider.GetService<IVectorReindexLedger>().ShouldBeNull();
    }

    [Fact]
    public void DevelopmentKeepsDeterministicInMemoryDefaultsUnlessLiveBackingIsExplicitlyEnabled()
    {
        ServiceCollection services = new();
        _ = services.AddChatBotCommandGateway();
        _ = services.AddChatBotMemoriesDerivedStores(new ConfigurationBuilder().Build(), Environment(Environments.Development));

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IDerivedStore>().ShouldBeOfType<InMemoryDerivedStore>();
        provider.GetRequiredService<IVectorReindexer>().ShouldBeOfType<InMemoryVectorReindexer>();
        provider.GetRequiredService<IVectorReindexLedger>().ShouldBeOfType<InMemoryVectorReindexLedger>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("redis://memories:6379")]
    [InlineData("http://user:secret@memories/")]
    public void ProductionRejectsMissingOrInvalidLiveEndpoint(string? endpoint)
    {
        IConfiguration configuration = Configuration(
            (MemoriesDerivedStoreServiceCollectionExtensions.EndpointConfigurationKey, endpoint),
            (MemoriesDerivedStoreServiceCollectionExtensions.ProjectsEndpointConfigurationKey, "http://projects/"),
            (MemoriesDerivedStoreServiceCollectionExtensions.ProjectsApiTokenConfigurationKey, "test-projects-token"),
            (MemoriesDerivedStoreServiceCollectionExtensions.WorkflowRuntimeConfigurationKey, "true"));
        ServiceCollection services = new();
        _ = services.AddChatBotCommandGateway();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            services.AddChatBotMemoriesDerivedStores(configuration, Environment(Environments.Production)));

        exception.Message.ShouldContain(MemoriesDerivedStoreServiceCollectionExtensions.EndpointConfigurationKey);
    }

    [Fact]
    public void ProductionRejectsDisabledDurableWorkflowRuntime()
    {
        IConfiguration configuration = Configuration(
            (MemoriesDerivedStoreServiceCollectionExtensions.EndpointConfigurationKey, "http://memories/"),
            (MemoriesDerivedStoreServiceCollectionExtensions.ProjectsEndpointConfigurationKey, "http://projects/"),
            (MemoriesDerivedStoreServiceCollectionExtensions.ProjectsApiTokenConfigurationKey, "test-projects-token"));
        ServiceCollection services = new();
        _ = services.AddChatBotCommandGateway();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            services.AddChatBotMemoriesDerivedStores(configuration, Environment(Environments.Production)));

        exception.Message.ShouldContain(MemoriesDerivedStoreServiceCollectionExtensions.WorkflowRuntimeConfigurationKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("redis://projects:6379")]
    [InlineData("http://user:secret@projects/")]
    [InlineData("http://projects/?token=secret")]
    [InlineData("http://projects/#secret")]
    public void ProductionRejectsMissingOrInvalidProjectsAuthorityEndpoint(string? endpoint)
    {
        IConfiguration configuration = Configuration(
            (MemoriesDerivedStoreServiceCollectionExtensions.EndpointConfigurationKey, "http://memories/"),
            (MemoriesDerivedStoreServiceCollectionExtensions.ProjectsEndpointConfigurationKey, endpoint),
            (MemoriesDerivedStoreServiceCollectionExtensions.ProjectsApiTokenConfigurationKey, "test-projects-token"),
            (MemoriesDerivedStoreServiceCollectionExtensions.WorkflowRuntimeConfigurationKey, "true"));
        ServiceCollection services = new();
        _ = services.AddChatBotCommandGateway();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            services.AddChatBotMemoriesDerivedStores(configuration, Environment(Environments.Production)));

        exception.Message.ShouldContain(MemoriesDerivedStoreServiceCollectionExtensions.ProjectsEndpointConfigurationKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ProductionRejectsMissingProjectsAuthorityToken(string? token)
    {
        IConfiguration configuration = Configuration(
            (MemoriesDerivedStoreServiceCollectionExtensions.EndpointConfigurationKey, "http://memories/"),
            (MemoriesDerivedStoreServiceCollectionExtensions.ProjectsEndpointConfigurationKey, "http://projects/"),
            (MemoriesDerivedStoreServiceCollectionExtensions.ProjectsApiTokenConfigurationKey, token),
            (MemoriesDerivedStoreServiceCollectionExtensions.WorkflowRuntimeConfigurationKey, "true"));
        ServiceCollection services = new();
        _ = services.AddChatBotCommandGateway();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            services.AddChatBotMemoriesDerivedStores(configuration, Environment(Environments.Production)));

        exception.Message.ShouldContain(MemoriesDerivedStoreServiceCollectionExtensions.ProjectsApiTokenConfigurationKey);
    }

    private static IConfiguration Configuration(params (string Key, string? Value)[] values)
        => new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(static pair => pair.Key, static pair => pair.Value)).Build();

    private static IHostEnvironment Environment(string environmentName)
        => new TestHostEnvironment { EnvironmentName = environmentName };

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Hexalith.ChatBot.Server.Tests";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
