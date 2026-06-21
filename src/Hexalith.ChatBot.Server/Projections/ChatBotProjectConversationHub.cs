using System.Security.Claims;

using Microsoft.AspNetCore.SignalR;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// ChatBot-owned, metadata-only SignalR hub (Story 10.6b transport): broadcasts an advisory "project-conversation
/// changed" signal to a tenant group so subscribed UI clients re-query the typed read state. The signal carries ONLY
/// the tenant id — never response text, raw provider chunks, prompts, or any content — and is never authoritative; the
/// client always re-queries authoritative server state. Colocated with the in-process ChatBot read model (unlike the
/// cross-host EventStore relay, which the real topology cannot wire), so it is directly verifiable in one host.
/// </summary>
internal sealed class ChatBotProjectConversationHub : Hub
{
    /// <summary>The hub endpoint path mapped by the ChatBot server host.</summary>
    public const string HubPath = "/hubs/chatbot/project-conversation-changes";

    /// <summary>The client-facing broadcast method name.</summary>
    public const string ProjectConversationChangedClientMethod = "ProjectConversationChanged";

    /// <summary>The SignalR group name for a tenant's project-conversation change signals.</summary>
    public static string GroupFor(string tenantId) => $"project-conversation:{tenantId}";

    /// <summary>
    /// Joins the caller to the tenant's project-conversation change group. Fails closed on a malformed tenant or a
    /// cross-tenant join when the caller is authenticated; when JWT auth is not configured (dev/test, mirroring the
    /// rest of the self-hosted topology) the unauthenticated caller is allowed.
    /// </summary>
    public async Task JoinTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || tenantId.Contains(':', StringComparison.Ordinal))
        {
            throw new HubException("invalid-tenant");
        }

        if (!IsTenantAuthorized(tenantId))
        {
            throw new HubException("tenant-forbidden");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(tenantId)).ConfigureAwait(false);
    }

    /// <summary>Leaves the caller from the tenant's project-conversation change group.</summary>
    public Task LeaveTenant(string tenantId)
        => string.IsNullOrWhiteSpace(tenantId)
            ? Task.CompletedTask
            : Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupFor(tenantId));

    private bool IsTenantAuthorized(string tenantId)
    {
        ClaimsPrincipal? user = Context.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return true;
        }

        return user.Claims.Any(claim =>
            claim.Type is "eventstore:tenant" or "tenant" &&
            string.Equals(claim.Value, tenantId, StringComparison.Ordinal));
    }
}
