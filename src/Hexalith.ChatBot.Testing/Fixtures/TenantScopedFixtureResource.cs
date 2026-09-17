using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Testing.Fixtures;

/// <summary>
/// Tenant-owned resource reference.
/// </summary>
/// <param name="ResourceType">The resource type.</param>
/// <param name="TenantId">The tenant that owns the resource.</param>
/// <param name="ResourceId">The stable resource identifier.</param>
public sealed record TenantScopedFixtureResource(
    [property: JsonPropertyName("resourceType")] string ResourceType,
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("resourceId")] string ResourceId);
