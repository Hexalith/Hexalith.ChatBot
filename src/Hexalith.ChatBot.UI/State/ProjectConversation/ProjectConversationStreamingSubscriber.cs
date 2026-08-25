using Microsoft.AspNetCore.SignalR.Client;

namespace Hexalith.ChatBot.UI.State.ProjectConversation;

/// <summary>
/// Owns the Story 10.6b client subscription: a SignalR <see cref="HubConnection"/> to the ChatBot-owned
/// project-conversation change hub. On a tenant-matched change signal it invokes a callback so the workspace can
/// dispatch a typed re-query. Advisory and fail-open — a failed connect/join/leave never throws to the surface; the
/// typed read continues to drive rendering. Owned per workspace component instance and disposed with it.
/// </summary>
internal sealed class ProjectConversationStreamingSubscriber(ChatBotHubEndpoint endpoint) : IAsyncDisposable
{
    /// <summary>The kebab projection type the signal pertains to (informational; the hub is tenant-grouped).</summary>
    public const string ProjectConversationProjectionType = "project-conversation";

    private const string RelativeHubPath = "hubs/chatbot/project-conversation-changes";
    private const string ProjectConversationChangedClientMethod = "ProjectConversationChanged";
    private const string JoinTenantHubMethod = "JoinTenant";

    private readonly ChatBotHubEndpoint _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

    private HubConnection? _connection;
    private Action? _onChanged;
    private Action? _onReconnected;
    private string? _subscribedTenant;

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

        if (_connection is not null && string.Equals(_subscribedTenant, tenantId, StringComparison.Ordinal))
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

        HubConnection connection;
        try
        {
            connection = new HubConnectionBuilder()
                .WithUrl(
                    HubUri(_endpoint.BaseAddress),
                    options =>
                    {
                        // JWT-on deployments forward the caller's bearer token so the server binds the tenant claim and the
                        // hub fails closed cross-tenant; null in the no-JWT dev/test posture (anonymous joins allowed there).
                        if (_endpoint.AccessTokenProvider is { } accessTokenProvider)
                        {
                            options.AccessTokenProvider = accessTokenProvider;
                        }
                    })
                .WithAutomaticReconnect()
                .Build();
        }
        catch (Exception)
        {
            // Uri construction / builder failure must not escape into OnAfterRenderAsync and break the surface.
            _subscribedTenant = null;
            return;
        }

        _connection = connection;
        _subscribedTenant = tenantId;

        _ = connection.On<string>(ProjectConversationChangedClientMethod, signalTenantId =>
        {
            if (string.Equals(signalTenantId, _subscribedTenant, StringComparison.Ordinal))
            {
                _onChanged?.Invoke();
            }
        });

        // On reconnect, SignalR drops group memberships AND does not replay signals missed during the disconnect
        // window, so rejoin the tenant group and then re-query authoritative server state via the reconnect callback. [AC5]
        connection.Reconnected += async _ =>
        {
            await JoinTenantAsync().ConfigureAwait(false);
            _onReconnected?.Invoke();
        };

        try
        {
            await connection.StartAsync().ConfigureAwait(false);
            await JoinTenantAsync().ConfigureAwait(false);
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
            await DisposeConnectionAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await DisposeConnectionAsync().ConfigureAwait(false);

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

    private async Task JoinTenantAsync()
    {
        if (_connection is { } connection && !string.IsNullOrWhiteSpace(_subscribedTenant))
        {
            try
            {
                await connection.InvokeAsync(JoinTenantHubMethod, _subscribedTenant).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Best-effort group join; a cross-tenant/unauthorized rejection leaves the surface on typed reads.
            }
        }
    }

    private async Task DisposeConnectionAsync()
    {
        _onChanged = null;
        _onReconnected = null;
        _subscribedTenant = null;
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
