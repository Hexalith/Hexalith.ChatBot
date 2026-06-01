using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class TaskIntentContractTests
{
    [Fact]
    public static void TaskIntentRecordShouldSerializeStableMetadataOnlyShape()
    {
        TaskIntentRecord record = Record();

        string json = JsonSerializer.Serialize(record, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"taskIntentId\":\"task-intent:abc\"");
        json.ShouldContain("\"detectedActionKind\":\"request-action\"");
        json.ShouldContain("\"state\":\"captured\"");
        json.ShouldContain("\"confidenceScore\":0.82");
        json.ShouldContain("\"detectedAt\":\"2026-06-01T00:00:00+00:00\"");
        json.ShouldContain("\"sourceEvidenceOffsets\"");
        json.ShouldNotContain("body", Case.Insensitive);
        json.ShouldNotContain("subject", Case.Insensitive);
        json.ShouldNotContain("prompt", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
        record.DetectedIntentSummary.Length.ShouldBeLessThanOrEqualTo(280);
        record.ConfidenceScore.ShouldBeInRange(0, 1);
    }

    [Fact]
    public static void CaptureTaskIntentShouldBeAChatBotCommandWithoutTenantBodyAuthority()
    {
        CaptureTaskIntent command = new(
            "project-001",
            "graph-message-001",
            "party-001",
            "authorized conversation item requests action",
            ProjectConversationDetectedActionKind.RequestAction,
            [new TaskIntentSourceEvidenceOffset("message:offset:001", 10, 40, "safe-token")],
            "chatbot.task-intent.kernel.m0.v1",
            0.82,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "metadata_only",
            "collaboration_input",
            8,
            "correlation-001",
            "policy-001",
            CorrectedContextReady: true,
            "chatbot.task-intent-record.v1");

        command.ShouldBeAssignableTo<IChatBotCommand>();
        command.GetType().GetProperties().Select(static property => property.Name).ShouldNotContain("TenantId");
    }

    private static TaskIntentRecord Record()
        => new(
            "task-intent:abc",
            "tenant-alpha",
            "project-001",
            "graph-message-001",
            "party-001",
            "authorized conversation item requests action",
            ProjectConversationDetectedActionKind.RequestAction,
            [new TaskIntentSourceEvidenceOffset("message:offset:001", 10, 40, "safe-token")],
            "chatbot.task-intent.kernel.m0.v1",
            0.82,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            TaskIntentState.Captured,
            "chatbot.task-intent-record.v1",
            "task_intent_captured",
            "authorized-project-conversation",
            "metadata_only",
            "collaboration_input",
            8,
            "correlation-001",
            "policy-001",
            ConversionReadinessBlocked: false,
            SafeNextAction: "review-task-intent-action");
}
