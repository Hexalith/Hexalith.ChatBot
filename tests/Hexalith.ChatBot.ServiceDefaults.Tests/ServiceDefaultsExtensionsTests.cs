using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
