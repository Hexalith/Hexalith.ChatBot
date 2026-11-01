using System.Text.Json;

using Hexalith.ChatBot.Server.Routing;
using Hexalith.ChatBot.Server.Tests.TrustedAuthority;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Routing;

/// <summary>Checks production Development enrollment in process; it supplies no deployed or native acceptance.</summary>
public sealed class ChatBotDevelopmentBootstrapTests
{
    /// <summary>The real bootstrap worker must publish the actual owner observer registered by Program.</summary>
    [Fact]
    public async Task DevelopmentBootstrapMustPublishChatBotOwnerObservation()
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("chatbot-bootstrap-");
        try
        {
            string component = Path.Combine(directory.FullName, "authority-component.json");
            string acl = Path.Combine(directory.FullName, "receiver-acl.json");
            await File.WriteAllBytesAsync(component, JsonSerializer.SerializeToUtf8Bytes(new
            {
                kind = "Component", metadata = new { name = "chatbot-http-fixture-catalog" },
                spec = new { type = "state.redis", metadata = new[] { new { name = "keyPrefix", value = "none" } } },
                scopes = new[] { "chatbot", "catalog-coordinator" },
            }), TestContext.Current.CancellationToken);
            await File.WriteAllBytesAsync(acl, JsonSerializer.SerializeToUtf8Bytes(new
            {
                spec = new
                {
                    accessControl = new
                    {
                        defaultAction = "deny", trustDomain = "public",
                        policies = new[]
                        {
                            new
                            {
                                appId = "catalog-coordinator", @namespace = "default", trustDomain = "public", defaultAction = "deny",
                                operations = new[] { "/process", "/query", "/project" }
                                    .Select(name => new { name, action = "allow", httpVerb = new[] { "POST" } }).ToArray(),
                            },
                        },
                    },
                },
            }), TestContext.Current.CancellationToken);
            Dictionary<string, string> settings = new()
            {
                ["APP_API_TOKEN"] = "chatbot-bootstrap-fixture-channel",
                ["EventStore:RouteCatalog:DevelopmentBootstrap:Directory"] = directory.FullName,
                ["EventStore:RouteCatalog:DevelopmentBootstrap:HostId"] = "chatbot",
                ["EventStore:RouteCatalog:DevelopmentBootstrap:CoordinatorHostId"] = "catalog-coordinator",
                ["EventStore:RouteCatalog:DevelopmentBootstrap:AttemptId"] = "chatbot-bootstrap-fixture",
                ["EventStore:RouteCatalog:DevelopmentBootstrap:RequiredHostIds:0"] = "chatbot",
                ["EventStore:RouteCatalog:DevelopmentBootstrap:RequiredHostIds:1"] = "catalog-coordinator",
                ["EventStore:RouteCatalog:DevelopmentBootstrap:ComponentPath"] = component,
                ["EventStore:RouteCatalog:DevelopmentBootstrap:AccessControlPath"] = acl,
            };
            using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                foreach ((string key, string value) in settings) { builder.UseSetting(key, value); }
                builder.ConfigureServices(services =>
                {
                    // Replace only authority storage; Program owns bootstrap, observation, and the real receiver fence.
                    services.RemoveAll<IRouteCatalogStateStore>();
                    services.AddSingleton<IRouteCatalogStateStore, HttpRouteCatalogStateStore>();
                });
            });
            using HttpClient client = factory.CreateClient();
            factory.Services.GetRequiredService<IRouteCatalogHostObserver>().ShouldBeOfType<ObservedRouteCatalogHost>();
            factory.Services.GetService<HttpRouteCatalogFixture>().ShouldBeNull();
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            string emitted = Path.Combine(directory.FullName, "chatbot.observation.json");
            while (!File.Exists(emitted)) { await Task.Delay(25, timeout.Token); }
            RouteCatalogHostObservation observation = JsonSerializer.Deserialize<RouteCatalogHostObservation>(
                await File.ReadAllBytesAsync(emitted, timeout.Token)).ShouldNotBeNull();

            observation.HostId.ShouldBe("chatbot");
            observation.AppId.ShouldBe("chatbot");
            observation.RequiredHostIds.ShouldBe(["chatbot", "catalog-coordinator"]);
            observation.Declarations.Select(RouteCatalogCodec.ComputeDeclarationDigest).Order()
                .ShouldBe(ChatBotCatalogDeclarations.Routes.Select(RouteCatalogCodec.ComputeDeclarationDigest).Order());
            observation.Operations.ShouldNotBeNull().Select(operation => operation.Digest).Order()
                .ShouldBe(ChatBotCatalogDeclarations.Operations.Select(operation => operation.Digest).Order());
            observation.Targets.Count.ShouldBe(73);
            observation.Targets.ShouldAllBe(target => target.AppId == "chatbot");
            observation.ReceiverSurface.ShouldNotBeNull().FenceVersion.ShouldBe(FiniteRouteCatalogReceiver.Version);
            foreach (string path in new[] { "/process", "/query", "/project" })
            {
                RouteCatalogReceiverEndpoint receiver = observation.ReceiverSurface.Endpoints.Where(endpoint => endpoint.Pattern == path).ShouldHaveSingleItem();
                receiver.Methods.ShouldBe(["POST"]);
                receiver.Admitted.ShouldBeTrue();
                receiver.Anonymous.ShouldBeFalse();
                receiver.Policies.ShouldContain(EventStoreDomainServiceRoutes.Operational.Single(route => route.Route == path).Policy);
            }
            observation.AdmissionPolicies.ShouldNotBeNull().ShouldBeEmpty();
            (await factory.Services.GetRequiredService<IRouteCatalogStateStore>().ReadAsync(timeout.Token)).State.Committed.ShouldBeNull();
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
