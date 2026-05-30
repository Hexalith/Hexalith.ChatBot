using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        Extensions.ChatBotActivitySourceName.ShouldBe("Hexalith.ChatBot");
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
}
