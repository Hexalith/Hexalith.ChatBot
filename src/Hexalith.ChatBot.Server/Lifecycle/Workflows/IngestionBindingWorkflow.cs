using Dapr.Workflow;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>ChatBot-owned durable workflow that ingests every governed source then finalizes one binding.</summary>
internal sealed class IngestionBindingWorkflow
    : Workflow<IngestionBindingRequest, IngestionBindingWorkflowResult>
{
    public override Task<IngestionBindingWorkflowResult> RunAsync(
        WorkflowContext context,
        IngestionBindingRequest input)
        => LegacyIngestionBindingWorkflowRunner.RunAsync(input, new DaprIngestionBindingWorkflowSteps(context));
}
