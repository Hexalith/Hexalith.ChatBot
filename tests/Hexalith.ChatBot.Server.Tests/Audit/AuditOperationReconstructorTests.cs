using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.2 (AC1, NFR50a) coverage for the per-operation reconstructability evaluator. Reconstructability is the
/// STRONGER NFR50a test (rebuild end-to-end from the chain alone), not the shipped NFR50 field-presence test: a
/// complete mapped chain reconstructs; an unmapped command is a completeness gap; a missing outcome / required field
/// cannot be rebuilt; an empty operation is chain-missing; and every one of the eleven NFR15a paths is mapped.
/// </summary>
public sealed class AuditOperationReconstructorTests
{
    // A mapped, fully-populated post-commit envelope (CreateOutboundDraft → outbound-draft-creation path).
    private static AuditEnvelope MappedEnvelope(string resourceId = "note-1")
        => WormAuditTestData.Envelope("tenant-alpha", commandName: "CreateOutboundDraft", resourceId: resourceId);

    [Fact]
    public void CompleteMappedChainReconstructsAndAssemblesEndState()
    {
        AuditOperationReconstructionResult result = AuditOperationReconstructor.Reconstruct([MappedEnvelope()]);

        result.IsReconstructable.ShouldBeTrue();
        result.ReasonCode.ShouldBe(AuditOperationReconstructionResult.ReconstructableReasonCode);
        result.PathCode.ShouldBe("outbound-draft-creation");
        result.State.ShouldNotBeNull();
        result.State!.ResourceId.ShouldBe("note-1");
        result.State.Outcome.ShouldBe("proposed");
        // The end-state is ASSEMBLED (path + transition + outcome), not merely "fields present".
        result.State.ResultingStateToken.ShouldBe("outbound-draft-creation:Proposed->Accepted:proposed");
        result.State.ProjectionRedactionState.ShouldBe("metadata_only");
    }

    [Fact]
    public void UnmappedCommandIsACompletenessGapNotSilentlyDropped()
    {
        // "TestCommand" maps to no NFR15a path; the operation is surfaced as a gap, never dropped from the denominator.
        AuditOperationReconstructionResult result = AuditOperationReconstructor.Reconstruct(
            [WormAuditTestData.Envelope("tenant-alpha", commandName: "TestCommand")]);

        result.IsReconstructable.ShouldBeFalse();
        result.ReasonCode.ShouldBe(AuditOperationReconstructionResult.UnmappedPathReasonCode);
    }

    [Fact]
    public void MissingOutcomeIsNotReconstructable()
    {
        AuditOperationReconstructionResult result = AuditOperationReconstructor.Reconstruct(
            [MappedEnvelope() with { Outcome = string.Empty }]);

        result.IsReconstructable.ShouldBeFalse();
        result.ReasonCode.ShouldBe(AuditOperationReconstructionResult.OutcomeAbsentReasonCode);
    }

    [Fact]
    public void MissingRequiredReconstructionFieldIsNotReconstructable()
    {
        // All fields present EXCEPT the policy snapshot id → the end-state cannot be rebuilt from the chain alone.
        AuditOperationReconstructionResult result = AuditOperationReconstructor.Reconstruct(
            [MappedEnvelope() with { PolicySnapshotId = string.Empty }]);

        result.IsReconstructable.ShouldBeFalse();
        result.ReasonCode.ShouldBe(AuditOperationReconstructionResult.StateUnreconstructableReasonCode);
    }

    [Fact]
    public void MissingSourceEvidenceRefsIsNotReconstructable()
    {
        // SourceEvidenceRefs is a required reconstruction field (an operation with no evidence cannot be rebuilt from
        // the chain alone). All other fields present but an empty ref set → state_unreconstructable, not a fabricated
        // success.
        AuditOperationReconstructionResult result = AuditOperationReconstructor.Reconstruct(
            [MappedEnvelope() with { SourceEvidenceRefs = [] }]);

        result.IsReconstructable.ShouldBeFalse();
        result.ReasonCode.ShouldBe(AuditOperationReconstructionResult.StateUnreconstructableReasonCode);
    }

    [Fact]
    public void MissingStateTransitionIsNotReconstructable()
    {
        // StateTransition is checked as non-empty (its '>' is intentionally outside the safe-token charset). An absent
        // transition means the end-state arrow cannot be assembled → state_unreconstructable.
        AuditOperationReconstructionResult result = AuditOperationReconstructor.Reconstruct(
            [MappedEnvelope() with { StateTransition = string.Empty }]);

        result.IsReconstructable.ShouldBeFalse();
        result.ReasonCode.ShouldBe(AuditOperationReconstructionResult.StateUnreconstructableReasonCode);
    }

    [Fact]
    public void EmptyOperationIsChainMissing()
    {
        AuditOperationReconstructionResult result = AuditOperationReconstructor.Reconstruct([]);

        result.IsReconstructable.ShouldBeFalse();
        result.ReasonCode.ShouldBe(AuditOperationReconstructionResult.ChainMissingReasonCode);
    }

    [Fact]
    public void PostCommitEnvelopeIsPreferredAsTheResultBearingRecord()
    {
        AuditEnvelope preCommit = MappedEnvelope() with { Phase = AuditCommitPhase.PreCommit, Outcome = "gate_passed" };
        AuditEnvelope postCommit = MappedEnvelope() with { Phase = AuditCommitPhase.PostCommit, Outcome = "proposed" };

        AuditOperationReconstructionResult result = AuditOperationReconstructor.Reconstruct([preCommit, postCommit]);

        result.IsReconstructable.ShouldBeTrue();
        result.State!.Outcome.ShouldBe("proposed");
    }

    [Fact]
    public void EveryInventoryPathIsMappedAndAnUnknownCommandIsAGap()
    {
        // AC1: the map must cover all eleven NFR15a paths (the value set of the map ⊇ the inventory codes), so no path
        // is unaccountable; and an unknown command resolves to no path (a gap).
        HashSet<string> mappedCodes = [];
        foreach (ChatBotStateWritingPath path in ChatBotStateWritingPathInventory.Paths)
        {
            // Each inventory path must be reachable by at least one command; assert via the reconstructor's resolution.
            mappedCodes.Add(path.Code);
        }

        // Resolve a representative command per path and confirm coverage of all eleven.
        HashSet<string> reachable = [];
        foreach (string command in new[]
                 {
                     "CaptureMailboxMessageIntake", "AssociateEmailToProject", "MarkEmailAssociationNeedsReview",
                     "CorrectEmailProjectAssociation", "ProposeAIAction", "DecideAiActionApproval",
                     "ExecuteApprovedAIAction", "CreateOutboundDraft", "ExecuteApprovedOutboundDraft",
                     "SubmitTenantPolicyChange", "AssignTenantAdminRole",
                 })
        {
            ChatBotStateWritingPath? path = ChatBotAuditPathMap.Resolve(WormAuditTestData.Envelope("t", commandName: command));
            path.ShouldNotBeNull();
            reachable.Add(path!.Code);
        }

        reachable.ShouldBe(mappedCodes, ignoreOrder: true);
        ChatBotAuditPathMap.Resolve(WormAuditTestData.Envelope("t", commandName: "NotAStateWritingCommand")).ShouldBeNull();
    }
}
