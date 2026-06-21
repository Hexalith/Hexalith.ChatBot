using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.ChatBot.UI.State.ProjectConversation;

/// <summary>
/// Owns the Story 10.6b reuse-transport client subscription: joins the tenant-scoped project-conversation
/// projection-changed SignalR group (EventStore <c>ProjectionChangedHub</c>, signal-only) and invokes a callback when a
/// change is observed, so the workspace can dispatch a typed re-query. Advisory and fail-open — a failed join or leave
/// never throws to the surface; the typed read continues to drive rendering. Owned per workspace component instance.
/// </summary>
internal sealed class ProjectConversationStreamingSubscriber(
    IProjectionSubscription subscription,
    IProjectionChangeNotifierWithTenant notifier) : IAsyncDisposable
{
    /// <summary>The kebab projection type / SignalR group root; must match the server publisher.</summary>
    public const string ProjectConversationProjectionType = "project-conversation";

    private readonly IProjectionSubscription _subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
    private readonly IProjectionChangeNotifierWithTenant _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));

    private Action<string, string>? _handler;
    private Action? _onChanged;
    private string? _subscribedTenant;
    private string? _subscribedKey;

    /// <summary>
    /// Ensures the surface is subscribed to the project-conversation projection-changed group for <paramref name="tenantId"/>.
    /// Re-subscribes if the tenant changed; no-ops if already subscribed for the same tenant or the tenant is blank.
    /// </summary>
    /// <param name="tenantId">The kebab tenant id whose project-conversation group to join.</param>
    /// <param name="onProjectConversationChanged">Invoked when a matching change signal is observed.</param>
    public async Task EnsureSubscribedAsync(string? tenantId, Action onProjectConversationChanged)
    {
        ArgumentNullException.ThrowIfNull(onProjectConversationChanged);
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        string key = $"{ProjectConversationProjectionType}:{tenantId}";
        if (string.Equals(_subscribedKey, key, StringComparison.Ordinal))
        {
            return;
        }

        await UnsubscribeAsync().ConfigureAwait(false);

        _onChanged = onProjectConversationChanged;
        _handler = OnProjectionChangedForTenant;
        _notifier.ProjectionChangedForTenant += _handler;
        _subscribedTenant = tenantId;
        _subscribedKey = key;

        try
        {
            await _subscription.SubscribeAsync(ProjectConversationProjectionType, tenantId).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Advisory transport: a failed group join (no live hub / unauthorized) must not break the surface.
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await UnsubscribeAsync().ConfigureAwait(false);

    private void OnProjectionChangedForTenant(string projectionType, string tenantId)
    {
        if (string.Equals(projectionType, ProjectConversationProjectionType, StringComparison.Ordinal) &&
            string.Equals(tenantId, _subscribedTenant, StringComparison.Ordinal))
        {
            _onChanged?.Invoke();
        }
    }

    private async Task UnsubscribeAsync()
    {
        if (_handler is { } handler)
        {
            _notifier.ProjectionChangedForTenant -= handler;
            _handler = null;
        }

        _onChanged = null;
        string? tenant = _subscribedTenant;
        _subscribedTenant = null;
        _subscribedKey = null;

        if (!string.IsNullOrWhiteSpace(tenant))
        {
            try
            {
                await _subscription.UnsubscribeAsync(ProjectConversationProjectionType, tenant).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Best-effort group leave on teardown; the hub also drops the group on disconnect.
            }
        }
    }
}
