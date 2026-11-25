using System.Diagnostics;

using Hexalith.ChatBot.UI.Hosting;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using OpenTelemetry;
using OpenTelemetry.Trace;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Verifies the UI host lifecycle while an exporter cannot drain.</summary>
public sealed class HostTelemetryShutdownTests
{
    [Fact]
    public async Task UiHostShouldStopAndDisposeWhileAnExporterIsBlocked()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using BlockingUiTelemetryExporter exporter = new();
        using ActivitySource source = new(nameof(HostTelemetryShutdownTests));
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://127.0.0.1:1";
        _ = builder.AddChatBotUiHostDefaults();
        _ = builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing
            .AddSource(source.Name)
            .AddProcessor(new BatchActivityExportProcessor(exporter, scheduledDelayMilliseconds: 10)));
        IHost host = builder.Build();
        try
        {
            await host.StartAsync(token);
            using (source.StartActivity("blocked-export").ShouldNotBeNull())
            {
            }
            await exporter.Entered.WaitAsync(TimeSpan.FromSeconds(5), token);

            Task shutdown = Task.Run(async () =>
            {
                await host.StopAsync(token).ConfigureAwait(false);
                await ((IAsyncDisposable)host).DisposeAsync().ConfigureAwait(false);
            }, token);
            try
            {
                await shutdown.WaitAsync(TimeSpan.FromSeconds(8), token);
            }
            finally
            {
                exporter.Release();
                await shutdown.ConfigureAwait(true);
            }
        }
        finally
        {
            exporter.Release();
            await ((IAsyncDisposable)host).DisposeAsync().ConfigureAwait(true);
        }
    }
}
