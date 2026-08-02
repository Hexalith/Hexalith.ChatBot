using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
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

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Tier-3 proof for both mandatory continuity scenarios through their existing coordinator.</summary>
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
        eventStore.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["DOTNET_SHUTDOWNTIMEOUTSECONDS"] = "5";
            context.EnvironmentVariables["EventStore__RateLimiting__PermitLimit"] = "100000";
            context.EnvironmentVariables["EventStore__RateLimiting__ConsumerPermitLimit"] = "10000";
        }));
        chatBot.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["ChatBot__Projection__Topic"] = $"{RecoveryValidationTopology.StorageTenantRef}.chatbot.events";
            context.EnvironmentVariables["ChatBot__Projection__DeadLetterTopic"] = $"deadletter.{RecoveryValidationTopology.StorageTenantRef}.chatbot.events";
        }));

        DistributedApplication application = await builder.BuildAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            using CancellationTokenSource startup = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            startup.CancelAfter(TimeSpan.FromMinutes(5));
            internalGrpcReservations.Release();
            await application.StartAsync(startup.Token).ConfigureAwait(true);
            foreach (string resource in new[] { "security", "eventstore", "chatbot", "recovery-sandbox" })
            {
                await application.ResourceNotifications.WaitForResourceHealthyAsync(resource, startup.Token).ConfigureAwait(true);
            }

            string mailboxAccessToken = await RecoveryAccessTokenProvider
                .AcquireMailboxAsync(application, mailboxClientSecret, startup.Token)
                .ConfigureAwait(true);
            string controlAccessToken = await RecoveryAccessTokenProvider
                .AcquireControlAsync(application, startup.Token)
                .ConfigureAwait(true);
            await AssertMailboxTokenAdmissionAsync(application, mailboxAccessToken, startup.Token).ConfigureAwait(true);
            using DaprClient recoveryDaprClient = new DaprClientBuilder()
                .UseGrpcEndpoint(application.GetEndpoint("chatbot-dapr-cli", "grpc").ToString())
                .UseHttpEndpoint(application.GetEndpoint("chatbot-dapr-cli", "http").ToString())
                .Build();
            DaprReadModelStore recoveryReadModels = new(recoveryDaprClient);
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
                ProjectionSchemaVersion = "project-conversation-v1",
                ValidationPartitionRef = "recovery-partition-v1",
                ControllerCapability = LiveRecoveryValidationOptions.AspireControllerCapability,
                ControllerSecret = controllerSecret,
                // A reachable per-scenario budget rather than the 4-hour recovery target: nine serial scenarios plus
                // topology margin have to fit inside WorkflowTimeout, so a 4-hour per-scenario budget was nominal and
                // silently truncated by the outer deadline. RestorationTimeout remains the lane's measurable recovery
                // ceiling and is published in every manifest.
                PerScenarioTimeout = TimeSpan.FromMinutes(25),
                RestorationTimeout = TimeSpan.FromMinutes(3),
                WorkflowTimeout = TimeSpan.FromHours(5),
                EvidenceDirectory = evidenceDirectory,
                EvidenceLocator = EvidenceArtifactLocator(),
            };
            options.Validate().ShouldBeNull();

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
                ResolvedAspireVersion());
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

            outcome.ScenariosRun.ShouldBe(ContinuityDrillScenarios.All.Count);
            outcome.Unmeasurable.ShouldBe(0);
            (outcome.Met + outcome.Missed).ShouldBe(ContinuityDrillScenarios.All.Count);
            outcome.Alerted.ShouldBe(outcome.Missed);

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
            LiveProjectionRebuildDriver rebuildDriver = new(
                validationDataset.SourceRecords,
                worm,
                recoveryReadModels,
                recoveryReadModels,
                dataset,
                options,
                new SystemClock());
            ProjectionRebuildValidationCoordinator rebuildCoordinator = new(
                rebuildDriver,
                audit,
                alerts,
                new SystemClock(),
                evidence);

            ProjectionRebuildOutcome rebuildOutcome = await rebuildCoordinator
                .RunAllAsync(tenantRef, [options.DatasetRef], runId, workflowToken)
                .ConfigureAwait(true);

            rebuildOutcome.TenantsValidated.ShouldBe(1);
            rebuildOutcome.Equivalent.ShouldBe(1);
            rebuildOutcome.Divergent.ShouldBe(0);
            rebuildOutcome.DurationExceeded.ShouldBe(0);
            rebuildOutcome.Unmeasurable.ShouldBe(0);
            rebuildOutcome.Alerted.ShouldBe(0);

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

            scopedOutcome.ScenariosValidated.ShouldBe(ScopedOutageDependencies.All.Count);
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
            // first, so hand-setting it to true meant the gate was handed its own answer.
            bool attemptSucceeded =
                runner.Failures.Count == 0 &&
                scopedDriver.Failures.Count == 0 &&
                outcome.Unmeasurable == 0 &&
                rebuildOutcome.Unmeasurable == 0 &&
                scopedOutcome.Unmeasurable == 0 &&
                manifestFiles.Length == expectedEvidence;

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
            // real artifact. A breached sweep must still retain a summary saying so.
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

            // Asserted only after the summary is durable, so a breached sweep still leaves retained evidence for the
            // independent gate to reject rather than failing here with nothing written.
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
        return $"artifact:{(string.IsNullOrWhiteSpace(configured) ? "live-recovery-validation-evidence" : configured.Trim())}";
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
            if (process is null || !process.WaitForExit(10_000))
            {
                process?.Kill(entireProcessTree: true);
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
            if (process is not null && process.WaitForExit(10_000) && process.ExitCode == 0)
            {
                string head = process.StandardOutput.ReadToEnd().Trim();
                if (AuditMetadata.IsSafeStableIdentifier(head))
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
        if (process is null || !process.WaitForExit(10_000) || process.ExitCode != 0)
        {
            throw new InvalidOperationException("The DAPR runtime version could not be resolved for live evidence.");
        }

        string? runtimeLine = process.StandardOutput
            .ReadToEnd()
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
    private static async Task AssertMailboxTokenAdmissionAsync(
        DistributedApplication application,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using HttpClient client = application.CreateHttpClient("chatbot");
        string body = $$"""
            {"commandId":"{{ChatBotCommandId.New().Value}}","commandType":"CaptureMailboxMessageIntake","command":{},"origin":"mailbox","requestSchemaVersion":"v1"}
            """;
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Correlation-Id", ChatBotCorrelationId.New().Value);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // The empty command body may legitimately be rejected on validation grounds; what must NOT happen is a
        // credential rejection, which is what this probe exists to rule out.
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                $"The recovery mailbox token was rejected by the running ChatBot (status {(int)response.StatusCode}).");
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
