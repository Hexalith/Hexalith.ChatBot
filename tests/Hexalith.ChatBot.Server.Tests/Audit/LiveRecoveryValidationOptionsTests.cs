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
        // A per-scenario budget the sweep cannot afford serially is rejected at validation rather than being silently
        // truncated by the outer workflow deadline. This replaces the former `PerScenarioTimeout >= MaxRto` rule, which
        // demanded a 4-hour per-scenario budget that nine serial scenarios can never fit inside a sub-RunnerBudget
        // workflow — a rule that read as a measurement guarantee while being arithmetically unsatisfiable.
        LiveRecoveryValidationOptions options = ValidOptions();
        options.PerScenarioTimeout = options.WorkflowTimeout / LiveRecoveryValidationOptions.MinimumSweepScenarioCount;
        options.Validate().ShouldNotBeNull().ShouldContain(nameof(LiveRecoveryValidationOptions.WorkflowTimeout));

        options = ValidOptions();
        options.WorkflowTimeout = TimeSpan.FromMinutes(31);
        options.Validate().ShouldNotBeNull().ShouldContain(nameof(LiveRecoveryValidationOptions.WorkflowTimeout));

        // WorkflowTimeout must fail closed before the runner kills the job mid-injection.
        options = ValidOptions();
        options.WorkflowTimeout = options.RunnerBudget;
        options.Validate().ShouldNotBeNull().ShouldContain(nameof(LiveRecoveryValidationOptions.RunnerBudget));

        // TimeSpan.MaxValue overflowed the serial-budget multiplication, so ValidateOnStart reported a stack trace
        // instead of a configuration error.
        options = ValidOptions();
        options.PerScenarioTimeout = TimeSpan.MaxValue;
        options.Validate().ShouldNotBeNull().ShouldContain(nameof(LiveRecoveryValidationOptions.WorkflowTimeout));

        // A per-scenario budget just under the first guard's own threshold — TimeSpan.MaxValue / N passes
        // `PerScenarioTimeout > MaximumTimeSpan / MinimumSweepScenarioCount` (equal, not greater) — still overflows
        // once multiplied back out by N and added to TopologyMargin, and must be caught by the try/catch rather than
        // let an unhandled OverflowException escape ValidateOnStart.
        options = ValidOptions();
        options.PerScenarioTimeout = TimeSpan.MaxValue / LiveRecoveryValidationOptions.MinimumSweepScenarioCount;
        options.Validate().ShouldNotBeNull().ShouldContain(nameof(TimeSpan));

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
            ProjectionSchemaVersion = "chatbot.project-conversation-source-email.v1",
            ValidationPartitionRef = "recovery-partition-v1",
            ControllerCapability = LiveRecoveryValidationOptions.AspireControllerCapability,
            ControllerSecret = "injected-by-tier3",
            PerScenarioTimeout = TimeSpan.FromMinutes(25),
            WorkflowTimeout = TimeSpan.FromHours(5),
            EvidenceDirectory = Path.GetFullPath("TestResults/live-recovery"),
            EvidenceLocator = "artifact:live-recovery-validation-evidence",
        };
}
