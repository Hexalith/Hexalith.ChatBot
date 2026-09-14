using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.ChatBot.AppHost.Aspire;

using Microsoft.Extensions.Configuration;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Story 12.15 Task 2 topology guards for the reserved identity, controls, and deterministic dataset.</summary>
public sealed class RecoveryValidationTopologyContractTests
{
    // Program.cs fails closed without a configured recovery mailbox secret. These topology-only contract tests
    // never exercise the recovery mailbox client, so a fixed, well-formed placeholder satisfies
    // PrepareKeycloakRealmImport without pulling every test into live-recovery validation configuration.
    private static readonly string[] MailboxSecretArgs =
    [
        $"--ChatBot:LiveRecoveryValidation:MailboxClientSecret={new string('a', 32)}",
    ];

    // The AppHost also fails closed without the authorization-filtered Projects endpoint/token, which it validates
    // AFTER PrepareKeycloakRealmImport. Tests that only need the realm-prep throw can stop at MailboxSecretArgs;
    // a test that needs CreateAsync to SUCCEED must satisfy the later gate too.
    private static readonly string[] RenderedRealmArgs =
    [
        $"--ChatBot:LiveRecoveryValidation:MailboxClientSecret={new string('a', 32)}",
        "--ChatBot:Projects:Endpoint=http://localhost:65535",
        $"--ChatBot:Projects:ApiToken={new string('b', 32)}",
    ];

    [Fact]
    public void RealmKeepsControlTenantsAndAddsDedicatedReplayValidationIdentity()
    {
        using JsonDocument realm = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "KeycloakRealms",
            "hexalith-realm.json")));

        Dictionary<string, JsonElement> users = realm.RootElement.GetProperty("users")
            .EnumerateArray()
            .ToDictionary(user => user.GetProperty("username").GetString()!, StringComparer.Ordinal);

        Tenant(users["actor-alpha"]).ShouldBe("tenant-alpha");
        Tenant(users["actor-beta"]).ShouldBe("tenant-beta");
        Tenant(users["recovery-validator"]).ShouldBe(RecoveryValidationTopology.StorageTenantRef);
        Tenant(users["recovery-validator"]).ShouldNotBe("tenant-alpha");
        Tenant(users["recovery-validator"]).ShouldNotBe("tenant-beta");
    }

    [Fact]
    public void RealmAddsClosedRecoveryMailboxServiceIdentityWithoutACommittedSecret()
    {
        using JsonDocument realm = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "KeycloakRealms",
            "hexalith-realm.json")));

        JsonElement client = realm.RootElement.GetProperty("clients")
            .EnumerateArray()
            .Single(value => string.Equals(
                value.GetProperty("clientId").GetString(),
                RecoveryValidationTopology.MailboxClientId,
                StringComparison.Ordinal));
        client.GetProperty("secret").GetString().ShouldBe("__HEXALITH_CHATBOT_RECOVERY_CLIENT_SECRET__");
        string mappers = client.GetProperty("protocolMappers").ToString();
        mappers.ShouldContain(RecoveryValidationTopology.StorageTenantRef);
        mappers.ShouldContain("CaptureMailboxMessageIntake");
        mappers.ShouldContain("mailbox.ingest");
        mappers.ShouldContain("mailbox-ingestion");

        string appHost = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "Program.cs"));
        appHost.ShouldContain("__HEXALITH_CHATBOT_RECOVERY_CLIENT_SECRET__");
        appHost.ShouldContain("ChatBot:LiveRecoveryValidation:MailboxClientSecret");
        appHost.ShouldContain("A recovery mailbox client secret must be supplied");
        appHost.ShouldNotContain("RandomNumberGenerator");
    }

    [Fact]
    public void VersionedDatasetHasExactPositiveMetadataPopulationAndAnIsolatedPartition()
    {
        using JsonDocument dataset = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "tests",
            "Hexalith.ChatBot.IntegrationTests",
            "Recovery",
            "Datasets",
            "recovery-baseline-v1.json")));
        JsonElement root = dataset.RootElement;

        root.GetProperty("datasetRef").GetString().ShouldBe("recovery-baseline");
        root.GetProperty("version").GetString().ShouldBe("v1");
        root.GetProperty("projectionSchemaVersion").GetString()
            .ShouldBe(ProjectConversationSourceEmailView.CurrentSchemaVersion);
        root.GetProperty("projectionMode").GetString().ShouldBe("isolated-validation-store");
        root.GetProperty("validationPartitionRef").GetString().ShouldBe("recovery-partition-v1");

        string[] collections =
        [
            "sourceRecords",
            "wormAuditRecords",
            "governedCommands",
            "approvals",
            "policySnapshots",
            "attachmentMetadata",
        ];
        int volume = collections.Sum(name => root.GetProperty(name).GetArrayLength());
        collections.ShouldAllBe(name => root.GetProperty(name).GetArrayLength() > 0);
        volume.ShouldBe(root.GetProperty("volume").GetInt32());

        string raw = root.GetRawText();
        raw.ShouldNotContain("subject", Case.Insensitive);
        raw.ShouldNotContain("body", Case.Insensitive);
        raw.ShouldNotContain("content", Case.Insensitive);
        raw.ShouldNotContain("password", Case.Insensitive);
        raw.ShouldNotContain("secret", Case.Insensitive);
        raw.ShouldNotContain("token", Case.Insensitive);
    }

    [Fact]
    public async Task RecoveryWorkerSimulatorIsAbsentByDefaultAndComposedOnlyWithExplicitCapabilityConfiguration()
    {
        IDistributedApplicationTestingBuilder ordinary = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(MailboxSecretArgs, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        try
        {
            ordinary.Resources.ShouldNotContain(resource => string.Equals(resource.Name, "recovery-sandbox", StringComparison.Ordinal));
        }
        finally
        {
            await ordinary.DisposeAsync().ConfigureAwait(true);
        }

        IDistributedApplicationTestingBuilder recovery = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(MailboxSecretArgs, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        try
        {
            _ = recovery.AddRecoverySandbox(
                "Testing",
                RecoveryValidationTopology.LogicalTenantRef,
                RecoveryValidationTopology.StorageTenantRef,
                LiveRecoveryValidationOptions.AspireControllerCapability,
                "tier3-injected-value",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW");

            recovery.Resources.ShouldContain(resource => string.Equals(resource.Name, "recovery-sandbox", StringComparison.Ordinal));
        }
        finally
        {
            await recovery.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData("Production", "replay-test:recovery-validation", "recovery-validation", LiveRecoveryValidationOptions.AspireControllerCapability)]
    [InlineData("Testing", "tenant-alpha", "tenant-alpha", LiveRecoveryValidationOptions.AspireControllerCapability)]
    [InlineData("Testing", "replay-test:tenant-alpha", "tenant-alpha", LiveRecoveryValidationOptions.AspireControllerCapability)]
    [InlineData("Testing", "replay-test:tenant-beta", "tenant-beta", LiveRecoveryValidationOptions.AspireControllerCapability)]
    [InlineData("Testing", "replay-test:recovery-validation", "wrong-storage", LiveRecoveryValidationOptions.AspireControllerCapability)]
    [InlineData("Testing", "replay-test:recovery-validation", "recovery-validation", "wrong-capability")]
    public async Task AddRecoverySandboxRejectsInvalidCapabilityTenantOrEnvironment(
        string environmentName,
        string tenantRef,
        string storageTenantRef,
        string controllerCapability)
    {
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(MailboxSecretArgs, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        try
        {
            _ = Should.Throw<InvalidOperationException>(() => builder.AddRecoverySandbox(
                environmentName,
                tenantRef,
                storageTenantRef,
                controllerCapability,
                "tier3-injected-value",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW"));
            builder.Resources.ShouldNotContain(resource => string.Equals(resource.Name, "recovery-sandbox", StringComparison.Ordinal));
        }
        finally
        {
            await builder.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RecoverySandboxComposedEnvironmentKeepsLogicalAndPhysicalTenantsDistinct()
    {
        IDistributedApplicationTestingBuilder recovery = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(MailboxSecretArgs, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        try
        {
            _ = recovery.AddRecoverySandbox(
                "Testing",
                RecoveryValidationTopology.LogicalTenantRef,
                RecoveryValidationTopology.StorageTenantRef,
                LiveRecoveryValidationOptions.AspireControllerCapability,
                "tier3-injected-value",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW");

            IResource sandbox = recovery.Resources.Single(resource =>
                string.Equals(resource.Name, "recovery-sandbox", StringComparison.Ordinal));
            IReadOnlyDictionary<string, string> environment = await ResolveEnvironmentAsync(sandbox).ConfigureAwait(true);
            environment["Recovery__TenantRef"].ShouldBe(RecoveryValidationTopology.LogicalTenantRef);
            environment["Recovery__StorageTenantRef"].ShouldBe(RecoveryValidationTopology.StorageTenantRef);
            environment["Recovery__TenantRef"].ShouldNotBe(environment["Recovery__StorageTenantRef"]);
        }
        finally
        {
            await recovery.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RecoveryTopologyAssignsEveryConfiguredDaprInternalGrpcPortToItsSidecar()
    {
        Dictionary<string, int> expected = new(StringComparer.Ordinal)
        {
            ["eventstore"] = 41_001,
            ["tenants"] = 41_002,
            ["chatbot"] = 41_003,
            ["eventstore-admin"] = 41_004,
            ["eventstore-admin-ui"] = 41_005,
        };
        string[] args = expected
            .Select(pair => $"--Dapr:InternalGrpcPorts:{pair.Key}={pair.Value}")
            .Concat(MailboxSecretArgs)
            .ToArray();

        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(args, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        try
        {
            foreach ((string appId, int port) in expected)
            {
                IResource application = builder.Resources.Single(resource =>
                    string.Equals(resource.Name, appId, StringComparison.Ordinal));
                DaprSidecarAnnotation sidecar = application.Annotations.OfType<DaprSidecarAnnotation>().Single();
                DaprSidecarOptions options = sidecar.Sidecar.Annotations
                    .OfType<DaprSidecarOptionsAnnotation>()
                    .Single()
                    .Options;
                options.DaprInternalGrpcPort.ShouldBe(port, appId);

                // Every sidecar in this topology must load the local, default-allow ACL config; the deny-by-default
                // accesscontrol.yaml would otherwise silently apply (mTLS is off, so its policy can never match) and
                // the eventstore -> chatbot round-trip would fail closed with no caller identity to blame.
                options.Config.ShouldNotBeNullOrWhiteSpace(appId);
                options.Config.ShouldEndWith("accesscontrol.local.yaml", customMessage: appId);
            }
        }
        finally
        {
            await builder.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task AddHexalithChatBotRejectsTwoSidecarsSharingOneDaprInternalGrpcPort()
    {
        string[] args =
        [
            "--Dapr:InternalGrpcPorts:eventstore=41101",
            "--Dapr:InternalGrpcPorts:chatbot=41101",
            .. MailboxSecretArgs,
        ];

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => DistributedApplicationTestingBuilder
                .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(args, TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("eventstore");
        exception.Message.ShouldContain("chatbot");
        exception.Message.ShouldContain("41101");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("65536")]
    [InlineData("not-a-port")]
    public async Task AddHexalithChatBotRejectsAnOutOfRangeOrUnparsableDaprInternalGrpcPort(string invalidPort)
    {
        string[] args =
        [
            $"--Dapr:InternalGrpcPorts:chatbot={invalidPort}",
            .. MailboxSecretArgs,
        ];

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => DistributedApplicationTestingBuilder
                .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(args, TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("chatbot");
        exception.Message.ShouldContain("must be an integer from 1 through 65535");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PrepareKeycloakRealmImportRejectsAWhitespaceOrBlankMailboxClientSecret(string blankSecret)
    {
        string[] args = [$"--ChatBot:LiveRecoveryValidation:MailboxClientSecret={blankSecret}"];

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => DistributedApplicationTestingBuilder
                .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(args, TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("A recovery mailbox client secret must be supplied");
    }

    [Fact]
    public async Task PrepareKeycloakRealmImportRejectsAMailboxClientSecretShorterThanThirtyTwoCharacters()
    {
        string[] args = [$"--ChatBot:LiveRecoveryValidation:MailboxClientSecret={new string('a', 31)}"];

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => DistributedApplicationTestingBuilder
                .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(args, TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("must be at least 32 characters long");
    }

    [Fact]
    public async Task PrepareKeycloakRealmImportRejectsAMailboxClientSecretWithDisallowedCharacters()
    {
        string[] args = [$"--ChatBot:LiveRecoveryValidation:MailboxClientSecret={new string('a', 31)}!"];

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => DistributedApplicationTestingBuilder
                .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(args, TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("must contain only ASCII letters, digits, '-', or '_'");
    }

    [Fact]
    public async Task PrepareKeycloakRealmImportWritesTheRenderedRealmWithOwnerOnlyPermissionsAtAnUnpredictablePath()
    {
        // Regression guard for the vulnerability this shape fixed: a predictable {temp}/hexalith-chatbot-keycloak/
        // {pid} path plus a default-umask create-then-chmod race previously left the literal client secret
        // world-readable. Nothing here asserted the successfully-generated file's permissions or path shape, so a
        // future refactor could silently drop UnixCreateMode with no test failing.
        string tempPath = Path.GetTempPath();
        HashSet<string> before = Directory
            .EnumerateDirectories(tempPath, "hexalith-chatbot-keycloak-*")
            .ToHashSet(StringComparer.Ordinal);

        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(MailboxSecretArgs, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string? generatedDirectory = null;
        try
        {
            // Identify this run's directory by its owner marker rather than by "the only new one". Nothing in this
            // assembly disables xUnit collection parallelism, and several other classes build
            // DistributedApplicationTestingBuilder topologies that call the same function, so a bare
            // SingleOrDefault() would throw on a concurrent run's directory -- and the finally block below would
            // then delete it.
            string ownerPrefix = Environment.ProcessId.ToString(CultureInfo.InvariantCulture) + ":";
            string[] candidates = [.. Directory
                .EnumerateDirectories(tempPath, "hexalith-chatbot-keycloak-*")
                .Except(before, StringComparer.Ordinal)
                .Where(directory => OwnerMarkerStartsWith(directory, ownerPrefix))];
            candidates.Length.ShouldBe(
                1,
                "PrepareKeycloakRealmImport must create exactly one new owner-marked hexalith-chatbot-keycloak-* temp subdirectory.");
            generatedDirectory = candidates[0];

            // A fixed {pid} suffix is the predictable shape the fix replaced; CreateTempSubdirectory's random suffix
            // must never collapse back to it.
            Path.GetFileName(generatedDirectory)
                .ShouldNotBe("hexalith-chatbot-keycloak-" + Environment.ProcessId, "the directory name must not be predictable.");

            string realmFile = Path.Combine(generatedDirectory, "hexalith-realm.json");
            File.Exists(realmFile).ShouldBeTrue();

            if (!OperatingSystem.IsWindows())
            {
                File.GetUnixFileMode(realmFile).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }
        finally
        {
            await builder.DisposeAsync().ConfigureAwait(true);
            if (generatedDirectory is not null)
            {
                Directory.Delete(generatedDirectory, recursive: true);
            }
        }
    }


    [Fact]
    public async Task PrepareKeycloakRealmImportSweepsAbandonedRealmDirectoriesAndSparesLiveOnes()
    {
        // The sweep decides liveness from the recorded owner process, never from modification time. The realm file
        // is written once and never touched again, so an mtime gate deleted the secret-bearing directory of any
        // session outliving its threshold -- including the multi-hour recovery lane and a concurrent test class.
        // Both directions are asserted: without the "spares live ones" half, inverting the gate would still pass.
        string tempPath = Path.GetTempPath();
        string abandoned = Directory.CreateTempSubdirectory("hexalith-chatbot-keycloak-").FullName;
        string live = Directory.CreateTempSubdirectory("hexalith-chatbot-keycloak-").FullName;
        string unreadable = Directory.CreateTempSubdirectory("hexalith-chatbot-keycloak-").FullName;
        string markerless = Directory.CreateTempSubdirectory("hexalith-chatbot-keycloak-").FullName;

        // A WELL-FORMED marker naming a process that no longer exists. This is the case the sweep exists for and
        // the one the PID-liveness design was chosen to detect; the previous fixture used an unparseable string,
        // which only ever exercised the parse-failure branch and left Process.GetProcessById untested.
        await File.WriteAllTextAsync(
            Path.Combine(abandoned, "owner.marker"),
            string.Create(CultureInfo.InvariantCulture, $"{DeadProcessId()}:{DateTimeOffset.UtcNow.AddHours(-2):O}"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Unreadable and absent markers are NOT evidence of abandonment. A marker is written non-atomically only
        // for an instant, but a concurrently starting AppHost reading it in that instant must not delete a live
        // run's secret-bearing directory -- the race the owner-marker scheme exists to remove.
        await File.WriteAllTextAsync(
            Path.Combine(unreadable, "owner.marker"),
            "this-is-not-a-valid-owner-marker",
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        using (Process current = Process.GetCurrentProcess())
        {
            await File.WriteAllTextAsync(
                Path.Combine(live, "owner.marker"),
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{current.Id}:{current.StartTime.ToUniversalTime():O}"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        HashSet<string> before = Directory
            .EnumerateDirectories(tempPath, "hexalith-chatbot-keycloak-*")
            .ToHashSet(StringComparer.Ordinal);
        string? generatedDirectory = null;
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(MailboxSecretArgs, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        try
        {
            Directory.Exists(abandoned).ShouldBeFalse(
                "a directory whose recorded owner process is gone must be swept.");
            Directory.Exists(live).ShouldBeTrue(
                "a directory whose owner process is still running must never be swept, however old it is.");
            Directory.Exists(unreadable).ShouldBeTrue(
                "an unparseable marker is not evidence of abandonment; deleting on it races a marker being written.");
            Directory.Exists(markerless).ShouldBeTrue(
                "a directory with no marker yet is a run that has just created it, and must be spared.");

            string ownerPrefix = Environment.ProcessId.ToString(CultureInfo.InvariantCulture) + ":";
            generatedDirectory = Directory
                .EnumerateDirectories(tempPath, "hexalith-chatbot-keycloak-*")
                .Except(before, StringComparer.Ordinal)
                .FirstOrDefault(directory => OwnerMarkerStartsWith(directory, ownerPrefix));
            generatedDirectory.ShouldNotBeNull("this run must record its own owner marker.");
        }
        finally
        {
            await builder.DisposeAsync().ConfigureAwait(true);
            foreach (string? directory in new[] { generatedDirectory, live, abandoned, unreadable, markerless })
            {
                if (directory is not null && Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
        }
    }

    /// <summary>
    /// Proves <c>AddEventStoreAdmin</c>'s own port guard rejects a collision, independently of
    /// <c>AddHexalithChatBot</c>.
    /// </summary>
    /// <remarks>
    /// The guard was added for composition roots that call <c>AddEventStoreAdmin</c> without a preceding
    /// <c>AddHexalithChatBot</c>, but no test reached it: the only production caller always calls
    /// <c>AddHexalithChatBot</c> first, so deleting the guard failed nothing.
    /// </remarks>
    [Fact]
    public void ValidateUniqueInternalGrpcPortsRejectsACollisionWithoutTheChatBotComposition()
    {
        IConfiguration colliding = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Dapr:InternalGrpcPorts:eventstore"] = "41101",
                ["Dapr:InternalGrpcPorts:eventstore-admin"] = "41101",
            })
            .Build();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => ChatBotAspireModule.ValidateUniqueInternalGrpcPorts(colliding));

        exception.Message.ShouldContain("41101");
    }

    /// <summary>
    /// <c>AddEventStoreAdmin</c> must actually invoke the port guard, not merely be able to.
    /// </summary>
    /// <remarks>
    /// The two theories around this one call <c>ValidateUniqueInternalGrpcPorts</c> DIRECTLY, so they prove the
    /// validator rejects a collision -- not that <c>AddEventStoreAdmin</c> calls it. Deleting the call site leaves
    /// both of them green, which is exactly the "silently removable" property the original finding named and which
    /// widening the method's visibility did not close.
    /// <para>
    /// This assertion is deliberately scoped to the <c>AddEventStoreAdmin</c> method body rather than the whole
    /// file, so an occurrence elsewhere cannot satisfy it. It is still source-scan evidence, which is weaker than
    /// executing the composition: invoking the method for real needs a <c>HexalithChatBotResources</c> and two
    /// project resource builders, i.e. a full topology. That residual is recorded in <c>deferred-work.md</c>
    /// rather than left implied.
    /// </para>
    /// </remarks>
    [Fact]
    public void AddEventStoreAdminBodyInvokesThePortGuard()
    {
        string source = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "Aspire",
            "ChatBotAspireModule.cs"));
        int start = source.IndexOf("public static void AddEventStoreAdmin(", StringComparison.Ordinal);
        start.ShouldBeGreaterThan(0, "AddEventStoreAdmin must exist to carry the guard.");
        int next = source.IndexOf("\n    public static ", start + 1, StringComparison.Ordinal);
        string body = next > start ? source[start..next] : source[start..];

        body.Contains("ValidateUniqueInternalGrpcPorts(builder.Configuration)", StringComparison.Ordinal)
            .ShouldBeTrue(
                "AddEventStoreAdmin must invoke the port guard itself; a composition root that calls it without a "
                + "preceding AddHexalithChatBot otherwise loses port-collision protection for its two sidecars.");
    }

    /// <summary>Proves distinct ports and absent configuration both pass the same guard.</summary>
    [Fact]
    public void ValidateUniqueInternalGrpcPortsAcceptsDistinctAndUnconfiguredPorts()
    {
        IConfiguration distinct = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Dapr:InternalGrpcPorts:eventstore"] = "41101",
                ["Dapr:InternalGrpcPorts:eventstore-admin"] = "41102",
            })
            .Build();

        Should.NotThrow(() => ChatBotAspireModule.ValidateUniqueInternalGrpcPorts(distinct));
        Should.NotThrow(() => ChatBotAspireModule.ValidateUniqueInternalGrpcPorts(
            new ConfigurationBuilder().Build()));
    }

    /// <summary>Returns a process id that is reliably not running, for the dead-owner sweep fixture.</summary>
    /// <returns>An exited process's identifier.</returns>
    private static int DeadProcessId()
    {
        // Start and reap a trivial process, then reuse its id: the id is genuinely gone, and unlike a guessed
        // constant it cannot collide with a live process on the runner.
        using Process probe = Process.Start(new ProcessStartInfo("dotnet", "--version")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;
        probe.WaitForExit();
        return probe.Id;
    }

    /// <summary>
    /// Renders the realm through the AppHost and reads EVERY substituted
    /// <c>chatbot:service-client-grant-expiry</c> claim back out through the real consumer,
    /// <c>ClaimsServiceClientGrantResolver</c>, asserting it does not collapse to
    /// <c>service_client_grant_missing</c>.
    /// </summary>
    /// <remarks>
    /// This is the gap that let the <c>+00:00</c> rendering bug ship. <c>DateTimeOffset.ToString("O")</c> on a
    /// non-UTC-kind value renders a <c>+</c> offset, which <c>AuditMetadata.IsSafeStableIdentifier</c> rejects
    /// (<c>+</c> is outside its charset), so every service client would have been denied. Nothing caught it: every
    /// resolver unit test hand-writes a <c>...Z</c> literal, the AppHost guard asserted only that the placeholder
    /// was present, and the Tier-3 lane mints a <b>user</b> token (<c>grant_type=password</c>) so it can never
    /// exercise a service client. Only reading the actually-rendered value through the actual consumer closes it.
    /// </remarks>
    [Fact]
    public async Task RenderedRealmServiceClientGrantExpiryIsAcceptedByTheClaimsGrantResolver()
    {
        string tempPath = Path.GetTempPath();
        HashSet<string> before = Directory
            .EnumerateDirectories(tempPath, "hexalith-chatbot-keycloak-*")
            .ToHashSet(StringComparer.Ordinal);

        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(RenderedRealmArgs, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string? generatedDirectory = null;
        try
        {
            string ownerPrefix = Environment.ProcessId.ToString(CultureInfo.InvariantCulture) + ":";
            generatedDirectory = Directory
                .EnumerateDirectories(tempPath, "hexalith-chatbot-keycloak-*")
                .Except(before, StringComparer.Ordinal)
                .FirstOrDefault(directory => OwnerMarkerStartsWith(directory, ownerPrefix));
            generatedDirectory.ShouldNotBeNull("PrepareKeycloakRealmImport must render this run's realm.");

            using JsonDocument rendered = JsonDocument.Parse(
                await File.ReadAllTextAsync(
                    Path.Combine(generatedDirectory, "hexalith-realm.json"),
                    TestContext.Current.CancellationToken).ConfigureAwait(true));

            (string ClientId, string Expiry)[] grants =
            [
                .. rendered.RootElement.GetProperty("clients").EnumerateArray()
                    .SelectMany(client => (client.TryGetProperty("protocolMappers", out JsonElement mappers)
                            ? mappers.EnumerateArray()
                            : Enumerable.Empty<JsonElement>())
                        .Where(mapper => mapper.TryGetProperty("name", out JsonElement name)
                            && string.Equals(name.GetString(), "chatbot-service-client-grant-expiry", StringComparison.Ordinal))
                        .Select(mapper => (
                            ClientId: client.GetProperty("clientId").GetString()!,
                            Expiry: mapper.GetProperty("config").GetProperty("claim.value").GetString()!))),
            ];

            grants.Length.ShouldBeGreaterThan(
                0,
                "The rendered realm must carry at least one substituted service-client grant expiry, or this test "
                + "proves nothing about the rendering.");

            ClaimsServiceClientGrantResolver resolver = new();
            foreach ((string clientId, string expiry) in grants)
            {
                expiry.ShouldNotContain(
                    "__HEXALITH_CHATBOT_SERVICE_GRANT_EXPIRES_AT__",
                    customMessage: $"Client '{clientId}' kept the unsubstituted placeholder.");

                // The two gates the resolver actually applies, in order, asserted individually so a failure names
                // which one rejected the rendering rather than only that the grant went missing.
                AuditMetadata.IsSafeStableIdentifier(expiry).ShouldBeTrue(
                    $"Client '{clientId}' rendered the grant expiry as '{expiry}', which AuditMetadata rejects. "
                    + "A DateTimeOffset with a non-UTC Kind renders a '+00:00' offset, and '+' is outside the safe "
                    + "charset; render expiresAt.UtcDateTime so the canonical 'O' form ends in 'Z'.");
                DateTimeOffset.TryParse(
                    expiry,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTimeOffset parsed).ShouldBeTrue(
                    $"Client '{clientId}' rendered a grant expiry the resolver cannot parse: '{expiry}'.");
                parsed.ShouldBeGreaterThan(
                    DateTimeOffset.UtcNow,
                    $"Client '{clientId}' rendered an already-expired grant expiry.");

                // ... and through the real consumer, end to end.
                ServiceClientGrantResolution resolution = await resolver
                    .ResolveAsync(
                        ServiceClientSubmission(),
                        ServiceClientActor(clientId, expiry),
                        new ChatBotTenantBinding("tenant-alpha"),
                        TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);

                resolution.ReasonCode.ShouldNotBe(
                    ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing,
                    $"ClaimsServiceClientGrantResolver denied client '{clientId}' with service_client_grant_missing "
                    + $"for the realm-rendered expiry '{expiry}'.");
                resolution.IsResolved.ShouldBeTrue(
                    $"Client '{clientId}' must resolve a grant from the realm-rendered expiry '{expiry}'.");
                resolution.Grant.ShouldNotBeNull().ExpiresAt.ShouldBe(parsed);
            }
        }
        finally
        {
            await builder.DisposeAsync().ConfigureAwait(true);
            if (generatedDirectory is not null && Directory.Exists(generatedDirectory))
            {
                Directory.Delete(generatedDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    /// A realm client whose grant-expiry mapper lost its substitution placeholder must fail AppHost startup naming
    /// that client, rather than reporting a placeholder count the operator then has to trace back to a client.
    /// </summary>
    [Fact]
    public async Task PrepareKeycloakRealmImportNamesTheClientWhoseGrantExpiryMapperLostItsPlaceholder()
    {
        string appHostDirectory = Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost");
        string realmPath = Path.Combine(appHostDirectory, "KeycloakRealms", "hexalith-realm.json");
        string original = await File.ReadAllTextAsync(realmPath, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Blank one client's placeholder, exactly as a hand-edit or a partial rotation would.
        const string offendingClientId = "mcp-tool-client";
        int clientIndex = original.IndexOf($"\"clientId\": \"{offendingClientId}\"", StringComparison.Ordinal);
        clientIndex.ShouldBeGreaterThan(-1, $"The realm must declare '{offendingClientId}'.");
        int placeholderIndex = original.IndexOf(
            "__HEXALITH_CHATBOT_SERVICE_GRANT_EXPIRES_AT__",
            clientIndex,
            StringComparison.Ordinal);
        placeholderIndex.ShouldBeGreaterThan(-1);
        string mutated = string.Concat(
            original.AsSpan(0, placeholderIndex),
            "2099-01-01T00:00:00.0000000Z",
            original.AsSpan(placeholderIndex + "__HEXALITH_CHATBOT_SERVICE_GRANT_EXPIRES_AT__".Length));

        try
        {
            await File.WriteAllTextAsync(realmPath, mutated, TestContext.Current.CancellationToken).ConfigureAwait(true);

            InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
                () => DistributedApplicationTestingBuilder
                    .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(MailboxSecretArgs, TestContext.Current.CancellationToken));

            exception.Message.ShouldContain(offendingClientId);
            exception.Message.ShouldContain("chatbot-service-client-grant-expiry");
        }
        finally
        {
            await File.WriteAllTextAsync(realmPath, original, TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
    }

    /// <summary>A service-client submission shaped like the CLI automation client's.</summary>
    private static ChatBotCommandSubmission ServiceClientSubmission()
        => new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "service-account")], "test")),
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = "RecordGovernedNote",
                Command = new { noteId = "01ARZ3NDEKTSV4RRFFQ69G5FAX" },
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            null,
            ChatBotSurfaceOrigin.Cli);

    /// <summary>
    /// The claims a service client's Keycloak token carries, with the grant expiry taken VERBATIM from the rendered
    /// realm. Every other claim is a well-formed constant, so the only thing under test is the rendered expiry.
    /// </summary>
    private static ChatBotAuthenticatedActor ServiceClientActor(string clientId, string renderedExpiry)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim("sub", "service-account"),
                new Claim(ClaimsServiceClientGrantResolver.ServiceClientIdClaim, clientId),
                new Claim(ClaimsServiceClientGrantResolver.ServiceClientClassClaim, "cli-automation"),
                new Claim(ClaimsServiceClientGrantResolver.GrantIdClaim, "01ARZ3NDEKTSV4RRFFQ69G5FAV"),
                new Claim(ClaimsServiceClientGrantResolver.GrantTenantClaim, "tenant-alpha"),
                new Claim(ClaimsServiceClientGrantResolver.GrantExpiryClaim, renderedExpiry),
                new Claim(ClaimsServiceClientGrantResolver.GrantScopeClaim, "notes.write"),
                new Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, "RecordGovernedNote"),
                new Claim(ClaimsServiceClientGrantResolver.GrantSurfaceClaim, "cli"),
                new Claim(ClaimsServiceClientGrantResolver.CommandSetVersionClaim, "command-set-v1"),
            ],
            "test"));

        return new ChatBotAuthenticatedActor("service-account", principal, "service", clientId);
    }

    private static bool OwnerMarkerStartsWith(string directory, string ownerPrefix)
    {
        string markerPath = Path.Combine(directory, "owner.marker");
        try
        {
            return File.Exists(markerPath)
                && File.ReadAllText(markerPath).StartsWith(ownerPrefix, StringComparison.Ordinal);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    [Fact]
    public void RecoverySandboxKeepsLogicalControlLocatorSeparateFromPhysicalCommandTenant()
    {
        string appHost = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "Program.cs"));
        string composer = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "tests",
            "Hexalith.ChatBot.IntegrationTests",
            "Recovery",
            "RecoverySandboxTopologyComposer.cs"));
        string sandbox = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "tests",
            "Hexalith.ChatBot.RecoverySandbox",
            "Program.cs"));

        appHost.ShouldNotContain("Recovery__StorageTenantRef");
        composer.ShouldContain("string storageTenantRef");
        composer.ShouldContain("Recovery__StorageTenantRef");
        sandbox.ShouldContain("Recovery:StorageTenantRef");
        sandbox.ShouldContain("GraphMailboxIntakeWorker worker = new(");
        sandbox.ShouldContain("storageTenantRef,");
    }

    [Fact]
    public async Task RecoveryTier3LaneBoundsEventStoreGracefulShutdownInsideAspireCommandDeadline()
    {
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(MailboxSecretArgs, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        try
        {
            IResource eventStore = builder.Resources.Single(resource =>
                string.Equals(resource.Name, "eventstore", StringComparison.Ordinal));
            LiveRecoveryTopologyConfiguration.ConfigureEventStore(eventStore);

            IReadOnlyDictionary<string, string> environment = await ResolveEnvironmentAsync(eventStore).ConfigureAwait(true);

            environment["DOTNET_SHUTDOWNTIMEOUTSECONDS"].ShouldBe("5");
            environment["EventStore__RateLimiting__PermitLimit"].ShouldBe("100000");
            environment["EventStore__RateLimiting__ConsumerPermitLimit"].ShouldBe("10000");
        }
        finally
        {
            await builder.DisposeAsync().ConfigureAwait(true);
        }
    }

    private static async Task<IReadOnlyDictionary<string, string>> ResolveEnvironmentAsync(IResource resource)
    {
        Dictionary<string, object> environment = new(StringComparer.Ordinal);
        EnvironmentCallbackContext context = new(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            resource,
            environment,
            TestContext.Current.CancellationToken);
        foreach (EnvironmentCallbackAnnotation annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(context).ConfigureAwait(true);
        }

        return environment.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value switch
            {
                string text => text,
                null => string.Empty,
                _ => pair.Value.ToString() ?? string.Empty,
            },
            StringComparer.Ordinal);
    }

    private static string Tenant(JsonElement user)
        => user.GetProperty("attributes").GetProperty("tenants")[0].GetString()!;

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
