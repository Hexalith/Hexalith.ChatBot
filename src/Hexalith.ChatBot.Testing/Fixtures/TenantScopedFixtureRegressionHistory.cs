using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Testing.Fixtures;

/// <summary>
/// Regression history slot.
/// </summary>
/// <param name="RunId">The optional run identifier.</param>
/// <param name="Outcome">The optional run outcome.</param>
/// <param name="RecordedAt">The optional run timestamp.</param>
public sealed record TenantScopedFixtureRegressionHistory(
    [property: JsonPropertyName("runId")] string? RunId,
    [property: JsonPropertyName("outcome")] string? Outcome,
    [property: JsonPropertyName("recordedAt")] DateTimeOffset? RecordedAt);
