using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Testing.Fixtures;

/// <summary>
/// Fixture expected outcome.
/// </summary>
/// <param name="State">The expected state.</param>
/// <param name="ReasonCode">The expected reason code.</param>
/// <param name="RedactionState">The expected redaction state.</param>
/// <param name="AuditExpectation">The expected audit behavior.</param>
public sealed record TenantScopedFixtureExpectedOutcome(
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("reasonCode")] string ReasonCode,
    [property: JsonPropertyName("redactionState")] string RedactionState,
    [property: JsonPropertyName("auditExpectation")] string AuditExpectation);
