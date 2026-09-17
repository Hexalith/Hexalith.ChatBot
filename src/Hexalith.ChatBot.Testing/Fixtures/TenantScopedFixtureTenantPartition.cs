using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Testing.Fixtures;

/// <summary>
/// Tenant partition metadata.
/// </summary>
/// <param name="TenantId">The synthetic tenant identifier.</param>
/// <param name="Alias">The partition alias used in diagnostics.</param>
/// <param name="Role">The tenant's fixture role.</param>
public sealed record TenantScopedFixtureTenantPartition(
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("alias")] string Alias,
    [property: JsonPropertyName("role")] string Role);
