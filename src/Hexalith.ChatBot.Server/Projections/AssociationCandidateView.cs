using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

public sealed record AssociationCandidateView(
    string TenantId,
    string AssociationId,
    string IntakeId,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    string? ProjectId,
    string? ProjectDisplayName,
    AssociationScoringOutcome Outcome,
    AssociationThresholdBand ThresholdBand,
    double ConfidenceScore,
    IReadOnlyList<AssociationCandidate> Candidates,
    IReadOnlyList<AssociationExclusion> Exclusions,
    string ThresholdPolicyVersion,
    string SchemaVersion,
    string SourceProvenance,
    string DerivationKernelVersion,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string CorrelationId,
    DateTimeOffset DetectedAt,
    DateTimeOffset LastUpdatedAt)
{
    public const string CurrentSchemaVersion = "chatbot.association-candidate-view.v1";
    public const string MailboxSourceProvenance = "m365-mailbox-intake";

    public static string KeyFor(string tenantId, string associationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(associationId);
        return $"{tenantId}:association:{associationId}";
    }
}
