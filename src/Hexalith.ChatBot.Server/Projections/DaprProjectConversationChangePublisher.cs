using Dapr.Client;

using Hexalith.EventStore.Client.Conventions;
using Hexalith.EventStore.Contracts.Projections;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Publishes the EventStore projection-changed pub/sub notification (Story 10.6b reuse transport) so the running
/// EventStore host relays it on its <c>ProjectionChangedHub</c> to subscribed ChatBot UI clients, which then
/// re-query the typed read state. ChatBot owns no SignalR hub; it reuses the platform projection-nudge channel.
/// </summary>
/// <remarks>
/// The pub/sub component name mirrors EventStore's <c>ProjectionChangeNotifierOptions.DefaultPubSubName</c> (the
/// platform enforces it to be <c>"pubsub"</c>); it lives in the EventStore host assembly that ChatBot.Server does
/// not reference, so it is duplicated here as a constant. End-to-end delivery requires the live EventStore host +
/// DAPR pub/sub topology; in environments without it the publish fails open and clients keep using typed re-query.
/// </remarks>
internal sealed class DaprProjectConversationChangePublisher(DaprClient daprClient) : IProjectConversationChangePublisher
{
    /// <summary>The project-conversation projection type / SignalR group root the UI subscribes to.</summary>
    public const string ProjectConversationProjectionType = "project-conversation";

    private const string PubSubName = "pubsub";

    private readonly DaprClient _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));

    /// <inheritdoc/>
    public async Task PublishProjectConversationChangedAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        // Advisory + fail-open (mirrors EventStore's ADR-18.5a broadcast posture): a publish failure — a missing DAPR
        // sidecar, a non-kebab tenant the naming convention rejects, or a transient transport error — must never break
        // projection application, so it is swallowed. The EventStore relay/controller logs on receipt, and clients
        // still converge via their typed re-query / fallback polling.
        try
        {
            string topic = NamingConventionEngine.GetProjectionChangedTopic(ProjectConversationProjectionType, tenantId);
            await _daprClient
                .PublishEventAsync(
                    PubSubName,
                    topic,
                    new ProjectionChangedNotification(ProjectConversationProjectionType, tenantId),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Best-effort advisory signal; swallow.
        }
    }
}
