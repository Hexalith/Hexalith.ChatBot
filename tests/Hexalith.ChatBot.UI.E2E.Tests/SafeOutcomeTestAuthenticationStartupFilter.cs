using System.Security.Claims;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>Supplies a consistent test principal to prerender and interactive requests in the safe outcome browser host.</summary>
internal sealed class SafeOutcomeTestAuthenticationStartupFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        => app =>
        {
            app.Use(async (context, nextRequest) =>
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("eventstore:tenant", "tenant-alpha"), new Claim("sub", "actor-alpha")],
                    "SafeOutcomeTests"));
                await nextRequest(context).ConfigureAwait(false);
            });
            next(app);
        };
}
