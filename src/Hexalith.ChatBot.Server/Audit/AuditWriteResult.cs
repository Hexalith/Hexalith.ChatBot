namespace Hexalith.ChatBot.Server.Audit;

internal sealed record AuditWriteResult(bool Succeeded, string ReasonCode)
{
    public static AuditWriteResult Success { get; } = new(true, "audit_written");

    public static AuditWriteResult Unavailable(string reasonCode = AuditFailureReasonCodes.AuditUnavailable)
        => new(false, reasonCode);
}
