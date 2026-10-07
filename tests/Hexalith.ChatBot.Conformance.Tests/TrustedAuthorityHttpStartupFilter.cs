using Hexalith.ChatBot.Tests.TrustedAuthority;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>Supplies genuinely authenticated synthetic actors to the actual server HTTP boundary.</summary>
internal sealed class TrustedAuthorityHttpStartupFilter(string tenant, string actorClass) : IStartupFilter
{
    /// <inheritdoc/>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        => application =>
        {
            application.Use(async (context, continuation) =>
            {
                context.User = TrustedAuthorityFixture.Principal(tenant, actorClass);
                await continuation().ConfigureAwait(false);
            });
            next(application);
        };
}
