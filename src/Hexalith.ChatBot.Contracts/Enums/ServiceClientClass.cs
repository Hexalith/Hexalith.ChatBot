using System.Runtime.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

public enum ServiceClientClass
{
    [EnumMember(Value = "cli-automation")]
    CliAutomation,

    [EnumMember(Value = "mcp-tool")]
    McpTool,

    [EnumMember(Value = "background-worker")]
    BackgroundWorker,

    [EnumMember(Value = "mailbox-ingestion")]
    MailboxIngestion,

    [EnumMember(Value = "audit-projection")]
    AuditProjection,

    [EnumMember(Value = "ai-action-execution")]
    AiActionExecution,
}
