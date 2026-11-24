using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Dapr;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

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
/// This route carries the one subscription. It reads the body once and runs each family's existing projection in the
/// original mapping order, binding the family's envelope with the host's HTTP JSON options. A family whose envelope
/// cannot bind the body is skipped, and its translator ignores foreign events that do bind. The route answers 200
/// only when every family succeeds. A family fault propagates so DAPR redelivers the event, and the idempotent,
/// version-stamped handlers absorb the replay of the families already applied.
/// </para>
/// </remarks>
internal static class ChatBotProjectionFanOutEndpoints
{
    /// <summary>The route the chatbot events subscription delivers every published event to.</summary>
    public const string FanOutRoute = "/chatbot/events";

    /// <summary>
    /// The projection families in their original mapping order. Each returns <see langword="null"/> when the body does
    /// not bind to its envelope.
    /// </summary>
    private static readonly Func<JsonElement, JsonSerializerOptions, HttpContext, CancellationToken, Task<IResult>?>[] _families =
    [
        static (body, options, context, cancellationToken) => TryBind(body, options, out PublishedGovernedOperationEvent? published)
            ? GovernedOperationProjectionEndpoints.ProjectAsync(
                published,
                Service<GovernedOperationProjectionHandler>(context),
                Service<GovernedControlStateProjectionHandler>(context),
                cancellationToken)
            : null,
        static (body, options, context, cancellationToken) => TryBind(body, options, out PublishedMailboxIntakeEvent? published)
            ? MailboxIntakeProjectionEndpoints.ProjectAsync(
                published,
                Service<AssociationProjectionHandler>(context),
                cancellationToken)
            : null,
        static (body, options, context, cancellationToken) => TryBind(body, options, out PublishedAssociationEvent? published)
            ? AssociationProjectionEndpoints.ProjectAsync(
                context,
                Service<IConfiguration>(context),
                published,
                Service<AssociationProjectionHandler>(context),
                cancellationToken)
            : null,
        static (body, options, context, cancellationToken) => TryBind(body, options, out PublishedParticipantResolutionEvent? published)
            ? ParticipantResolutionProjectionEndpoints.ProjectAsync(
                published,
                Service<ParticipantResolutionProjectionHandler>(context),
                cancellationToken)
            : null,
        static (body, _, context, cancellationToken) => AiOutcomeProjectionEndpoints.ProjectAsync(
            body,
            Service<AiOutcomeProjectionHandler>(context),
            cancellationToken),
        static (body, options, context, cancellationToken) => TryBind(body, options, out PublishedTaskIntentEvent? published)
            ? TaskIntentProjectionEndpoints.ProjectAsync(
                published,
                Service<TaskIntentProjectionHandler>(context),
                cancellationToken)
            : null,
        static (body, options, context, cancellationToken) => TryBind(body, options, out PublishedAiActionApprovalEvent? published)
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

    private static bool TryBind<TEnvelope>(JsonElement body, JsonSerializerOptions options, [NotNullWhen(true)] out TEnvelope? published)
        where TEnvelope : class
    {
        try
        {
            published = body.Deserialize<TEnvelope>(options);
        }
        catch (JsonException)
        {
            published = null;
        }

        return published is not null;
    }

    private static TService Service<TService>(HttpContext context)
        where TService : notnull
        => context.RequestServices.GetRequiredService<TService>();
}
