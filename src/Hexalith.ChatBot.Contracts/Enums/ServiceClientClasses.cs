namespace Hexalith.ChatBot.Contracts.Enums;

public static class ServiceClientClasses
{
    public static bool TryFromWireValue(string? value, out ServiceClientClass clientClass)
    {
        clientClass = ServiceClientClass.CliAutomation;
        switch (value?.Trim().ToLowerInvariant())
        {
            case "cli-automation":
                clientClass = ServiceClientClass.CliAutomation;
                return true;
            case "mcp-tool":
                clientClass = ServiceClientClass.McpTool;
                return true;
            case "background-worker":
                clientClass = ServiceClientClass.BackgroundWorker;
                return true;
            case "mailbox-ingestion":
                clientClass = ServiceClientClass.MailboxIngestion;
                return true;
            case "audit-projection":
                clientClass = ServiceClientClass.AuditProjection;
                return true;
            case "ai-action-execution":
                clientClass = ServiceClientClass.AiActionExecution;
                return true;
            default:
                return false;
        }
    }

    public static string ToWireValue(ServiceClientClass clientClass)
        => clientClass switch
        {
            ServiceClientClass.CliAutomation => "cli-automation",
            ServiceClientClass.McpTool => "mcp-tool",
            ServiceClientClass.BackgroundWorker => "background-worker",
            ServiceClientClass.MailboxIngestion => "mailbox-ingestion",
            ServiceClientClass.AuditProjection => "audit-projection",
            ServiceClientClass.AiActionExecution => "ai-action-execution",
            _ => "cli-automation",
        };
}
