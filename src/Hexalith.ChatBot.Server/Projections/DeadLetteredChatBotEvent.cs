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
/// The metadata-only projection of a dead-lettered ChatBot event envelope. Only the EventStore-stamped identity
/// fields are bound; every other member of the envelope — including the persisted event payload — is discarded by
/// the JSON reader, so the message body cannot reach a log sink through this path.
/// </summary>
/// <param name="MessageId">The originating message ULID stamped by EventStore, when present.</param>
/// <param name="CorrelationId">The correlation id carried through the spine, when present.</param>
internal sealed record DeadLetteredChatBotEvent(
    [property: JsonPropertyName("messageId")] string? MessageId,
    [property: JsonPropertyName("correlationId")] string? CorrelationId);
