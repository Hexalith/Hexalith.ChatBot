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
        AssociationReviewService service = new(client, AssociationReviewTestText.Create());

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
        AssociationReviewService service = new(client, AssociationReviewTestText.Create());
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
        AssociationReviewService service = new(client, AssociationReviewTestText.Create());
        AssociationReviewModel review = await service.GetAssociationReviewAsync("01ARZ3NDEKTSV4RRFFQ69G5FAZ", TestContext.Current.CancellationToken);

        AssociationCorrectionSubmitResult result = await service.SubmitCorrectionAsync(
            review,
            "01ARZ3NDEKTSV4RRFFQ69G5FBC",
            " Safe correction rationale ",
            TestContext.Current.CancellationToken);

        client.SubmitCount.ShouldBe(1);
        Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation command = client.SubmittedCommand
            .ShouldBeOfType<Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation>();
        // The server's PriorProjectId, not a guess derived from the candidate list.
        command.PriorProjectId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FB9");
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
        AssociationReviewModel review = await new AssociationReviewService(client, AssociationReviewTestText.Create())
            .GetAssociationReviewAsync("01ARZ3NDEKTSV4RRFFQ69G5FAZ", TestContext.Current.CancellationToken);

        review.HasAuthorizedCandidates.ShouldBeFalse();
        review.DisabledActionReasonCodes.ShouldContain("candidate-required");
        review.ReasonCodes.ShouldContain("no-authorized-candidate");
    }

    [Fact]
    public async Task ServiceShouldClassifyRestrictedEvidenceUsingServerLabels()
    {
        FakeChatBotClient client = new() { ReturnRestrictedEvidence = true };
        AssociationReviewModel review = await new AssociationReviewService(client, AssociationReviewTestText.Create())
            .GetAssociationReviewAsync("01ARZ3NDEKTSV4RRFFQ69G5FAZ", TestContext.Current.CancellationToken);

        AssociationEvidenceModel evidence = review.Candidates.Single().Evidence.Single();
        evidence.State.ShouldBe(ChatBotEvidenceState.Redacted);
        evidence.UnavailableReason.ShouldBe("Evidence restricted");
    }

    [Theory]
    [InlineData("unauthorized")]
    [InlineData("restricted")]
    [InlineData("suppressed")]
    [InlineData("redacted")]
    [InlineData("unavailable")]
    [InlineData("expired")]
    [InlineData("stale")]
    public async Task AvailableEvidenceNamesShouldNotOverrideServerLabels(string word)
    {
        foreach (bool omitLabels in new[] { false, true })
        {
            FakeChatBotClient client = new()
            {
                EvidenceOverride = new AssociationEvidenceReference
                {
                    EvidenceReference = $"project-{word}-reference",
                    EvidenceKind = $"{word}-signal",
                    EvidenceFingerprint = "fingerprint-1",
                    VisibilityState = omitLabels ? null : AssociationEvidenceReferenceVisibilityState.Available,
                    RedactionState = omitLabels ? null : AssociationEvidenceReferenceRedactionState.Metadata_only,
                    FreshnessState = omitLabels ? null : AssociationEvidenceReferenceFreshnessState.Fresh,
                },
            };
            AssociationReviewService service = new(client, AssociationReviewTestText.Create());
            AssociationReviewModel review = await service.GetAssociationReviewAsync("association-001", TestContext.Current.CancellationToken);

            review.Evidence.Single().State.ShouldBe(ChatBotEvidenceState.Available);
            review.Candidates.Single().Evidence.Single().State.ShouldBe(ChatBotEvidenceState.Available);
            await service.SubmitDecisionAsync(review, "choose-candidate", review.Candidates.Single().ProjectId, null, TestContext.Current.CancellationToken);
            client.SubmittedCommand.ShouldBeOfType<Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject>()
                .CandidateEvidenceFingerprint.ShouldBe("fingerprint-1");
        }
    }

    [Theory]
    [InlineData("visibility", 1, ChatBotEvidenceState.Redacted)]
    [InlineData("visibility", 2, ChatBotEvidenceState.Unavailable)]
    [InlineData("visibility", 999, ChatBotEvidenceState.Unavailable)]
    [InlineData("redaction", 1, ChatBotEvidenceState.Redacted)]
    [InlineData("redaction", 2, ChatBotEvidenceState.Unavailable)]
    [InlineData("redaction", 999, ChatBotEvidenceState.Unavailable)]
    [InlineData("freshness", 1, ChatBotEvidenceState.Unavailable)]
    [InlineData("freshness", 2, ChatBotEvidenceState.Unavailable)]
    [InlineData("freshness", 999, ChatBotEvidenceState.Unavailable)]
    public async Task StructuredRestrictionShouldWithholdNeutralNamedEvidence(string label, int value, ChatBotEvidenceState expected)
    {
        AssociationEvidenceReference evidence = new()
        {
            EvidenceReference = "neutral-reference",
            EvidenceKind = "subject-signal",
            EvidenceFingerprint = "fingerprint-1",
        };
        switch (label)
        {
            case "visibility": evidence.VisibilityState = (AssociationEvidenceReferenceVisibilityState)value; break;
            case "redaction": evidence.RedactionState = (AssociationEvidenceReferenceRedactionState)value; break;
            case "freshness": evidence.FreshnessState = (AssociationEvidenceReferenceFreshnessState)value; break;
        }

        FakeChatBotClient client = new() { EvidenceOverride = evidence };
        AssociationReviewService service = new(client, AssociationReviewTestText.Create());
        AssociationReviewModel review = await service.GetAssociationReviewAsync("association-001", TestContext.Current.CancellationToken);

        review.Evidence.Single().State.ShouldBe(expected);
        review.Candidates.Single().Evidence.Single().State.ShouldBe(expected);
        InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(() =>
            service.SubmitDecisionAsync(review, "choose-candidate", review.Candidates.Single().ProjectId, null, TestContext.Current.CancellationToken));
        failure.Message.ShouldBe("stale-evidence");
        client.SubmitCount.ShouldBe(0);
    }

    [Fact]
    public async Task ServiceShouldHonorServerRedactionStateEvenWhenEvidenceTextHasNoRestrictionKeyword()
    {
        FakeChatBotClient client = new() { ReturnStructurallyRedactedEvidence = true };
        AssociationReviewModel review = await new AssociationReviewService(client, AssociationReviewTestText.Create())
            .GetAssociationReviewAsync("01ARZ3NDEKTSV4RRFFQ69G5FAZ", TestContext.Current.CancellationToken);

        AssociationEvidenceModel evidence = review.Candidates.Single().Evidence.Single();
        evidence.State.ShouldBe(ChatBotEvidenceState.Redacted);
    }

    /// <summary>
    /// The prior project is an assertion written into the audit trail. When the server supplies none, the
    /// correction must fail closed rather than nominate an arbitrary non-target candidate.
    /// </summary>
    [Fact]
    public async Task ServiceShouldRefuseToInventThePriorProjectWhenTheServerSuppliesNone()
    {
        FakeChatBotClient client = new() { OmitPriorProjectId = true };
        AssociationReviewService service = new(client, AssociationReviewTestText.Create());
        AssociationReviewModel review = await service.GetAssociationReviewAsync("01ARZ3NDEKTSV4RRFFQ69G5FAZ", TestContext.Current.CancellationToken);

        InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(() =>
            service.SubmitCorrectionAsync(review, "01ARZ3NDEKTSV4RRFFQ69G5FBC", null, TestContext.Current.CancellationToken));

        failure.Message.ShouldBe("correction-source-required");
        client.SubmitCount.ShouldBe(0);
    }

    /// <summary>
    /// The fingerprint binds the decision to the evidence it was made on, so a candidate whose evidence is
    /// restricted must not borrow another candidate's fingerprint.
    /// </summary>
    [Fact]
    public async Task ServiceShouldFailClosedRatherThanSignADecisionWithUnusableEvidence()
    {
        FakeChatBotClient client = new() { ReturnRestrictedEvidence = true };
        AssociationReviewService service = new(client, AssociationReviewTestText.Create());
        AssociationReviewModel review = await service.GetAssociationReviewAsync("01ARZ3NDEKTSV4RRFFQ69G5FAZ", TestContext.Current.CancellationToken);

        InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(() =>
            service.SubmitDecisionAsync(review, "choose-candidate", review.Candidates.Single().ProjectId, null, TestContext.Current.CancellationToken));

        failure.Message.ShouldBe("stale-evidence");
        client.SubmitCount.ShouldBe(0);
    }

    /// <summary>A multi-line note is evidence; normalization must not reflow it onto one line.</summary>
    [Fact]
    public async Task ServiceShouldPreserveTheReviewersLineBreaksInADecisionNote()
    {
        FakeChatBotClient client = new();
        AssociationReviewService service = new(client, AssociationReviewTestText.Create());
        AssociationReviewModel review = await service.GetAssociationReviewAsync("01ARZ3NDEKTSV4RRFFQ69G5FAZ", TestContext.Current.CancellationToken);

        await service.SubmitDecisionAsync(
            review,
            "choose-candidate",
            review.Candidates.Single().ProjectId,
            "First   line\nSecond line",
            TestContext.Current.CancellationToken);

        client.SubmittedCommand.ShouldBeOfType<Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject>()
            .DecisionNote.ShouldBe("First line\nSecond line");
    }

    [Fact]
    public async Task ServiceShouldRejectANoteBeyondTheDocumentedCap()
    {
        FakeChatBotClient client = new();
        AssociationReviewService service = new(client, AssociationReviewTestText.Create());
        AssociationReviewModel review = await service.GetAssociationReviewAsync("01ARZ3NDEKTSV4RRFFQ69G5FAZ", TestContext.Current.CancellationToken);

        InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(() =>
            service.SubmitDecisionAsync(
                review,
                "choose-candidate",
                review.Candidates.Single().ProjectId,
                new string('x', AssociationReviewService.MaximumNoteLength + 1),
                TestContext.Current.CancellationToken));

        failure.Message.ShouldBe("association-review-note-too-long");
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

        public AssociationEvidenceReference? EvidenceOverride { get; init; }

        public bool ReturnStructurallyRedactedEvidence { get; init; }

        public bool ReturnCorrectedAssociation { get; set; }

        public bool OmitPriorProjectId { get; init; }

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
            return Task.FromResult(CreateStatus(associationId, ReturnEmptyCandidates, ReturnRestrictedEvidence, ReturnStructurallyRedactedEvidence, ReturnCorrectedAssociation, OmitPriorProjectId, EvidenceOverride));
        }

        public Task<ProjectConversationResponse> GetProjectConversationAsync(
            string projectId,
            string? cursor = null,
            int pageSize = 25,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        private static AssociationRoutingStatus CreateStatus(string associationId, bool empty, bool restrictedEvidence, bool structurallyRedacted, bool corrected,
            bool omitPrior, AssociationEvidenceReference? evidenceOverride)
        {
            AssociationEvidenceReference evidence = evidenceOverride ?? new()
            {
                EvidenceReference = "evidence-ref-1",
                EvidenceFingerprint = "fingerprint-1",
                EvidenceKind = "subject-signal",
                VisibilityState = structurallyRedacted ? AssociationEvidenceReferenceVisibilityState.Redacted : null,
                RedactionState = structurallyRedacted || restrictedEvidence ? AssociationEvidenceReferenceRedactionState.Redacted : null,
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
                PriorProjectId = omitPrior ? null : "01ARZ3NDEKTSV4RRFFQ69G5FB9",
                DownstreamImpactStatus = corrected ? "preview-only" : null,
            };
        }
    }
}
