using System.Diagnostics.Metrics;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenTelemetry;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Shouldly;

namespace Hexalith.ChatBot.ServiceDefaults.Tests;

public static class ServiceDefaultsExtensionsTests
{
    [Fact]
    public static void AddServiceDefaultsShouldReturnSameBuilderAndRegisterServiceDiscovery()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        IHostApplicationBuilder result = builder.AddServiceDefaults();

        result.ShouldBeSameAs(builder);
        builder.Services.Any(static service =>
            service.ServiceType.FullName is not null
            && service.ServiceType.FullName.Contains("ServiceDiscovery", StringComparison.Ordinal))
            .ShouldBeTrue();
        builder.Services.Any(static service =>
            service.ServiceType.FullName is not null
            && service.ServiceType.FullName.Contains("OpenTelemetry", StringComparison.Ordinal))
            .ShouldBeTrue();
        builder.Services.Any(static service => service.ServiceType == typeof(ILoggerProvider))
            .ShouldBeTrue();

        // The tracer and meter providers (not just options/builder types) must be registered so tracing and
        // metrics are actually wired, alongside the logging provider above.
        builder.Services.Any(static service => service.ServiceType == typeof(TracerProvider))
            .ShouldBeTrue();
        builder.Services.Any(static service => service.ServiceType == typeof(MeterProvider))
            .ShouldBeTrue();
    }

    [Fact]
    public static void OpenTelemetryShouldNotCaptureRequestOrResponseBodies()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        _ = builder.AddServiceDefaults();
        using WebApplication app = builder.Build();

        AspNetCoreTraceInstrumentationOptions options = app.Services
            .GetRequiredService<IOptions<AspNetCoreTraceInstrumentationOptions>>()
            .Value;

        // Metadata-only invariant: ASP.NET Core instrumentation must not be configured to read request or
        // response payloads. Body capture would only be possible through enrichment callbacks, which must stay
        // unset so no command payload can leak into spans.
        options.EnrichWithHttpRequest.ShouldBeNull();
        options.EnrichWithHttpResponse.ShouldBeNull();
    }

    [Fact]
    public static void ChatBotMeterIsWiredIntoTheMetricsPipelineSoItsInstrumentsAreCollected()
    {
        // Story 8.2 (AC9): the dedicated ChatBot meter must be registered on the always-on metrics pipeline via
        // AddMeter, under the exact name the Server metrics seam creates its Meter with. An in-memory reader proves
        // an instrument published on that meter is ACTUALLY collected — so removing `.AddMeter(ChatBotMeterName)`
        // (or drifting the meter name) fails this test rather than passing silently.
        Extensions.ChatBotMeterName.ShouldBe("Hexalith.ChatBot");

        List<string> collectedMeterNames = [];
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        _ = builder.AddServiceDefaults();
        _ = builder.Services.ConfigureOpenTelemetryMeterProvider(metrics =>
            metrics.AddReader(new BaseExportingMetricReader(new CollectingMetricExporter(collectedMeterNames))));
        using WebApplication app = builder.Build();

        MeterProvider meterProvider = app.Services.GetRequiredService<MeterProvider>();

        using Meter meter = new(Extensions.ChatBotMeterName);
        Counter<long> probe = meter.CreateCounter<long>("chatbot.metrics.wiring.probe");
        probe.Add(1);

        _ = meterProvider.ForceFlush();

        collectedMeterNames.ShouldContain(Extensions.ChatBotMeterName);
    }

    [Fact]
    public static void ChatBotMetricsPipelineBuildsWithTheOtlpExporter()
    {
        // Story 8.2: building with an OTLP endpoint set exercises the AddMeter + AddOtlpExporter path so a
        // construction-time wiring regression on the exported metrics pipeline surfaces here.
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317";
        _ = builder.AddServiceDefaults();
        using WebApplication app = builder.Build();

        app.Services.GetRequiredService<MeterProvider>().ShouldNotBeNull();
    }

    [Fact]
    public static void MapDefaultEndpointsShouldRegisterHealthAndAliveRoutes()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();

        _ = app.MapDefaultEndpoints();

        string[] routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(static endpoint => endpoint.RoutePattern.RawText ?? string.Empty)
            .ToArray();

        routes.ShouldContain("/health");
        routes.ShouldContain("/alive");
    }

    // Captures the meter name of every collected metric so a test can prove a specific meter is subscribed on the
    // MeterProvider (i.e. AddMeter was called for it), without needing a network exporter.
    private sealed class CollectingMetricExporter(List<string> meterNames) : BaseExporter<Metric>
    {
        public override ExportResult Export(in Batch<Metric> batch)
        {
            foreach (Metric metric in batch)
            {
                meterNames.Add(metric.MeterName);
            }

            return ExportResult.Success;
        }
    }
}
