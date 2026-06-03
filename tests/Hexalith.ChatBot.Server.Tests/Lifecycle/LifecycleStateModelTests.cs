using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public static class LifecycleStateModelTests
{
    [Fact]
    public static void StateVocabularyShouldBeStableAndOrdered()
    {
        LifecycleStates.All.ShouldBe(
            [
                "Received",
                "Proposed",
                "Associated",
                "Rejected",
                "Deferred",
                "NeedsReview",
                "Failed",
                "Skipped",
                "Corrected",
                "Correcting",
                "Correction-delayed",
                "Active",
                "Disabled",
                "Quarantined",
            ],
            ignoreOrder: false);
        LifecycleSubStates.All.ShouldBe(["Correcting", "Correction-delayed"], ignoreOrder: false);
    }

    [Theory]
    [InlineData("Received", "Proposed")]
    [InlineData("Received", "NeedsReview")]
    [InlineData("Received", "Failed")]
    [InlineData("Received", "Skipped")]
    [InlineData("Proposed", "Associated")]
    [InlineData("Proposed", "Rejected")]
    [InlineData("Proposed", "Deferred")]
    [InlineData("Proposed", "NeedsReview")]
    [InlineData("Proposed", "Failed")]
    [InlineData("Deferred", "Proposed")]
    [InlineData("Deferred", "Rejected")]
    [InlineData("Deferred", "NeedsReview")]
    [InlineData("NeedsReview", "Proposed")]
    [InlineData("NeedsReview", "Associated")]
    [InlineData("NeedsReview", "Rejected")]
    [InlineData("NeedsReview", "Deferred")]
    [InlineData("Associated", "Corrected")]
    [InlineData("Corrected", "Correcting")]
    [InlineData("Correcting", "Corrected")]
    [InlineData("Correcting", "Correction-delayed")]
    [InlineData("Correction-delayed", "Corrected")]
    [InlineData("Active", "Disabled")]
    [InlineData("Active", "Quarantined")]
    public static void ValidatorShouldAcceptExplicitEdgesOnly(string from, string to)
    {
        LifecycleTransitionValidation result = LifecycleTransitionValidator.Validate(new LifecycleTransitionDefinition(from, to));

        result.IsValid.ShouldBeTrue();
        result.ReasonCode.ShouldBe(LifecycleTransitionReasonCodes.ValidTransition);
        result.Transition.ToString().ShouldBe($"{from}->{to}");
    }

    [Theory]
    [InlineData("Received", "Associated")]
    [InlineData("Proposed", "Corrected")]
    [InlineData("NeedsReview", "Failed")]
    [InlineData("Corrected", "Associated")]
    [InlineData("Correction-delayed", "Correcting")]
    [InlineData("Rejected", "Proposed")]
    [InlineData("Failed", "Received")]
    [InlineData("Skipped", "Received")]
    public static void ValidatorShouldRejectRepresentativeInvalidEdges(string from, string to)
    {
        LifecycleTransitionValidation result = LifecycleTransitionValidator.Validate(new LifecycleTransitionDefinition(from, to));

        result.IsValid.ShouldBeFalse();
        result.ReasonCode.ShouldBe(LifecycleTransitionReasonCodes.InvalidTransition);
    }

    [Theory]
    [InlineData("Rejected")]
    [InlineData("Failed")]
    [InlineData("Skipped")]
    public static void TerminalStatesShouldNotTransitionInPlaceAndRequireReprocessPlan(string terminalState)
    {
        LifecycleTerminalStates.IsTerminal(terminalState).ShouldBeTrue();
        LifecycleTransitionValidator.Validate(new LifecycleTransitionDefinition(terminalState, "Received")).IsValid.ShouldBeFalse();

        LifecycleReprocessPlan plan = LifecycleReprocessFactory.Create(
            terminalState,
            "workflow-old",
            "workflow-new");

        plan.SupersededWorkflowId.ShouldBe("workflow-old");
        plan.NewWorkflowId.ShouldBe("workflow-new");
        plan.SupersededByAuditLinkName.ShouldBe("superseded_by_workflow");
        plan.SupersedesAuditLinkName.ShouldBe("supersedes_workflow");
    }

    // ---- M1 lifecycle completion (Story 7.27) ----

    [Fact]
    public static void SkippedShouldBeTerminalWithNoOutgoingEdge()
    {
        // Skipped is terminal: it has no outgoing edge in the matrix. Reprocessing a skipped item creates a NEW
        // workflow instance via the reprocess factory (supersedes/superseded_by links) — never an outgoing edge.
        LifecycleTerminalStates.IsTerminal(LifecycleStates.Skipped).ShouldBeTrue();
        foreach (string target in LifecycleStates.All)
        {
            LifecycleTransitionValidator
                .Validate(new LifecycleTransitionDefinition(LifecycleStates.Skipped, target))
                .IsValid.ShouldBeFalse();
        }

        LifecycleReprocessPlan plan = LifecycleReprocessFactory.Create(LifecycleStates.Skipped, "workflow-skipped", "workflow-reprocessed");
        plan.SupersededWorkflowId.ShouldBe("workflow-skipped");
        plan.NewWorkflowId.ShouldBe("workflow-reprocessed");
    }

    [Fact]
    public static void GuardShouldMapEverySkipTriggerToAValidReceivedToSkippedTransition()
    {
        // AC6: both M1 skip triggers (duplicate-suppression, out-of-scope mailbox) map through the guard to a
        // VALID Received->Skipped transition that is present in the matrix — never a fabricated magic string.
        CommandSubmissionLifecycleTransitionGuard guard = new();

        foreach (LifecycleSkipTrigger trigger in new[] { LifecycleSkipTrigger.DuplicateSuppression, LifecycleSkipTrigger.OutOfScopeMailbox })
        {
            LifecycleTransitionValidation result = guard.ResolveSkipTransition(trigger);

            result.IsValid.ShouldBeTrue();
            result.ReasonCode.ShouldBe(LifecycleTransitionReasonCodes.ValidTransition);
            result.Transition.From.ShouldBe(LifecycleStates.Received);
            result.Transition.To.ShouldBe(LifecycleStates.Skipped);
        }
    }

    [Theory]
    [InlineData("AssociateEmailToProject", "NeedsReview", "Associated")]
    [InlineData("RejectEmailProjectAssociation", "NeedsReview", "Rejected")]
    [InlineData("DeferEmailProjectAssociation", "NeedsReview", "Deferred")]
    [InlineData("MarkEmailAssociationNeedsReview", "NeedsReview", "NeedsReview")]
    [InlineData("CorrectEmailProjectAssociation", "Associated", "Corrected")]
    [InlineData("ApproveMailboxSourceDisable", "Active", "Disabled")]
    [InlineData("ApproveServiceClientDisable", "Active", "Disabled")]
    [InlineData("ApproveAiActorDisable", "Active", "Disabled")]
    [InlineData("ApproveCommandCapabilityDisable", "Active", "Disabled")]
    [InlineData("ApproveOutboundChannelDisable", "Active", "Disabled")]
    [InlineData("ApproveOutboundChannelQuarantine", "Active", "Quarantined")]
    [InlineData("ApproveCommandCapabilityQuarantine", "Active", "Quarantined")]
    [InlineData("ApproveAiActorQuarantine", "Active", "Quarantined")]
    [InlineData("ApproveMailboxSourceQuarantine", "Active", "Quarantined")]
    [InlineData("ApproveServiceClientQuarantine", "Active", "Quarantined")]
    [InlineData("CaptureMailboxMessageIntake", "Received", "Proposed")]
    [InlineData("UnmappedCommandFallsToDefault", "Received", "Proposed")]
    public static void EveryGuardSwitchArmShouldResolveToAValidTransition(string commandType, string from, string to)
    {
        // AC7: every shipped command/guard mapping must resolve to a VALID matrix edge (matrix closure). The
        // default arm (unmapped command) lands on the legal Received->Proposed edge.
        CommandSubmissionLifecycleTransitionGuard guard = new();

        LifecycleTransitionValidation result = guard.ValidateCommandSubmission(Context(commandType));

        result.IsValid.ShouldBeTrue();
        result.Transition.From.ShouldBe(from);
        result.Transition.To.ShouldBe(to);
    }

    [Fact]
    public static void GuardRejectedTransitionShouldBeRecordedWithTheInvalidReasonCode()
    {
        // A representative invalid transition is still rejected before mutation and carries the invalid reason code.
        LifecycleTransitionValidation rejected = LifecycleTransitionValidator
            .Validate(new LifecycleTransitionDefinition(LifecycleStates.Skipped, LifecycleStates.Proposed));

        rejected.IsValid.ShouldBeFalse();
        rejected.ReasonCode.ShouldBe(LifecycleTransitionReasonCodes.InvalidTransition);
    }

    private static ChatBotGatewayContext Context(string commandType)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test"));
        ChatBotCommandSubmission submission = new(
            principal,
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = commandType,
                Command = new Hexalith.ChatBot.Contracts.Commands.RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAX"),
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            null,
            ChatBotSurfaceOrigin.Ui);
        ChatBotAuthenticatedActor actor = new("actor-alpha", principal);
        return new ChatBotGatewayContext(submission, actor, new ChatBotTenantBinding("tenant-alpha"));
    }
}
