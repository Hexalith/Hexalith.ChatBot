using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Hexalith.ChatBot.UI.Hosting;

internal static class ChatBotUiHostDefaultsExtensions
{
    public static IHostApplicationBuilder AddChatBotUiHostDefaults(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        _ = builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => _ = metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => _ = tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            _ = builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        _ = builder.Services.AddServiceDiscovery();
        _ = builder.Services.ConfigureHttpClientDefaults(static http =>
        {
            _ = http.AddStandardResilienceHandler(static resilience =>
            {
                // CommandGateway POSTs already own durable idempotency and expose an in-progress duplicate as a
                // conflict. Transport-level retries can race the original admission and turn a successful command
                // into a visible 409, so retry only safe HTTP methods; callers explicitly decide mutation retries.
                resilience.Retry.DisableForUnsafeHttpMethods();

                // The standard 10-second attempt timeout is shorter than a cold governed EventStore admission.
                // Keep that wait bounded while satisfying the standard pipeline's sampling-duration coupling.
                resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
                resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
            });
            _ = http.AddServiceDiscovery();
        });

        return builder;
    }

    public static WebApplication MapChatBotUiHealthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        _ = app.MapGet("/health", static () => Results.Ok("Healthy"));
        _ = app.MapGet("/alive", static () => Results.Ok("Alive"));
        return app;
    }
}
