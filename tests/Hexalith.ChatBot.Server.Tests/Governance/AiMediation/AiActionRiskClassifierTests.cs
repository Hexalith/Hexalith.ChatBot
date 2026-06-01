using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Governance.AiMediation;

public sealed class AiActionRiskClassifierTests
{
    [Theory]
    [InlineData(AiActionRiskActionClass.ModifiesState)]
    [InlineData(AiActionRiskActionClass.ExposesFiles)]
    [InlineData(AiActionRiskActionClass.SendsExternal)]
    [InlineData(AiActionRiskActionClass.CreatesTasks)]
    [InlineData(AiActionRiskActionClass.InvokesTools)]
    [InlineData(AiActionRiskActionClass.ActsOnBehalf)]
    public void RiskyActionClassShouldRequireApproval(AiActionRiskActionClass actionClass)
    {
        AiActionRiskClassificationRecord result = AiActionRiskClassifier.Classify(Input(actionClasses: [actionClass]));

        result.RiskClass.ShouldBe(AiActionRiskClass.ApprovalRequired);
        result.RiskActionClasses.ShouldBe([actionClass], ignoreOrder: false);
        result.ReasonCode.ShouldBe("risky_action_class");
    }

    [Fact]
    public void AppendConversationMessageM0MetadataShouldRequireApproval()
    {
        AiActionRiskClassificationRecord result = AiActionRiskClassifier.Classify(Input(
            intendedCommandName: AiActionCommandMetadataProvider.AppendConversationMessageCommandName));

        result.RiskClass.ShouldBe(AiActionRiskClass.ApprovalRequired);
        result.CommandAllowlistVersion.ShouldBe(AiActionCommandMetadataProvider.M0AllowlistVersion);
        result.CommandDefaultRisk.ShouldBe(AiActionRiskClass.ApprovalRequired);
        result.ReasonCode.ShouldBe("risky_action_class");
    }

    [Fact]
    public void ReadOnlyKnownAuthorizedTupleMayBeLowRisk()
    {
        AiActionRiskClassificationRecord result = AiActionRiskClassifier.Classify(Input(
            intendedCommandName: "Project.ReadConversation",
            actionClasses: [],
            effectSurface: "read-only",
            tenantPolicyClassification: "low-risk",
            commandDefaultRisk: AiActionRiskClass.LowRisk));

        result.RiskClass.ShouldBe(AiActionRiskClass.LowRisk);
        result.RiskActionClasses.ShouldBeEmpty();
        result.ReasonCode.ShouldBe("low_risk_tuple");
    }

    [Fact]
    public void MissingMetadataShouldFailClosedWithIndeterminateReason()
    {
        AiActionRiskClassificationRecord result = AiActionRiskClassifier.Classify(Input(
            intendedCommandName: "Project.UnknownCommand",
            effectSurface: null,
            tenantPolicyClassification: null,
            requesterAuthorityClass: null,
            commandAllowlistVersion: null,
            commandDefaultRisk: null));

        result.RiskClass.ShouldBe(AiActionRiskClass.ApprovalRequired);
        result.IndeterminateReason.ShouldBe("missing_effect_surface");
        result.ReasonCode.ShouldBe("indeterminate_missing_effect_surface");
    }

    [Fact]
    public void MixedRequestShouldRequireApprovalAndOrderContributingClassesDeterministically()
    {
        AiActionRiskClassificationRecord result = AiActionRiskClassifier.Classify(Input(
            actionClasses:
            [
                AiActionRiskActionClass.InvokesTools,
                AiActionRiskActionClass.ExposesFiles,
                AiActionRiskActionClass.ModifiesState,
            ]));

        result.RiskClass.ShouldBe(AiActionRiskClass.ApprovalRequired);
        result.RiskActionClasses.ShouldBe(
            [
                AiActionRiskActionClass.ModifiesState,
                AiActionRiskActionClass.ExposesFiles,
                AiActionRiskActionClass.InvokesTools,
            ],
            ignoreOrder: false);
    }

    [Fact]
    public void UnknownActionClassShouldFailClosedWithoutSerializingInvalidEnumValues()
    {
        AiActionRiskClassificationRecord result = AiActionRiskClassifier.Classify(Input(
            intendedCommandName: "Project.ReadConversation",
            actionClasses: [(AiActionRiskActionClass)999],
            effectSurface: "read-only",
            tenantPolicyClassification: "low-risk",
            commandDefaultRisk: AiActionRiskClass.LowRisk));

        result.RiskClass.ShouldBe(AiActionRiskClass.ApprovalRequired);
        result.RiskActionClasses.ShouldBeEmpty();
        result.IndeterminateReason.ShouldBe("unknown_action_class");
        result.ReasonCode.ShouldBe("indeterminate_unknown_action_class");

        string serialized = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        serialized.ShouldContain("unknown_action_class");
        serialized.ShouldNotContain("999");
    }

    [Fact]
    public void DisallowedMetadataShouldReturnRejectionWithoutInventingThirdRiskClass()
    {
        AiActionRiskClassificationRecord result = AiActionRiskClassifier.Classify(Input(
            tenantPolicyClassification: "unsupported"));

        result.RiskClass.ShouldBe(AiActionRiskClass.ApprovalRequired);
        result.Rejected.ShouldBeTrue();
        result.ReasonCode.ShouldBe("unsupported_ai_action_command");
    }

    [Fact]
    public void ClassifierDisagreementEventShapeShouldBeMetadataOnly()
    {
        AiActionRiskClassificationRecord classification = AiActionRiskClassifier.Classify(Input(
            actionClasses: [AiActionRiskActionClass.CreatesTasks]));
        AiActionRiskClassifierDisagreementRecorded recorded = new(
            "ai-proposal:task-intent-001:transition-001",
            "reviewer-001",
            "override-to-approval-required",
            "reviewer-requested-calibration",
            classification.RiskClass,
            classification.ClassifierVersion,
            classification.InputTuple,
            classification.CorrelationId,
            classification.PolicySnapshotId,
            new DateTimeOffset(2026, 6, 1, 16, 45, 0, TimeSpan.Zero));

        recorded.RedactionState.ShouldBe("metadata_only");
        recorded.RetentionClass.ShouldBe("collaboration_input");
        recorded.SchemaVersion.ShouldBe("chatbot.ai-action-risk-classifier-disagreement.v1");

        string serialized = JsonSerializer.Serialize(recorded, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        serialized.ShouldContain("classifierVersion");
        serialized.ShouldContain("inputTuple");
        serialized.ShouldContain("classification");
        serialized.ShouldContain("reviewerDecision");
        serialized.ShouldContain("resolution");
        serialized.ShouldContain("proposalId");
        serialized.ShouldContain("correlationId");
        serialized.ShouldContain("policySnapshotId");
        serialized.ShouldNotContain("raw prompt", Case.Insensitive);
        serialized.ShouldNotContain("message body", Case.Insensitive);
        serialized.ShouldNotContain("provider payload", Case.Insensitive);
        serialized.ShouldNotContain("tool arguments", Case.Insensitive);
    }

    private static AiActionRiskInputTuple Input(
        string intendedCommandName = "Project.AppendConversationMessage",
        IReadOnlyList<AiActionRiskActionClass>? actionClasses = null,
        string? effectSurface = "project-conversation",
        string? tenantPolicyClassification = "approval-required",
        string? requesterAuthorityClass = "project-contributor",
        string? commandAllowlistVersion = "ai-action-command-allowlist.m0",
        AiActionRiskClass? commandDefaultRisk = AiActionRiskClass.ApprovalRequired)
        => new(
            intendedCommandName,
            actionClasses ?? [AiActionRiskActionClass.ModifiesState],
            effectSurface,
            tenantPolicyClassification,
            requesterAuthorityClass,
            "policy:tenant-alpha:ai-action-risk",
            commandAllowlistVersion,
            commandDefaultRisk,
            "declared",
            "authorized",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");
}
