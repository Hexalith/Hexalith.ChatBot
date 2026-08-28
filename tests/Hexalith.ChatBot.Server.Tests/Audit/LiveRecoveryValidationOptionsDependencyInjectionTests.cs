using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 12.15 wiring guard: <c>AddChatBotCommandGateway</c> must bind
/// <see cref="LiveRecoveryValidationOptions"/> from the <c>ChatBot:LiveRecoveryValidation</c> configuration section
/// and fail closed as soon as it is resolved when an enabled configuration violates
/// <see cref="LiveRecoveryValidationOptions.Validate"/>. Without the section actually bound, a deployment flipping
/// <c>ChatBot:LiveRecoveryValidation:Enabled=true</c> (or a Production environment name, or a non-replay-test
/// tenant) would silently keep the disabled defaults instead of failing startup.
/// </summary>
public sealed class LiveRecoveryValidationOptionsDependencyInjectionTests
{
    [Fact]
    public void EnabledProductionConfigurationFailsOptionsValidationOnResolve()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ChatBot:LiveRecoveryValidation:Enabled"] = "true",
                ["ChatBot:LiveRecoveryValidation:EnvironmentName"] = "Production",
            })
            .Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);
        _ = services.AddChatBotCommandGateway();
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Should.Throw<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<LiveRecoveryValidationOptions>>().Value);
        exception.Message.ShouldContain(nameof(LiveRecoveryValidationOptions.EnvironmentName));
    }

    [Fact]
    public void EnabledNonReplayTestTenantConfigurationFailsOptionsValidationOnResolve()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ChatBot:LiveRecoveryValidation:Enabled"] = "true",
                ["ChatBot:LiveRecoveryValidation:EnvironmentName"] = "Testing",
                ["ChatBot:LiveRecoveryValidation:TestTenantRef"] = "tenant-alpha",
            })
            .Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);
        _ = services.AddChatBotCommandGateway();
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Should.Throw<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<LiveRecoveryValidationOptions>>().Value);
        exception.Message.ShouldContain("replay-test:");
    }

    [Fact]
    public void EnabledCompleteTestingConfigurationBindsAndValidatesSuccessfully()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ChatBot:LiveRecoveryValidation:Enabled"] = "true",
                ["ChatBot:LiveRecoveryValidation:EnvironmentName"] = "Testing",
                ["ChatBot:LiveRecoveryValidation:TestTenantRef"] = "replay-test:recovery-validation",
                ["ChatBot:LiveRecoveryValidation:DatasetRef"] = "recovery-baseline",
                ["ChatBot:LiveRecoveryValidation:DatasetVersion"] = "v1",
                ["ChatBot:LiveRecoveryValidation:DatasetVolume"] = "6",
                ["ChatBot:LiveRecoveryValidation:ProjectionSchemaVersion"] = "chatbot.project-conversation-source-email.v1",
                ["ChatBot:LiveRecoveryValidation:ValidationPartitionRef"] = "recovery-partition-v1",
                ["ChatBot:LiveRecoveryValidation:ControllerCapability"] = LiveRecoveryValidationOptions.AspireControllerCapability,
                ["ChatBot:LiveRecoveryValidation:ControllerSecret"] = "injected-by-tier3",
                ["ChatBot:LiveRecoveryValidation:PerScenarioTimeout"] = "00:25:00",
                ["ChatBot:LiveRecoveryValidation:WorkflowTimeout"] = "05:00:00",
                ["ChatBot:LiveRecoveryValidation:EvidenceDirectory"] = Path.GetFullPath("TestResults/live-recovery"),
                ["ChatBot:LiveRecoveryValidation:EvidenceLocator"] = "artifact:live-recovery-validation-evidence",
            })
            .Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);
        _ = services.AddChatBotCommandGateway();
        using ServiceProvider provider = services.BuildServiceProvider();

        LiveRecoveryValidationOptions options = provider.GetRequiredService<IOptions<LiveRecoveryValidationOptions>>().Value;

        options.Enabled.ShouldBeTrue();
        options.EnvironmentName.ShouldBe("Testing");
        options.TestTenantRef.ShouldBe("replay-test:recovery-validation");
        options.Validate().ShouldBeNull();
    }

    [Fact]
    public void DisabledDefaultConfigurationBindsAndValidatesSuccessfullyWithoutTheSectionPresent()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);
        _ = services.AddChatBotCommandGateway();
        using ServiceProvider provider = services.BuildServiceProvider();

        LiveRecoveryValidationOptions options = provider.GetRequiredService<IOptions<LiveRecoveryValidationOptions>>().Value;
        IRecoveryValidationEvidenceRetentionFailureSink retentionFailureSink =
            provider.GetRequiredService<IRecoveryValidationEvidenceRetentionFailureSink>();

        options.Enabled.ShouldBeFalse();
        retentionFailureSink.ShouldBeSameAs(DiscardingRecoveryValidationEvidenceRetentionFailureSink.Instance);
    }
}
