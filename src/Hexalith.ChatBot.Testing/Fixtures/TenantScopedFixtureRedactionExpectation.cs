using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Testing.Fixtures;

/// <summary>
/// Redaction expectation for a fixture case.
/// </summary>
/// <param name="Mode">The expected redaction mode.</param>
/// <param name="ForbiddenPayloadClasses">Payload classes that must not appear in diagnostics.</param>
public sealed record TenantScopedFixtureRedactionExpectation(
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("forbiddenPayloadClasses")] IReadOnlyList<string> ForbiddenPayloadClasses);
