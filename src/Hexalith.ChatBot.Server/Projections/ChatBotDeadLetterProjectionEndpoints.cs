using System.Text.Json;
using System.Text.Json.Serialization;

using Dapr;

using Hexalith.ChatBot.Server.Audit;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Maps the DAPR subscriber that DRAINS the projection dead-letter topic. The seven ChatBot projection
/// subscriptions route poison messages here once DAPR exhausts redelivery; without a subscriber they accumulate
/// silently in Redis with nothing observing or alerting on them.
/// </summary>
/// <remarks>
/// <para>
/// This subscription deliberately declares NO <c>DeadLetterTopic</c> of its own. A dead-letter subscription that
/// itself dead-letters on failure can loop a poison message back onto its own queue; this endpoint drains instead —
/// it logs at Error and always answers 200 so DAPR acknowledges the delivery.
/// </para>
/// <para>
/// Logging is METADATA-ONLY (Epic 1 redaction floor): pub/sub component, dead-letter topic, and the
/// EventStore-stamped message/correlation identity. The message body is never logged, and structurally cannot be —
/// <see cref="DeadLetteredChatBotEvent"/> binds only the two identity fields, so the rest of the envelope
/// (including the persisted event payload) is discarded by the JSON reader before the handler runs.
/// </para>
/// </remarks>
internal static partial class ChatBotDeadLetterProjectionEndpoints
{
    /// <summary>The route the dead-letter subscription delivers drained poison messages to.</summary>
    public const string DeadLetterDrainRoute = "/chatbot/events/dead-letter";

    private const string DeadLetterObservedLog =
        "ChatBot projection message was dead-lettered. PubSub={PubSubName} DeadLetterTopic={DeadLetterTopic} "
        + "MessageId={MessageId} CorrelationId={CorrelationId}";

    /// <summary>Maps the dead-letter drain subscriber and its DAPR topic subscription.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pubSubName">The DAPR pub/sub component name (e.g. <c>chatbot-pubsub</c>).</param>
    /// <param name="deadLetterTopic">The dead-letter topic the projection subscriptions route poison messages to.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapChatBotDeadLetterProjectionEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pubSubName,
        string deadLetterTopic)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pubSubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(deadLetterTopic);

        _ = endpoints
            .MapPost(
                DeadLetterDrainRoute,
                async (
                    HttpRequest request,
                    ILoggerFactory loggerFactory) =>
                {
                    // The body is read DEFENSIVELY rather than bound as a required complex parameter. A dead-lettered
                    // message is by definition one the projection path already failed on, so it may be empty, not
                    // JSON, or not a CloudEvent at all. Minimal-API binding rejects those with 400 BEFORE the handler
                    // runs — a non-2xx, which is exactly the infinite-redelivery outcome this drain exists to end.
                    DeadLetteredChatBotEvent? deadLettered = null;
                    try
                    {
                        deadLettered = await request
                            .ReadFromJsonAsync<DeadLetteredChatBotEvent>(request.HttpContext.RequestAborted)
                            .ConfigureAwait(false);
                    }
                    catch (JsonException)
                    {
                        // Unparseable body: still drained, still metadata-only. The exception is deliberately NOT
                        // logged — its message can quote the offending payload, which would leak the body the Epic 1
                        // redaction floor keeps out of logs.
                    }
                    catch (InvalidOperationException)
                    {
                        // Unsupported or absent content type. Same posture as above.
                    }

                    DeadLetterObserved(
                        loggerFactory.CreateLogger(typeof(ChatBotDeadLetterProjectionEndpoints)),
                        pubSubName,
                        deadLetterTopic,
                        SafeIdentity(deadLettered?.MessageId),
                        SafeIdentity(deadLettered?.CorrelationId));

                    // Always 200: a non-2xx here would make DAPR redeliver the poison message forever.
                    return Results.Ok();
                })
            .WithTopic(new TopicOptions
            {
                PubsubName = pubSubName,
                Name = deadLetterTopic,
            });

        return endpoints;
    }

    /// <summary>
    /// Reduces an untrusted wire identifier to a safe, bounded token before it reaches a log sink. A dead-lettered
    /// message is by definition one the projection path rejected, so its fields cannot be assumed well-formed.
    /// </summary>
    private static string SafeIdentity(string? value)
        => AuditMetadata.SafeOptionalToken(value) ?? "unknown";

    [LoggerMessage(EventId = 110301, Level = LogLevel.Error, Message = DeadLetterObservedLog)]
    private static partial void DeadLetterObserved(
        ILogger logger,
        string pubSubName,
        string deadLetterTopic,
        string messageId,
        string correlationId);
}

/// <summary>
/// The metadata-only projection of a dead-lettered ChatBot event envelope. Only the EventStore-stamped identity
/// fields are bound; every other member of the envelope — including the persisted event payload — is discarded by
/// the JSON reader, so the message body cannot reach a log sink through this path.
/// </summary>
/// <param name="MessageId">The originating message ULID stamped by EventStore, when present.</param>
/// <param name="CorrelationId">The correlation id carried through the spine, when present.</param>
internal sealed record DeadLetteredChatBotEvent(
    [property: JsonPropertyName("messageId")] string? MessageId,
    [property: JsonPropertyName("correlationId")] string? CorrelationId);
