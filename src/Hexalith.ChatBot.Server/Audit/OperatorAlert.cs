namespace Hexalith.ChatBot.Server.Audit;

internal sealed record OperatorAlert(
    OperatorAlertKind Kind,
    string ReasonCode,
    string TenantId,
    string CommandName,
    string CorrelationId,
    DateTimeOffset RaisedAt);
