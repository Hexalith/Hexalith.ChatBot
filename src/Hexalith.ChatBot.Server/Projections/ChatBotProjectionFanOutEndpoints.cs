using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Dapr;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static Hexalith.ChatBot.Server.Projections.ChatBotProjectionPayload;

using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Maps the single DAPR subscription for the ChatBot events topic and fans each delivery out to every projection
/// family.
/// </summary>
/// <remarks>
/// <para>
/// Dapr.AspNetCore's subscribe handler keeps only the first default route it finds for a pub/sub topic. When seven
/// family routes each subscribed to the same topic without match rules, the sidecar delivered every event to the
/// governed-operation route alone, so the other six read models never received published events.
/// </para>
/// <para>
/// This route carries the one subscription. EventStore envelopes are matched by event type and adapted from their
/// concrete payload bytes. Flattened deliveries still run each family's original binding in order. An unbindable
/// envelope is skipped with a metadata-only warning. Projection faults propagate so DAPR redelivers the event, and
/// the idempotent, version-stamped handlers absorb replays.
/// </para>
/// <para>
/// The association family runs without its own application-channel comparison: this route's sidecar guard already
/// verified the channel, and a second, differently normalized comparison would refuse every event it admits and stop
/// the families after it.
/// </para>
/// </remarks>
internal static partial class ChatBotProjectionFanOutEndpoints
{
    /// <summary>The route the chatbot events subscription delivers every published event to.</summary>
    public const string FanOutRoute = "/chatbot/events";

    private const string AiOutcomeFamily = "ai-outcome";

    private const string EnvelopeBindingSkippedLog =
        "ChatBot projection fan-out skipped a family whose envelope could not bind the delivered event. "
        + "Family={Family} EventTypeName={EventTypeName}";

    /// <summary>
    /// The projection families in their original mapping order. Each returns <see langword="null"/> when the body does
    /// not bind to its envelope.
    /// </summary>
    private static readonly Func<JsonElement, JsonSerializerOptions, HttpContext, CancellationToken, Task<IResult>?>[] _families =
    [
        static (body, options, context, cancellationToken) => TryBind(body, options, context, "governed-operation", out PublishedGovernedOperationEvent? published)
            ? GovernedOperationProjectionEndpoints.ProjectAsync(
                published,
                Service<GovernedOperationProjectionHandler>(context),
                Service<GovernedControlStateProjectionHandler>(context),
                cancellationToken)
            : null,
        static (body, options, context, cancellationToken) => TryBind(body, options, context, "mailbox-intake", out PublishedMailboxIntakeEvent? published)
            ? MailboxIntakeProjectionEndpoints.ProjectAsync(
                published,
                Service<AssociationProjectionHandler>(context),
                cancellationToken)
            : null,
        static (body, options, context, cancellationToken) => TryBind(body, options, context, "association", out PublishedAssociationEvent? published)
            ? AssociationProjectionEndpoints.ProjectAsync(
                published,
                Service<AssociationProjectionHandler>(context),
                cancellationToken)
            : null,
        static (body, options, context, cancellationToken) => TryBind(body, options, context, "participant-resolution", out PublishedParticipantResolutionEvent? published)
            ? ParticipantResolutionProjectionEndpoints.ProjectAsync(
                published,
                Service<ParticipantResolutionProjectionHandler>(context),
                cancellationToken)
            : null,
        static (body, options, context, cancellationToken) => TryBindAiOutcome(body, options, context)
            ? AiOutcomeProjectionEndpoints.ProjectAsync(
                body,
                Service<AiOutcomeProjectionHandler>(context),
                cancellationToken)
            : null,
        static (body, options, context, cancellationToken) => TryBind(body, options, context, "task-intent", out PublishedTaskIntentEvent? published)
            ? TaskIntentProjectionEndpoints.ProjectAsync(
                published,
                Service<TaskIntentProjectionHandler>(context),
                cancellationToken)
            : null,
        static (body, options, context, cancellationToken) => TryBind(body, options, context, "approval", out PublishedAiActionApprovalEvent? published)
            ? ApprovalProjectionEndpoints.ProjectAsync(
                published,
                Service<ApprovalProjectionHandler>(context),
                cancellationToken)
            : null,
    ];

    /// <summary>Maps the sidecar-guarded fan-out route and its DAPR topic subscription.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pubSubName">The DAPR pub/sub component name (e.g. <c>chatbot-pubsub</c>).</param>
    /// <param name="topicName">The topic the EventStore publishes chatbot events to.</param>
    /// <param name="deadLetterTopic">The topic DAPR routes poison messages to once redelivery is exhausted.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapChatBotProjectionFanOutEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pubSubName,
        string topicName,
        string deadLetterTopic)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pubSubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentException.ThrowIfNullOrWhiteSpace(deadLetterTopic);

        _ = endpoints
            .MapPost(FanOutRoute, ProjectAsync)
            .WithTopic(new TopicOptions
            {
                PubsubName = pubSubName,
                Name = topicName,
                DeadLetterTopic = deadLetterTopic,
            })
            .RequireEventStoreSidecarChannel();

        return endpoints;
    }

    /// <summary>Runs every projection family for one published event.</summary>
    /// <param name="context">The delivery request context.</param>
    /// <param name="jsonOptions">The host's HTTP JSON options, used to bind each family's envelope.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// 400 when the body is not a JSON object, the first non-200 family result, or an empty 200 result when every
    /// family succeeded.
    /// </returns>
    public static async Task<IResult> ProjectAsync(
        HttpContext context,
        IOptions<HttpJsonOptions> jsonOptions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(jsonOptions);

        JsonDocument document;
        try
        {
            document = await JsonDocument
                .ParseAsync(context.Request.Body, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return Results.BadRequest();
        }

        using (document)
        {
            JsonElement body = document.RootElement;
            if (body.ValueKind != JsonValueKind.Object)
            {
                return Results.BadRequest();
            }

            JsonSerializerOptions options = jsonOptions.Value.SerializerOptions;
            if (TryGetProperty(body, "payload", out JsonElement payload) && payload.ValueKind != JsonValueKind.Null)
            {
                return await ProjectEventStoreAsync(body, payload, options, context, cancellationToken).ConfigureAwait(false);
            }

            foreach (Func<JsonElement, JsonSerializerOptions, HttpContext, CancellationToken, Task<IResult>?> family in _families)
            {
                Task<IResult>? projection = family(body, options, context, cancellationToken);
                if (projection is null)
                {
                    continue;
                }

                IResult result = await projection.ConfigureAwait(false);
                if (result is not IStatusCodeHttpResult { StatusCode: StatusCodes.Status200OK })
                {
                    return result;
                }
            }
        }

        return Results.Ok();
    }

    private static async Task<IResult> ProjectEventStoreAsync(
        JsonElement body,
        JsonElement payload,
        JsonSerializerOptions options,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        JsonElement metadata = TryGetProperty(body, "metadata", out JsonElement nestedMetadata) ? nestedMetadata : body;
        if (!TryBind(metadata, options, context, "event-store", out PublishedGovernedOperationEvent? envelope) ||
            envelope.Domain != ChatBotEventStore.DomainName ||
            string.IsNullOrWhiteSpace(envelope.EventTypeName) ||
            string.IsNullOrWhiteSpace(envelope.TenantId) ||
            string.IsNullOrWhiteSpace(envelope.AggregateId) ||
            envelope.SequenceNumber <= 0 ||
            envelope.Timestamp == default)
        {
            return Results.Ok();
        }

        if (payload.ValueKind != JsonValueKind.String || !payload.TryGetBytesFromBase64(out byte[]? bytes))
        {
            LogBindingSkipped(context, "event-store", metadata);
            return Results.Ok();
        }

        string eventType = envelope.EventTypeName.Split(',', 2)[0];
        var request = new ProjectionRequest(envelope.TenantId, envelope.Domain, envelope.AggregateId, []);
        var projectionEvent = new ProjectionEventDto(
            eventType, bytes, "json", envelope.SequenceNumber, envelope.Timestamp, envelope.CorrelationId ?? string.Empty, envelope.MessageId);

        object? published;
        try
        {
            published = CreatePublishedEvent(request, projectionEvent);
        }
        catch (JsonException)
        {
            LogBindingSkipped(context, "event-store", metadata);
            return Results.Ok();
        }

        // Invoke only the read-model handlers. The SDK projection owns the outbox, queues and coverage.
        switch (published)
        {
            case PublishedMailboxIntakeEvent mailbox when MailboxIntakeProjectionTranslator.TryCreateNotification(mailbox) is { } intake:
                _ = await Service<AssociationProjectionHandler>(context).HandleAsync(
                    intake.Captured, intake.TenantId, intake.SourceVersion, intake.CorrelationId, runSideEffects: false, cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok();
            case PublishedAssociationEvent association when AssociationProjectionTranslator.TryCreateNotification(association) is { } notification:
                _ = await Service<AssociationProjectionHandler>(context).HandleAsync(
                    notification, runSideEffects: false, cancellationToken).ConfigureAwait(false);
                return Results.Ok();
            case PublishedParticipantResolutionEvent participant:
                return await ParticipantResolutionProjectionEndpoints.ProjectAsync(
                    participant, Service<ParticipantResolutionProjectionHandler>(context), cancellationToken).ConfigureAwait(false);
            case PublishedAiActionExecutionEvent execution:
                _ = await Service<AiOutcomeProjectionHandler>(context).HandleAsync(execution, cancellationToken).ConfigureAwait(false);
                return Results.Ok();
            case PublishedTaskIntentEvent task:
                return await TaskIntentProjectionEndpoints.ProjectAsync(
                    task, Service<TaskIntentProjectionHandler>(context), cancellationToken).ConfigureAwait(false);
            case PublishedAiActionApprovalEvent approval:
                return await ApprovalProjectionEndpoints.ProjectAsync(
                    approval, Service<ApprovalProjectionHandler>(context), cancellationToken).ConfigureAwait(false);
            default:
                return await GovernedOperationProjectionEndpoints.ProjectAsync(
                    envelope with { EventTypeName = eventType, Payload = bytes },
                    Service<GovernedOperationProjectionHandler>(context),
                    Service<GovernedControlStateProjectionHandler>(context),
                    cancellationToken).ConfigureAwait(false);
        }
    }

    private static object? CreatePublishedEvent(ProjectionRequest request, ProjectionEventDto item)
    {
        string eventType = item.EventTypeName;
        if (eventType == MailboxIntakeProjectionTranslator.IntakeCapturedEventType)
        {
            return TryCreatePublishedEvent<PublishedMailboxIntakeEvent>(request, item);
        }

        if (AssociationProjectionTranslator.IsAssociationEvent(eventType))
        {
            return TryCreatePublishedEvent<PublishedAssociationEvent>(request, item);
        }

        if (eventType == ParticipantResolutionProjectionTranslator.ResolvedEventType ||
            eventType == ParticipantResolutionProjectionTranslator.UnresolvedEventType)
        {
            return TryCreatePublishedEvent<PublishedParticipantResolutionEvent>(request, item);
        }

        if (TaskIntentProjectionTranslator.IsSupportedTaskIntentEvent(eventType))
        {
            return TryCreateFlatTaskIntentEvent(request, item) ?? TryCreatePublishedEvent<PublishedTaskIntentEvent>(request, item);
        }

        return (object?)TryCreateFlatConversationEvent(request, item)
            ?? (object?)TryCreateFlatApprovalEvent(request, item)
            ?? TryCreateFlatAiActionExecutionEvent(request, item);
    }

    private static bool TryGetProperty(JsonElement body, string name, out JsonElement value)
    {
        if (body.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in body.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static void LogBindingSkipped(HttpContext context, string family, JsonElement metadata)
        => EnvelopeBindingSkipped(
            Service<ILoggerFactory>(context).CreateLogger(typeof(ChatBotProjectionFanOutEndpoints)),
            family,
            EventTypeName(metadata));

    private static bool TryBind<TEnvelope>(
        JsonElement body,
        JsonSerializerOptions options,
        HttpContext context,
        string family,
        [NotNullWhen(true)] out TEnvelope? published)
        where TEnvelope : class
    {
        try
        {
            published = body.Deserialize<TEnvelope>(options);
        }
        catch (JsonException)
        {
            // The exception is deliberately not logged: its message can quote the delivered body.
            LogBindingSkipped(context, family, body);
            published = null;
        }

        return published is not null;
    }

    /// <summary>
    /// Binds the envelope the AI outcome handler reads from the raw body: an execution event when the body names an
    /// event type, otherwise a governed AI outcome. Binding it here lets an unbindable body skip the family while store
    /// faults inside the handler still propagate.
    /// </summary>
    private static bool TryBindAiOutcome(JsonElement body, JsonSerializerOptions options, HttpContext context)
        => body.TryGetProperty("eventTypeName", out _)
            ? TryBind(body, options, context, AiOutcomeFamily, out PublishedAiActionExecutionEvent? _)
            : TryBind(body, options, context, AiOutcomeFamily, out PublishedAiOutcomeEvent? _);

    /// <summary>Reduces the untrusted event type name to a bounded, log-safe token.</summary>
    private static string EventTypeName(JsonElement body)
        => AuditMetadata.SafeOptionalToken(
            TryGetProperty(body, "eventTypeName", out JsonElement eventTypeName) && eventTypeName.ValueKind == JsonValueKind.String
                ? eventTypeName.GetString()
                : null)
            ?? "unknown";

    private static TService Service<TService>(HttpContext context)
        where TService : notnull
        => context.RequestServices.GetRequiredService<TService>();

    [LoggerMessage(EventId = 110302, Level = LogLevel.Warning, Message = EnvelopeBindingSkippedLog)]
    private static partial void EnvelopeBindingSkipped(ILogger logger, string family, string eventTypeName);
}
