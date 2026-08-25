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
/// the poller re-delivers the same aggregates indefinitely.
/// </remarks>
public sealed class ChatBotProjectionIdentityTests
{
    [Fact]
    public void ResolvedIdentityIsUsableAndIsNotTheAssemblyName()
    {
        using WebApplicationFactory<Program> factory = new();

        DomainProjectionIdentityOptions identity = factory.Services
            .GetRequiredService<IOptions<DomainProjectionIdentityOptions>>()
            .Value;

        ChatBotDomainServiceIdentity.IsUsableIdentityComponent(identity.AppId).ShouldBeTrue();
        ChatBotDomainServiceIdentity.IsUsableIdentityComponent(identity.ServiceVersion).ShouldBeTrue();
        identity.AppId.ShouldNotBe(typeof(Program).Assembly.GetName().Name);

        // Asserting equality with the pinned constant would make this test environment-dependent: DAPR_APP_ID sits
        // ABOVE the constant in the precedence chain, so any developer or agent running under `dapr run` -- and any
        // runner that exports it -- would turn the story's headline regression guard red for a correctly behaving
        // service. Assert the property that actually matters and hold the constant only when nothing overrides it.
        if (Environment.GetEnvironmentVariable("DAPR_APP_ID") is null or "")
        {
            identity.AppId.ShouldBe(ChatBotDomainServiceIdentity.AppId);
        }
    }

    /// <summary>
    /// The <c>ValidateOnStart</c> gate must actually refuse an unusable configured identity.
    /// </summary>
    /// <remarks>
    /// Without this, deleting <c>.Validate(...)</c> and <c>.ValidateOnStart()</c> from <c>Program.cs</c> failed
    /// nothing: the predicate was covered only in isolation, and both host-level tests configured valid values. A
    /// deployment with a malformed AppId would then boot and have every operational-index refresh refused, which
    /// post-cutover is a permanently stalled projection checkpoint.
    /// </remarks>
    [Theory]
    [InlineData("AppId", "chat bot")]
    [InlineData("AppId", "chatbot/v1")]
    [InlineData("ServiceVersion", "v 1")]
    public void AnUnusableConfiguredIdentityFailsStartup(string component, string value)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting(
                $"{ChatBotDomainServiceIdentity.ConfigurationSection}:{component}",
                value));

        _ = Should.Throw<OptionsValidationException>(() => factory.Services
            .GetRequiredService<IOptions<DomainProjectionIdentityOptions>>()
            .Value);
    }

    /// <summary>
    /// A malformed candidate must NOT fall through to the next precedence tier.
    /// </summary>
    /// <remarks>
    /// The resolver deliberately selects the first non-blank candidate without filtering for usability, so a
    /// malformed value reaches the startup gate and fails the host rather than silently resolving to a different
    /// identity than the operator configured. Pin that direction: making the resolver skip malformed candidates
    /// would substitute a silent wrong answer for a noisy refusal.
    /// </remarks>
    [Fact]
    public void AMalformedHigherPrecedenceCandidateDoesNotFallThroughToTheConstant()
        => ChatBotDomainServiceIdentity.ResolveAppId("chat bot", null, null).ShouldBe("chat bot");

    /// <summary>
    /// Precedence is the whole point: an unconditional override silently kills <c>EventStore:DomainService</c>
    /// binding and <c>DAPR_APP_ID</c>, so any prefixed, canary or multi-instance deployment reintroduces the
    /// mismatch this exists to prevent.
    /// </summary>
    /// <param name="chatBotConfigured">The ChatBot-specific override.</param>
    /// <param name="sdkConfigured">The SDK's own configuration key.</param>
    /// <param name="daprAppId">The DAPR-supplied app id.</param>
    /// <param name="expected">The app id that must win.</param>
    [Theory]
    [InlineData("chatbot-canary", "chatbot-sdk", "chatbot-dapr", "chatbot-canary")]
    [InlineData(null, "chatbot-sdk", "chatbot-dapr", "chatbot-sdk")]
    [InlineData(null, null, "chatbot-dapr", "chatbot-dapr")]
    [InlineData(null, null, null, "chatbot")]
    [InlineData("   ", "  ", "chatbot-dapr", "chatbot-dapr")]
    public void AppIdPrecedenceIsExplicitConfigThenSdkConfigThenDaprThenConstant(
        string? chatBotConfigured,
        string? sdkConfigured,
        string? daprAppId,
        string expected)
        => ChatBotDomainServiceIdentity.ResolveAppId(chatBotConfigured, sdkConfigured, daprAppId).ShouldBe(expected);

    [Theory]
    [InlineData("v2", null, "v2")]
    [InlineData(null, "v3", "v3")]
    [InlineData(null, null, "v1")]
    public void ServiceVersionPrecedenceFollowsTheSameOrder(string? configured, string? sdkConfigured, string expected)
        => ChatBotDomainServiceIdentity.ResolveServiceVersion(configured, sdkConfigured).ShouldBe(expected);

    /// <summary>
    /// The validation must be reachable: EventStore compares these verbatim, so an identity that is not a safe
    /// stable identifier can only ever produce a silent capability refusal.
    /// </summary>
    /// <param name="candidate">The component under test.</param>
    /// <param name="expected">Whether it is usable.</param>
    [Theory]
    [InlineData("chatbot", true)]
    [InlineData("chatbot-canary.1", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("chat bot", false)]
    [InlineData("chatbot/v1", false)]
    public void OnlySafeStableIdentifiersAreUsable(string candidate, bool expected)
        => ChatBotDomainServiceIdentity.IsUsableIdentityComponent(candidate).ShouldBe(expected);

    [Fact]
    public void AConfiguredIdentityOverridesTheDefaultEndToEnd()
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
