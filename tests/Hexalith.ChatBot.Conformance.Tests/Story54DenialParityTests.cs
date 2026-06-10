using System.Text.Json;

using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Server.Gateway;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Story 5.4 denial conformance coverage. These cases prove high-risk authorization denials remain equivalent
/// across the UI/API, CLI, and MCP origins at the comparable record level: problem category/code/action,
/// correlation semantics, metadata-only details, audit reason, and zero durable work.
/// </summary>
public static class Story54DenialParityTests
{
    [Theory]
    [InlineData("authentication-denied", ChatBotAuthorizationReasonCodes.AuthenticationDenied, 401)]
    [InlineData("stale-grant", ChatBotAuthorizationReasonCodes.ServiceClientGrantExpired, 403)]
    [InlineData("revoked-grant", ChatBotAuthorizationReasonCodes.ServiceClientGrantRevoked, 403)]
    [InlineData("wrong-surface", ChatBotAuthorizationReasonCodes.ServiceClientWrongSurface, 403)]
    [InlineData("unknown-resource", ChatBotAuthorizationReasonCodes.SafeNotFound, 403)]
    [InlineData("tenant-mismatch", ChatBotAuthorizationReasonCodes.TenantMismatch, 403)]
    public static async Task CriticalAuthorizationDenialsShouldBeEquivalentAcrossRequiredSurfaces(
        string caseName,
        string expectedReasonCode,
        int expectedStatus)
    {
        DenialOutcome[] outcomes = await RunCaseAsync(caseName);

        outcomes.Select(static outcome => outcome.ArmName).ShouldBe(["ui-api", "cli", "mcp"], caseName);
        outcomes.Select(static outcome => outcome.SurfaceOrigin).ShouldBe(["ui", "cli", "mcp"], caseName);

        IReadOnlyList<KeyValuePair<string, string>> baseline = ComparableFacts(outcomes[0]);
        foreach (DenialOutcome outcome in outcomes)
        {
            outcome.ReasonCode.ShouldBe(expectedReasonCode, caseName);
            outcome.Status.ShouldBe(expectedStatus, caseName);
            outcome.DetailsVisibility.ShouldBe("Metadata_only", caseName);
            outcome.DispatchCount.ShouldBe(0, caseName);
            outcome.CoarseIdempotencyRecordCount.ShouldBe(0, caseName);
            ComparableFacts(outcome).ShouldBe(baseline, caseName);
            AssertNoRestrictedLeakage(outcome);
        }
    }

    private static async Task<DenialOutcome[]> RunCaseAsync(string caseName)
    {
        List<DenialOutcome> outcomes = [];
        foreach (ISurfaceArm arm in SurfaceArms.All)
        {
            outcomes.Add(caseName switch
            {
                "authentication-denied" => await DenialConformanceHarness
                    .RunAuthenticationDeniedAsync(arm, TestContext.Current.CancellationToken)
                    .ConfigureAwait(false),
                "tenant-mismatch" => await DenialConformanceHarness
                    .RunTenantMismatchAsync(arm, TestContext.Current.CancellationToken)
                    .ConfigureAwait(false),
                "stale-grant" => await DenialConformanceHarness
                    .RunAuthorizationDeniedAsync(arm, ChatBotAuthorizationReasonCodes.ServiceClientGrantExpired, TestContext.Current.CancellationToken)
                    .ConfigureAwait(false),
                "revoked-grant" => await DenialConformanceHarness
                    .RunAuthorizationDeniedAsync(arm, ChatBotAuthorizationReasonCodes.ServiceClientGrantRevoked, TestContext.Current.CancellationToken)
                    .ConfigureAwait(false),
                "wrong-surface" => await DenialConformanceHarness
                    .RunAuthorizationDeniedAsync(arm, ChatBotAuthorizationReasonCodes.ServiceClientWrongSurface, TestContext.Current.CancellationToken)
                    .ConfigureAwait(false),
                "unknown-resource" => await DenialConformanceHarness
                    .RunAuthorizationDeniedAsync(arm, ChatBotAuthorizationReasonCodes.SafeNotFound, TestContext.Current.CancellationToken)
                    .ConfigureAwait(false),
                _ => throw new InvalidOperationException($"Unknown Story 5.4 denial case '{caseName}'."),
            });
        }

        return [.. outcomes];
    }

    private static IReadOnlyList<KeyValuePair<string, string>> ComparableFacts(DenialOutcome outcome)
        =>
        [
            new("reasonCode", outcome.ReasonCode),
            new("category", outcome.Category),
            new("code", outcome.Code),
            new("clientAction", outcome.ClientAction),
            new("detailsVisibility", outcome.DetailsVisibility),
            new("correlationId", outcome.CorrelationId),
            new("taskId", outcome.TaskId ?? string.Empty),
            new("status", outcome.Status.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("dispatchCount", outcome.DispatchCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("coarseIdempotencyRecordCount", outcome.CoarseIdempotencyRecordCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        ];

    private static void AssertNoRestrictedLeakage(DenialOutcome outcome)
    {
        string serialized = JsonSerializer.Serialize(outcome, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        foreach (string sentinel in RestrictedLeakageSentinels())
        {
            serialized.ShouldNotContain(sentinel, Case.Insensitive);
        }
    }

    private static string[] RestrictedLeakageSentinels()
        =>
        [
            "restricted project",
            "candidate evidence",
            "file metadata",
            "cursor-token",
            "bearer-token",
            "raw-claim",
            "provider-payload",
            "stack trace",
            "audit internals",
            "/restricted/path",
        ];
}
