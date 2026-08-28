using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.RecoverySandbox;
using Hexalith.ChatBot.Server.Audit;
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

    /// <summary>How long a governed-operation projection is given to materialise before presence is a timeout.</summary>
    private static readonly TimeSpan PresenceConfirmationWindow = TimeSpan.FromMinutes(1);
    private readonly DistributedApplication _application;
    private readonly IResource _eventStoreResource;
    private readonly HttpClient _chatBotClient;
    private readonly HttpClient _eventStoreClient;
    private readonly HttpClient _sandboxClient;
    private readonly ReadModelProjectConversationProjectionStore _readModels;

    /// <summary>
    /// Reads the governed-operation projection through the production store type, so the absence probe cannot drift
    /// from the store name and key the projection handler actually writes.
    /// </summary>
    private readonly ReadModelGovernedOperationViewStore _governedOperationProjections;
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
    private string? _controlledPreFaultIntakeRef;
    private string? _controlledRejectedCandidateRef;
    private string? _controlledPostRecoveryIntakeRef;
    private bool _controlledFaultLeftStateUnchanged;
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
        _governedOperationProjections = new ReadModelGovernedOperationViewStore(readModelStore);
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
            await WaitForGovernedOperationProjectionAsync(
                    noteRef,
                    RecoveryValidationTopology.StorageTenantRef,
                    cancellationToken)
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
        await WaitForGovernedOperationProjectionAsync(
                controlNoteRef,
                RecoveryValidationTopology.ControlTenantRef,
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

        // "did not accept traffic" named the wrong side of the gap once: the listener was accepting traffic and
        // answering 503, and the message sent an investigation after a connectivity problem that did not exist.
        // The diagnostic knows which side is broken, so the message says it.
        ListenerGap gap = await DescribeListenerGapAsync().ConfigureAwait(false);
        throw new TimeoutException(
            $"EventStore did not become ready within {ListenerReadinessBudget.TotalSeconds:N0}s: {gap.Verdict} {gap.Detail}");
    }

    /// <summary>
    /// Reports which side of the "process is listening but the harness cannot reach it" gap is broken.
    /// </summary>
    /// <remarks>
    /// Runs only after the readiness budget has already expired and the failure is being thrown, and only
    /// observes: it compares the endpoint the retained client holds against the endpoint Aspire resolves now, and
    /// probes the freshly resolved one once. It never returns success, never retries into the budget, and never
    /// makes a run pass — a resolved endpoint that answers here is a diagnosis, not a recovery.
    /// </remarks>
    /// <returns>A metadata-only description of the gap and which side of it is broken.</returns>
    private async Task<ListenerGap> DescribeListenerGapAsync()
    {
        using CancellationTokenSource diagnosticTimeout = new(TimeSpan.FromSeconds(10));
        string retained = _eventStoreClient.BaseAddress?.ToString() ?? "none";
        string resolved;
        try
        {
            resolved = _application.GetEndpoint(_eventStoreResource.Name, "http").ToString();
        }
        catch (Exception exception)
        {
            return new ListenerGap(
                "the harness could not resolve the resource endpoint.",
                $"retainedEndpoint={retained} resolvedEndpoint=<{exception.GetType().Name}>");
        }

        string verdict;
        string freshProbe;
        try
        {
            using HttpClient fresh = _application.CreateHttpClient(_eventStoreResource.Name, "http");
            fresh.Timeout = TimeSpan.FromSeconds(10);
            using HttpResponseMessage response = await fresh
                .GetAsync("/health", diagnosticTimeout.Token)
                .ConfigureAwait(false);
            freshProbe = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);
            verdict = response.IsSuccessStatusCode
                ? "the application answers a fresh client but not the retained one, so the retained endpoint is stale."
                : "the application is accepting traffic and reporting itself UNHEALTHY, so the resource is not ready.";
        }
        catch (Exception exception)
        {
            freshProbe = $"<{exception.GetType().Name}>";
            verdict = "the application is not reachable at all, so the listener is genuinely absent.";
        }

        return new ListenerGap(
            verdict,
            $"retainedEndpoint={retained} resolvedEndpoint={resolved} freshClientHealth={freshProbe}");
    }

    /// <summary>Which side of the "listening versus reachable" gap is broken, plus its metadata-only evidence.</summary>
    /// <param name="Verdict">The side of the gap the observation points at.</param>
    /// <param name="Detail">The endpoints and status observed.</param>
    private sealed record ListenerGap(string Verdict, string Detail);

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
        string? firstReconstructedNoteRef = null;
        foreach (string noteRef in _checkpointNoteRefs)
        {
            bool projectionPresent = false;
            try
            {
                if (await WaitForGovernedOperationProjectionAsync(
                    noteRef,
                    RecoveryValidationTopology.StorageTenantRef,
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

            if (projectionPresent)
            {
                // Remember a note the loop OBSERVED present, so the positive control below asserts something this
                // run actually established rather than assuming note[0] survived.
                firstReconstructedNoteRef ??= noteRef;
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
            // Positive control first: prove the channel can read a view this run OBSERVED the server write.
            // Using _checkpointNoteRefs[0] unconditionally asserted a note the loop above explicitly permits to be
            // missing ("a real partial-loss signal"), so a drill that genuinely lost the first note reported a
            // broken observation channel instead of the data loss it had just measured.
            if (firstReconstructedNoteRef is not null)
            {
                await AssertProjectionChannelReadsServerWritesAsync(
                    RecoveryValidationTopology.StorageTenantRef,
                    firstReconstructedNoteRef,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Nothing was reconstructed at all. There is no known-present write to control against, so the
                // absence result below cannot be trusted and must not be reported as evidence.
                throw new InvalidOperationException(
                    "No checkpoint note was reconstructed, so no positive control exists for the unauthorized-mutation "
                    + "absence probe and its result cannot be trusted.");
            }
            bool projectionFaultAbsent = await RemainsGovernedOperationProjectionAbsentAsync(
                RecoveryValidationTopology.StorageTenantRef,
                _faultProbeNoteRef,
                cancellationToken).ConfigureAwait(false);
            // Always await the durable half, even when the projection half already decided the outcome: short
            // circuiting left a started task unobserved on the NFR59 path, so a fault in it surfaced as an
            // unobserved task exception instead of a verdict input.
            bool durableAbsent = await durableFaultAbsent.ConfigureAwait(false);
            unauthorizedMutationAbsent = projectionFaultAbsent && durableAbsent;
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

        // Mirror WaitForGovernedOperationProjectionAsync's discipline: isolation must be OBSERVED, never inferred from "not the
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

        // No bearer is acquired here any more: every check below reads the projection channel directly. The two
        // acquisitions that used to sit here were dead once cleanup moved off the read API, and each carried a
        // 3-minute retry budget that could throw and abort cleanup before its RECOVERY_CLEANUP_INCOMPLETE
        // diagnostic could be emitted -- losing the diagnosis in exactly the case it exists for.
        bool complete = true;

        // Metadata-only diagnostic: `cleanup-complete: false` reaches the evidence bundle as a single boolean, so a
        // failing hosted run cannot say which of the five sub-checks did not hold. Only stable check names are
        // recorded — never an identifier, a tenant or a payload — and they go to stderr, not to any report.
        List<string> incompleteChecks = [];
        // Read presence through the projection channel, not the read API. A governed operation that is missing
        // answers 403 safe-not-found, which threw past the TimeoutException handlers below and took the whole
        // cleanup — and its RECOVERY_CLEANUP_INCOMPLETE diagnostic — out with it, so three of the five sub-checks
        // could never be reported and the diagnostic could not fire in the case it exists for.
        foreach (string noteRef in _checkpointNoteRefs)
        {
            try
            {
                // Bounded poll: a single read reported ordinary post-outage projection lag as a missing note and a
                // spuriously incomplete cleanup.
                if (!await IsGovernedOperationProjectionPresentAsync(
                    RecoveryValidationTopology.StorageTenantRef,
                    noteRef,
                    cancellationToken).ConfigureAwait(false))
                {
                    complete = false;
                    incompleteChecks.Add("checkpoint-note-not-present");
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                complete = false;
                incompleteChecks.Add("checkpoint-note-presence-faulted");
            }
        }

        if (!string.IsNullOrWhiteSpace(_controlTenantNoteRef))
        {
            try
            {
                if (!await IsGovernedOperationProjectionPresentAsync(
                    RecoveryValidationTopology.ControlTenantRef,
                    _controlTenantNoteRef,
                    cancellationToken).ConfigureAwait(false))
                {
                    complete = false;
                    incompleteChecks.Add("control-note-not-present");
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                complete = false;
                incompleteChecks.Add("control-note-presence-faulted");
            }
        }

        if (!string.IsNullOrWhiteSpace(_faultProbeNoteRef))
        {
            try
            {
                // Same channel change as the NFR59 assertion above, for the same reason: the read API answers
                // 403 for a governed operation that does not exist, so verifying cleanup through it would report
                // every successfully cleaned run as incomplete.
                if (!await RemainsGovernedOperationProjectionAbsentAsync(
                    RecoveryValidationTopology.StorageTenantRef,
                    _faultProbeNoteRef,
                    cancellationToken).ConfigureAwait(false))
                {
                    complete = false;
                    incompleteChecks.Add("fault-probe-projection-present");
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                complete = false;
                incompleteChecks.Add("fault-probe-absence-faulted");
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
                .Select(noteRef => RemainsGovernedOperationProjectionAbsentAsync(
                    RecoveryValidationTopology.StorageTenantRef,
                    noteRef,
                    cancellationToken))
                .ToList();
            if (!string.IsNullOrWhiteSpace(_controlTenantNoteRef))
            {
                absenceChecks.Add(RemainsGovernedOperationProjectionAbsentAsync(
                    RecoveryValidationTopology.ControlTenantRef,
                    _controlTenantNoteRef,
                    cancellationToken));
            }
            bool[] absent = await Task.WhenAll(absenceChecks).ConfigureAwait(false);
            if (!absent.All(static isAbsent => isAbsent))
            {
                complete = false;
                incompleteChecks.Add("erased-projection-still-present");
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Symmetric with the fault-probe branch above: both now read Dapr directly and can throw Dapr or
            // HTTP exceptions, not only InvalidOperationException.
            complete = false;
            incompleteChecks.Add("post-erase-absence-faulted");
        }

        if (complete)
        {
            _faultProbeNoteRef = null;
            _controlTenantNoteRef = null;
            _checkpointNoteRefs.Clear();
            _checkpointCommittedAtUtc.Clear();
            _checkpointCorrelationId = null;
        }

        if (!complete)
        {
            await Console.Error
                .WriteLineAsync($"RECOVERY_CLEANUP_INCOMPLETE checks={string.Join(',', incompleteChecks.Distinct())}")
                .ConfigureAwait(false);
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

    /// <summary>Produces and reads one authoritative durable bound for the controlled-loss lane.</summary>
    internal async ValueTask<DurableCommitObservation> WitnessControlledLossCommitAsync(
        string tenantRef,
        bool preFault,
        CancellationToken cancellationToken)
    {
        string phase = preFault
            ? RecoveryNotificationIdentity.PreFaultPhase
            : RecoveryNotificationIdentity.PostRecoveryPhase;
        using JsonDocument process = await SendSandboxControlAsync(
            tenantRef,
            "process",
            includeBearer: true,
            cancellationToken,
            notificationPhase: phase,
            scenarioLane: RecoveryNotificationIdentity.ControlledLossLane).ConfigureAwait(false);
        JsonElement root = process.RootElement;
        if (!root.GetProperty("submitted").GetBoolean() ||
            root.GetProperty("intakeId").GetString() is not { Length: > 0 } intakeRef ||
            root.GetProperty("candidateRef").GetString() is not { Length: > 0 } candidateRef ||
            !string.Equals(intakeRef, candidateRef, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The controlled-loss retained bound did not reach the real Worker submission path.");
        }

        DurableCommitObservation observation = await _durableState.WaitForMailboxIntakeCommitAsync(
            RecoveryValidationTopology.StorageTenantRef,
            intakeRef,
            cancellationToken).ConfigureAwait(false);
        if (preFault)
        {
            _controlledPreFaultIntakeRef = intakeRef;
        }
        else
        {
            _controlledPostRecoveryIntakeRef = intakeRef;
        }

        return observation;
    }

    /// <summary>Obtains the candidate identity captured before the controlled 503 dependency rejection.</summary>
    internal async ValueTask<ControlledLossCandidateObservation> RejectControlledLossCandidateAsync(
        string tenantRef,
        CancellationToken cancellationToken)
    {
        using JsonDocument response = await SendSandboxControlAsync(
            tenantRef,
            "process",
            includeBearer: true,
            cancellationToken,
            notificationPhase: RecoveryNotificationIdentity.LossPhase,
            scenarioLane: RecoveryNotificationIdentity.ControlledLossLane).ConfigureAwait(false);
        JsonElement root = response.RootElement;
        string? candidateRef = root.GetProperty("candidateRef").GetString();

        // Read the observation instant defensively. When the sandbox never reached the submission boundary the
        // capture is null, and an eager GetDateTimeOffset() replaced this method's own diagnostic with a raw
        // "element is not a string" JSON error in the hosted log.
        DateTimeOffset? observedAtUtc =
            root.TryGetProperty("candidateObservedAtUtc", out JsonElement observed) &&
            observed.ValueKind == JsonValueKind.String &&
            observed.TryGetDateTimeOffset(out DateTimeOffset parsedObservedAtUtc)
                ? parsedObservedAtUtc
                : null;
        bool rejected = !root.GetProperty("submitted").GetBoolean() &&
            string.Equals(root.GetProperty("reasonCode").GetString(), "chatbot_submission_recoverable", StringComparison.Ordinal);
        if (!rejected || !RecoveryValidationEvidenceManifest.IsCanonicalUlid(candidateRef) ||
            observedAtUtc is not { Offset.Ticks: 0 })
        {
            throw new InvalidOperationException("The controlled fault-window candidate was not safely identified before rejection.");
        }

        ProjectConversationSourceEmailView? affectedAfterFault = await _readModels
            .GetSourceEmailAsync(_affectedTenantSentinel!.TenantId, _affectedTenantSentinel.IntakeId, cancellationToken)
            .ConfigureAwait(false);
        ProjectConversationSourceEmailView? controlAfterFault = await _readModels
            .GetSourceEmailAsync(_controlTenantSentinel!.TenantId, _controlTenantSentinel.IntakeId, cancellationToken)
            .ConfigureAwait(false);
        _controlledFaultLeftStateUnchanged = Equals(affectedAfterFault, _affectedTenantSentinel) &&
            Equals(controlAfterFault, _controlTenantSentinel);
        _controlledRejectedCandidateRef = candidateRef!;
        return new ControlledLossCandidateObservation(candidateRef!, observedAtUtc.Value, rejected);
    }

    /// <summary>Reads all retained, absent, and tenant-isolation invariants after controlled-loss restoration.</summary>
    internal async ValueTask<ControlledLossPathSafetyObservation> ReadControlledLossSafetyAsync(
        string tenantRef,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        if (_controlledPreFaultIntakeRef is null || _controlledRejectedCandidateRef is null ||
            _controlledPostRecoveryIntakeRef is null || _affectedTenantSentinel is null ||
            _controlTenantSentinel is null)
        {
            throw new InvalidOperationException("The controlled-loss safety observation is incomplete.");
        }

        Task<bool> candidateAggregateAbsent = _durableState.RemainsAbsentAsync(
            RecoveryValidationTopology.StorageTenantRef,
            _controlledRejectedCandidateRef,
            AbsenceConfirmationWindow,
            cancellationToken);
        Task<bool> candidateReadModelsAbsent = RemainsIntakeReadModelsAbsentAsync(
            RecoveryValidationTopology.StorageTenantRef,
            _controlledRejectedCandidateRef,
            AbsenceConfirmationWindow,
            cancellationToken);
        Task<bool> controlPreAggregateAbsent = _durableState.RemainsAbsentAsync(
            RecoveryValidationTopology.ControlTenantRef,
            _controlledPreFaultIntakeRef,
            AbsenceConfirmationWindow,
            cancellationToken);
        Task<bool> controlPreReadModelsAbsent = RemainsIntakeReadModelsAbsentAsync(
            RecoveryValidationTopology.ControlTenantRef,
            _controlledPreFaultIntakeRef,
            AbsenceConfirmationWindow,
            cancellationToken);
        Task<bool> controlCandidateAggregateAbsent = _durableState.RemainsAbsentAsync(
            RecoveryValidationTopology.ControlTenantRef,
            _controlledRejectedCandidateRef,
            AbsenceConfirmationWindow,
            cancellationToken);
        Task<bool> controlCandidateReadModelsAbsent = RemainsIntakeReadModelsAbsentAsync(
            RecoveryValidationTopology.ControlTenantRef,
            _controlledRejectedCandidateRef,
            AbsenceConfirmationWindow,
            cancellationToken);
        Task<bool> controlPostAggregateAbsent = _durableState.RemainsAbsentAsync(
            RecoveryValidationTopology.ControlTenantRef,
            _controlledPostRecoveryIntakeRef,
            AbsenceConfirmationWindow,
            cancellationToken);
        Task<bool> controlPostReadModelsAbsent = RemainsIntakeReadModelsAbsentAsync(
            RecoveryValidationTopology.ControlTenantRef,
            _controlledPostRecoveryIntakeRef,
            AbsenceConfirmationWindow,
            cancellationToken);
        await Task.WhenAll(
            candidateAggregateAbsent,
            candidateReadModelsAbsent,
            controlPreAggregateAbsent,
            controlPreReadModelsAbsent,
            controlCandidateAggregateAbsent,
            controlCandidateReadModelsAbsent,
            controlPostAggregateAbsent,
            controlPostReadModelsAbsent).ConfigureAwait(false);
        bool preRetained = await _durableState.IsMailboxIntakeCommittedAsync(
            RecoveryValidationTopology.StorageTenantRef,
            _controlledPreFaultIntakeRef,
            cancellationToken).ConfigureAwait(false);
        bool postRetained = await _durableState.IsMailboxIntakeCommittedAsync(
            RecoveryValidationTopology.StorageTenantRef,
            _controlledPostRecoveryIntakeRef,
            cancellationToken).ConfigureAwait(false);
        ProjectConversationSourceEmailView? affectedSentinel = await _readModels.GetSourceEmailAsync(
            _affectedTenantSentinel.TenantId,
            _affectedTenantSentinel.IntakeId,
            cancellationToken).ConfigureAwait(false);
        ProjectConversationSourceEmailView? controlSentinel = await _readModels.GetSourceEmailAsync(
            _controlTenantSentinel.TenantId,
            _controlTenantSentinel.IntakeId,
            cancellationToken).ConfigureAwait(false);
        bool sentinelsUnchanged = Equals(affectedSentinel, _affectedTenantSentinel) &&
            Equals(controlSentinel, _controlTenantSentinel);
        return EvaluateControlledLossSafety(
            preRetained,
            candidateAggregateAbsent.Result,
            candidateReadModelsAbsent.Result,
            postRetained,
            controlPreAggregateAbsent.Result,
            controlPreReadModelsAbsent.Result,
            controlCandidateAggregateAbsent.Result,
            controlCandidateReadModelsAbsent.Result,
            controlPostAggregateAbsent.Result,
            controlPostReadModelsAbsent.Result,
            sentinelsUnchanged,
            _controlledFaultLeftStateUnchanged);
    }

    /// <summary>Combines independently observed controlled-loss durability, isolation, and sentinel facts.</summary>
    internal static ControlledLossPathSafetyObservation EvaluateControlledLossSafety(
        bool preFaultRetained,
        bool candidateAggregateAbsent,
        bool candidateReadModelsAbsent,
        bool postRecoveryRetained,
        bool controlPreAggregateAbsent,
        bool controlPreReadModelsAbsent,
        bool controlCandidateAggregateAbsent,
        bool controlCandidateReadModelsAbsent,
        bool controlPostAggregateAbsent,
        bool controlPostReadModelsAbsent,
        bool sentinelsUnchangedAfterRecovery,
        bool sentinelsUnchangedDuringFault)
        => new(
            preFaultRetained,
            candidateAggregateAbsent && candidateReadModelsAbsent,
            postRecoveryRetained,
            controlPreAggregateAbsent &&
                controlPreReadModelsAbsent &&
                controlCandidateAggregateAbsent &&
                controlCandidateReadModelsAbsent &&
                controlPostAggregateAbsent &&
                controlPostReadModelsAbsent &&
                sentinelsUnchangedAfterRecovery,
            sentinelsUnchangedDuringFault && sentinelsUnchangedAfterRecovery);

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
            (RecoveryValidationTopology.StorageTenantRef, _controlledPreFaultIntakeRef),
            (RecoveryValidationTopology.StorageTenantRef, _controlledRejectedCandidateRef),
            (RecoveryValidationTopology.StorageTenantRef, _controlledPostRecoveryIntakeRef),
            (RecoveryValidationTopology.ControlTenantRef, _controlledPreFaultIntakeRef),
            (RecoveryValidationTopology.ControlTenantRef, _controlledRejectedCandidateRef),
            (RecoveryValidationTopology.ControlTenantRef, _controlledPostRecoveryIntakeRef),
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
        foreach (string? controlledIdentity in new[]
        {
            _controlledPreFaultIntakeRef,
            _controlledRejectedCandidateRef,
            _controlledPostRecoveryIntakeRef,
        })
        {
            if (!string.IsNullOrWhiteSpace(controlledIdentity))
            {
                complete &= await _durableState.RemainsAbsentAsync(
                    RecoveryValidationTopology.ControlTenantRef,
                    controlledIdentity,
                    AbsenceConfirmationWindow,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        if (!string.IsNullOrWhiteSpace(_controlledRejectedCandidateRef))
        {
            complete &= await _durableState.RemainsAbsentAsync(
                RecoveryValidationTopology.StorageTenantRef,
                _controlledRejectedCandidateRef,
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
            _controlledPreFaultIntakeRef = null;
            _controlledRejectedCandidateRef = null;
            _controlledPostRecoveryIntakeRef = null;

            // Reset the isolation observation with the refs it was derived from. Left set, a later controlled-loss
            // run on this instance would publish the previous run's tenant-isolation fact as its own.
            _controlledFaultLeftStateUnchanged = false;
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
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The 30-second _chatBotClient.Timeout elapsing mid-poll is the same transient dependency state as a
                // refused connection, and the two sibling polls above already classify it that way. Treating it as
                // fatal here aborted whole scenarios during seeding — before any fault was injected — whenever the
                // ChatBot's own resilience ladder (10s attempt timeout, retried) outlasted this client's budget, and
                // the fail-safe coordinator then reduced that to an `unmeasurable` report with no retained cause.
                // The 3-minute bound and the non-transient-status throw below are unchanged.
                statusCode = HttpStatusCode.ServiceUnavailable;
            }

            if (statusCode == HttpStatusCode.Accepted)
            {
                return;
            }

            if ((int)statusCode < 500 && statusCode is not HttpStatusCode.RequestTimeout and not HttpStatusCode.TooManyRequests)
            {
                // Naming the refused status is the difference between a diagnosable failure and an opaque one: the
                // fail-safe coordinator reduces whatever is thrown here to `unmeasurable`, so a message that omits
                // the status leaves a hosted run with no way to tell an authorization regression from a bad payload.
                throw new InvalidOperationException(
                    $"The governed checkpoint command failed outside a transient dependency state (status {(int)statusCode}).");
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

    /// <summary>
    /// Proves the projection channel can actually read a view the SERVER wrote, before any absence result from it
    /// is believed.
    /// </summary>
    /// <remarks>
    /// Without this, the absence probe is vacuous by construction: point it at the wrong tenant partition or the
    /// wrong component and every call returns "absent", so "no unauthorized mutation landed" and "erasure
    /// succeeded" both pass while observing nothing at all. A known-present read through the same store, same
    /// component and same key shape is what makes a subsequent null a fact rather than an artefact.
    /// </remarks>
    /// <param name="tenantRef">The tenant partition the absence probe will read.</param>
    /// <param name="presentNoteRef">A governed note the server is known to have projected.</param>
    /// <param name="cancellationToken">Cancels the control read.</param>
    /// <returns>A task that completes when the channel is proven readable.</returns>
    private async Task AssertProjectionChannelReadsServerWritesAsync(
        string tenantRef,
        string presentNoteRef,
        CancellationToken cancellationToken)
    {
        // Bounded poll, not a single read: every other read on this path carries a budget, and an eventually
        // consistent store returning null once is projection lag, not a broken channel. A single read turned
        // ordinary lag into a hard, misleading "the channel is broken" abort.
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.Elapsed < PresenceConfirmationWindow)
        {
            GovernedOperationView? polled = await _governedOperationProjections
                .GetAsync(tenantRef, presentNoteRef, cancellationToken)
                .ConfigureAwait(false);
            if (polled is not null)
            {
                return;
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        GovernedOperationView? control = await _governedOperationProjections
            .GetAsync(tenantRef, presentNoteRef, cancellationToken)
            .ConfigureAwait(false);
        if (control is null)
        {
            throw new InvalidOperationException(
                "The governed-operation projection channel could not read a view the server is known to have "
                + "written, so no absence observed through it can be trusted.");
        }
    }

    /// <summary>
    /// Observes, over the full absence window, that the fault-probe note never materialises in the governed-operation
    /// projection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The read API cannot answer this question. A governed operation that does not exist is refused
    /// <c>403 authorization-denied</c> by the shipped safe-not-found semantics, and a <c>403</c> is not evidence that
    /// an unauthorized mutation is absent — absence must be observed, never inferred from "not 200". The API contract
    /// is deliberately not reopened; the probe moves to a channel that can actually observe the answer.
    /// </para>
    /// <para>
    /// It reads the projection through the production <see cref="ReadModelGovernedOperationViewStore"/>, so the store
    /// name and key are exactly the ones <c>GovernedOperationProjectionHandler</c> writes and cannot drift from them.
    /// A missing key is a definite answer from the store that would hold the projection if it existed, not a refusal.
    /// A read that faults propagates rather than counting as absence.
    /// </para>
    /// <para>
    /// This stays independent of the durable half, which <see cref="ReadEventStoreEndStateAsync"/> computes
    /// separately from EventStore's aggregate actor state through <see cref="EventStoreDurableStateProbe"/> — a
    /// different service and a different store. Neither half is derived from the other, and the assertion requires
    /// both.
    /// </para>
    /// </remarks>
    /// <param name="tenantRef">The tenant partition the projection would have been written under.</param>
    /// <param name="noteRef">The governed note that must never have landed.</param>
    /// <param name="cancellationToken">Cancels the observation.</param>
    /// <returns><see langword="true"/> when the projection stayed absent for the whole window.</returns>
    private async Task<bool> RemainsGovernedOperationProjectionAbsentAsync(
        string tenantRef,
        string noteRef,
        CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        bool observedAtLeastOnce = false;
        while (timer.Elapsed < AbsenceConfirmationWindow)
        {
            GovernedOperationView? view = await _governedOperationProjections
                .GetAsync(tenantRef, noteRef, cancellationToken)
                .ConfigureAwait(false);
            observedAtLeastOnce = true;
            if (view is not null)
            {
                return false;
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        // Close the final-delay race, and never report absence from a window in which nothing was read.
        GovernedOperationView? finalView = await _governedOperationProjections
            .GetAsync(tenantRef, noteRef, cancellationToken)
            .ConfigureAwait(false);

        // The loop always sets observedAtLeastOnce on its first iteration, so it only guarded the case where the
        // window was zero — and there the method would otherwise report a null final read as "present". Keep the
        // explicit requirement that at least one in-window read completed.
        return observedAtLeastOnce && finalView is null;
    }

    /// <summary>
    /// Waits, over a bounded budget, for a governed operation to become present in the projection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reads the projection channel, not the read API, for the same reason the absence probe and the cleanup
    /// verification already do: a governed operation that does not exist is refused <c>403 authorization-denied</c>
    /// by the shipped safe-not-found semantics, never <c>404</c>. Polling the API for presence therefore threw on
    /// the FIRST read whenever the projection had not landed yet — the ordinary case at seeding and the whole point
    /// of the budget after an outage — and it threw <see cref="InvalidOperationException"/>, which the
    /// reconstruction loop's <c>catch (TimeoutException)</c> does not catch. The measurable partial-loss outcome
    /// this drill exists to report was structurally unreachable: a genuinely lost note aborted the scenario as
    /// unmeasurable instead of decrementing the reconstructed count.
    /// </para>
    /// <para>
    /// The same store, component and key shape as <see cref="RemainsGovernedOperationProjectionAbsentAsync"/>, so
    /// presence and absence are decided by one channel and cannot disagree for want of a different reader.
    /// </para>
    /// </remarks>
    /// <param name="noteRef">The governed note to wait for.</param>
    /// <param name="tenantRef">The tenant partition it was written under.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns><see langword="true"/> once the projection is present.</returns>
    /// <exception cref="TimeoutException">The projection did not materialise within the budget.</exception>
    /// <summary>Polls, over the presence budget, for a governed-operation projection without throwing.</summary>
    /// <param name="tenantRef">The tenant partition to read.</param>
    /// <param name="noteRef">The governed note to look for.</param>
    /// <param name="cancellationToken">Cancels the poll.</param>
    /// <returns><see langword="true"/> when the projection was observed present.</returns>
    private async Task<bool> IsGovernedOperationProjectionPresentAsync(
        string tenantRef,
        string noteRef,
        CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.Elapsed < PresenceConfirmationWindow)
        {
            GovernedOperationView? view = await _governedOperationProjections
                .GetAsync(tenantRef, noteRef, cancellationToken)
                .ConfigureAwait(false);
            if (view is not null)
            {
                return true;
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        return await _governedOperationProjections
            .GetAsync(tenantRef, noteRef, cancellationToken)
            .ConfigureAwait(false) is not null;
    }

    private async Task<bool> WaitForGovernedOperationProjectionAsync(
        string noteRef,
        string tenantRef,
        CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.Elapsed < PresenceConfirmationWindow)
        {
            GovernedOperationView? view = await _governedOperationProjections
                .GetAsync(tenantRef, noteRef, cancellationToken)
                .ConfigureAwait(false);
            if (view is not null)
            {
                return true;
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        // Close the final-delay race: a projection can materialize after the last loop read but before the delay
        // crosses the deadline. The boundary observation, rather than the preceding sleep, decides the result.
        GovernedOperationView? finalView = await _governedOperationProjections
            .GetAsync(tenantRef, noteRef, cancellationToken)
            .ConfigureAwait(false);
        return finalView is not null
            ? true
            : throw new TimeoutException(
                "The governed checkpoint projection did not materialize before the scenario deadline.");
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
        string? notificationPhase = null,
        string scenarioLane = RecoveryNotificationIdentity.ContinuityLane)
    {
        using HttpRequestMessage request = SandboxRequest(
            tenantRef,
            action,
            includeBearer,
            method ?? HttpMethod.Post,
            scenarioLane);
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
            // Deliberately NOT retried: `process` is side-effectful, and a blind retry would manufacture the very
            // duplicate side effects this scenario exists to rule out. Naming the action and status is what makes
            // the resulting `unmeasurable` diagnosable instead of opaque.
            throw new InvalidOperationException(
                $"The closed recovery-sandbox action '{action}' did not complete successfully (status {(int)response.StatusCode}).");
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonDocument.Parse(body);
    }

    private HttpRequestMessage SandboxRequest(
        string tenantRef,
        string action,
        bool includeBearer,
        HttpMethod method,
        string scenarioLane = RecoveryNotificationIdentity.ContinuityLane)
    {
        string tenant = Uri.EscapeDataString(tenantRef);
        HttpRequestMessage request = new(method, $"/recovery/{tenant}/m365-subscription-failure/{action}");
        request.Headers.Add("X-Recovery-Controller-Secret", _controllerSecret);
        request.Headers.Add("X-Recovery-Scenario-Lane", scenarioLane);

        return request;
    }
}
