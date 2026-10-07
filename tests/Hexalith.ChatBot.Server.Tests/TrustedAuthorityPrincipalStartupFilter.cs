using System.Security.Claims;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Hexalith.ChatBot.Server.Tests;

/// <summary>Supplies authenticated receiver evidence to real SDK and HTTP test requests.</summary>
internal sealed class TrustedAuthorityPrincipalStartupFilter(Func<string, ClaimsPrincipal> principal) : IStartupFilter
{
    /// <inheritdoc/>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (http, continuation) =>
        {
            http.User = principal(http.Request.Path == "/process" ? "domain-service:process" : "domain-service:query");
            await continuation().ConfigureAwait(false);
        });
        next(app);
    };
}
