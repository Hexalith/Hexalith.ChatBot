using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

/// <summary>
/// Story 8.5 AC8: the runbook-diagnostic completeness validator passes a fully-populated diagnostic, flags every
/// individually-omitted/placeholder/legacy-stub field, rejects a malformed last-transition triple and a non-catalog
/// failure reason, and produces a deterministic sample report counting complete vs defect items.
/// </summary>
public static class RunbookDiagnosticContractTests
{
    [Fact]
    public static void PassesAFullyPopulatedRunbookRealDiagnostic()
    {
        RunbookDiagnosticCompletenessValidator.Validate(Complete()).ShouldBeEmpty();
        RunbookDiagnosticCompletenessValidator.IsComplete(Complete()).ShouldBeTrue();
    }

    [Fact]
    public static void FlagsEachIndividuallyOmittedOrPlaceholderField()
    {
        RunbookDiagnosticCompletenessValidator.Validate(Complete() with { CorrelationId = "unknown" }).ShouldBe(["CorrelationId"]);
        RunbookDiagnosticCompletenessValidator.Validate(Complete() with { TenantRef = "unknown" }).ShouldBe(["TenantRef"]);
        RunbookDiagnosticCompletenessValidator.Validate(Complete() with { WorkflowItemRef = "unknown" }).ShouldBe(["WorkflowItemRef"]);
        RunbookDiagnosticCompletenessValidator.Validate(Complete() with { CurrentState = "  " }).ShouldBe(["CurrentState"]);
        RunbookDiagnosticCompletenessValidator.Validate(Complete() with { NextSafeAction = "unknown" }).ShouldBe(["NextSafeAction"]);
        RunbookDiagnosticCompletenessValidator.Validate(Complete() with { MailboxRef = "unknown" }).ShouldBe(["MailboxRef"]);
        RunbookDiagnosticCompletenessValidator.Validate(Complete() with { RetryCount = -1 }).ShouldBe(["RetryCount"]);
    }

    [Fact]
    public static void RejectsTheLegacyStubPrefixesThatWouldOtherwisePassANaiveSafeTokenCheck()
    {
        RunbookDiagnosticCompletenessValidator.Validate(Complete() with { CorrelationId = "correlation:item:001" }).ShouldBe(["CorrelationId"]);
        RunbookDiagnosticCompletenessValidator.Validate(Complete() with { TenantRef = "tenant:current" }).ShouldBe(["TenantRef"]);
        RunbookDiagnosticCompletenessValidator.Validate(Complete() with { LastTransition = "last-transition:waiting" }).ShouldBe(["LastTransition"]);
    }

    [Fact]
    public static void AllowsANullMailboxRefForANonMailboxItem()
        => RunbookDiagnosticCompletenessValidator.Validate(Complete() with { MailboxRef = null }).ShouldBeEmpty();

    [Theory]
    [InlineData("from:request|actor:requester-a")] // only two components
    [InlineData("from:unknown|actor:requester-a|at:1717387200")] // unknown from-state
    [InlineData("from:request|actor:unknown|at:1717387200")] // unknown actor
    [InlineData("from:request|actor:requester-a|at:0")] // epoch-0 placeholder timestamp
    [InlineData("from:request|actor:requester-a|at:notanumber")] // non-numeric timestamp
    [InlineData("requested by a at noon")] // not a triple at all
    public static void RejectsAMalformedLastTransition(string lastTransition)
        => RunbookDiagnosticCompletenessValidator.Validate(Complete() with { LastTransition = lastTransition }).ShouldBe(["LastTransition"]);

    [Fact]
    public static void RejectsANonCatalogFailureReasonButAcceptsACatalogCodeOrNull()
    {
        RunbookDiagnosticCompletenessValidator.Validate(Complete() with { FailureReason = "not_a_catalog_code" }).ShouldBe(["FailureReason"]);
        RunbookDiagnosticCompletenessValidator.Validate(Complete() with { FailureReason = ChatBotMessageCodes.RetryExhausted }).ShouldBeEmpty();
        RunbookDiagnosticCompletenessValidator.Validate(Complete() with { FailureReason = null }).ShouldBeEmpty();
    }

    [Fact]
    public static void EvaluateSampleCountsCompleteVersusDefectDeterministicallyAndListsDefectRefs()
    {
        OperationalQueueDiagnostics defectOne = Complete() with { WorkflowItemRef = "item:defect-1", CorrelationId = "unknown" };
        OperationalQueueDiagnostics defectTwo = Complete() with { WorkflowItemRef = "item:defect-2", LastTransition = "from:unknown|actor:unknown|at:0" };
        OperationalQueueDiagnostics[] sample =
        [
            Complete() with { WorkflowItemRef = "item:ok-1" },
            defectOne,
            Complete() with { WorkflowItemRef = "item:ok-2" },
            defectTwo,
        ];

        RunbookDiagnosticCompletenessReport report = RunbookDiagnosticCompletenessValidator.EvaluateSample(sample);

        report.Sampled.ShouldBe(4);
        report.Complete.ShouldBe(2);
        report.DefectWorkflowItemRefs.ShouldBe(["item:defect-1", "item:defect-2"]);

        // Deterministic: the same sample yields the same report.
        RunbookDiagnosticCompletenessValidator.EvaluateSample(sample).ShouldBeEquivalentTo(report);
    }

    [Fact]
    public static void EvaluateSampleRecordsTheUnknownPlaceholderForADefectItemWhoseWorkflowItemRefIsItselfUnusable()
    {
        // A defect item that also lacks a usable (safe) WorkflowItemRef cannot self-identify in the report; the
        // fail-closed "unknown" placeholder is recorded rather than a missing entry or a fabricated ref — so the
        // defect is still counted and surfaced.
        OperationalQueueDiagnostics anonymousDefect = Complete() with { WorkflowItemRef = "not a safe ref" };

        RunbookDiagnosticCompletenessReport report = RunbookDiagnosticCompletenessValidator.EvaluateSample([anonymousDefect]);

        report.Sampled.ShouldBe(1);
        report.Complete.ShouldBe(0);
        report.DefectWorkflowItemRefs.ShouldBe(["unknown"]);
    }

    private static OperationalQueueDiagnostics Complete()
        => new(
            CorrelationId: "corr-alpha-01",
            TenantRef: "t-alpha",
            MailboxRef: "mailbox:ops",
            WorkflowItemRef: "item:001",
            CurrentState: "waiting",
            LastTransition: "from:request|actor:requester-a|at:1717387200",
            RetryCount: 0,
            FailureReason: null,
            NextSafeAction: "review");
}
