using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

/// <summary>
/// The projection identity this service presents to EventStore's named-projection capability negotiation.
/// </summary>
/// <remarks>
/// It was never pinned, so the SDK derived it from <c>DAPR_APP_ID</c> falling back to the .NET application name
/// (<c>Hexalith.ChatBot.Server</c>) — never the DAPR app id EventStore registers and invokes this service under —
/// and the SDK answered <c>400 UnsupportedCapability</c> to every operational-index refresh. Post-cutover that
/// stops the projection checkpoint advancing at all, because advancement requires the named fenced completion, so
/// the poller re-delivers the same aggregates indefinitely. These tests exist so the identity cannot silently
/// regress to the assembly name or to blank.
/// </remarks>
public sealed class ChatBotProjectionIdentityTests
{
    [Fact]
    public void ResolvedIdentityIsNonEmptyAndMatchesTheDaprAppIdEventStoreInvokes()
    {
        using WebApplicationFactory<Program> factory = new();

        DomainProjectionIdentityOptions identity = factory.Services
            .GetRequiredService<IOptions<DomainProjectionIdentityOptions>>()
            .Value;

        // The defect was an identity that resolved to the assembly name; the SDK compares it to the caller's
        // registered DAPR app id and refuses the whole capability on mismatch.
        identity.AppId.ShouldNotBeNullOrWhiteSpace();
        identity.ServiceVersion.ShouldNotBeNullOrWhiteSpace();
        identity.AppId.ShouldNotBe(typeof(Program).Assembly.GetName().Name);
        identity.AppId.ShouldBe(ChatBotDomainServiceIdentity.AppId);
        identity.ServiceVersion.ShouldBe(ChatBotDomainServiceIdentity.ServiceVersion);
        Should.NotThrow(identity.Validate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankConfiguredIdentityFallsBackToTheDaprAppIdRatherThanTheAssemblyName(string configured)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting(
                $"{ChatBotDomainServiceIdentity.ConfigurationSection}:AppId",
                configured));

        DomainProjectionIdentityOptions identity = factory.Services
            .GetRequiredService<IOptions<DomainProjectionIdentityOptions>>()
            .Value;

        identity.AppId.ShouldBe(ChatBotDomainServiceIdentity.AppId);
        Should.NotThrow(identity.Validate);
    }

    [Fact]
    public void AConfiguredIdentityOverridesTheDefaultSoADeployedVersionCanDiffer()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                _ = builder.UseSetting($"{ChatBotDomainServiceIdentity.ConfigurationSection}:AppId", "chatbot-canary");
                _ = builder.UseSetting($"{ChatBotDomainServiceIdentity.ConfigurationSection}:ServiceVersion", "v2");
            });

        DomainProjectionIdentityOptions identity = factory.Services
            .GetRequiredService<IOptions<DomainProjectionIdentityOptions>>()
            .Value;

        identity.AppId.ShouldBe("chatbot-canary");
        identity.ServiceVersion.ShouldBe("v2");
    }
}
