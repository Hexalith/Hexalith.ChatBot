namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

/// <summary>Closed operation identifiers for tenant-scoped execution recovery.</summary>
internal static class AiExecutionRecoveryOperations
{
    /// <summary>Lists exhausted execution metadata within the bound tenant.</summary>
    public const string List = "chatbot-ai-execution-exhausted";
    /// <summary>Requeues one exhausted execution within the bound tenant.</summary>
    public const string Recover = "chatbot-ai-execution-recover";
}
