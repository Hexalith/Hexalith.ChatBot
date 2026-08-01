using System.Text.Json;

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Story 12.15 Task 2 topology guards for the reserved identity, controls, and deterministic dataset.</summary>
public sealed class RecoveryValidationTopologyContractTests
{
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
        appHost.ShouldContain("RandomNumberGenerator.GetBytes");
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
        root.GetProperty("projectionSchemaVersion").GetString().ShouldBe("project-conversation-v1");
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
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        ordinary.Resources.ShouldNotContain(resource => string.Equals(resource.Name, "recovery-sandbox", StringComparison.Ordinal));

        IDistributedApplicationTestingBuilder recovery = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        _ = recovery.AddRecoverySandbox(
            "Testing",
            "replay-test:recovery-validation",
            "recovery-validation",
            LiveRecoveryValidationOptions.AspireControllerCapability,
            "tier3-injected-value",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        recovery.Resources.ShouldContain(resource => string.Equals(resource.Name, "recovery-sandbox", StringComparison.Ordinal));
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
            .ToArray();

        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(args, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

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
    public void RecoveryTier3LaneBoundsEventStoreGracefulShutdownInsideAspireCommandDeadline()
    {
        string source = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "tests",
            "Hexalith.ChatBot.IntegrationTests",
            "Recovery",
            "LiveContinuityAspireE2eTests.cs"));

        source.ShouldContain("DOTNET_SHUTDOWNTIMEOUTSECONDS");
        source.ShouldContain("= \"5\"");
        source.ShouldContain("EventStore__RateLimiting__PermitLimit");
        source.ShouldContain("EventStore__RateLimiting__ConsumerPermitLimit");
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
