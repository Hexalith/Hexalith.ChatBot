using Microsoft.Extensions.Configuration;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>The resolved DAPR binding every ChatBot projection subscription (and the dead-letter drain) uses.</summary>
/// <param name="PubSubName">The DAPR pub/sub component name.</param>
/// <param name="Topic">The topic the projection subscriptions bind to.</param>
/// <param name="DeadLetterTopic">The dead-letter topic poison messages are routed to and drained from.</param>
internal sealed record ChatBotProjectionSubscriptionBinding(
    string PubSubName,
    string Topic,
    string DeadLetterTopic);
