using System.Text.Json;

using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.AspNetCore.Hosting;

namespace Hexalith.ChatBot.Server.Tests.TrustedAuthority;

/// <summary>Owns the private finite receiver's loaded ACL and channel configuration for one in-process host.</summary>
internal sealed class HttpRouteCatalogFixtureFiles : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("chatbot-finite-http-").FullName;

    /// <summary>Creates only canonical SDK grants; the real receiver independently captures its installed endpoints.</summary>
    public HttpRouteCatalogFixtureFiles()
    {
        AccessControl = JsonSerializer.SerializeToUtf8Bytes(new
        {
            spec = new { accessControl = new
            {
                defaultAction = "deny", trustDomain = "public", policies = new[] { new
                {
                    appId = "chatbot-http-fixture-consumer", defaultAction = "deny",
                    operations = EventStoreDomainServiceRoutes.Operational.Select(route => new
                    {
                        name = route.Route.TrimStart('/'), action = "allow", httpVerb = new[] { "POST" },
                    }).ToArray(),
                } },
            } },
        });
        File.WriteAllBytes(Path.Combine(_directory, "acl.json"), AccessControl);
        Component = JsonSerializer.SerializeToUtf8Bytes(new
        {
            kind = "Component", metadata = new { name = "chatbot-http-fixture-catalog" },
            spec = new { type = "state.redis", metadata = new[] { new { name = "keyPrefix", value = "none" } } },
            scopes = new[] { "chatbot", "chatbot-http-fixture-consumer" },
        });
        File.WriteAllBytes(Path.Combine(_directory, "component.json"), Component);
    }

    /// <summary>Gets the exact ACL bytes loaded by the finite receiver.</summary>
    public byte[] AccessControl { get; }

    /// <summary>Gets the actual loaded in-process authority component description.</summary>
    public byte[] Component { get; }

    /// <summary>Configures an ordinary finite receiver without enrolling technical membership or production authority.</summary>
    public void Configure(IWebHostBuilder builder)
    {
        Dictionary<string, string> settings = new()
        {
            ["EventStore:RouteCatalog:DevelopmentBootstrap:Directory"] = _directory,
            ["EventStore:RouteCatalog:DevelopmentBootstrap:AccessControlPath"] = Path.Combine(_directory, "acl.json"),
            ["EventStore:RouteCatalog:DevelopmentBootstrap:ComponentPath"] = Path.Combine(_directory, "component.json"),
            ["EventStore:RouteCatalog:DevelopmentBootstrap:HostId"] = "chatbot",
            ["EventStore:RouteCatalog:DevelopmentBootstrap:CoordinatorHostId"] = "chatbot-http-fixture-consumer",
            ["EventStore:RouteCatalog:DevelopmentBootstrap:AttemptId"] = "chatbot-http-fixture",
            ["EventStore:RouteCatalog:DevelopmentBootstrap:RequiredHostIds:0"] = "chatbot",
            ["EventStore:RouteCatalog:DevelopmentBootstrap:RequiredHostIds:1"] = "chatbot-http-fixture-consumer",
            [DaprAppChannelToken.ConfigurationKey] = "chatbot-private-fixture-channel",
        };
        foreach ((string key, string value) in settings) { builder.UseSetting(key, value); }
    }

    /// <inheritdoc/>
    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
