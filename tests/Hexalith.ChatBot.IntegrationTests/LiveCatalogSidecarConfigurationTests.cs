using System.Text.Json.Nodes;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>Verifies the configuration written for sidecars in the live catalog fixture.</summary>
public sealed class LiveCatalogSidecarConfigurationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("spec: {}\n")]
    [InlineData("spec:\n  tracing:\n    samplingRate: '1'\n  mtls:\n    enabled: false\n")]
    public void AppliesIsolatedDiscoveryAndMutualTlsWithOrWithoutExistingSpec(string spec)
    {
        const string header = "apiVersion: dapr.io/v1alpha1\nkind: Configuration\nmetadata:\n  name: test-sidecar\n";
        JsonObject config = LiveCatalogFixture.PrepareSidecarConfiguration(header + spec, "isolated-discovery.db");

        config["metadata"]!["name"]!.GetValue<string>().ShouldBe("test-sidecar");
        JsonNode configuredSpec = config["spec"].ShouldNotBeNull();
        configuredSpec["mtls"]!["enabled"]!.GetValue<bool>().ShouldBeTrue();
        JsonNode discovery = configuredSpec["nameResolution"].ShouldNotBeNull();
        discovery["component"]!.GetValue<string>().ShouldBe("sqlite");
        discovery["configuration"]!["connectionString"]!.GetValue<string>().ShouldBe("isolated-discovery.db");
        if (spec.Contains("tracing", StringComparison.Ordinal))
        {
            configuredSpec["tracing"]!["samplingRate"]!.GetValue<string>().ShouldBe("1");
        }
    }

    [Theory]
    [InlineData("[]", "The selected sidecar configuration must be an object.")]
    [InlineData("spec: []", "The selected sidecar configuration spec must be an object.")]
    [InlineData("spec: null", "The selected sidecar configuration spec must be an object.")]
    public void RejectsNonObjectConfigurationShapes(string yaml, string diagnostic)
        => Should.Throw<InvalidOperationException>(() => LiveCatalogFixture.PrepareSidecarConfiguration(yaml, "unused.db"))
            .Message.ShouldBe(diagnostic);
}
