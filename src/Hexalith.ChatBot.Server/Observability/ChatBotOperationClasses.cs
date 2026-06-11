namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// The finite, bounded operation-class taxonomy used as the low-cardinality <c>operation-class</c> metric
/// dimension (Story 8.2, AC3). These are the same stable literal tokens that already flow on
/// <c>OperationStatus.OperationClass</c>, <c>MailboxIntakeWorkerResult.OperationClass</c> and the retry/duplicate
/// terminal paths — centralised here so every emission validates against one closed set and no free-form or
/// high-cardinality value can ever become a dimension.
/// </summary>
internal static class ChatBotOperationClasses
{
    public const string MessageIntake = "message-intake";
    public const string Association = "association";
    public const string Approval = "approval";
    public const string CommandExecution = "command-execution";
    public const string Retry = "retry";
    public const string DuplicateHandling = "duplicate-handling";
    public const string Workflow = "workflow";
    public const string AuditProjectionLag = "audit-projection-lag";

    /// <summary>Story 9.2 (NFR50a): the audit-completeness observable-gauge collection path.</summary>
    public const string AuditCompleteness = "audit-completeness";

    /// <summary>The closed set of valid operation-class tokens. Any value outside this set is rejected before emission.</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        MessageIntake,
        Association,
        Approval,
        CommandExecution,
        Retry,
        DuplicateHandling,
        Workflow,
        AuditProjectionLag,
        AuditCompleteness,
    };

    public static bool IsKnown(string operationClass) => All.Contains(operationClass);
}
