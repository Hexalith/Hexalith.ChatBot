using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Projects;
using Hexalith.ChatBot.Server.Association.Scoring;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Tests.Observability;

using Shouldly;

using GeneratedCommandSubmissionRequest = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest;
using GeneratedRequestSchemaVersion = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequestRequestSchemaVersion;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class AssociationScoringOrchestratorTests
{
    private const string AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAB";
    private const string IntakeId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string Tenant = "tenant-alpha";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly DateTimeOffset DetectedAt = new(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ScoreAsyncShouldUseGatewayTenantDefaultPolicyClockAndAuthorizedCandidates()
    {
        RecordingProjectDirectory directory = new(new ProjectDirectoryAssociationResult(
            true,
            [new ProjectAssociationCandidateEvidence("project-001", "Roadmap", [Signal()])],
            []));
        AssociationScoringOrchestrator orchestrator = new(directory, new FixedClock());

        ScoreMailboxMessageAssociation scored = await orchestrator.ScoreAsync(
            Command(thresholdPolicy: null),
            Context(),
            TestContext.Current.CancellationToken);

        directory.Request.ShouldNotBeNull();
        directory.Request.TenantId.ShouldBe(Tenant);
        directory.Request.CorrelationId.ShouldBe(CorrelationId);
        directory.Request.SourceConversationId.ShouldBe("conversation-001");
        directory.Request.SourceThreadId.ShouldBe("thread-001");
        directory.Request.Signals.ShouldHaveSingleItem().ProjectId.ShouldBe("project-001");

        scored.ThresholdPolicy.ShouldBe(AssociationThresholdPolicySnapshot.DefaultM0);
        scored.Candidates.ShouldNotBeNull().ShouldHaveSingleItem().ProjectId.ShouldBe("project-001");
        scored.Result.ShouldNotBeNull().Outcome.ShouldBe(AssociationScoringOutcome.AutoAssociated);
        scored.Result.ThresholdBand.ShouldBe(AssociationThresholdBand.Auto);
        scored.Result.ConfidenceScore.ShouldBe(0.9);
        scored.Result.DetectedAt.ShouldBe(DetectedAt);
        scored.Result.CorrelationId.ShouldBe(CorrelationId);
        scored.ScoringKernelVersion.ShouldBe(DeterministicAssociationScorer.CurrentKernelVersion);
    }

    [Fact]
    public async Task ScoreAsyncShouldFailClosedWhenAuthorizationEvidenceIsUnavailable()
    {
        AssociationExclusion exclusion = new(
            "project-001",
            AssociationExclusionState.Unavailable,
            AssociationReasonCode.AuthorizationEvidenceUnavailable,
            "mailbox:project-id",
            "hash-project");
        RecordingProjectDirectory directory = new(ProjectDirectoryAssociationResult.Unavailable([exclusion]));
        AssociationScoringOrchestrator orchestrator = new(directory, new FixedClock());

        ScoreMailboxMessageAssociation scored = await orchestrator.ScoreAsync(
            Command(thresholdPolicy: AssociationThresholdPolicySnapshot.DefaultM0),
            Context(),
            TestContext.Current.CancellationToken);

        scored.Candidates.ShouldNotBeNull().ShouldBeEmpty();
        scored.Exclusions.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBe(exclusion);
        scored.Result.ShouldNotBeNull().Outcome.ShouldBe(AssociationScoringOutcome.FailedClosed);
        scored.Result.ThresholdBand.ShouldBe(AssociationThresholdBand.FailClosed);
        scored.Result.ConfidenceScore.ShouldBe(0.0);
        scored.Result.ReasonCodes.ShouldBe([AssociationReasonCode.AuthorizationEvidenceUnavailable]);
        scored.Result.DetectedAt.ShouldBe(DetectedAt);
    }

    [Fact]
    public async Task ScoreAsyncShouldRecordAssociationLatencyOnceForTheBoundTenant()
    {
        RecordingProjectDirectory directory = new(ProjectDirectoryAssociationResult.Unavailable([]));
        RecordingChatBotMetrics metrics = new();
        AssociationScoringOrchestrator orchestrator = new(directory, new FixedClock(), metrics);

        _ = await orchestrator.ScoreAsync(Command(thresholdPolicy: null), Context(), TestContext.Current.CancellationToken);

        (string operationClass, string tenantId, double milliseconds) = metrics.Latencies.ShouldHaveSingleItem();
        operationClass.ShouldBe(ChatBotOperationClasses.Association);
        tenantId.ShouldBe(Tenant);
        milliseconds.ShouldBeGreaterThanOrEqualTo(0);
    }

    private static ScoreMailboxMessageAssociation Command(AssociationThresholdPolicySnapshot? thresholdPolicy)
        => new(
            AssociationId,
            IntakeId,
            "controlled-mailbox-001",
            "conversation-001",
            "thread-001",
            [Signal()],
            thresholdPolicy,
            [],
            [],
            null,
            string.Empty);

    private static AssociationDeterministicSignal Signal()
        => new(
            AssociationSignalClass.ExplicitProjectIdentifier,
            "project-001",
            "mailbox:project-id",
            "hash-project",
            0.9,
            RequiredForAutoAssociation: true);

    private static ChatBotGatewayContext Context()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test"));
        ChatBotCommandSubmission submission = new(
            principal,
            new GeneratedCommandSubmissionRequest
            {
                CommandId = IntakeId,
                CommandType = nameof(ScoreMailboxMessageAssociation),
                Command = JsonDocument.Parse("{}").RootElement.Clone(),
                RequestSchemaVersion = GeneratedRequestSchemaVersion.V1,
            },
            CorrelationId,
            null,
            ChatBotSurfaceOrigin.Mailbox);

        return new ChatBotGatewayContext(
            submission,
            new ChatBotAuthenticatedActor("actor-alpha", principal),
            new ChatBotTenantBinding(Tenant));
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DetectedAt;
    }

    private sealed class RecordingProjectDirectory(ProjectDirectoryAssociationResult result) : IProjectDirectory
    {
        public ProjectDirectoryAssociationRequest? Request { get; private set; }

        public ValueTask<ProjectDirectoryAssociationResult> FindAuthorizedCandidatesAsync(
            ProjectDirectoryAssociationRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Request = request;
            return ValueTask.FromResult(result);
        }
    }
}
