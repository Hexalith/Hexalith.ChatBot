using System.Collections.Concurrent;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Notifications;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;

internal sealed class PeriodicEnforcementBackgroundService(
    PeriodicEnforcementCoordinator coordinator,
    IOptions<PeriodicEnforcementOptions> options,
    ISystemClock clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.UsePeriodicEnforcementRuntime)
        {
            return;
        }

        using PeriodicTimer timer = new(options.Value.Cadence);
        do
        {
            string correlationId = string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"periodic-enforcement:{clock.UtcNow.UtcDateTime:yyyyMMddHHmmss}");

            // The health check must run even when the pass throws — it is the detector for exactly that failure.
            // Letting the throw escape ExecuteAsync also stopped the whole host (the default
            // BackgroundServiceExceptionBehavior is StopHost), so an unwrapped phase such as tenant enumeration could
            // take the server down instead of being reported. RunOnceAsync already recorded the failure before
            // rethrowing, and the stale LastSucceededAtUtc it leaves behind is what CheckHealthAsync alerts on.
            try
            {
                _ = await coordinator.RunOnceAsync(correlationId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Recorded by RunOnceAsync; surfaced below via the missed-cadence/stalled alerts.
            }

            try
            {
                await coordinator.CheckHealthAsync(correlationId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // A failing monitor must not stop the scheduler it monitors.
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
