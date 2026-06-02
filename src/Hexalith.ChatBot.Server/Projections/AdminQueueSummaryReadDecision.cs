namespace Hexalith.ChatBot.Server.Projections;

internal sealed record AdminQueueSummaryReadDecision(
    bool IsAllowed,
    string ReasonCode,
    string RedactionState)
{
    public static AdminQueueSummaryReadDecision Allowed()
        => new(true, "allowed", "metadata_only");

    public static AdminQueueSummaryReadDecision Denied(string reasonCode)
        => new(false, reasonCode, "metadata_only");
}
