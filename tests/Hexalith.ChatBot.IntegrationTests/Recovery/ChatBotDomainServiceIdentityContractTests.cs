using Hexalith.ChatBot.AppHost.Aspire;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// The identity the server presents must equal the DAPR app id the topology registers it under.
/// </summary>
/// <remarks>
/// <para>
/// These are two independent literals in two assemblies. EventStore invokes the service by the AppHost's app id
/// and compares the posted identity verbatim against the server's, so if they ever diverge the named-projection
/// capability is refused with <c>400 UnsupportedCapability</c> — and post-cutover that means the projection
/// checkpoint never advances and the poller re-delivers indefinitely.
/// </para>
/// <para>
/// A test that compares the resolved option to the constant it came from cannot see that: rename either literal
/// and it stays green while the defect returns. This assembly is the only one that sees both, so the cross-assembly
/// invariant is asserted here.
/// </para>
/// </remarks>
public sealed class ChatBotDomainServiceIdentityContractTests
{
    [Fact]
    public void ServerIdentityAppIdEqualsTheTopologyDaprAppId()
        => ChatBotDomainServiceIdentity.AppId.ShouldBe(ChatBotAspireModule.AppId);

    [Fact]
    public void ServiceVersionMatchesTheVersionEventStorePostsForAnUnversionedRegistration()
    {
        // EventStore's operational-index refresher sends registration.Version, defaulting to "v1" when the
        // registration declares none — which is how this topology registers the ChatBot domain service.
        ChatBotDomainServiceIdentity.ServiceVersion.ShouldBe("v1");
        ChatBotDomainServiceIdentity.IsUsableIdentityComponent(ChatBotDomainServiceIdentity.ServiceVersion)
            .ShouldBeTrue();
    }
}
