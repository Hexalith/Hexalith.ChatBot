using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Tests.Observability;

/// <summary>
/// Test double that records every <see cref="IChatBotMetrics"/> emission so instrumentation-point tests can assert
/// a seam recorded the right operation-class + bound tenant exactly once, without an exporter.
/// </summary>
internal sealed class RecordingChatBotMetrics : IChatBotMetrics
{
    public List<(string OperationClass, string TenantId, double Milliseconds)> Latencies { get; } = [];

    public List<string> RetryExhaustedTenants { get; } = [];

    public List<string> DuplicateSuppressedTenants { get; } = [];

    public void RecordIngestionLatency(string tenantId, double milliseconds)
        => Latencies.Add((ChatBotOperationClasses.MessageIntake, tenantId, milliseconds));

    public void RecordAssociationLatency(string tenantId, double milliseconds)
        => Latencies.Add((ChatBotOperationClasses.Association, tenantId, milliseconds));

    public void RecordApprovalLatency(string tenantId, double milliseconds)
        => Latencies.Add((ChatBotOperationClasses.Approval, tenantId, milliseconds));

    public void RecordCommandExecutionLatency(string tenantId, double milliseconds)
        => Latencies.Add((ChatBotOperationClasses.CommandExecution, tenantId, milliseconds));

    public void RecordRetryExhausted(string tenantId) => RetryExhaustedTenants.Add(tenantId);

    public void RecordDuplicateSuppressed(string tenantId) => DuplicateSuppressedTenants.Add(tenantId);
}
