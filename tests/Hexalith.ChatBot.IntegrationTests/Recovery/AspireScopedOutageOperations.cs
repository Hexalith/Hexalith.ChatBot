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
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Projections;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Closed Aspire/provider-sandbox operations for the six canonical scoped-outage dependencies.</summary>
internal sealed class AspireScopedOutageOperations : IScopedOutageSandboxOperations
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// How long an absence assertion polls before concluding a resource genuinely is not there. Kept equal to the
    /// presence budget so a no-duplicate claim is not systematically easier to satisfy than a materialization claim.
    /// </summary>
    private static readonly TimeSpan AbsenceConfirmationWindow = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How long to wait for a stopped identity provider to stop serving, matching EventStore's stop confirmation.
    /// </summary>
    private static readonly TimeSpan StopConfirmationTimeout = TimeSpan.FromSeconds(60);
    private readonly DistributedApplication _application;
    private readonly IResource _securityResource;
    private readonly HttpClient _chatBotClient;
    private readonly HttpClient _sandboxClient;
    private readonly HttpClient _securityClient;
    private readonly ReadModelProjectConversationProjectionStore _readModels;
    private readonly IReadModelConditionalEraser _readModelEraser;
    private readonly EventStoreDurableStateProbe _durableState;
    private string _controlAccessToken;
    private readonly string _mailboxClientSecret;
    private readonly string _controllerSecret;
    private ProjectConversationSourceEmailView? _graphAffectedSentinel;
    private ProjectConversationSourceEmailView? _graphControlSentinel;
    private string? _graphRecoveredIntakeRef;
    private string? _graphDuplicateProbeIntakeRef;
    private bool _graphFaultLeftStateUnchanged;
    private ProjectConversationSourceEmailView? _identityAffectedSentinel;
    private ProjectConversationSourceEmailView? _identityControlSentinel;
    private bool _identityFaultLeftStateUnchanged;
    private readonly List<string> _controlOperationRefs = [];

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
        _durableState = durableState;
        _chatBotClient = application.CreateHttpClient("chatbot");
        _chatBotClient.Timeout = TimeSpan.FromSeconds(30);
        _sandboxClient = application.CreateHttpClient("recovery-sandbox", "http");
        _sandboxClient.Timeout = TimeSpan.FromSeconds(30);
        _securityClient = application.CreateHttpClient("security", "http");
        _securityClient.Timeout = TimeSpan.FromSeconds(10);
    }

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
            _controlAccessToken = await RecoveryAccessTokenProvider
                .AcquireControlAsync(_application, cancellationToken)
                .ConfigureAwait(false);

            _identityAffectedSentinel = IdentitySentinel(
                RecoveryValidationTopology.StorageTenantRef,
                "recovery-identity-affected-sentinel");
            _identityControlSentinel = IdentitySentinel(
                RecoveryValidationTopology.ControlTenantRef,
                "recovery-identity-control-sentinel");
            await _readModels.UpsertSourceEmailAsync(_identityAffectedSentinel, cancellationToken).ConfigureAwait(false);
            await _readModels.UpsertSourceEmailAsync(_identityControlSentinel, cancellationToken).ConfigureAwait(false);
        }
        else if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            using JsonDocument restored = await SendSubscriptionAsync(tenantRef, "restore", includeBearer: false, cancellationToken).ConfigureAwait(false);
            if (RecoverySandboxRestoreResponse.WasPreviouslyFaulted(restored.RootElement) ||
                RecoverySandboxRestoreResponse.IsCurrentlyFaulted(restored.RootElement))
            {
                throw new InvalidOperationException("The Graph boundary was not clean before injection.");
            }

            _graphAffectedSentinel = GraphSentinel(
                RecoveryValidationTopology.StorageTenantRef,
                "recovery-graph-affected-sentinel");
            _graphControlSentinel = GraphSentinel(
                RecoveryValidationTopology.ControlTenantRef,
                "recovery-graph-control-sentinel");
            await _readModels.UpsertSourceEmailAsync(_graphAffectedSentinel, cancellationToken).ConfigureAwait(false);
            await _readModels.UpsertSourceEmailAsync(_graphControlSentinel, cancellationToken).ConfigureAwait(false);
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

                        await Task.Delay(PollInterval, stopDeadline.Token).ConfigureAwait(false);
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
                    _identityAffectedSentinel!.TenantId,
                    _identityAffectedSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            ProjectConversationSourceEmailView? control = await _readModels
                .GetSourceEmailAsync(
                    _identityControlSentinel!.TenantId,
                    _identityControlSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            _identityFaultLeftStateUnchanged = Equals(affected, _identityAffectedSentinel) &&
                Equals(control, _identityControlSentinel);
            unauthorizedMutationDetected = !_identityFaultLeftStateUnchanged;
        }
        else if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            using JsonDocument process = await SendSubscriptionAsync(tenantRef, "process", includeBearer: true, cancellationToken).ConfigureAwait(false);
            JsonElement root = process.RootElement;
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
                    _graphAffectedSentinel!.TenantId,
                    _graphAffectedSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            ProjectConversationSourceEmailView? control = await _readModels
                .GetSourceEmailAsync(
                    _graphControlSentinel!.TenantId,
                    _graphControlSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            _graphFaultLeftStateUnchanged = Equals(affected, _graphAffectedSentinel) &&
                Equals(control, _graphControlSentinel);
            unauthorizedMutationDetected = !_graphFaultLeftStateUnchanged;
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

        bool controlSucceeded = await IsChatBotControlAvailableAsync(dependency, cancellationToken).ConfigureAwait(false);
        return new ScopedOutageFaultObservation(
            observedAtUtc,
            recordedAtUtc,
            observedScope,
            controlSucceeded,
            unauthorizedMutationDetected);
    }

    /// <inheritdoc />
    public async ValueTask<bool> RestoreAsync(string dependency, string tenantRef, CancellationToken cancellationToken)
    {
        if (string.Equals(dependency, ScopedOutageDependencies.Identity, StringComparison.Ordinal))
        {
            ExecuteCommandResult result = await ExecuteSecurityCommandAsync(KnownResourceCommands.StartCommand, cancellationToken).ConfigureAwait(false);
            if (!result.Success || result.Canceled)
            {
                throw new InvalidOperationException("The allowlisted identity start command did not complete.");
            }

            await _application.ResourceNotifications
                .WaitForResourceHealthyAsync(_securityResource.Name, cancellationToken)
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
                    _identityAffectedSentinel!.TenantId,
                    _identityAffectedSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            ProjectConversationSourceEmailView? control = await _readModels
                .GetSourceEmailAsync(
                    _identityControlSentinel!.TenantId,
                    _identityControlSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            bool affectedUnchanged = Equals(affected, _identityAffectedSentinel);
            bool controlUnchanged = Equals(control, _identityControlSentinel);
            recovered = await TryAcquireRecoveryTokenOnceAsync(cancellationToken).ConfigureAwait(false) && affectedUnchanged;
            leakage = !controlUnchanged;
            silentLoss = affected is null;
            // Identity has no duplicate-commit probe; sentinel drift is leakage/non-recovery, not duplication.
            duplicate = false;
        }
        else if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            using JsonDocument process = await SendSubscriptionAsync(tenantRef, "process", includeBearer: true, cancellationToken).ConfigureAwait(false);
            _graphRecoveredIntakeRef = process.RootElement.GetProperty("intakeId").GetString();
            if (!process.RootElement.GetProperty("submitted").GetBoolean() ||
                string.IsNullOrWhiteSpace(_graphRecoveredIntakeRef))
            {
                throw new InvalidOperationException("The restored Graph path did not return an intake identity.");
            }

            await _durableState.WaitForMailboxIntakeAsync(
                RecoveryValidationTopology.StorageTenantRef,
                _graphRecoveredIntakeRef,
                cancellationToken).ConfigureAwait(false);
            using JsonDocument duplicateProbe = await SendSubscriptionAsync(
                tenantRef,
                "process",
                includeBearer: true,
                cancellationToken).ConfigureAwait(false);
            _graphDuplicateProbeIntakeRef = duplicateProbe.RootElement.GetProperty("intakeId").GetString();
            if (!duplicateProbe.RootElement.GetProperty("submitted").GetBoolean() ||
                string.IsNullOrWhiteSpace(_graphDuplicateProbeIntakeRef))
            {
                throw new InvalidOperationException("The Graph duplicate probe did not reach the Worker path.");
            }

            // Poll for the full absence window rather than reading once after a fixed 2s sleep: a duplicate aggregate
            // that commits a moment later than the sleep was being reported as "no duplicate side effect".
            Task<bool> duplicateAggregateAbsent = _durableState.RemainsAbsentAsync(
                RecoveryValidationTopology.StorageTenantRef,
                _graphDuplicateProbeIntakeRef,
                AbsenceConfirmationWindow,
                cancellationToken);
            Task<bool> controlTenantAggregateAbsent = _durableState.RemainsAbsentAsync(
                RecoveryValidationTopology.ControlTenantRef,
                _graphRecoveredIntakeRef,
                AbsenceConfirmationWindow,
                cancellationToken);
            Task<bool> controlTenantReadModelsAbsent = RemainsIntakeReadModelsAbsentAsync(
                RecoveryValidationTopology.ControlTenantRef,
                _graphRecoveredIntakeRef,
                AbsenceConfirmationWindow,
                cancellationToken);
            bool[] isolationOutcomes = await Task.WhenAll(
                duplicateAggregateAbsent,
                controlTenantAggregateAbsent,
                controlTenantReadModelsAbsent).ConfigureAwait(false);
            bool recoveredAggregateStillCommitted = await _durableState.IsMailboxIntakeCommittedAsync(
                RecoveryValidationTopology.StorageTenantRef,
                _graphRecoveredIntakeRef,
                cancellationToken).ConfigureAwait(false);
            ProjectConversationSourceEmailView? affected = await _readModels
                .GetSourceEmailAsync(
                    _graphAffectedSentinel!.TenantId,
                    _graphAffectedSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            ProjectConversationSourceEmailView? control = await _readModels
                .GetSourceEmailAsync(
                    _graphControlSentinel!.TenantId,
                    _graphControlSentinel.IntakeId,
                    cancellationToken)
                .ConfigureAwait(false);
            recovered = recoveredAggregateStillCommitted;
            duplicate = string.Equals(_graphDuplicateProbeIntakeRef, _graphRecoveredIntakeRef, StringComparison.Ordinal) ||
                !isolationOutcomes[0];
            silentLoss = !recovered;
            leakage = !_graphFaultLeftStateUnchanged ||
                !Equals(affected, _graphAffectedSentinel) ||
                !Equals(control, _graphControlSentinel) ||
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
        foreach (string controlNoteRef in _controlOperationRefs)
        {
            controlIsolationChecks.Add(_durableState.RemainsGovernedNoteAbsentAsync(
                RecoveryValidationTopology.StorageTenantRef,
                controlNoteRef,
                AbsenceConfirmationWindow,
                cancellationToken));
            controlIsolationChecks.Add(RemainsReadModelKeyAbsentAsync(
                GovernedOperationView.KeyFor(RecoveryValidationTopology.StorageTenantRef, controlNoteRef),
                AbsenceConfirmationWindow,
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
        List<(string Tenant, string Intake)> intakeTargets = [];
        if (string.Equals(dependency, ScopedOutageDependencies.Identity, StringComparison.Ordinal))
        {
            AddIntakeTarget(intakeTargets, _identityAffectedSentinel);
            AddIntakeTarget(intakeTargets, _identityControlSentinel);
        }
        else if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            AddIntakeTarget(intakeTargets, _graphAffectedSentinel);
            AddIntakeTarget(intakeTargets, _graphControlSentinel);
            AddIntakeTarget(intakeTargets, RecoveryValidationTopology.StorageTenantRef, _graphRecoveredIntakeRef);
            AddIntakeTarget(intakeTargets, RecoveryValidationTopology.StorageTenantRef, _graphDuplicateProbeIntakeRef);
        }

        try
        {
            foreach ((string cleanupTenant, string cleanupIntake) in intakeTargets)
            {
                await EraseIntakeReadModelsAsync(cleanupTenant, cleanupIntake, cancellationToken).ConfigureAwait(false);
            }

            foreach (string controlNoteRef in _controlOperationRefs)
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
            complete &= await AreIntakeReadModelsAbsentAsync(
                cleanupTenant,
                cleanupIntake,
                cancellationToken).ConfigureAwait(false);
        }

        foreach (string controlNoteRef in _controlOperationRefs)
        {
            (bool present, _) = await _readModelEraser.TryReadEtagAsync(
                ChatBotReadModelStoreNames.StateStoreName,
                GovernedOperationView.KeyFor(RecoveryValidationTopology.ControlTenantRef, controlNoteRef),
                cancellationToken).ConfigureAwait(false);
            complete &= !present;
        }

        if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(_graphDuplicateProbeIntakeRef))
        {
            complete &= await _durableState.RemainsAbsentAsync(
                RecoveryValidationTopology.StorageTenantRef,
                _graphDuplicateProbeIntakeRef,
                AbsenceConfirmationWindow,
                cancellationToken).ConfigureAwait(false);
        }

        if (complete)
        {
            _identityAffectedSentinel = null;
            _identityControlSentinel = null;
            _identityFaultLeftStateUnchanged = false;
            _graphAffectedSentinel = null;
            _graphControlSentinel = null;
            _graphRecoveredIntakeRef = null;
            _graphDuplicateProbeIntakeRef = null;
            _graphFaultLeftStateUnchanged = false;
            _controlOperationRefs.Clear();
        }

        return complete;
    }

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
            targets.Add((tenantId, intakeId));
        }
    }

    /// <summary>Erases every read model an intake materializes, not just its source-email view.</summary>
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
            (bool present, _) = await _readModelEraser.TryReadEtagAsync(
                ChatBotReadModelStoreNames.StateStoreName,
                key,
                cancellationToken).ConfigureAwait(false);
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

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
        while (timer.Elapsed < window);

        (bool finalPresent, _) = await _readModelEraser.TryReadEtagAsync(
            ChatBotReadModelStoreNames.StateStoreName,
            key,
            cancellationToken).ConfigureAwait(false);
        return !finalPresent;
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
        ResourceCommandService commands = _application.Services.GetRequiredService<ResourceCommandService>();
        return await commands.ExecuteCommandAsync(_securityResource, command, cancellationToken).ConfigureAwait(false);
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
        try
        {
            using HttpResponseMessage response = await _securityClient.GetAsync("/realms/hexalith", cancellationToken).ConfigureAwait(false);
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

    private async Task<bool> IsChatBotControlAvailableAsync(string dependency, CancellationToken cancellationToken)
    {
        try
        {
            // During the identity outage the independent control must use the bearer minted at checkpoint; trying
            // to refresh it would test the known-faulted token endpoint instead of ChatBot's cached-key validation.
            // Other scenarios mint at point of use, and RestoreAsync refreshes the retained bearer after Keycloak
            // restarts so no post-recovery operation relies on the pre-restart signing key.
            string controlAccessToken = string.Equals(dependency, ScopedOutageDependencies.Identity, StringComparison.Ordinal)
                ? _controlAccessToken
                : await RecoveryAccessTokenProvider.AcquireControlAsync(_application, cancellationToken).ConfigureAwait(false);
            string noteRef = GovernedNoteId.New().Value;
            string commandRef = ChatBotCommandId.New().Value;
            string operationRef = ChatBotTaskId.New().Value;
            string correlationId = ChatBotCorrelationId.New().Value;
            _controlOperationRefs.Add(noteRef);
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
            using HttpResponseMessage response = await _chatBotClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                return false;
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
            return true;
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

    private async Task WaitForIdentityAsync(CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromMinutes(3))
        {
            if (await TryAcquireRecoveryTokenOnceAsync(cancellationToken).ConfigureAwait(false))
            {
                _controlAccessToken = await RecoveryAccessTokenProvider
                    .AcquireControlAsync(_application, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
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
        while (timer.Elapsed < AbsenceConfirmationWindow)
        {
            using HttpRequestMessage request = new(
                HttpMethod.Get,
                $"/api/v1/governed-operations/{noteRef}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("X-Correlation-Id", correlationId);
            using HttpResponseMessage response = await _chatBotClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(
                    $"The independent control projection returned unexpected status {(int)response.StatusCode}.");
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("The independent control operation did not reach projected state.");
    }

    private async Task<bool> TryAcquireRecoveryTokenOnceAsync(CancellationToken cancellationToken)
    {
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
            using HttpResponseMessage response = await _securityClient
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

    private async ValueTask<JsonDocument> SendSubscriptionAsync(
        string tenantRef,
        string action,
        bool includeBearer,
        CancellationToken cancellationToken,
        HttpMethod? method = null)
    {
        string tenant = Uri.EscapeDataString(tenantRef);
        using HttpRequestMessage request = new(method ?? HttpMethod.Post, $"/recovery/{tenant}/m365-subscription-failure/{action}");
        request.Headers.Add("X-Recovery-Controller-Secret", _controllerSecret);
        request.Headers.Add("X-Recovery-Scenario-Lane", "graph");
        if (string.Equals(action, "process", StringComparison.Ordinal))
        {
            request.Headers.Add(
                RecoveryNotificationIdentity.HeaderName,
                RecoveryNotificationIdentity.RecoveryPhase);
        }
        if (includeBearer)
        {
            string mailboxAccessToken = await RecoveryAccessTokenProvider
                .AcquireMailboxAsync(_application, _mailboxClientSecret, cancellationToken)
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
        request.Headers.Add("X-Recovery-Controller-Secret", _controllerSecret);
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
        request.Headers.Add("X-Recovery-Controller-Secret", _controllerSecret);
        return await SendSandboxAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<JsonDocument> SendSandboxAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _sandboxClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
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
