using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Maps a chained audit envelope to the NFR15a state-writing path it belongs to (Story 9.2, AC1). The path set is the
/// authoritative <see cref="ChatBotStateWritingPathInventory.Paths"/> — the eleven enumerated paths — consumed by
/// reference, never re-listed: this map only resolves <em>which</em> of those paths a given <see cref="AuditEnvelope"/>
/// realises, keyed off the safe <see cref="AuditEnvelope.CommandName"/> — the runtime command type the
/// <see cref="AuditEnvelopeFactory"/> stamps on every record. An envelope whose command name is absent or maps to none
/// of the inventory commands (including the system-emitted observability/alert records, which are not NFR15a
/// state-writing operations) resolves to <see langword="null"/> — a completeness gap, never silently dropped.
/// <para>
/// An envelope that maps to <b>no</b> known path is deliberately surfaced as <see langword="null"/> (a completeness
/// gap), never silently dropped — the measurer treats an unmapped state-mutating record as not-reconstructable rather
/// than excluding it from the denominator, so a path that stops emitting cannot hide as "100% complete".
/// </para>
/// </summary>
internal static class ChatBotAuditPathMap
{
    // Command-name → path-code. The command names are the runtime type names AuditEnvelopeFactory already stamps on
    // CommandName (e.g. nameof(CaptureMailboxMessageIntake)); the path codes are the inventory's stable codes. Grouped
    // by path so the eleven NFR15a paths are individually accounted for and a reviewer can audit the coverage.
    private static readonly IReadOnlyDictionary<string, string> CommandToPathCode = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // m365-mailbox-intake
        [nameof(CaptureMailboxMessageIntake)] = "m365-mailbox-intake",
        [nameof(RecordMailboxProviderConnection)] = "m365-mailbox-intake",

        // deterministic-association
        [nameof(AssociateEmailToProject)] = "deterministic-association",
        [nameof(ScoreMailboxMessageAssociation)] = "deterministic-association",

        // ambiguous-user-association
        [nameof(MarkEmailAssociationNeedsReview)] = "ambiguous-user-association",
        [nameof(DeferEmailProjectAssociation)] = "ambiguous-user-association",
        [nameof(RejectEmailProjectAssociation)] = "ambiguous-user-association",
        [nameof(ResolveMailboxMessageParticipants)] = "ambiguous-user-association",

        // correction
        [nameof(CorrectEmailProjectAssociation)] = "correction",
        [nameof(MarkAiActionProposalInvalidatedByCorrection)] = "correction",

        // ai-action-proposal
        [nameof(ProposeAIAction)] = "ai-action-proposal",
        [nameof(ExecuteLowRiskAIAssistance)] = "ai-action-proposal",
        [nameof(CaptureTaskIntent)] = "ai-action-proposal",
        [nameof(MarkTaskIntentDisposition)] = "ai-action-proposal",

        // approval-decision
        [nameof(DecideAiActionApproval)] = "approval-decision",
        [nameof(RequestOutboundSendApproval)] = "approval-decision",
        [nameof(DecideOutboundApproval)] = "approval-decision",

        // command-execution
        [nameof(ExecuteApprovedAIAction)] = "command-execution",
        [nameof(ExecuteAdminQueueOperation)] = "command-execution",
        [nameof(RequestFailedWorkflowRetry)] = "command-execution",
        [nameof(RecordGovernedNote)] = "command-execution",

        // outbound-draft-creation
        [nameof(CreateOutboundDraft)] = "outbound-draft-creation",

        // outbound-send
        [nameof(ExecuteApprovedOutboundDraft)] = "outbound-send",

        // tenant-policy-mutation
        [nameof(SubmitTenantPolicyChange)] = "tenant-policy-mutation",
        [nameof(ApproveTenantPolicyChange)] = "tenant-policy-mutation",
        [nameof(SubmitMailboxConfigurationChange)] = "tenant-policy-mutation",
        [nameof(SubmitNotificationRoutingChange)] = "tenant-policy-mutation",
        [nameof(SubmitEscalationPolicyChange)] = "tenant-policy-mutation",
        [nameof(SetAssociationConfidenceThresholds)] = "tenant-policy-mutation",

        // allowlist-mutation
        [nameof(AssignTenantAdminRole)] = "allowlist-mutation",
    };

    /// <summary>
    /// Resolves the inventory path a chained envelope realises, or <see langword="null"/> when the envelope maps to no
    /// known state-writing path (a completeness gap the measurer counts against the fraction). Pure and deterministic.
    /// </summary>
    public static ChatBotStateWritingPath? Resolve(AuditEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.CommandName is { Length: > 0 } commandName &&
            CommandToPathCode.TryGetValue(commandName, out string? pathCode))
        {
            return FindPath(pathCode);
        }

        return null;
    }

    private static ChatBotStateWritingPath? FindPath(string pathCode)
    {
        foreach (ChatBotStateWritingPath path in ChatBotStateWritingPathInventory.Paths)
        {
            if (string.Equals(path.Code, pathCode, StringComparison.Ordinal))
            {
                return path;
            }
        }

        return null;
    }
}
