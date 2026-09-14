using Hexalith.ChatBot.AppHost.Aspire;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.Extensions.Configuration;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Ties the AppHost's DAPR topology constants to the Server's EFFECTIVE subscription binding.
/// </summary>
/// <remarks>
/// <c>ChatBotAspireModule.DeadLetterTopicName</c> was previously referenced only by its own declaration and two
/// source-text assertions — the "unused constant" the 2026-09-13 review named — while the Server re-hardcoded the
/// same literal as an unshared inline fallback. Asserting the two through the Server's real resolution path makes
/// the constant load-bearing: changing either side alone fails here rather than silently splitting the topology
/// from the subscriber.
/// </remarks>
public static class ChatBotDeadLetterSubscriptionTopologyTests
{
    [Fact]
    public static void ServerSubscriptionDefaultsMustMatchTheAspireTopologyConstants()
    {
        // No configuration at all: this is the binding a ChatBot host falls back to when the AppHost supplies none.
        IConfiguration empty = new ConfigurationBuilder().Build();

        ChatBotProjectionSubscriptionBinding binding = ChatBotProjectionSubscriptionDefaults.Resolve(empty);

        binding.PubSubName.ShouldBe(ChatBotAspireModule.PubSubComponentName);
        binding.Topic.ShouldBe(ChatBotAspireModule.PubSubTopicName);
        binding.DeadLetterTopic.ShouldBe(ChatBotAspireModule.DeadLetterTopicName);
    }

    [Fact]
    public static void ServerSubscriptionBindingMustHonourTheAppHostSuppliedTenantDeadLetterTopic()
    {
        // The AppHost overrides the dead-letter topic per tenant (Program.cs: ChatBot__Projection__DeadLetterTopic =
        // GetTenantDeadLetterTopic("tenant-alpha")). The subscriber — and therefore the dead-letter drain — must
        // follow the override rather than the untenanted default, or poison messages land on a topic nothing reads.
        string tenantDeadLetterTopic = ChatBotAspireModule.GetTenantDeadLetterTopic("tenant-alpha");
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ChatBotProjectionSubscriptionDefaults.PubSubNameConfigurationKey] = "other-pubsub",
                [ChatBotProjectionSubscriptionDefaults.TopicConfigurationKey] = "other.events",
                [ChatBotProjectionSubscriptionDefaults.DeadLetterTopicConfigurationKey] = tenantDeadLetterTopic,
            })
            .Build();

        ChatBotProjectionSubscriptionBinding binding = ChatBotProjectionSubscriptionDefaults.Resolve(configuration);

        binding.PubSubName.ShouldBe("other-pubsub");
        binding.Topic.ShouldBe("other.events");
        binding.DeadLetterTopic.ShouldBe(tenantDeadLetterTopic);
        tenantDeadLetterTopic.ShouldNotBe(ChatBotAspireModule.DeadLetterTopicName);
    }

    [Fact]
    public static void BlankConfiguredValuesMustFallBackRatherThanReachTheSubscriptionAsEmpty()
    {
        // The subscription mappers guard with ArgumentException.ThrowIfNullOrWhiteSpace, so a blank configured
        // value would take down every projection subscription at startup instead of degrading to the default.
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ChatBotProjectionSubscriptionDefaults.PubSubNameConfigurationKey] = "   ",
                [ChatBotProjectionSubscriptionDefaults.TopicConfigurationKey] = string.Empty,
                [ChatBotProjectionSubscriptionDefaults.DeadLetterTopicConfigurationKey] = "  ",
            })
            .Build();

        ChatBotProjectionSubscriptionBinding binding = ChatBotProjectionSubscriptionDefaults.Resolve(configuration);

        binding.PubSubName.ShouldBe(ChatBotAspireModule.PubSubComponentName);
        binding.Topic.ShouldBe(ChatBotAspireModule.PubSubTopicName);
        binding.DeadLetterTopic.ShouldBe(ChatBotAspireModule.DeadLetterTopicName);
    }
}
