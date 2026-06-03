using System.Diagnostics;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Projects;
using Hexalith.ChatBot.Server.Association.Scoring;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class AssociationScoringOrchestrator(
    IProjectDirectory projectDirectory,
    ISystemClock clock,
    IChatBotMetrics? metrics = null) : IAssociationScoringOrchestrator
{
    private readonly IChatBotMetrics _metrics = metrics ?? NullChatBotMetrics.Instance;

    public async ValueTask<ScoreMailboxMessageAssociation> ScoreAsync(
        ScoreMailboxMessageAssociation command,
        ChatBotGatewayContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        // Story 8.2: association-scoring latency (operation-class `association`), full orchestration duration.
        long startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            return await ScoreCoreAsync(command, context, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _metrics.RecordAssociationLatency(context.TenantBinding.TenantId, Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
        }
    }

    private async ValueTask<ScoreMailboxMessageAssociation> ScoreCoreAsync(
        ScoreMailboxMessageAssociation command,
        ChatBotGatewayContext context,
        CancellationToken cancellationToken)
    {
        AssociationThresholdPolicySnapshot policy = command.ThresholdPolicy ?? AssociationThresholdPolicySnapshot.DefaultM0;
        ProjectDirectoryAssociationResult directory = await projectDirectory
            .FindAuthorizedCandidatesAsync(
                new ProjectDirectoryAssociationRequest(
                    context.TenantBinding.TenantId,
                    command.SourceConversationId,
                    command.SourceThreadId,
                    command.DeterministicSignals,
                    context.Submission.CorrelationId),
                cancellationToken)
            .ConfigureAwait(false);

        AssociationScoringComputation computation = directory.IsAvailable
            ? DeterministicAssociationScorer.Score(new AssociationScoringInput(
                command.AssociationId,
                command.IntakeId,
                command.SourceMailboxId,
                command.SourceConversationId,
                command.SourceThreadId,
                command.DeterministicSignals,
                directory.Candidates,
                directory.Exclusions,
                policy,
                string.IsNullOrWhiteSpace(command.ScoringKernelVersion)
                    ? DeterministicAssociationScorer.CurrentKernelVersion
                    : command.ScoringKernelVersion,
                clock.UtcNow,
                context.Submission.CorrelationId,
                command.ExternalSender,
                command.StrictnessPolicy,
                command.Authenticity))
            : FailClosed(command, context, policy, directory.Exclusions, clock.UtcNow);

        return command with
        {
            ThresholdPolicy = policy,
            Candidates = computation.Candidates,
            Exclusions = computation.Exclusions,
            Result = computation.Result,
            ScoringKernelVersion = computation.Result.KernelVersion,
        };
    }

    private static AssociationScoringComputation FailClosed(
        ScoreMailboxMessageAssociation command,
        ChatBotGatewayContext context,
        AssociationThresholdPolicySnapshot policy,
        IReadOnlyList<AssociationExclusion> exclusions,
        DateTimeOffset detectedAt)
        => new(
            new AssociationScoringResult(
                0.0,
                AssociationThresholdBand.FailClosed,
                AssociationScoringOutcome.FailedClosed,
                [AssociationReasonCode.AuthorizationEvidenceUnavailable],
                string.IsNullOrWhiteSpace(command.ScoringKernelVersion)
                    ? DeterministicAssociationScorer.CurrentKernelVersion
                    : command.ScoringKernelVersion,
                detectedAt.ToUniversalTime(),
                command.SourceMailboxId,
                command.IntakeId,
                command.SourceConversationId,
                command.SourceThreadId,
                context.Submission.CorrelationId,
                DeterministicAssociationScorer.MetadataOnlyRedactionState,
                DeterministicAssociationScorer.CollaborationRetentionClass,
                DeterministicAssociationScorer.ResultSchemaVersion,
                command.ExternalSender,
                command.StrictnessPolicy,
                "authorization-evidence-unavailable"),
            [],
            exclusions);
}
