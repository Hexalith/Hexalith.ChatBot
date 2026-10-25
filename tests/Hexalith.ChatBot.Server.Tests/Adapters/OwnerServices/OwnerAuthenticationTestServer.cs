using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hexalith.ChatBot.Server.Tests.Adapters.OwnerServices;

/// <summary>A task-owned loopback Kestrel listener for real HTTP-factory and authenticated receiver tests.</summary>
internal sealed class OwnerAuthenticationTestServer(WebApplication application, Uri endpoint) : IAsyncDisposable
{
    /// <summary>Gets the allocated loopback endpoint.</summary>
    public Uri Endpoint { get; } = endpoint;

    /// <summary>Starts a listener, optionally with ASP.NET authentication registered by the caller.</summary>
    public static async Task<OwnerAuthenticationTestServer> StartAsync(RequestDelegate handler,
        Action<IServiceCollection>? configureAuthentication = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        configureAuthentication?.Invoke(builder.Services);
        WebApplication app = builder.Build();
        if (configureAuthentication is not null) app.UseAuthentication();
        app.Run(handler);
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        return new(app, new Uri(app.Urls.Single().TrimEnd('/') + "/"));
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => application.DisposeAsync();
}
