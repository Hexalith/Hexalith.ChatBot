namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// No-op <see cref="IChatBotMetrics"/> used as the default at instrumentation seams when no real metrics
/// implementation is injected (e.g. existing unit tests that construct a stage directly). Emission is purely
/// passive observability, so a missing meter must never change behaviour — the seams coalesce a null dependency to
/// <see cref="Instance"/> and keep working exactly as before.
/// </summary>
internal sealed class NullChatBotMetrics : IChatBotMetrics
{
    public static readonly NullChatBotMetrics Instance = new();

    private NullChatBotMetrics()
    {
    }

    public void RecordIngestionLatency(string tenantId, double milliseconds)
    {
    }

    public void RecordAssociationLatency(string tenantId, double milliseconds)
    {
    }

    public void RecordApprovalLatency(string tenantId, double milliseconds)
    {
    }

    public void RecordCommandExecutionLatency(string tenantId, double milliseconds)
    {
    }

    public void RecordRetryExhausted(string tenantId)
    {
    }

    public void RecordDuplicateSuppressed(string tenantId)
    {
    }
}
