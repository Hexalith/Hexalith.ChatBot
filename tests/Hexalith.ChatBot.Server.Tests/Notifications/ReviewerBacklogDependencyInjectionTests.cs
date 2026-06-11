using System.Linq;
using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Notifications;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Notifications;

/// <summary>
/// Story 7.10 wiring guard for <c>AddChatBotCommandGateway</c>: the reviewer-backlog coordinator must resolve with
/// the shared notification sink, audit writer, and system clock used by the runtime delivery path.
/// </summary>
public sealed class ReviewerBacklogDependencyInjectionTests
{
    [Fact]
    public void ReviewerBacklogRuntimeSeamsResolveToSharedInMemoryDefaults()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<INotificationSink>().ShouldBeSameAs(provider.GetRequiredService<InMemoryNotificationSink>());
        provider.GetRequiredService<IAuditWriter>().ShouldBeOfType<ChainedAuditWriter>();
        provider.GetRequiredService<ISystemClock>().ShouldBeOfType<SystemClock>();
        provider.GetRequiredService<ReviewerBacklogAlertCoordinator>().ShouldNotBeNull();
    }

    [Fact]
    public async Task RegisteredCoordinatorFiresMetadataOnlyBacklogAlertAndDelivers()
    {
        using ServiceProvider provider = BuildProvider();
        ReviewerBacklogAlertCoordinator coordinator = provider.GetRequiredService<ReviewerBacklogAlertCoordinator>();

        ReviewerBacklogAlertOutcome outcome = await coordinator.EvaluateAndDeliverAsync(
            BacklogItems("reviewer-a", 26, ageSeconds: 900),
            [Candidate("admin-001", "tenant-admin"), Candidate("reviewer-a", "operations-admin")],
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ReviewerBacklogThreshold.SafeDefault,
            TestContext.Current.CancellationToken);

        outcome.Fired.ShouldBe(1);
        outcome.Delivered.ShouldBe(1);
        outcome.AuditUnavailable.ShouldBe(0);

        InMemoryNotificationSink sink = provider.GetRequiredService<InMemoryNotificationSink>();
        NotificationDelivery delivery = sink.Deliveries.ShouldHaveSingleItem();
        delivery.RecipientRef.ShouldBe("admin-001");
        delivery.RecipientRole.ShouldBe(AdminRole.TenantAdmin);
        delivery.Visibility.ShouldBe(NotificationContentVisibility.MetadataRedacted);
        delivery.ItemRef.ShouldBeNull();

        AuditEnvelope envelope = provider.GetRequiredService<InMemoryAuditWriter>().Envelopes.ShouldHaveSingleItem();
        envelope.SourceEvidenceRefs.ShouldContain("admin-operation:reviewer-backlog-alert-fired");
        envelope.SourceEvidenceRefs.ShouldContain("reviewer:reviewer-a");
        envelope.SourceEvidenceRefs.ShouldContain("backlog-depth:26");
        envelope.SourceEvidenceRefs.ShouldContain("backlog-oldest-age-seconds:900");
        envelope.SourceEvidenceRefs.ShouldContain("backlog-threshold:25");
        envelope.SourceEvidenceRefs.ShouldContain("notification-channel:in-app");
        envelope.SourceEvidenceRefs.ShouldContain("recipient-role:tenant-admin");
        envelope.SourceEvidenceRefs.ShouldNotContain(reference => reference.Contains('@', StringComparison.Ordinal));
        envelope.SourceEvidenceRefs.ShouldNotContain(reference => reference.Contains("secret", StringComparison.OrdinalIgnoreCase));
        envelope.SourceEvidenceRefs.ShouldNotContain(reference => reference.Contains("project-", StringComparison.OrdinalIgnoreCase));
    }

    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        _ = services.AddChatBotCommandGateway();
        return services.BuildServiceProvider();
    }

    private static List<AdminQueueSummaryProjectionItem> BacklogItems(string reviewer, int count, int ageSeconds)
        => Enumerable.Range(0, count).Select(i => new AdminQueueSummaryProjectionItem(
            QueueRef: "queue:approvals",
            ItemRef: $"i-{reviewer}-{i}",
            Status: "pending",
            OwnerClass: "operations",
            Health: ChatBotHealthStatus.Degraded,
            AgeSeconds: ageSeconds,
            QueueFamily: OperationalQueueFamily.PendingApproval,
            AssigneeRef: reviewer)).ToList();

    private static NotificationRecipientCandidate Candidate(string recipientRef, string role)
    {
        List<Claim> claims =
        [
            new Claim("sub", recipientRef),
            new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
            new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
        ];
        return new NotificationRecipientCandidate(recipientRef, new ClaimsPrincipal(new ClaimsIdentity(claims, "test")));
    }
}
