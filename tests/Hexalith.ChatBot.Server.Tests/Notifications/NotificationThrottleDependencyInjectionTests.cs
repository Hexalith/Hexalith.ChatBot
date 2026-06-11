using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Notifications;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Notifications;

/// <summary>
/// Story 7.9 wiring guard for <c>AddChatBotCommandGateway</c>: the throttle coordinator must resolve with the same
/// singleton notification sink, delivery-history store, digest store, audit writer, and system clock that the runtime
/// delivery path depends on. Unit tests cover the evaluator/coordinator logic; this test catches registration drift at
/// the integration seam.
/// </summary>
public sealed class NotificationThrottleDependencyInjectionTests
{
    [Fact]
    public void NotificationThrottleRuntimeSeamsResolveToSharedInMemoryDefaults()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<INotificationSink>().ShouldBeSameAs(provider.GetRequiredService<InMemoryNotificationSink>());
        provider.GetRequiredService<INotificationDeliveryHistoryStore>().ShouldBeSameAs(provider.GetRequiredService<InMemoryNotificationDeliveryHistoryStore>());
        provider.GetRequiredService<INotificationDigestStore>().ShouldBeSameAs(provider.GetRequiredService<InMemoryNotificationDigestStore>());
        provider.GetRequiredService<IAuditWriter>().ShouldBeOfType<ChainedAuditWriter>();
        provider.GetRequiredService<ISystemClock>().ShouldBeOfType<SystemClock>();
        provider.GetRequiredService<NotificationThrottleCoordinator>().ShouldNotBeNull();
    }

    [Fact]
    public async Task RegisteredCoordinatorDeliversFirstPushAndRollsOverflowIntoDigest()
    {
        using ServiceProvider provider = BuildProvider();
        NotificationThrottleCoordinator coordinator = provider.GetRequiredService<NotificationThrottleCoordinator>();

        NotificationThrottleOutcome outcome = await coordinator.EvaluateAndDeliverAsync(
            [Delivery(), Delivery()],
            new NotificationThrottleCeilings(1, 30),
            "tenant-alpha",
            TestContext.Current.CancellationToken);

        outcome.Delivered.ShouldBe(1);
        outcome.Throttled.ShouldBe(1);
        outcome.AuditUnavailable.ShouldBe(0);

        provider.GetRequiredService<InMemoryNotificationSink>().Deliveries.Count.ShouldBe(1);
        provider.GetRequiredService<InMemoryNotificationDeliveryHistoryStore>()
            .GetImmediatePushTimestamps("tenant-alpha", "operator-001")
            .Count.ShouldBe(1);
        provider.GetRequiredService<InMemoryNotificationDigestStore>()
            .GetPendingEntries("tenant-alpha", "operator-001")
            .Count.ShouldBe(1);

        IReadOnlyList<AuditEnvelope> envelopes = provider.GetRequiredService<InMemoryAuditWriter>().Envelopes;
        envelopes.Count.ShouldBe(2);
        envelopes.SelectMany(static envelope => envelope.SourceEvidenceRefs)
            .ShouldContain("throttle-decision:delivered");
        envelopes.SelectMany(static envelope => envelope.SourceEvidenceRefs)
            .ShouldContain("throttle-decision:digest");
    }

    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        _ = services.AddChatBotCommandGateway();
        return services.BuildServiceProvider();
    }

    private static NotificationDelivery Delivery()
        => new(
            NotificationStateClass.Failure,
            NotificationChannel.OperatorAlert,
            AdminRole.OperationsAdmin,
            AdminScope.Operate,
            "operator-001",
            "tenant-alpha",
            null,
            "queue:operations",
            "review_needed",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            NotificationContentVisibility.MetadataRedacted,
            DateTimeOffset.UtcNow);
}
