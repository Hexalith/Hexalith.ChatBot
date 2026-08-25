using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Dapr.Client;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Projections;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Tier-3 proof that continuity, projection-rebuild, and scoped-outage coordinators run live fault injection and pass
/// the evidence gate.
/// </summary>
[Trait("Category", "E2E")]
[Collection(LiveRecoveryValidationCollection.Name)]
public sealed class LiveContinuityAspireE2eTests
{
    private static readonly string[] DaprInternalGrpcAppIds =
    [
        "eventstore",
        "tenants",
        "chatbot",
        "eventstore-admin",
        "eventstore-admin-ui",
    ];

    [Fact]
    public async Task LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate()
    {
        RequireTier3Runtime();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tenantRef = RecoveryValidationTopology.LogicalTenantRef;
        // Minted from a CSPRNG, not from the ULID correlation-id generator. ULIDs are time-ordered with a predictable
        // timestamp prefix and the same generator produces the runId that is published in evidence artifacts, so a
        // ULID-derived secret leaked far more structure than a credential should.
        string controllerSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        // Distinct from the controller secret on purpose: the fault-injection header must never double as the
        // Keycloak confidential-client secret that mints CaptureMailboxMessageIntake service-client tokens.
        string mailboxClientSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        string runId = ChatBotCorrelationId.New().Value;
        using PortReservationSet internalGrpcReservations = PortReservationSet.Reserve(DaprInternalGrpcAppIds.Length);
        List<string> arguments =
        [
            "--LiveRecoveryValidation:Enabled=true",
            "--LiveRecoveryValidation:EnvironmentName=Testing",
            $"--LiveRecoveryValidation:TestTenantRef={tenantRef}",
            $"--LiveRecoveryValidation:StorageTenantRef={RecoveryValidationTopology.StorageTenantRef}",
            $"--LiveRecoveryValidation:ControllerCapability={LiveRecoveryValidationOptions.AspireControllerCapability}",
            $"--LiveRecoveryValidation:ControllerSecret={controllerSecret}",

            // ChatBot:LiveRecoveryValidation:MailboxClientSecret is the primary key (aligned with the Server
            // section binding); the legacy AppHost-only LiveRecoveryValidation:MailboxClientSecret key is also
            // supplied so both resolution paths stay exercised.
            $"--ChatBot:LiveRecoveryValidation:MailboxClientSecret={mailboxClientSecret}",
            $"--LiveRecoveryValidation:MailboxClientSecret={mailboxClientSecret}",
        ];
        arguments.AddRange(DaprInternalGrpcAppIds.Select((appId, index) =>
            $"--Dapr:InternalGrpcPorts:{appId}={internalGrpcReservations.Ports[index]}"));
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(arguments.ToArray(), cancellationToken)
            .ConfigureAwait(true);
        _ = builder.AddRecoverySandbox(
            "Testing",
            tenantRef,
            RecoveryValidationTopology.StorageTenantRef,
            LiveRecoveryValidationOptions.AspireControllerCapability,
            controllerSecret,
            runId);
        IResource eventStore = builder.Resources.Single(resource => string.Equals(resource.Name, "eventstore", StringComparison.Ordinal));
        IResource security = builder.Resources.Single(resource => string.Equals(resource.Name, "security", StringComparison.Ordinal));
        IResource chatBot = builder.Resources.Single(resource => string.Equals(resource.Name, "chatbot", StringComparison.Ordinal));
        LiveRecoveryTopologyConfiguration.ConfigureEventStore(eventStore);
        chatBot.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["ChatBot__Projection__Topic"] = $"{RecoveryValidationTopology.StorageTenantRef}.chatbot.events";
            context.EnvironmentVariables["ChatBot__Projection__DeadLetterTopic"] = $"deadletter.{RecoveryValidationTopology.StorageTenantRef}.chatbot.events";
        }));

        DistributedApplication application = await builder.BuildAsync(cancellationToken).ConfigureAwait(true);
        IReadOnlyList<string>? rebuildFreshKeys = null;
        IReadModelConditionalEraser? rebuildEraser = null;
        TimeSpan rebuildEraseTimeout = TimeSpan.FromMinutes(3);
        string? freshPartitionTenant = null;
        try
        {
            using CancellationTokenSource startup = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            startup.CancelAfter(TimeSpan.FromMinutes(5));
            internalGrpcReservations.Release();
            await application.StartAsync(startup.Token).ConfigureAwait(true);

            // Before the readiness waits, because readiness depends on it: a fresh store reports EventStore
            // Unhealthy until the store-global writer protocol is activated, so waiting for health first would
            // deadlock. Driven through the same admin endpoint a real deployment uses.
            await RecoveryWriterProtocolProvisioner
                .ActivateAsync(application, ResolvedRepositoryCommit(), startup.Token)
                .ConfigureAwait(true);

            foreach (string resource in new[] { "security", "eventstore", "chatbot", "recovery-sandbox" })
            {
                await application.ResourceNotifications.WaitForResourceHealthyAsync(resource, startup.Token).ConfigureAwait(true);
            }

            // Diagnostic only: without a retained tail of the composed resources' own logs, a startup failure on this
            // lane reported an HTTP status with no cause, and the recovery topology's 503 could not be attributed.
            ResourceLoggerService resourceLoggers = application.Services.GetRequiredService<ResourceLoggerService>();
            RecoveryResourceLogTail chatBotLogTail = await RecoveryResourceLogTail
                .StartAsync("chatbot", resourceLoggers.WatchAsync("chatbot"), 400, startup.Token)
                .ConfigureAwait(true);
            await using ConfiguredAsyncDisposable chatBotLogTailScope = chatBotLogTail.ConfigureAwait(true);
            RecoveryResourceLogTail eventStoreLogTail = await RecoveryResourceLogTail
                .StartAsync("eventstore", resourceLoggers.WatchAsync("eventstore"), 200, startup.Token)
                .ConfigureAwait(true);
            await using ConfiguredAsyncDisposable eventStoreLogTailScope = eventStoreLogTail.ConfigureAwait(true);
            string ServerDiagnostics() => string.Join(
                Environment.NewLine,
                chatBotLogTail.Render(),
                eventStoreLogTail.Render());

            string mailboxAccessToken = await RecoveryAccessTokenProvider
                .AcquireMailboxAsync(application, mailboxClientSecret, startup.Token)
                .ConfigureAwait(true);
            string controlAccessToken = await RecoveryAccessTokenProvider
                .AcquireControlAsync(application, startup.Token)
                .ConfigureAwait(true);
            await AssertMailboxTokenAdmissionAsync(application, mailboxAccessToken, ServerDiagnostics, startup.Token)
                .ConfigureAwait(true);
            await AssertInvalidMailboxBearerIsRejectedBeforeAdmissionAsync(application, startup.Token).ConfigureAwait(true);
            using DaprClient recoveryDaprClient = new DaprClientBuilder()
                .UseGrpcEndpoint(application.GetEndpoint("chatbot-dapr-cli", "grpc").ToString())
                .UseHttpEndpoint(application.GetEndpoint("chatbot-dapr-cli", "http").ToString())
                .Build();
            DaprReadModelStore recoveryReadModels = new(recoveryDaprClient);
            rebuildEraser = recoveryReadModels;
            using EventStoreDurableStateProbe durableState = new(
                application.GetEndpoint("eventstore-dapr-cli", "http"));
            string evidenceDirectory = Path.Combine(RepositoryRoot(), "TestResults", "live-recovery", runId);
            LiveRecoveryValidationOptions options = new()
            {
                Enabled = true,
                EnvironmentName = "Testing",
                TestTenantRef = tenantRef,
                DatasetRef = "recovery-baseline",
                DatasetVersion = "v1",
                DatasetVolume = 6,
                ProjectionSchemaVersion = "chatbot.project-conversation-source-email.v1",
                ValidationPartitionRef = "recovery-partition-v1",
                ControllerCapability = LiveRecoveryValidationOptions.AspireControllerCapability,
                ControllerSecret = controllerSecret,
                // A reachable per-scenario budget rather than the 4-hour recovery target: nine serial scenarios plus
                // topology margin have to fit inside WorkflowTimeout, so a 4-hour per-scenario budget was nominal and
                // silently truncated by the outer deadline. RestorationTimeout remains the lane's measurable recovery
                // ceiling and is published in every manifest.
                PerScenarioTimeout = TimeSpan.FromMinutes(25),
                RestorationTimeout = TimeSpan.FromMinutes(3),
                WorkflowTimeout = RecoveryWorkflowTimeout(
                    Environment.GetEnvironmentVariable("HEXALITH_CHATBOT_RECOVERY_WORKFLOW_TIMEOUT_MINUTES")),
                EvidenceDirectory = evidenceDirectory,
                EvidenceLocator = EvidenceArtifactLocator(),
            };
            options.Validate().ShouldBeNull();
            rebuildEraseTimeout = options.RestorationTimeout;

            // Enforce the configured WorkflowTimeout in-process. Without it the only real bound was the GitHub
            // `timeout-minutes: 330`, so a hung scenario was killed by the runner mid-injection with no `finally`
            // reached — leaving EventStore or Keycloak stopped instead of failing closed with cleanup.
            using CancellationTokenSource workflowDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            workflowDeadline.CancelAfter(options.WorkflowTimeout);
            CancellationToken workflowToken = workflowDeadline.Token;
            AspireRecoverySandboxOperations operations = new(
                application,
                eventStore,
                controllerSecret,
                mailboxClientSecret,
                recoveryReadModels,
                recoveryReadModels,
                durableState);
            LiveContinuityDrillScenarioRunner liveRunner = new(operations, options);
            CapturingContinuityDrillScenarioRunner runner = new(liveRunner);
            FileRecoveryValidationEvidenceSink evidence = new(
                options,
                ResolvedRepositoryCommit(),
                InstalledDaprRuntimeVersion(),
                ResolvedAspireVersion(),
                ResolvedAppHostVersion());
            DateTimeOffset attemptStartedAtUtc = DateTimeOffset.UtcNow;
            InMemoryAuditWriter audit = new();
            InMemoryOperatorAlertSink alerts = new();
            ContinuityDrillCoordinator coordinator = new(runner, audit, alerts, new SystemClock(), evidence);

            ContinuityDrillOutcome outcome = await coordinator
                .RunAllScenariosAsync(tenantRef, runId, workflowToken)
                .ConfigureAwait(true);

            if (runner.Failures.Count > 0)
            {
                throw new AggregateException("Live continuity validation produced unmeasurable scenario failures.", runner.Failures);
            }

            RecoveryValidationDataset validationDataset = RecoveryValidationDataset.Load(
                Path.Combine(
                    RepositoryRoot(),
                    "tests",
                    "Hexalith.ChatBot.IntegrationTests",
                    "Recovery",
                    "Datasets",
                    "recovery-baseline-v1.json"),
                tenantRef);
            RecoveryValidationDatasetDescriptor dataset = validationDataset.Descriptor;
            dataset.Validate(
                options.DatasetRef,
                options.DatasetVersion,
                options.DatasetVolume,
                options.ProjectionSchemaVersion,
                options.ValidationPartitionRef).ShouldBeNull();
            InMemoryWormAuditStore worm = new();
            foreach (AuditEnvelope envelope in validationDataset.AuditEnvelopes)
            {
                _ = await worm.AppendAsync(envelope, workflowToken).ConfigureAwait(true);
            }

            await LiveProjectionRebuildDriver.SeedBaselineAsync(
                recoveryReadModels,
                tenantRef,
                dataset,
                validationDataset.SourceRecords,
                worm.EnumerateChain(tenantRef),
                workflowToken).ConfigureAwait(true);
            freshPartitionTenant = LiveProjectionRebuildDriver.FreshPartitionTenant(
                tenantRef,
                options.ValidationPartitionRef,
                runId);
            rebuildFreshKeys = LiveProjectionRebuildDriver.ProjectionKeys(
                freshPartitionTenant,
                validationDataset.SourceRecords,
                worm.EnumerateChain(tenantRef).ToArray());
            LiveProjectionRebuildDriver rebuildDriver = new(
                validationDataset.SourceRecords,
                worm,
                recoveryReadModels,
                recoveryReadModels,
                dataset,
                options,
                new SystemClock());
            CapturingProjectionRebuildDriver capturingRebuildDriver = new(rebuildDriver);
            ProjectionRebuildValidationCoordinator rebuildCoordinator = new(
                capturingRebuildDriver,
                audit,
                alerts,
                new SystemClock(),
                evidence);

            ProjectionRebuildOutcome rebuildOutcome = await rebuildCoordinator
                .RunAllAsync(tenantRef, [options.DatasetRef], runId, workflowToken)
                .ConfigureAwait(true);

            if (capturingRebuildDriver.Failures.Count > 0)
            {
                throw new AggregateException(
                    "Live projection-rebuild validation produced unmeasurable scenario failures.",
                    capturingRebuildDriver.Failures);
            }

            AspireScopedOutageOperations scopedOperations = new(
                application,
                security,
                controlAccessToken,
                mailboxClientSecret,
                controllerSecret,
                recoveryReadModels,
                recoveryReadModels,
                durableState);
            LiveScopedOutageInjectionDriver liveScopedDriver = new(scopedOperations, options);
            CapturingScopedOutageInjectionDriver scopedDriver = new(liveScopedDriver);
            ScopedOutageDegradationValidationCoordinator scopedCoordinator = new(
                scopedDriver,
                audit,
                alerts,
                new SystemClock(),
                evidence);

            ScopedOutageDegradationOutcome scopedOutcome = await scopedCoordinator
                .RunAllScenariosAsync(tenantRef, runId, workflowToken)
                .ConfigureAwait(true);

            if (scopedDriver.Failures.Count > 0)
            {
                throw new AggregateException(
                    "Live scoped-outage validation produced unmeasurable scenario failures.",
                    scopedDriver.Failures.Values);
            }

            int expectedEvidence = ContinuityDrillScenarios.All.Count + 1 + ScopedOutageDependencies.All.Count;

            // The sinks' failure path writes a SECOND report+manifest pair for a substituted Unmeasurable report, so an
            // exact count turned a genuine sink/manifest error into an opaque count mismatch. Require at least one pair
            // per scenario and let the manifest contents (asserted below and by the gate) carry the real verdict.
            Directory.Exists(evidenceDirectory)
                .ShouldBeTrue($"No live-recovery evidence was written to '{evidenceDirectory}'.");
            string[] reportFiles = Directory.GetFiles(evidenceDirectory, "*.report.json");
            string[] manifestFiles = Directory.GetFiles(evidenceDirectory, "*.manifest.json");
            reportFiles.Length.ShouldBeGreaterThanOrEqualTo(expectedEvidence);
            manifestFiles.Length.ShouldBe(
                reportFiles.Length,
                "Every retained report must have a matching evidence manifest.");
            List<RecoveryValidationEvidenceManifest> manifests = [];
            foreach (string manifestFile in manifestFiles)
            {
                await using FileStream stream = File.OpenRead(manifestFile);
                RecoveryValidationEvidenceManifest? manifest = await JsonSerializer
                    .DeserializeAsync<RecoveryValidationEvidenceManifest>(
                        stream,
                        new JsonSerializerOptions(JsonSerializerDefaults.Web),
                        cancellationToken)
                    .ConfigureAwait(true);
                manifests.Add(manifest.ShouldNotBeNull());
            }

            DateTimeOffset attemptCompletedAtUtc = DateTimeOffset.UtcNow;

            // Derived from what the run actually produced, not asserted. This field is the one the gate branches on
            // first, so hand-setting it to true meant the gate was handed its own answer. Continuity Missed and
            // rebuild equivalence are part of success — not only Unmeasurable/Contained tallies.
            bool attemptSucceeded =
                runner.Failures.Count == 0 &&
                capturingRebuildDriver.Failures.Count == 0 &&
                scopedDriver.Failures.Count == 0 &&
                outcome.Unmeasurable == 0 &&
                outcome.Missed == 0 &&
                outcome.Met == ContinuityDrillScenarios.All.Count &&
                rebuildOutcome.Unmeasurable == 0 &&
                rebuildOutcome.Equivalent == 1 &&
                rebuildOutcome.Divergent == 0 &&
                rebuildOutcome.DurationExceeded == 0 &&
                scopedOutcome.Unmeasurable == 0 &&
                scopedOutcome.Contained == ScopedOutageDependencies.All.Count &&
                scopedOutcome.Breached == 0 &&
                scopedOutcome.ScopeRecordingExceeded == 0 &&
                scopedOutcome.Alerted == 0 &&
                manifestFiles.Length >= expectedEvidence;

            Dictionary<string, int> alertsDeliveredByJob = new(StringComparer.Ordinal)
            {
                [LiveRecoveryValidationJobs.Continuity] = outcome.Alerted,
                [LiveRecoveryValidationJobs.ProjectionRebuild] = rebuildOutcome.Alerted,
                [LiveRecoveryValidationJobs.ScopedOutage] = scopedOutcome.Alerted,
            };

            // Retain the run's own observations beside its manifests so the release gate can be re-evaluated in a
            // separate job from a fresh process. Alert counts and sweep completion are facts only this run can see;
            // every threshold they are judged by comes from the release path's policy, not from here.
            // Enabled mirrors the configured option rather than a literal, and the summary is written BEFORE the
            // outcome assertions below: hand-setting it to true, then only reaching the write on a fully passing run,
            // made the gate's `live_validation_disabled` and `latest_attempt_incomplete` branches unreachable from any
            // real artifact. A breached/missed/divergent sweep must still retain a summary saying so.
            LiveRecoveryValidationAttemptSummary summary = new()
            {
                Enabled = options.Enabled,
                RunId = runId,
                StartedAtUtc = attemptStartedAtUtc,
                CompletedAtUtc = attemptCompletedAtUtc,
                LatestAttemptCompletedSuccessfully = attemptSucceeded,
                AlertsDeliveredByJob = alertsDeliveredByJob,
            };
            await File.WriteAllTextAsync(
                    Path.Combine(evidenceDirectory, LiveRecoveryValidationAttemptSummary.FileName),
                    JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }),
                    cancellationToken)
                .ConfigureAwait(true);

            // Asserted only after the summary is durable, so a breached/missed/divergent sweep still leaves retained
            // evidence for the independent gate to reject rather than failing here with nothing written.
            outcome.ScenariosRun.ShouldBe(ContinuityDrillScenarios.All.Count);
            outcome.Unmeasurable.ShouldBe(0);
            outcome.Missed.ShouldBe(0);
            outcome.Met.ShouldBe(ContinuityDrillScenarios.All.Count);
            outcome.Alerted.ShouldBe(0);
            rebuildOutcome.TenantsValidated.ShouldBe(1);
            rebuildOutcome.Equivalent.ShouldBe(1);
            rebuildOutcome.Divergent.ShouldBe(0);
            rebuildOutcome.DurationExceeded.ShouldBe(0);
            rebuildOutcome.Unmeasurable.ShouldBe(0);
            rebuildOutcome.Alerted.ShouldBe(0);
            scopedOutcome.ScenariosValidated.ShouldBe(ScopedOutageDependencies.All.Count);
            scopedOutcome.Contained.ShouldBe(ScopedOutageDependencies.All.Count);
            scopedOutcome.Breached.ShouldBe(0);
            scopedOutcome.ScopeRecordingExceeded.ShouldBe(0);
            scopedOutcome.Unmeasurable.ShouldBe(0);
            scopedOutcome.Alerted.ShouldBe(0);

            LiveRecoveryValidationEvidenceAttempt attempt = new(
                Enabled: options.Enabled,
                RunId: runId,
                StartedAtUtc: attemptStartedAtUtc,
                CompletedAtUtc: attemptCompletedAtUtc,
                LatestAttemptCompletedSuccessfully: attemptSucceeded,
                Evidence: manifests,
                AlertsDeliveredByJob: alertsDeliveredByJob);

            // Evaluate at a real wall-clock instant rather than reusing attemptCompletedAtUtc. Passing the completion
            // timestamp back in made evaluatedAtUtc - CompletedAtUtc identically zero, so neither the attempt-level nor
            // the per-manifest staleness branch could ever fire.
            //
            // This in-process evaluation is a fast smoke check only. The authoritative, independent evaluation runs in
            // the separate `live-recovery-evidence-gate` workflow job (LiveRecoveryEvidenceGateReplayTests) against
            // the uploaded artifact, so the run no longer grades its own homework.
            LiveRecoveryValidationEvidenceGateDecision gate = LiveRecoveryValidationEvidenceGate.Evaluate(
                attempt,
                new LiveRecoveryValidationGatePolicy(
                    ConfiguredProjectionDatasets: [options.DatasetRef],
                    TargetDeviationsBlockRelease: true,
                    RequiredDriverMode: RecoveryValidationEvidenceManifest.LiveDriverMode,
                    MaximumEvidenceAge: options.MaximumEvidenceAge,
                    ExpectedDatasetVersion: options.DatasetVersion,
                    MinimumDatasetVolume: options.DatasetVolume,
                    MaximumMeasurableRecoveryCeilingSeconds: options.RestorationTimeout.TotalSeconds),
                DateTimeOffset.UtcNow);
            gate.IsStopShip.ShouldBeFalse(string.Join(',', gate.StopShipReasons));
            gate.TargetDeviationReasons.ShouldBeEmpty();
        }
        finally
        {
            // Decision 2 option 1: Task 4 may have left failed-partition keys; erase after evidence capture attempt.
            if (rebuildEraser is not null && rebuildFreshKeys is { Count: > 0 })
            {
                try
                {
                    await LiveProjectionRebuildDriver
                        .ErasePartitionAsync(rebuildEraser, rebuildFreshKeys, rebuildEraseTimeout)
                        .ConfigureAwait(true);
                }
                catch (Exception exception)
                {
                    // Prefer disposing the topology; stranded keys are still better than blocking teardown. Log
                    // rather than silently discard: a repeated failure here is exactly the "stranded fresh-partition
                    // keys poison later runs" scenario this compensating erase exists to prevent, and nothing else
                    // connects a later poisoned run back to this cause.
                    Console.Error.WriteLine(
                        $"Post-rebuild compensating erase failed for run '{runId}', partition '{freshPartitionTenant}' " +
                        $"({rebuildFreshKeys.Count} keys): {exception}");
                }
            }

            await application.DisposeAsync().ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Returns the locator naming the workflow artifact this run's evidence is actually uploaded as. It was a literal
    /// (<c>artifact:live-recovery-validation</c>) that matched neither uploaded artifact name, so every retained
    /// manifest pointed a reviewer at nothing; <see cref="RecoveryValidationEvidenceManifest.IsSafeArtifactLocator"/>
    /// is a syntax check and cannot catch that.
    /// </summary>
    private static string EvidenceArtifactLocator()
    {
        string? configured = Environment.GetEnvironmentVariable("HEXALITH_CHATBOT_RECOVERY_EVIDENCE_ARTIFACT");
        string locator = $"artifact:{(string.IsNullOrWhiteSpace(configured) ? "live-recovery-validation-evidence" : configured.Trim())}";
        if (!RecoveryValidationEvidenceManifest.IsSafeArtifactLocator(locator))
        {
            throw new InvalidOperationException(
                "HEXALITH_CHATBOT_RECOVERY_EVIDENCE_ARTIFACT is not a safe artifact locator.");
        }

        return locator;
    }

    internal static TimeSpan RecoveryWorkflowTimeout(string? configured)
    {
        const int DefaultMinutes = 300;
        if (string.IsNullOrWhiteSpace(configured))
        {
            return TimeSpan.FromMinutes(DefaultMinutes);
        }

        if (!int.TryParse(
                configured,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int minutes)
            || minutes is < 1 or > DefaultMinutes)
        {
            throw new InvalidOperationException(
                "HEXALITH_CHATBOT_RECOVERY_WORKFLOW_TIMEOUT_MINUTES must be an integer from 1 through 300.");
        }

        return TimeSpan.FromMinutes(minutes);
    }

    private static void RequireTier3Runtime()
    {
        bool available = string.Equals(Environment.GetEnvironmentVariable("HEXALITH_CHATBOT_TIER3"), "1", StringComparison.Ordinal) &&
            CommandSucceeds("docker", "info") &&
            CommandSucceeds("dapr", "--version");
        bool required = string.Equals(Environment.GetEnvironmentVariable("HEXALITH_CHATBOT_TIER3_REQUIRED"), "1", StringComparison.Ordinal);
        if (required && !available)
        {
            throw new InvalidOperationException("The required live-recovery Tier-3 runtime is unavailable.");
        }

        Assert.SkipUnless(available, "Set HEXALITH_CHATBOT_TIER3=1 with Docker and DAPR available to run live recovery validation.");
    }

    private static bool CommandSucceeds(string fileName, string arguments)
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            if (process is null)
            {
                return false;
            }

            // Drain both streams concurrently: reading them sequentially can deadlock if the un-drained stream fills
            // its OS pipe buffer before the process closes the stream being read first.
            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();
            Task.WaitAll(stdoutTask, stderrTask);
            if (!process.WaitForExit(10_000))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves the commit the evidence is attributed to. Never falls back to a literal SHA: a hard-coded fallback
    /// made a local run publish a manifest attributed to a commit it was not built from, which is worse than
    /// declaring the provenance unknown.
    /// </summary>
    private static string ResolvedRepositoryCommit()
    {
        string? ambient = Environment.GetEnvironmentVariable("GITHUB_SHA");
        if (!string.IsNullOrWhiteSpace(ambient) && AuditMetadata.IsSafeStableIdentifier(ambient))
        {
            return ambient;
        }

        try
        {
            using Process? process = Process.Start(new ProcessStartInfo("git", "rev-parse HEAD")
            {
                WorkingDirectory = RepositoryRoot(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            if (process is not null)
            {
                // Drain both streams concurrently — see CommandSucceeds for why sequential ReadToEnd can deadlock.
                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                Task<string> stderrTask = process.StandardError.ReadToEndAsync();
                Task.WaitAll(stdoutTask, stderrTask);
                string head = stdoutTask.Result.Trim();
                if (process.WaitForExit(10_000) && process.ExitCode == 0 &&
                    AuditMetadata.IsSafeStableIdentifier(head))
                {
                    return head;
                }
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // git is unavailable on this host; fall through to the fail-closed throw below.
        }

        // Fail closed rather than inventing provenance. The previous hard-coded SHA fallback made a local run publish
        // a manifest attributed to a commit it was not built from, which is worse than refusing to write evidence.
        throw new InvalidOperationException(
            "The repository commit for live-recovery evidence could not be resolved from GITHUB_SHA or git.");
    }

    /// <summary>Reads the AppHost assembly version so a bump cannot silently leave a hardcoded provenance token.</summary>
    private static string ResolvedAppHostVersion()
    {
        System.Reflection.Assembly assembly = System.Reflection.Assembly.Load("Hexalith.ChatBot.AppHost");
        string informational = assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), inherit: false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? string.Empty;

        int plus = informational.IndexOf('+', StringComparison.Ordinal);
        string version = plus < 0 ? informational : informational[..plus];
        return AuditMetadata.IsSafeStableIdentifier(version)
            ? version
            : throw new InvalidOperationException("The AppHost version was not a safe evidence token.");
    }

    /// <summary>Reads the Aspire version from the loaded assembly so a package bump cannot silently invalidate it.</summary>
    private static string ResolvedAspireVersion()
    {
        string informational = typeof(DistributedApplication).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), inherit: false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion
            ?? typeof(DistributedApplication).Assembly.GetName().Version?.ToString()
            ?? string.Empty;

        // Informational versions can carry a '+<sha>' build metadata suffix that is not a safe evidence token.
        int plus = informational.IndexOf('+', StringComparison.Ordinal);
        string version = plus < 0 ? informational : informational[..plus];
        return AuditMetadata.IsSafeStableIdentifier(version)
            ? version
            : throw new InvalidOperationException("The Aspire version was not a safe evidence token.");
    }

    private static string InstalledDaprRuntimeVersion()
    {
        using Process? process = Process.Start(new ProcessStartInfo("dapr", "--version")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        });
        if (process is null)
        {
            throw new InvalidOperationException("The DAPR runtime version could not be resolved for live evidence.");
        }

        // Drain both streams concurrently — see CommandSucceeds for why sequential ReadToEnd can deadlock.
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        Task.WaitAll(stdoutTask, stderrTask);
        string stdout = stdoutTask.Result;
        if (!process.WaitForExit(10_000) || process.ExitCode != 0)
        {
            throw new InvalidOperationException("The DAPR runtime version could not be resolved for live evidence.");
        }

        string? runtimeLine = stdout
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SingleOrDefault(line => line.StartsWith("Runtime version:", StringComparison.OrdinalIgnoreCase));
        string runtimeVersion = runtimeLine?["Runtime version:".Length..].Trim() ?? string.Empty;
        return AuditMetadata.IsSafeStableIdentifier(runtimeVersion)
            ? runtimeVersion
            : throw new InvalidOperationException("The DAPR runtime version was not a safe evidence token.");
    }

    /// <summary>
    /// Proves the recovery mailbox bearer is admitted by the running ChatBot, not by a re-implementation of admission
    /// inside the test process. The previous version base64-decoded the JWT payload WITHOUT verifying its signature,
    /// hand-built a <see cref="ClaimsPrincipal"/>, and ran the admission stages locally — so signature, issuer,
    /// audience, expiry and server-side scope enforcement were all invisible, and ChatBot could have stopped
    /// validating token signatures entirely while this probe still passed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The admission proof is the emitted problem <c>type</c>, not a status class. Requiring <c>400</c>/<c>422</c>
    /// was unsatisfiable for every possible input: <see cref="ChatBotProblemDetailsFactory"/> emits only
    /// <c>401</c>, <c>403</c>, <c>409</c> and <c>503</c>, and the deliberately invalid <c>IntakeId</c> below is
    /// rejected inside <c>AcceptedCommandDispatcher.BuildPlanAsync</c>, whose
    /// <see cref="InvalidOperationException"/> <c>CommandGateway</c> classifies as a transient dispatch outage.
    /// The lane therefore retried a permanent, correct-by-design <c>503</c> until its startup budget expired.
    /// </para>
    /// <para>
    /// <c>dispatch-unavailable</c> is emitted from exactly one place — the <c>catch</c> around
    /// <c>dispatcher.DispatchAsync</c>, which is reachable only after <c>admissionDecision.IsAccepted</c> — so it is
    /// categorical proof that the bearer cleared authentication, authorization, tenant binding and the service-client
    /// grant. That is a strictly more specific assertion than "the status was 400 or 422", and unlike it, reachable.
    /// A credential rejection (<c>401</c>/<c>403</c>) and the pre-commit <c>audit-unavailable</c> denial are
    /// deliberately NOT accepted: both are emitted before or instead of admission acceptance.
    /// </para>
    /// </remarks>
    private static async Task AssertMailboxTokenAdmissionAsync(
        DistributedApplication application,
        string accessToken,
        Func<string> serverDiagnostics,
        CancellationToken cancellationToken)
    {
        using HttpClient client = application.CreateHttpClient("chatbot");
        // Deliberately invalid after admission: IntakeId is not a ULID. Auth must pass; the command must not Accepted.
        string body = $$"""
            {"commandId":"{{ChatBotCommandId.New().Value}}","commandType":"CaptureMailboxMessageIntake","command":{"intakeId":"not-a-ulid","source":{"providerMessageId":"probe","mailboxId":"probe","receivedAtUtc":"2026-08-01T00:00:00Z"},"recipients":[],"attachments":[]},"origin":"mailbox","requestSchemaVersion":"v1"}
            """;
        HttpStatusCode? lastStatus = null;
        // The probe used to report only the status code, so a persistent 503 could not be told apart from a slow
        // start and never named its own reason code. ChatBot answers this path with a redacted ProblemDetails whose
        // `type`/`code` distinguishes dispatch-unavailable from audit-unavailable; retaining a bounded copy of the
        // last one turns the failure into a diagnosis. It is test output only and never reaches an evidence artifact.
        string? lastProblemDetails = null;
        while (true)
        {
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("X-Correlation-Id", ChatBotCorrelationId.New().Value);
                using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

                // Decision 3 option 1: only post-admission validation failures prove admission without enqueueing work.
                if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
                {
                    return;
                }

                // Resource Healthy means the adapter endpoint is serving, not that the EventStore/DAPR command spine
                // behind admission has finished warming up. That path intentionally returns 503 until it is ready,
                // matching the ordinary Tier-3 acceptance test's first-command retry discipline.
                lastStatus = response.StatusCode;
                string problemDetails = await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);
                lastProblemDetails = problemDetails.Length > 512
                    ? problemDetails[..512]
                    : problemDetails;

                // The other reachable post-admission outcome (see the remarks above): only the accepted branch of
                // CommandGateway can emit this type, so observing it proves the bearer was admitted.
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable
                    && IsDispatchUnavailableProblem(problemDetails))
                {
                    return;
                }

                if (!IsTransientMailboxAdmissionStatus(response.StatusCode))
                {
                    throw new InvalidOperationException(
                        $"The recovery mailbox admission probe returned an unexpected status {(int)response.StatusCode}. "
                        + $"Response: {lastProblemDetails}{Environment.NewLine}{serverDiagnostics()}");
                }
            }
            catch (HttpRequestException)
            {
                // The endpoint or its DAPR command path is still starting; retry inside the existing startup budget.
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // A per-request HttpClient timeout is transient while the command path warms up.
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new InvalidOperationException(
                    "The recovery mailbox admission probe never admitted inside the startup budget; last observed "
                    + (lastStatus is null
                        ? "no HTTP response."
                        : $"status {(int)lastStatus.Value}. Response: {lastProblemDetails}")
                    + Environment.NewLine
                    + serverDiagnostics());
            }
        }
    }

    /// <summary>
    /// Recognises the one problem type that <c>CommandGateway</c> can only emit after admission accepted the caller.
    /// </summary>
    /// <param name="problemDetails">The verbatim response body.</param>
    /// <returns><see langword="true"/> when the body is a dispatch-unavailable problem document.</returns>
    internal static bool IsDispatchUnavailableProblem(string problemDetails)
    {
        if (string.IsNullOrWhiteSpace(problemDetails))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(problemDetails);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("type", out JsonElement type)
                && type.ValueKind == JsonValueKind.String
                && string.Equals(
                    type.GetString(),
                    ChatBotProblemTypes.DispatchUnavailable,
                    StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            // A body that is not a problem document proves nothing; keep retrying inside the startup budget.
            return false;
        }
    }

    /// <summary>Classifies only startup statuses that can become healthy without changing the probe request.</summary>
    /// <remarks>
    /// Enumerated rather than a <c>&gt;= 500</c> catch-all. The catch-all made every clause below unreachable and
    /// contradicted this summary: it also retried statuses that never become healthy on their own -- notably
    /// <c>501 NotImplemented</c> and <c>505 HttpVersionNotSupported</c> -- burning the whole startup budget on a
    /// permanent misconfiguration and discarding the status code that would have named it.
    /// <c>500 InternalServerError</c> stays transient: the command spine genuinely returns it while warming.
    /// </remarks>
    internal static bool IsTransientMailboxAdmissionStatus(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            or HttpStatusCode.InsufficientStorage;

    /// <summary>
    /// Companion to <see cref="AssertMailboxTokenAdmissionAsync"/>: proves the auth-before-validation pipeline
    /// ordering that probe's own <c>400</c>/<c>422</c> expectation depends on, by sending the identical
    /// post-admission-invalid payload with no bearer at all and requiring the auth boundary — not the same
    /// validation failure — to reject it first.
    /// </summary>
    private static async Task AssertInvalidMailboxBearerIsRejectedBeforeAdmissionAsync(
        DistributedApplication application,
        CancellationToken cancellationToken)
    {
        using HttpClient client = application.CreateHttpClient("chatbot");
        string body = $$"""
            {"commandId":"{{ChatBotCommandId.New().Value}}","commandType":"CaptureMailboxMessageIntake","command":{"intakeId":"not-a-ulid","source":{"providerMessageId":"probe","mailboxId":"probe","receivedAtUtc":"2026-08-01T00:00:00Z"},"recipients":[],"attachments":[]},"origin":"mailbox","requestSchemaVersion":"v1"}
            """;
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");
        request.Headers.Add("X-Correlation-Id", ChatBotCorrelationId.New().Value);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException(
                $"An invalid mailbox bearer returned {(int)response.StatusCode} instead of 401 — the sibling admission " +
                "probe's 400/422 expectation would not prove auth ran first.");
        }
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
