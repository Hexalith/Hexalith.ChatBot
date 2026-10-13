using Hexalith.ChatBot.Contracts.Messages;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry.Metrics;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Verifies that the actual server host exports catalog fallbacks.</summary>
public sealed class ServerSafeOutcomeHostMetricTests
{
    /// <summary>Checks that the host meter subscription exports input-free fallback measurements.</summary>
    [Fact]
    public void ServerHostShouldExportInputFreeFallbackMeasurement()
    {
        ServerSafeOutcomeMetricExporter exporter = new();
        using BaseExportingMetricReader reader = new(exporter);
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddReader(reader))));
        using HttpClient client = factory.CreateClient();
        reader.Collect().ShouldBeTrue();
        long before = exporter.Count;

        ChatBotMessageCatalog.Resolve("restricted-server-host-secret").Code.ShouldBe(ChatBotMessageCodes.GenericDenial);

        reader.Collect().ShouldBeTrue();
        exporter.Count.ShouldBeGreaterThan(before);
        exporter.HasTags.ShouldBeFalse();
    }
}
