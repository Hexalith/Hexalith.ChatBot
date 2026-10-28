using Microsoft.AspNetCore.SignalR.Client;

namespace Hexalith.ChatBot.UI.State.ProjectConversation;

/// <summary>
/// Owns the Story 10.6b client subscription: a SignalR <see cref="HubConnection"/> to the ChatBot-owned
/// project-conversation change hub. On a tenant-matched change signal it invokes a callback so the workspace can
/// dispatch a typed re-query. Advisory and fail-open — a failed connect/join/leave never throws to the surface; the
/// typed read continues to drive rendering. Owned per workspace component instance and disposed with it.
/// </summary>
internal sealed class ProjectConversationStreamingSubscriber : IAsyncDisposable
{
    /// <summary>The kebab projection type the signal pertains to (informational; the hub is tenant-grouped).</summary>
    public const string ProjectConversationProjectionType = "project-conversation";

    private const string RelativeHubPath = "hubs/chatbot/project-conversation-changes";
    private const string ProjectConversationChangedClientMethod = "ProjectConversationChanged";
    private const string JoinTenantHubMethod = "JoinTenant";

    private readonly ChatBotHubEndpoint _endpoint;
    private readonly IProjectConversationHubConnectionFactory _connectionFactory;
    private readonly TimeSpan _initialRecoveryDelay;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _subscriptionChanges = new(1, 1);

    private IProjectConversationHubConnection? _connection;
    private Func<Task>? _onChanged;
    private Func<Task>? _onReconnected;
    private string? _subscribedTenant;
    private int _recoveryActive;
    private volatile bool _disposed;

    public ProjectConversationStreamingSubscriber(
        ChatBotHubEndpoint endpoint,
        IProjectConversationHubConnectionFactory? connectionFactory = null,
        TimeSpan? initialRecoveryDelay = null)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _connectionFactory = connectionFactory ?? SignalRProjectConversationHubConnectionFactory.Instance;
        _initialRecoveryDelay = initialRecoveryDelay ?? TimeSpan.FromMilliseconds(250);
    }

    /// <summary>
    /// Ensures a hub connection is established and joined to <paramref name="tenantId"/>'s change group. Re-connects if
    /// the tenant changed; no-ops if already subscribed for the same tenant or the tenant is blank.
    /// </summary>
    /// <param name="tenantId">The kebab tenant id whose change group to join.</param>
    /// <param name="onProjectConversationChanged">Invoked when a matching change signal is observed.</param>
    /// <param name="onReconnected">
    /// Invoked after the connection transparently reconnects and rejoins the tenant group, so the caller can re-query
    /// authoritative server state — SignalR does not replay signals missed during the disconnect window. [AC5]
    /// </param>
    public async Task EnsureSubscribedAsync(string? tenantId, Func<Task> onProjectConversationChanged, Func<Task> onReconnected)
    {
        await _subscriptionChanges.WaitAsync().ConfigureAwait(false);
        try
        {
            await EnsureSubscribedCoreAsync(tenantId, onProjectConversationChanged, onReconnected).ConfigureAwait(false);
        }
        finally
        {
            _ = _subscriptionChanges.Release();
        }
    }

    private async Task EnsureSubscribedCoreAsync(string? tenantId, Func<Task> onProjectConversationChanged, Func<Task> onReconnected)
    {
        ArgumentNullException.ThrowIfNull(onProjectConversationChanged);
        ArgumentNullException.ThrowIfNull(onReconnected);
        if (_disposed || string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        if (_connection is not null &&
            string.Equals(_subscribedTenant, tenantId, StringComparison.Ordinal) &&
            _connection.State is not HubConnectionState.Disconnected)
        {
            // Same tenant, live connection: keep it, but ALWAYS refresh the callbacks. They close over the caller's
            // current project id, and returning without rebinding them left the previous project's closures in place
            // after an in-tenant project switch -- change signals were then discarded as cross-project and a reconnect
            // re-queried the project the user had navigated away from.
            _onChanged = onProjectConversationChanged;
            _onReconnected = onReconnected;
            return;
        }

        await DisposeConnectionAsync().ConfigureAwait(false);

        if (_disposed)
        {
            return;
        }

        _onChanged = onProjectConversationChanged;
        _onReconnected = onReconnected;
        _subscribedTenant = tenantId;

        IProjectConversationHubConnection connection;
        try
        {
            connection = _connectionFactory.Create(HubUri(_endpoint.BaseAddress), _endpoint.AccessTokenProvider);
        }
        catch (Exception)
        {
            // Uri construction / builder failure must not escape into OnAfterRenderAsync and break the surface.
            ScheduleRecovery();
            return;
        }

        _connection = connection;
        _subscribedTenant = tenantId;

        if (_disposed)
        {
            await DisposeConnectionAsync(expectedConnection: connection).ConfigureAwait(false);
            return;
        }

        try
        {
            connection.RegisterChanged(ProjectConversationChangedClientMethod, async signalTenantId =>
            {
                if (IsCurrentConnection(connection) &&
                    string.Equals(signalTenantId, _subscribedTenant, StringComparison.Ordinal))
                {
                    await NotifyAsync(_onChanged).ConfigureAwait(false);
                }
            });

            // On reconnect, SignalR drops group memberships AND does not replay signals missed during the disconnect
            // window, so rejoin the tenant group and then re-query authoritative server state via the reconnect callback. [AC5]
            connection.Reconnected += async () =>
            {
                if (!IsCurrentConnection(connection))
                {
                    return;
                }

                bool joined = await JoinTenantAsync(connection).ConfigureAwait(false);
                if (!IsCurrentConnection(connection))
                {
                    return;
                }

                if (joined)
                {
                    await NotifyAsync(_onReconnected).ConfigureAwait(false);
                }
                else
                {
                    await DisposeConnectionAsync(clearSubscription: false, expectedConnection: connection).ConfigureAwait(false);
                    ScheduleRecovery();
                }
            };
            connection.Closed += () =>
            {
                if (ReferenceEquals(_connection, connection))
                {
                    ScheduleRecovery();
                }

                return Task.CompletedTask;
            };

            await connection.StartAsync().ConfigureAwait(false);
            if (!IsCurrentConnection(connection))
            {
                return;
            }

            if (!await JoinTenantAsync(connection).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The project-conversation hub tenant group could not be joined.");
            }
        }
        catch (Exception)
        {
            // Advisory transport: a failed connect/join (hub unmapped / unauthorized) must not break the surface; the
            // typed read continues to drive rendering and the next user action re-queries.
            //
            // Tear the dead connection down rather than leaving it assigned. WithAutomaticReconnect does not retry a
            // failed INITIAL start, so keeping _connection/_subscribedTenant set made every later call short-circuit on
            // the "already subscribed" branch above and streaming stayed off for the rest of the session after a single
            // transient blip at page load.
            await DisposeConnectionAsync(clearSubscription: false, expectedConnection: connection).ConfigureAwait(false);
            ScheduleRecovery();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _lifetime.CancelAsync().ConfigureAwait(false);
        await DisposeConnectionAsync().ConfigureAwait(false);
        _lifetime.Dispose();
    }

    // Resolve the hub path against the base address WITHOUT discarding a path prefix. An absolute "/hubs/..." path
    // replaces the whole base path, so a deployment served under e.g. https://host/chatbot/ dialled https://host/hubs/...
    // and 404'd silently.
    private static Uri HubUri(Uri baseAddress)
    {
        string basePath = baseAddress.AbsoluteUri;
        if (!basePath.EndsWith('/'))
        {
            basePath += "/";
        }

        return new Uri(new Uri(basePath, UriKind.Absolute), RelativeHubPath);
    }

    private async Task<bool> JoinTenantAsync(IProjectConversationHubConnection connection)
    {
        if (IsCurrentConnection(connection) && !string.IsNullOrWhiteSpace(_subscribedTenant))
        {
            try
            {
                await connection.InvokeAsync(JoinTenantHubMethod, _subscribedTenant).ConfigureAwait(false);
                return IsCurrentConnection(connection);
            }
            catch (Exception)
            {
                // Best-effort group join; a cross-tenant/unauthorized rejection leaves the surface on typed reads.
            }
        }

        return false;
    }

    private bool IsCurrentConnection(IProjectConversationHubConnection connection)
        => !_disposed && ReferenceEquals(_connection, connection);

    private async Task NotifyAsync(Func<Task>? callback)
    {
        if (_disposed || callback is null)
        {
            return;
        }

        try
        {
            await callback().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Advisory notifications must not fault SignalR event delivery or the UI circuit during teardown.
        }
    }

    private void ScheduleRecovery()
    {
        if (_disposed || string.IsNullOrWhiteSpace(_subscribedTenant) ||
            Interlocked.CompareExchange(ref _recoveryActive, 1, 0) != 0)
        {
            return;
        }

        _ = RecoverUntilConnectedAsync();
    }

    private async Task RecoverUntilConnectedAsync()
    {
        try
        {
            TimeSpan delay = _initialRecoveryDelay;
            while (!_lifetime.IsCancellationRequested &&
                !string.IsNullOrWhiteSpace(_subscribedTenant))
            {
                try
                {
                    await Task.Delay(delay, _lifetime.Token).ConfigureAwait(false);
                    if (await RecoverCurrentSubscriptionAsync().ConfigureAwait(false))
                    {
                        await NotifyAsync(_onReconnected).ConfigureAwait(false);
                        return;
                    }
                }
                catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception)
                {
                    // Advisory transport recovery remains isolated from the UI circuit.
                }

                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 5000));
            }
        }
        finally
        {
            _ = Interlocked.Exchange(ref _recoveryActive, 0);
            if (!_disposed && _connection?.State is not HubConnectionState.Connected &&
                !string.IsNullOrWhiteSpace(_subscribedTenant))
            {
                ScheduleRecovery();
            }
        }
    }

    private async Task<bool> RecoverCurrentSubscriptionAsync()
    {
        await _subscriptionChanges.WaitAsync(_lifetime.Token).ConfigureAwait(false);
        try
        {
            // Read only after admission: a newer render may have changed tenant/project while recovery was queued.
            if (_disposed || _subscribedTenant is not { } tenant ||
                _onChanged is not { } onChanged || _onReconnected is not { } onReconnected)
            {
                return false;
            }

            await EnsureSubscribedCoreAsync(tenant, onChanged, onReconnected).ConfigureAwait(false);
            return _connection?.State is HubConnectionState.Connected;
        }
        finally
        {
            _ = _subscriptionChanges.Release();
        }
    }

    private async Task DisposeConnectionAsync(bool clearSubscription = true, IProjectConversationHubConnection? expectedConnection = null)
    {
        IProjectConversationHubConnection? connection = expectedConnection is null
            ? Interlocked.Exchange(ref _connection, null)
            : Interlocked.CompareExchange(ref _connection, null, expectedConnection);
        if (expectedConnection is not null && !ReferenceEquals(connection, expectedConnection))
        {
            return;
        }

        if (clearSubscription)
        {
            _onChanged = null;
            _onReconnected = null;
            _subscribedTenant = null;
        }
        if (connection is not null)
        {
            try
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Teardown must not throw.
            }
        }
    }
}
