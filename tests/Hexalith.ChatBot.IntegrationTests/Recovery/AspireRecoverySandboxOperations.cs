using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.RecoverySandbox;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Projections;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Live Aspire/DAPR implementation of the continuity operations. EventStore lifecycle changes go only through
/// <see cref="ResourceCommandService"/>; subscription changes go only through the authenticated closed sandbox API.
/// </summary>
internal sealed class AspireRecoverySandboxOperations : IRecoverySandboxOperations
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// How long to wait for a stopped EventStore to actually stop serving. Must comfortably exceed the lane's
    /// configured graceful-shutdown window (DOTNET_SHUTDOWNTIMEOUTSECONDS).
    /// </summary>
    private static readonly TimeSpan StopConfirmationTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// How long a resource that already reports healthy gets to start accepting traffic. Deliberately shorter than the
    /// caller's restoration budget so the two failure modes stay distinguishable.
    /// </summary>
    private static readonly TimeSpan ListenerReadinessBudget = TimeSpan.FromSeconds(90);

    /// <summary>
    /// How long an absence assertion polls before concluding a resource genuinely is not there. Deliberately equal to
    /// the presence budget: a shorter absence window silently biases every no-duplicate / no-unauthorized-mutation
    /// check toward passing on an eventually-consistent read path.
    /// </summary>
    /// <remarks>
    /// <see cref="ReadEventStoreEndStateAsync"/> stamps its returned <c>RecoveredAtUtc</c> only after the concurrent
    /// cross-tenant/fault-probe checks that use this window complete, so a full <see cref="AbsenceConfirmationWindow"/>
    /// of harness self-verification is included in the measured RTO on the happy path — the published
    /// <c>MeasurableRecoveryCeilingSeconds</c> bounds harness verification time as well as product recovery time, not
    /// product recovery time alone. Deliberate: the sustained-isolation proof must complete before recovery is
    /// declared, and re-timing the stamp to first-presence would let the isolation proof land after the timestamp it
    /// is meant to validate.
    /// </remarks>
    private static readonly TimeSpan AbsenceConfirmationWindow = TimeSpan.FromMinutes(1);
    private readonly DistributedApplication _application;
    private readonly IResource _eventStoreResource;
    private readonly HttpClient _chatBotClient;
    private readonly HttpClient _eventStoreClient;
    private readonly HttpClient _sandboxClient;
    private readonly ReadModelProjectConversationProjectionStore _readModels;
    private readonly IReadModelConditionalEraser _readModelEraser;
    private readonly EventStoreDurableStateProbe _durableState;
    private readonly string _mailboxClientSecret;
    private readonly string _controllerSecret;
    /// <summary>
    /// The number of independently committed governed notes seeded per continuity checkpoint. A single-record
    /// checkpoint can only ever report 0 or 1 reconstructed, which is a trivial boolean wearing a count; three
    /// independently timestamped records make a genuine partial-reconstruction outcome (e.g. 2 of 3) observable.
    /// </summary>
    private const int CommittedCheckpointRecordCount = 3;
    private readonly List<string> _checkpointNoteRefs = [];
    private readonly List<DateTimeOffset> _checkpointCommittedAtUtc = [];
    private string? _checkpointCorrelationId;
    private string? _faultProbeNoteRef;
    private string? _controlTenantNoteRef;
    private ProjectConversationSourceEmailView? _affectedTenantSentinel;
    private ProjectConversationSourceEmailView? _controlTenantSentinel;
    private string? _subscriptionCheckpointIntakeRef;
    private string? _reconciledIntakeRef;
    private string? _duplicateProbeIntakeRef;
    private bool _subscriptionFaultLeftStateUnchanged;

    public AspireRecoverySandboxOperations(
        DistributedApplication application,
        IResource eventStoreResource,
        string controllerSecret,
        string mailboxClientSecret,
        IReadModelStore readModelStore,
        IReadModelConditionalEraser readModelEraser,
        EventStoreDurableStateProbe durableState)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(eventStoreResource);
        ArgumentException.ThrowIfNullOrWhiteSpace(controllerSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(mailboxClientSecret);
        ArgumentNullException.ThrowIfNull(readModelStore);
        ArgumentNullException.ThrowIfNull(readModelEraser);
        ArgumentNullException.ThrowIfNull(durableState);

        _application = application;
        _eventStoreResource = eventStoreResource;
        _controllerSecret = controllerSecret;
        _mailboxClientSecret = mailboxClientSecret;
        _readModels = new ReadModelProjectConversationProjectionStore(readModelStore);
        _readModelEraser = readModelEraser;
        _durableState = durableState;
        _chatBotClient = application.CreateHttpClient("chatbot");
        _chatBotClient.Timeout = TimeSpan.FromSeconds(30);
        _eventStoreClient = application.CreateHttpClient("eventstore", "http");
        _eventStoreClient.Timeout = TimeSpan.FromSeconds(15);
        _sandboxClient = application.CreateHttpClient("recovery-sandbox", "http");
        _sandboxClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public async ValueTask<RecoveryOperationCheckpoint> SeedCommittedOperationAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        if (!string.Equals(tenantRef, RecoveryValidationTopology.LogicalTenantRef, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Continuity seeding requires logical tenant '{RecoveryValidationTopology.LogicalTenantRef}', not '{tenantRef}'.");
        }

        string userAccessToken = await RecoveryAccessTokenProvider
            .AcquireAsync(_application, cancellationToken).ConfigureAwait(false);
        string controlAccessToken = await RecoveryAccessTokenProvider
            .AcquireControlAsync(_application, cancellationToken).ConfigureAwait(false);
        string operationRef = ChatBotTaskId.New().Value;
        _checkpointNoteRefs.Clear();
        _checkpointCommittedAtUtc.Clear();
        _checkpointCorrelationId = correlationId;
        for (int index = 0; index < CommittedCheckpointRecordCount; index++)
        {
            string noteRef = GovernedNoteId.New().Value;
            string commandRef = ChatBotCommandId.New().Value;
            _checkpointNoteRefs.Add(noteRef);
            await SubmitUntilAcceptedAsync(noteRef, commandRef, operationRef, correlationId, userAccessToken, cancellationToken)
                .ConfigureAwait(false);
            await WaitForGovernedOperationAsync(noteRef, correlationId, expectPresent: true, userAccessToken, cancellationToken)
                .ConfigureAwait(false);
            await _durableState.WaitForGovernedNoteAsync(
                RecoveryValidationTopology.StorageTenantRef,
                noteRef,
                cancellationToken).ConfigureAwait(false);
            _checkpointCommittedAtUtc.Add(UtcNow);
        }

        // Seed a resource owned by an INDEPENDENT control tenant. Probing a randomly generated id with the caller's
        // own token can only ever 404, so it proves nothing; a real foreign-tenant resource that must stay
        // unreadable after recovery is what makes the isolation assertion falsifiable.
        string controlNoteRef = GovernedNoteId.New().Value;
        _controlTenantNoteRef = controlNoteRef;
        await SubmitUntilAcceptedAsync(
                controlNoteRef,
                ChatBotCommandId.New().Value,
                ChatBotTaskId.New().Value,
                correlationId,
                controlAccessToken,
                cancellationToken)
            .ConfigureAwait(false);
        await WaitForGovernedOperationAsync(
                controlNoteRef,
                correlationId,
                expectPresent: true,
                controlAccessToken,
                cancellationToken)
            .ConfigureAwait(false);
        await _durableState.WaitForGovernedNoteAsync(
            RecoveryValidationTopology.ControlTenantRef,
            controlNoteRef,
            cancellationToken).ConfigureAwait(false);
        return new RecoveryOperationCheckpoint(_checkpointNoteRefs.Count, _checkpointCommittedAtUtc.Max(), operationRef);
    }

    /// <inheritdoc />
    public async ValueTask StopEventStoreAsync(CancellationToken cancellationToken)
    {
        ExecuteCommandResult result = await ExecuteResourceCommandResultAsync(
            KnownResourceCommands.StopCommand,
            cancellationToken).ConfigureAwait(false);
        bool commandSucceeded = result.Success && !result.Canceled;

        // Confirm the resource is actually unreachable before returning, whatever the command reported. A successful
        // StopCommand only means the request was accepted: the container can still serve requests inside its graceful
        // shutdown window, and the fault probe that runs next would then see a healthy EventStore and declare the
        // outage never reached the command boundary — a flaky `unmeasurable` for a correctly injected fault.
        bool endpointAvailable = true;
        using (CancellationTokenSource stopDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            stopDeadline.CancelAfter(StopConfirmationTimeout);
            try
            {
                while (true)
                {
                    endpointAvailable = await IsEventStoreEndpointAvailableAsync(stopDeadline.Token).ConfigureAwait(false);
                    if (!endpointAvailable)
                    {
                        break;
                    }

                    await Task.Delay(PollInterval, stopDeadline.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The confirmation window elapsed with the endpoint still serving; fall through to the guard below.
            }
        }

        // Check the specific "command succeeded but the endpoint never went away" case before the generic boundary
        // check below: StopReachedDependencyBoundary(true, true) is false, so the generic check would otherwise throw
        // a misleading ResourceCommandFailure for a command that actually succeeded.
        if (commandSucceeded && endpointAvailable)
        {
            throw new InvalidOperationException(
                $"EventStore was still reachable {StopConfirmationTimeout.TotalSeconds:N0}s after an accepted stop command.");
        }

        if (!StopReachedDependencyBoundary(commandSucceeded, endpointAvailable))
        {
            throw ResourceCommandFailure(KnownResourceCommands.StopCommand, result);
        }
    }

    /// <inheritdoc />
    public async ValueTask<RecoveryFaultObservation> ObserveEventStoreFaultAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        string userAccessToken = await RecoveryAccessTokenProvider
            .AcquireAsync(_application, cancellationToken).ConfigureAwait(false);
        string noteRef = GovernedNoteId.New().Value;
        _faultProbeNoteRef = noteRef;
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            HttpStatusCode statusCode = await SubmitOnceAsync(
                noteRef,
                ChatBotCommandId.New().Value,
                ChatBotTaskId.New().Value,
                correlationId,
                userAccessToken,
                deadline.Token).ConfigureAwait(false);
            if (statusCode == HttpStatusCode.Accepted)
            {
                throw new InvalidOperationException("The EventStore outage did not reach the ChatBot command boundary.");
            }

            // Only a dependency-boundary failure counts. Previously every status outside {202, 401, 403, 404} fell
            // through and was recorded as a witnessed outage, so a contract-drift 400, a 409, or a rate-limit 429
            // became the RTO start timestamp.
            if ((int)statusCode < 500 && statusCode != HttpStatusCode.RequestTimeout)
            {
                throw new InvalidOperationException(
                    $"The EventStore probe failed outside the intended dependency boundary (status {(int)statusCode}).");
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // A bounded DAPR invocation timeout is an observed EventStore dependency failure, not a fabricated pass.
        }
        catch (HttpRequestException)
        {
            // The real ChatBot/DAPR command path surfaced the dependency fault as transport unavailability.
        }

        if (await IsEventStoreEndpointAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                "The ChatBot fault probe failed, but EventStore itself was reachable; the requested dependency boundary was not proven.");
        }

        return new RecoveryFaultObservation(UtcNow, "eventstore-command-path-unavailable");
    }

    /// <inheritdoc />
    public async ValueTask StartEventStoreAsync(CancellationToken cancellationToken)
        => await ExecuteResourceCommandAsync(KnownResourceCommands.StartCommand, cancellationToken).ConfigureAwait(false);

    internal static bool StopReachedDependencyBoundary(bool commandSucceeded, bool endpointAvailable)
        => commandSucceeded && !endpointAvailable;

    /// <inheritdoc />
    public async ValueTask<DateTimeOffset> WaitForEventStoreRecoveryAsync(CancellationToken cancellationToken)
    {
        await _application.ResourceNotifications
            .WaitForResourceHealthyAsync(_eventStoreResource.Name, cancellationToken)
            .ConfigureAwait(false);
        // Own sub-budget, strictly shorter than the caller's restoration budget. Previously this loop allowed the same
        // 3 minutes the caller's CTS already covered — and the caller's clock started earlier, across the start command
        // and the health wait — so this TimeoutException was unreachable and every slow recovery surfaced as an opaque
        // OperationCanceledException. A caller cancellation now means "the resource never came back"; this timeout
        // means "the resource is healthy but its listener never accepted traffic", which are different failures.
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.Elapsed < ListenerReadinessBudget)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using HttpResponseMessage response = await _eventStoreClient.GetAsync("/health", cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return UtcNow;
                }
            }
            catch (HttpRequestException)
            {
                // The process is Running but the listener is not ready yet.
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // HttpClient.Timeout (15s) mid-poll is "listener not ready", not caller cancellation.
                _ = ex;
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"EventStore reported healthy but its listener did not accept traffic within {ListenerReadinessBudget.TotalSeconds:N0}s.");
    }

    /// <inheritdoc />
    public async ValueTask<RecoveryEventStoreEndState> ReadEventStoreEndStateAsync(
        RecoveryOperationCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        if (_checkpointNoteRefs.Count == 0)
        {
            throw new InvalidOperationException("No committed recovery checkpoint exists.");
        }

        if (string.IsNullOrWhiteSpace(_controlTenantNoteRef))
        {
            throw new InvalidOperationException("No independent control-tenant resource was seeded for the isolation probe.");
        }

        string userAccessToken = await RecoveryAccessTokenProvider
            .AcquireAsync(_application, cancellationToken).ConfigureAwait(false);
        string controlAccessToken = await RecoveryAccessTokenProvider
            .AcquireControlAsync(_application, cancellationToken).ConfigureAwait(false);
        int reconstructedCount = 0;
        foreach (string noteRef in _checkpointNoteRefs)
        {
            bool projectionPresent = false;
            try
            {
                if (await WaitForGovernedOperationAsync(
                    noteRef,
                    checkpoint.OperationRef,
                    expectPresent: true,
                    userAccessToken,
                    cancellationToken).ConfigureAwait(false))
                {
                    projectionPresent = true;
                }
            }
            catch (TimeoutException)
            {
                // Genuinely not reconstructed within the presence budget — a real partial-loss signal, not every
                // committed record needs to survive for the loop to keep checking the rest.
            }

            bool durablePresent = await _durableState.IsGovernedNoteCommittedAsync(
                RecoveryValidationTopology.StorageTenantRef,
                noteRef,
                cancellationToken).ConfigureAwait(false);
            if (projectionPresent && durablePresent)
            {
                reconstructedCount++;
            }
        }

        bool unauthorizedMutationAbsent = true;
        if (!string.IsNullOrWhiteSpace(_faultProbeNoteRef))
        {
            Task<bool> durableFaultAbsent = _durableState.RemainsGovernedNoteAbsentAsync(
                RecoveryValidationTopology.StorageTenantRef,
                _faultProbeNoteRef,
                AbsenceConfirmationWindow,
                cancellationToken);
            bool projectionFaultAbsent = !await WaitForGovernedOperationAsync(
                _faultProbeNoteRef,
                checkpoint.OperationRef,
                expectPresent: false,
                userAccessToken,
                cancellationToken).ConfigureAwait(false);
            unauthorizedMutationAbsent = projectionFaultAbsent && await durableFaultAbsent.ConfigureAwait(false);
        }

        List<Task<bool>> crossTenantAbsenceChecks = _checkpointNoteRefs
            .Select(noteRef => _durableState.RemainsGovernedNoteAbsentAsync(
                RecoveryValidationTopology.ControlTenantRef,
                noteRef,
                AbsenceConfirmationWindow,
                cancellationToken))
            .ToList();
        crossTenantAbsenceChecks.Add(_durableState.RemainsGovernedNoteAbsentAsync(
            RecoveryValidationTopology.StorageTenantRef,
            _controlTenantNoteRef,
            AbsenceConfirmationWindow,
            cancellationToken));
        bool[] crossTenantAbsence = await Task.WhenAll(crossTenantAbsenceChecks).ConfigureAwait(false);
        bool controlDurable = await _durableState.IsGovernedNoteCommittedAsync(
            RecoveryValidationTopology.ControlTenantRef,
            _controlTenantNoteRef,
            cancellationToken).ConfigureAwait(false);

        // Read a REAL resource owned by the independent control tenant using the recovery tenant's own bearer. The
        // control tenant must still be able to read it, and the recovery tenant must not.
        using HttpResponseMessage foreignRead = await GetAuthorizedAsync(
            $"/api/v1/governed-operations/{_controlTenantNoteRef}",
            checkpoint.OperationRef,
            userAccessToken,
            cancellationToken).ConfigureAwait(false);
        using HttpResponseMessage controlRead = await GetAuthorizedAsync(
            $"/api/v1/governed-operations/{_controlTenantNoteRef}",
            checkpoint.OperationRef,
            controlAccessToken,
            cancellationToken).ConfigureAwait(false);

        // Mirror WaitForGovernedOperationAsync's discipline: isolation must be OBSERVED, never inferred from "not the
        // expected status". A 401 from an expired bearer or a 5xx from a broken read path is not evidence either way
        // and must not be silently folded into "isolation failed" alongside a genuine cross-tenant read.
        if (foreignRead.StatusCode is not (HttpStatusCode.Forbidden or HttpStatusCode.NotFound))
        {
            throw new InvalidOperationException(
                $"The foreign-tenant isolation probe returned an unexpected status {(int)foreignRead.StatusCode}.");
        }

        if (controlRead.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"The control-tenant isolation probe returned an unexpected status {(int)controlRead.StatusCode}.");
        }

        bool tenantIsolation = controlDurable && crossTenantAbsence.All(static absent => absent);
        return new RecoveryEventStoreEndState(UtcNow, reconstructedCount, tenantIsolation, unauthorizedMutationAbsent);
    }

    /// <inheritdoc />
    public async ValueTask<bool> CleanupEventStoreScenarioAsync(CancellationToken cancellationToken)
    {
        if (!await IsEventStoreEndpointAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("EventStore was not available during continuity cleanup verification.");
        }

        bool hasOwnedState = _checkpointNoteRefs.Count > 0 ||
            !string.IsNullOrWhiteSpace(_controlTenantNoteRef) ||
            !string.IsNullOrWhiteSpace(_faultProbeNoteRef);
        if (!hasOwnedState)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(_checkpointCorrelationId))
        {
            throw new InvalidOperationException("Continuity cleanup lost the correlation identity for its owned state.");
        }

        string userAccessToken = await RecoveryAccessTokenProvider
            .AcquireAsync(_application, cancellationToken).ConfigureAwait(false);
        string controlAccessToken = await RecoveryAccessTokenProvider
            .AcquireControlAsync(_application, cancellationToken).ConfigureAwait(false);
        bool complete = true;
        foreach (string noteRef in _checkpointNoteRefs)
        {
            try
            {
                if (!await WaitForGovernedOperationAsync(
                    noteRef,
                    _checkpointCorrelationId,
                    expectPresent: true,
                    userAccessToken,
                    cancellationToken).ConfigureAwait(false))
                {
                    complete = false;
                }
            }
            catch (TimeoutException)
            {
                complete = false;
            }
        }

        if (!string.IsNullOrWhiteSpace(_controlTenantNoteRef))
        {
            try
            {
                if (!await WaitForGovernedOperationAsync(
                    _controlTenantNoteRef,
                    _checkpointCorrelationId,
                    expectPresent: true,
                    controlAccessToken,
                    cancellationToken).ConfigureAwait(false))
                {
                    complete = false;
                }
            }
            catch (TimeoutException)
            {
                complete = false;
            }
        }

        if (!string.IsNullOrWhiteSpace(_faultProbeNoteRef))
        {
            try
            {
                bool stillPresent = await WaitForGovernedOperationAsync(
                    _faultProbeNoteRef,
                    _checkpointCorrelationId,
                    expectPresent: false,
                    userAccessToken,
                    cancellationToken).ConfigureAwait(false);
                complete &= !stillPresent;
            }
            catch (InvalidOperationException)
            {
                complete = false;
            }
        }

        // Erase projected residue so shared recovery/control tenants do not accumulate checkpoint notes across runs.
        // EventStore aggregates remain append-only; cleanup owns the ChatBot governed-operation read models.
        foreach (string noteRef in _checkpointNoteRefs)
        {
            await EraseReadModelAsync(
                GovernedOperationView.KeyFor(RecoveryValidationTopology.StorageTenantRef, noteRef),
                cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(_controlTenantNoteRef))
        {
            await EraseReadModelAsync(
                GovernedOperationView.KeyFor(RecoveryValidationTopology.ControlTenantRef, _controlTenantNoteRef),
                cancellationToken).ConfigureAwait(false);
        }
        if (!string.IsNullOrWhiteSpace(_faultProbeNoteRef))
        {
            await EraseReadModelAsync(
                GovernedOperationView.KeyFor(RecoveryValidationTopology.StorageTenantRef, _faultProbeNoteRef),
                cancellationToken).ConfigureAwait(false);
        }

        try
        {
            List<Task<bool>> absenceChecks = _checkpointNoteRefs
                .Select(noteRef => WaitForGovernedOperationAsync(
                    noteRef,
                    _checkpointCorrelationId,
                    expectPresent: false,
                    userAccessToken,
                    cancellationToken))
                .ToList();
            if (!string.IsNullOrWhiteSpace(_controlTenantNoteRef))
            {
                absenceChecks.Add(WaitForGovernedOperationAsync(
                    _controlTenantNoteRef,
                    _checkpointCorrelationId,
                    expectPresent: false,
                    controlAccessToken,
                    cancellationToken));
            }
            bool[] stillPresent = await Task.WhenAll(absenceChecks).ConfigureAwait(false);
            complete &= stillPresent.All(static present => !present);
        }
        catch (InvalidOperationException)
        {
            complete = false;
        }

        if (complete)
        {
            _faultProbeNoteRef = null;
            _controlTenantNoteRef = null;
            _checkpointNoteRefs.Clear();
            _checkpointCommittedAtUtc.Clear();
            _checkpointCorrelationId = null;
        }

        return complete;
    }

    /// <inheritdoc />
    public async ValueTask<RecoveryOperationCheckpoint> CheckpointSubscriptionCommittedBoundAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        // Witness a healthy delivery before the outage so loss-path RPO is bounded by committed-before-outage state,
        // not harness wall-clock start (Story 12.15 chunk-1a decision 1.2).
        using JsonDocument process = await SendSandboxControlAsync(
            tenantRef,
            "process",
            includeBearer: true,
            cancellationToken,
            notificationPhase: RecoveryNotificationIdentity.CheckpointPhase).ConfigureAwait(false);
        JsonElement root = process.RootElement;
        if (!root.GetProperty("submitted").GetBoolean())
        {
            string kind = root.GetProperty("kind").GetString() ?? "unknown";
            string reasonCode = root.GetProperty("reasonCode").GetString() ?? "unknown";
            throw new InvalidOperationException(
                $"Subscription committed-before-outage checkpoint failed (kind={kind}, reason={reasonCode}).");
        }

        string intakeId = root.TryGetProperty("intakeId", out JsonElement intake) && intake.GetString() is { Length: > 0 } id
            ? id
            : throw new InvalidOperationException(
                "Subscription committed-before-outage checkpoint did not return an intake identity.");
        if (!root.TryGetProperty("observedAtUtc", out JsonElement observed))
        {
            throw new InvalidOperationException(
                "Subscription committed-before-outage checkpoint did not return observedAtUtc.");
        }

        await _durableState.WaitForMailboxIntakeAsync(
            RecoveryValidationTopology.StorageTenantRef,
            intakeId,
            cancellationToken).ConfigureAwait(false);
        _subscriptionCheckpointIntakeRef = intakeId;
        DateTimeOffset committedAtUtc = observed.GetDateTimeOffset().ToUniversalTime();
        return new RecoveryOperationCheckpoint(1, committedAtUtc, intakeId);
    }

    /// <inheritdoc />
    public async ValueTask ExpireSubscriptionAsync(string tenantRef, CancellationToken cancellationToken)
    {
        using JsonDocument cleanBoundary = await SendSandboxControlAsync(
            tenantRef,
            "restore",
            includeBearer: false,
            cancellationToken).ConfigureAwait(false);
        if (RecoverySandboxRestoreResponse.WasPreviouslyFaulted(cleanBoundary.RootElement) ||
            RecoverySandboxRestoreResponse.IsCurrentlyFaulted(cleanBoundary.RootElement))
        {
            throw new InvalidOperationException("The subscription boundary was not clean before fault injection.");
        }

        _affectedTenantSentinel = SubscriptionSentinel(
            RecoveryValidationTopology.StorageTenantRef,
            "recovery-subscription-affected-sentinel");
        _controlTenantSentinel = SubscriptionSentinel(
            RecoveryValidationTopology.ControlTenantRef,
            "recovery-subscription-control-sentinel");
        await _readModels.UpsertSourceEmailAsync(_affectedTenantSentinel, cancellationToken).ConfigureAwait(false);
        await _readModels.UpsertSourceEmailAsync(_controlTenantSentinel, cancellationToken).ConfigureAwait(false);
        using JsonDocument faulted = await SendSandboxControlAsync(
            tenantRef,
            "fault",
            includeBearer: false,
            cancellationToken).ConfigureAwait(false);

        // Verify the injection actually took, as the symmetric restore/status checks do. Binding the response and
        // never reading it meant a fault that silently failed to apply was measured as a successful injection.
        if (!faulted.RootElement.GetProperty("faulted").GetBoolean())
        {
            throw new InvalidOperationException("The subscription simulator did not report itself faulted after injection.");
        }
    }

    /// <inheritdoc />
    public async ValueTask<RecoveryFaultObservation> ObserveSubscriptionFaultAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        using JsonDocument response = await SendSandboxControlAsync(
            tenantRef,
            "process",
            includeBearer: true,
            cancellationToken,
            notificationPhase: RecoveryNotificationIdentity.RecoveryPhase).ConfigureAwait(false);
        JsonElement root = response.RootElement;
        if (root.GetProperty("submitted").GetBoolean() ||
            !string.Equals(root.GetProperty("reasonCode").GetString(), "graph_subscription_expired", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The real Worker path did not observe the expired subscription boundary.");
        }

        ProjectConversationSourceEmailView? affectedAfterFault = await _readModels
            .GetSourceEmailAsync(
                _affectedTenantSentinel!.TenantId,
                _affectedTenantSentinel.IntakeId,
                cancellationToken)
            .ConfigureAwait(false);
        ProjectConversationSourceEmailView? controlAfterFault = await _readModels
            .GetSourceEmailAsync(
                _controlTenantSentinel!.TenantId,
                _controlTenantSentinel.IntakeId,
                cancellationToken)
            .ConfigureAwait(false);
        _subscriptionFaultLeftStateUnchanged = Equals(affectedAfterFault, _affectedTenantSentinel) &&
            Equals(controlAfterFault, _controlTenantSentinel);

        return new RecoveryFaultObservation(
            root.GetProperty("observedAtUtc").GetDateTimeOffset().ToUniversalTime(),
            "graph-subscription-expired");
    }

    /// <inheritdoc />
    public async ValueTask RestoreSubscriptionAsync(string tenantRef, CancellationToken cancellationToken)
    {
        using JsonDocument restored = await SendSandboxControlAsync(
            tenantRef,
            "restore",
            includeBearer: false,
            cancellationToken).ConfigureAwait(false);
        if (RecoverySandboxRestoreResponse.IsCurrentlyFaulted(restored.RootElement))
        {
            throw new InvalidOperationException("The subscription simulator remained faulted after restore.");
        }
    }

    /// <inheritdoc />
    public async ValueTask<RecoverySubscriptionEndState> ReconcileSubscriptionAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        using JsonDocument process = await SendSandboxControlAsync(
            tenantRef,
            "process",
            includeBearer: true,
            cancellationToken,
            notificationPhase: RecoveryNotificationIdentity.RecoveryPhase).ConfigureAwait(false);
        if (!process.RootElement.GetProperty("submitted").GetBoolean())
        {
            string kind = process.RootElement.GetProperty("kind").GetString() ?? "unknown";
            string reasonCode = process.RootElement.GetProperty("reasonCode").GetString() ?? "unknown";
            throw new InvalidOperationException(
                $"The restored Worker path did not reconcile the seeded notification (kind={kind}, reason={reasonCode}).");
        }

        _reconciledIntakeRef = process.RootElement.GetProperty("intakeId").GetString();
        if (string.IsNullOrWhiteSpace(_reconciledIntakeRef))
        {
            throw new InvalidOperationException("The restored Worker did not return its materialized intake identity.");
        }

        await _durableState.WaitForMailboxIntakeAsync(
            RecoveryValidationTopology.StorageTenantRef,
            _reconciledIntakeRef,
            cancellationToken).ConfigureAwait(false);

        // Exercise the same provider notification again. The gateway's coarse idempotency key is based on
        // tenant + mailbox + provider message, while the Worker generates a fresh intake id. Therefore a second
        // source-email projection under that fresh id would be direct evidence of a duplicate side effect.
        using JsonDocument duplicateProbe = await SendSandboxControlAsync(
            tenantRef,
            "process",
            includeBearer: true,
            cancellationToken,
            notificationPhase: RecoveryNotificationIdentity.RecoveryPhase).ConfigureAwait(false);
        _duplicateProbeIntakeRef = duplicateProbe.RootElement.GetProperty("intakeId").GetString();
        if (!duplicateProbe.RootElement.GetProperty("submitted").GetBoolean() ||
            string.IsNullOrWhiteSpace(_duplicateProbeIntakeRef))
        {
            throw new InvalidOperationException("The duplicate-notification probe did not reach the real Worker submission path.");
        }

        Task<bool> duplicateAggregateAbsent = _durableState.RemainsAbsentAsync(
            RecoveryValidationTopology.StorageTenantRef,
            _duplicateProbeIntakeRef,
            AbsenceConfirmationWindow,
            cancellationToken);
        Task<bool> controlTenantAggregateAbsent = _durableState.RemainsAbsentAsync(
            RecoveryValidationTopology.ControlTenantRef,
            _reconciledIntakeRef,
            AbsenceConfirmationWindow,
            cancellationToken);
        Task<bool> controlTenantReadModelsAbsent = RemainsIntakeReadModelsAbsentAsync(
            RecoveryValidationTopology.ControlTenantRef,
            _reconciledIntakeRef,
            AbsenceConfirmationWindow,
            cancellationToken);
        bool[] isolationOutcomes = await Task.WhenAll(
            duplicateAggregateAbsent,
            controlTenantAggregateAbsent,
            controlTenantReadModelsAbsent).ConfigureAwait(false);
        bool recoveredAggregateStillCommitted = await _durableState.IsMailboxIntakeCommittedAsync(
            RecoveryValidationTopology.StorageTenantRef,
            _reconciledIntakeRef,
            cancellationToken).ConfigureAwait(false);
        ProjectConversationSourceEmailView? affectedSentinel = await _readModels
            .GetSourceEmailAsync(
                _affectedTenantSentinel!.TenantId,
                _affectedTenantSentinel.IntakeId,
                cancellationToken)
            .ConfigureAwait(false);
        ProjectConversationSourceEmailView? controlSentinel = await _readModels
            .GetSourceEmailAsync(
                _controlTenantSentinel!.TenantId,
                _controlTenantSentinel.IntakeId,
                cancellationToken)
            .ConfigureAwait(false);

        // Isolation combines sentinel preservation with exact generated-identity absence in the control tenant. A new
        // cross-tenant row no longer passes merely because the two fixed sentinel rows stayed unchanged.
        bool isolated = Equals(affectedSentinel, _affectedTenantSentinel) &&
            Equals(controlSentinel, _controlTenantSentinel) &&
            isolationOutcomes[1] &&
            isolationOutcomes[2];
        // Duplication is a distinct dimension from silent loss: a recovery that lost data (recoveredAggregateStillCommitted
        // false) is a NoSilentLoss breach, not evidence of duplication. Mirrors the Graph scoped-outage sibling's
        // same-id-or-still-present check, which does not condition on recovery success either.
        bool noDuplicateProjection = !string.Equals(_duplicateProbeIntakeRef, _reconciledIntakeRef, StringComparison.Ordinal) &&
            isolationOutcomes[0];
        return new RecoverySubscriptionEndState(
            UtcNow,
            DeliveredCount: recoveredAggregateStillCommitted ? 1 : 0,
            NoSilentLoss: recoveredAggregateStillCommitted,
            NoDuplicateSideEffects: noDuplicateProjection,
            TenantIsolationPreserved: isolated,
            UnauthorizedMutationAbsent: _subscriptionFaultLeftStateUnchanged);
    }

    /// <inheritdoc />
    public async ValueTask<bool> CleanupSubscriptionScenarioAsync(string tenantRef, CancellationToken cancellationToken)
    {
        (string Tenant, string? Intake)[] cleanupTargets =
        [
            (_affectedTenantSentinel?.TenantId ?? string.Empty, _affectedTenantSentinel?.IntakeId),
            (_controlTenantSentinel?.TenantId ?? string.Empty, _controlTenantSentinel?.IntakeId),
            (RecoveryValidationTopology.StorageTenantRef, _subscriptionCheckpointIntakeRef),
            (RecoveryValidationTopology.StorageTenantRef, _reconciledIntakeRef),
            (RecoveryValidationTopology.StorageTenantRef, _duplicateProbeIntakeRef),
        ];
        // Erase FIRST, then restore in finally. Running restore ahead of erase stranded rows when restore failed;
        // running erase without finally left the simulator faulted when erase threw.
        try
        {
            foreach ((string cleanupTenant, string? cleanupIntake) in cleanupTargets)
            {
                if (!string.IsNullOrWhiteSpace(cleanupTenant) && !string.IsNullOrWhiteSpace(cleanupIntake))
                {
                    await EraseIntakeReadModelsAsync(cleanupTenant, cleanupIntake, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            using JsonDocument restored = await SendSandboxControlAsync(
                tenantRef,
                "restore",
                includeBearer: false,
                cancellationToken).ConfigureAwait(false);
            if (RecoverySandboxRestoreResponse.IsCurrentlyFaulted(restored.RootElement))
            {
                throw new InvalidOperationException("The subscription simulator remained faulted after cleanup.");
            }
        }

        bool complete = true;
        foreach ((string cleanupTenant, string? cleanupIntake) in cleanupTargets)
        {
            if (string.IsNullOrWhiteSpace(cleanupTenant) || string.IsNullOrWhiteSpace(cleanupIntake))
            {
                continue;
            }

            complete &= await AreIntakeReadModelsAbsentAsync(
                cleanupTenant,
                cleanupIntake,
                cancellationToken).ConfigureAwait(false);
        }

        // The reconciled aggregate is append-only and must remain committed; only the fresh duplicate-probe identity
        // is expected to remain absent in durable state.
        if (!string.IsNullOrWhiteSpace(_duplicateProbeIntakeRef))
        {
            complete &= await _durableState.RemainsAbsentAsync(
                RecoveryValidationTopology.StorageTenantRef,
                _duplicateProbeIntakeRef,
                AbsenceConfirmationWindow,
                cancellationToken).ConfigureAwait(false);
        }

        if (complete)
        {
            _affectedTenantSentinel = null;
            _controlTenantSentinel = null;
            _subscriptionCheckpointIntakeRef = null;
            _reconciledIntakeRef = null;
            _duplicateProbeIntakeRef = null;
        }

        return complete;
    }

    /// <summary>
    /// Erases every read model an intake materializes, not just its source-email view. The controlled Graph source
    /// always attaches one attachment, so the association projection also writes an attachment-set view; erasing only
    /// the source-email key left those rows accumulating under the validation tenant on every run.
    /// </summary>
    private async Task EraseIntakeReadModelsAsync(string tenantId, string intakeId, CancellationToken cancellationToken)
    {
        await EraseReadModelAsync(
            ProjectConversationSourceEmailView.KeyFor(tenantId, intakeId),
            cancellationToken).ConfigureAwait(false);
        await EraseReadModelAsync(
            ProjectConversationAttachmentSetView.KeyFor(tenantId, intakeId),
            cancellationToken).ConfigureAwait(false);
        await EraseReadModelAsync(
            AttachmentIndexKeyFor(tenantId, intakeId),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> AreIntakeReadModelsAbsentAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken)
    {
        foreach (string key in IntakeReadModelKeys(tenantId, intakeId))
        {
            (bool present, _) = await _readModelEraser
                .TryReadEtagAsync(ChatBotReadModelStoreNames.StateStoreName, key, cancellationToken)
                .ConfigureAwait(false);
            if (present)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> RemainsIntakeReadModelsAbsentAsync(
        string tenantId,
        string intakeId,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        do
        {
            if (!await AreIntakeReadModelsAbsentAsync(tenantId, intakeId, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
        while (timer.Elapsed < window);

        return await AreIntakeReadModelsAbsentAsync(tenantId, intakeId, cancellationToken).ConfigureAwait(false);
    }

    private static string[] IntakeReadModelKeys(string tenantId, string intakeId)
        =>
        [
            ProjectConversationSourceEmailView.KeyFor(tenantId, intakeId),
            ProjectConversationAttachmentSetView.KeyFor(tenantId, intakeId),
            AttachmentIndexKeyFor(tenantId, intakeId),
        ];

    private static string AttachmentIndexKeyFor(string tenantId, string intakeId)
        => $"{tenantId}:project-conversation:{intakeId}:attachments";

    private async Task EraseReadModelAsync(string key, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            (bool present, string etag) = await _readModelEraser
                .TryReadEtagAsync(ChatBotReadModelStoreNames.StateStoreName, key, cancellationToken)
                .ConfigureAwait(false);
            if (!present || await _readModelEraser
                .TryEraseAsync(ChatBotReadModelStoreNames.StateStoreName, key, etag, cancellationToken)
                .ConfigureAwait(false))
            {
                return;
            }
        }

        throw new InvalidOperationException($"Recovery cleanup could not erase read-model key '{key}'.");
    }

    private static ProjectConversationSourceEmailView SubscriptionSentinel(string tenantRef, string intakeRef)
        => new(
            tenantRef,
            intakeRef,
            "recovery-sentinel-mailbox",
            "recovery-sentinel-message",
            InternetMessageId: null,
            "recovery-sentinel-conversation",
            SourceThreadId: null,
            DateTimeOffset.Parse("2026-08-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            SourceSentAtUtc: null,
            SourceCreatedAtUtc: null,
            "UTC",
            "Microsoft 365 mailbox",
            "m365-mailbox",
            "metadata-only",
            "standard",
            ProjectConversationSourceEmailView.CurrentSchemaVersion,
            SourceVersion: 1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private async ValueTask ExecuteResourceCommandAsync(string command, CancellationToken cancellationToken)
    {
        ExecuteCommandResult result = await ExecuteResourceCommandResultAsync(command, cancellationToken).ConfigureAwait(false);
        if (!result.Success || result.Canceled)
        {
            throw ResourceCommandFailure(command, result);
        }
    }

    private async ValueTask<ExecuteCommandResult> ExecuteResourceCommandResultAsync(
        string command,
        CancellationToken cancellationToken)
    {
        ResourceCommandService commands = _application.Services.GetRequiredService<ResourceCommandService>();
        return await commands
            .ExecuteCommandAsync(_eventStoreResource, command, cancellationToken)
            .ConfigureAwait(false);
    }

    private async ValueTask<bool> IsEventStoreEndpointAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _eventStoreClient
                .GetAsync("/health", cancellationToken)
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private static InvalidOperationException ResourceCommandFailure(string command, ExecuteCommandResult result)
    {
        string detail = result.Message ?? "no command detail";
        return new InvalidOperationException(
            $"The allowlisted Aspire EventStore resource command '{command}' did not complete successfully: {detail}.");
    }

    private async Task SubmitUntilAcceptedAsync(
        string noteRef,
        string commandRef,
        string operationRef,
        string correlationId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromMinutes(3))
        {
            HttpStatusCode statusCode;
            try
            {
                statusCode = await SubmitOnceAsync(noteRef, commandRef, operationRef, correlationId, accessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                statusCode = HttpStatusCode.ServiceUnavailable;
            }

            if (statusCode == HttpStatusCode.Accepted)
            {
                return;
            }

            if ((int)statusCode < 500 && statusCode is not HttpStatusCode.RequestTimeout and not HttpStatusCode.TooManyRequests)
            {
                throw new InvalidOperationException("The governed checkpoint command failed outside a transient dependency state.");
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("The governed checkpoint command was not accepted before the scenario deadline.");
    }

    private async Task<HttpStatusCode> SubmitOnceAsync(
        string noteRef,
        string commandRef,
        string operationRef,
        string correlationId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        string body = $$"""
            {"commandId":"{{commandRef}}","commandType":"RecordGovernedNote","command":{"noteId":"{{noteRef}}"},"origin":"ui","requestSchemaVersion":"v1"}
            """;
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Correlation-Id", correlationId);
        request.Headers.Add("X-Hexalith-Task-Id", operationRef);
        using HttpResponseMessage response = await _chatBotClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.StatusCode;
    }

    private async Task<bool> WaitForGovernedOperationAsync(
        string noteRef,
        string correlationId,
        bool expectPresent,
        string accessToken,
        CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();

        // Absence gets the same budget as presence. A 5s absence window against a 1min presence window on the same
        // eventually-consistent read path meant a mutation that DID land during the outage, but projected slowly, was
        // recorded as "absent" — i.e. unauthorizedMutationAbsent = true. The asymmetry biased toward passing.
        TimeSpan deadline = expectPresent ? TimeSpan.FromMinutes(1) : AbsenceConfirmationWindow;
        while (timer.Elapsed < deadline)
        {
            using HttpResponseMessage response = await GetAuthorizedAsync(
                $"/api/v1/governed-operations/{noteRef}",
                correlationId,
                accessToken,
                cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            // Absence must be OBSERVED, never inferred from "not 200". A 401 from an expired bearer or a 5xx from a
            // broken read path is not evidence that an unauthorized mutation is absent — and for presence probes those
            // same statuses are not "not yet projected"; they are verification failures.
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Keep polling for the full window. A late materialization invalidates absence.
            }
            else
            {
                throw new InvalidOperationException(
                    $"The governed-operation {(expectPresent ? "presence" : "absence")} probe returned an unexpected status {(int)response.StatusCode}.");
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        // Close the final-delay race: a projection can materialize after the last loop read but before the delay
        // crosses the deadline. The boundary observation, rather than the preceding sleep, decides the result.
        using HttpResponseMessage finalResponse = await GetAuthorizedAsync(
            $"/api/v1/governed-operations/{noteRef}",
            correlationId,
            accessToken,
            cancellationToken).ConfigureAwait(false);
        if (finalResponse.StatusCode == HttpStatusCode.OK)
        {
            return true;
        }

        if (finalResponse.StatusCode != HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                $"The governed-operation {(expectPresent ? "presence" : "absence")} probe returned an unexpected status {(int)finalResponse.StatusCode}.");
        }

        if (expectPresent)
        {
            throw new TimeoutException("The governed checkpoint projection did not materialize before the scenario deadline.");
        }

        return false;
    }

    private async Task<HttpResponseMessage> GetAuthorizedAsync(
        string path,
        string correlationId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Correlation-Id", correlationId);
        return await _chatBotClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<JsonDocument> SendSandboxControlAsync(
        string tenantRef,
        string action,
        bool includeBearer,
        CancellationToken cancellationToken,
        HttpMethod? method = null,
        string? notificationPhase = null)
    {
        using HttpRequestMessage request = SandboxRequest(tenantRef, action, includeBearer, method ?? HttpMethod.Post);
        if (includeBearer)
        {
            string mailboxAccessToken = await RecoveryAccessTokenProvider
                .AcquireMailboxAsync(_application, _mailboxClientSecret, cancellationToken)
                .ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mailboxAccessToken);
        }

        if (string.Equals(action, "process", StringComparison.Ordinal))
        {
            request.Headers.Add(
                RecoveryNotificationIdentity.HeaderName,
                notificationPhase ?? throw new InvalidOperationException(
                    "A recovery notification phase is required for every process action."));
        }

        using HttpResponseMessage response = await _sandboxClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("The closed recovery-sandbox action did not complete successfully.");
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonDocument.Parse(body);
    }

    private HttpRequestMessage SandboxRequest(string tenantRef, string action, bool includeBearer, HttpMethod method)
    {
        string tenant = Uri.EscapeDataString(tenantRef);
        HttpRequestMessage request = new(method, $"/recovery/{tenant}/m365-subscription-failure/{action}");
        request.Headers.Add("X-Recovery-Controller-Secret", _controllerSecret);
        request.Headers.Add("X-Recovery-Scenario-Lane", "continuity");

        return request;
    }
}
