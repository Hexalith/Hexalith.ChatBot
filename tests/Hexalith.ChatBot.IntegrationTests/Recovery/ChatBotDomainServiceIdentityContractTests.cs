using System.Text.RegularExpressions;

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
    public void DaprSuppliedAppIdResolvesToTheTopologyIdentity()
        => ChatBotDomainServiceIdentity.ResolveAppId(null, null, ChatBotAspireModule.AppId)
            .ShouldBe(ChatBotAspireModule.AppId);

    [Fact]
    public void ServiceVersionMatchesTheVersionEventStorePostsForAnUnversionedRegistration()
    {
        string eventStoreSource = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "references",
            "Hexalith.EventStore",
            "src",
            "Hexalith.EventStore",
            "Indexes",
            "AdminOperationalIndexHostedService.cs"));
        Match defaultVersion = Regex.Match(
            eventStoreSource,
            "GetServiceVersion\\(DomainServiceRegistration registration\\)[\\s\\S]{0,200}?"
            + "\\? \\\"(?<version>[A-Za-z0-9_.-]+)\\\" : registration\\.Version",
            RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

        defaultVersion.Success.ShouldBeTrue(
            "the contract test must read EventStore's own unversioned-registration default");
        ChatBotDomainServiceIdentity.ServiceVersion.ShouldBe(defaultVersion.Groups["version"].Value);
        ChatBotDomainServiceIdentity.IsUsableIdentityComponent(ChatBotDomainServiceIdentity.ServiceVersion)
            .ShouldBeTrue();
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Hexalith.ChatBot.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
