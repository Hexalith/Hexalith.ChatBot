using System.Globalization;

using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.UI.Hosting;
using Hexalith.ChatBot.UI.Localization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using OpenTelemetry.Metrics;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Verifies that the actual UI host metrics configuration exports safe-message fallbacks.</summary>
public sealed class SafeOutcomeHostMetricTests
{
    /// <summary>Checks the host subscription through a metric reader and exported input-free measurement.</summary>
    [Fact]
    public async Task UiHostShouldExportInputFreeFallbackMeasurement()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddChatBotUiHostDefaults();
        builder.Services.AddLocalization();
        builder.Services.AddSingleton<ChatBotUiTextLocalizer>();
        SafeOutcomeMetricExporter exporter = new();
        using BaseExportingMetricReader reader = new(exporter);
        builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddReader(reader));
        using IHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        reader.Collect().ShouldBeTrue();
        long before = exporter.Count;
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            host.Services.GetRequiredService<ChatBotUiTextLocalizer>()
                .FailureCatalogEntry("restricted-host-secret").Code.ShouldBe(ChatBotMessageCodes.GenericDenial);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
        reader.Collect().ShouldBeTrue();
        exporter.Count.ShouldBeGreaterThan(before);
        exporter.HasTags.ShouldBeFalse();
        await host.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
    }
}
