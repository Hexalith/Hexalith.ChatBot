using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>Story 12.15 Task 2 configuration guard tests for the doubly opted-in live-recovery lane.</summary>
public sealed class LiveRecoveryValidationOptionsTests
{
    [Fact]
    public void DefaultsAreDisabledNonDestructiveAndValid()
    {
        LiveRecoveryValidationOptions options = new();

        options.Enabled.ShouldBeFalse();
        options.ControllerCapability.ShouldBeEmpty();
        options.ControllerSecret.ShouldBeEmpty();
        options.Validate().ShouldBeNull();
    }

    [Fact]
    public void EnabledConfigurationRequiresReplayTenantCapabilitySecretAndDatasetProvenance()
    {
        LiveRecoveryValidationOptions options = ValidOptions();
        options.TestTenantRef = "tenant-alpha";

        options.Validate().ShouldNotBeNull().ShouldContain("replay-test:");

        options = ValidOptions();
        options.ControllerSecret = string.Empty;
        options.Validate().ShouldNotBeNull().ShouldContain(nameof(LiveRecoveryValidationOptions.ControllerSecret));

        options = ValidOptions();
        options.DatasetVolume = 0;
        options.Validate().ShouldNotBeNull().ShouldContain(nameof(LiveRecoveryValidationOptions.DatasetVolume));
    }

    [Fact]
    public void CompleteSandboxConfigurationIsValid()
    {
        LiveRecoveryValidationOptions options = ValidOptions();

        options.Validate().ShouldBeNull();
    }

    [Fact]
    public void EnabledConfigurationCrossValidatesRecoveryScheduleAndWorkflowTimeouts()
    {
        LiveRecoveryValidationOptions options = ValidOptions();
        options.PerScenarioTimeout = RecoveryTargets.MaxRto - TimeSpan.FromTicks(1);
        options.Validate().ShouldNotBeNull().ShouldContain(nameof(LiveRecoveryValidationOptions.PerScenarioTimeout));

        options = ValidOptions();
        options.WorkflowTimeout = RecoveryTargets.MaxRto + options.RestorationTimeout;
        options.Validate().ShouldNotBeNull().ShouldContain(nameof(LiveRecoveryValidationOptions.WorkflowTimeout));

        options = ValidOptions();
        options.MaximumEvidenceAge = options.Cadence - TimeSpan.FromTicks(1);
        options.Validate().ShouldNotBeNull().ShouldContain(nameof(LiveRecoveryValidationOptions.MaximumEvidenceAge));
    }

    private static LiveRecoveryValidationOptions ValidOptions()
        => new()
        {
            Enabled = true,
            EnvironmentName = "Testing",
            TestTenantRef = "replay-test:recovery-validation",
            DatasetRef = "recovery-baseline",
            DatasetVersion = "v1",
            DatasetVolume = 6,
            ProjectionSchemaVersion = "project-conversation-v1",
            ValidationPartitionRef = "recovery-partition-v1",
            ControllerCapability = LiveRecoveryValidationOptions.AspireControllerCapability,
            ControllerSecret = "injected-by-tier3",
            PerScenarioTimeout = RecoveryTargets.MaxRto,
            WorkflowTimeout = TimeSpan.FromHours(5),
            EvidenceDirectory = Path.GetFullPath("TestResults/live-recovery"),
            EvidenceLocator = "artifact:live-recovery-validation",
        };
}
