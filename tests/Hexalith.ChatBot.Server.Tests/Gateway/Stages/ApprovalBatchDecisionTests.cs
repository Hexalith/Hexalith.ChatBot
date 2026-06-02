using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;

using Shouldly;

using ApprovalDecisionKind = Hexalith.ChatBot.Contracts.Enums.ApprovalDecisionKind;
using DecideAiActionApproval = Hexalith.ChatBot.Contracts.Commands.DecideAiActionApproval;
using DecideOutboundApproval = Hexalith.ChatBot.Contracts.Commands.DecideOutboundApproval;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class ApprovalBatchDecisionTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);
    private const string GroupFingerprint = "sha256:b1946ac92492d2347c6235b4d2611184";

    [Fact]
    public void BatchShouldFanOutToOneSingleItemCommandPerUnderlyingItem()
    {
        BatchDecisionItem[] items =
        [
            AiItem("approval-001", 3),
            AiItem("approval-002", 5),
            AiItem("approval-003", 7),
        ];

        BatchDecisionPlan plan = ApprovalBatchDecisionPlanner.Plan(
            ApprovalBatchDecisionPlanner.HumanActorValue,
            ApprovalDecisionKind.Approve,
            GroupFingerprint,
            items);

        plan.Authorized.ShouldBeTrue();
        plan.AcceptedCount.ShouldBe(3);
        plan.DeniedCount.ShouldBe(0);
        plan.GroupKeyFingerprint.ShouldBe(GroupFingerprint);

        // One governed single-item decision command per item — never one collapsed batch command (NFR46).
        IChatBotCommand[] commands = plan.Commands.ToArray();
        commands.Length.ShouldBe(3);
        commands.ShouldAllBe(command => command is DecideAiActionApproval);
        commands.Cast<DecideAiActionApproval>().Select(static command => command.ApprovalId)
            .ShouldBe(["approval-001", "approval-002", "approval-003"]);
        commands.Cast<DecideAiActionApproval>().ShouldAllBe(command => command.Decision == ApprovalDecisionKind.Approve);
    }

    [Theory]
    [InlineData("service")]
    [InlineData("ai")]
    [InlineData("automation")]
    public void BatchShouldDenyNonHumanActorsBeforeStateLoad(string actorType)
    {
        BatchDecisionPlan plan = ApprovalBatchDecisionPlanner.Plan(
            actorType,
            ApprovalDecisionKind.Approve,
            GroupFingerprint,
            [AiItem("approval-001", 1)]);

        plan.Authorized.ShouldBeFalse();
        plan.ReasonCode.ShouldBe(ApprovalBatchDecisionPlanner.NonHumanActorReasonCode);
        plan.Outcomes.ShouldBeEmpty();
        plan.Commands.ShouldBeEmpty();
    }

    [Fact]
    public void PartialAuthorityBatchShouldActOnlyOnAuthorizedItemsWithSafeDenialForTheRest()
    {
        BatchDecisionItem[] items =
        [
            AiItem("approval-001", 1, reviewerHasAuthority: true),
            AiItem("approval-002", 1, reviewerHasAuthority: false),
            AiItem("approval-003", 1, reviewerHasAuthority: true),
        ];

        BatchDecisionPlan plan = ApprovalBatchDecisionPlanner.Plan(
            ApprovalBatchDecisionPlanner.HumanActorValue,
            ApprovalDecisionKind.Reject,
            GroupFingerprint,
            items);

        plan.Authorized.ShouldBeTrue();
        plan.AcceptedCount.ShouldBe(2);
        plan.DeniedCount.ShouldBe(1);

        BatchDecisionOutcome denied = plan.Outcomes.Single(static outcome => !outcome.Accepted);
        denied.ApprovalId.ShouldBe("approval-002");
        denied.Command.ShouldBeNull();
        denied.ReasonCode.ShouldBe(ApprovalBatchDecisionPlanner.InsufficientAuthorityReasonCode);
        // Safe reason code — no existence leakage / no project name.
        denied.ReasonCode.ShouldNotContain("project", Case.Insensitive);
    }

    [Fact]
    public void BatchShouldFanOutOutboundDecisionCommands()
    {
        BatchDecisionPlan plan = ApprovalBatchDecisionPlanner.Plan(
            ApprovalBatchDecisionPlanner.HumanActorValue,
            ApprovalDecisionKind.Approve,
            GroupFingerprint,
            [OutboundItem("approval-out-001"), OutboundItem("approval-out-002")]);

        plan.AcceptedCount.ShouldBe(2);
        plan.Commands.ShouldAllBe(command => command is DecideOutboundApproval);
    }

    [Fact]
    public void EachBatchItemAuditEnvelopeCarriesSafeGroupRefAndIsMetadataOnly()
    {
        // Two items in the batch → two independent envelopes (one per item), never one collapsed batch envelope.
        AuditEnvelope first = EnvelopeForDecision("approval-001", "proposal-001");
        AuditEnvelope second = EnvelopeForDecision("approval-002", "proposal-002");

        foreach (AuditEnvelope envelope in new[] { first, second })
        {
            envelope.SourceEvidenceRefs.ShouldContain($"approval-group:{GroupFingerprint}");
            envelope.SourceEvidenceRefs.ShouldContain("approval-risk-class:high");
            envelope.SourceEvidenceRefs.ShouldContain("approval-authority-rank:3");
            string serialized = JsonSerializer.Serialize(envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            serialized.ShouldNotContain("secret", Case.Insensitive);
            serialized.ShouldNotContain("bearer", Case.Insensitive);
            serialized.ShouldNotContain("project-content", Case.Insensitive);
        }

        // Distinct per-item identity proves the fan-out is per item, not collapsed.
        first.SourceEvidenceRefs.ShouldContain("approval:approval-001");
        second.SourceEvidenceRefs.ShouldContain("approval:approval-002");
    }

    [Theory]
    [InlineData(ApprovalDecisionKind.Approve)]
    [InlineData(ApprovalDecisionKind.Reject)]
    [InlineData(ApprovalDecisionKind.RequestRevision)]
    [InlineData(ApprovalDecisionKind.Cancel)]
    public void EveryDecisionKindShouldFanOutOneCommandPerItemCarryingThatDecision(ApprovalDecisionKind decision)
    {
        BatchDecisionPlan plan = ApprovalBatchDecisionPlanner.Plan(
            ApprovalBatchDecisionPlanner.HumanActorValue,
            decision,
            GroupFingerprint,
            [AiItem("approval-001", 1), AiItem("approval-002", 2)]);

        plan.Authorized.ShouldBeTrue();
        plan.AcceptedCount.ShouldBe(2);
        plan.Commands.Cast<DecideAiActionApproval>().ShouldAllBe(command => command.Decision == decision);
    }

    [Fact]
    public void EachFannedCommandShouldCarryItsOwnApprovalIdAndExpectedSourceVersion()
    {
        // AC4: each per-item decision carries its OWN approval id + expected source version — never a shared batch token.
        BatchDecisionPlan plan = ApprovalBatchDecisionPlanner.Plan(
            ApprovalBatchDecisionPlanner.HumanActorValue,
            ApprovalDecisionKind.Approve,
            GroupFingerprint,
            [AiItem("approval-001", 3), AiItem("approval-002", 17)]);

        DecideAiActionApproval[] commands = plan.Commands.Cast<DecideAiActionApproval>().ToArray();
        commands.Single(static command => command.ApprovalId == "approval-001").ExpectedApprovalSourceVersion.ShouldBe(3);
        commands.Single(static command => command.ApprovalId == "approval-002").ExpectedApprovalSourceVersion.ShouldBe(17);
    }

    [Fact]
    public void BatchWithNoAuthorizedItemsShouldDenyEveryItemAndProduceNoCommands()
    {
        // The extreme of partial authority: a human reviewer with authority over none of the grouped items.
        BatchDecisionPlan plan = ApprovalBatchDecisionPlanner.Plan(
            ApprovalBatchDecisionPlanner.HumanActorValue,
            ApprovalDecisionKind.Approve,
            GroupFingerprint,
            [AiItem("approval-001", 1, reviewerHasAuthority: false), AiItem("approval-002", 1, reviewerHasAuthority: false)]);

        // The batch is still "authorized" to run (human actor), but nothing is acted on and no command is dispatched.
        plan.Authorized.ShouldBeTrue();
        plan.AcceptedCount.ShouldBe(0);
        plan.DeniedCount.ShouldBe(2);
        plan.Commands.ShouldBeEmpty();
        plan.Outcomes.ShouldAllBe(outcome => outcome.ReasonCode == ApprovalBatchDecisionPlanner.InsufficientAuthorityReasonCode);
    }

    [Fact]
    public void EmptyBatchShouldBeAuthorizedWithNoOutcomesOrCommands()
    {
        BatchDecisionPlan plan = ApprovalBatchDecisionPlanner.Plan(
            ApprovalBatchDecisionPlanner.HumanActorValue,
            ApprovalDecisionKind.Approve,
            GroupFingerprint,
            []);

        plan.Authorized.ShouldBeTrue();
        plan.AcceptedCount.ShouldBe(0);
        plan.DeniedCount.ShouldBe(0);
        plan.Outcomes.ShouldBeEmpty();
        plan.Commands.ShouldBeEmpty();
    }

    private static AuditEnvelope EnvelopeForDecision(string approvalId, string proposalId)
    {
        JsonElement command = JsonSerializer.SerializeToElement(new
        {
            projectId = "project-alpha",
            approvalId,
            proposalId,
            decision = "approve",
            groupKeyFingerprint = GroupFingerprint,
            riskClass = "high",
            authorityRank = "3",
        });

        ChatBotCommandSubmission submission = new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test")),
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FA" + approvalId[^1],
                CommandType = nameof(DecideAiActionApproval),
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            null,
            ChatBotSurfaceOrigin.Ui);

        ChatBotAuthenticatedActor actor = new("actor-alpha", submission.Principal);
        ChatBotGatewayContext context = new(submission, actor, new ChatBotTenantBinding("tenant-alpha"));
        return AuditEnvelopeFactory.PreCommit(
            context,
            new LifecycleTransitionDefinition(LifecycleStates.Received, LifecycleStates.Proposed),
            Now);
    }

    private static BatchDecisionItem AiItem(string approvalId, long sourceVersion, bool reviewerHasAuthority = true)
        => new(
            BatchDecisionItemKind.AiAction,
            approvalId,
            "project-alpha",
            sourceVersion,
            reviewerHasAuthority,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            DecisionId: "decision-" + approvalId,
            ProposalId: "proposal-" + approvalId,
            SourceMessageId: "message-" + approvalId);

    private static BatchDecisionItem OutboundItem(string approvalId)
        => new(
            BatchDecisionItemKind.Outbound,
            approvalId,
            "project-alpha",
            1,
            true,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            DecisionId: "decision-" + approvalId,
            DraftId: "draft-" + approvalId);
}
