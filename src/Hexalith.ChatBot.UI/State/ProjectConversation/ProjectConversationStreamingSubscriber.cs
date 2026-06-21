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

    private const string HubPath = "/hubs/chatbot/project-conversation-changes";
    private const string ProjectConversationChangedClientMethod = "ProjectConversationChanged";
    private const string JoinTenantHubMethod = "JoinTenant";

    private readonly ChatBotHubEndpoint _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

    private HubConnection? _connection;
    private Action? _onChanged;
    private string? _subscribedTenant;

    /// <summary>
    /// Ensures a hub connection is established and joined to <paramref name="tenantId"/>'s change group. Re-connects if
    /// the tenant changed; no-ops if already subscribed for the same tenant or the tenant is blank.
    /// </summary>
    /// <param name="tenantId">The kebab tenant id whose change group to join.</param>
    /// <param name="onProjectConversationChanged">Invoked when a matching change signal is observed.</param>
    public async Task EnsureSubscribedAsync(string? tenantId, Action onProjectConversationChanged)
    {
        ArgumentNullException.ThrowIfNull(onProjectConversationChanged);
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        if (_connection is not null && string.Equals(_subscribedTenant, tenantId, StringComparison.Ordinal))
        {
            return;
        }

        await DisposeConnectionAsync().ConfigureAwait(false);

        _onChanged = onProjectConversationChanged;
        _subscribedTenant = tenantId;

        HubConnection connection = new HubConnectionBuilder()
            .WithUrl(new Uri(_endpoint.BaseAddress, HubPath))
            .WithAutomaticReconnect()
            .Build();
        _connection = connection;

        _ = connection.On<string>(ProjectConversationChangedClientMethod, signalTenantId =>
        {
            if (string.Equals(signalTenantId, _subscribedTenant, StringComparison.Ordinal))
            {
                _onChanged?.Invoke();
            }
        });

        // On reconnect, SignalR drops group memberships, so rejoin the tenant group before relying on signals again.
        connection.Reconnected += async _ => await JoinTenantAsync().ConfigureAwait(false);

        try
        {
            await connection.StartAsync().ConfigureAwait(false);
            await JoinTenantAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Advisory transport: a failed connect/join (hub unmapped / unauthorized) must not break the surface; the
            // typed read continues to drive rendering and the next user action re-queries.
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await DisposeConnectionAsync().ConfigureAwait(false);

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
