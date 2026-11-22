using Hexalith.ChatBot.Server.Operations;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Commits owner-observed review transitions before local work can be marked complete.</summary>
internal interface IWorkflowReviewCommandWriter
{
    Task RecordAsync(string tenant, string correlation, RecordWorkflowReviewTransition command, CancellationToken token);
}
