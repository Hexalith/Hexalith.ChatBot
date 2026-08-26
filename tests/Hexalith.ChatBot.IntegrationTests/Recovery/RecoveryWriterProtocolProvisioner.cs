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

    /// <summary>How long a Conflict is given to resolve into an observably ready store.</summary>
    private static readonly TimeSpan ConflictResolutionBudget = TimeSpan.FromMinutes(1);

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
        string adminToken = await RecoveryAccessTokenProvider
            .AcquireGlobalAdministratorAsync(application, cancellationToken)
            .ConfigureAwait(false);

        await ActivateAsync(client, adminToken, cutoverCommit, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Activates the writer protocol through an already-bound EventStore client.</summary>
    /// <remarks>
    /// This seam keeps the protocol state machine independently testable without composing a second Aspire
    /// topology. The production entry point above remains responsible for resolving the real endpoint and
    /// acquiring the global-administrator bearer.
    /// </remarks>
    /// <param name="client">A client bound to EventStore's HTTP endpoint.</param>
    /// <param name="adminToken">The global-administrator bearer.</param>
    /// <param name="cutoverCommit">The provenance recorded in the marker.</param>
    /// <param name="cancellationToken">Cancels provisioning.</param>
    /// <param name="activationBudget">An optional test-only activation budget.</param>
    /// <param name="conflictResolutionBudget">An optional test-only conflict probe budget.</param>
    /// <param name="pollInterval">An optional test-only retry interval.</param>
    /// <returns>A task that completes once the marker is active.</returns>
    internal static async Task ActivateAsync(
        HttpClient client,
        string adminToken,
        string cutoverCommit,
        CancellationToken cancellationToken,
        TimeSpan? activationBudget = null,
        TimeSpan? conflictResolutionBudget = null,
        TimeSpan? pollInterval = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(cutoverCommit);
        TimeSpan effectiveActivationBudget = activationBudget ?? ActivationBudget;
        TimeSpan effectiveConflictResolutionBudget = conflictResolutionBudget ?? ConflictResolutionBudget;
        TimeSpan effectivePollInterval = pollInterval ?? PollInterval;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(effectiveActivationBudget, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(effectiveConflictResolutionBudget, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(effectivePollInterval, TimeSpan.Zero);

        // Attestations are literal for a disposable sandbox store: nothing predates this topology, so there are no
        // old writers or retry workers to quiesce and no backup to reference beyond the run itself.
        string body = $$"""
            {"cutoverCommit":"{{cutoverCommit}}","backupReference":"recovery-sandbox-ephemeral-store","writersQuiesced":true,"retryWorkersQuiesced":true,"downgradeProhibitedAcknowledged":true}
            """;
        string lastOutcome = "no attempt completed";
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.Elapsed < effectiveActivationBudget)
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
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    // Conflict collapses two very different states. ProjectionDeliveryCutover returns it when a
                    // marker exists AND (it is current with a different commit) OR (it is not current at all).
                    // Treating both as success was wrong in each direction: a NON-current marker can never satisfy
                    // ProjectionDeliveryWriterProtocolHealthCheck, so the run reported "activated" and then hung to
                    // the startup deadline with a misleading listener-gap diagnostic; and a current-but-FOREIGN
                    // marker means this run silently inherited another application's cutover -- the shared-Redis
                    // contamination this provisioning exists to eliminate -- with nothing recording that.
                    //
                    // The admin API exposes no marker read-back, so the health check is used as the IsCurrent
                    // oracle: it reports Healthy exactly when a current marker exists. That distinguishes the two
                    // cases with what the topology actually offers, and it attributes the failure to the marker
                    // rather than to the listener.
                    if (await IsWriterProtocolCurrentAsync(
                        client,
                        effectiveConflictResolutionBudget,
                        effectivePollInterval,
                        cancellationToken).ConfigureAwait(false))
                    {
                        // Metadata only: never a payload. Recorded so a reviewer can tell an inherited marker from
                        // one this run activated -- the provenance the A1 claim depends on.
                        await Console.Error
                            .WriteLineAsync(
                                "RECOVERY_WRITER_PROTOCOL_INHERITED reason=conflict-marker-already-current "
                                + $"attemptedCutoverCommit={cutoverCommit}")
                            .ConfigureAwait(false);
                        return;
                    }

                    throw new InvalidOperationException(
                        "The projection delivery writer protocol reported Conflict and the store did not become "
                        + "ready, so an existing marker is present but NOT current. It can never satisfy the "
                        + "readiness contract and the drill must not proceed; clear the stale marker before rerunning.");
                }

                string detail = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                lastOutcome = $"status {(int)response.StatusCode}: {(detail.Length > 256 ? detail[..256] : detail)}";

                // A refusal that is not a startup condition is fatal: activating is a precondition, not best effort.
                // 503 is retried only while it can still be a warming store: the controller also returns it
                // PERMANENTLY when IProjectionDeliveryCutover is unregistered, and retrying that for the full budget
                // is the same defect this change set criticises in IsTransientMailboxAdmissionStatus -- burning the
                // whole budget on a permanent misconfiguration and discarding the status that would have named it.
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable
                    && detail.Contains("capability is not available", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The projection delivery cutover capability is not registered on this EventStore host, so the "
                        + $"writer protocol can never be activated ({lastOutcome}).");
                }

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

            await Task.Delay(effectivePollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"The projection delivery writer protocol was not activated within {effectiveActivationBudget.TotalSeconds:N0}s; "
            + $"last attempt: {lastOutcome}.");
    }

    /// <summary>Whether the store reports a current writer-protocol marker, using health as the oracle.</summary>
    /// <remarks>
    /// <c>ProjectionDeliveryWriterProtocolHealthCheck</c> is Healthy exactly when <c>marker?.IsCurrent == true</c>,
    /// so a bounded health probe answers the one question the admin API does not expose.
    /// </remarks>
    /// <param name="client">A client bound to the EventStore resource.</param>
    /// <param name="cancellationToken">Cancels the probe.</param>
    /// <returns><see langword="true"/> when a current marker exists.</returns>
    private static async Task<bool> IsWriterProtocolCurrentAsync(
        HttpClient client,
        TimeSpan conflictResolutionBudget,
        TimeSpan pollInterval,
        CancellationToken cancellationToken)
    {
        Stopwatch probe = Stopwatch.StartNew();
        while (probe.Elapsed < conflictResolutionBudget)
        {
            try
            {
                using HttpResponseMessage health = await client
                    .GetAsync("/health", cancellationToken)
                    .ConfigureAwait(false);
                if (health.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException
                && !cancellationToken.IsCancellationRequested)
            {
                // Still starting: not evidence either way.
            }

            await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}
