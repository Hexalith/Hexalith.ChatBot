using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

/// <summary>
/// Coverage for the read-only operational dashboard aggregation (Story 8.1): all six FR67 views plus
/// audit-projection-lag render from existing queue/health sources, status is the worst-health enum (never
/// count-derived), the overview stays metadata-only, the audit-lag derivation is fail-safe, and the see-only read
/// policy allows human admins without per-project membership while denying service/AI and failing closed.
/// </summary>
public sealed class OperationalDashboardProjectorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DashboardShouldRenderAllSixViewsPlusAuditLagFromExistingSources()
    {
        OperationalDashboardOverview overview = OperationalDashboardProjector.Create(
            SampleItems(),
            AuditProjectionLagEvaluator.Evaluate(100, 100, Now.AddMinutes(-1), Now),
            OperationalDashboardAiOutcomeInput.Unknown(Now.AddMinutes(-1)),
            Now,
            "correlation-alpha");

        overview.Views.Select(static view => view.View).ShouldBe(DashboardObservabilityViews.All, ignoreOrder: false);
        OperationalDashboardContractValidator.IsValid(overview).ShouldBeTrue();
        overview.Views.ShouldAllBe(view => Enum.IsDefined(view.Health));
        overview.SchemaVersion.ShouldBe("chatbot.operational-dashboard.v1");

        // Mailbox processing aggregates failed-ingestion + failed-attachment families.
        OperationalDashboardView mailbox = ViewFor(overview, DashboardObservabilityView.MailboxProcessing);
        mailbox.Depth.ShouldBe(2);
        mailbox.DetailLinkState.ShouldBe(OperationalDashboardContractValidator.DetailRequestAccess);

        OperationalDashboardView auditLag = ViewFor(overview, DashboardObservabilityView.AuditProjectionLag);
        auditLag.Health.ShouldBe(ChatBotHealthStatus.Healthy);
        auditLag.LagIndicator.ShouldBe(AuditProjectionLagEvaluator.IndicatorCurrent);
        auditLag.Depth.ShouldBeNull();
    }

    [Fact]
    public void StatusShouldBeWorstHealthEnumNeverDerivedFromCount()
    {
        // Failed associations: many healthy rows plus a single failed row -> Failed by worst-health, not by count.
        AdminQueueSummaryProjectionItem[] items =
        [
            QueueItem(OperationalQueueFamily.AmbiguousAssociation, "item:a1", ChatBotHealthStatus.Healthy),
            QueueItem(OperationalQueueFamily.AmbiguousAssociation, "item:a2", ChatBotHealthStatus.Healthy),
            QueueItem(OperationalQueueFamily.AmbiguousAssociation, "item:a3", ChatBotHealthStatus.Healthy),
            QueueItem(OperationalQueueFamily.UnresolvedParticipant, "item:p1", ChatBotHealthStatus.Failed),
        ];

        OperationalDashboardOverview overview = OperationalDashboardProjector.Create(
            items,
            AuditProjectionLagEvaluator.Evaluate(null, null, Now, Now),
            OperationalDashboardAiOutcomeInput.Unknown(Now),
            Now,
            "correlation-alpha");

        OperationalDashboardView failedAssociations = ViewFor(overview, DashboardObservabilityView.FailedAssociations);
        failedAssociations.Health.ShouldBe(ChatBotHealthStatus.Failed);
        failedAssociations.Depth.ShouldBe(4);

        // No checkpoint positions -> fail-safe Unknown, never a fabricated Healthy.
        ViewFor(overview, DashboardObservabilityView.AuditProjectionLag).Health.ShouldBe(ChatBotHealthStatus.Unknown);
        // No contributing AI-outcome source -> Unknown.
        ViewFor(overview, DashboardObservabilityView.AiActionOutcomes).Health.ShouldBe(ChatBotHealthStatus.Unknown);
    }

    [Fact]
    public void EmptyViewShouldRenderUnknownNotFabricatedHealthy()
    {
        OperationalDashboardOverview overview = OperationalDashboardProjector.Create(
            [],
            AuditProjectionLagEvaluator.Evaluate(10, 10, Now, Now),
            OperationalDashboardAiOutcomeInput.Unknown(Now),
            Now,
            "correlation-alpha");

        ViewFor(overview, DashboardObservabilityView.MailboxProcessing).Health.ShouldBe(ChatBotHealthStatus.Unknown);
        ViewFor(overview, DashboardObservabilityView.MailboxProcessing).Depth.ShouldBe(0);
        OperationalDashboardContractValidator.IsValid(overview).ShouldBeTrue();
    }

    [Fact]
    public void DegradedViewShouldCarryResolvedAffectedScopeKindAndNextSafeActionWhileHealthyViewsLeaveThemNull()
    {
        // Mailbox processing aggregates a Degraded failed-ingestion item (mailbox:ops, next action "claim").
        OperationalDashboardOverview overview = OperationalDashboardProjector.Create(
            SampleItems(),
            AuditProjectionLagEvaluator.Evaluate(100, 100, Now.AddMinutes(-1), Now), // Healthy audit lag
            OperationalDashboardAiOutcomeInput.Unknown(Now.AddMinutes(-1)),
            Now,
            "correlation-alpha");

        OperationalDashboardView mailbox = ViewFor(overview, DashboardObservabilityView.MailboxProcessing);
        mailbox.Health.ShouldBe(ChatBotHealthStatus.Degraded);
        mailbox.AffectedScope.ShouldBe("mailbox:ops");
        mailbox.ScopeKind.ShouldBe("mailbox");
        mailbox.NextSafeAction.ShouldBe("claim");

        // A healthy view leaves the new fields null (no fabricated scope on a healthy surface).
        OperationalDashboardView approvals = ViewFor(overview, DashboardObservabilityView.ApprovalQueues);
        approvals.Health.ShouldBe(ChatBotHealthStatus.Healthy);
        approvals.AffectedScope.ShouldBeNull();
        approvals.ScopeKind.ShouldBeNull();
        approvals.NextSafeAction.ShouldBeNull();

        // The degraded view still classifies freshness within the NFR6 bounded-staleness window, and the overview
        // remains contract-valid with the degraded four-element surface populated.
        mailbox.FreshnessState.ShouldBe(ChatBotFreshnessState.Fresh);
        OperationalDashboardContractValidator.IsValid(overview).ShouldBeTrue();
    }

    [Fact]
    public void EmptyDegradedSurfaceShouldFailClosedToUnknownScopeNeverFabricated()
    {
        // A failed AI-outcome signal has no per-item scope token: the surface fails closed to scope:unknown rather
        // than fabricating a granularity, while still satisfying the degraded four-element requirement.
        OperationalDashboardOverview overview = OperationalDashboardProjector.Create(
            [],
            AuditProjectionLagEvaluator.Evaluate(100, 100, Now.AddMinutes(-1), Now),
            new OperationalDashboardAiOutcomeInput(ChatBotHealthStatus.Failed, 0, 0, Now.AddMinutes(-1)),
            Now,
            "correlation-alpha");

        OperationalDashboardView ai = ViewFor(overview, DashboardObservabilityView.AiActionOutcomes);
        ai.Health.ShouldBe(ChatBotHealthStatus.Failed);
        ai.AffectedScope.ShouldBe("scope:unknown");
        ai.ScopeKind.ShouldBe("unknown");
        ai.NextSafeAction.ShouldBe("escalate-to-operations");
        OperationalDashboardContractValidator.IsValid(overview).ShouldBeTrue();
    }

    [Fact]
    public void DashboardShouldStayMetadataOnlyAndOmitProjectEvidenceFileAuditAndMailboxDetail()
    {
        OperationalDashboardOverview overview = OperationalDashboardProjector.Create(
            SampleItems(),
            AuditProjectionLagEvaluator.Evaluate(100, 130, Now.AddMinutes(-1), Now),
            OperationalDashboardAiOutcomeInput.Unknown(Now.AddMinutes(-1)),
            Now,
            "correlation-alpha");

        string json = JsonSerializer.Serialize(overview, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("Project Alpha", Case.Insensitive);
        json.ShouldNotContain("evidence content sentinel", Case.Insensitive);
        json.ShouldNotContain("file-secret.pdf", Case.Insensitive);
        json.ShouldNotContain("raw audit reason", Case.Insensitive);
        json.ShouldNotContain("customer subject", Case.Insensitive);
        json.ShouldNotContain("candidate evidence", Case.Insensitive);
    }

    [Fact]
    public void AuditLagEvaluatorShouldClassifyHealthFromCheckpointPositionsAndFailSafe()
    {
        AuditProjectionLagEvaluator.Evaluate(100, 100, Now.AddMinutes(-1), Now).Health.ShouldBe(ChatBotHealthStatus.Healthy);
        AuditProjectionLagEvaluator.Evaluate(100, 130, Now.AddMinutes(-1), Now).Health.ShouldBe(ChatBotHealthStatus.Healthy);
        AuditProjectionLagEvaluator.Evaluate(100, 400, Now.AddMinutes(-1), Now).Health.ShouldBe(ChatBotHealthStatus.Degraded);
        AuditProjectionLagEvaluator.Evaluate(100, 5000, Now.AddMinutes(-1), Now).Health.ShouldBe(ChatBotHealthStatus.Failed);

        // Missing checkpoint position -> Unknown (fail-safe), not Healthy.
        AuditProjectionLagEvaluator.Evaluate(null, 100, Now, Now).Health.ShouldBe(ChatBotHealthStatus.Unknown);
        AuditProjectionLagEvaluator.Evaluate(100, null, Now, Now).Health.ShouldBe(ChatBotHealthStatus.Unknown);

        // Expired snapshot can no longer assert health -> Unknown.
        AuditProjectionLagStatus expired = AuditProjectionLagEvaluator.Evaluate(100, 110, Now.AddMinutes(-20), Now);
        expired.Health.ShouldBe(ChatBotHealthStatus.Unknown);
        expired.LagIndicator.ShouldBe(AuditProjectionLagEvaluator.IndicatorUnknown);
    }

    [Fact]
    public void ReadPolicyShouldAllowHumanSeeOnlyWithoutMembershipAndDenyServiceAiAndFailClosed()
    {
        OperationalDashboardReadPolicy.Evaluate(Principal("operations-admin"), aggregationCount: 4, auditThreshold: 10, auditAvailable: true)
            .IsAllowed.ShouldBeTrue();

        foreach (ClaimsPrincipal nonHuman in new[] { Principal("tenant-admin", "service"), Principal("tenant-admin", "ai") })
        {
            AdminQueueSummaryReadDecision denied = OperationalDashboardReadPolicy.Evaluate(nonHuman, aggregationCount: 1, auditThreshold: 10, auditAvailable: true);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        AdminQueueSummaryReadDecision failedClosed = OperationalDashboardReadPolicy.Evaluate(Principal("tenant-admin"), aggregationCount: 10, auditThreshold: 10, auditAvailable: false);
        failedClosed.IsAllowed.ShouldBeFalse();
        failedClosed.ReasonCode.ShouldBe("audit_unavailable");
    }

    [Theory]
    [InlineData(AdminRoles.TenantAdmin)]
    [InlineData(AdminRoles.MailboxAdmin)]
    [InlineData(AdminRoles.PolicyAdmin)]
    [InlineData(AdminRoles.ComplianceAdmin)]
    [InlineData(AdminRoles.OperationsAdmin)]
    public void ReadPolicyShouldAllowEverySeeOnlyAdminRoleWithoutPerProjectMembership(string role)
    {
        // AC5: any human see-only admin role reads tenant-wide summaries across all projects without holding
        // per-project membership (the read policy never inspects project membership), provided audit is available.
        AdminQueueSummaryReadDecision decision = OperationalDashboardReadPolicy.Evaluate(
            Principal(role),
            aggregationCount: 4,
            auditThreshold: 10,
            auditAvailable: true);

        decision.IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public void ReadPolicyShouldDenyHumanCallerWithoutAnAdminSeeOnlyScope()
    {
        // AC7: a human caller carrying no admin role (or an unrecognized role) has no see-only scope and is denied
        // before state load with a safe reason code — no resource-existence leakage.
        AdminQueueSummaryReadDecision nonAdminRole = OperationalDashboardReadPolicy.Evaluate(
            Principal("project-member"),
            aggregationCount: 1,
            auditThreshold: 10,
            auditAvailable: true);
        nonAdminRole.IsAllowed.ShouldBeFalse();
        nonAdminRole.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);

        AdminQueueSummaryReadDecision noRoleClaim = OperationalDashboardReadPolicy.Evaluate(
            PrincipalWithoutRole(),
            aggregationCount: 1,
            auditThreshold: 10,
            auditAvailable: true);
        noRoleClaim.IsAllowed.ShouldBeFalse();
        noRoleClaim.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
    }

    [Fact]
    public void DashboardDetailLinksShouldNeverOpenRestrictedDetailAndAlwaysCarryASafeReason()
    {
        // AC6: the dashboard surfaces metadata-only rows, so no view exposes an openable ("available") detail link;
        // queue views offer a safe request-access escalation and aggregate views a disabled state, each with a
        // stable reason code and no resource-existence leakage.
        OperationalDashboardOverview overview = OperationalDashboardProjector.Create(
            SampleItems(),
            AuditProjectionLagEvaluator.Evaluate(100, 130, Now.AddMinutes(-1), Now),
            OperationalDashboardAiOutcomeInput.Unknown(Now.AddMinutes(-1)),
            Now,
            "correlation-alpha");

        overview.Views.ShouldAllBe(view => view.DetailLinkState != OperationalDashboardContractValidator.DetailAvailable);
        overview.Views.ShouldAllBe(view => view.DisabledDetailReasonCodes.Count > 0);

        DashboardObservabilityView[] queueViews =
        [
            DashboardObservabilityView.MailboxProcessing,
            DashboardObservabilityView.FailedAssociations,
            DashboardObservabilityView.ApprovalQueues,
            DashboardObservabilityView.DuplicateHandling,
        ];

        foreach (DashboardObservabilityView queueView in queueViews)
        {
            OperationalDashboardView view = ViewFor(overview, queueView);
            view.DetailLinkState.ShouldBe(OperationalDashboardContractValidator.DetailRequestAccess);
            view.DisabledDetailReasonCodes.ShouldContain(ChatBotDisabledActionReasons.InsufficientAuthority);
        }

        foreach (DashboardObservabilityView aggregateView in new[] { DashboardObservabilityView.AiActionOutcomes, DashboardObservabilityView.AuditProjectionLag })
        {
            OperationalDashboardView view = ViewFor(overview, aggregateView);
            view.DetailLinkState.ShouldBe(OperationalDashboardContractValidator.DetailDisabled);
            view.DisabledDetailReasonCodes.ShouldContain(ChatBotDisabledActionReasons.StateNotPermitted);
        }
    }

    [Fact]
    public void OverviewShouldCarryPublishedSlosWithAuditLagBurnFromHealthAndUnknownForUnwiredSlos()
    {
        // Story 8.3 AC3/AC5: the published-SLO catalog rides the overview; the audit-projection-lag SLO's coarse
        // burn is mapped from the live audit-lag health, while every SLO without a wired signal carries Unknown.
        OperationalDashboardOverview degraded = OperationalDashboardProjector.Create(
            SampleItems(),
            AuditProjectionLagEvaluator.Evaluate(100, 400, Now.AddMinutes(-1), Now), // Degraded -> Approaching
            OperationalDashboardAiOutcomeInput.Unknown(Now.AddMinutes(-1)),
            Now,
            "correlation-alpha");

        degraded.PublishedSlos.ShouldNotBeNull();
        OperationalDashboardContractValidator.IsValid(degraded).ShouldBeTrue();
        degraded.PublishedSlos!.Select(static slo => slo.MetricName).ShouldBe(
            OperatingBaselineCatalog.Published.Select(static slo => slo.MetricName), ignoreOrder: true);

        PublishedSlo auditSlo = degraded.PublishedSlos!.Single(slo => slo.MetricName == OperatingBaselineMetrics.AuditProjectionLag);
        auditSlo.BurnState.ShouldBe(ErrorBudgetBurnState.Approaching);

        // Every other SLO has no wired signal -> honest Unknown burn (never a fabricated within-budget).
        degraded.PublishedSlos!
            .Where(slo => slo.MetricName != OperatingBaselineMetrics.AuditProjectionLag)
            .ShouldAllBe(slo => slo.BurnState == ErrorBudgetBurnState.Unknown);
    }

    [Theory]
    [InlineData(100, 100, ErrorBudgetBurnState.WithinBudget)] // Healthy
    [InlineData(100, 5000, ErrorBudgetBurnState.Exhausted)]   // Failed
    public void AuditLagBurnShouldTrackTheLagHealthSignal(long projected, long committed, ErrorBudgetBurnState expected)
    {
        OperationalDashboardOverview overview = OperationalDashboardProjector.Create(
            SampleItems(),
            AuditProjectionLagEvaluator.Evaluate(projected, committed, Now.AddMinutes(-1), Now),
            OperationalDashboardAiOutcomeInput.Unknown(Now.AddMinutes(-1)),
            Now,
            "correlation-alpha");

        overview.PublishedSlos!
            .Single(slo => slo.MetricName == OperatingBaselineMetrics.AuditProjectionLag)
            .BurnState.ShouldBe(expected);
    }

    [Fact]
    public void UnknownAuditLagShouldYieldUnknownBurnNeverFabricatedWithinBudget()
    {
        OperationalDashboardOverview overview = OperationalDashboardProjector.Create(
            SampleItems(),
            AuditProjectionLagEvaluator.Evaluate(null, null, Now, Now), // Unknown
            OperationalDashboardAiOutcomeInput.Unknown(Now),
            Now,
            "correlation-alpha");

        overview.PublishedSlos!
            .Single(slo => slo.MetricName == OperatingBaselineMetrics.AuditProjectionLag)
            .BurnState.ShouldBe(ErrorBudgetBurnState.Unknown);
    }

    [Fact]
    public void PublishedSlosShouldRideTheGatedOverviewAllowedForHumanSeeOnlyAndDeniedForNonHumanAndUnscoped()
    {
        // Story 8.3 AC4/AC8: the published SLOs + burn ride the SAME OperationalDashboardOverview that flows through
        // the NFR38 see-only human-admin gate. There is no separate SLO authorization path; the catalog is visible to
        // an authorized operator and denied (before state load) to non-human and unscoped principals.
        OperationalDashboardOverview overview = OperationalDashboardProjector.Create(
            SampleItems(),
            AuditProjectionLagEvaluator.Evaluate(100, 130, Now.AddMinutes(-1), Now),
            OperationalDashboardAiOutcomeInput.Unknown(Now.AddMinutes(-1)),
            Now,
            "correlation-alpha");

        overview.PublishedSlos.ShouldNotBeNull();
        overview.PublishedSlos!.ShouldNotBeEmpty();

        OperationalDashboardReadPolicy.Evaluate(Principal("operations-admin"), aggregationCount: overview.Views.Count, auditThreshold: 10, auditAvailable: true)
            .IsAllowed.ShouldBeTrue();

        foreach (ClaimsPrincipal nonHuman in new[] { Principal("operations-admin", "service"), Principal("operations-admin", "ai") })
        {
            AdminQueueSummaryReadDecision denied = OperationalDashboardReadPolicy.Evaluate(nonHuman, aggregationCount: overview.Views.Count, auditThreshold: 10, auditAvailable: true);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        OperationalDashboardReadPolicy.Evaluate(PrincipalWithoutRole(), aggregationCount: overview.Views.Count, auditThreshold: 10, auditAvailable: true)
            .IsAllowed.ShouldBeFalse();
    }

    private static OperationalDashboardView ViewFor(OperationalDashboardOverview overview, DashboardObservabilityView view)
        => overview.Views.Single(row => row.View == view);

    private static AdminQueueSummaryProjectionItem[] SampleItems()
        =>
        [
            QueueItem(OperationalQueueFamily.FailedIngestion, "item:ingestion-001", ChatBotHealthStatus.Degraded),
            QueueItem(OperationalQueueFamily.FailedAttachment, "item:attachment-001", ChatBotHealthStatus.Healthy),
            QueueItem(OperationalQueueFamily.AmbiguousAssociation, "item:ambiguous-001", ChatBotHealthStatus.Healthy),
            QueueItem(OperationalQueueFamily.PendingApproval, "item:approval-001", ChatBotHealthStatus.Healthy),
            QueueItem(OperationalQueueFamily.RetryableOperation, "item:retry-001", ChatBotHealthStatus.Healthy),
        ];

    private static AdminQueueSummaryProjectionItem QueueItem(
        OperationalQueueFamily family,
        string itemRef,
        ChatBotHealthStatus health)
        => new(
            QueueRef: $"queue:{OperationalQueueFamilies.ToWireValue(family)}",
            ItemRef: itemRef,
            Status: "waiting",
            OwnerClass: "operations",
            Health: health,
            AgeSeconds: 120,
            QueueFamily: family,
            Risk: "medium",
            Confidence: 0.5m,
            AssigneeRef: "admin:reviewer-a",
            NextAction: "claim",
            RetryCount: 0,
            FreshnessTimestampUtc: Now.AddMinutes(-1),
            OwnerRole: "operations-admin",
            MailboxRef: "mailbox:ops",
            FailureState: "waiting",
            SourceVersion: 1,
            PriorityScore: 10,
            PriorityExplanation: "risk-age-source",
            ProjectName: "Project Alpha",
            EvidenceContent: "evidence content sentinel",
            FileMetadata: "file-secret.pdf",
            AuditReason: "raw audit reason",
            MailboxSubject: "customer subject",
            CandidateEvidence: "candidate evidence");

    private static ClaimsPrincipal Principal(string role, string actorType = "human")
        => new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
            ],
            "test"));

    private static ClaimsPrincipal PrincipalWithoutRole()
        => new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, "human"),
            ],
            "test"));
}
