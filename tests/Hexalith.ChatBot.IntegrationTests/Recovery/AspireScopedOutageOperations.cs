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

/// <summary>Closed Aspire/provider-sandbox operations for the six canonical scoped-outage dependencies.</summary>
internal sealed class AspireScopedOutageOperations : IScopedOutageSandboxOperations
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// How long an absence assertion polls before concluding a resource genuinely is not there. Kept equal to the
    /// presence budget so a no-duplicate claim is not systematically easier to satisfy than a materialization claim.
    /// </summary>
    private static readonly TimeSpan DefaultAbsenceConfirmationWindow = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How long to wait for a stopped identity provider to stop serving, matching EventStore's stop confirmation.
    /// </summary>
    private static readonly TimeSpan StopConfirmationTimeout = TimeSpan.FromSeconds(60);
    private readonly DistributedApplication? _application;
    private readonly IResource? _securityResource;
    private readonly HttpClient? _chatBotClient;
    private readonly HttpClient? _sandboxClient;
    private readonly HttpClient? _securityClient;
    private readonly ReadModelProjectConversationProjectionStore _readModels;
    private readonly IReadModelConditionalEraser _readModelEraser;
    private readonly RecoveryIntakeReadModelProbe _intakeReadModels;
    private readonly EventStoreDurableStateProbe _durableState;
    private string? _controlAccessToken;
    private readonly string? _mailboxClientSecret;
    private readonly string? _controllerSecret;
    private readonly ScopedOutageOperationsTestSeam? _testSeam;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _absenceConfirmationWindow;
    private ScopedOutageRecoveryCleanupState _cleanupState = new();

    public AspireScopedOutageOperations(
        DistributedApplication application,
        IResource securityResource,
        string controlAccessToken,
        string mailboxClientSecret,
        string controllerSecret,
        IReadModelStore readModelStore,
        IReadModelConditionalEraser readModelEraser,
        EventStoreDurableStateProbe durableState)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(securityResource);
        ArgumentException.ThrowIfNullOrWhiteSpace(controlAccessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(mailboxClientSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(controllerSecret);
        ArgumentNullException.ThrowIfNull(readModelStore);
        ArgumentNullException.ThrowIfNull(readModelEraser);
        ArgumentNullException.ThrowIfNull(durableState);

        _application = application;
        _securityResource = securityResource;
        _controlAccessToken = controlAccessToken;
        _mailboxClientSecret = mailboxClientSecret;
        _controllerSecret = controllerSecret;
        _readModels = new ReadModelProjectConversationProjectionStore(readModelStore);
        _readModelEraser = readModelEraser;
        _intakeReadModels = new RecoveryIntakeReadModelProbe(readModelEraser);
        _durableState = durableState;
        _chatBotClient = application.CreateHttpClient("chatbot");
        _chatBotClient.Timeout = TimeSpan.FromSeconds(30);
        _sandboxClient = application.CreateHttpClient("recovery-sandbox", "http");
        _sandboxClient.Timeout = TimeSpan.FromSeconds(30);
        _securityClient = application.CreateHttpClient("security", "http");
        _securityClient.Timeout = TimeSpan.FromSeconds(10);
        _pollInterval = DefaultPollInterval;
        _absenceConfirmationWindow = DefaultAbsenceConfirmationWindow;
    }

    /// <summary>Initializes infrastructure-free concrete scoped-outage operations for always-run tests.</summary>
    /// <param name="testSeam">The environment-facing delegates used by the concrete methods.</param>
    /// <param name="controlAccessToken">The retained control bearer used by the Identity branch.</param>
    /// <param name="readModelStore">The read-model store used by production projection readers.</param>
    /// <param name="readModelEraser">The conditional read-model eraser.</param>
    /// <param name="durableState">The EventStore durable-state probe.</param>
    /// <param name="pollInterval">The polling cadence used by test observations.</param>
    /// <param name="absenceConfirmationWindow">The sustained-absence observation window.</param>
    internal AspireScopedOutageOperations(
        ScopedOutageOperationsTestSeam testSeam,
        string controlAccessToken,
        IReadModelStore readModelStore,
        IReadModelConditionalEraser readModelEraser,
        EventStoreDurableStateProbe durableState,
        TimeSpan pollInterval,
        TimeSpan absenceConfirmationWindow)
    {
        ArgumentNullException.ThrowIfNull(testSeam);
        ArgumentException.ThrowIfNullOrWhiteSpace(controlAccessToken);
        ArgumentNullException.ThrowIfNull(readModelStore);
        ArgumentNullException.ThrowIfNull(readModelEraser);
        ArgumentNullException.ThrowIfNull(durableState);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pollInterval, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(absenceConfirmationWindow, TimeSpan.Zero);

        _testSeam = testSeam;
        _controlAccessToken = controlAccessToken;
        _readModels = new ReadModelProjectConversationProjectionStore(readModelStore);
        _readModelEraser = readModelEraser;
        _intakeReadModels = new RecoveryIntakeReadModelProbe(readModelEraser, pollInterval);
        _durableState = durableState;
        _pollInterval = pollInterval;
        _absenceConfirmationWindow = absenceConfirmationWindow;
    }

    /// <summary>Gets the currently active scoped-outage cleanup generation for regression assertions.</summary>
    internal ScopedOutageRecoveryCleanupState ActiveCleanupState => _cleanupState;

    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public async ValueTask CheckpointAsync(
        string dependency,
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (string.Equals(dependency, ScopedOutageDependencies.Identity, StringComparison.Ordinal))
        {
            if (!await TryAcquireRecoveryTokenOnceAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The identity boundary was unavailable before injection.");
            }

            // Refresh the retained control bearer here, while identity is still healthy and moments before injection.
            // It cannot be refreshed DURING the outage (that would probe the known-faulted token endpoint instead of
            // ChatBot's cached-key validation), but minting it at topology startup meant that by the time Identity ran
            // — last in the sweep, after both continuity drills, the rebuild and five scoped scenarios — it could be
            // older than the realm's 3600s lifespan. An expired bearer then produced a 401 that the driver reported as
            // an NFR58 containment failure rather than a harness problem.
            _controlAccessToken = await AcquireControlAccessTokenAsync(cancellationToken).ConfigureAwait(false);

            _cleanupState.IdentityAffectedSentinel = IdentitySentinel(
                RecoveryValidationTopology.StorageTenantRef,
                "recovery-identity-affected-sentinel");
            _cleanupState.IdentityControlSentinel = IdentitySentinel(
                RecoveryValidationTopology.ControlTenantRef,
                "recovery-identity-control-sentinel");
            await _readModels.UpsertSourceEmailAsync(_cleanupState.IdentityAffectedSentinel, cancellationToken).ConfigureAwait(false);
            await _readModels.UpsertSourceEmailAsync(_cleanupState.IdentityControlSentinel, cancellationToken).ConfigureAwait(false);
        }
        else if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            using JsonDocument restored = await SendSubscriptionAsync(tenantRef, "restore", includeBearer: false, cancellationToken).ConfigureAwait(false);
            if (RecoverySandboxRestoreResponse.WasPreviouslyFaulted(restored.RootElement) ||
                RecoverySandboxRestoreResponse.IsCurrentlyFaulted(restored.RootElement))
            {
                throw new InvalidOperationException("The Graph boundary was not clean before injection.");
            }

            _cleanupState.GraphAffectedSentinel = GraphSentinel(
                RecoveryValidationTopology.StorageTenantRef,
                "recovery-graph-affected-sentinel");
            _cleanupState.GraphControlSentinel = GraphSentinel(
                RecoveryValidationTopology.ControlTenantRef,
                "recovery-graph-control-sentinel");
            await _readModels.UpsertSourceEmailAsync(_cleanupState.GraphAffectedSentinel, cancellationToken).ConfigureAwait(false);
            await _readModels.UpsertSourceEmailAsync(_cleanupState.GraphControlSentinel, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            using JsonDocument restored = await SendScopedAsync(
                dependency,
                tenantRef,
                "restore",
                correlationId: null,
                cancellationToken)
                .ConfigureAwait(false);
            if (RecoverySandboxRestoreResponse.WasPreviouslyFaulted(restored.RootElement) ||
                RecoverySandboxRestoreResponse.IsCurrentlyFaulted(restored.RootElement))
            {
                throw new InvalidOperationException("The scoped dependency was not clean before injection.");
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask FaultAsync(string dependency, string tenantRef, CancellationToken cancellationToken)
    {
        if (string.Equals(dependency, ScopedOutageDependencies.Identity, StringComparison.Ordinal))
        {
            ExecuteCommandResult result = await ExecuteSecurityCommandAsync(KnownResourceCommands.StopCommand, cancellationToken).ConfigureAwait(false);
            bool commandSucceeded = result.Success && !result.Canceled;
            bool available = true;
            using (CancellationTokenSource stopDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                stopDeadline.CancelAfter(StopConfirmationTimeout);
                try
                {
                    while (true)
                    {
                        available = await IsIdentityAvailableAsync(stopDeadline.Token).ConfigureAwait(false);
                        if (!available)
                        {
                            break;
                        }

                        await Task.Delay(_pollInterval, stopDeadline.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Confirmation window elapsed while Keycloak still served.
                }
            }

            // Shared with the EventStore boundary check (AspireRecoverySandboxOperations.StopReachedDependencyBoundary),
            // which the LiveContinuityDrillScenarioRunnerTests theory table-tests exhaustively for this exact
            // (commandSucceeded, endpointAvailable) predicate — this branch previously had no coverage outside the
            // skipped Tier-3 E2E.
            if (!AspireRecoverySandboxOperations.StopReachedDependencyBoundary(commandSucceeded, available))
            {
                if (!commandSucceeded)
                {
                    throw new InvalidOperationException("The allowlisted identity stop command did not complete successfully.");
                }

                throw new InvalidOperationException(
                    $"Identity was still reachable {StopConfirmationTimeout.TotalSeconds:N0}s after an accepted stop command.");
            }

            return;
        }

        if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            using JsonDocument faulted = await SendSubscriptionAsync(tenantRef, "fault", includeBearer: false, cancellationToken).ConfigureAwait(false);
            if (!faulted.RootElement.GetProperty("faulted").GetBoolean())
            {
                throw new InvalidOperationException("The Graph boundary did not report itself faulted after injection.");
            }

            return;
        }

        using JsonDocument scopedFaulted = await SendScopedAsync(dependency, tenantRef, "fault", correlationId: null, cancellationToken).ConfigureAwait(false);
        if (!scopedFaulted.RootElement.GetProperty("faulted").GetBoolean())
        {
            throw new InvalidOperationException(
                $"The scoped dependency '{dependency}' did not report itself faulted after injection.");
        }
    }

    /// <inheritdoc />
    public async ValueTask<ScopedOutageFaultObservation> ObserveFaultAsync(
        string dependency,
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        DateTimeOffset observedAtUtc;
        DateTimeOffset recordedAtUtc;
        string observedScope;
        bool unauthorizedMutationDetected;
        if (string.Equals(dependency, ScopedOutageDependencies.Identity, StringComparison.Ordinal))
        {
            observedAtUtc = UtcNow;
            if (await TryAcquireRecoveryTokenOnceAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The identity outage was not observed by token acquisition.");
            }

            using JsonDocument monitored = await SendScopeObservationAsync(
                dependency,
                tenantRef,
                correlationId,
                "identity_token_unavailable",
                cancellationToken).ConfigureAwait(false);
            observedAtUtc = monitored.RootElement
                .GetProperty("dependencyFailureObservedAtUtc").GetDateTimeOffset().ToUniversalTime();
            recordedAtUtc = monitored.RootElement
                .GetProperty("scopeRecordedAtUtc").GetDateTimeOffset().ToUniversalTime();
            observedScope = RequireObservedScope(
                monitored.RootElement.GetProperty("observedScope").GetString(),
                "Identity");
            RequireNonDegenerateScopeStamps(observedAtUtc, recordedAtUtc, "Identity");
            ProjectConversationSourceEmailView? affected = await _readModels
                .GetSourceEmailAsync(
                    _cleanupState.IdentityAffectedSentinel!.TenantId,
                    _cleanupState.IdentityAffectedSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            ProjectConversationSourceEmailView? control = await _readModels
                .GetSourceEmailAsync(
                    _cleanupState.IdentityControlSentinel!.TenantId,
                    _cleanupState.IdentityControlSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            _cleanupState.IdentityFaultLeftStateUnchanged = Equals(affected, _cleanupState.IdentityAffectedSentinel) &&
                Equals(control, _cleanupState.IdentityControlSentinel);
            unauthorizedMutationDetected = !_cleanupState.IdentityFaultLeftStateUnchanged;
        }
        else if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            using JsonDocument process = await SendSubscriptionAsync(tenantRef, "process", includeBearer: true, cancellationToken).ConfigureAwait(false);
            JsonElement root = process.RootElement;
            _ = CaptureGraphResponseIdentities(
                root,
                _cleanupState.GraphIntakeRefs,
                requireEquality: true,
                "The Graph fault response returned an invalid cleanup identity.");
            if (root.GetProperty("submitted").GetBoolean() ||
                !string.Equals(root.GetProperty("reasonCode").GetString(), "graph_subscription_expired", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The Worker path did not observe the Graph outage.");
            }

            using JsonDocument monitored = await SendScopeObservationAsync(
                dependency,
                tenantRef,
                correlationId,
                root.GetProperty("reasonCode").GetString() ?? "graph_subscription_expired",
                cancellationToken).ConfigureAwait(false);
            observedAtUtc = monitored.RootElement
                .GetProperty("dependencyFailureObservedAtUtc").GetDateTimeOffset().ToUniversalTime();
            recordedAtUtc = monitored.RootElement
                .GetProperty("scopeRecordedAtUtc").GetDateTimeOffset().ToUniversalTime();
            observedScope = RequireObservedScope(
                monitored.RootElement.GetProperty("observedScope").GetString(),
                "Graph");
            RequireNonDegenerateScopeStamps(observedAtUtc, recordedAtUtc, "Graph");
            ProjectConversationSourceEmailView? affected = await _readModels
                .GetSourceEmailAsync(
                    _cleanupState.GraphAffectedSentinel!.TenantId,
                    _cleanupState.GraphAffectedSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            ProjectConversationSourceEmailView? control = await _readModels
                .GetSourceEmailAsync(
                    _cleanupState.GraphControlSentinel!.TenantId,
                    _cleanupState.GraphControlSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            _cleanupState.GraphFaultLeftStateUnchanged = Equals(affected, _cleanupState.GraphAffectedSentinel) &&
                Equals(control, _cleanupState.GraphControlSentinel);
            unauthorizedMutationDetected = !_cleanupState.GraphFaultLeftStateUnchanged;
        }
        else
        {
            using JsonDocument process = await SendScopedAsync(dependency, tenantRef, "process", correlationId, cancellationToken).ConfigureAwait(false);
            JsonElement root = process.RootElement;
            if (!root.GetProperty("faulted").GetBoolean() ||
                !string.Equals(root.GetProperty("outcome").GetString(), "recoverable-failure", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The topology-composed dependency boundary did not observe its injected fault.");
            }

            observedAtUtc = root.GetProperty("observedAtUtc").GetDateTimeOffset().ToUniversalTime();
            recordedAtUtc = root.GetProperty("scopeRecordedAtUtc").GetDateTimeOffset().ToUniversalTime();
            observedScope = RequireObservedScope(root.GetProperty("observedScope").GetString(), dependency);
            RequireNonDegenerateScopeStamps(observedAtUtc, recordedAtUtc, dependency);
            unauthorizedMutationDetected = root.GetProperty("unauthorizedMutationDetected").GetBoolean();
        }

        ControlAvailability controlAvailability = await IsChatBotControlAvailableAsync(dependency, cancellationToken)
            .ConfigureAwait(false);
        return new ScopedOutageFaultObservation(
            observedAtUtc,
            recordedAtUtc,
            observedScope,
            controlAvailability.Succeeded,
            unauthorizedMutationDetected)
        {
            IndependentControlUnobserved = controlAvailability.Unobserved,
            IndependentControlCause = controlAvailability.Cause,
        };
    }

    /// <inheritdoc />
    public async ValueTask<bool> RestoreAsync(string dependency, string tenantRef, CancellationToken cancellationToken)
    {
        if (_testSeam?.RestoreAsync is { } testRestore)
        {
            return await testRestore(dependency, tenantRef, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(dependency, ScopedOutageDependencies.Identity, StringComparison.Ordinal))
        {
            ExecuteCommandResult result = await ExecuteSecurityCommandAsync(KnownResourceCommands.StartCommand, cancellationToken).ConfigureAwait(false);
            if (!result.Success || result.Canceled)
            {
                throw new InvalidOperationException("The allowlisted identity start command did not complete.");
            }

            await Application.ResourceNotifications
                .WaitForResourceHealthyAsync(SecurityResource.Name, cancellationToken)
                .ConfigureAwait(false);
            await WaitForIdentityAsync(cancellationToken).ConfigureAwait(false);

            // Identity leak detection compares sentinel read models directly (see ObserveFaultAsync/VerifyRecoveryAsync)
            // rather than the scoped-outage effect ledger, so restore does not clear anything this check would see.
            return false;
        }

        if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            using JsonDocument restored = await SendSubscriptionAsync(tenantRef, "restore", includeBearer: false, cancellationToken).ConfigureAwait(false);
            if (RecoverySandboxRestoreResponse.IsCurrentlyFaulted(restored.RootElement))
            {
                throw new InvalidOperationException("The Graph boundary remained faulted after restore.");
            }

            // Graph leak detection also compares sentinel read models (see ObserveFaultAsync/VerifyRecoveryAsync), not
            // the scoped-outage effect ledger this restore call clears.
            return false;
        }

        using JsonDocument scopedRestored = await SendScopedAsync(dependency, tenantRef, "restore", correlationId: null, cancellationToken).ConfigureAwait(false);
        if (RecoverySandboxRestoreResponse.IsCurrentlyFaulted(scopedRestored.RootElement))
        {
            throw new InvalidOperationException($"The scoped dependency '{dependency}' remained faulted after restore.");
        }

        // Read before the sandbox's effect ledger clear is lost to the caller: a cross-tenant write during the fault
        // window (the highest-risk moment) must be observable here, since VerifyRecoveryAsync's post-restore probes
        // cannot see anything this restore already erased.
        return RecoverySandboxRestoreResponse.CrossTenantEffectDetectedBeforeRestore(scopedRestored.RootElement);
    }

    /// <inheritdoc />
    public async ValueTask<ScopedOutageRecoveryEndState> VerifyRecoveryAsync(
        string dependency,
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        bool recovered;
        bool duplicate = false;
        bool silentLoss = false;
        bool leakage = false;
        if (string.Equals(dependency, ScopedOutageDependencies.Identity, StringComparison.Ordinal))
        {
            ProjectConversationSourceEmailView? affected = await _readModels
                .GetSourceEmailAsync(
                    _cleanupState.IdentityAffectedSentinel!.TenantId,
                    _cleanupState.IdentityAffectedSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            ProjectConversationSourceEmailView? control = await _readModels
                .GetSourceEmailAsync(
                    _cleanupState.IdentityControlSentinel!.TenantId,
                    _cleanupState.IdentityControlSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            bool affectedUnchanged = Equals(affected, _cleanupState.IdentityAffectedSentinel);
            bool controlUnchanged = Equals(control, _cleanupState.IdentityControlSentinel);
            recovered = await TryAcquireRecoveryTokenOnceAsync(cancellationToken).ConfigureAwait(false) && affectedUnchanged;
            leakage = !controlUnchanged;
            silentLoss = affected is null;
            // Identity has no duplicate-commit probe; sentinel drift is leakage/non-recovery, not duplication.
            duplicate = false;
        }
        else if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            using JsonDocument process = await SendSubscriptionAsync(tenantRef, "process", includeBearer: true, cancellationToken).ConfigureAwait(false);
            JsonElement processRoot = process.RootElement;
            (string? recoveredIntakeRef, string? recoveredCandidateRef) = CaptureGraphResponseIdentities(
                processRoot,
                _cleanupState.GraphIntakeRefs,
                requireEquality: true,
                "The restored Graph path returned an invalid cleanup identity.");
            if (!processRoot.GetProperty("submitted").GetBoolean() ||
                recoveredIntakeRef is null || recoveredCandidateRef is null)
            {
                throw new InvalidOperationException("The restored Graph path did not return both contract identities.");
            }

            _cleanupState.GraphRecoveredIntakeRef = recoveredIntakeRef;

            await _durableState.WaitForMailboxIntakeAsync(
                RecoveryValidationTopology.StorageTenantRef,
                recoveredIntakeRef,
                cancellationToken).ConfigureAwait(false);
            using JsonDocument duplicateProbe = await SendSubscriptionAsync(
                tenantRef,
                "process",
                includeBearer: true,
                cancellationToken).ConfigureAwait(false);
            JsonElement duplicateRoot = duplicateProbe.RootElement;
            (string? duplicateIntakeRef, string? duplicateCandidateRef) = CaptureGraphResponseIdentities(
                duplicateRoot,
                _cleanupState.GraphIntakeRefs,
                requireEquality: true,
                "The Graph duplicate probe returned an invalid cleanup identity.");
            foreach (string identity in EnumeratePresent(duplicateIntakeRef, duplicateCandidateRef))
            {
                _cleanupState.GraphDurableAbsenceRefs.Add(identity);
            }

            if (!duplicateRoot.GetProperty("submitted").GetBoolean() ||
                duplicateIntakeRef is null || duplicateCandidateRef is null)
            {
                throw new InvalidOperationException("The Graph duplicate probe did not reach the Worker path.");
            }

            _cleanupState.GraphDuplicateProbeIntakeRef = duplicateIntakeRef;

            // Poll for the full absence window rather than reading once after a fixed 2s sleep: a duplicate aggregate
            // that commits a moment later than the sleep was being reported as "no duplicate side effect".
            Task<bool> duplicateAggregateAbsent = _durableState.RemainsAbsentAsync(
                RecoveryValidationTopology.StorageTenantRef,
                duplicateIntakeRef,
                _absenceConfirmationWindow,
                cancellationToken);
            Task<bool> controlTenantAggregateAbsent = _durableState.RemainsAbsentAsync(
                RecoveryValidationTopology.ControlTenantRef,
                recoveredIntakeRef,
                _absenceConfirmationWindow,
                cancellationToken);
            Task<bool> controlTenantReadModelsAbsent = _intakeReadModels.RemainsAbsentAsync(
                RecoveryValidationTopology.ControlTenantRef,
                recoveredIntakeRef,
                _absenceConfirmationWindow,
                cancellationToken);
            bool[] isolationOutcomes = await Task.WhenAll(
                duplicateAggregateAbsent,
                controlTenantAggregateAbsent,
                controlTenantReadModelsAbsent).ConfigureAwait(false);
            bool recoveredAggregateStillCommitted = await _durableState.IsMailboxIntakeCommittedAsync(
                RecoveryValidationTopology.StorageTenantRef,
                recoveredIntakeRef,
                cancellationToken).ConfigureAwait(false);
            ProjectConversationSourceEmailView? affected = await _readModels
                .GetSourceEmailAsync(
                    _cleanupState.GraphAffectedSentinel!.TenantId,
                    _cleanupState.GraphAffectedSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            ProjectConversationSourceEmailView? control = await _readModels
                .GetSourceEmailAsync(
                    _cleanupState.GraphControlSentinel!.TenantId,
                    _cleanupState.GraphControlSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            recovered = recoveredAggregateStillCommitted;
            duplicate = string.Equals(duplicateIntakeRef, recoveredIntakeRef, StringComparison.Ordinal) ||
                !isolationOutcomes[0];
            silentLoss = !recovered;
            leakage = !_cleanupState.GraphFaultLeftStateUnchanged ||
                !Equals(affected, _cleanupState.GraphAffectedSentinel) ||
                !Equals(control, _cleanupState.GraphControlSentinel) ||
                !isolationOutcomes[1] ||
                !isolationOutcomes[2];
        }
        else
        {
            using JsonDocument first = await SendScopedAsync(dependency, tenantRef, "process", correlationId, cancellationToken).ConfigureAwait(false);
            JsonElement firstRoot = first.RootElement;
            if (firstRoot.GetProperty("faulted").GetBoolean() ||
                !string.Equals(firstRoot.GetProperty("outcome").GetString(), "completed", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The first post-restore process for '{dependency}' did not complete cleanly.");
            }

            using JsonDocument second = await SendScopedAsync(dependency, tenantRef, "process", correlationId, cancellationToken).ConfigureAwait(false);
            JsonElement root = second.RootElement;
            recovered = !root.GetProperty("faulted").GetBoolean() &&
                string.Equals(root.GetProperty("outcome").GetString(), "completed", StringComparison.Ordinal);
            duplicate = root.GetProperty("duplicateSideEffectDetected").GetBoolean() ||
                root.GetProperty("effectCount").GetInt32() != 1;
            silentLoss = root.GetProperty("silentDataLossDetected").GetBoolean();
            leakage = root.GetProperty("crossTenantLeakageDetected").GetBoolean();
        }

        List<Task<bool>> controlIsolationChecks = [];
        foreach (string controlNoteRef in _cleanupState.ControlOperationRefs)
        {
            controlIsolationChecks.Add(_durableState.RemainsGovernedNoteAbsentAsync(
                RecoveryValidationTopology.StorageTenantRef,
                controlNoteRef,
                _absenceConfirmationWindow,
                cancellationToken));
            controlIsolationChecks.Add(RemainsReadModelKeyAbsentAsync(
                GovernedOperationView.KeyFor(RecoveryValidationTopology.StorageTenantRef, controlNoteRef),
                _absenceConfirmationWindow,
                cancellationToken));
        }

        if (controlIsolationChecks.Count > 0)
        {
            bool[] controlIsolation = await Task.WhenAll(controlIsolationChecks).ConfigureAwait(false);
            leakage |= controlIsolation.Any(static absent => !absent);
        }

        return new ScopedOutageRecoveryEndState(recovered, leakage, silentLoss, duplicate);
    }

    /// <inheritdoc />
    public async ValueTask<bool> CleanupAsync(string dependency, string tenantRef, CancellationToken cancellationToken)
    {
        ScopedOutageRecoveryCleanupState cleanupState = ScopedOutageRecoveryCleanupState.DetachAndReset(
            ref _cleanupState);
        List<(string Tenant, string Intake)> intakeTargets = [];
        if (string.Equals(dependency, ScopedOutageDependencies.Identity, StringComparison.Ordinal))
        {
            AddIntakeTarget(intakeTargets, cleanupState.IdentityAffectedSentinel);
            AddIntakeTarget(intakeTargets, cleanupState.IdentityControlSentinel);
        }
        else if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            AddIntakeTarget(intakeTargets, cleanupState.GraphAffectedSentinel);
            AddIntakeTarget(intakeTargets, cleanupState.GraphControlSentinel);
            foreach (string intakeRef in cleanupState.GraphIntakeRefs)
            {
                AddIntakeTarget(intakeTargets, RecoveryValidationTopology.StorageTenantRef, intakeRef);
            }
        }

        try
        {
            foreach ((string cleanupTenant, string cleanupIntake) in intakeTargets)
            {
                await EraseIntakeReadModelsAsync(cleanupTenant, cleanupIntake, cancellationToken).ConfigureAwait(false);
            }

            foreach (string controlNoteRef in cleanupState.ControlOperationRefs)
            {
                await EraseReadModelAsync(
                    GovernedOperationView.KeyFor(RecoveryValidationTopology.ControlTenantRef, controlNoteRef),
                    cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            if (string.Equals(dependency, ScopedOutageDependencies.Identity, StringComparison.Ordinal))
            {
                if (!await TryAcquireRecoveryTokenOnceAsync(cancellationToken).ConfigureAwait(false) ||
                    !await IsIdentityAvailableAsync(cancellationToken).ConfigureAwait(false))
                {
                    await RestoreAsync(dependency, tenantRef, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                await RestoreAsync(dependency, tenantRef, cancellationToken).ConfigureAwait(false);
            }
        }

        if (string.Equals(dependency, ScopedOutageDependencies.Identity, StringComparison.Ordinal))
        {
            if (!await TryAcquireRecoveryTokenOnceAsync(cancellationToken).ConfigureAwait(false) ||
                !await IsIdentityAvailableAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("Identity remained unavailable after scoped-outage cleanup.");
            }
        }
        else
        {
            using JsonDocument status = string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal)
                ? await SendSubscriptionAsync(tenantRef, "status", includeBearer: false, cancellationToken, HttpMethod.Get).ConfigureAwait(false)
                : await SendScopedAsync(dependency, tenantRef, "status", correlationId: null, cancellationToken, HttpMethod.Get).ConfigureAwait(false);
            if (status.RootElement.GetProperty("faulted").GetBoolean() ||
                (!string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal) &&
                    status.RootElement.GetProperty("effectCount").GetInt32() != 0))
            {
                throw new InvalidOperationException("The dependency retained fault/effect state after cleanup.");
            }
        }

        bool complete = true;
        foreach ((string cleanupTenant, string cleanupIntake) in intakeTargets)
        {
            complete &= await _intakeReadModels.AreAbsentAsync(
                cleanupTenant,
                cleanupIntake,
                cancellationToken).ConfigureAwait(false);
        }

        foreach (string controlNoteRef in cleanupState.ControlOperationRefs)
        {
            (bool present, _) = await _readModelEraser.TryReadEtagAsync(
                ChatBotReadModelStoreNames.StateStoreName,
                GovernedOperationView.KeyFor(RecoveryValidationTopology.ControlTenantRef, controlNoteRef),
                cancellationToken).ConfigureAwait(false);
            complete &= !present;
        }

        if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            foreach (string intakeRef in cleanupState.GraphDurableAbsenceRefs)
            {
                complete &= await _durableState.RemainsAbsentAsync(
                    RecoveryValidationTopology.StorageTenantRef,
                    intakeRef,
                    _absenceConfirmationWindow,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return complete;
    }

    private static (string? IntakeRef, string? CandidateRef) CaptureGraphResponseIdentities(
        JsonElement root,
        ISet<string> retainedIdentities,
        bool requireEquality,
        string invalidResponseMessage)
    {
        ArgumentNullException.ThrowIfNull(retainedIdentities);
        ArgumentException.ThrowIfNullOrWhiteSpace(invalidResponseMessage);

        (bool IntakeExposed, string? IntakeRef) = ReadOptionalIdentity(root, "intakeId");
        (bool CandidateExposed, string? CandidateRef) = ReadOptionalIdentity(root, "candidateRef");
        bool intakeValid = !IntakeExposed || RecoveryValidationEvidenceManifest.IsCanonicalUlid(IntakeRef);
        bool candidateValid = !CandidateExposed || RecoveryValidationEvidenceManifest.IsCanonicalUlid(CandidateRef);
        if (intakeValid && IntakeRef is not null)
        {
            retainedIdentities.Add(IntakeRef);
        }

        if (candidateValid && CandidateRef is not null)
        {
            retainedIdentities.Add(CandidateRef);
        }

        if (!intakeValid || !candidateValid)
        {
            throw new InvalidOperationException(invalidResponseMessage);
        }

        if (requireEquality && IntakeRef is not null && CandidateRef is not null &&
            !string.Equals(IntakeRef, CandidateRef, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(invalidResponseMessage);
        }

        return (IntakeRef, CandidateRef);
    }

    private static (bool Exposed, string? Identity) ReadOptionalIdentity(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement identity) || identity.ValueKind == JsonValueKind.Null)
        {
            return (false, null);
        }

        return identity.ValueKind == JsonValueKind.String
            ? (true, identity.GetString())
            : (true, null);
    }

    private static IEnumerable<string> EnumeratePresent(params string?[] identities)
        => identities.OfType<string>();

    private static void AddIntakeTarget(
        ICollection<(string Tenant, string Intake)> targets,
        ProjectConversationSourceEmailView? sentinel)
    {
        if (sentinel is not null)
        {
            AddIntakeTarget(targets, sentinel.TenantId, sentinel.IntakeId);
        }
    }

    private static void AddIntakeTarget(
        ICollection<(string Tenant, string Intake)> targets,
        string tenantId,
        string? intakeId)
    {
        if (!string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(intakeId))
        {
            (string Tenant, string Intake) target = (tenantId, intakeId);
            if (!targets.Contains(target))
            {
                targets.Add(target);
            }
        }
    }

    /// <summary>Erases every read model an intake materializes, not just its source-email view.</summary>
    private async Task EraseIntakeReadModelsAsync(string tenantId, string intakeId, CancellationToken cancellationToken)
    {
        foreach (string key in RecoveryIntakeReadModelProbe.KeysFor(tenantId, intakeId))
        {
            await EraseReadModelAsync(key, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<bool> RemainsReadModelKeyAbsentAsync(
        string key,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        do
        {
            (bool present, _) = await _readModelEraser.TryReadEtagAsync(
                ChatBotReadModelStoreNames.StateStoreName,
                key,
                cancellationToken).ConfigureAwait(false);
            if (present)
            {
                return false;
            }

            await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
        }
        while (timer.Elapsed < window);

        (bool finalPresent, _) = await _readModelEraser.TryReadEtagAsync(
            ChatBotReadModelStoreNames.StateStoreName,
            key,
            cancellationToken).ConfigureAwait(false);
        return !finalPresent;
    }

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

        throw new InvalidOperationException($"Graph recovery cleanup could not erase read-model key '{key}'.");
    }

    private static ProjectConversationSourceEmailView GraphSentinel(string tenantRef, string intakeRef)
        => new(
            tenantRef,
            intakeRef,
            "recovery-graph-sentinel-mailbox",
            "recovery-graph-sentinel-message",
            InternetMessageId: null,
            "recovery-graph-sentinel-conversation",
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

    private static ProjectConversationSourceEmailView IdentitySentinel(string tenantRef, string intakeRef)
        => GraphSentinel(tenantRef, intakeRef) with
        {
            SourceMailboxId = "recovery-identity-sentinel-mailbox",
            SourceProviderMessageId = "recovery-identity-sentinel-message",
            SourceConversationId = "recovery-identity-sentinel-conversation",
        };

    private async ValueTask<ExecuteCommandResult> ExecuteSecurityCommandAsync(string command, CancellationToken cancellationToken)
    {
        ResourceCommandService commands = Application.Services.GetRequiredService<ResourceCommandService>();
        return await commands.ExecuteCommandAsync(SecurityResource, command, cancellationToken).ConfigureAwait(false);
    }

    private static string RequireObservedScope(string? observedScope, string dependencyLabel)
    {
        if (string.IsNullOrWhiteSpace(observedScope))
        {
            throw new InvalidOperationException(
                $"{dependencyLabel} scope monitoring returned no observed scope (missing monitoring is unmeasurable, not Tenant-by-default).");
        }

        return observedScope;
    }

    internal static void RequireNonDegenerateScopeStamps(
        DateTimeOffset observedAtUtc,
        DateTimeOffset recordedAtUtc,
        string dependencyLabel)
    {
        if (observedAtUtc == default || recordedAtUtc == default)
        {
            throw new InvalidOperationException(
                $"{dependencyLabel} scope monitoring returned a default timestamp (missing monitoring is unmeasurable).");
        }

        // Equal clocks collapse NFR41 latency to 0ms and look measured; Task 5 requires that path to be unmeasurable.
        if (recordedAtUtc <= observedAtUtc)
        {
            throw new InvalidOperationException(
                $"{dependencyLabel} scope-recording stamps are degenerate (recordedAtUtc <= observedAtUtc).");
        }
    }

    private async Task<bool> IsIdentityAvailableAsync(CancellationToken cancellationToken)
    {
        if (_testSeam?.IsIdentityAvailableAsync is { } testAvailability)
        {
            return await testAvailability(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            using HttpResponseMessage response = await SecurityClient.GetAsync("/realms/hexalith", cancellationToken).ConfigureAwait(false);
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

    private async Task<ControlAvailability> IsChatBotControlAvailableAsync(
        string dependency,
        CancellationToken cancellationToken)
    {
        try
        {
            // During the identity outage the independent control must use the bearer minted at checkpoint; trying
            // to refresh it would test the known-faulted token endpoint instead of ChatBot's cached-key validation.
            // Other scenarios mint at point of use, and RestoreAsync refreshes the retained bearer after Keycloak
            // restarts so no post-recovery operation relies on the pre-restart signing key.
            string controlAccessToken = string.Equals(dependency, ScopedOutageDependencies.Identity, StringComparison.Ordinal)
                ? ControlAccessToken
                : await AcquireControlAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            string noteRef = GovernedNoteId.New().Value;
            string commandRef = ChatBotCommandId.New().Value;
            string operationRef = ChatBotTaskId.New().Value;
            string correlationId = ChatBotCorrelationId.New().Value;
            _cleanupState.ControlOperationRefs.Add(noteRef);
            string body = $$"""
                {"commandId":"{{commandRef}}","commandType":"RecordGovernedNote","command":{"noteId":"{{noteRef}}"},"origin":"ui","requestSchemaVersion":"v1"}
                """;
            using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", controlAccessToken);
            request.Headers.Add("X-Correlation-Id", correlationId);
            request.Headers.Add("X-Hexalith-Task-Id", operationRef);
            using HttpResponseMessage response = await ChatBotClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                // A real answer from ChatBot: negative containment evidence, not a missing observation.
                return new ControlAvailability(
                    Succeeded: false,
                    Unobserved: false,
                    Cause: string.Create(CultureInfo.InvariantCulture, $"status-{(int)response.StatusCode}"));
            }

            await _durableState.WaitForGovernedNoteAsync(
                RecoveryValidationTopology.ControlTenantRef,
                noteRef,
                cancellationToken).ConfigureAwait(false);
            await WaitForControlOperationProjectionAsync(
                noteRef,
                correlationId,
                controlAccessToken,
                cancellationToken).ConfigureAwait(false);
            return new ControlAvailability(Succeeded: true, Unobserved: false, Cause: null);
        }
        catch (HttpRequestException exception)
        {
            // Never reached ChatBot. Previously reported as "did not succeed", i.e. indistinguishable from a
            // refusal — but a connection that never landed is a missing observation, not negative evidence.
            return new ControlAvailability(
                Succeeded: false,
                Unobserved: true,
                Cause: $"transport-{exception.HttpRequestError}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Distinct from a non-202: the control operation was never answered, so this run cannot say whether the
            // unaffected path kept working. Deliberately NOT retried and NOT treated as available — widening this
            // would let a genuine degradation of the control path read as containment (the NFR58 evidence).
            return new ControlAvailability(Succeeded: false, Unobserved: true, Cause: "client-timeout");
        }
    }

    /// <summary>A control result plus whether its absence was observed and a metadata-only cause.</summary>
    private sealed record ControlAvailability(bool Succeeded, bool Unobserved, string? Cause);

    private async Task WaitForIdentityAsync(CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromMinutes(3))
        {
            if (await TryAcquireRecoveryTokenOnceAsync(cancellationToken).ConfigureAwait(false))
            {
                _controlAccessToken = await AcquireControlAccessTokenAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("The identity provider did not recover before the restoration deadline.");
    }

    private async Task WaitForControlOperationProjectionAsync(
        string noteRef,
        string correlationId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.Elapsed < _absenceConfirmationWindow)
        {
            using HttpRequestMessage request = new(
                HttpMethod.Get,
                $"/api/v1/governed-operations/{noteRef}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("X-Correlation-Id", correlationId);
            using HttpResponseMessage response = await ChatBotClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(
                    $"The independent control projection returned unexpected status {(int)response.StatusCode}.");
            }

            await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("The independent control operation did not reach projected state.");
    }

    private async Task<bool> TryAcquireRecoveryTokenOnceAsync(CancellationToken cancellationToken)
    {
        if (_testSeam?.TryAcquireRecoveryTokenOnceAsync is { } testAcquire)
        {
            return await testAcquire(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            using FormUrlEncodedContent form = new(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "hexalith-chatbot",
                ["username"] = "recovery-validator",
                ["password"] = "recovery-validator-pass",
                ["scope"] = "openid",
            });
            using HttpResponseMessage response = await SecurityClient
                .PostAsync("/realms/hexalith/protocol/openid-connect/token", form, cancellationToken)
                .ConfigureAwait(false);
            return response.StatusCode == HttpStatusCode.OK;
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

    private async ValueTask<string> AcquireControlAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_testSeam?.AcquireControlAccessTokenAsync is { } testAcquire)
        {
            return await testAcquire(cancellationToken).ConfigureAwait(false);
        }

        return await RecoveryAccessTokenProvider.AcquireControlAsync(Application, cancellationToken).ConfigureAwait(false);
    }

    private DistributedApplication Application => _application ??
        throw new InvalidOperationException("This scoped-outage operation requires the live Aspire application.");

    private IResource SecurityResource => _securityResource ??
        throw new InvalidOperationException("This scoped-outage operation requires the live identity resource.");

    private HttpClient ChatBotClient => _chatBotClient ??
        throw new InvalidOperationException("This scoped-outage operation requires the live ChatBot client.");

    private HttpClient SandboxClient => _sandboxClient ??
        throw new InvalidOperationException("This scoped-outage operation requires the live recovery-sandbox client.");

    private HttpClient SecurityClient => _securityClient ??
        throw new InvalidOperationException("This scoped-outage operation requires the live identity client.");

    private string ControlAccessToken => _controlAccessToken ??
        throw new InvalidOperationException("This scoped-outage operation requires the retained control bearer.");

    private string MailboxClientSecret => _mailboxClientSecret ??
        throw new InvalidOperationException("This scoped-outage operation requires the mailbox client secret.");

    private string ControllerSecret => _controllerSecret ??
        throw new InvalidOperationException("This scoped-outage operation requires the recovery controller secret.");

    private async ValueTask<JsonDocument> SendSubscriptionAsync(
        string tenantRef,
        string action,
        bool includeBearer,
        CancellationToken cancellationToken,
        HttpMethod? method = null)
    {
        if (_testSeam?.SendSubscriptionAsync is { } testSubscription)
        {
            return await testSubscription(
                tenantRef,
                action,
                includeBearer,
                cancellationToken,
                method).ConfigureAwait(false);
        }

        string tenant = Uri.EscapeDataString(tenantRef);
        using HttpRequestMessage request = new(method ?? HttpMethod.Post, $"/recovery/{tenant}/m365-subscription-failure/{action}");
        request.Headers.Add("X-Recovery-Controller-Secret", ControllerSecret);
        request.Headers.Add("X-Recovery-Scenario-Lane", RecoveryNotificationIdentity.GraphLane);
        if (string.Equals(action, "process", StringComparison.Ordinal))
        {
            request.Headers.Add(
                RecoveryNotificationIdentity.HeaderName,
                RecoveryNotificationIdentity.RecoveryPhase);
        }
        if (includeBearer)
        {
            string mailboxAccessToken = await RecoveryAccessTokenProvider
                .AcquireMailboxAsync(Application, MailboxClientSecret, cancellationToken)
                .ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mailboxAccessToken);
        }

        return await SendSandboxAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<JsonDocument> SendScopedAsync(
        string dependency,
        string tenantRef,
        string action,
        string? correlationId,
        CancellationToken cancellationToken,
        HttpMethod? method = null)
    {
        using HttpRequestMessage request = new(
            method ?? HttpMethod.Post,
            RecoverySandboxRoute.ScopedOutage(tenantRef, dependency, action, correlationId));
        request.Headers.Add("X-Recovery-Controller-Secret", ControllerSecret);
        return await SendSandboxAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<JsonDocument> SendScopeObservationAsync(
        string dependency,
        string tenantRef,
        string correlationId,
        string faultSignalCode,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            $"/recovery/{Uri.EscapeDataString(tenantRef)}/scope-observation/{Uri.EscapeDataString(dependency)}/{Uri.EscapeDataString(correlationId)}" +
            $"?faultSignalCode={Uri.EscapeDataString(faultSignalCode)}");
        request.Headers.Add("X-Recovery-Controller-Secret", ControllerSecret);
        return await SendSandboxAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<JsonDocument> SendSandboxAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await SandboxClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"The closed recovery-sandbox action '{request.Method} {request.RequestUri}' returned " +
                $"{(int)response.StatusCode} ({response.StatusCode}): {content}");
        }

        return JsonDocument.Parse(content);
    }
}
