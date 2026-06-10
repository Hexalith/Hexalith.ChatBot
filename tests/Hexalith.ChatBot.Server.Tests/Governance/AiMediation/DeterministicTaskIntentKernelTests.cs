using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Governance.AiMediation;

public static class DeterministicTaskIntentKernelTests
{
    [Fact]
    public static void DetectShouldCaptureAuthorizedActionIntentWithStableMetadata()
    {
        TaskIntentDetectionResult result = DeterministicTaskIntentKernel.Detect(Request(["request-action"]));

        result.State.ShouldBe(TaskIntentState.Captured);
        result.Record.ShouldNotBeNull();
        result.Record.DetectedActionKind.ShouldBe(ProjectConversationDetectedActionKind.RequestAction);
        result.Record.TaskIntentId.ShouldStartWith("task-intent:");
        result.Record.SourceEvidenceOffsets.ShouldHaveSingleItem().EvidenceReference.ShouldBe("message:offset:001");
        result.Record.DetectedAt.Offset.ShouldBe(TimeSpan.Zero);
        result.Record.DetectedIntentSummary.Length.ShouldBeLessThanOrEqualTo(DeterministicTaskIntentKernel.SummaryMaxLength);
        result.Record.SafeNextAction.ShouldBe("review-task-intent-action");
    }

    [Theory]
    [InlineData(false, true, true, true, true, TaskIntentReasonCodes.MissingTenantScope)]
    [InlineData(true, false, true, true, true, TaskIntentReasonCodes.MissingProjectAuthorization)]
    [InlineData(true, true, false, true, true, TaskIntentReasonCodes.MissingSourceAuthorization)]
    [InlineData(true, true, true, false, true, TaskIntentReasonCodes.MissingAuditReadiness)]
    [InlineData(true, true, true, true, false, TaskIntentReasonCodes.StaleCorrectedContext)]
    public static void DetectShouldFailClosedForUnsafeContext(
        bool tenantResolved,
        bool projectAuthorized,
        bool sourceAuthorized,
        bool auditReady,
        bool correctedContextReady,
        string expectedReason)
    {
        TaskIntentDetectionResult result = DeterministicTaskIntentKernel.Detect(Request(
            ["request-action"],
            tenantResolved,
            projectAuthorized,
            sourceAuthorized,
            auditReady,
            correctedContextReady));

        result.ReasonCode.ShouldBe(expectedReason);
        if (expectedReason == TaskIntentReasonCodes.StaleCorrectedContext)
        {
            result.State.ShouldBe(TaskIntentState.Blocked);
            result.Record.ShouldNotBeNull().ConversionReadinessBlocked.ShouldBeTrue();
            result.Record.SafeNextAction.ShouldBe("wait-for-correction-propagation");
        }
        else
        {
            result.State.ShouldBe(TaskIntentState.Rejected);
            result.Record.ShouldBeNull();
        }
    }

    [Fact]
    public static void EquivalentDetectionsShouldConvergeToSameRecordId()
    {
        TaskIntentRecord first = DeterministicTaskIntentKernel.Detect(Request(["request-action"])).Record.ShouldNotBeNull();
        TaskIntentRecord replay = DeterministicTaskIntentKernel.Detect(Request(["request-action"])).Record.ShouldNotBeNull();
        TaskIntentRecord changedKernel = DeterministicTaskIntentKernel
            .Detect(Request(["request-action"]) with { KernelVersion = "chatbot.task-intent.kernel.m0.v2" })
            .Record
            .ShouldNotBeNull();

        replay.TaskIntentId.ShouldBe(first.TaskIntentId);
        changedKernel.TaskIntentId.ShouldNotBe(first.TaskIntentId);
    }

    [Fact]
    public static void InformationalOnlySignalShouldNotCaptureTaskIntent()
    {
        TaskIntentDetectionResult result = DeterministicTaskIntentKernel.Detect(Request(["status-update"]));

        result.State.ShouldBe(TaskIntentState.NotActionable);
        result.Record.ShouldBeNull();
    }

    [Fact]
    public static void MissingSourceEvidenceShouldFailClosedWithoutCapture()
    {
        TaskIntentDetectionResult result = DeterministicTaskIntentKernel.Detect(Request(["request-action"]) with
        {
            SourceEvidenceOffsets = [],
        });

        result.State.ShouldBe(TaskIntentState.Rejected);
        result.ReasonCode.ShouldBe(TaskIntentReasonCodes.MissingSourceEvidence);
        result.Record.ShouldBeNull();
    }

    [Fact]
    public static void MissingRequesterPartyShouldFailClosedWithoutCapture()
    {
        TaskIntentDetectionResult result = DeterministicTaskIntentKernel.Detect(Request(["request-action"]) with
        {
            RequesterPartyId = "   ",
        });

        result.State.ShouldBe(TaskIntentState.Rejected);
        result.ReasonCode.ShouldBe(TaskIntentReasonCodes.MissingRequesterParty);
        result.Record.ShouldBeNull();
    }

    [Theory]
    [InlineData("redacted")]
    [InlineData("unavailable")]
    public static void RedactedOrUnavailableSourceShouldFailClosedWithoutCapture(string redactionState)
    {
        TaskIntentDetectionResult result = DeterministicTaskIntentKernel.Detect(Request(["request-action"]) with
        {
            RedactionState = redactionState,
        });

        result.State.ShouldBe(TaskIntentState.Rejected);
        result.ReasonCode.ShouldBe(TaskIntentReasonCodes.RedactedSource);
        result.Record.ShouldBeNull();
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public static void OutOfRangeConfidenceShouldFailClosedWithoutCapture(double confidence)
    {
        TaskIntentDetectionResult result = DeterministicTaskIntentKernel.Detect(Request(["request-action"]) with
        {
            ConfidenceScore = confidence,
        });

        result.State.ShouldBe(TaskIntentState.Rejected);
        result.ReasonCode.ShouldBe(TaskIntentReasonCodes.InvalidConfidence);
        result.Record.ShouldBeNull();
    }

    private static TaskIntentDetectionRequest Request(
        IReadOnlyList<string> signals,
        bool tenantResolved = true,
        bool projectAuthorized = true,
        bool sourceAuthorized = true,
        bool auditReady = true,
        bool correctedContextReady = true)
        => new(
            "tenant-alpha",
            "project-001",
            "graph-message-001",
            "party-001",
            signals,
            [new TaskIntentSourceEvidenceOffset("message:offset:001", 10, 40, "safe-token")],
            "metadata_only",
            "collaboration_input",
            8,
            "correlation-001",
            tenantResolved,
            projectAuthorized,
            sourceAuthorized,
            auditReady,
            correctedContextReady,
            0.82,
            "authorized-project-conversation",
            "policy-001",
            "correction-001",
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
}
