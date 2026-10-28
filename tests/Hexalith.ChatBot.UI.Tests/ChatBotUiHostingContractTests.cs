using Shouldly;

using static Hexalith.ChatBot.UI.Tests.ComponentContractSource;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ChatBotUiHostingContractTests
{
    [Fact]
    public void UiHostDefaultsShouldPreserveServiceDiscoveryTelemetryResilienceAndHealthProbes()
    {
        string defaults = ReadProjectFile("src/Hexalith.ChatBot.UI/Hosting/ChatBotUiHostDefaultsExtensions.cs");
        string project = ReadProjectFile("src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj");

        defaults.ShouldContain("AddServiceDiscovery");
        defaults.ShouldContain("AddStandardResilienceHandler");
        defaults.ShouldContain("DisableForUnsafeHttpMethods");
        defaults.ShouldContain("resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30)");
        defaults.ShouldContain("resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60)");
        defaults.ShouldContain("resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60)");
        defaults.ShouldContain("AddOpenTelemetry");
        defaults.ShouldContain("UseOtlpExporter");
        defaults.ShouldContain("MapGet(\"/health\"");
        defaults.ShouldContain("MapGet(\"/alive\"");
        project.ShouldContain("Microsoft.Extensions.Http.Resilience");
        project.ShouldContain("Microsoft.Extensions.ServiceDiscovery");
        project.ShouldContain("OpenTelemetry.Extensions.Hosting");
        project.ShouldNotContain("Hexalith.ChatBot.ServiceDefaults");
    }
}
