using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.ChatBot.Server.Tests.TrustedAuthority;

/// <summary>Supplies only committed fixture generation headers before the existing SDK receiver boundary.</summary>
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
                RouteCatalogGenerationReadback generation = await fixture.ActivateAsync(context.RequestServices, context.RequestAborted).ConfigureAwait(false);
                if (!context.Request.Headers.ContainsKey(RouteCatalogInvocationHandler.RootHeader)
                    && !context.Request.Headers.ContainsKey(RouteCatalogInvocationHandler.EpochHeader))
                {
                    context.Request.Headers[RouteCatalogInvocationHandler.RootHeader] = generation.RootDigest;
                    context.Request.Headers[RouteCatalogInvocationHandler.EpochHeader] = generation.Epoch.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            await proceed().ConfigureAwait(false);
        });
        next(app);
    };
}
