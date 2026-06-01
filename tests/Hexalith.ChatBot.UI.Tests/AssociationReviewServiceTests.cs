using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.Services;
using Hexalith.ChatBot.UI.State.AssociationReview;

using Shouldly;

using IChatBotCommand = Hexalith.ChatBot.Contracts.Commands.IChatBotCommand;
using ChatBotSurfaceOrigin = Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class AssociationReviewServiceTests
{
    [Fact]
    public async Task ServiceShouldReadRoutingStatusThroughClientAndMapAuthorizedMetadataOnlyCandidates()
    {
        FakeChatBotClient client = new();
        AssociationReviewService service = new(client);

        AssociationReviewModel review = await service.GetAssociationReviewAsync("01ARZ3NDEKTSV4RRFFQ69G5FAZ", TestContext.Current.CancellationToken);

        client.LastAssociationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAZ");
        review.LifecycleState.ShouldBe("NeedsReview");
        review.ThresholdBand.ShouldBe("ambiguous");
        review.Candidates.ShouldHaveSingleItem().Evidence.ShouldHaveSingleItem().State.ShouldBe(ChatBotEvidenceState.Available);
        review.DisabledActionReasonCodes.ShouldBeEmpty();
        review.NextActionReasonCodes.ShouldContain("association_ambiguous_routed");
        review.Candidates.Single().DisplayLabel.ShouldBe("Authorized candidate");
    }

    [Fact]
    public async Task ServiceShouldSubmitChooseCandidateThroughClientAndRefreshRoutingStatus()
    {
        FakeChatBotClient client = new();
        AssociationReviewService service = new(client);
        AssociationReviewModel review = await service.GetAssociationReviewAsync("01ARZ3NDEKTSV4RRFFQ69G5FAZ", TestContext.Current.CancellationToken);

        AssociationDecisionSubmitResult result = await service.SubmitDecisionAsync(
            review,
            "choose-candidate",
            review.Candidates.Single().ProjectId,
            " Safe note ",
            TestContext.Current.CancellationToken);

        client.SubmitCount.ShouldBe(1);
        client.SubmittedCommand.ShouldBeOfType<Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject>()
            .CandidateEvidenceFingerprint.ShouldBe("fingerprint-1");
        client.SubmittedOrigin.ShouldBe(ChatBotSurfaceOrigin.Ui);
        result.CommandId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAC");
        result.Review.AssociationId.ShouldBe(review.AssociationId);
        client.RoutingReadCount.ShouldBe(2);
    }

    [Fact]
    public async Task ServiceShouldSubmitCorrectionThroughClientAndRefreshRoutingStatus()
    {
        FakeChatBotClient client = new();
        AssociationReviewService service = new(client);
        AssociationReviewModel review = await service.GetAssociationReviewAsync("01ARZ3NDEKTSV4RRFFQ69G5FAZ", TestContext.Current.CancellationToken);

        AssociationCorrectionSubmitResult result = await service.SubmitCorrectionAsync(
            review,
            "01ARZ3NDEKTSV4RRFFQ69G5FBC",
            " Safe correction rationale ",
            TestContext.Current.CancellationToken);

        client.SubmitCount.ShouldBe(1);
        Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation command = client.SubmittedCommand
            .ShouldBeOfType<Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation>();
        command.PriorProjectId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FBB");
        command.TargetProjectId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FBC");
        command.CorrectionKind.ShouldBe(Hexalith.ChatBot.Contracts.Enums.AssociationCorrectionKind.ProjectReassignment);
        command.CorrectionRationale.ShouldBe("Safe correction rationale");
        client.SubmittedOrigin.ShouldBe(ChatBotSurfaceOrigin.Ui);
        result.Review.LifecycleState.ShouldBe("Corrected");
        result.Review.CorrectedProjectId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FBC");
        client.RoutingReadCount.ShouldBe(2);
    }

    [Fact]
    public async Task ServiceShouldPreserveNoAuthorizedCandidatesAsVisibleReviewState()
    {
        FakeChatBotClient client = new() { ReturnEmptyCandidates = true };
        AssociationReviewModel review = await new AssociationReviewService(client)
            .GetAssociationReviewAsync("01ARZ3NDEKTSV4RRFFQ69G5FAZ", TestContext.Current.CancellationToken);

        review.HasAuthorizedCandidates.ShouldBeFalse();
        review.DisabledActionReasonCodes.ShouldContain("candidate-required");
        review.ReasonCodes.ShouldContain("no-authorized-candidate");
    }

    [Fact]
    public async Task ServiceShouldClassifyRestrictedEvidenceWithoutRelyingOnlyOnUnauthorizedKind()
    {
        FakeChatBotClient client = new() { ReturnRestrictedEvidence = true };
        AssociationReviewModel review = await new AssociationReviewService(client)
            .GetAssociationReviewAsync("01ARZ3NDEKTSV4RRFFQ69G5FAZ", TestContext.Current.CancellationToken);

        AssociationEvidenceModel evidence = review.Candidates.Single().Evidence.Single();
        evidence.State.ShouldBe(ChatBotEvidenceState.Unauthorized);
        evidence.UnavailableReason.ShouldBe("Evidence restricted");
    }

    private sealed class FakeChatBotClient : IChatBotClient
    {
        public string? LastAssociationId { get; private set; }

        public int RoutingReadCount { get; private set; }

        public int SubmitCount { get; private set; }

        public IChatBotCommand? SubmittedCommand { get; private set; }

        public ChatBotSurfaceOrigin SubmittedOrigin { get; private set; }

        public bool ReturnEmptyCandidates { get; init; }

        public bool ReturnRestrictedEvidence { get; init; }

        public bool ReturnCorrectedAssociation { get; set; }

        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
        {
            SubmitCount++;
            SubmittedCommand = command;
            SubmittedOrigin = origin;
            ReturnCorrectedAssociation = true;
            return Task.FromResult(new CommandSubmissionResponse
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAC",
                CorrelationId = correlationId ?? "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                LifecycleState = LifecycleState.Proposed,
                AcceptedAt = new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero),
            });
        }

        public Task<OperationStatus> GetOperationStatusAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(
            string associationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
        {
            LastAssociationId = associationId;
            RoutingReadCount++;
            return Task.FromResult(CreateStatus(associationId, ReturnEmptyCandidates, ReturnRestrictedEvidence, ReturnCorrectedAssociation));
        }

        public Task<ProjectConversationResponse> GetProjectConversationAsync(
            string projectId,
            string? cursor = null,
            int pageSize = 25,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        private static AssociationRoutingStatus CreateStatus(string associationId, bool empty, bool restrictedEvidence, bool corrected)
        {
            AssociationEvidenceReference evidence = new()
            {
                EvidenceReference = restrictedEvidence ? "suppressed-candidate-metadata" : "evidence-ref-1",
                EvidenceFingerprint = "fingerprint-1",
                EvidenceKind = restrictedEvidence ? "restricted-project-signal" : "subject-signal",
            };

            return new AssociationRoutingStatus
            {
                AssociationId = associationId,
                IntakeId = "01ARZ3NDEKTSV4RRFFQ69G5FBA",
                SourceMailboxId = "mailbox-metadata",
                SourceConversationId = "conversation-metadata",
                LifecycleState = corrected ? LifecycleState.Corrected : LifecycleState.NeedsReview,
                Outcome = AssociationScoringOutcome.CandidatesGenerated,
                ThresholdBand = AssociationThresholdBand.Ambiguous,
                ConfidenceScore = 0.64,
                ReasonCodes = empty ? [AssociationReasonCode.NoAuthorizedCandidate] : [AssociationReasonCode.MultipleAuthorizedCandidates],
                Candidates = empty
                    ? []
                    :
                    [
                        new AssociationCandidate
                        {
                            ProjectId = "01ARZ3NDEKTSV4RRFFQ69G5FBB",
                            DisplayName = "Authorized candidate",
                            ConfidenceScore = 0.64,
                            Rank = 1,
                            ReasonCodes = [AssociationReasonCode.ExplicitProjectIdentifierMatched],
                            EvidenceRefs = [evidence],
                            ConfidenceInputs = [],
                            RequiredEvidenceComplete = true,
                        },
                    ],
                Exclusions = [],
                ThresholdPolicyVersion = "association-thresholds.m0.default.v1",
                EvidenceRefs = [evidence],
                KernelVersion = "association-deterministic.kernel.m0.v1",
                DetectedAt = new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero),
                SourceProvenance = AssociationRoutingStatusSourceProvenance.M365MailboxIntake,
                RedactionState = AssociationRoutingStatusRedactionState.Metadata_only,
                RetentionClass = AssociationRoutingStatusRetentionClass.Collaboration_input,
                SchemaVersion = "chatbot.association-routing-status.v1",
                SourceVersion = 1,
                CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                DisabledActionReasonCodes = empty ? ["candidate-required"] : [],
                NextActionReasonCodes = [ChatBotMessageCode.Association_ambiguous_routed],
                CorrectedProjectId = corrected ? "01ARZ3NDEKTSV4RRFFQ69G5FBC" : null,
                PredecessorAssociationId = corrected ? associationId : null,
                DownstreamImpactStatus = corrected ? "preview-only" : null,
            };
        }
    }
}
