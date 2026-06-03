using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.10 (AC4, NFR7/FR68): the server-callable <see cref="ConsentGateEvaluator"/> seam. The pure
/// <c>ConsentRequirementPolicy</c>/<c>ConsentGate</c> are covered in the contracts tests; these pin the COMPOSITION the
/// live AI-processing / retention paths will consult — requirement resolution (unknown kind / missing entry ⇒
/// <c>required</c>) folded into the active-basis gate (only an <c>active</c> basis satisfies a <c>required</c> kind).
/// Also pins the deferred <see cref="ConsentRequirementProfileMapper"/> hook returning the regulatory-profile default.
/// </summary>
public sealed class ConsentGateEvaluatorTests
{
    [Theory]
    // A required kind (the published default) is satisfied only by an active basis.
    [InlineData(ConsentSubjectKinds.AiProcessing, ConsentRecordStatuses.Active, ConsentGateDecisions.Satisfied)]
    [InlineData(ConsentSubjectKinds.AiProcessing, null, ConsentGateDecisions.BlockedMissingBasis)]
    [InlineData(ConsentSubjectKinds.ExternalParticipant, ConsentRecordStatuses.Withdrawn, ConsentGateDecisions.BlockedMissingBasis)]
    [InlineData(ConsentSubjectKinds.RetainedContent, ConsentRecordStatuses.Expired, ConsentGateDecisions.BlockedMissingBasis)]
    // AC4 fail-closed: an unknown subject kind biases to required ⇒ blocked when no active basis exists.
    [InlineData("unknown-subject", null, ConsentGateDecisions.BlockedMissingBasis)]
    public void EvaluateForGovernedActionShouldFailClosedAgainstThePublishedProfile(
        string subjectKind,
        string? activeRecordStatus,
        string expected)
        => ConsentGateEvaluator.EvaluateForGovernedAction(
            subjectKind, ConsentRequirementMatrix.Published, activeRecordStatus).ShouldBe(expected);

    [Fact]
    public void EvaluateForGovernedActionShouldSatisfyANotRequiredKindWithoutABasis()
    {
        // A tenant-relaxed profile that marks a kind not-required satisfies the gate without any active basis.
        ConsentRequirementProfile relaxed = new(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ConsentSubjectKinds.Attachment] = ConsentRequirementDispositions.NotRequired,
        });

        ConsentGateEvaluator.EvaluateForGovernedAction(ConsentSubjectKinds.Attachment, relaxed, activeRecordStatus: null)
            .ShouldBe(ConsentGateDecisions.Satisfied);
    }

    [Theory]
    [InlineData("policy-snapshot-admin-v1")]
    [InlineData(null)]
    public void ProfileMapperShouldReturnThePublishedRegulatoryDefault(string? policySnapshotId)
        => ConsentRequirementProfileMapper.ProfileFor(policySnapshotId)
            .ShouldBeSameAs(ConsentRequirementMatrix.Published);
}
