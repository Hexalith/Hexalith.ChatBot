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
        PeriodicEnforcementRunOutcome outcome = await coordinator.RunOnceAsync("m2-partial", cancellationToken);
        await coordinator.CheckHealthAsync("m2-partial-health", cancellationToken);

        // Fail isolation: the throwing WORM sweep is recorded and does not abort the pass.
        outcome.EvaluatorsFailed.ShouldBe(1);
        outcome.AuditChainVerification.ShouldBeNull();
        outcome.AuditChainVerificationExecution.ShouldBe(M2SweepExecution.Failed);
        coordinator.Status.EvaluatorFailureCounts["worm-audit-chain"].ShouldBe(1);
        coordinator.Status.M2SweepStatuses["worm-audit-chain"].LastRanAtUtc.ShouldBe(clock.UtcNow);
        coordinator.Status.M2SweepStatuses["worm-audit-chain"].LastSucceededAtUtc.ShouldBeNull();

        // Independence, the direction that matters: the other two sweeps still ran to completion and still emitted
        // their own breach alert while a sibling sweep was failing.
        outcome.ReplayIsolationProbe.ShouldNotBeNull().Breaches.ShouldBe(1);
        outcome.ReplayIsolationProbeExecution.ShouldBe(M2SweepExecution.Completed);
        outcome.DerivedStoreIsolationProbe.ShouldNotBeNull().Breaches.ShouldBe(0);
        outcome.DerivedStoreIsolationProbeExecution.ShouldBe(M2SweepExecution.Completed);
        alerts.Alerts.ShouldContain(alert => alert.Kind == OperatorAlertKind.ReplayIsolationBreach);

        // A failed sweep is alerted immediately on its own reason code rather than waiting out the cadence budget —
        // a process that restarts more often than that budget would otherwise never report it.
        alerts.Alerts.ShouldContain(alert => alert.ReasonCode == "m2_worm_verify_sweep_failed");

        // ...and the failure must NOT bleed into the other sweeps' signals. The earlier spelling of this test advanced
        // the clock past the budget before the first run, so the process-start baseline alone tripped the assertion —
        // it would have passed even if the WORM sweep had never failed, and it never checked the siblings at all.
        alerts.Alerts.ShouldNotContain(alert => alert.ReasonCode == "m2_replay_isolation_sweep_failed");
        alerts.Alerts.ShouldNotContain(alert => alert.ReasonCode == "m2_derived_store_isolation_sweep_failed");
        alerts.Alerts.ShouldNotContain(alert => alert.ReasonCode == "m2_replay_isolation_missed_cadence");
        alerts.Alerts.ShouldNotContain(alert => alert.ReasonCode == "m2_derived_store_isolation_missed_cadence");
    }

    [Fact]
    public async Task RunOnceAsyncShouldRetryAFailedSweepWithinItsPartitionAndAlertOnceItsCadenceBudgetLapses()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        InMemoryAuditWriter auditWriter = new();

        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new StaticInputSource([], EmptyInputs()),
            alerts,
            new AuditChainVerificationCoordinator(new ThrowingTenantEnumerationWormStore(), auditWriter, alerts, clock),
            new ReplayIsolationProbeCoordinator(new InMemoryOutboundTraceStore(), new InMemoryWormAuditStore(), auditWriter, alerts, clock),
            new DerivedStoreIsolationProbeCoordinator(new InMemoryDerivedStore(), auditWriter, alerts, clock),
            new PeriodicEnforcementOptions
            {
                RunM2AuditRecoverySweeps = true,
                M2SweepCadence = PeriodicEnforcementOptions.DefaultM2SweepCadence,
                MissedCadenceAlertAfter = TimeSpan.FromMinutes(5),
                M2SweepRetryAfter = TimeSpan.FromMinutes(15),
            });

        PeriodicEnforcementRunOutcome first = await coordinator.RunOnceAsync("m2-retry-1", cancellationToken);
        first.AuditChainVerificationExecution.ShouldBe(M2SweepExecution.Failed);

        // Still inside the retry backoff: the sweep is skipped rather than hammered every tick.
        clock.UtcNow = Now + TimeSpan.FromMinutes(5);
        PeriodicEnforcementRunOutcome tooSoon = await coordinator.RunOnceAsync("m2-retry-2", cancellationToken);
        tooSoon.AuditChainVerificationExecution.ShouldBe(M2SweepExecution.Skipped);
        coordinator.Status.EvaluatorFailureCounts["worm-audit-chain"].ShouldBe(1);

        // Past the backoff and still inside the same UTC day: the failed sweep is retried. The partition was never
        // committed, so a transient failure cannot cost the whole period.
        clock.UtcNow = Now + TimeSpan.FromMinutes(20);
        PeriodicEnforcementRunOutcome retried = await coordinator.RunOnceAsync("m2-retry-3", cancellationToken);
        retried.AuditChainVerificationExecution.ShouldBe(M2SweepExecution.Failed);
        coordinator.Status.EvaluatorFailureCounts["worm-audit-chain"].ShouldBe(2);

        // Inside the budget, measured from when monitoring began: still silent.
        clock.UtcNow = Now + PeriodicEnforcementOptions.DefaultM2SweepCadence;
        await coordinator.CheckHealthAsync("m2-retry-health-early", cancellationToken);
        alerts.Alerts.ShouldNotContain(alert => alert.ReasonCode == "m2_worm_verify_missed_cadence");

        // Once the budget lapses the miss becomes observable — and it is de-duplicated, so a permanently failing
        // nightly sweep does not append an identical alert on every tick for a full day.
        //
        // The budget runs from the last *success* (or, as here, from when monitoring began), NOT from the last
        // attempt. An earlier spelling measured from the last attempt, which meant a sweep retrying every 15 minutes
        // refreshed its own baseline forever and this alert could never fire for the one case it exists to catch:
        // a sweep that is being attempted and failing every single time. See
        // CheckHealthAsyncShouldReportASweepThatHasNeverSucceededOnceItsBudgetLapses.
        clock.UtcNow = Now + PeriodicEnforcementOptions.DefaultM2SweepCadence + TimeSpan.FromMinutes(30);
        await coordinator.CheckHealthAsync("m2-retry-health-1", cancellationToken);
        await coordinator.CheckHealthAsync("m2-retry-health-2", cancellationToken);

        alerts.Alerts.Count(alert => alert.ReasonCode == "m2_worm_verify_missed_cadence").ShouldBe(1);
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

    [Fact]
    public void M2ReleaseGateShouldTreatAnythingLessThanAVerifiedCleanSweepAsStopShip()
    {
        TimeSpan maximumResultAge = PeriodicEnforcementOptions.DefaultM2SweepCadence + TimeSpan.FromMinutes(5);
        PeriodicEnforcementM2SweepStatus NeverCompleted()
            => new(Now, "run-1", null, null, null, null, null, LastAttemptCompletedSuccessfully: false);
        PeriodicEnforcementM2SweepStatus Completed(int breaches, int coverage)
            => new(Now, "run-1", Now, "run-1", breaches, coverage, coverage, LastAttemptCompletedSuccessfully: true);

        PeriodicEnforcementRunStatus Status(Dictionary<string, PeriodicEnforcementM2SweepStatus> sweeps)
            => new(false, Now, Now, null, TimeSpan.Zero, 0, new Dictionary<string, int>(StringComparer.Ordinal), "c", null, sweeps);

        PeriodicEnforcementM2ReleaseGateResponse Gate(
            Dictionary<string, PeriodicEnforcementM2SweepStatus> sweeps,
            bool enabled = true,
            DateTimeOffset? evaluatedAtUtc = null)
            => PeriodicEnforcementM2ReleaseGateResponse.From(
                Status(sweeps),
                enabled,
                evaluatedAtUtc ?? Now,
                maximumResultAge);

        Dictionary<string, PeriodicEnforcementM2SweepStatus> clean = new(StringComparer.Ordinal)
        {
            [M2SweepJobs.WormAuditChain] = Completed(0, 3),
            [M2SweepJobs.ReplayIsolationProbe] = Completed(0, 3),
            [M2SweepJobs.DerivedStoreIsolationProbe] = Completed(0, 6),
        };

        // The only releasable state: every sweep completed, zero breaches, non-zero coverage.
        PeriodicEnforcementM2ReleaseGateResponse pass = Gate(clean);
        pass.IsStopShip.ShouldBeFalse();
        pass.StopShipReasons.ShouldBeEmpty();

        // Sweeps disabled — nothing was ever scheduled, so there is no evidence to release on.
        PeriodicEnforcementM2ReleaseGateResponse disabled = Gate(clean, enabled: false);
        disabled.IsStopShip.ShouldBeTrue();
        disabled.StopShipReasons.ShouldContain("m2_sweeps_disabled");

        // No status recorded at all for any sweep.
        PeriodicEnforcementM2ReleaseGateResponse empty = Gate(
            new Dictionary<string, PeriodicEnforcementM2SweepStatus>(StringComparer.Ordinal));
        empty.IsStopShip.ShouldBeTrue();
        empty.StopShipReasons.ShouldContain($"{M2SweepJobs.WormAuditChain}:never_completed");
        empty.M2SweepStatuses.Count.ShouldBe(3);

        // Attempted but never succeeded: LastBreaches stays null because only a success writes it.
        Dictionary<string, PeriodicEnforcementM2SweepStatus> attempted = new(clean, StringComparer.Ordinal)
        {
            [M2SweepJobs.WormAuditChain] = NeverCompleted(),
        };
        PeriodicEnforcementM2ReleaseGateResponse neverCompleted = Gate(attempted);
        neverCompleted.IsStopShip.ShouldBeTrue();
        neverCompleted.StopShipReasons.ShouldContain($"{M2SweepJobs.WormAuditChain}:never_completed");

        // A newer attempt invalidates the prior clean verdict immediately, while it is running and after it fails.
        Dictionary<string, PeriodicEnforcementM2SweepStatus> newerIncompleteAttempt = new(clean, StringComparer.Ordinal)
        {
            [M2SweepJobs.WormAuditChain] = new(
                Now.AddMinutes(1),
                "run-2",
                Now,
                "run-1",
                0,
                3,
                3,
                LastAttemptCompletedSuccessfully: false),
        };
        PeriodicEnforcementM2ReleaseGateResponse incomplete = Gate(
            newerIncompleteAttempt,
            evaluatedAtUtc: Now.AddMinutes(1));
        incomplete.IsStopShip.ShouldBeTrue();
        incomplete.StopShipReasons.ShouldContain($"{M2SweepJobs.WormAuditChain}:latest_attempt_incomplete");

        // A clean result expires if the scheduler stops producing evidence.
        PeriodicEnforcementM2ReleaseGateResponse stale = Gate(
            clean,
            evaluatedAtUtc: Now + maximumResultAge + TimeSpan.FromTicks(1));
        stale.IsStopShip.ShouldBeTrue();
        stale.StopShipReasons.ShouldContain($"{M2SweepJobs.WormAuditChain}:stale_result");

        // Succeeded while examining nothing — "verified clean" and "never checked" must not be the same answer. Three
        // tenants were present (so six ordered pairs existed to probe) yet nothing was probed: a real anomaly, not the
        // structural single-tenant case exempted further down.
        Dictionary<string, PeriodicEnforcementM2SweepStatus> vacuous = new(clean, StringComparer.Ordinal)
        {
            [M2SweepJobs.DerivedStoreIsolationProbe] = new(
                Now,
                "run-1",
                Now,
                "run-1",
                0,
                0,
                3,
                LastAttemptCompletedSuccessfully: true),
        };
        PeriodicEnforcementM2ReleaseGateResponse zeroCoverage = Gate(vacuous);
        zeroCoverage.IsStopShip.ShouldBeTrue();
        zeroCoverage.StopShipReasons.ShouldContain($"{M2SweepJobs.DerivedStoreIsolationProbe}:zero_coverage");
        zeroCoverage.M2SweepStatuses[M2SweepJobs.DerivedStoreIsolationProbe].HasCoverage.ShouldBeFalse();

        // The one exemption, and its boundaries. Exactly one positively observed tenant cannot form an ordered pair.
        // An empty population is not exempt: it is also what an absent or misbound derived store reports.
        PeriodicEnforcementM2SweepStatus CompletedWithPopulation(int coverage, int population)
            => new(Now, "run-1", Now, "run-1", 0, coverage, population, LastAttemptCompletedSuccessfully: true);

        Dictionary<string, PeriodicEnforcementM2SweepStatus> emptyPopulation = new(clean, StringComparer.Ordinal)
        {
            [M2SweepJobs.DerivedStoreIsolationProbe] = CompletedWithPopulation(0, 0),
        };
        Gate(emptyPopulation).StopShipReasons.ShouldContain($"{M2SweepJobs.DerivedStoreIsolationProbe}:zero_coverage");

        Dictionary<string, PeriodicEnforcementM2SweepStatus> singleTenant = new(clean, StringComparer.Ordinal)
        {
            [M2SweepJobs.DerivedStoreIsolationProbe] = CompletedWithPopulation(0, 1),
        };
        PeriodicEnforcementM2ReleaseGateResponse exempt = Gate(singleTenant);
        exempt.IsStopShip.ShouldBeFalse("one positively observed tenant cannot form an ordered pair");
        exempt.StopShipReasons.ShouldBeEmpty();

        // At two tenants pairs exist, so zero coverage is a real anomaly again and the gate re-arms by itself.
        Dictionary<string, PeriodicEnforcementM2SweepStatus> reArmed = new(clean, StringComparer.Ordinal)
        {
            [M2SweepJobs.DerivedStoreIsolationProbe] = CompletedWithPopulation(0, 2),
        };
        PeriodicEnforcementM2ReleaseGateResponse armed = Gate(reArmed);
        armed.IsStopShip.ShouldBeTrue();
        armed.StopShipReasons.ShouldContain($"{M2SweepJobs.DerivedStoreIsolationProbe}:zero_coverage");

        // The exemption is scoped to that probe only — the WORM and replay sweeps enumerate single tenants and have
        // no structural floor, so zero coverage from them is always stop-ship regardless of population.
        Dictionary<string, PeriodicEnforcementM2SweepStatus> wormVacuous = new(clean, StringComparer.Ordinal)
        {
            [M2SweepJobs.WormAuditChain] = CompletedWithPopulation(0, 1),
        };
        PeriodicEnforcementM2ReleaseGateResponse wormZero = Gate(wormVacuous);
        wormZero.IsStopShip.ShouldBeTrue();
        wormZero.StopShipReasons.ShouldContain($"{M2SweepJobs.WormAuditChain}:zero_coverage");

        // And the case it always caught.
        Dictionary<string, PeriodicEnforcementM2SweepStatus> breached = new(clean, StringComparer.Ordinal)
        {
            [M2SweepJobs.ReplayIsolationProbe] = Completed(2, 3),
        };
        PeriodicEnforcementM2ReleaseGateResponse breach = Gate(breached);
        breach.IsStopShip.ShouldBeTrue();
        breach.StopShipReasons.ShouldContain($"{M2SweepJobs.ReplayIsolationProbe}:breaches_detected");
        breach.M2SweepStatuses[M2SweepJobs.ReplayIsolationProbe].HasBreaches.ShouldBeTrue();
    }

    [Fact]
    public void StatusStoreShouldInvalidateAPreviousCleanVerdictWhenANewerAttemptStarts()
    {
        InMemoryPeriodicEnforcementStatusStore store = new();
        store.RecordM2SweepRan(M2SweepJobs.WormAuditChain, Now, "run-1");
        store.RecordM2SweepSucceeded(M2SweepJobs.WormAuditChain, Now, "run-1", breaches: 0, coverage: 2, population: 2);
        store.RecordM2SweepRan(M2SweepJobs.WormAuditChain, Now.AddMinutes(1), "run-2");

        PeriodicEnforcementM2SweepStatus status = store.Read().M2SweepStatuses[M2SweepJobs.WormAuditChain];

        status.LastSuccessCorrelationId.ShouldBe("run-1");
        status.LastRunCorrelationId.ShouldBe("run-2");
        status.LastPopulation.ShouldBe(2);
        status.LastAttemptCompletedSuccessfully.ShouldBeFalse();
    }

    [Fact]
    public void AnonymousLivenessPayloadShouldCarryNoM2State()
    {
        // The anonymous endpoint must not disclose whether isolation or the WORM chain is currently broken. This is a
        // compile-time-enforced guarantee: the liveness record has no M2 members at all, so there is nothing to leak.
        PeriodicEnforcementRunStatus status = new(
            false,
            Now,
            Now,
            null,
            TimeSpan.Zero,
            0,
            new Dictionary<string, int>(StringComparer.Ordinal) { [M2SweepJobs.WormAuditChain] = 3 },
            "c",
            null,
            new Dictionary<string, PeriodicEnforcementM2SweepStatus>(StringComparer.Ordinal)
            {
                [M2SweepJobs.WormAuditChain] = new(
                    Now,
                    "run-1",
                    Now,
                    "run-1",
                    7,
                    3,
                    3,
                    LastAttemptCompletedSuccessfully: true),
            });

        PeriodicEnforcementHealthResponse liveness = PeriodicEnforcementHealthResponse.From(status);
        string serialized = System.Text.Json.JsonSerializer.Serialize(liveness);

        serialized.ShouldNotContain("Breach", Case.Insensitive);
        serialized.ShouldNotContain("StopShip", Case.Insensitive);
        serialized.ShouldNotContain("Coverage", Case.Insensitive);
        serialized.ShouldNotContain(M2SweepJobs.WormAuditChain);
        serialized.ShouldNotContain("7");
        liveness.IsRunning.ShouldBeFalse();
        liveness.LastSucceededAtUtc.ShouldBe(Now);
    }

    [Fact]
    public async Task RunOnceAsyncShouldContainATenantPhaseFailureAndStillRunM2Sweeps()
    {
        // The M2 coordinators self-enumerate their stores. A failure in ordinary tenant discovery must be recorded but
        // must not suppress all three process-global audit/isolation sweeps.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new ThrowingInputSource(),
            alerts,
            runtimeOptions: new PeriodicEnforcementOptions { RunM2AuditRecoverySweeps = true });

        PeriodicEnforcementRunOutcome outcome = await coordinator
            .RunOnceAsync("tenant-phase-fail-1", cancellationToken)
            .ConfigureAwait(true);

        outcome.EvaluatorsFailed.ShouldBe(1);
        outcome.AuditChainVerificationExecution.ShouldBe(M2SweepExecution.Completed);
        outcome.ReplayIsolationProbeExecution.ShouldBe(M2SweepExecution.Completed);
        outcome.DerivedStoreIsolationProbeExecution.ShouldBe(M2SweepExecution.Completed);
        coordinator.Status.EvaluatorFailureCounts["tenant-enforcement"].ShouldBe(1);

        await coordinator.CheckHealthAsync("pass-fail-health", cancellationToken);
        alerts.Alerts.ShouldContain(alert => alert.ReasonCode == "periodic_enforcement_pass_failed");

        // De-duplicated: a permanently failing pass ticks every minute and must not append an alert per tick.
        clock.UtcNow = Now.AddMinutes(1);
        await coordinator.CheckHealthAsync("pass-fail-health-2", cancellationToken);
        alerts.Alerts.Count(alert => alert.ReasonCode == "periodic_enforcement_pass_failed").ShouldBe(1);
    }

    [Fact]
    public void EnabledM2SweepsShouldRejectAHostTickSlowerThanTheirCadence()
    {
        PeriodicEnforcementOptions options = new()
        {
            RunM2AuditRecoverySweeps = true,
            Cadence = TimeSpan.FromDays(2),
            M2SweepCadence = TimeSpan.FromDays(1),
        };

        string validationError = options.Validate().ShouldNotBeNull();
        validationError.ShouldContain(nameof(PeriodicEnforcementOptions.Cadence));
    }

    [Fact]
    public async Task CheckHealthAsyncShouldReportASweepThatHasNeverSucceededOnceItsBudgetLapses()
    {
        // The baseline must not fall back to LastRanAtUtc: that field is refreshed on every attempt including
        // failures, so a sweep failing on its retry loop kept the baseline permanently fresh and this alert could
        // never fire — the exact "attempted and failed every time" case it exists for.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        InMemoryAuditWriter auditWriter = new();
        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new StaticInputSource([], EmptyInputs()),
            alerts,
            new AuditChainVerificationCoordinator(new ThrowingTenantEnumerationWormStore(), auditWriter, alerts, clock),
            runtimeOptions: new PeriodicEnforcementOptions
            {
                RunM2AuditRecoverySweeps = true,
                M2SweepCadence = PeriodicEnforcementOptions.DefaultM2SweepCadence,
                MissedCadenceAlertAfter = TimeSpan.FromMinutes(5),
                M2SweepRetryAfter = TimeSpan.FromMinutes(15),
            });

        // Keep attempting on the retry cadence across more than a full budget, exactly as the hosted loop does.
        for (int minutes = 0; minutes <= 1500; minutes += 15)
        {
            clock.UtcNow = Now.AddMinutes(minutes);
            _ = await coordinator.RunOnceAsync($"never-succeeded-{minutes}", cancellationToken);
        }

        coordinator.Status.M2SweepStatuses[M2SweepJobs.WormAuditChain].LastSucceededAtUtc.ShouldBeNull();
        coordinator.Status.M2SweepStatuses[M2SweepJobs.WormAuditChain].LastRanAtUtc.ShouldBe(clock.UtcNow);

        await coordinator.CheckHealthAsync("never-succeeded-health", cancellationToken);
        alerts.Alerts.ShouldContain(alert => alert.ReasonCode == "m2_worm_verify_missed_cadence");
    }

    [Fact]
    public async Task CheckHealthAsyncShouldEmitABreachAndAMissedCadenceAlertIndependentlyInTheSamePass()
    {
        // AC2's actual claim: a missed cadence and a detected breach are independent signals that must survive
        // together. The prior test named for this never advanced the clock, so no miss was ever emitted and every
        // cadence assertion was a ShouldNotContain — the co-occurrence went untested.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryOutboundTraceStore traceStore = new();
        await traceStore.RecordAsync(ReplayTraceRecord(Tenant), cancellationToken).ConfigureAwait(true);

        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new StaticInputSource([], EmptyInputs()),
            alerts,
            new AuditChainVerificationCoordinator(new ThrowingTenantEnumerationWormStore(), auditWriter, alerts, clock),
            new ReplayIsolationProbeCoordinator(traceStore, new InMemoryWormAuditStore(), auditWriter, alerts, clock),
            new DerivedStoreIsolationProbeCoordinator(new InMemoryDerivedStore(), auditWriter, alerts, clock),
            new PeriodicEnforcementOptions
            {
                RunM2AuditRecoverySweeps = true,
                M2SweepCadence = PeriodicEnforcementOptions.DefaultM2SweepCadence,
                MissedCadenceAlertAfter = TimeSpan.FromMinutes(5),
                M2SweepRetryAfter = TimeSpan.FromMinutes(15),
            });

        // Day 1: replay probe completes with a real breach; the WORM sweep throws.
        PeriodicEnforcementRunOutcome day1 = await coordinator.RunOnceAsync("indep-1", cancellationToken);
        day1.ReplayIsolationProbe.ShouldNotBeNull().Breaches.ShouldBe(1);
        day1.AuditChainVerificationExecution.ShouldBe(M2SweepExecution.Failed);

        // Move past the WORM sweep's budget while the replay probe stays healthy, then run a pass that produces a
        // fresh breach in the same tick that the WORM cadence is overdue.
        clock.UtcNow = Now + PeriodicEnforcementOptions.DefaultM2SweepCadence + TimeSpan.FromMinutes(30);
        PeriodicEnforcementRunOutcome day2 = await coordinator.RunOnceAsync("indep-2", cancellationToken);
        await coordinator.CheckHealthAsync("indep-2-health", cancellationToken);

        // Both signals present, in the same pass.
        day2.ReplayIsolationProbe.ShouldNotBeNull().Breaches.ShouldBe(1);
        alerts.Alerts.ShouldContain(alert => alert.Kind == OperatorAlertKind.ReplayIsolationBreach);
        alerts.Alerts.ShouldContain(alert => alert.ReasonCode == "m2_worm_verify_missed_cadence");

        // Independent means scoped: the healthy sweeps must not inherit the failing one's cadence alert.
        alerts.Alerts.ShouldNotContain(alert => alert.ReasonCode == "m2_replay_isolation_missed_cadence");
        alerts.Alerts.ShouldNotContain(alert => alert.ReasonCode == "m2_derived_store_isolation_missed_cadence");
    }

    [Fact]
    public async Task RunOnceAsyncShouldFailASweepThatExceedsItsTimeoutRatherThanBlockingThePass()
    {
        // Without a deadline a sweep that hangs (rather than throws) blocks the pass forever, and because
        // CheckHealthAsync runs only after the pass returns, it also blocks the detector meant to report the stall.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        InMemoryAuditWriter auditWriter = new();

        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new StaticInputSource([], EmptyInputs()),
            alerts,
            derivedStoreIsolationProbeCoordinator: new DerivedStoreIsolationProbeCoordinator(
                new HangingDerivedStore(),
                auditWriter,
                alerts,
                clock),
            runtimeOptions: new PeriodicEnforcementOptions
            {
                RunM2AuditRecoverySweeps = true,
                M2SweepCadence = PeriodicEnforcementOptions.DefaultM2SweepCadence,
                M2SweepTimeout = TimeSpan.FromMilliseconds(150),
            });

        PeriodicEnforcementRunOutcome outcome = await coordinator.RunOnceAsync("m2-timeout", cancellationToken);

        // The hang became an ordinary failure: isolated, alerted, partition uncommitted, pass still completed —
        // and critically, RunOnceAsync returned at all, so CheckHealthAsync downstream of it can still run.
        outcome.DerivedStoreIsolationProbeExecution.ShouldBe(M2SweepExecution.Failed);
        outcome.AuditChainVerificationExecution.ShouldBe(M2SweepExecution.Completed);
        outcome.ReplayIsolationProbeExecution.ShouldBe(M2SweepExecution.Completed);
        coordinator.Status.EvaluatorFailureCounts[M2SweepJobs.DerivedStoreIsolationProbe].ShouldBe(1);
        alerts.Alerts.ShouldContain(alert => alert.ReasonCode == "m2_derived_store_isolation_sweep_failed");
    }

    [Fact]
    public async Task RunOnceAsyncShouldNotCarryAFailedAttemptsBackoffIntoTheNextPartition()
    {
        // The backoff bounds retries *within* a partition, as the option's contract says. Applying it across the
        // boundary meant a sweep that failed late in one period also delayed the first attempt of the next.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MutableClock clock = new(Now);
        InMemoryOperatorAlertSink alerts = new();
        InMemoryAuditWriter auditWriter = new();
        RecoverableWormStore wormStore = new();

        PeriodicEnforcementCoordinator coordinator = BuildCoordinator(
            clock,
            new InMemoryGovernedControlStateProjectionStore(),
            new StaticInputSource([], EmptyInputs()),
            alerts,
            new AuditChainVerificationCoordinator(wormStore, auditWriter, alerts, clock),
            runtimeOptions: new PeriodicEnforcementOptions
            {
                RunM2AuditRecoverySweeps = true,
                M2SweepCadence = PeriodicEnforcementOptions.DefaultM2SweepCadence,
                M2SweepDayAnchorUtc = TimeSpan.Zero,
                M2SweepRetryAfter = TimeSpan.FromHours(6),
            });

        // Fail late in day 1 (Now is 12:00 UTC, so +11h30m is 23:30), well inside a 6h backoff that would otherwise
        // reach into day 2.
        clock.UtcNow = Now.AddHours(11).AddMinutes(30);
        PeriodicEnforcementRunOutcome failed = await coordinator.RunOnceAsync("boundary-1", cancellationToken);
        failed.AuditChainVerificationExecution.ShouldBe(M2SweepExecution.Failed);

        // Day 2 at 00:10, only 40 minutes later: a new partition owes a fresh attempt regardless of the backoff.
        wormStore.Recovered = true;
        clock.UtcNow = Now.AddHours(12).AddMinutes(10);
        PeriodicEnforcementRunOutcome nextPartition = await coordinator.RunOnceAsync("boundary-2", cancellationToken);
        nextPartition.AuditChainVerificationExecution.ShouldBe(M2SweepExecution.Completed);
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

    /// <summary>Enumerates tenants only after <see cref="Recovered"/> is set, so a partition boundary can be crossed
    /// between a failing attempt and a succeeding one.</summary>
    private sealed class RecoverableWormStore : IWormAuditStore
    {
        public bool Recovered { get; set; }

        public ValueTask<WormAuditAppendOutcome> AppendAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IReadOnlyList<WormAuditChainRecord> EnumerateChain(string tenantId) => [];

        public IReadOnlyList<string> EnumerateTenants()
            => Recovered ? [] : throw new InvalidOperationException("worm tenant enumeration unavailable");
    }

    /// <summary>
    /// A derived store whose round-trips never return — the realistic sweep-hang shape, and the one a timeout can
    /// actually bound. (A purely synchronous CPU-bound hang is not interruptible by any cancellation token; the
    /// deadline covers stalled I/O, which is what an unresponsive store does.)
    /// </summary>
    private sealed class HangingDerivedStore : IDerivedStore
    {
        public async ValueTask PutAsync(
            DerivedStoreClass cls,
            string tenantId,
            string resourceId,
            DerivedStoreEntry entry,
            CancellationToken cancellationToken)
            => await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);

        public async ValueTask<DerivedStoreEntry?> GetAsync(
            DerivedStoreClass cls,
            string tenantId,
            string resourceId,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            return null;
        }

        public ValueTask<bool> InvalidateAsync(
            DerivedStoreClass cls,
            string tenantId,
            string resourceId,
            CancellationToken cancellationToken) => ValueTask.FromResult(false);

        public IReadOnlyList<string> EnumerateResourceIds(DerivedStoreClass cls, string tenantId) => [];

        public IReadOnlyList<string> EnumerateTenants() => [Tenant, "tenant-beta"];
    }

    private sealed class ThrowingInputSource : IPeriodicEnforcementInputSource
    {
        public ValueTask<IReadOnlyList<string>> GetTenantRefsAsync(CancellationToken cancellationToken)
            => throw new InvalidOperationException("tenant projection store unavailable");

        public ValueTask<PeriodicEnforcementTenantInputs> GetTenantInputsAsync(string tenantRef, CancellationToken cancellationToken)
            => throw new InvalidOperationException("tenant projection store unavailable");
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
