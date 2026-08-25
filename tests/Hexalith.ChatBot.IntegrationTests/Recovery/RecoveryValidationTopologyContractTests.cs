using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.ChatBot.AppHost.Aspire;

using Microsoft.Extensions.Configuration;

using Hexalith.ChatBot.Server.Audit;
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
        await File.WriteAllTextAsync(
            Path.Combine(abandoned, "owner.marker"),
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
                "a directory whose owner cannot be established as live must be swept.");
            Directory.Exists(live).ShouldBeTrue(
                "a directory whose owner process is still running must never be swept, however old it is.");

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
            foreach (string? directory in new[] { generatedDirectory, live, abandoned })
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
