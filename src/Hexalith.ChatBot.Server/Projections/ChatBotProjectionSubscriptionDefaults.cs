using Microsoft.Extensions.Configuration;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// The single source of the DAPR pub/sub component, topic, and dead-letter topic the ChatBot projection
/// subscriptions bind to, plus the configuration keys that override them.
/// </summary>
/// <remarks>
/// These literals were previously duplicated as unshared inline fallbacks at the one call site while the AppHost
/// carried its own copies as separate constants, so the two could drift with nothing failing. The Aspire module's
/// <c>DeadLetterTopicName</c> is asserted against <see cref="DeadLetterTopic"/> by the integration suite, which is
/// what makes that constant load-bearing rather than merely source-asserted.
/// </remarks>
internal static class ChatBotProjectionSubscriptionDefaults
{
    /// <summary>The DAPR pub/sub component carrying governed ChatBot events.</summary>
    public const string PubSubName = "chatbot-pubsub";

    /// <summary>The topic every ChatBot projection subscription binds to.</summary>
    public const string Topic = "chatbot.events";

    /// <summary>
    /// The dead-letter topic poison projection messages are routed to once DAPR exhausts redelivery. The AppHost
    /// overrides it per tenant (<c>deadletter.{tenant}.chatbot.events</c>); this is the untenanted default.
    /// </summary>
    public const string DeadLetterTopic = "deadletter.chatbot.events";

    /// <summary>The configuration key overriding <see cref="PubSubName"/>.</summary>
    public const string PubSubNameConfigurationKey = "ChatBot:Projection:PubSubName";

    /// <summary>The configuration key overriding <see cref="Topic"/>.</summary>
    public const string TopicConfigurationKey = "ChatBot:Projection:Topic";

    /// <summary>The configuration key overriding <see cref="DeadLetterTopic"/>.</summary>
    public const string DeadLetterTopicConfigurationKey = "ChatBot:Projection:DeadLetterTopic";

    /// <summary>
    /// Resolves the effective subscription binding from configuration, falling back to the defaults above.
    /// </summary>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The pub/sub component, topic, and dead-letter topic the subscriptions must bind to.</returns>
    public static ChatBotProjectionSubscriptionBinding Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new ChatBotProjectionSubscriptionBinding(
            Coalesce(configuration[PubSubNameConfigurationKey], PubSubName),
            Coalesce(configuration[TopicConfigurationKey], Topic),
            Coalesce(configuration[DeadLetterTopicConfigurationKey], DeadLetterTopic));
    }

    private static string Coalesce(string? configured, string fallback)
        => string.IsNullOrWhiteSpace(configured) ? fallback : configured;
}
