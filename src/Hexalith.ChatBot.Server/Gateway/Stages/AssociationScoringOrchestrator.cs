using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Projects;
using Hexalith.ChatBot.Server.Association.Scoring;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class AssociationScoringOrchestrator(
    IProjectDirectory projectDirectory,
    ISystemClock clock) : IAssociationScoringOrchestrator
{
    public async ValueTask<ScoreMailboxMessageAssociation> ScoreAsync(
        ScoreMailboxMessageAssociation command,
        ChatBotGatewayContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

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
                context.Submission.CorrelationId))
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
                DeterministicAssociationScorer.ResultSchemaVersion),
            [],
            exclusions);
}
