using Dapr.Workflow;

using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Records the shared observable UTC clock as an activity result so workflow replay stays deterministic.</summary>
internal sealed class CorrectionPropagationClockActivity(ISystemClock clock) : WorkflowActivity<string, DateTimeOffset>
{
    /// <summary>Returns the same UTC authority used by operation-status writers and reads.</summary>
    public override Task<DateTimeOffset> RunAsync(WorkflowActivityContext context, string input)
        => Task.FromResult(clock.UtcNow.ToUniversalTime());
}
