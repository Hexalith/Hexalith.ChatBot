using Hexalith.ChatBot.UI.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ChatBotOidcConfigurationTests
{
    public static TheoryData<Dictionary<string, string?>> InvalidProductionConfigurations => new()
    {
        new Dictionary<string, string?>(),
        Configuration(authority: "https://identity.example", clientId: null),
        Configuration(authority: null, clientId: "hexalith-chatbot"),
        Configuration(authority: "file:///identity", clientId: "hexalith-chatbot"),
        Configuration(authority: "identity.example", clientId: "hexalith-chatbot"),
        Configuration(
            authority: "https://identity.example",
            clientId: "hexalith-chatbot",
            issuer: "urn:identity:issuer"),
    };

    [Theory]
    [MemberData(nameof(InvalidProductionConfigurations))]
    public void ProductionStartupShouldFailClosedForAbsentPartialMalformedOrInvalidOidc(
        Dictionary<string, string?> values)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        _ = Should.Throw<InvalidOperationException>(
            () => ChatBotOidcConfiguration.Resolve(configuration, Environment(Environments.Production)));
    }

    [Fact]
    public void ValidProductionOidcShouldResolveAudienceAndIssuerDefaults()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(Configuration("https://identity.example/realms/chatbot/", "hexalith-chatbot"))
            .Build();

        ChatBotOidcConfiguration resolved = ChatBotOidcConfiguration.Resolve(
            configuration,
            Environment(Environments.Production));

        resolved.Enabled.ShouldBeTrue();
        resolved.Authority.ShouldBe(new Uri("https://identity.example/realms/chatbot/"));
        resolved.ClientId.ShouldBe("hexalith-chatbot");
        resolved.Audience.ShouldBe("hexalith-chatbot");
        resolved.Issuer.ShouldBe("https://identity.example/realms/chatbot");
    }

    [Fact]
    public void UnconfiguredDevelopmentOidcShouldRemainDisabledForLocalCompatibility()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        ChatBotOidcConfiguration resolved = ChatBotOidcConfiguration.Resolve(
            configuration,
            Environment(Environments.Development));

        resolved.Enabled.ShouldBeFalse();
    }

    private static Dictionary<string, string?> Configuration(
        string? authority,
        string? clientId,
        string? audience = null,
        string? issuer = null)
        => new()
        {
            ["Authentication:OpenIdConnect:Authority"] = authority,
            ["Authentication:OpenIdConnect:ClientId"] = clientId,
            ["Authentication:OpenIdConnect:Audience"] = audience,
            ["Authentication:OpenIdConnect:Issuer"] = issuer,
        };

    private static IHostEnvironment Environment(string name)
        => new TestHostEnvironment { EnvironmentName = name };

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Hexalith.ChatBot.UI.Tests";

        public string ContentRootPath { get; set; } = "/";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
