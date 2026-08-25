using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Activates EventStore's store-global projection delivery writer protocol before the drill runs.
/// </summary>
/// <remarks>
/// <para>
/// <c>ProjectionDeliveryWriterProtocolHealthCheck</c> reports the server <c>Unhealthy</c> until a store-global
/// marker with <c>IsCurrent</c> exists, and post-cutover a projection checkpoint can only advance through the
/// named fenced completion. A fresh deployment — which is what a hosted runner starts with, against an empty
/// Redis — therefore has to activate it, exactly as any real EventStore deployment must.
/// </para>
/// <para>
/// Local runs were previously green only because an unrelated application's harness had left a marker in the
/// shared development Redis. That is not a property a hosted lane can rely on, and relying on it silently is how
/// three green runs said nothing about a clean environment.
/// </para>
/// <para>
/// This drives the same operator-facing admin endpoint a deployment would, with the realm's global-administrator
/// bearer, and fails closed: if activation does not report success the drill must not proceed and claim a
/// measurement from an unready store.
/// </para>
/// </remarks>
internal static class RecoveryWriterProtocolProvisioner
{
    private const string ActivateRoute = "api/v1/admin/projections/delivery-writer-protocol/activate";
    private static readonly TimeSpan ActivationBudget = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>Activates the writer protocol, or throws if the store cannot be made ready.</summary>
    /// <param name="application">The composed topology.</param>
    /// <param name="cutoverCommit">The provenance recorded in the marker.</param>
    /// <param name="cancellationToken">Cancels provisioning.</param>
    /// <returns>A task that completes once the marker is active.</returns>
    public static async Task ActivateAsync(
        DistributedApplication application,
        string cutoverCommit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentException.ThrowIfNullOrWhiteSpace(cutoverCommit);

        // Runs before the readiness waits, because readiness DEPENDS on the marker: EventStore reports Unhealthy
        // until it exists, so waiting for health first would deadlock on a fresh store. That means the listener may
        // not be accepting yet, and a refused connection here is "not up yet", not a failure to activate.
        using HttpClient client = application.CreateHttpClient("eventstore", "http");
        client.Timeout = TimeSpan.FromSeconds(30);
        string adminToken = await AcquireAdminTokenWhenAvailableAsync(application, cancellationToken)
            .ConfigureAwait(false);

        // Attestations are literal for a disposable sandbox store: nothing predates this topology, so there are no
        // old writers or retry workers to quiesce and no backup to reference beyond the run itself.
        string body = $$"""
            {"cutoverCommit":"{{cutoverCommit}}","backupReference":"recovery-sandbox-ephemeral-store","writersQuiesced":true,"retryWorkersQuiesced":true,"downgradeProhibitedAcknowledged":true}
            """;
        string lastOutcome = "no attempt completed";
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.Elapsed < ActivationBudget)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using HttpRequestMessage attempt = new(HttpMethod.Post, ActivateRoute)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                };
                attempt.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
                using HttpResponseMessage response = await client.SendAsync(attempt, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict)
                {
                    // Conflict means a marker for a different cutover is already current, which still satisfies the
                    // readiness contract this provisioning exists to satisfy.
                    return;
                }

                string detail = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                lastOutcome = $"status {(int)response.StatusCode}: {(detail.Length > 256 ? detail[..256] : detail)}";

                // A refusal that is not a startup condition is fatal: activating is a precondition, not best effort.
                if (response.StatusCode is not (HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway
                    or HttpStatusCode.GatewayTimeout or HttpStatusCode.NotFound))
                {
                    throw new InvalidOperationException(
                        $"The projection delivery writer protocol could not be activated ({lastOutcome}).");
                }
            }
            catch (HttpRequestException exception)
            {
                lastOutcome = $"transport-{exception.HttpRequestError}";
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastOutcome = "client-timeout";
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"The projection delivery writer protocol was not activated within {ActivationBudget.TotalSeconds:N0}s; "
            + $"last attempt: {lastOutcome}.");
    }

    private static async Task<string> AcquireAdminTokenWhenAvailableAsync(
        DistributedApplication application,
        CancellationToken cancellationToken)
        => await RecoveryAccessTokenProvider
            .AcquireGlobalAdministratorAsync(application, cancellationToken)
            .ConfigureAwait(false);
}
