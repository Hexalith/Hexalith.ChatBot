using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>Submits trusted correction invalidations with an envelope-bound admission marker.</summary>
internal sealed class AiActionProposalInvalidationCoordinator(
    IProjectConversationProjectionStore conversationStore,
    IEventStoreGatewayClient eventStore,
    IChatBotAdmissionMarker admissionMarker) : IAiActionProposalInvalidationCoordinator
{
    /// <inheritdoc/>
    public async Task InvalidateAsync(AssociationCandidateView correctedAssociation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(correctedAssociation);
        if (string.IsNullOrWhiteSpace(correctedAssociation.CorrectionId))
        {
            return;
        }

        IReadOnlyList<AiActionProposalRecord> proposals = await conversationStore
            .ReadAiActionProposalsForAssociationAsync(
                correctedAssociation.TenantId,
                correctedAssociation.AssociationId,
                correctedAssociation.SourceVersion,
                cancellationToken)
            .ConfigureAwait(false);

        foreach (AiActionProposalRecord proposal in proposals)
        {
            string projectId = ProjectIdFromResources(proposal.AffectedResourceReferences);
            MarkAiActionProposalInvalidatedByCorrection command = new(
                projectId,
                proposal.ProposalId,
                ApprovalId: null,
                proposal.TaskIntentId,
                proposal.SourceMessageId,
                proposal.SourceConversationItemId,
                proposal.RequesterId,
                correctedAssociation.AssociationId,
                correctedAssociation.CorrectionId,
                correctedAssociation.LifecycleState.ToString().ToLowerInvariant(),
                correctedAssociation.SourceVersion,
                correctedAssociation.CorrelationId,
                proposal.RedactionState,
                proposal.RetentionClass)
            {
                StateOwnerAggregateId = proposal.StateOwnerAggregateId ?? projectId,
            };

            string messageId = $"{correctedAssociation.CorrectionId}:{proposal.ProposalId}:invalidated-by-correction";
            const string actorId = "ai-action-proposal-invalidation-coordinator";
            JsonElement payload = JsonSerializer.SerializeToElement(command);
            SubmitCommandRequest request = new(
                MessageId: messageId,
                Tenant: correctedAssociation.TenantId,
                Domain: ChatBotEventStore.DomainName,
                AggregateId: command.StateOwnerAggregateId,
                CommandType: nameof(MarkAiActionProposalInvalidatedByCorrection),
                Payload: payload,
                CorrelationId: correctedAssociation.CorrelationId,
                Extensions: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["surfaceOrigin"] = "workflow",
                    ["actorType"] = "system",
                    ["actorId"] = actorId,
                    [DataProtectionChatBotAdmissionMarker.ExtensionKey] = admissionMarker.Create(messageId, correctedAssociation.TenantId,
                        command.StateOwnerAggregateId, nameof(MarkAiActionProposalInvalidatedByCorrection), payload,
                        correctedAssociation.CorrelationId, actorId, "workflow", null),
                    ["workflowInstanceId"] = correctedAssociation.WorkflowInstanceId ?? correctedAssociation.CorrectionId,
                });

            _ = await eventStore.SubmitCommandAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string ProjectIdFromResources(IReadOnlyList<string> affectedResources)
    {
        string? project = affectedResources.FirstOrDefault(static value => value.StartsWith("project:", StringComparison.Ordinal));
        return string.IsNullOrWhiteSpace(project) ? "unavailable" : project["project:".Length..];
    }
}
