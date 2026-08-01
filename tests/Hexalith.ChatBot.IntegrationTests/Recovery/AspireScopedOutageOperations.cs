using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Hexalith.ChatBot.Contracts.Identities;
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
    private static readonly TimeSpan AbsenceConfirmationWindow = TimeSpan.FromSeconds(30);
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
            if (restored.RootElement.GetProperty("faulted").GetBoolean())
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
            if (restored.RootElement.GetProperty("faulted").GetBoolean())
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
            bool available = await IsIdentityAvailableAsync(cancellationToken).ConfigureAwait(false);
            if ((!result.Success || result.Canceled) && available)
            {
                throw new InvalidOperationException("The allowlisted identity stop command did not reach the dependency boundary.");
            }

            return;
        }

        if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            using JsonDocument _ = await SendSubscriptionAsync(tenantRef, "fault", includeBearer: false, cancellationToken).ConfigureAwait(false);
            return;
        }

        using JsonDocument __ = await SendScopedAsync(dependency, tenantRef, "fault", correlationId: null, cancellationToken).ConfigureAwait(false);
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
                cancellationToken).ConfigureAwait(false);
            observedAtUtc = monitored.RootElement
                .GetProperty("dependencyFailureObservedAtUtc").GetDateTimeOffset().ToUniversalTime();
            recordedAtUtc = monitored.RootElement
                .GetProperty("scopeRecordedAtUtc").GetDateTimeOffset().ToUniversalTime();
            observedScope = monitored.RootElement.GetProperty("observedScope").GetString()
                ?? throw new InvalidOperationException("Identity monitoring returned no observed scope.");
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
                cancellationToken).ConfigureAwait(false);
            observedAtUtc = monitored.RootElement
                .GetProperty("dependencyFailureObservedAtUtc").GetDateTimeOffset().ToUniversalTime();
            recordedAtUtc = monitored.RootElement
                .GetProperty("scopeRecordedAtUtc").GetDateTimeOffset().ToUniversalTime();
            observedScope = monitored.RootElement.GetProperty("observedScope").GetString()
                ?? throw new InvalidOperationException("Graph monitoring returned no observed scope.");
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
            observedScope = root.GetProperty("observedScope").GetString() ?? ScopedOutageScopes.Tenant;
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
    public async ValueTask RestoreAsync(string dependency, string tenantRef, CancellationToken cancellationToken)
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
            return;
        }

        if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            using JsonDocument _ = await SendSubscriptionAsync(tenantRef, "restore", includeBearer: false, cancellationToken).ConfigureAwait(false);
            return;
        }

        using JsonDocument __ = await SendScopedAsync(dependency, tenantRef, "restore", correlationId: null, cancellationToken).ConfigureAwait(false);
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
            duplicate = !_identityFaultLeftStateUnchanged || !affectedUnchanged || !controlUnchanged;
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
            bool duplicateAggregateCommitted = !await _durableState.RemainsAbsentAsync(
                RecoveryValidationTopology.StorageTenantRef,
                _graphDuplicateProbeIntakeRef,
                AbsenceConfirmationWindow,
                cancellationToken).ConfigureAwait(false);
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
                duplicateAggregateCommitted ||
                !recoveredAggregateStillCommitted;
            silentLoss = !recovered;
            leakage = !_graphFaultLeftStateUnchanged ||
                !Equals(affected, _graphAffectedSentinel) ||
                !Equals(control, _graphControlSentinel);
        }
        else
        {
            using JsonDocument first = await SendScopedAsync(dependency, tenantRef, "process", correlationId, cancellationToken).ConfigureAwait(false);
            using JsonDocument second = await SendScopedAsync(dependency, tenantRef, "process", correlationId, cancellationToken).ConfigureAwait(false);
            JsonElement root = second.RootElement;
            recovered = !root.GetProperty("faulted").GetBoolean() &&
                string.Equals(root.GetProperty("outcome").GetString(), "completed", StringComparison.Ordinal);
            duplicate = root.GetProperty("duplicateSideEffectDetected").GetBoolean() ||
                root.GetProperty("effectCount").GetInt32() != 1;
            silentLoss = root.GetProperty("silentDataLossDetected").GetBoolean();
            leakage = root.GetProperty("crossTenantLeakageDetected").GetBoolean();
        }

        return new ScopedOutageRecoveryEndState(recovered, leakage, silentLoss, duplicate);
    }

    /// <inheritdoc />
    public async ValueTask CleanupAsync(string dependency, string tenantRef, CancellationToken cancellationToken)
    {
        // Erase before restoring/verifying. Sentinel rows are seeded into the shared control tenant `tenant-beta`, so
        // a throw from RestoreAsync or a non-2xx status used to strand them permanently and poison later runs.
        if (string.Equals(dependency, ScopedOutageDependencies.Identity, StringComparison.Ordinal))
        {
            await EraseSentinelsAsync(
                [_identityAffectedSentinel, _identityControlSentinel],
                cancellationToken).ConfigureAwait(false);

            if (!await TryAcquireRecoveryTokenOnceAsync(cancellationToken).ConfigureAwait(false))
            {
                await RestoreAsync(dependency, tenantRef, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            await EraseSentinelsAsync([_graphAffectedSentinel, _graphControlSentinel], cancellationToken)
                .ConfigureAwait(false);
            foreach (string? cleanupIntake in new[] { _graphRecoveredIntakeRef, _graphDuplicateProbeIntakeRef })
            {
                if (!string.IsNullOrWhiteSpace(cleanupIntake))
                {
                    await EraseIntakeReadModelsAsync(
                        RecoveryValidationTopology.StorageTenantRef,
                        cleanupIntake,
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }

        await RestoreAsync(dependency, tenantRef, cancellationToken).ConfigureAwait(false);
        using JsonDocument status = string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal)
            ? await SendSubscriptionAsync(tenantRef, "status", includeBearer: false, cancellationToken, HttpMethod.Get).ConfigureAwait(false)
            : await SendScopedAsync(dependency, tenantRef, "status", correlationId: null, cancellationToken, HttpMethod.Get).ConfigureAwait(false);
        if (status.RootElement.GetProperty("faulted").GetBoolean())
        {
            throw new InvalidOperationException("The dependency remained faulted after cleanup.");
        }
    }

    private async Task EraseSentinelsAsync(
        IEnumerable<ProjectConversationSourceEmailView?> sentinels,
        CancellationToken cancellationToken)
    {
        foreach (ProjectConversationSourceEmailView? sentinel in sentinels)
        {
            if (sentinel is not null &&
                !string.IsNullOrWhiteSpace(sentinel.TenantId) &&
                !string.IsNullOrWhiteSpace(sentinel.IntakeId))
            {
                await EraseIntakeReadModelsAsync(sentinel.TenantId, sentinel.IntakeId, cancellationToken)
                    .ConfigureAwait(false);
            }
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
        ResourceCommandService commands = _application.Services.GetRequiredService<ResourceCommandService>();
        return await commands.ExecuteCommandAsync(_securityResource, command, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsIdentityAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _securityClient.GetAsync("/realms/hexalith", cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
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
            string body = $$"""
                {"commandId":"{{commandRef}}","commandType":"RecordGovernedNote","command":{"noteId":"{{noteRef}}"},"origin":"ui","requestSchemaVersion":"v1"}
                """;
            using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", controlAccessToken);
            request.Headers.Add("X-Correlation-Id", ChatBotCorrelationId.New().Value);
            request.Headers.Add("X-Hexalith-Task-Id", operationRef);
            using HttpResponseMessage response = await _chatBotClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return response.StatusCode == HttpStatusCode.Accepted;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
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
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
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
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            $"/recovery/{Uri.EscapeDataString(tenantRef)}/scope-observation/{Uri.EscapeDataString(dependency)}/{Uri.EscapeDataString(correlationId)}");
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
