using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Notifications;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Projections.DerivedStores;
using Hexalith.ChatBot.Server.Tests.Audit;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Operations.PeriodicEnforcement;

public sealed class PeriodicEnforcementCoordinatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);
    private const string Tenant = "tenant-alpha";
    private const string Correlation = "periodic-test";

    [Fact]
    public async Task RunOnceAsyncShouldRefreshTrustedActiveControlStateWithoutWipingRateLimitBudget()
    {
        MutableClock clock = new(Now);
        InMemoryGovernedControlStateProjectionStore controlStore = new();
        await controlStore.SaveAsync(new GovernedControlStateView(
            Tenant,
            GovernedControlSubjectClasses.ServiceClient,
            "svc-1",
            GovernedControlStateView.Active,
            RateLimitBudget: 4,
            GovernedControlStateView.RollingHour,
            SourceVersion: 7,
            Correlation,
            Now.AddHours(-1),
            Now.AddMinutes(-5),
            RevocationSensitive: false,
            [Now.AddMinutes(-10)]),
            TestContext.Current.CancellationToken);
        await controlStore.SaveAsync(new GovernedControlStateView(
            Tenant,
            GovernedControlSubjectClasses.ServiceClient,
            "svc-disabled",
            GovernedControlStateView.Disabled,
            RateLimitBudget: 4,
            GovernedControlStateView.RollingHour,
            SourceVersion: 8,
            Correlation,
            Now.AddHours(-1),
            Now.AddMinutes(-5),
            RevocationSensitive: false),
            TestContext.Current.CancellationToken);
        await controlStore.SaveAsync(new GovernedControlStateView(
            Tenant,
            GovernedControlSubjectClasses.ServiceClient,
            "svc-revocation",
            GovernedControlStateView.Active,
            RateLimitBudget: 4,
            GovernedControlStateView.RollingHour,
            SourceVersion: 9,
            Correlation,
            Now.AddHours(-1),
            Now.AddMinutes(-5),
            RevocationSensitive: true),
            TestContext.Current.CancellationToken);

        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            controlStore,
            new StaticInputSource([Tenant], EmptyInputs()));

        PeriodicEnforcementRunOutcome outcome = await coordinator.RunOnceAsync(Correlation, TestContext.Current.CancellationToken);

        outcome.ControlStateHeartbeats.ShouldBe(1);
        GovernedControlStateView active = (await controlStore.GetAsync(
            Tenant,
            GovernedControlSubjectClasses.ServiceClient,
            "svc-1",
            TestContext.Current.CancellationToken))!;
        active.LastUpdatedAtUtc.ShouldBe(Now);
        active.RateLimitBudget.ShouldBe(4);
        active.RateLimitWindow.ShouldBe(GovernedControlStateView.RollingHour);
        active.RecentAdmittedAtUtc.ShouldBe([Now.AddMinutes(-10)]);

        GovernedControlStateView disabled = (await controlStore.GetAsync(
            Tenant,
            GovernedControlSubjectClasses.ServiceClient,
            "svc-disabled",
            TestContext.Current.CancellationToken))!;
        disabled.LastUpdatedAtUtc.ShouldBe(Now.AddMinutes(-5));
        disabled.ControlState.ShouldBe(GovernedControlStateView.Disabled);

        GovernedControlStateView revocation = (await controlStore.GetAsync(
            Tenant,
            GovernedControlSubjectClasses.ServiceClient,
            "svc-revocation",
            TestContext.Current.CancellationToken))!;
        revocation.LastUpdatedAtUtc.ShouldBe(Now.AddMinutes(-5));
    }

    [Fact]
    public async Task RunOnceAsyncShouldUseRealRunbookSampleSizeAndAlertOnDefects()
    {
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        PeriodicEnforcementTenantInputs inputs = EmptyInputs() with
        {
            RunbookDiagnostics =
            [
                Diagnostic("item-complete"),
                Diagnostic("item-defect", currentState: RunbookDiagnosticCompletenessValidator.UnknownPlaceholder),
            ],
        };
        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new StaticInputSource([Tenant], inputs),
            alerts);

        PeriodicEnforcementRunOutcome outcome = await coordinator.RunOnceAsync(Correlation, TestContext.Current.CancellationToken);

        outcome.RunbookReport.Sampled.ShouldBe(2);
        outcome.RunbookReport.Complete.ShouldBe(1);
        outcome.RunbookReport.DefectWorkflowItemRefs.ShouldBe(["item-defect"]);
        alerts.Alerts.ShouldContain(alert => alert.ReasonCode == "runbook_diagnostic_defect_detected");
    }

    [Fact]
    public async Task RunOnceAsyncShouldSkipOverlappingPassAndEmitSchedulerAlert()
    {
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        BlockingInputSource inputSource = new([Tenant], EmptyInputs());
        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            inputSource,
            alerts);

        Task<PeriodicEnforcementRunOutcome> first = coordinator
            .RunOnceAsync("periodic-first", TestContext.Current.CancellationToken)
            .AsTask();
        await inputSource.WaitForInputsRequestedAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        PeriodicEnforcementRunOutcome overlap = await coordinator
            .RunOnceAsync("periodic-overlap", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        overlap.TenantsEvaluated.ShouldBe(0);
        overlap.EvaluatorsFailed.ShouldBe(1);
        coordinator.Status.SkippedOverlapCount.ShouldBe(1);
        alerts.Alerts.ShouldContain(alert => alert.ReasonCode == "periodic_enforcement_overlap_skipped");

        inputSource.Release();
        PeriodicEnforcementRunOutcome completed = await first.ConfigureAwait(true);
        completed.TenantsEvaluated.ShouldBe(1);
    }

    [Fact]
    public async Task RunOnceAsyncShouldSelectExactlyOneHundredRunbookDiagnosticsWhenMoreAreEligible()
    {
        MutableClock clock = new(Now);
        PeriodicEnforcementTenantInputs inputs = EmptyInputs() with
        {
            RunbookDiagnostics = Enumerable.Range(0, 120)
                .Select(index => Diagnostic($"item-{index:D3}"))
                .ToArray(),
        };
        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new StaticInputSource([Tenant], inputs));

        PeriodicEnforcementRunOutcome outcome = await coordinator.RunOnceAsync(Correlation, TestContext.Current.CancellationToken);

        outcome.RunbookReport.Sampled.ShouldBe(100);
        outcome.RunbookReport.Complete.ShouldBe(100);
        outcome.RunbookReport.DefectWorkflowItemRefs.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunOnceAsyncShouldSampleRunbookOncePerIsoWeekAndRecordMetadataOnlyEvidence()
    {
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        PeriodicEnforcementTenantInputs inputs = EmptyInputs() with
        {
            RunbookDiagnostics =
            [
                Diagnostic("item-complete"),
                Diagnostic("item-defect", currentState: RunbookDiagnosticCompletenessValidator.UnknownPlaceholder),
            ],
        };
        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new StaticInputSource([Tenant], inputs),
            alerts);

        PeriodicEnforcementRunOutcome first = await coordinator.RunOnceAsync("periodic-week-1", TestContext.Current.CancellationToken);

        first.RunbookReport.Sampled.ShouldBe(2);
        alerts.Alerts.Count(alert => alert.ReasonCode == "runbook_diagnostic_defect_detected").ShouldBe(1);

        // AC5: metadata-only NFR44 evidence is recorded for the weekly sweep (counts only, no tenant refs).
        PeriodicEnforcementRunbookEvidence evidence = coordinator.Status.LastRunbookSweep.ShouldNotBeNull();
        evidence.Sampled.ShouldBe(2);
        evidence.Complete.ShouldBe(1);
        evidence.DefectCount.ShouldBe(1);
        evidence.SweptAtUtc.ShouldBe(Now);
        evidence.CorrelationId.ShouldBe("periodic-week-1");

        // Same ISO week, later cadence tick → the sweep is gated: no re-sample, no duplicate defect alert.
        clock.UtcNow = Now.AddMinutes(1);
        PeriodicEnforcementRunOutcome second = await coordinator.RunOnceAsync("periodic-week-1b", TestContext.Current.CancellationToken);
        second.RunbookReport.Sampled.ShouldBe(0);
        alerts.Alerts.Count(alert => alert.ReasonCode == "runbook_diagnostic_defect_detected").ShouldBe(1);

        // A new ISO week → the sweep runs again and re-alerts.
        clock.UtcNow = Now.AddDays(8);
        PeriodicEnforcementRunOutcome third = await coordinator.RunOnceAsync("periodic-week-2", TestContext.Current.CancellationToken);
        third.RunbookReport.Sampled.ShouldBe(2);
        alerts.Alerts.Count(alert => alert.ReasonCode == "runbook_diagnostic_defect_detected").ShouldBe(2);
    }

    [Fact]
    public async Task RunOnceAsyncShouldRunM2SweepsOncePerUtcDayAndExposeTheirOutcomes()
    {
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryWormAuditStore wormStore = new();
        _ = await wormStore.AppendAsync(WormAuditTestData.Envelope(Tenant), TestContext.Current.CancellationToken);

        InMemoryDerivedStore derivedStore = new();
        await derivedStore.PutAsync(
            DerivedStoreClass.VectorIndex,
            Tenant,
            "resource-alpha",
            DerivedStoreEntry.Create("resource-alpha", "digest-alpha"),
            TestContext.Current.CancellationToken);
        await derivedStore.PutAsync(
            DerivedStoreClass.VectorIndex,
            "tenant-beta",
            "resource-beta",
            DerivedStoreEntry.Create("resource-beta", "digest-beta"),
            TestContext.Current.CancellationToken);

        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new StaticInputSource([], EmptyInputs()),
            alerts,
            new AuditChainVerificationCoordinator(wormStore, auditWriter, alerts, clock),
            new ReplayIsolationProbeCoordinator(new InMemoryOutboundTraceStore(), wormStore, auditWriter, alerts, clock),
            new DerivedStoreIsolationProbeCoordinator(derivedStore, auditWriter, alerts, clock),
            new PeriodicEnforcementOptions
            {
                RunM2AuditRecoverySweeps = true,
                M2SweepDayAnchorUtc = TimeSpan.Zero,
            });

        PeriodicEnforcementRunOutcome first = await coordinator.RunOnceAsync("m2-day-1", TestContext.Current.CancellationToken);
        first.AuditChainVerification.ShouldNotBeNull().TenantsChecked.ShouldBe(1);
        ReplayIsolationProbeOutcome firstReplayOutcome = first.ReplayIsolationProbe.ShouldNotBeNull();
        firstReplayOutcome.TenantsSwept.ShouldBe(1);
        firstReplayOutcome.Breaches.ShouldBe(0);
        DerivedStoreIsolationProbeOutcome firstDerivedOutcome = first.DerivedStoreIsolationProbe.ShouldNotBeNull();
        firstDerivedOutcome.PartitionsProbed.ShouldBe(2);
        firstDerivedOutcome.Breaches.ShouldBe(0);
        PeriodicEnforcementRunStatus firstStatus = coordinator.Status;
        firstStatus.M2SweepStatuses["worm-audit-chain"].LastRanAtUtc.ShouldBe(Now);
        firstStatus.M2SweepStatuses["worm-audit-chain"].LastSucceededAtUtc.ShouldBe(Now);
        firstStatus.M2SweepStatuses["replay-isolation-probe"].LastSucceededAtUtc.ShouldBe(Now);
        firstStatus.M2SweepStatuses["derived-store-isolation-probe"].LastSucceededAtUtc.ShouldBe(Now);

        clock.UtcNow = Now.AddMinutes(1);
        PeriodicEnforcementRunOutcome sameDay = await coordinator.RunOnceAsync("m2-day-1b", TestContext.Current.CancellationToken);
        sameDay.AuditChainVerification.ShouldBeNull();
        sameDay.ReplayIsolationProbe.ShouldBeNull();
        sameDay.DerivedStoreIsolationProbe.ShouldBeNull();

        clock.UtcNow = Now.AddHours(13);
        PeriodicEnforcementRunOutcome nextDay = await coordinator.RunOnceAsync("m2-day-2", TestContext.Current.CancellationToken);
        nextDay.AuditChainVerification.ShouldNotBeNull().TenantsChecked.ShouldBe(1);
        nextDay.ReplayIsolationProbe.ShouldNotBeNull().TenantsSwept.ShouldBe(1);
        nextDay.DerivedStoreIsolationProbe.ShouldNotBeNull().PartitionsProbed.ShouldBe(2);
    }

    [Fact]
    public async Task RunOnceAsyncShouldSurfaceEveryM2BreachAsAStopShipOutcomeAndAlert()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        InMemoryAuditWriter auditWriter = new();
        IReadOnlyList<WormAuditChainRecord> tamperedChain = await WormAuditTestData
            .BuildTamperedChainAsync(Tenant, length: 3, tamperAtSequence: 1)
            .ConfigureAwait(true);
        WormAuditTestData.StubWormAuditStore wormStore = new(Tenant, tamperedChain);
        InMemoryOutboundTraceStore traceStore = new();
        await traceStore.RecordAsync(ReplayTraceRecord(Tenant), cancellationToken).ConfigureAwait(true);
        LeakyDerivedStore derivedStore = new(Tenant, "tenant-beta");

        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new StaticInputSource([], EmptyInputs()),
            alerts,
            new AuditChainVerificationCoordinator(wormStore, auditWriter, alerts, clock),
            new ReplayIsolationProbeCoordinator(traceStore, wormStore, auditWriter, alerts, clock),
            new DerivedStoreIsolationProbeCoordinator(derivedStore, auditWriter, alerts, clock),
            new PeriodicEnforcementOptions { RunM2AuditRecoverySweeps = true });

        PeriodicEnforcementRunOutcome outcome = await coordinator.RunOnceAsync("m2-stop-ship", cancellationToken);

        outcome.AuditChainVerification.ShouldNotBeNull().Breaches.ShouldBe(1);
        outcome.ReplayIsolationProbe.ShouldNotBeNull().Breaches.ShouldBe(1);
        outcome.DerivedStoreIsolationProbe.ShouldNotBeNull().Breaches.ShouldBe(2);
        coordinator.Status.M2SweepStatuses["worm-audit-chain"].LastBreaches.ShouldBe(1);
        coordinator.Status.M2SweepStatuses["replay-isolation-probe"].LastBreaches.ShouldBe(1);
        coordinator.Status.M2SweepStatuses["derived-store-isolation-probe"].LastBreaches.ShouldBe(2);
        alerts.Alerts.ShouldContain(alert => alert.Kind == OperatorAlertKind.AuditChainBroken);
        alerts.Alerts.ShouldContain(alert => alert.Kind == OperatorAlertKind.ReplayIsolationBreach);
        alerts.Alerts.ShouldContain(alert => alert.Kind == OperatorAlertKind.DerivedStoreIsolationBreach);
    }

    [Fact]
    public async Task RunOnceAsyncShouldFailIsolateAThrowingSweepAndKeepBreachAndCadenceAlertsIndependent()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryWormAuditStore replayWormStore = new();
        InMemoryOutboundTraceStore traceStore = new();
        await traceStore.RecordAsync(ReplayTraceRecord(Tenant), cancellationToken).ConfigureAwait(true);

        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new StaticInputSource([], EmptyInputs()),
            alerts,
            new AuditChainVerificationCoordinator(new ThrowingTenantEnumerationWormStore(), auditWriter, alerts, clock),
            new ReplayIsolationProbeCoordinator(traceStore, replayWormStore, auditWriter, alerts, clock),
            new DerivedStoreIsolationProbeCoordinator(new InMemoryDerivedStore(), auditWriter, alerts, clock),
            new PeriodicEnforcementOptions
            {
                RunM2AuditRecoverySweeps = true,
                M2SweepCadence = PeriodicEnforcementOptions.DefaultM2SweepCadence,
                MissedCadenceAlertAfter = TimeSpan.FromMinutes(5),
            });
        clock.UtcNow = Now + PeriodicEnforcementOptions.DefaultM2SweepCadence + TimeSpan.FromMinutes(6);

        PeriodicEnforcementRunOutcome outcome = await coordinator.RunOnceAsync("m2-partial", cancellationToken);
        await coordinator.CheckHealthAsync("m2-partial-health", cancellationToken);

        outcome.EvaluatorsFailed.ShouldBe(1);
        outcome.AuditChainVerification.ShouldBeNull();
        outcome.ReplayIsolationProbe.ShouldNotBeNull().Breaches.ShouldBe(1);
        outcome.DerivedStoreIsolationProbe.ShouldNotBeNull().Breaches.ShouldBe(0);
        coordinator.Status.EvaluatorFailureCounts["worm-audit-chain"].ShouldBe(1);
        coordinator.Status.M2SweepStatuses["worm-audit-chain"].LastRanAtUtc.ShouldBe(clock.UtcNow);
        coordinator.Status.M2SweepStatuses["worm-audit-chain"].LastSucceededAtUtc.ShouldBeNull();
        alerts.Alerts.ShouldContain(alert => alert.Kind == OperatorAlertKind.ReplayIsolationBreach);
        alerts.Alerts.ShouldContain(alert => alert.ReasonCode == "m2_worm_verify_missed_cadence");
    }

    [Fact]
    public async Task CheckHealthAsyncShouldAlertForEveryM2SweepThatMissesItsCadenceBudget()
    {
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new StaticInputSource([], EmptyInputs()),
            alerts,
            runtimeOptions: new PeriodicEnforcementOptions
            {
                RunM2AuditRecoverySweeps = true,
                M2SweepCadence = PeriodicEnforcementOptions.DefaultM2SweepCadence,
                MissedCadenceAlertAfter = TimeSpan.FromMinutes(5),
            });

        await coordinator.CheckHealthAsync("m2-health-initial", TestContext.Current.CancellationToken);
        alerts.Alerts.ShouldNotContain(alert => alert.ReasonCode.StartsWith("m2_", StringComparison.Ordinal));

        clock.UtcNow = Now + PeriodicEnforcementOptions.DefaultM2SweepCadence + TimeSpan.FromMinutes(6);
        await coordinator.CheckHealthAsync("m2-health-overdue", TestContext.Current.CancellationToken);

        alerts.Alerts.ShouldContain(alert => alert.ReasonCode == "m2_worm_verify_missed_cadence");
        alerts.Alerts.ShouldContain(alert => alert.ReasonCode == "m2_replay_isolation_missed_cadence");
        alerts.Alerts.ShouldContain(alert => alert.ReasonCode == "m2_derived_store_isolation_missed_cadence");
    }

    [Fact]
    public async Task CheckHealthAsyncShouldEmitMissedCadenceAlertWithinFiveMinuteBound()
    {
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new StaticInputSource([], EmptyInputs()),
            alerts);

        await coordinator.CheckHealthAsync(Correlation, TestContext.Current.CancellationToken);

        alerts.Alerts.ShouldContain(alert => alert.ReasonCode == "periodic_enforcement_missed_cadence");
        alerts.Alerts.ShouldContain(alert => alert.FirstBreakLocator == "owner:operations-admin");
    }

    [Fact]
    public async Task CheckHealthAsyncShouldStaySilentWithinBoundThenAlertOnceOverdue()
    {
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new StaticInputSource([], EmptyInputs()),
            alerts);

        // A successful pass establishes last-observed = Now.
        _ = await coordinator.RunOnceAsync(Correlation, TestContext.Current.CancellationToken);

        // Within the 5-minute NFR41 bound → no missed-cadence alert.
        clock.UtcNow = Now.AddMinutes(4);
        await coordinator.CheckHealthAsync("health-within", TestContext.Current.CancellationToken);
        alerts.Alerts.ShouldNotContain(alert => alert.ReasonCode == "periodic_enforcement_missed_cadence");

        // Past the bound → the overdue alert fires off an injected clock, no sleeps.
        clock.UtcNow = Now.AddMinutes(6);
        await coordinator.CheckHealthAsync("health-overdue", TestContext.Current.CancellationToken);
        alerts.Alerts.ShouldContain(alert => alert.ReasonCode == "periodic_enforcement_missed_cadence");
    }

    [Fact]
    public async Task ProjectionBackedInputSourceShouldReadTenantScopedQueueDiagnosticsAndApprovalDecisionSamples()
    {
        InMemoryProjectConversationProjectionStore store = new();
        await store
            .UpsertApprovalEventAsync(ApprovalRequest(Tenant, "approval-alpha"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await store
            .UpsertApprovalEventAsync(ApprovalDecision(Tenant, "approval-alpha"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await store
            .UpsertApprovalEventAsync(ApprovalRequest("tenant-beta", "approval-beta"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        ProjectionBackedPeriodicEnforcementInputSource source = new(store, new MutableClock(Now));

        IReadOnlyList<string> tenants = await source
            .GetTenantRefsAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        PeriodicEnforcementTenantInputs inputs = await source
            .GetTenantInputsAsync(Tenant, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        tenants.ShouldBe(["tenant-alpha", "tenant-beta"]);
        AdminQueueSummaryProjectionItem queueItem = inputs.QueueItems.ShouldHaveSingleItem();
        queueItem.TenantRef.ShouldBe(Tenant);
        queueItem.ProjectName.ShouldBeNull();
        queueItem.EvidenceContent.ShouldBeNull();
        inputs.RunbookDiagnostics.ShouldHaveSingleItem().TenantRef.ShouldBe(Tenant);

        ApprovalDecisionSample sample = inputs.ApprovalDecisionSamples.ShouldHaveSingleItem();
        sample.TenantRef.ShouldBe(Tenant);
        sample.ReviewerRef.ShouldBe("reviewer-1");
        sample.DecisionKind.ShouldBe(ApprovalDecisionKind.Approve);
        sample.AiRiskClass.ShouldBe(AiActionRiskClass.ApprovalRequired);
    }

    private static PeriodicEnforcementCoordinator BuildCoordinator(
        MutableClock clock,
        IGovernedControlStateProjectionStore controlStore,
        IPeriodicEnforcementInputSource inputSource,
        InMemoryOperatorAlertSink? alerts = null,
        AuditChainVerificationCoordinator? auditChainVerificationCoordinator = null,
        ReplayIsolationProbeCoordinator? replayIsolationProbeCoordinator = null,
        DerivedStoreIsolationProbeCoordinator? derivedStoreIsolationProbeCoordinator = null,
        PeriodicEnforcementOptions? runtimeOptions = null)
    {
        InMemoryNotificationSink notificationSink = new();
        InMemoryNotificationDeliveryHistoryStore history = new();
        InMemoryNotificationDigestStore digest = new();
        InMemoryAuditWriter auditWriter = new();
        alerts ??= new InMemoryOperatorAlertSink();
        InMemoryWormAuditStore wormStore = new();
        InMemoryGovernedOperationProjectionStore operationStore = new();
        AuditCompletenessMeasurer measurer = new(wormStore, operationStore, clock);
        AuditCompletenessAlertCoordinator completenessAlert = new(wormStore, measurer, auditWriter, alerts, clock);
        SweepBackedAuditCompletenessSource completenessSource = new();
        CheckpointBackedAuditProjectionLagSource lagSource = new();
        auditChainVerificationCoordinator ??= new AuditChainVerificationCoordinator(wormStore, auditWriter, alerts, clock);
        replayIsolationProbeCoordinator ??= new ReplayIsolationProbeCoordinator(
            new InMemoryOutboundTraceStore(),
            wormStore,
            auditWriter,
            alerts,
            clock);
        derivedStoreIsolationProbeCoordinator ??= new DerivedStoreIsolationProbeCoordinator(
            new InMemoryDerivedStore(),
            auditWriter,
            alerts,
            clock);
        runtimeOptions ??= new PeriodicEnforcementOptions
        {
            ControlStateHeartbeatBeforeStale = TimeSpan.FromMinutes(4),
            MissedCadenceAlertAfter = TimeSpan.FromMinutes(5),
            RunbookSampleSize = 100,
        };

        return new PeriodicEnforcementCoordinator(
            inputSource,
            controlStore,
            new EscalationEvaluationCoordinator(notificationSink, auditWriter, clock),
            new NotificationThrottleCoordinator(notificationSink, history, digest, auditWriter, clock),
            new ReviewerBacklogAlertCoordinator(notificationSink, auditWriter, clock),
            new ApprovalRubberStampRateCoordinator(auditWriter, clock),
            new OperationalAlertWiringCoordinator(
                notificationSink,
                auditWriter,
                lagSource,
                new InMemoryRetryExhaustionAlertSource(),
                new InMemoryAuthorizationFailureCounter(clock),
                clock),
            auditChainVerificationCoordinator,
            replayIsolationProbeCoordinator,
            derivedStoreIsolationProbeCoordinator,
            measurer,
            completenessAlert,
            completenessSource,
            new UnavailableAuditProjectionCheckpointSource(),
            lagSource,
            alerts,
            new InMemoryPeriodicEnforcementStatusStore(),
            clock,
            Options.Create(runtimeOptions));
    }

    private static PeriodicEnforcementTenantInputs EmptyInputs()
        => new(
            QueueItems: [],
            RecipientCandidates: [],
            new EscalationPolicyChangeSet([]),
            NotificationDeliveries: [],
            NotificationThrottleCeilings.SafeDefaults,
            ReviewerBacklogThreshold.SafeDefault,
            ApprovalDecisionSamples: [],
            RunbookDiagnostics: []);

    private static OutboundTraceRecord ReplayTraceRecord(string tenantId)
        => new(
            tenantId,
            "project-001",
            "draft-001",
            "approval-001",
            "send-001",
            "requester-001",
            "actor-alpha",
            "AuthenticatedUserSend",
            "send",
            Correlation,
            "replay-run-001",
            Now);

    private static OperationalQueueDiagnostics Diagnostic(
        string itemRef,
        string currentState = "pending")
        => new(
            CorrelationId: "corr-1",
            TenantRef: Tenant,
            MailboxRef: "mailbox-1",
            WorkflowItemRef: itemRef,
            CurrentState: currentState,
            LastTransition: "from:request|actor:operator|at:1800000000",
            RetryCount: 0,
            FailureReason: null,
            NextSafeAction: "review");

    private static ApprovalEventView ApprovalRequest(string tenantRef, string approvalId)
        => new(
            TenantId: tenantRef,
            ProjectId: "project-1",
            ApprovalId: approvalId,
            EventKind: ApprovalEventKind.Request,
            Status: ApprovalStatus.Pending,
            OccurredAtUtc: Now.AddMinutes(-4),
            SourceVersion: 1,
            CorrelationId: "correlation-alpha",
            RequesterId: "requester-1",
            RequestedAtUtc: Now.AddMinutes(-4),
            CommandName: "Project.AppendConversationMessage",
            RiskClass: RiskClass.High,
            AiRiskClass: AiActionRiskClass.ApprovalRequired,
            PolicySnapshotVisibility: "metadata_only");

    private static ApprovalEventView ApprovalDecision(string tenantRef, string approvalId)
        => new(
            TenantId: tenantRef,
            ProjectId: "project-1",
            ApprovalId: approvalId,
            EventKind: ApprovalEventKind.Decision,
            Status: ApprovalStatus.Approved,
            OccurredAtUtc: Now.AddMinutes(-1),
            SourceVersion: 2,
            CorrelationId: "correlation-alpha",
            DecisionKind: ApprovalDecisionKind.Approve,
            DecisionActorId: "reviewer-1",
            DecidedAtUtc: Now.AddMinutes(-1),
            AiRiskClass: AiActionRiskClass.ApprovalRequired);

    private sealed class StaticInputSource(
        IReadOnlyList<string> tenants,
        PeriodicEnforcementTenantInputs inputs) : IPeriodicEnforcementInputSource
    {
        public ValueTask<IReadOnlyList<string>> GetTenantRefsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(tenants);
        }

        public ValueTask<PeriodicEnforcementTenantInputs> GetTenantInputsAsync(string tenantRef, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(inputs);
        }
    }

    private sealed class BlockingInputSource(
        IReadOnlyList<string> tenants,
        PeriodicEnforcementTenantInputs inputs) : IPeriodicEnforcementInputSource
    {
        private readonly TaskCompletionSource _inputsRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<IReadOnlyList<string>> GetTenantRefsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(tenants);
        }

        public async ValueTask<PeriodicEnforcementTenantInputs> GetTenantInputsAsync(
            string tenantRef,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = _inputsRequested.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return inputs;
        }

        public async Task WaitForInputsRequestedAsync(CancellationToken cancellationToken)
            => await _inputsRequested.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        public void Release()
            => _release.TrySetResult();
    }

    private sealed class ThrowingTenantEnumerationWormStore : IWormAuditStore
    {
        public ValueTask<WormAuditAppendOutcome> AppendAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IReadOnlyList<WormAuditChainRecord> EnumerateChain(string tenantId) => [];

        public IReadOnlyList<string> EnumerateTenants()
            => throw new InvalidOperationException("worm tenant enumeration unavailable");
    }

    private sealed class LeakyDerivedStore(params string[] tenants) : IDerivedStore
    {
        private readonly Dictionary<string, DerivedStoreEntry> _entries = new(StringComparer.Ordinal);

        public ValueTask PutAsync(
            DerivedStoreClass cls,
            string tenantId,
            string resourceId,
            DerivedStoreEntry entry,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _entries[$"{DerivedStorePartition.Segment(cls)}:{resourceId}"] = entry;
            return ValueTask.CompletedTask;
        }

        public ValueTask<DerivedStoreEntry?> GetAsync(
            DerivedStoreClass cls,
            string tenantId,
            string resourceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(_entries.GetValueOrDefault($"{DerivedStorePartition.Segment(cls)}:{resourceId}"));
        }

        public ValueTask<bool> InvalidateAsync(
            DerivedStoreClass cls,
            string tenantId,
            string resourceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(_entries.Remove($"{DerivedStorePartition.Segment(cls)}:{resourceId}"));
        }

        public IReadOnlyList<string> EnumerateResourceIds(DerivedStoreClass cls, string tenantId) => [];

        public IReadOnlyList<string> EnumerateTenants() => tenants;
    }

    private sealed class MutableClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;
    }
}
