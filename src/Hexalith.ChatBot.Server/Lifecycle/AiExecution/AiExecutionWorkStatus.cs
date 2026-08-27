namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal enum AiExecutionWorkStatus
{
    Pending,
    Executing,
    CancellationRequested,
    CompletionPending,
    Terminal,
    Exhausted,
    Quarantined,
}
