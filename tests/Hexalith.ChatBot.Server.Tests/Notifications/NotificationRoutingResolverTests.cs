using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Notifications;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Notifications;

public sealed class NotificationRoutingResolverTests
{
    [Fact]
    public void AllSixStateClassesShouldRouteToConfiguredRecipientAndChannel()
    {
        NotificationRoutingChangeSet routing = new(
        [
            new NotificationRoutingEntry(NotificationStateClass.ReviewNeeded, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.InApp),
            new NotificationRoutingEntry(NotificationStateClass.ApprovalPending, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.Email),
            new NotificationRoutingEntry(NotificationStateClass.Failure, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
            new NotificationRoutingEntry(NotificationStateClass.Degraded, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
            new NotificationRoutingEntry(NotificationStateClass.Quarantine, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.Webhook),
            new NotificationRoutingEntry(NotificationStateClass.Retry, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.InApp),
        ]);

        NotificationRecipientCandidate[] candidates = [Candidate("operator-001", "operations-admin")];

        foreach (NotificationStateClass stateClass in NotificationStateClasses.All)
        {
            NotificationRoutingEntry expected = routing.Entries.Single(entry => entry.StateClass == stateClass);
            IReadOnlyList<NotificationDelivery> deliveries = NotificationRoutingResolver.Resolve(
                Event(stateClass),
                routing,
                candidates);

            NotificationDelivery delivery = deliveries.ShouldHaveSingleItem();
            delivery.StateClass.ShouldBe(stateClass);
            delivery.Channel.ShouldBe(expected.Channel);
            delivery.RecipientRole.ShouldBe(AdminRole.OperationsAdmin);
            delivery.RecipientRef.ShouldBe("operator-001");
        }
    }

    [Fact]
    public void ItemSpecificContextShouldReachOnlyRecipientsWithPerItemAuthority()
    {
        NotificationRoutingChangeSet routing = new(
        [
            new NotificationRoutingEntry(NotificationStateClass.Failure, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
        ]);

        NotificationRecipientCandidate authorized = Candidate("operator-owner", "operations-admin", projectRef: "project-x");
        NotificationRecipientCandidate unauthorized = Candidate("operator-blind", "operations-admin");

        IReadOnlyList<NotificationDelivery> deliveries = NotificationRoutingResolver.Resolve(
            Event(NotificationStateClass.Failure, itemProjectRef: "project-x"),
            routing,
            [authorized, unauthorized]);

        deliveries.Count.ShouldBe(2);

        NotificationDelivery authorizedDelivery = deliveries.Single(d => d.RecipientRef == "operator-owner");
        authorizedDelivery.Visibility.ShouldBe(NotificationContentVisibility.ItemContext);
        authorizedDelivery.ItemRef.ShouldBe("item-77");

        NotificationDelivery redactedDelivery = deliveries.Single(d => d.RecipientRef == "operator-blind");
        redactedDelivery.Visibility.ShouldBe(NotificationContentVisibility.MetadataRedacted);
        redactedDelivery.ItemRef.ShouldBeNull();

        // No resource-existence leakage: the redacted form must be indistinguishable from safe-not-found.
        string json = JsonSerializer.Serialize(redactedDelivery, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("item-77");
        json.ShouldNotContain("project-x");
    }

    [Fact]
    public void AggregateEventsShouldNeverCarryItemContext()
    {
        NotificationRoutingChangeSet routing = new(
        [
            new NotificationRoutingEntry(NotificationStateClass.ReviewNeeded, AdminScope.SeeOnly, AdminRole.OperationsAdmin, NotificationChannel.InApp),
        ]);

        IReadOnlyList<NotificationDelivery> deliveries = NotificationRoutingResolver.Resolve(
            Event(NotificationStateClass.ReviewNeeded),
            routing,
            [Candidate("operator-owner", "operations-admin", projectRef: "*")]);

        NotificationDelivery delivery = deliveries.ShouldHaveSingleItem();
        delivery.Visibility.ShouldBe(NotificationContentVisibility.MetadataRedacted);
        delivery.ItemRef.ShouldBeNull();
    }

    [Fact]
    public void WildcardProjectOwnerShouldReceiveItemContextForItemSpecificEvents()
    {
        // The notification-routing audience intentionally honors the tenant-wide "*" project-owner wildcard (matching
        // the gateway/outbound convention), unlike the compliance full-detail path which requires an explicit grant.
        // This pins that divergence so the shared AdminAuthorityEvaluator.HasProjectAuthority helper cannot silently
        // drop wildcard support for routing.
        NotificationRoutingChangeSet routing = new(
        [
            new NotificationRoutingEntry(NotificationStateClass.Failure, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
        ]);

        IReadOnlyList<NotificationDelivery> deliveries = NotificationRoutingResolver.Resolve(
            Event(NotificationStateClass.Failure, itemProjectRef: "project-x"),
            routing,
            [Candidate("operator-wildcard", "operations-admin", projectRef: "*")]);

        NotificationDelivery delivery = deliveries.ShouldHaveSingleItem();
        delivery.Visibility.ShouldBe(NotificationContentVisibility.ItemContext);
        delivery.ItemRef.ShouldBe("item-77");
    }

    [Fact]
    public void RecipientsWithoutTheConfiguredRoleShouldReceiveNoNotification()
    {
        NotificationRoutingChangeSet routing = new(
        [
            new NotificationRoutingEntry(NotificationStateClass.Quarantine, AdminScope.Compliance, AdminRole.ComplianceAdmin, NotificationChannel.Email),
        ]);

        IReadOnlyList<NotificationDelivery> deliveries = NotificationRoutingResolver.Resolve(
            Event(NotificationStateClass.Quarantine, itemProjectRef: "project-x"),
            routing,
            [Candidate("operator-blind", "operations-admin", projectRef: "project-x")]);

        deliveries.ShouldBeEmpty();
    }

    [Fact]
    public void InvalidRoutingMapShouldProduceNoDeliveries()
    {
        NotificationRoutingChangeSet invalid = new(
        [
            new NotificationRoutingEntry((NotificationStateClass)99, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.InApp),
        ]);

        NotificationRoutingResolver.Resolve(
            Event(NotificationStateClass.Failure),
            invalid,
            [Candidate("operator-001", "operations-admin")]).ShouldBeEmpty();
    }

    [Fact]
    public void RaisedAtShouldBeNormalizedToUtcOnTheDelivery()
    {
        // AC1: the delivered metadata carries a UTC raised-at. A non-UTC source offset must be normalized.
        NotificationStateEvent stateEvent = new(
            "tenant-alpha",
            NotificationStateClass.Failure,
            "item-77",
            "queue:operations",
            "needs-attention",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            new DateTimeOffset(2026, 6, 2, 9, 30, 0, TimeSpan.FromHours(2)));

        NotificationRoutingChangeSet routing = new(
        [
            new NotificationRoutingEntry(NotificationStateClass.Failure, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
        ]);

        NotificationDelivery delivery = NotificationRoutingResolver
            .Resolve(stateEvent, routing, [Candidate("operator-001", "operations-admin")])
            .ShouldHaveSingleItem();

        delivery.RaisedAtUtc.Offset.ShouldBe(TimeSpan.Zero);
        delivery.RaisedAtUtc.ShouldBe(new DateTimeOffset(2026, 6, 2, 7, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void TenantRefShouldComeFromTheEventBindingNeverRecipientOrCorrelation()
    {
        // Architecture guardrail: tenant id is the authenticated event binding — never an item/recipient/correlation id.
        NotificationStateEvent stateEvent = new(
            "tenant-alpha",
            NotificationStateClass.Failure,
            "item-77",
            "queue:operations",
            "needs-attention",
            "correlation-tenant-beta",
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero));

        NotificationRoutingChangeSet routing = new(
        [
            new NotificationRoutingEntry(NotificationStateClass.Failure, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
        ]);

        NotificationDelivery delivery = NotificationRoutingResolver
            .Resolve(stateEvent, routing, [Candidate("operator-tenant-gamma", "operations-admin")])
            .ShouldHaveSingleItem();

        delivery.TenantRef.ShouldBe("tenant-alpha");
        delivery.TenantRef.ShouldNotBe("correlation-tenant-beta");
        delivery.TenantRef.ShouldNotBe("operator-tenant-gamma");
    }

    [Fact]
    public async Task ResolvedDeliveriesShouldFlowThroughTheMetadataOnlySinkWithoutLeakage()
    {
        // AC1/AC6: notifications are actually delivered, and the delivery seam record stays metadata-only.
        NotificationRoutingChangeSet routing = new(
        [
            new NotificationRoutingEntry(NotificationStateClass.Failure, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
        ]);

        IReadOnlyList<NotificationDelivery> resolved = NotificationRoutingResolver.Resolve(
            Event(NotificationStateClass.Failure, itemProjectRef: "project-x"),
            routing,
            [
                Candidate("operator-owner", "operations-admin", projectRef: "project-x"),
                Candidate("operator-blind", "operations-admin"),
            ]);

        InMemoryNotificationSink sink = new();
        foreach (NotificationDelivery delivery in resolved)
        {
            await sink.DeliverAsync(delivery, TestContext.Current.CancellationToken);
        }

        sink.Deliveries.Count.ShouldBe(2);
        sink.Deliveries.ShouldContain(d => d.RecipientRef == "operator-owner" && d.Visibility == NotificationContentVisibility.ItemContext);
        sink.Deliveries.ShouldContain(d => d.RecipientRef == "operator-blind" && d.Visibility == NotificationContentVisibility.MetadataRedacted);

        // The stored delivery seam must never carry restricted content, recipient addresses, or secrets.
        string json = JsonSerializer.Serialize(sink.Deliveries, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("@");
        json.ShouldNotContain("address", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
    }

    private static NotificationStateEvent Event(NotificationStateClass stateClass, string? itemProjectRef = null)
        => new(
            "tenant-alpha",
            stateClass,
            "item-77",
            "queue:operations",
            "needs-attention",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero),
            itemProjectRef);

    private static NotificationRecipientCandidate Candidate(string recipientRef, string role, string? projectRef = null)
    {
        List<Claim> claims =
        [
            new Claim("sub", recipientRef),
            new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
            new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
        ];
        if (!string.IsNullOrWhiteSpace(projectRef))
        {
            claims.Add(new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, projectRef));
        }

        return new NotificationRecipientCandidate(recipientRef, new ClaimsPrincipal(new ClaimsIdentity(claims, "test")));
    }
}
