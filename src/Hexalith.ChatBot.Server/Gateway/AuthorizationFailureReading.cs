namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>
/// A coarse, metadata-only per-tenant authorization-failure rolling-window count (Story 8.4, AC5). It carries only
/// the tenant ref, the aggregate failure count within the window, and the window start instant — never an actor id,
/// command type, reason code, or any per-failure detail (NFR2). The count is the aggregate failure count (an integer,
/// never a percentile).
/// </summary>
internal sealed record AuthorizationFailureReading(string TenantId, int FailureCount, DateTimeOffset WindowStartUtc);
