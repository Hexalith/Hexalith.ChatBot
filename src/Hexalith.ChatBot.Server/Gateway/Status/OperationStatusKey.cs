namespace Hexalith.ChatBot.Server.Gateway.Status;

/// <summary>The tenant-partitioned status identity.</summary>
/// <param name="TenantId">The trusted tenant partition.</param>
/// <param name="OperationId">The exact lifetime operation identity.</param>
internal sealed record OperationStatusKey(string TenantId, string OperationId);
