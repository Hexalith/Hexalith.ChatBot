using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Operations;

namespace Hexalith.ChatBot.Server.Projections;

internal static class AssociationProjectionTranslator
{
    public static readonly string CandidatesGeneratedEventType = typeof(MailboxAssociationCandidatesGenerated).FullName!;
    public static readonly string AutoAssociatedEventType = typeof(MailboxEmailAssociatedToProject).FullName!;
    public static readonly string FailedClosedEventType = typeof(MailboxAssociationScoringFailedClosed).FullName!;

    public static AssociationNotification? TryCreateNotification(PublishedAssociationEvent? published)
    {
        if (published is null ||
            !string.Equals(published.Domain, ChatBotEventStore.DomainName, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(published.TenantId) ||
            string.IsNullOrWhiteSpace(published.AggregateId) ||
            string.IsNullOrWhiteSpace(published.IntakeId) ||
            string.IsNullOrWhiteSpace(published.SourceMailboxId) ||
            string.IsNullOrWhiteSpace(published.SourceConversationId) ||
            string.IsNullOrWhiteSpace(published.ThresholdPolicyVersion) ||
            string.IsNullOrWhiteSpace(published.DerivationKernelVersion) ||
            string.IsNullOrWhiteSpace(published.RedactionState) ||
            string.IsNullOrWhiteSpace(published.RetentionClass) ||
            published.SequenceNumber <= 0)
        {
            return null;
        }

        AssociationScoringOutcome? outcome = OutcomeFor(published);
        if (outcome is null)
        {
            return null;
        }

        return new AssociationNotification(
            published.TenantId,
            published.AggregateId,
            published.IntakeId,
            published.SourceMailboxId,
            published.SourceConversationId,
            published.SourceThreadId,
            published.ProjectId,
            published.ProjectDisplayName,
            outcome.Value,
            published.ThresholdBand,
            published.ConfidenceScore,
            published.Candidates ?? [],
            published.Exclusions ?? [],
            published.ThresholdPolicyVersion,
            published.DerivationKernelVersion,
            published.RedactionState,
            published.RetentionClass,
            published.SequenceNumber,
            published.DetectedAt == default ? published.Timestamp : published.DetectedAt,
            published.CorrelationId ?? string.Empty);
    }

    private static AssociationScoringOutcome? OutcomeFor(PublishedAssociationEvent published)
    {
        if (string.Equals(published.EventTypeName, CandidatesGeneratedEventType, StringComparison.Ordinal))
        {
            return published.Outcome ?? AssociationScoringOutcome.CandidatesGenerated;
        }

        if (string.Equals(published.EventTypeName, AutoAssociatedEventType, StringComparison.Ordinal))
        {
            return AssociationScoringOutcome.AutoAssociated;
        }

        if (string.Equals(published.EventTypeName, FailedClosedEventType, StringComparison.Ordinal))
        {
            return AssociationScoringOutcome.FailedClosed;
        }

        return null;
    }
}
