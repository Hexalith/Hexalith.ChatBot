using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Differential-conformance harness — rejection intents (FR86 "including the rejection intents"). Both the
/// fine-idempotency re-record rejection and the fail-closed (non-allowlisted) rejection must yield an identical
/// rejection outcome across the UI/CLI/MCP arms, compared as a first-class event/problem record (never a bare
/// error code) and with no additional durable effect — differing only in the declared surface origin.
/// </summary>
public static class RejectionIntentParityTests
{
    private const string NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";

    [Fact]
    public static async Task ReRecordOfAlreadyRecordedNoteShouldRejectIdenticallyWithNoExtraDurableEffect()
    {
        SemanticIntent intent = new(NoteId);

        ArmOutcome ui = await GovernedCommandConformanceHarness.RunReRecordRejectionAsync(new UiSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome cli = await GovernedCommandConformanceHarness.RunReRecordRejectionAsync(new CliSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome mcp = await GovernedCommandConformanceHarness.RunReRecordRejectionAsync(new McpSurfaceArm(), intent, TestContext.Current.CancellationToken);

        foreach (ArmOutcome outcome in new[] { ui, cli, mcp })
        {
            // The rejection is a first-class event identity, never a bare error code.
            outcome.DomainOutcomeIdentity.ShouldBe("GovernedNoteAlreadyRecordedRejection");

            // No extra durable effect: the pre-existing view stays at source version 1.
            outcome.DurableView.ShouldNotBeNull();
            outcome.DurableView.SourceVersion.ShouldBe(1);
        }

        // Identical rejection outcome across arms (the rejection event is origin-free by construction).
        DifferentialOracle.Compare(ui, cli).AreEqual.ShouldBeTrue();
        DifferentialOracle.Compare(ui, mcp).AreEqual.ShouldBeTrue();
        DifferentialOracle.Compare(cli, mcp).AreEqual.ShouldBeTrue();
    }

    [Fact]
    public static async Task FailClosedNonAllowlistedSubmitShouldReturnIdenticalRedactedProblemWithNoStateMutation()
    {
        ArmOutcome ui = await GovernedCommandConformanceHarness.RunFailClosedRejectionAsync(new UiSurfaceArm(), TestContext.Current.CancellationToken);
        ArmOutcome cli = await GovernedCommandConformanceHarness.RunFailClosedRejectionAsync(new CliSurfaceArm(), TestContext.Current.CancellationToken);
        ArmOutcome mcp = await GovernedCommandConformanceHarness.RunFailClosedRejectionAsync(new McpSurfaceArm(), TestContext.Current.CancellationToken);

        string expectedProblem =
            $"problem:{ProblemDetailsCategory.Authorization_denied}:{ChatBotMessageCodes.RefusalBlockedAction}:{ChatBotAuthorizationReasonCodes.CommandNotAllowlisted}";

        foreach (ArmOutcome outcome in new[] { ui, cli, mcp })
        {
            // Same catalog-backed redacted problem (category + code + reasonCode), compared as a record.
            outcome.DomainOutcomeIdentity.ShouldBe(expectedProblem);

            // Zero durable mutation on the fail-closed path: no view (safe-not-found), no dispatch, no admission.
            outcome.DurableView.ShouldBeNull();
            outcome.DispatchCount.ShouldBe(0);
            outcome.CoarseIdempotencyRecordCount.ShouldBe(0);

            // Metadata-only leakage sentinel over the entire captured outcome.
            string serialized = JsonSerializer.Serialize(outcome, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            serialized.ShouldNotContain("tenant-alpha", Case.Insensitive);
            serialized.ShouldNotContain("conformance-probe-resource", Case.Insensitive);
        }

        // The single permitted delta: the declared origin is audited on the authorization-failure fact.
        ui.AuditedOrigin.ShouldBe("ui");
        cli.AuditedOrigin.ShouldBe("cli");
        mcp.AuditedOrigin.ShouldBe("mcp");

        DifferentialOracle.Compare(ui, cli).AreEqual.ShouldBeTrue();
        DifferentialOracle.Compare(ui, mcp).AreEqual.ShouldBeTrue();
    }
}
