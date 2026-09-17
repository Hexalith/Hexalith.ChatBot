using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Testing.Fixtures;

/// <summary>
/// Dataset partition metadata.
/// </summary>
/// <param name="Name">The partition name.</param>
/// <param name="Purpose">The partition purpose.</param>
public sealed record TenantScopedFixturePartition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("purpose")] string Purpose);
