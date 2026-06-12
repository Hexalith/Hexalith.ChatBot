using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Hexalith.ChatBot.ServiceDefaults;

public static class Extensions
{
    /// <summary>
    /// Name of the single dedicated ChatBot OpenTelemetry meter (Story 8.2). It is registered on the
    /// always-on metrics pipeline via <c>AddMeter</c> so operational instruments export through the same MeterProvider/OTLP path the M2
    /// dashboards read. The <c>Hexalith.ChatBot.Server</c> metrics seam creates its <see cref="System.Diagnostics.Metrics.Meter"/>
    /// with this exact name; both must stay in lockstep.
    /// </summary>
    public const string ChatBotMeterName = "Hexalith.ChatBot";

    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.ConfigureOpenTelemetry();
        _ = builder.Services.AddServiceDiscovery();
        _ = builder.Services.ConfigureHttpClientDefaults(static http =>
        {
            _ = http.AddStandardResilienceHandler();
            _ = http.AddServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        bool useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        _ = builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            if (useOtlpExporter)
            {
                _ = logging.AddOtlpExporter();
            }
        });

        _ = builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                _ = metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    // Story 8.2: register the always-on ChatBot operational meter so its FR94 instruments
                    // (ingestion/association/approval/command-execution latency, retry exhaustion, duplicate
                    // suppression, audit-projection lag, emission-failure gap counter) export through this
                    // MeterProvider — not behind the trim-able dashboard read stage.
                    .AddMeter(ChatBotMeterName);
                if (useOtlpExporter)
                {
                    _ = metrics.AddOtlpExporter();
                }
            })
            .WithTracing(tracing =>
            {
                _ = tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
                if (useOtlpExporter)
                {
                    _ = tracing.AddOtlpExporter();
                }
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
