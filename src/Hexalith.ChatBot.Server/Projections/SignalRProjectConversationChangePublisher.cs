using Microsoft.AspNetCore.SignalR;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Broadcasts the advisory "project-conversation changed" signal over the ChatBot-owned
/// <see cref="ChatBotProjectConversationHub"/> to the tenant group (Story 10.6b transport). Metadata-only (tenant id
/// only); the UI re-queries authoritative server state on receipt.
/// </summary>
internal sealed class SignalRProjectConversationChangePublisher(IHubContext<ChatBotProjectConversationHub> hubContext)
    : IProjectConversationChangePublisher
{
    private readonly IHubContext<ChatBotProjectConversationHub> _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));

    /// <inheritdoc/>
    public async Task PublishProjectConversationChangedAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        // Advisory + fail-open: a broadcast failure (no clients, transport hiccup) must never break projection
        // application. Clients still converge via their typed re-query / fallback.
        try
        {
            await _hubContext.Clients
                .Group(ChatBotProjectConversationHub.GroupFor(tenantId))
                .SendAsync(ChatBotProjectConversationHub.ProjectConversationChangedClientMethod, tenantId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Best-effort advisory signal; swallow.
        }
    }
}
