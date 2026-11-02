using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hexalith.ChatBot.Server.Tests.TrustedAuthority;

/// <summary>Activates ordinary fixture authority and sends unbound calls through the actual SDK invocation handler.</summary>
internal sealed class HttpRouteCatalogStartupFilter : IStartupFilter
{
    /// <inheritdoc/>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, proceed) =>
        {
            HttpRouteCatalogFixture fixture = context.RequestServices.GetRequiredService<HttpRouteCatalogFixture>();
            if (fixture.AutomaticActivation && EventStoreDomainServiceRoutes.Operational.Any(route => route.Route == context.Request.Path.Value))
            {
                await fixture.ActivateAsync(context.RequestServices, context.RequestAborted).ConfigureAwait(false);
                if (!context.Request.Headers.ContainsKey(RouteCatalogInvocationHandler.RootHeader)
                    && !context.Request.Headers.ContainsKey(RouteCatalogInvocationHandler.EpochHeader))
                {
                    // The real SDK sender owns all catalog headers and holds its lease through the receiver response.
                    // A separate custom invocation transport preserves this fixture's in-process delivery semantics.
                    using HttpMessageInvoker sender = new(new RouteCatalogInvocationHandler(
                        context.RequestServices.GetRequiredService<IRouteCatalogAuthority>(),
                        new HttpRouteCatalogInvocationTransport(context, proceed),
                        Options.Create(new RouteCatalogHostOptions
                        {
                            InvocationTransportFactory = _ => new HttpRouteCatalogInvocationTransport(context, proceed),
                        })));
                    using HttpRequestMessage request = new(new HttpMethod(context.Request.Method),
                        "http://localhost" + context.Request.Path)
                    {
                        Content = new StreamContent(context.Request.Body),
                    };
                    foreach (var header in context.Request.Headers)
                    {
                        if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                        {
                            request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                        }
                    }
                    // Model actual configured app-channel delivery; explicit caller tokens remain unmodified.
                    if (!request.Headers.Contains(DaprAppChannelToken.HeaderName))
                    {
                        request.Headers.TryAddWithoutValidation(DaprAppChannelToken.HeaderName, context.RequestServices
                            .GetRequiredService<IConfiguration>()[DaprAppChannelToken.ConfigurationKey]);
                    }
                    request.Headers.Remove("dapr-app-id");
                    request.Headers.TryAddWithoutValidation("dapr-app-id", context.RequestServices
                        .GetRequiredService<IOptions<DomainProjectionIdentityOptions>>().Value.AppId);
                    using HttpResponseMessage response = await sender.SendAsync(request, context.RequestAborted).ConfigureAwait(false);
                    return;
                }
            }
            await proceed().ConfigureAwait(false);
        });
        next(app);
    };
}
