namespace Hexalith.ChatBot.Server.Audit;

internal sealed record OperatorAlert(
    OperatorAlertKind Kind,
    string ReasonCode,
    string TenantId,
    string CommandName,
    string CorrelationId,
    DateTimeOffset RaisedAt,
    // Story 9.1 (NFR49a): an optional, metadata-only locator token pointing at the first detected break in a WORM
    // chain (e.g. the per-tenant sequence of the offending record). Null for every pre-9.1 alert kind.
    string? FirstBreakLocator = null);
