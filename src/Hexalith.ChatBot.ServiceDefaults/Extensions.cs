using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hexalith.ChatBot.ServiceDefaults;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Services.AddServiceDiscovery();
        _ = builder.Services.ConfigureHttpClientDefaults(static http =>
        {
            _ = http.AddStandardResilienceHandler();
            _ = http.AddServiceDiscovery();
        });

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        _ = app.MapGet("/health", static () => Results.Ok("Healthy"));
        _ = app.MapGet("/alive", static () => Results.Ok("Alive"));
        return app;
    }
}
