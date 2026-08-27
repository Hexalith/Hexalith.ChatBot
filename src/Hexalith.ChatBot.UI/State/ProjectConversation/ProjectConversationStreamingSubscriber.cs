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

    private IProjectConversationHubConnection? _connection;
    private Action? _onChanged;
    private Action? _onReconnected;
    private string? _subscribedTenant;
    private int _recoveryActive;
    private bool _disposed;

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
    public async Task EnsureSubscribedAsync(string? tenantId, Action onProjectConversationChanged, Action onReconnected)
    {
        ArgumentNullException.ThrowIfNull(onProjectConversationChanged);
        ArgumentNullException.ThrowIfNull(onReconnected);
        if (string.IsNullOrWhiteSpace(tenantId))
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

        connection.RegisterChanged(ProjectConversationChangedClientMethod, signalTenantId =>
        {
            if (string.Equals(signalTenantId, _subscribedTenant, StringComparison.Ordinal))
            {
                _onChanged?.Invoke();
            }
        });

        // On reconnect, SignalR drops group memberships AND does not replay signals missed during the disconnect
        // window, so rejoin the tenant group and then re-query authoritative server state via the reconnect callback. [AC5]
        connection.Reconnected += async () =>
        {
            if (await JoinTenantAsync().ConfigureAwait(false))
            {
                _onReconnected?.Invoke();
            }
            else
            {
                await DisposeConnectionAsync(clearSubscription: false).ConfigureAwait(false);
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

        try
        {
            await connection.StartAsync().ConfigureAwait(false);
            if (!await JoinTenantAsync().ConfigureAwait(false))
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
            await DisposeConnectionAsync(clearSubscription: false).ConfigureAwait(false);
            ScheduleRecovery();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
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

    private async Task<bool> JoinTenantAsync()
    {
        if (_connection is { } connection && !string.IsNullOrWhiteSpace(_subscribedTenant))
        {
            try
            {
                await connection.InvokeAsync(JoinTenantHubMethod, _subscribedTenant).ConfigureAwait(false);
                return true;
            }
            catch (Exception)
            {
                // Best-effort group join; a cross-tenant/unauthorized rejection leaves the surface on typed reads.
            }
        }

        return false;
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
                _subscribedTenant is { } tenant &&
                _onChanged is { } onChanged &&
                _onReconnected is { } onReconnected)
            {
                try
                {
                    await Task.Delay(delay, _lifetime.Token).ConfigureAwait(false);
                    await EnsureSubscribedAsync(tenant, onChanged, onReconnected).ConfigureAwait(false);
                    if (_connection?.State is HubConnectionState.Connected)
                    {
                        onReconnected();
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

    private async Task DisposeConnectionAsync(bool clearSubscription = true)
    {
        if (clearSubscription)
        {
            _onChanged = null;
            _onReconnected = null;
            _subscribedTenant = null;
        }
        if (_connection is { } connection)
        {
            _connection = null;
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

internal interface IProjectConversationHubConnectionFactory
{
    IProjectConversationHubConnection Create(Uri hubUri, Func<Task<string?>>? accessTokenProvider);
}

internal interface IProjectConversationHubConnection : IAsyncDisposable
{
    HubConnectionState State { get; }

    event Func<Task>? Closed;

    event Func<Task>? Reconnected;

    void RegisterChanged(string methodName, Action<string> callback);

    Task StartAsync();

    Task InvokeAsync(string methodName, string tenantId);
}

internal sealed class SignalRProjectConversationHubConnectionFactory : IProjectConversationHubConnectionFactory
{
    public static SignalRProjectConversationHubConnectionFactory Instance { get; } = new();

    public IProjectConversationHubConnection Create(Uri hubUri, Func<Task<string?>>? accessTokenProvider)
    {
        HubConnection connection = new HubConnectionBuilder()
            .WithUrl(
                hubUri,
                options =>
                {
                    if (accessTokenProvider is not null)
                    {
                        options.AccessTokenProvider = accessTokenProvider;
                    }
                })
            .WithAutomaticReconnect()
            .Build();
        return new SignalRProjectConversationHubConnection(connection);
    }
}

internal sealed class SignalRProjectConversationHubConnection : IProjectConversationHubConnection
{
    private readonly HubConnection _connection;

    public SignalRProjectConversationHubConnection(HubConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _connection.Closed += OnClosed;
        _connection.Reconnected += OnReconnected;
    }

    public HubConnectionState State => _connection.State;

    public event Func<Task>? Closed;

    public event Func<Task>? Reconnected;

    public void RegisterChanged(string methodName, Action<string> callback)
        => _ = _connection.On(methodName, callback);

    public Task StartAsync() => _connection.StartAsync();

    public Task InvokeAsync(string methodName, string tenantId) => _connection.InvokeAsync(methodName, tenantId);

    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    private Task OnClosed(Exception? exception) => Closed?.Invoke() ?? Task.CompletedTask;

    private Task OnReconnected(string? connectionId) => Reconnected?.Invoke() ?? Task.CompletedTask;
}
