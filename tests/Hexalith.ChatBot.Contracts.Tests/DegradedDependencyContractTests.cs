using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

/// <summary>
/// Story 8.5 AC2/AC3: contract coverage for the metadata-only degraded-dependency incident and its finite-token
/// validator — accepts a well-formed incident, rejects each individual safe-token/enum/UTC/budget/catalog
/// violation, and stays metadata-only (no restricted detail) under serialization.
/// </summary>
public static class DegradedDependencyContractTests
{
    [Fact]
    public static void ValidatorAcceptsAWellFormedIncident()
    {
        DegradedDependencyContractValidator.IsValid(Valid()).ShouldBeTrue();
        DegradedDependencyContractValidator.Validate(Valid()).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ChatBotHealthStatus.Healthy)]
    [InlineData(ChatBotHealthStatus.Unknown)]
    public static void ValidatorRejectsANonDegradedOrFailedHealth(ChatBotHealthStatus health)
        => DegradedDependencyContractValidator.Validate(Valid() with { Health = health }).ShouldContain("health_invalid");

    [Fact]
    public static void ValidatorRejectsANonUtcDetectionTimestamp()
        => DegradedDependencyContractValidator
            .Validate(Valid() with { DetectedAtUtc = new DateTimeOffset(2026, 6, 3, 6, 0, 0, TimeSpan.FromHours(2)) })
            .ShouldContain("detected_at_not_utc");

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(301)]
    public static void ValidatorRejectsAnOutOfRangeDetectionBudget(int budget)
        => DegradedDependencyContractValidator
            .Validate(Valid() with { DetectionBudgetSeconds = budget })
            .ShouldContain("detection_budget_invalid");

    [Theory]
    [InlineData(DependencyScopeKind.Unknown)]
    [InlineData((DependencyScopeKind)999)]
    public static void ValidatorRejectsAnUnknownOrUndefinedScopeKind(DependencyScopeKind scopeKind)
        => DegradedDependencyContractValidator
            .Validate(Valid() with { ScopeKind = scopeKind })
            .ShouldContain("scope_kind_invalid");

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("has space")]
    [InlineData("contains-secret")]
    [InlineData("bearer-xyz")]
    [InlineData("payload.json")]
    public static void ValidatorRejectsNonSafeOrMarkerBannedStringFields(string unsafeToken)
    {
        DegradedDependencyContractValidator.Validate(Valid() with { DependencyId = unsafeToken }).ShouldContain("dependency_id_invalid");
        DegradedDependencyContractValidator.Validate(Valid() with { AffectedScope = unsafeToken }).ShouldContain("affected_scope_invalid");
        DegradedDependencyContractValidator.Validate(Valid() with { OwnerRole = unsafeToken }).ShouldContain("owner_role_invalid");
        DegradedDependencyContractValidator.Validate(Valid() with { NextSafeAction = unsafeToken }).ShouldContain("next_safe_action_invalid");
        DegradedDependencyContractValidator.Validate(Valid() with { CorrelationId = unsafeToken }).ShouldContain("correlation_id_invalid");
    }

    [Fact]
    public static void ValidatorRejectsAReasonCodeOutsideTheFr77Catalog()
    {
        // A safe token that is simply not a member of the catalog is rejected.
        DegradedDependencyContractValidator.Validate(Valid() with { ReasonCode = "not_a_catalog_code" }).ShouldContain("reason_code_invalid");
        // A genuine catalog code is accepted.
        DegradedDependencyContractValidator.Validate(Valid() with { ReasonCode = ChatBotMessageCodes.RetryExhausted }).ShouldNotContain("reason_code_invalid");
    }

    [Fact]
    public static void IncidentStaysMetadataOnlyUnderSerialization()
    {
        string json = JsonSerializer.Serialize(Valid(), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("mailbox:mb-01");
        json.ShouldContain("degraded");
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("password", Case.Insensitive);
        json.ShouldNotContain("bearer", Case.Insensitive);
    }

    private static DegradedDependencyIncident Valid()
        => new(
            DependencyId: "graph-subscription",
            ScopeKind: DependencyScopeKind.Mailbox,
            AffectedScope: "mailbox:mb-01",
            Health: ChatBotHealthStatus.Degraded,
            DetectedAtUtc: new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.Zero),
            DetectionBudgetSeconds: DegradedDependencyContractValidator.DefaultDetectionBudgetSeconds,
            OwnerRole: "mailbox-admin",
            NextSafeAction: "renew-graph-subscription",
            ReasonCode: ChatBotMessageCodes.DegradedMailbox,
            CorrelationId: "correlation-alpha");
}
