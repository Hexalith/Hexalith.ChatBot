using Hexalith.ChatBot.Client;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>
/// Story 13.9 real-render host: boots the actual <c>Hexalith.ChatBot.UI</c> Blazor Server app
/// (<see cref="Program"/>) on a loopback Kestrel listener (<c>127.0.0.1</c>, OS-assigned free port) that a
/// Playwright browser can reach over a real socket. The in-memory <c>TestServer</c> that
/// <see cref="WebApplicationFactory{TEntryPoint}"/> uses by default is NOT browser-reachable, so this factory
/// stands up a second, real Kestrel host (the documented minimal-hosting double-host pattern) and exposes its
/// dynamic address through <see cref="BaseUri"/>.
/// <para>
/// The only overridden service is <see cref="IChatBotClient"/>, replaced by <see cref="FakeChatBotClient"/> so
/// every routable surface renders from a deterministic UI-boundary seam without any live backend dependency
/// (Server, gateway, EventStore, Dapr). No production service, projection, store, or transport is touched.
/// </para>
/// </summary>
internal sealed class LiveChatBotUiHost : WebApplicationFactory<Program>
{
    private IHost? _kestrelHost;

    /// <summary>Gets the loopback base URI (no trailing slash) the browser navigates to, e.g. <c>http://127.0.0.1:53124</c>.</summary>
    public string BaseUri => ClientOptions.BaseAddress.ToString().TrimEnd('/');

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development environment + explicit static-web-assets load so Fluent UI's _content/* JS/CSS and the UI
        // wwwroot assets are served by the real host exactly as they are at runtime. The explicit call is required
        // because WebApplication.CreateBuilder only auto-loads the manifest when the environment is already
        // Development at construction time, which is before WebApplicationFactory flips it here.
        builder.UseEnvironment("Development");
        _ = builder.UseStaticWebAssets();

        builder.ConfigureTestServices(services =>
        {
            // Swap the live typed Client facade for the deterministic metadata-only fake. Everything else
            // (FrontComposer shell, Fluxor, localization, Fluent UI) stays exactly as the production host wires it.
            services.RemoveAll<IChatBotClient>();
            services.AddScoped<IChatBotClient, FakeChatBotClient>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Build (but do not yet start) the in-memory TestServer host the base class expects.
        IHost testHost = builder.Build();

        // Re-configure the same builder to use a real Kestrel listener on a loopback dynamic port, then build and
        // start that second host. Kestrel must be started before the TestServer host so the minimal-hosting
        // deferred builder initialises the server address feature.
        _ = builder.ConfigureWebHost(webHostBuilder => webHostBuilder
            .UseKestrel()
            .UseUrls("http://127.0.0.1:0"));

        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        IServer server = _kestrelHost.Services.GetRequiredService<IServer>();
        IServerAddressesFeature addresses = server.Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("The loopback Kestrel host did not expose a server address feature.");
        ClientOptions.BaseAddress = addresses.Addresses
            .Select(static address => new Uri(address))
            .Last();

        // Return the TestServer host so WebApplicationFactory internals stay consistent.
        testHost.Start();
        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _kestrelHost?.Dispose();
        }
    }
}
