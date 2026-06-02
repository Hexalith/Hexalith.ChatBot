using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Association.Participants;
using Hexalith.ChatBot.Server.Association.Scoring;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.Mailbox;
using Hexalith.ChatBot.Server.Governance.Outbound;
using Hexalith.ChatBot.Server.Governance.Policy;
using Hexalith.ChatBot.Server.Governance.ServiceClient;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// Event-sourced aggregate (Pattern A) for the Story 1.9 walking-skeleton governed note. The base
/// <see cref="EventStoreAggregate{TState}"/> is itself the <c>IDomainProcessor</c>: it reflection-discovers
/// the typed <see cref="Handle(RecordGovernedNote, GovernedOperationState?)"/> and the state's <c>Apply</c>
/// method. <see cref="Handle"/> is pure — no I/O, DAPR, authorization, or sibling calls — and never throws for
/// a business-rule violation (it returns a structured rejection so the idempotency cache is honored).
/// </summary>
public sealed class GovernedOperationAggregate : EventStoreAggregate<GovernedOperationState>
{
    /// <summary>
    /// Records a governed note. Fine-grained (aggregate-altitude) idempotency: recording a second note
    /// against an already-recorded aggregate yields a structured rejection rather than a duplicate event,
    /// so a repeated submission resolves to exactly one durable effect.
    /// </summary>
    /// <param name="command">The governed note command.</param>
    /// <param name="state">The replayed aggregate state, or <see langword="null"/> for a new aggregate.</param>
    /// <returns>A success result carrying the recorded-note event, or a structured rejection.</returns>
    public static DomainResult Handle(RecordGovernedNote command, GovernedOperationState? state)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (state is { IsRecorded: true })
        {
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                new GovernedNoteAlreadyRecordedRejection(command.NoteId),
            });
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new GovernedNoteRecorded(command.NoteId),
        });
    }

    public static DomainResult Handle(SubmitTenantPolicyChange command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidTenantPolicyChange(command))
        {
            return RejectTenantPolicyChange(command.PolicyChangeId, "invalid_tenant_policy_change", command.SourceVersion, command.CorrelationId);
        }

        if (state?.TenantPolicyPendingApprovals.ContainsKey(command.PolicyChangeId) == true ||
            state?.TenantPolicySnapshots.ContainsKey(command.ProposedPolicySnapshotId) == true)
        {
            return DomainResult.NoOp();
        }

        bool requiresApproval = command.ChangedKnobIds.Any(TenantPolicySchema.IsSensitive);
        long nextVersion = command.SourceVersion + 1;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (requiresApproval)
        {
            return DomainResult.Success(new IEventPayload[]
            {
                new TenantPolicyChangePendingApproval(
                    command.PolicyChangeId,
                    envelope.TenantId,
                    command.SourcePolicySnapshotId,
                    command.ProposedPolicySnapshotId,
                    command.ChangedKnobIds,
                    command.ChangeSet,
                    envelope.UserId,
                    command.RequesterRef,
                    command.ReasonCode,
                    command.OldValueFingerprint,
                    command.NewValueFingerprint,
                    now,
                    nextVersion,
                    command.CorrelationId),
            });
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new TenantPolicySnapshotActivated(
                command.PolicyChangeId,
                envelope.TenantId,
                command.SourcePolicySnapshotId,
                command.ProposedPolicySnapshotId,
                command.ChangedKnobIds,
                command.ChangeSet,
                TenantPolicyApprovalStatus.NotRequired,
                command.RequesterRef,
                null,
                command.ReasonCode,
                now,
                nextVersion,
                command.CorrelationId),
        });
    }

    public static DomainResult Handle(ApproveTenantPolicyChange command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidTenantPolicyApproval(command))
        {
            return RejectTenantPolicyChange(command.PolicyChangeId, "invalid_tenant_policy_approval", command.SourceVersion, command.CorrelationId);
        }

        if (state is null ||
            !state.TenantPolicyPendingApprovals.TryGetValue(command.PolicyChangeId, out TenantPolicyChangePendingApproval? pending))
        {
            return RejectTenantPolicyChange(command.PolicyChangeId, "tenant_policy_change_unavailable", command.SourceVersion, command.CorrelationId);
        }

        if (pending.SourceVersion != command.SourceVersion ||
            !string.Equals(pending.ProposedPolicySnapshotId, command.PendingPolicySnapshotId, StringComparison.Ordinal) ||
            !EquivalentMetadata(pending.ChangedKnobIds, command.ChangedKnobIds) ||
            string.Equals(pending.RequesterRef, command.ApproverRef, StringComparison.Ordinal) ||
            string.Equals(pending.RequesterActorId, envelope.UserId, StringComparison.Ordinal))
        {
            return RejectTenantPolicyChange(command.PolicyChangeId, "tenant_policy_approval_scope_invalid", pending.SourceVersion, command.CorrelationId);
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new TenantPolicySnapshotActivated(
                command.PolicyChangeId,
                envelope.TenantId,
                pending.SourcePolicySnapshotId,
                command.ActivatedPolicySnapshotId,
                command.ChangedKnobIds,
                pending.ChangeSet,
                TenantPolicyApprovalStatus.Approved,
                pending.RequesterRef,
                command.ApproverRef,
                command.ReasonCode,
                DateTimeOffset.UtcNow,
                pending.SourceVersion + 1,
                command.CorrelationId),
        });
    }

    public static DomainResult Handle(SubmitMailboxSourceDisable command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidMailboxSourceDisable(command))
        {
            return RejectMailboxSourceDisable(command.DisableChangeId, "invalid_mailbox_source_disable", command.SourceVersion, command.CorrelationId);
        }

        if (state?.MailboxSourceDisablePendingApprovals.ContainsKey(command.DisableChangeId) == true ||
            state?.DisabledMailboxSources.ContainsKey(command.MailboxSourceRef) == true)
        {
            return DomainResult.NoOp();
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxSourceDisablePendingApproval(
                command.DisableChangeId,
                envelope.TenantId,
                command.MailboxSourceRef,
                envelope.UserId,
                command.RequesterRef,
                command.ReasonCode,
                command.PolicySnapshotId,
                MailboxSourceControlState.Active,
                MailboxSourceControlState.Disabled,
                DateTimeOffset.UtcNow,
                command.SourceVersion + 1,
                command.CorrelationId),
        });
    }

    public static DomainResult Handle(ApproveMailboxSourceDisable command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidMailboxSourceDisableApproval(command))
        {
            return RejectMailboxSourceDisable(command.DisableChangeId, "invalid_mailbox_source_disable_approval", command.SourceVersion, command.CorrelationId);
        }

        if (state is null ||
            !state.MailboxSourceDisablePendingApprovals.TryGetValue(command.DisableChangeId, out MailboxSourceDisablePendingApproval? pending))
        {
            return RejectMailboxSourceDisable(command.DisableChangeId, "mailbox_source_disable_unavailable", command.SourceVersion, command.CorrelationId);
        }

        if (pending.SourceVersion != command.SourceVersion ||
            !string.Equals(pending.MailboxSourceRef, command.MailboxSourceRef, StringComparison.Ordinal) ||
            !string.Equals(pending.ReasonCode, command.ReasonCode, StringComparison.Ordinal) ||
            string.Equals(pending.RequesterRef, command.ApproverRef, StringComparison.Ordinal) ||
            string.Equals(pending.RequesterActorId, envelope.UserId, StringComparison.Ordinal))
        {
            return RejectMailboxSourceDisable(command.DisableChangeId, "mailbox_source_disable_approval_scope_invalid", pending.SourceVersion, command.CorrelationId);
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxSourceDisabled(
                command.DisableChangeId,
                envelope.TenantId,
                pending.MailboxSourceRef,
                pending.RequesterRef,
                command.ApproverRef,
                command.ReasonCode,
                command.PolicySnapshotId,
                MailboxSourceControlState.Active,
                MailboxSourceControlState.Disabled,
                DateTimeOffset.UtcNow,
                pending.SourceVersion + 1,
                command.CorrelationId),
        });
    }

    public static DomainResult Handle(SubmitServiceClientDisable command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidServiceClientDisable(command))
        {
            return RejectServiceClientDisable(command.DisableChangeId, "invalid_service_client_disable", command.SourceVersion, command.CorrelationId);
        }

        if (state?.ServiceClientDisablePendingApprovals.ContainsKey(command.DisableChangeId) == true ||
            state?.DisabledServiceClients.ContainsKey(command.ServiceClientRef) == true)
        {
            return DomainResult.NoOp();
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new ServiceClientDisablePendingApproval(
                command.DisableChangeId,
                envelope.TenantId,
                command.ServiceClientRef,
                envelope.UserId,
                command.RequesterRef,
                command.ReasonCode,
                command.PolicySnapshotId,
                ServiceClientControlState.Active,
                ServiceClientControlState.Disabled,
                DateTimeOffset.UtcNow,
                command.SourceVersion + 1,
                command.CorrelationId),
        });
    }

    public static DomainResult Handle(ApproveServiceClientDisable command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidServiceClientDisableApproval(command))
        {
            return RejectServiceClientDisable(command.DisableChangeId, "invalid_service_client_disable_approval", command.SourceVersion, command.CorrelationId);
        }

        if (state is null ||
            !state.ServiceClientDisablePendingApprovals.TryGetValue(command.DisableChangeId, out ServiceClientDisablePendingApproval? pending))
        {
            return RejectServiceClientDisable(command.DisableChangeId, "service_client_disable_unavailable", command.SourceVersion, command.CorrelationId);
        }

        if (pending.SourceVersion != command.SourceVersion ||
            !string.Equals(pending.ServiceClientRef, command.ServiceClientRef, StringComparison.Ordinal) ||
            !string.Equals(pending.ReasonCode, command.ReasonCode, StringComparison.Ordinal) ||
            string.Equals(pending.RequesterRef, command.ApproverRef, StringComparison.Ordinal) ||
            string.Equals(pending.RequesterActorId, envelope.UserId, StringComparison.Ordinal))
        {
            return RejectServiceClientDisable(command.DisableChangeId, "service_client_disable_approval_scope_invalid", pending.SourceVersion, command.CorrelationId);
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new ServiceClientDisabled(
                command.DisableChangeId,
                envelope.TenantId,
                pending.ServiceClientRef,
                pending.RequesterRef,
                command.ApproverRef,
                command.ReasonCode,
                command.PolicySnapshotId,
                ServiceClientControlState.Active,
                ServiceClientControlState.Disabled,
                DateTimeOffset.UtcNow,
                pending.SourceVersion + 1,
                command.CorrelationId),
        });
    }

    public static DomainResult Handle(SubmitServiceClientQuarantine command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidServiceClientQuarantine(command))
        {
            return RejectServiceClientQuarantine(command.QuarantineChangeId, "invalid_service_client_quarantine", command.SourceVersion, command.CorrelationId);
        }

        if (state?.ServiceClientQuarantinePendingApprovals.ContainsKey(command.QuarantineChangeId) == true ||
            state?.QuarantinedServiceClients.ContainsKey(command.ServiceClientRef) == true)
        {
            return DomainResult.NoOp();
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new ServiceClientQuarantinePendingApproval(
                command.QuarantineChangeId,
                envelope.TenantId,
                command.ServiceClientRef,
                envelope.UserId,
                command.RequesterRef,
                command.ReasonCode,
                command.PolicySnapshotId,
                ServiceClientControlState.Active,
                ServiceClientControlState.Quarantined,
                DateTimeOffset.UtcNow,
                command.SourceVersion + 1,
                command.CorrelationId),
        });
    }

    public static DomainResult Handle(ApproveServiceClientQuarantine command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidServiceClientQuarantineApproval(command))
        {
            return RejectServiceClientQuarantine(command.QuarantineChangeId, "invalid_service_client_quarantine_approval", command.SourceVersion, command.CorrelationId);
        }

        if (state is null ||
            !state.ServiceClientQuarantinePendingApprovals.TryGetValue(command.QuarantineChangeId, out ServiceClientQuarantinePendingApproval? pending))
        {
            return RejectServiceClientQuarantine(command.QuarantineChangeId, "service_client_quarantine_unavailable", command.SourceVersion, command.CorrelationId);
        }

        if (pending.SourceVersion != command.SourceVersion ||
            !string.Equals(pending.ServiceClientRef, command.ServiceClientRef, StringComparison.Ordinal) ||
            !string.Equals(pending.ReasonCode, command.ReasonCode, StringComparison.Ordinal) ||
            string.Equals(pending.RequesterRef, command.ApproverRef, StringComparison.Ordinal) ||
            string.Equals(pending.RequesterActorId, envelope.UserId, StringComparison.Ordinal))
        {
            return RejectServiceClientQuarantine(command.QuarantineChangeId, "service_client_quarantine_approval_scope_invalid", pending.SourceVersion, command.CorrelationId);
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new ServiceClientQuarantined(
                command.QuarantineChangeId,
                envelope.TenantId,
                pending.ServiceClientRef,
                pending.RequesterRef,
                command.ApproverRef,
                command.ReasonCode,
                command.PolicySnapshotId,
                ServiceClientControlState.Active,
                ServiceClientControlState.Quarantined,
                DateTimeOffset.UtcNow,
                pending.SourceVersion + 1,
                command.CorrelationId),
        });
    }

    public static DomainResult Handle(SubmitMailboxSourceQuarantine command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidMailboxSourceQuarantine(command))
        {
            return RejectMailboxSourceQuarantine(command.QuarantineChangeId, "invalid_mailbox_source_quarantine", command.SourceVersion, command.CorrelationId);
        }

        if (state?.MailboxSourceQuarantinePendingApprovals.ContainsKey(command.QuarantineChangeId) == true ||
            state?.QuarantinedMailboxSources.ContainsKey(command.MailboxSourceRef) == true)
        {
            return DomainResult.NoOp();
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxSourceQuarantinePendingApproval(
                command.QuarantineChangeId,
                envelope.TenantId,
                command.MailboxSourceRef,
                envelope.UserId,
                command.RequesterRef,
                command.ReasonCode,
                command.PolicySnapshotId,
                MailboxSourceControlState.Active,
                MailboxSourceControlState.Quarantined,
                DateTimeOffset.UtcNow,
                command.SourceVersion + 1,
                command.CorrelationId),
        });
    }

    public static DomainResult Handle(ApproveMailboxSourceQuarantine command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidMailboxSourceQuarantineApproval(command))
        {
            return RejectMailboxSourceQuarantine(command.QuarantineChangeId, "invalid_mailbox_source_quarantine_approval", command.SourceVersion, command.CorrelationId);
        }

        if (state is null ||
            !state.MailboxSourceQuarantinePendingApprovals.TryGetValue(command.QuarantineChangeId, out MailboxSourceQuarantinePendingApproval? pending))
        {
            return RejectMailboxSourceQuarantine(command.QuarantineChangeId, "mailbox_source_quarantine_unavailable", command.SourceVersion, command.CorrelationId);
        }

        if (pending.SourceVersion != command.SourceVersion ||
            !string.Equals(pending.MailboxSourceRef, command.MailboxSourceRef, StringComparison.Ordinal) ||
            !string.Equals(pending.ReasonCode, command.ReasonCode, StringComparison.Ordinal) ||
            string.Equals(pending.RequesterRef, command.ApproverRef, StringComparison.Ordinal) ||
            string.Equals(pending.RequesterActorId, envelope.UserId, StringComparison.Ordinal))
        {
            return RejectMailboxSourceQuarantine(command.QuarantineChangeId, "mailbox_source_quarantine_approval_scope_invalid", pending.SourceVersion, command.CorrelationId);
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxSourceQuarantined(
                command.QuarantineChangeId,
                envelope.TenantId,
                pending.MailboxSourceRef,
                pending.RequesterRef,
                command.ApproverRef,
                command.ReasonCode,
                command.PolicySnapshotId,
                MailboxSourceControlState.Active,
                MailboxSourceControlState.Quarantined,
                DateTimeOffset.UtcNow,
                pending.SourceVersion + 1,
                command.CorrelationId),
        });
    }

    public static DomainResult Handle(SubmitMailboxSourceRateLimit command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidMailboxSourceRateLimit(command))
        {
            return RejectMailboxSourceRateLimit(command.RateLimitChangeId, "invalid_mailbox_source_rate_limit", command.SourceVersion, command.CorrelationId);
        }

        // Defense-in-depth: the gateway already bounds the budget, but reject out-of-bounds here too — never raise the cap.
        if (!new MailboxRateLimitBounds(command.NewBudget).IsWithinBounds)
        {
            return RejectMailboxSourceRateLimit(command.RateLimitChangeId, "mailbox_source_rate_limit_out_of_bounds", command.SourceVersion, command.CorrelationId);
        }

        // Idempotency: a re-submit that yields the identical budget for the source is a NoOp (single durable effect).
        if (state?.MailboxSourceRateLimits.TryGetValue(command.MailboxSourceRef, out MailboxSourceRateLimitConfigured? existing) == true &&
            existing.NewBudget == command.NewBudget &&
            existing.Window == command.Window)
        {
            return DomainResult.NoOp();
        }

        // Single-actor direct activation (mirror the non-sensitive tenant-policy knob path) — no pending-approval event.
        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxSourceRateLimitConfigured(
                command.RateLimitChangeId,
                envelope.TenantId,
                command.MailboxSourceRef,
                envelope.UserId,
                command.RequesterRef,
                command.ReasonCode,
                command.PolicySnapshotId,
                command.OldBudget,
                command.NewBudget,
                command.Window,
                DateTimeOffset.UtcNow,
                command.SourceVersion + 1,
                command.CorrelationId),
        });
    }

    public static DomainResult Handle(SubmitServiceClientRateLimit command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidServiceClientRateLimit(command))
        {
            return RejectServiceClientRateLimit(command.RateLimitChangeId, "invalid_service_client_rate_limit", command.SourceVersion, command.CorrelationId);
        }

        // Defense-in-depth: the gateway already bounds the budget, but reject out-of-bounds here too — never raise the cap.
        if (!new ServiceClientRateLimitBounds(command.NewBudget).IsWithinBounds)
        {
            return RejectServiceClientRateLimit(command.RateLimitChangeId, "service_client_rate_limit_out_of_bounds", command.SourceVersion, command.CorrelationId);
        }

        // Idempotency: a re-submit that yields the identical budget for the client is a NoOp (single durable effect).
        if (state?.ServiceClientRateLimits.TryGetValue(command.ServiceClientRef, out ServiceClientRateLimitConfigured? existing) == true &&
            existing.NewBudget == command.NewBudget &&
            existing.Window == command.Window)
        {
            return DomainResult.NoOp();
        }

        // Single-actor direct activation (mirror the non-sensitive tenant-policy knob path) — no pending-approval event.
        return DomainResult.Success(new IEventPayload[]
        {
            new ServiceClientRateLimitConfigured(
                command.RateLimitChangeId,
                envelope.TenantId,
                command.ServiceClientRef,
                envelope.UserId,
                command.RequesterRef,
                command.ReasonCode,
                command.PolicySnapshotId,
                command.OldBudget,
                command.NewBudget,
                command.Window,
                DateTimeOffset.UtcNow,
                command.SourceVersion + 1,
                command.CorrelationId),
        });
    }

    public static DomainResult Handle(CreateOutboundDraft command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidOutboundDraft(command))
        {
            return RejectOutboundDraft(command, ChatBotDisabledActionReasons.PolicyBlocked);
        }

        if (state?.OutboundDrafts.TryGetValue(command.DraftId, out OutboundDraftCreated? existing) == true)
        {
            return IsEquivalentOutboundDraft(command, existing)
                ? DomainResult.NoOp()
                : RejectOutboundDraft(command, "idempotency_conflict_outbound_draft_creation");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new OutboundDraftCreated(
                command.DraftId,
                command.ProjectId,
                command.RequesterId,
                command.SourceActorId,
                command.SourceConversationId,
                command.SourceMessageId,
                command.SourceConversationItemId,
                command.RecipientRefs,
                command.ContextRefs,
                command.PolicySnapshotId,
                command.CorrelationId,
                SenderAuthorityClass.DraftOnly,
                command.GovernedContent,
                DateTimeOffset.UtcNow,
                command.RedactionState,
                command.RetentionClass),
        });
    }

    public static DomainResult Handle(RequestOutboundSendApproval command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidOutboundApprovalRequest(command))
        {
            return RejectOutboundApprovalRequest(command, ChatBotDisabledActionReasons.PolicyBlocked);
        }

        if (state is null ||
            !state.OutboundDrafts.TryGetValue(command.DraftId, out OutboundDraftCreated? draft) ||
            !string.Equals(draft.ProjectId, command.ProjectId, StringComparison.Ordinal) ||
            !string.Equals(draft.RequesterId, command.RequesterId, StringComparison.Ordinal))
        {
            return RejectOutboundApprovalRequest(command, "outbound_draft_unavailable");
        }

        if (state.OutboundApprovalRequests.TryGetValue(command.ApprovalId, out OutboundApprovalRequested? existing))
        {
            return IsEquivalentOutboundApprovalRequest(command, existing)
                ? DomainResult.NoOp()
                : RejectOutboundApprovalRequest(command, "idempotency_conflict_outbound_approval_request");
        }

        long sourceVersion = command.ExpectedDraftSourceVersion + 1;
        return DomainResult.Success(new IEventPayload[]
        {
            new OutboundApprovalRequested(
                command.ApprovalId,
                command.DraftId,
                command.ProjectId,
                command.RequesterId,
                ActorType(envelope, "human"),
                command.SourceConversationId,
                command.SourceMessageId,
                command.SourceConversationItemId,
                command.RecipientRefs,
                command.ContextRefs,
                command.PolicySnapshotId,
                command.PolicySnapshotVisibility,
                command.CommandName,
                command.CommandAllowlistVersion,
                command.ContentSnapshot,
                command.SenderAuthorityClass,
                command.EvidenceFreshness,
                command.ExpectedPostStateRedactionState,
                command.ExpectedDraftSourceVersion,
                sourceVersion,
                DateTimeOffset.UtcNow,
                command.CorrelationId,
                command.RedactionState,
                command.RetentionClass,
                AuthorityResult: command.AuthorityResult),
        });
    }

    public static DomainResult Handle(DecideOutboundApproval command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidOutboundApprovalDecision(command))
        {
            return RejectOutboundApprovalDecision(command, "invalid_outbound_approval_decision_payload", null);
        }

        if (state is null ||
            !state.OutboundApprovalRequests.TryGetValue(command.ApprovalId, out OutboundApprovalRequested? request) ||
            !string.Equals(request.DraftId, command.DraftId, StringComparison.Ordinal) ||
            !string.Equals(request.ProjectId, command.ProjectId, StringComparison.Ordinal))
        {
            return RejectOutboundApprovalDecision(command, "outbound_approval_request_unavailable", command.ExpectedApprovalSourceVersion);
        }

        if (request.SourceVersion != command.ExpectedApprovalSourceVersion)
        {
            return RejectOutboundApprovalDecision(command, "outbound_approval_source_version_mismatch", request.SourceVersion);
        }

        if (state.OutboundApprovalDecisions.TryGetValue(command.ApprovalId, out OutboundApprovalDecisionRecorded? existing))
        {
            return existing.DecisionKind == command.Decision && string.Equals(existing.DecisionActorId, envelope.UserId, StringComparison.Ordinal)
                ? DomainResult.NoOp()
                : RejectOutboundApprovalDecision(command, "outbound_approval_already_decided", request.SourceVersion);
        }

        if (command.Decision is ApprovalDecisionKind.Approve &&
            request.EvidenceFreshness is ApprovalEvidenceFreshness.Expired)
        {
            return RejectOutboundApprovalDecision(command, ChatBotRefusalReasonCodes.EvidenceExpired, request.SourceVersion);
        }

        OutboundApprovalContentSnapshot contentSnapshot = command.Decision is ApprovalDecisionKind.Approve
            ? request.ContentSnapshot with
            {
                ApprovedContent = command.ApprovedContent ?? request.ContentSnapshot.ProposedContent,
                ApprovedContentRedactionState = "governed_content",
            }
            : request.ContentSnapshot;
        long decisionSourceVersion = request.SourceVersion + 1;
        string safeNextAction = command.Decision switch
        {
            ApprovalDecisionKind.Approve => "send-approved-outbound-draft",
            ApprovalDecisionKind.RequestRevision => "revise-outbound-draft",
            _ => "none",
        };

        return DomainResult.Success(new IEventPayload[]
        {
            new OutboundApprovalDecisionRecorded(
                command.ApprovalId,
                command.DraftId,
                command.ProjectId,
                command.Decision,
                envelope.UserId,
                ActorType(envelope, "human"),
                DateTimeOffset.UtcNow,
                command.ExpectedApprovalSourceVersion,
                "authorized",
                null,
                command.DecisionRationaleRedactionState,
                $"audit:{command.DecisionId}",
                "available",
                request.PolicySnapshotId,
                safeNextAction,
                contentSnapshot,
                decisionSourceVersion,
                command.CorrelationId),
        });
    }

    public static DomainResult Handle(ExecuteApprovedOutboundDraft command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidOutboundSend(command))
        {
            return RejectOutboundSend(command, "invalid_outbound_send_payload", null, null);
        }

        string sendKey = OutboundSendKey(envelope.TenantId, command.DraftId, command.SendActorId);
        if (state?.OutboundSends.ContainsKey(sendKey) == true)
        {
            return RejectOutboundSend(
                command,
                "idempotency_conflict_outbound_send",
                command.ExpectedApprovalSourceVersion,
                command.ExpectedDraftSourceVersion);
        }

        if (state is null ||
            !state.OutboundDrafts.ContainsKey(command.DraftId) ||
            !state.OutboundApprovalRequests.TryGetValue(command.ApprovalId, out OutboundApprovalRequested? request) ||
            !state.OutboundApprovalDecisions.TryGetValue(command.ApprovalId, out OutboundApprovalDecisionRecorded? decision))
        {
            return RejectOutboundSend(command, ChatBotRefusalReasonCodes.ApprovalStateInvalid, command.ExpectedApprovalSourceVersion, command.ExpectedDraftSourceVersion);
        }

        string? approvalRejection = ValidateOutboundSendApproval(command, request, decision);
        if (approvalRejection is not null)
        {
            return RejectOutboundSend(command, approvalRejection, decision.SourceVersion, request.ExpectedDraftSourceVersion);
        }

        if (command.AuthorityResult is null ||
            command.AuthorityResult.DenialReason is not null ||
            !string.Equals(command.AuthorityResult.ApprovalRef, $"approval:{command.ApprovalId}", StringComparison.Ordinal) ||
            command.EvidenceFreshness is ApprovalEvidenceFreshness.Expired)
        {
            return RejectOutboundSend(command, command.AuthorityResult?.DenialReason ?? ChatBotDisabledActionReasons.PolicyBlocked, decision.SourceVersion, request.ExpectedDraftSourceVersion);
        }

        if (!string.Equals(command.AdapterMode, "approved", StringComparison.Ordinal))
        {
            return RejectOutboundSend(command, "adapter_not_approved_mode", decision.SourceVersion, request.ExpectedDraftSourceVersion);
        }

        if (!string.Equals(command.AdapterStatus, "sent", StringComparison.Ordinal))
        {
            return RejectOutboundSend(command, "outbound_adapter_unavailable", decision.SourceVersion, request.ExpectedDraftSourceVersion);
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        return DomainResult.Success(new IEventPayload[]
        {
            new OutboundSendStarted(
                command.SendId,
                sendKey,
                command.ApprovalId,
                command.DraftId,
                command.ProjectId,
                command.RequesterId,
                command.SendActorId,
                command.SenderAuthorityClass,
                command.AuthorityResult,
                command.AdapterMode,
                command.AdapterRef,
                command.ExpectedApprovalSourceVersion,
                command.ExpectedDraftSourceVersion,
                now,
                command.CorrelationId,
                command.RedactionState,
                command.RetentionClass),
            new OutboundSendSucceeded(
                command.SendId,
                sendKey,
                command.ApprovalId,
                command.DraftId,
                command.ProjectId,
                command.AdapterRef,
                now,
                $"audit:{command.SendId}",
                "available",
                command.CorrelationId,
                command.RedactionState,
                command.RetentionClass),
            new OutboundApprovalOutcomeRecorded(
                command.ApprovalId,
                command.DraftId,
                command.ProjectId,
                ApprovalStatus.Executed,
                "sent",
                now,
                $"audit:{command.SendId}",
                "available",
                null,
                null,
                command.ExpectedApprovalSourceVersion + 1,
                command.CorrelationId,
                command.RedactionState,
                command.RetentionClass),
        });
    }

    public static DomainResult Handle(CaptureMailboxMessageIntake command, GovernedOperationState? state)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!MailboxMessageIntakeId.TryParse(command.IntakeId, out _))
        {
            return Invalid(command.IntakeId, "invalid_intake_id");
        }

        if (state is { IsMailboxIntakeCaptured: true })
        {
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                new MailboxMessageIntakeAlreadyCapturedRejection(command.IntakeId),
            });
        }

        if (command.Source is null ||
            command.Recipients is null ||
            command.Attachments is null ||
            command.Source.Sender is null ||
            string.IsNullOrWhiteSpace(command.Source.ProviderMessageId) ||
            string.IsNullOrWhiteSpace(command.Source.MailboxId) ||
            string.IsNullOrWhiteSpace(command.Source.InternetMessageId) ||
            string.IsNullOrWhiteSpace(command.Source.ConversationId) ||
            string.IsNullOrWhiteSpace(command.Source.SourceContext) ||
            string.IsNullOrWhiteSpace(command.Source.Sender.Address) ||
            command.Source.SourceSchemaVersion <= 0 ||
            command.Recipients.Count == 0 ||
            command.Recipients.Any(static recipient => string.IsNullOrWhiteSpace(recipient.Address) || string.IsNullOrWhiteSpace(recipient.Kind)) ||
            command.Attachments.Any(static attachment => string.IsNullOrWhiteSpace(attachment.ProviderAttachmentId)) ||
            !IsValidAuthenticity(command.Authenticity) ||
            !IsValidDelegatedSender(command.Source.DelegatedSender) ||
            !IsValidExternalSender(command.Source.ExternalSender))
        {
            return Invalid(command.IntakeId, "missing_source_identity");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxMessageIntakeCaptured(
                command.IntakeId,
                command.Source.ProviderMessageId,
                command.Source.InternetMessageId,
                command.Source.ConversationId,
                command.Source.ThreadId,
                command.Source.MailboxId,
                command.Source.Sender,
                command.Recipients,
                command.Source.ReceivedAt.ToUniversalTime(),
                command.Source.SentAt?.ToUniversalTime(),
                command.Source.CreatedAt?.ToUniversalTime(),
                command.Attachments,
                command.Source.SourceTimezone,
                command.Source.SourceContext,
                "m365-mailbox-intake",
                "mailbox-intake.kernel.v1",
                "metadata_only",
            "collaboration_input",
            command.Source.SourceSchemaVersion,
            command.Authenticity,
            command.Source.DelegatedSender,
            command.Source.ExternalSender),
        });
    }

    public static DomainResult Handle(RequestFailedWorkflowRetry command, GovernedOperationState? state)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!ChatBotIdentity.IsValidUlid(command.RetryId) ||
            !ChatBotIdentity.IsValidUlid(command.FailedEventId) ||
            string.IsNullOrWhiteSpace(command.FailedOperationClass) ||
            string.IsNullOrWhiteSpace(command.FailureReasonCode) ||
            command.ExpectedFailedSourceVersion <= 0)
        {
            return InvalidRetry(command.RetryId, "invalid_workflow_retry_payload");
        }

        if (state?.WorkflowRetryIds.Contains(command.RetryId) == true)
        {
            return InvalidRetry(command.RetryId, "workflow_retry_already_recorded");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new WorkflowRetryRequested(
                command.RetryId,
                command.FailedEventId,
                command.FailedOperationClass,
                command.FailureReasonCode,
                command.ExpectedFailedSourceVersion,
                command.Rationale),
        });
    }

    public static DomainResult Handle(CaptureTaskIntent command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (string.IsNullOrWhiteSpace(envelope.TenantId) ||
            string.IsNullOrWhiteSpace(command.ProjectId) ||
            string.IsNullOrWhiteSpace(command.SourceMessageId) ||
            string.IsNullOrWhiteSpace(command.RequesterPartyId) ||
            string.IsNullOrWhiteSpace(command.DetectedIntentSummary) ||
            command.DetectedIntentSummary.Length > DeterministicTaskIntentKernel.SummaryMaxLength ||
            command.SourceEvidenceOffsets is not { Count: > 0 } ||
            command.SourceEvidenceOffsets.Any(static evidence => string.IsNullOrWhiteSpace(evidence.EvidenceReference)) ||
            string.IsNullOrWhiteSpace(command.KernelVersion) ||
            double.IsNaN(command.ConfidenceScore) ||
            double.IsInfinity(command.ConfidenceScore) ||
            command.ConfidenceScore is < 0 or > 1 ||
            command.DetectedAt == default ||
            string.IsNullOrWhiteSpace(command.RedactionState) ||
            string.IsNullOrWhiteSpace(command.RetentionClass) ||
            command.SourceVersion <= 0 ||
            string.IsNullOrWhiteSpace(command.CorrelationId) ||
            string.IsNullOrWhiteSpace(command.SchemaVersion))
        {
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                new TaskIntentCaptureRejected(command.SourceMessageId, "invalid_task_intent_payload"),
            });
        }

        if (!command.CorrectedContextReady)
        {
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                new TaskIntentCaptureRejected(command.SourceMessageId, TaskIntentReasonCodes.StaleCorrectedContext),
            });
        }

        string taskIntentId = TaskIntentIdempotency.ComposeKey(
            envelope.TenantId,
            command.ProjectId,
            command.SourceMessageId,
            command.RequesterPartyId,
            command.KernelVersion,
            command.DetectedActionKind,
            command.SourceEvidenceOffsets);
        if (state?.TaskIntentIds.Contains(taskIntentId) == true)
        {
            return DomainResult.NoOp();
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new TaskIntentCaptured(new(
                taskIntentId,
                envelope.TenantId,
                command.ProjectId,
                command.SourceMessageId,
                command.RequesterPartyId,
                command.DetectedIntentSummary,
                command.DetectedActionKind,
                command.SourceEvidenceOffsets,
                command.KernelVersion,
                command.ConfidenceScore,
                command.DetectedAt.ToUniversalTime(),
                TaskIntentState.Captured,
                command.SchemaVersion,
                TaskIntentReasonCodes.Captured,
                "authorized-project-conversation",
                command.RedactionState,
                command.RetentionClass,
                command.SourceVersion,
                command.CorrelationId,
                command.PolicySnapshotId,
                ConversionReadinessBlocked: false,
                SafeNextAction: "review-task-intent")),
        });
    }

    public static DomainResult Handle(ProposeAIAction command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidTransitionIdentity(command.ProjectId, command.TaskIntentId, command.SourceMessageId, command.TransitionId, command.CorrelationId) ||
            string.IsNullOrWhiteSpace(command.RequesterId) ||
            string.IsNullOrWhiteSpace(command.IntendedCommandName) ||
            string.IsNullOrWhiteSpace(command.ActionKind) ||
            string.IsNullOrWhiteSpace(command.SchemaVersion) ||
            string.IsNullOrWhiteSpace(command.RedactionState) ||
            string.IsNullOrWhiteSpace(command.RetentionClass) ||
            command.ExpectedSourceVersion <= 0 ||
            command.EvidenceReferences is not { Count: > 0 } ||
            !AllSafeMetadataTokens(command.EvidenceReferences) ||
            command.AffectedResourceReferences is null ||
            !AllSafeMetadataTokens(command.AffectedResourceReferences) ||
            command.RecipientReferences is null ||
            !AllSafeMetadataTokens(command.RecipientReferences) ||
            !IsSafeOptionalMetadataToken(command.PolicySnapshotId) ||
            !IsSafeOptionalMetadataToken(command.SourceConversationItemId) ||
            !IsSafeOptionalMetadataMap(command.ProposalInputMetadata) ||
            !IsSafeClassification(command.RiskClassification, command))
        {
            return RejectTransition(command.TaskIntentId, command.ProjectId, command.TransitionId, TaskIntentReasonCodes.InvalidMetadata, null, command.CorrelationId);
        }

        if (state?.TaskIntentTransitionIds.TryGetValue(command.TransitionId, out string? existingTaskIntentId) == true)
        {
            return string.Equals(existingTaskIntentId, command.TaskIntentId, StringComparison.Ordinal)
                ? DomainResult.NoOp()
                : RejectTransition(command.TaskIntentId, command.ProjectId, command.TransitionId, TaskIntentReasonCodes.IdempotencyConflict, command.ExpectedSourceVersion, command.CorrelationId);
        }

        if (state is null || !state.TaskIntents.TryGetValue(command.TaskIntentId, out TaskIntentRecord? record))
        {
            return RejectTransition(command.TaskIntentId, command.ProjectId, command.TransitionId, TaskIntentReasonCodes.MissingCapturedIntent, command.ExpectedSourceVersion, command.CorrelationId);
        }

        string? rejection = ValidateCapturedRecord(envelope.TenantId, command.ProjectId, command.SourceMessageId, command.ExpectedSourceVersion, record);
        if (rejection is not null)
        {
            return RejectTransition(command.TaskIntentId, command.ProjectId, command.TransitionId, rejection, record.SourceVersion, command.CorrelationId);
        }

        if (!string.Equals(command.RequesterId, record.RequesterPartyId, StringComparison.Ordinal))
        {
            return RejectTransition(command.TaskIntentId, command.ProjectId, command.TransitionId, TaskIntentReasonCodes.InvalidMetadata, record.SourceVersion, command.CorrelationId);
        }

        DateTimeOffset decidedAt = DateTimeOffset.UtcNow;
        string proposalId = $"ai-proposal:{command.TaskIntentId}:{command.TransitionId}";
        string auditOperationId = $"audit:{command.TransitionId}";
        TaskIntentRecord transitioned = record with
        {
            State = TaskIntentState.Converted,
            ReasonCode = TaskIntentReasonCodes.Converted,
            SafeNextAction = "review-ai-action",
            ConvertedProposalId = proposalId,
            ReviewerActorId = envelope.UserId,
            DecidedAtUtc = decidedAt,
            AuditOperationId = auditOperationId,
            TransitionId = command.TransitionId,
            PolicySnapshotId = command.PolicySnapshotId ?? record.PolicySnapshotId,
            CorrelationId = command.CorrelationId,
        };
        AiActionProposalRecord proposal = new(
            proposalId,
            command.TaskIntentId,
            command.SourceMessageId,
            command.SourceConversationItemId,
            command.RequesterId,
            envelope.UserId,
            command.EvidenceReferences,
            command.IntendedCommandName,
            command.ActionKind,
            command.AffectedResourceReferences,
            command.RecipientReferences,
            command.PolicySnapshotId ?? record.PolicySnapshotId,
            command.ExpectedSourceVersion,
            command.CorrelationId,
            command.RedactionState,
            command.RetentionClass,
            command.SchemaVersion,
            "review-ai-action",
            command.ProposalInputMetadata,
            command.RiskClassification!.RiskClass,
            command.RiskClassification.RiskActionClasses,
            command.RiskClassification.ClassifierVersion,
            command.RiskClassification.InputTuple,
            command.RiskClassification.ReasonCode,
            command.RiskClassification.CommandAllowlistVersion,
            command.RiskClassification.CommandDefaultRisk,
            command.RiskClassification.RequesterAuthorityClass,
            command.RiskClassification.ProducedAtUtc,
            command.RiskClassification.IndeterminateReason,
            record.CorrectionLineageId,
            MetadataValue(command.ProposalInputMetadata, "associationId"),
            MetadataLong(command.ProposalInputMetadata, "evidenceSnapshotSourceVersion"),
            MetadataValue(command.ProposalInputMetadata, "contextPackageId"),
            MetadataValue(command.ProposalInputMetadata, "contextPackageVersion"));

        List<IEventPayload> events =
        [
            new TaskIntentConvertedToAiActionProposal(transitioned, proposal, envelope.UserId, decidedAt, auditOperationId),
        ];
        if (RequiresApproval(proposal))
        {
            events.Add(ApprovalRequestedFromProposal(proposal, command.TaskIntentId, ActorType(envelope, "human"), decidedAt));
        }

        return DomainResult.Success(events);
    }

    public static DomainResult Handle(MarkAiActionProposalInvalidatedByCorrection command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsSafeMetadataToken(command.ProjectId) ||
            !IsSafeMetadataToken(command.ProposalId) ||
            !IsSafeOptionalMetadataToken(command.ApprovalId) ||
            !IsSafeMetadataToken(command.TaskIntentId) ||
            !IsSafeMetadataToken(command.SourceMessageId) ||
            !IsSafeOptionalMetadataToken(command.SourceConversationItemId) ||
            !IsSafeMetadataToken(command.RequesterId) ||
            !AssociationWorkflowId.TryParse(command.AssociationId, out _) ||
            !IsSafeMetadataToken(command.CorrectionId) ||
            !IsSafeMetadataToken(command.CorrectedEvidenceState) ||
            command.EvidenceSnapshotSourceVersion <= 0 ||
            !IsSafeMetadataToken(command.CorrelationId) ||
            !IsMetadataOnly(command.RedactionState, command.RetentionClass) ||
            !IsSafeMetadataToken(command.SchemaVersion))
        {
            return RejectProposalInvalidation(command, TaskIntentReasonCodes.InvalidMetadata, null);
        }

        if (state is null ||
            !state.AiActionProposals.TryGetValue(command.ProposalId, out AiActionProposalRecord? proposal) ||
            !string.Equals(proposal.TaskIntentId, command.TaskIntentId, StringComparison.Ordinal) ||
            !string.Equals(proposal.SourceMessageId, command.SourceMessageId, StringComparison.Ordinal) ||
            !string.Equals(proposal.SourceConversationItemId, command.SourceConversationItemId, StringComparison.Ordinal) ||
            !string.Equals(proposal.RequesterId, command.RequesterId, StringComparison.Ordinal) ||
            !string.Equals(ProjectIdFromResources(proposal.AffectedResourceReferences), command.ProjectId, StringComparison.Ordinal) ||
            !string.Equals(proposal.AssociationId, command.AssociationId, StringComparison.Ordinal) ||
            proposal.EvidenceSnapshotSourceVersion is null ||
            proposal.EvidenceSnapshotSourceVersion > command.EvidenceSnapshotSourceVersion)
        {
            return RejectProposalInvalidation(command, "proposal_unavailable", command.EvidenceSnapshotSourceVersion);
        }

        string? approvalId = command.ApprovalId ?? state.ApprovalRequests.Values
            .FirstOrDefault(request => string.Equals(request.ProposalId, command.ProposalId, StringComparison.Ordinal))
            ?.ApprovalId;
        if (!string.IsNullOrWhiteSpace(command.ApprovalId) &&
            (!state.ApprovalRequests.TryGetValue(command.ApprovalId, out AiActionApprovalRequested? approval) ||
                !string.Equals(approval.ProposalId, command.ProposalId, StringComparison.Ordinal)))
        {
            return RejectProposalInvalidation(command, "approval_request_unavailable", command.EvidenceSnapshotSourceVersion);
        }

        AiActionProposalInvalidatedByCorrection invalidated = new(
            command.ProposalId,
            approvalId,
            command.TaskIntentId,
            command.SourceMessageId,
            command.SourceConversationItemId,
            command.RequesterId,
            command.ProjectId,
            command.AssociationId,
            command.CorrectionId,
            command.CorrectedEvidenceState,
            command.EvidenceSnapshotSourceVersion,
            command.CorrelationId,
            command.RedactionState,
            command.RetentionClass,
            command.SchemaVersion);

        if (state.InvalidatedAiActionProposals.TryGetValue(command.ProposalId, out AiActionProposalInvalidatedByCorrection? existing))
        {
            return EquivalentInvalidation(existing, invalidated)
                ? DomainResult.NoOp()
                : RejectProposalInvalidation(command, ChatBotRefusalReasonCodes.CorrectedContextInvalidated, existing.EvidenceSnapshotSourceVersion);
        }

        return DomainResult.Success(new IEventPayload[] { invalidated });
    }

    public static DomainResult Handle(ExecuteLowRiskAIAssistance command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidTransitionIdentity(command.ProjectId, command.TaskIntentId, command.SourceMessageId, command.TransitionId, command.CorrelationId) ||
            string.IsNullOrWhiteSpace(command.ProposalId) ||
            string.IsNullOrWhiteSpace(command.RequesterId) ||
            string.IsNullOrWhiteSpace(command.ContextPackageId) ||
            string.IsNullOrWhiteSpace(command.ContextPackageVersion) ||
            string.IsNullOrWhiteSpace(command.ContextPackageRedactionState) ||
            string.IsNullOrWhiteSpace(command.RetentionClass) ||
            string.IsNullOrWhiteSpace(command.ProviderReuseSetting) ||
            string.IsNullOrWhiteSpace(command.ExecutionId) ||
            string.IsNullOrWhiteSpace(command.SchemaVersion) ||
            string.IsNullOrWhiteSpace(command.RedactionState) ||
            command.ExpectedProposalSourceVersion <= 0 ||
            command.SourceEvidenceReferences is not { Count: > 0 } ||
            !AllSafeMetadataTokens(command.SourceEvidenceReferences) ||
            command.AuthorizedContextReferences is null ||
            !AllSafeMetadataTokens(command.AuthorizedContextReferences) ||
            command.ExcludedContextReasons is null ||
            !AllSafeMetadataTokens(command.ExcludedContextReasons) ||
            !IsSafeOptionalMetadataToken(command.PolicySnapshotId) ||
            !IsSafeOptionalMetadataToken(command.SourceConversationItemId) ||
            !IsSafeExecutionClassification(command.RiskClassification) ||
            !IsSafeExecutionRecord(command.ExecutionRecord, command))
        {
            return RejectTransition(command.TaskIntentId, command.ProjectId, command.TransitionId, TaskIntentReasonCodes.InvalidMetadata, null, command.CorrelationId);
        }

        if (state?.LowRiskAiExecutionIds.Contains(command.ExecutionId) == true)
        {
            return DomainResult.NoOp();
        }

        if (state?.InvalidatedAiActionProposals.ContainsKey(command.ProposalId) == true)
        {
            return RejectTransition(
                command.TaskIntentId,
                command.ProjectId,
                command.TransitionId,
                ChatBotRefusalReasonCodes.CorrectedContextInvalidated,
                command.ExpectedProposalSourceVersion,
                command.CorrelationId);
        }

        LowRiskAiAssistanceExecutionRecord record = command.ExecutionRecord!;
        if (string.Equals(record.Outcome, "pending-approval", StringComparison.Ordinal))
        {
            AiActionApprovalRequested approval = ApprovalRequestedFromLowRiskRoute(command, envelope, record);
            return DomainResult.Success(new IEventPayload[]
            {
                new LowRiskAiAssistanceRoutedToApproval(record),
                approval,
            });
        }

        LowRiskAiAssistanceExecutionStarted started = new(
            command.ExecutionId,
            command.ProposalId,
            command.ProjectId,
            command.TaskIntentId,
            command.SourceMessageId,
            command.RequesterId,
            AssistanceKindToken(command.AssistanceKind),
            command.ContextPackageId,
            command.ContextPackageVersion,
            command.PolicySnapshotId ?? record.PolicySnapshotId,
            record.PolicyReasonCode,
            command.ExpectedProposalSourceVersion,
            command.CorrelationId,
            DateTimeOffset.UtcNow,
            command.RedactionState,
            command.RetentionClass,
            command.SchemaVersion);

        IEventPayload completed = string.Equals(record.Outcome, "success", StringComparison.Ordinal)
            ? new LowRiskAiAssistanceExecutionSucceeded(record)
            : new LowRiskAiAssistanceExecutionFailed(record);

        return DomainResult.Success(new IEventPayload[]
        {
            started,
            completed,
        });
    }

    public static DomainResult Handle(DecideAiActionApproval command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (string.IsNullOrWhiteSpace(command.ProjectId) ||
            !IsSafeMetadataToken(command.ApprovalId) ||
            !IsSafeMetadataToken(command.ProposalId) ||
            !IsSafeMetadataToken(command.SourceMessageId) ||
            command.ExpectedApprovalSourceVersion <= 0 ||
            !IsSafeMetadataToken(command.CorrelationId) ||
            !IsSafeMetadataToken(command.DecisionId) ||
            !IsSafeMetadataToken(command.RationaleRedactionState) ||
            !IsSafeMetadataToken(command.SchemaVersion))
        {
            return RejectApprovalDecision(command.ApprovalId, command.ProposalId, "invalid_approval_decision_payload", null, command.CorrelationId);
        }

        if (state is null ||
            !state.ApprovalRequests.TryGetValue(command.ApprovalId, out AiActionApprovalRequested? request) ||
            !string.Equals(request.ProjectId, command.ProjectId, StringComparison.Ordinal) ||
            !string.Equals(request.ProposalId, command.ProposalId, StringComparison.Ordinal) ||
            !string.Equals(request.SourceMessageId, command.SourceMessageId, StringComparison.Ordinal))
        {
            return RejectApprovalDecision(command.ApprovalId, command.ProposalId, "approval_request_unavailable", command.ExpectedApprovalSourceVersion, command.CorrelationId);
        }

        if (request.SourceVersion != command.ExpectedApprovalSourceVersion)
        {
            return RejectApprovalDecision(command.ApprovalId, command.ProposalId, "approval_source_version_mismatch", request.SourceVersion, command.CorrelationId);
        }

        if (state.ApprovalDecisions.TryGetValue(command.ApprovalId, out AiActionApprovalDecisionRecorded? existing))
        {
            return existing.DecisionKind == command.Decision && string.Equals(existing.DecisionActorId, envelope.UserId, StringComparison.Ordinal)
                ? DomainResult.NoOp()
                : RejectApprovalDecision(command.ApprovalId, command.ProposalId, "approval_already_decided", request.SourceVersion, command.CorrelationId);
        }

        if (command.Decision is ApprovalDecisionKind.Approve &&
            state.InvalidatedAiActionProposals.ContainsKey(command.ProposalId))
        {
            return RejectApprovalDecision(
                command.ApprovalId,
                command.ProposalId,
                ChatBotRefusalReasonCodes.CorrectedContextInvalidated,
                request.SourceVersion,
                command.CorrelationId);
        }

        string? disabledReason = ApprovalDisabledReason(command.Decision, request);
        if (disabledReason is not null)
        {
            return RejectApprovalDecision(command.ApprovalId, command.ProposalId, disabledReason, request.SourceVersion, command.CorrelationId);
        }

        long decisionSourceVersion = request.SourceVersion + 1;
        string safeNextAction = command.Decision switch
        {
            ApprovalDecisionKind.Approve => "execute-approved-ai-action",
            ApprovalDecisionKind.RequestRevision => "revise-ai-action",
            _ => "none",
        };

        return DomainResult.Success(new IEventPayload[]
        {
            new AiActionApprovalDecisionRecorded(
                command.ApprovalId,
                command.ProjectId,
                command.ProposalId,
                command.SourceMessageId,
                command.Decision,
                envelope.UserId,
                ActorType(envelope, "human"),
                DateTimeOffset.UtcNow,
                command.ExpectedApprovalSourceVersion,
                "authorized",
                null,
                command.RationaleRedactionState,
                $"audit:{command.DecisionId}",
                "available",
                request.PolicySnapshotId,
                safeNextAction,
                decisionSourceVersion,
                command.CorrelationId),
        });
    }

    public static DomainResult Handle(ExecuteApprovedAIAction command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        ApprovedAiActionCommandAllowlist allowlist = new();
        if (!IsValidApprovedExecutionPayload(command) ||
            !allowlist.IsAllowed(command.CommandName, command.CommandAllowlistVersion))
        {
            return RejectApprovedAiExecution(command, ChatBotRefusalReasonCodes.CommandNotAllowlisted, null);
        }

        if (!command.CorrectedContextReady)
        {
            return RejectApprovedAiExecution(command, ChatBotRefusalReasonCodes.CorrectedContextInvalidated, command.ExpectedApprovalSourceVersion);
        }

        if (state?.InvalidatedAiActionProposals.ContainsKey(command.ProposalId) == true)
        {
            return RejectApprovedAiExecution(command, ChatBotRefusalReasonCodes.CorrectedContextInvalidated, command.ExpectedApprovalSourceVersion);
        }

        if (state?.ApprovedAiExecutions.TryGetValue(command.ExecutionId, out ApprovedAiActionExecutionStarted? existingExecution) == true)
        {
            return IsEquivalentApprovedExecution(command, existingExecution)
                ? DomainResult.NoOp()
                : RejectApprovedAiExecution(command, ChatBotRefusalReasonCodes.ApprovalStateInvalid, command.ExpectedApprovalSourceVersion);
        }

        if (state is null ||
            !state.ApprovalRequests.TryGetValue(command.ApprovalId, out AiActionApprovalRequested? request) ||
            !state.ApprovalDecisions.TryGetValue(command.ApprovalId, out AiActionApprovalDecisionRecorded? decision))
        {
            return RejectApprovedAiExecution(command, ChatBotRefusalReasonCodes.ApprovalStateInvalid, command.ExpectedApprovalSourceVersion);
        }

        if (string.IsNullOrWhiteSpace(command.PolicySnapshotId) &&
            string.IsNullOrWhiteSpace(request.PolicySnapshotId))
        {
            return RejectApprovedAiExecution(command, ChatBotRefusalReasonCodes.PolicySnapshotUnavailable, command.ExpectedApprovalSourceVersion);
        }

        string? approvalRejection = ValidateApprovedExecutionApproval(command, request, decision);
        if (approvalRejection is not null)
        {
            return RejectApprovedAiExecution(command, approvalRejection, decision.SourceVersion);
        }

        ApprovedAiActionExecutionRecord record = command.ExecutionRecord!;
        ApprovedAiActionExecutionStarted started = new(
            command.ExecutionId,
            command.ProposalId,
            command.ApprovalId,
            command.ProjectId,
            command.TaskIntentId,
            command.SourceMessageId,
            command.SourceConversationItemId,
            command.RequesterId,
            command.CommandName,
            command.CommandAllowlistVersion,
            command.ExpectedApprovalSourceVersion,
            command.ExpectedProposalSourceVersion,
            command.PolicySnapshotId ?? request.PolicySnapshotId,
            command.CorrelationId,
            DateTimeOffset.UtcNow,
            command.RedactionState,
            command.RetentionClass);

        IEventPayload completed = string.Equals(record.Outcome, "success", StringComparison.Ordinal)
            ? new ApprovedAiActionExecutionSucceeded(
                record,
                command.ProjectId,
                command.RequesterId,
                command.SourceMessageId,
                command.SourceConversationItemId)
            : new ApprovedAiActionExecutionFailed(
                record,
                command.ProjectId,
                command.RequesterId,
                command.SourceMessageId,
                command.SourceConversationItemId);

        return DomainResult.Success(new[]
        {
            started,
            completed,
        });
    }

    public static DomainResult Handle(MarkTaskIntentDisposition command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidTransitionIdentity(command.ProjectId, command.TaskIntentId, command.SourceMessageId, command.TransitionId, command.CorrelationId) ||
            command.ExpectedSourceVersion <= 0 ||
            command.EvidenceReferences is not { Count: > 0 } ||
            !AllSafeMetadataTokens(command.EvidenceReferences) ||
            !IsSafeOptionalMetadataToken(command.PolicySnapshotId) ||
            !IsSafeOptionalMetadataToken(command.PredecessorTaskIntentId) ||
            !IsSafeOptionalMetadataToken(command.ReasonCode) ||
            !TryDispositionState(command.Disposition, out TaskIntentState dispositionState))
        {
            return RejectTransition(command.TaskIntentId, command.ProjectId, command.TransitionId, TaskIntentReasonCodes.InvalidMetadata, null, command.CorrelationId);
        }

        if (state?.TaskIntentTransitionIds.TryGetValue(command.TransitionId, out string? existingTaskIntentId) == true)
        {
            return string.Equals(existingTaskIntentId, command.TaskIntentId, StringComparison.Ordinal)
                ? DomainResult.NoOp()
                : RejectTransition(command.TaskIntentId, command.ProjectId, command.TransitionId, TaskIntentReasonCodes.IdempotencyConflict, command.ExpectedSourceVersion, command.CorrelationId);
        }

        if (state is null || !state.TaskIntents.TryGetValue(command.TaskIntentId, out TaskIntentRecord? record))
        {
            return RejectTransition(command.TaskIntentId, command.ProjectId, command.TransitionId, TaskIntentReasonCodes.MissingCapturedIntent, command.ExpectedSourceVersion, command.CorrelationId);
        }

        string? rejection = ValidateCapturedRecord(envelope.TenantId, command.ProjectId, command.SourceMessageId, command.ExpectedSourceVersion, record);
        if (rejection is not null)
        {
            return RejectTransition(command.TaskIntentId, command.ProjectId, command.TransitionId, rejection, record.SourceVersion, command.CorrelationId);
        }

        if (dispositionState is TaskIntentState.Duplicate && !IsValidDuplicatePredecessor(command, state, record.TenantId))
        {
            return RejectTransition(command.TaskIntentId, command.ProjectId, command.TransitionId, TaskIntentReasonCodes.DuplicatePredecessorInvalid, record.SourceVersion, command.CorrelationId);
        }

        DateTimeOffset decidedAt = DateTimeOffset.UtcNow;
        string auditOperationId = $"audit:{command.TransitionId}";
        string reasonCode = string.IsNullOrWhiteSpace(command.ReasonCode)
            ? TaskIntentReasonCodes.DispositionMarked
            : command.ReasonCode;
        TaskIntentRecord transitioned = record with
        {
            State = dispositionState,
            ReasonCode = reasonCode,
            SafeNextAction = "none",
            DuplicatePredecessorTaskIntentId = dispositionState is TaskIntentState.Duplicate ? command.PredecessorTaskIntentId : null,
            ReviewerActorId = envelope.UserId,
            DecidedAtUtc = decidedAt,
            AuditOperationId = auditOperationId,
            TransitionId = command.TransitionId,
            PolicySnapshotId = command.PolicySnapshotId ?? record.PolicySnapshotId,
            CorrelationId = command.CorrelationId,
        };

        return DomainResult.Success(new IEventPayload[]
        {
            new TaskIntentDispositionMarked(transitioned, command.Disposition, envelope.UserId, decidedAt, command.PredecessorTaskIntentId, auditOperationId),
        });
    }

    public static DomainResult Handle(ResolveMailboxMessageParticipants command, GovernedOperationState? state)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!ParticipantResolutionId.TryParse(command.ResolutionId, out _) ||
            !MailboxMessageIntakeId.TryParse(command.IntakeId, out _))
        {
            return InvalidResolution(command.ResolutionId, "invalid_resolution_identity");
        }

        if (state?.ParticipantResolutionIds.Contains(command.ResolutionId) == true)
        {
            return InvalidResolution(command.ResolutionId, "participant_resolution_already_recorded");
        }

        if (command.SourceParticipants is null ||
            command.ResolvedParticipants is null ||
            command.UnresolvedParticipants is null ||
            string.IsNullOrWhiteSpace(command.SourceMailboxId) ||
            string.IsNullOrWhiteSpace(command.ResolutionKernelVersion) ||
            command.SourceParticipants.Count == 0 ||
            (command.ResolvedParticipants.Count + command.UnresolvedParticipants.Count) == 0)
        {
            return InvalidResolution(command.ResolutionId, "missing_participant_resolution");
        }

        HashSet<string> sourceIds = command.SourceParticipants
            .Select(static source => source.SourceParticipantId)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        if (sourceIds.Count != command.SourceParticipants.Count)
        {
            return InvalidResolution(command.ResolutionId, "invalid_source_participant");
        }

        List<IEventPayload> events = [];
        foreach (ResolvedMailboxParticipantReference resolved in command.ResolvedParticipants)
        {
            if (!sourceIds.Contains(resolved.SourceParticipantId) ||
                resolved.Status != ParticipantResolutionStatus.Resolved ||
                string.IsNullOrWhiteSpace(resolved.PartyId) ||
                string.IsNullOrWhiteSpace(resolved.PartyTenantId) ||
                string.IsNullOrWhiteSpace(resolved.EvidenceReference) ||
                string.IsNullOrWhiteSpace(resolved.EvidenceFingerprint))
            {
                return InvalidResolution(command.ResolutionId, "invalid_resolved_participant");
            }

            events.Add(new MailboxParticipantResolved(
                command.ResolutionId,
                command.IntakeId,
                resolved.SourceParticipantId,
                resolved.PartyId,
                resolved.PartyTenantId,
                resolved.EvidenceReference,
                resolved.EvidenceFingerprint,
                command.SourceMailboxId,
                "m365-mailbox-intake",
                command.ResolutionKernelVersion,
                "metadata_only",
                "collaboration_input",
                1,
                "chatbot.participant-resolution-event.v1"));
        }

        foreach (UnresolvedMailboxParticipantEvidence unresolved in command.UnresolvedParticipants)
        {
            if (!sourceIds.Contains(unresolved.SourceParticipantId) ||
                string.IsNullOrWhiteSpace(unresolved.EvidenceReference) ||
                string.IsNullOrWhiteSpace(unresolved.EvidenceFingerprint) ||
                unresolved.AllowedReviewActions is null ||
                unresolved.AllowedReviewActions.Count == 0)
            {
                return InvalidResolution(command.ResolutionId, "invalid_unresolved_participant");
            }

            events.Add(new MailboxParticipantUnresolved(
                command.ResolutionId,
                command.IntakeId,
                unresolved.SourceParticipantId,
                unresolved.EvidenceReference,
                unresolved.EvidenceFingerprint,
                unresolved.Reason,
                unresolved.AllowedReviewActions,
                command.SourceMailboxId,
                "m365-mailbox-intake",
                command.ResolutionKernelVersion,
                "metadata_only",
                "collaboration_input",
                1,
                "chatbot.participant-resolution-event.v1"));
        }

        return DomainResult.Success(events);
    }

    public static DomainResult Handle(ScoreMailboxMessageAssociation command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!AssociationWorkflowId.TryParse(command.AssociationId, out _) ||
            !MailboxMessageIntakeId.TryParse(command.IntakeId, out _))
        {
            return InvalidAssociation(command.AssociationId, "invalid_association_identity");
        }

        if (state?.AssociationIds.Contains(command.AssociationId) == true)
        {
            return InvalidAssociation(command.AssociationId, "association_scoring_already_recorded");
        }

        if (command.DeterministicSignals is null ||
            command.Candidates is null ||
            command.Exclusions is null ||
            command.Result is null ||
            command.ThresholdPolicy is null ||
            command.DeterministicSignals.Count == 0 ||
            string.IsNullOrWhiteSpace(command.SourceMailboxId) ||
            string.IsNullOrWhiteSpace(command.SourceConversationId) ||
            string.IsNullOrWhiteSpace(command.ScoringKernelVersion) ||
            !AssociationThresholdPolicyValidator.IsValid(command.ThresholdPolicy) ||
            !IsValidScore(command.Result.ConfidenceScore) ||
            !IsValidAuthenticity(command.Authenticity) ||
            !IsConsistentAssociationResult(command, envelope))
        {
            return InvalidAssociation(command.AssociationId, "invalid_association_scoring_payload");
        }

        if (command.Candidates.Any(static candidate =>
            string.IsNullOrWhiteSpace(candidate.ProjectId) ||
            candidate.ConfidenceScore < 0.0 ||
            candidate.ConfidenceScore > 1.0 ||
            candidate.Rank <= 0 ||
            candidate.ReasonCodes is null ||
            candidate.EvidenceRefs is null ||
            candidate.ConfidenceInputs is null))
        {
            return InvalidAssociation(command.AssociationId, "invalid_association_candidate");
        }

        if (IsAutoAssociatedButInvalid(command, command.Result))
        {
            return InvalidAssociation(command.AssociationId, "invalid_auto_association_scoring_payload");
        }

        if (command.Result.Outcome == AssociationScoringOutcome.FailedClosed && command.Candidates.Count != 0)
        {
            return InvalidAssociation(command.AssociationId, "invalid_fail_closed_association_scoring_payload");
        }

        if (RoutesToReview(command.Result) && !IsValidReviewTransition())
        {
            return InvalidAssociation(command.AssociationId, "invalid_association_lifecycle_transition");
        }

        string tenantId = envelope.TenantId;
        AssociationScoringResult result = command.Result;
        return result.Outcome switch
        {
            AssociationScoringOutcome.AutoAssociated when IsValidAutoAssociation(command, result) =>
                DomainResult.Success(new IEventPayload[]
                {
                    new MailboxEmailAssociatedToProject(
                        command.AssociationId,
                        command.IntakeId,
                        tenantId,
                        command.Candidates[0].ProjectId,
                        command.Candidates[0].DisplayName,
                        command.SourceMailboxId,
                        command.SourceConversationId,
                        command.SourceThreadId,
                        command.Candidates[0].EvidenceRefs,
                        command.Candidates[0].ConfidenceInputs,
                        result.ConfidenceScore,
                        result.ThresholdBand,
                        result.ReasonCodes,
                        command.ThresholdPolicy.PolicyVersion,
                        result.KernelVersion,
                        result.DetectedAt.ToUniversalTime(),
                        result.RedactionState,
                        result.RetentionClass,
                        1,
                        result.SchemaVersion,
                        envelope.CorrelationId,
                        envelope.UserId,
                        ActorType(envelope, "system"),
                        "associate",
                        SurfaceOrigin(envelope, "worker"),
                        result.DetectedAt.ToUniversalTime(),
                        result.ExternalSender,
                        result.StrictnessPolicy,
                        result.RoutingReason),
                }),
            AssociationScoringOutcome.FailedClosed =>
                DomainResult.Success(new IEventPayload[]
                {
                    new MailboxAssociationScoringFailedClosed(
                        command.AssociationId,
                        command.IntakeId,
                        tenantId,
                        command.SourceMailboxId,
                        command.SourceConversationId,
                        command.SourceThreadId,
                        command.Exclusions,
                        LifecycleState.NeedsReview,
                        result.ConfidenceScore,
                        result.ThresholdBand,
                        result.ReasonCodes,
                        command.ThresholdPolicy.PolicyVersion,
                        result.KernelVersion,
                        result.DetectedAt.ToUniversalTime(),
                        result.RedactionState,
                        result.RetentionClass,
                        1,
                        result.SchemaVersion,
                        envelope.CorrelationId,
                        result.ExternalSender,
                        result.StrictnessPolicy,
                        result.RoutingReason),
                }),
            _ =>
                DomainResult.Success(new IEventPayload[]
                {
                    new MailboxAssociationCandidatesGenerated(
                        command.AssociationId,
                        command.IntakeId,
                        tenantId,
                        command.SourceMailboxId,
                        command.SourceConversationId,
                        command.SourceThreadId,
                        command.Candidates,
                        command.Exclusions,
                        LifecycleState.NeedsReview,
                        result.ConfidenceScore,
                        result.ThresholdBand,
                        result.Outcome,
                        result.ReasonCodes,
                        command.ThresholdPolicy.PolicyVersion,
                        result.KernelVersion,
                        result.DetectedAt.ToUniversalTime(),
                        result.RedactionState,
                        result.RetentionClass,
                        1,
                        result.SchemaVersion,
                        envelope.CorrelationId,
                        result.ExternalSender,
                        result.StrictnessPolicy,
                        result.RoutingReason),
                }),
        };
    }

    public static DomainResult Handle(SetAssociationConfidenceThresholds command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (string.IsNullOrWhiteSpace(command.PolicyId) ||
            string.IsNullOrWhiteSpace(command.PolicyVersion))
        {
            return InvalidThreshold(command.PolicyId, "invalid_threshold_policy");
        }

        if (state?.ThresholdPolicyVersions.Contains(command.PolicyVersion) == true)
        {
            return InvalidThreshold(command.PolicyId, "threshold_policy_already_recorded");
        }

        if (!AssociationThresholdPolicyValidator.IsValid(command.THigh, command.TLow, command.EvaluationRunReference))
        {
            return InvalidThreshold(command.PolicyId, "invalid_threshold_policy");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new AssociationConfidenceThresholdsChanged(
                command.PolicyId,
                envelope.TenantId,
                state?.AssociationTHigh ?? AssociationThresholdPolicySnapshot.DefaultM0High,
                state?.AssociationTLow ?? AssociationThresholdPolicySnapshot.DefaultM0Low,
                state?.AssociationThresholdPolicyVersion ?? AssociationThresholdPolicySnapshot.DefaultM0.PolicyVersion,
                command.THigh,
                command.TLow,
                command.PolicyVersion,
                command.EvaluationRunReference,
                envelope.UserId,
                envelope.CorrelationId,
                (command.ChangedAt ?? DateTimeOffset.UnixEpoch).ToUniversalTime(),
                "metadata_only",
                "collaboration_input",
                1,
                "chatbot.association-threshold-policy-event.v1"),
        });
    }

    public static DomainResult Handle(AssociateEmailToProject command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionValidation validation = ValidateDecision(
            command.AssociationId,
            command.IntakeId,
            command.DecisionKind,
            AssociationDecisionKind.Associate,
            command.CandidateEvidenceFingerprint,
            command.SourceVersion,
            command.SchemaVersion,
            command.DecisionNote,
            state);
        if (!validation.IsValid)
        {
            return InvalidAssociationDecision(command.AssociationId, validation.ReasonCode);
        }

        AssociationDecisionSourceSnapshot source = validation.Source!;
        AssociationCandidate? selected = source.Candidates.FirstOrDefault(candidate => string.Equals(candidate.ProjectId, command.ProjectId, StringComparison.Ordinal));
        if (selected is null)
        {
            return InvalidAssociationDecision(command.AssociationId, "missing_authorized_candidate");
        }

        if (!EvidenceFingerprintMatches(selected.EvidenceRefs, command.CandidateEvidenceFingerprint))
        {
            return InvalidAssociationDecision(command.AssociationId, "stale_evidence");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxEmailAssociationConfirmed(
                command.AssociationId,
                command.IntakeId,
                envelope.TenantId,
                envelope.UserId,
                ActorType(envelope),
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.DecisionKind,
                selected.ProjectId,
                selected.DisplayName,
                source.Candidates.Select(static candidate => candidate.ProjectId).ToArray(),
                selected.EvidenceRefs,
                selected.ConfidenceInputs,
                source.ConfidenceScore,
                source.ThresholdBand,
                source.ReasonCodes,
                source.ThresholdPolicyVersion,
                source.DerivationKernelVersion,
                source.DetectedAt,
                DecisionTimestamp(envelope, source.DetectedAt),
                AssociationCandidateView.MailboxSourceProvenance,
                source.RedactionState,
                source.RetentionClass,
                source.SourceVersion + 1,
                command.SchemaVersion,
                envelope.CorrelationId,
                SurfaceOrigin(envelope),
                validation.SanitizedNote,
                "metadata_only",
                source.ThresholdPolicyVersion),
        });
    }

    public static DomainResult Handle(RejectEmailProjectAssociation command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionValidation validation = ValidateDecision(
            command.AssociationId,
            command.IntakeId,
            command.DecisionKind,
            AssociationDecisionKind.Reject,
            command.CandidateEvidenceFingerprint,
            command.SourceVersion,
            command.SchemaVersion,
            command.DecisionNote,
            state);
        if (!validation.IsValid)
        {
            return InvalidAssociationDecision(command.AssociationId, validation.ReasonCode);
        }

        AssociationDecisionSourceSnapshot source = validation.Source!;
        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxEmailAssociationRejected(
                command.AssociationId,
                command.IntakeId,
                envelope.TenantId,
                envelope.UserId,
                ActorType(envelope),
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.DecisionKind,
                source.Candidates.Select(static candidate => candidate.ProjectId).ToArray(),
                AllEvidenceRefs(source),
                source.ConfidenceScore,
                source.ThresholdBand,
                source.ReasonCodes,
                source.ThresholdPolicyVersion,
                source.DerivationKernelVersion,
                source.DetectedAt,
                DecisionTimestamp(envelope, source.DetectedAt),
                AssociationCandidateView.MailboxSourceProvenance,
                source.RedactionState,
                source.RetentionClass,
                source.SourceVersion + 1,
                command.SchemaVersion,
                envelope.CorrelationId,
                SurfaceOrigin(envelope),
                validation.SanitizedNote,
                "metadata_only",
                source.ThresholdPolicyVersion),
        });
    }

    public static DomainResult Handle(DeferEmailProjectAssociation command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionValidation validation = ValidateDecision(
            command.AssociationId,
            command.IntakeId,
            command.DecisionKind,
            AssociationDecisionKind.Defer,
            command.CandidateEvidenceFingerprint,
            command.SourceVersion,
            command.SchemaVersion,
            command.DecisionNote,
            state);
        if (!validation.IsValid)
        {
            return InvalidAssociationDecision(command.AssociationId, validation.ReasonCode);
        }

        AssociationDecisionSourceSnapshot source = validation.Source!;
        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxEmailAssociationDeferred(
                command.AssociationId,
                command.IntakeId,
                envelope.TenantId,
                envelope.UserId,
                ActorType(envelope),
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.DecisionKind,
                source.Candidates.Select(static candidate => candidate.ProjectId).ToArray(),
                AllEvidenceRefs(source),
                source.ConfidenceScore,
                source.ThresholdBand,
                source.ReasonCodes,
                source.ThresholdPolicyVersion,
                source.DerivationKernelVersion,
                source.DetectedAt,
                DecisionTimestamp(envelope, source.DetectedAt),
                AssociationCandidateView.MailboxSourceProvenance,
                source.RedactionState,
                source.RetentionClass,
                source.SourceVersion + 1,
                command.SchemaVersion,
                envelope.CorrelationId,
                SurfaceOrigin(envelope),
                validation.SanitizedNote,
                "metadata_only",
                source.ThresholdPolicyVersion),
        });
    }

    public static DomainResult Handle(MarkEmailAssociationNeedsReview command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionValidation validation = ValidateDecision(
            command.AssociationId,
            command.IntakeId,
            command.DecisionKind,
            AssociationDecisionKind.NeedsReview,
            command.CandidateEvidenceFingerprint,
            command.SourceVersion,
            command.SchemaVersion,
            command.DecisionNote,
            state);
        if (!validation.IsValid)
        {
            return InvalidAssociationDecision(command.AssociationId, validation.ReasonCode);
        }

        AssociationDecisionSourceSnapshot source = validation.Source!;
        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxEmailAssociationMarkedNeedsReview(
                command.AssociationId,
                command.IntakeId,
                envelope.TenantId,
                envelope.UserId,
                ActorType(envelope),
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.DecisionKind,
                source.Candidates.Select(static candidate => candidate.ProjectId).ToArray(),
                AllEvidenceRefs(source),
                source.ConfidenceScore,
                source.ThresholdBand,
                source.ReasonCodes,
                source.ThresholdPolicyVersion,
                source.DerivationKernelVersion,
                source.DetectedAt,
                DecisionTimestamp(envelope, source.DetectedAt),
                AssociationCandidateView.MailboxSourceProvenance,
                source.RedactionState,
                source.RetentionClass,
                source.SourceVersion + 1,
                command.SchemaVersion,
                envelope.CorrelationId,
                SurfaceOrigin(envelope),
                validation.SanitizedNote,
                "metadata_only",
                source.ThresholdPolicyVersion),
        });
    }

    public static DomainResult Handle(CorrectEmailProjectAssociation command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!AssociationWorkflowId.TryParse(command.AssociationId, out _) ||
            !MailboxMessageIntakeId.TryParse(command.IntakeId, out _) ||
            !AssociationWorkflowId.TryParse(command.PredecessorAssociationId, out _) ||
            command.CorrectionKind != AssociationCorrectionKind.ProjectReassignment ||
            string.IsNullOrWhiteSpace(command.PriorProjectId) ||
            string.IsNullOrWhiteSpace(command.TargetProjectId) ||
            string.IsNullOrWhiteSpace(command.CandidateEvidenceFingerprint) ||
            command.SourceVersion <= 0 ||
            string.IsNullOrWhiteSpace(command.SchemaVersion))
        {
            return InvalidAssociationCorrection(command.AssociationId, "invalid_association_correction_payload");
        }

        AssociationDecisionSourceSnapshot? source = state?.AssociationDecisionSource;
        if (source is null ||
            !string.Equals(source.AssociationId, command.AssociationId, StringComparison.Ordinal) ||
            !string.Equals(source.IntakeId, command.IntakeId, StringComparison.Ordinal))
        {
            return InvalidAssociationCorrection(command.AssociationId, "missing_association_evidence");
        }

        if (state!.AssociationLifecycleState is not (LifecycleState.Associated or LifecycleState.Corrected))
        {
            return InvalidAssociationCorrection(command.AssociationId, "invalid_association_lifecycle_transition");
        }

        long currentSourceVersion = state.LastAssociationDecisionSourceVersion ?? source.SourceVersion;
        if (currentSourceVersion != command.SourceVersion)
        {
            return InvalidAssociationCorrection(command.AssociationId, "stale_evidence");
        }

        if (string.IsNullOrWhiteSpace(state.CurrentAssociationProjectId) ||
            !string.Equals(state.CurrentAssociationProjectId, command.PriorProjectId, StringComparison.Ordinal) ||
            string.Equals(state.CurrentAssociationProjectId, command.TargetProjectId, StringComparison.Ordinal))
        {
            return InvalidAssociationCorrection(command.AssociationId, "association_already_corrected");
        }

        if (!EvidenceFingerprintMatches(AllEvidenceRefs(source), command.CandidateEvidenceFingerprint))
        {
            return InvalidAssociationCorrection(command.AssociationId, "stale_evidence");
        }

        if (!TrySanitizeDecisionNote(command.CorrectionRationale, out string? sanitizedRationale))
        {
            return InvalidAssociationCorrection(command.AssociationId, "invalid_correction_rationale");
        }

        AssociationEvidenceReference correctionEvidence = new(
            "association:correction",
            command.CandidateEvidenceFingerprint,
            "association-correction");

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxEmailAssociationCorrected(
                command.AssociationId,
                command.IntakeId,
                envelope.TenantId,
                envelope.UserId,
                ActorType(envelope),
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.CorrectionKind,
                state.CurrentAssociationProjectId,
                command.TargetProjectId,
                null,
                command.PredecessorAssociationId,
                command.PredecessorAssociationId,
                source.Candidates.Select(static candidate => candidate.ProjectId).Append(command.TargetProjectId).Distinct(StringComparer.Ordinal).ToArray(),
                [correctionEvidence],
                source.Candidates.SelectMany(static candidate => candidate.ConfidenceInputs).ToArray(),
                source.ConfidenceScore,
                source.ThresholdBand,
                source.ReasonCodes,
                source.ThresholdPolicyVersion,
                source.DerivationKernelVersion,
                source.DetectedAt,
                DecisionTimestamp(envelope, source.DetectedAt),
                AssociationCandidateView.MailboxSourceProvenance,
                source.RedactionState,
                source.RetentionClass,
                currentSourceVersion + 1,
                command.SchemaVersion,
                envelope.CorrelationId,
                SurfaceOrigin(envelope),
                sanitizedRationale,
                "metadata_only",
                source.ThresholdPolicyVersion,
                CorrectionPropagationStatuses.Pending),
        });
    }

    public static DomainResult Handle(StartMailboxAssociationCorrectionPropagation command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionSourceSnapshot? source = state?.AssociationDecisionSource;
        if (!IsValidPropagationCommand(command.AssociationId, command.IntakeId, command.CorrectionId, command.WorkflowInstanceId, command.SourceVersion, command.SchemaVersion) ||
            source is null ||
            state is null ||
            state.AssociationLifecycleState is not LifecycleState.Corrected ||
            !string.Equals(source.AssociationId, command.AssociationId, StringComparison.Ordinal) ||
            !string.Equals(source.IntakeId, command.IntakeId, StringComparison.Ordinal) ||
            state.LastAssociationDecisionSourceVersion != command.SourceVersion ||
            string.IsNullOrWhiteSpace(command.PriorProjectId) ||
            string.IsNullOrWhiteSpace(command.CorrectedProjectId) ||
            command.RequiredStoreKeys is not { Count: > 0 } ||
            command.RequiredStoreKeys.Any(static key => !CorrectionPropagationStoreKeys.RequiredM0Set.Contains(key)) ||
            command.EstimatedCompletionAtUtc < command.StartedAtUtc ||
            string.IsNullOrWhiteSpace(command.ResponsibleOwnerRole) ||
            string.IsNullOrWhiteSpace(command.NextSafeAction))
        {
            return InvalidAssociationCorrection(command.AssociationId, "invalid_correction_propagation_start");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxAssociationCorrectionPropagationStarted(
                command.AssociationId,
                command.IntakeId,
                envelope.TenantId,
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.CorrectionId,
                command.WorkflowInstanceId,
                command.PriorProjectId,
                command.CorrectedProjectId,
                command.RequiredStoreKeys.Distinct(StringComparer.Ordinal).ToArray(),
                command.StartedAtUtc.ToUniversalTime(),
                command.EstimatedCompletionAtUtc.ToUniversalTime(),
                command.ResponsibleOwnerRole,
                command.NextSafeAction,
                source.RedactionState,
                source.RetentionClass,
                command.SourceVersion,
                command.SchemaVersion,
                envelope.CorrelationId),
        });
    }

    public static DomainResult Handle(AcknowledgeMailboxAssociationCorrectionStoreInvalidated command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionSourceSnapshot? source = state?.AssociationDecisionSource;
        if (!IsValidPropagationCommand(command.AssociationId, source?.IntakeId, command.CorrectionId, command.WorkflowInstanceId, command.SourceVersion, command.SchemaVersion) ||
            source is null ||
            state is null ||
            state.AssociationLifecycleState is not (LifecycleState.Correcting or LifecycleState.CorrectionDelayed) ||
            !string.Equals(state.CorrectionPropagationCorrectionId, command.CorrectionId, StringComparison.Ordinal) ||
            !string.Equals(state.CorrectionPropagationWorkflowInstanceId, command.WorkflowInstanceId, StringComparison.Ordinal) ||
            state.CorrectionPropagationSourceVersion != command.SourceVersion ||
            !state.CorrectionPropagationRequiredStores.Contains(command.StoreKey) ||
            !string.Equals(command.PriorProjectId, state.PriorAssociationProjectId, StringComparison.Ordinal) ||
            !string.Equals(command.CorrectedProjectId, state.CurrentAssociationProjectId, StringComparison.Ordinal) ||
            command.CompletedAtUtc < command.StartedAtUtc ||
            command.Outcome is not ("success" or "failed") ||
            (string.Equals(command.Outcome, "failed", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(command.FailureReasonCode)) ||
            !IsMetadataOnly(command.RedactionState, command.RetentionClass))
        {
            return InvalidAssociationCorrection(command.AssociationId, "invalid_correction_store_acknowledgement");
        }

        if (state.CorrectionPropagationStores.TryGetValue(command.StoreKey, out CorrectionPropagationStoreAcknowledgement? existing) &&
            existing.SourceVersion == command.SourceVersion &&
            string.Equals(existing.Outcome, command.Outcome, StringComparison.Ordinal) &&
            string.Equals(existing.FailureReasonCode, command.FailureReasonCode, StringComparison.Ordinal) &&
            existing.StartedAtUtc == command.StartedAtUtc.ToUniversalTime() &&
            existing.CompletedAtUtc == command.CompletedAtUtc.ToUniversalTime())
        {
            return DomainResult.NoOp();
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxAssociationCorrectionStoreInvalidated(
                command.AssociationId,
                source.IntakeId,
                envelope.TenantId,
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.CorrectionId,
                command.StoreKey,
                command.WorkflowInstanceId,
                command.SourceVersion,
                command.PriorProjectId,
                command.CorrectedProjectId,
                command.StartedAtUtc.ToUniversalTime(),
                command.CompletedAtUtc.ToUniversalTime(),
                command.Outcome,
                command.FailureReasonCode,
                command.RedactionState,
                command.RetentionClass,
                command.SchemaVersion,
                envelope.CorrelationId),
        });
    }

    public static DomainResult Handle(CompleteMailboxAssociationCorrectionPropagation command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionSourceSnapshot? source = state?.AssociationDecisionSource;
        if (!IsValidPropagationCommand(command.AssociationId, source?.IntakeId, command.CorrectionId, command.WorkflowInstanceId, command.SourceVersion, command.SchemaVersion) ||
            source is null ||
            state is null ||
            state.AssociationLifecycleState is not (LifecycleState.Correcting or LifecycleState.CorrectionDelayed) ||
            !string.Equals(state.CorrectionPropagationCorrectionId, command.CorrectionId, StringComparison.Ordinal) ||
            !string.Equals(state.CorrectionPropagationWorkflowInstanceId, command.WorkflowInstanceId, StringComparison.Ordinal) ||
            state.CorrectionPropagationSourceVersion != command.SourceVersion ||
            string.IsNullOrWhiteSpace(state.PriorAssociationProjectId) ||
            string.IsNullOrWhiteSpace(state.CurrentAssociationProjectId) ||
            !string.Equals(command.DownstreamImpactStatus, CorrectionPropagationStatuses.Complete, StringComparison.Ordinal) ||
            state.CorrectionPropagationRequiredStores.Count == 0 ||
            state.CorrectionPropagationRequiredStores.Any(storeKey => !state.CorrectionPropagationStores.TryGetValue(storeKey, out CorrectionPropagationStoreAcknowledgement? ack) || !ack.IsSuccessful))
        {
            return InvalidAssociationCorrection(command.AssociationId, "incomplete_correction_propagation");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxAssociationCorrectionPropagationCompleted(
                command.AssociationId,
                source.IntakeId,
                envelope.TenantId,
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.CorrectionId,
                command.WorkflowInstanceId,
                command.SourceVersion,
                state.PriorAssociationProjectId ?? string.Empty,
                state.CurrentAssociationProjectId ?? string.Empty,
                state.CorrectionPropagationStores.Values.Where(static ack => ack.IsSuccessful).Select(static ack => ack.StoreKey).Order(StringComparer.Ordinal).ToArray(),
                command.CompletedAtUtc.ToUniversalTime(),
                command.DownstreamImpactStatus,
                source.RedactionState,
                source.RetentionClass,
                command.SchemaVersion,
                envelope.CorrelationId),
        });
    }

    public static DomainResult Handle(DelayMailboxAssociationCorrectionPropagation command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionSourceSnapshot? source = state?.AssociationDecisionSource;
        if (!IsValidPropagationCommand(command.AssociationId, source?.IntakeId, command.CorrectionId, command.WorkflowInstanceId, command.SourceVersion, command.SchemaVersion) ||
            source is null ||
            state is null ||
            state.AssociationLifecycleState is not LifecycleState.Correcting ||
            !string.Equals(state.CorrectionPropagationCorrectionId, command.CorrectionId, StringComparison.Ordinal) ||
            !string.Equals(state.CorrectionPropagationWorkflowInstanceId, command.WorkflowInstanceId, StringComparison.Ordinal) ||
            state.CorrectionPropagationSourceVersion != command.SourceVersion ||
            string.IsNullOrWhiteSpace(state.PriorAssociationProjectId) ||
            string.IsNullOrWhiteSpace(state.CurrentAssociationProjectId) ||
            string.IsNullOrWhiteSpace(command.ResponsibleOwnerRole) ||
            string.IsNullOrWhiteSpace(command.NextSafeAction) ||
            string.IsNullOrWhiteSpace(command.ReasonCode))
        {
            return InvalidAssociationCorrection(command.AssociationId, "invalid_correction_propagation_delay");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxAssociationCorrectionPropagationDelayed(
                command.AssociationId,
                source.IntakeId,
                envelope.TenantId,
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.CorrectionId,
                command.WorkflowInstanceId,
                command.SourceVersion,
                state.PriorAssociationProjectId ?? string.Empty,
                state.CurrentAssociationProjectId ?? string.Empty,
                command.DelayedAtUtc.ToUniversalTime(),
                command.ResponsibleOwnerRole,
                command.NextSafeAction,
                command.ReasonCode,
                source.RedactionState,
                source.RetentionClass,
                command.SchemaVersion,
                envelope.CorrelationId),
        });
    }

    private static DomainResult Invalid(string? intakeId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new MailboxMessageIntakeInvalidRejection(intakeId, reasonCode),
        });

    private static DomainResult InvalidResolution(string? resolutionId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new MailboxParticipantResolutionInvalidRejection(resolutionId, reasonCode),
        });

    private static DomainResult InvalidAssociation(string? associationId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new MailboxAssociationInvalidRejection(associationId, reasonCode),
        });

    private static DomainResult InvalidAssociationDecision(string? associationId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new MailboxAssociationDecisionInvalidRejection(associationId, reasonCode),
        });

    private static DomainResult InvalidAssociationCorrection(string? associationId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new MailboxAssociationCorrectionInvalidRejection(associationId, reasonCode),
        });

    private static DomainResult InvalidThreshold(string? policyId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new AssociationThresholdPolicyInvalidRejection(policyId, reasonCode),
        });

    private static DomainResult InvalidRetry(string? retryId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new WorkflowRetryInvalidRejection(retryId, reasonCode),
        });

    private static DomainResult RejectOutboundDraft(CreateOutboundDraft command, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new OutboundDraftCreationRejected(
                SafeRejectionToken(command.DraftId),
                SafeRejectionToken(command.ProjectId),
                SafeRejectionToken(command.RequesterId),
                SafeRejectionToken(reasonCode),
                SafeRejectionToken(command.CorrelationId),
                SafeOptionalRejectionToken(command.PolicySnapshotId),
                SafeRejectionToken(command.RedactionState),
                SafeRejectionToken(command.RetentionClass)),
        });

    private static bool IsValidOutboundDraft(CreateOutboundDraft command)
        => IsSafeMetadataToken(command.DraftId) &&
            IsSafeMetadataToken(command.ProjectId) &&
            IsSafeMetadataToken(command.RequesterId) &&
            IsSafeMetadataToken(command.SourceActorId) &&
            IsSafeOptionalMetadataToken(command.SourceConversationId) &&
            IsSafeOptionalMetadataToken(command.SourceMessageId) &&
            IsSafeOptionalMetadataToken(command.SourceConversationItemId) &&
            command.RecipientRefs is { Count: > 0 } &&
            AllSafeMetadataTokens(command.RecipientRefs) &&
            command.ContextRefs is not null &&
            AllSafeMetadataTokens(command.ContextRefs) &&
            IsSafeMetadataToken(command.PolicySnapshotId) &&
            IsSafeMetadataToken(command.CorrelationId) &&
            command.SenderAuthorityClass is SenderAuthorityClass.DraftOnly &&
            !command.HasM365SendPosture &&
            IsValidOutboundDraftContent(command.GovernedContent) &&
            IsMetadataOnly(command.RedactionState, command.RetentionClass) &&
            IsSafeMetadataToken(command.SchemaVersion);

    private static bool IsValidOutboundDraftContent(OutboundDraftContent? content)
        => content is not null &&
            !string.IsNullOrWhiteSpace(content.Subject) &&
            content.Subject.Length <= 512 &&
            !string.IsNullOrWhiteSpace(content.ContentText) &&
            content.ContentText.Length <= 20000 &&
            content.ContentFormat is "text/plain" or "text/markdown" &&
            string.Equals(content.ContentRedactionState, "governed_content", StringComparison.Ordinal);

    private static bool IsEquivalentOutboundDraft(CreateOutboundDraft command, OutboundDraftCreated existing)
        => string.Equals(command.ProjectId, existing.ProjectId, StringComparison.Ordinal) &&
            string.Equals(command.RequesterId, existing.RequesterId, StringComparison.Ordinal) &&
            string.Equals(command.SourceActorId, existing.SourceActorId, StringComparison.Ordinal) &&
            string.Equals(command.SourceConversationId, existing.SourceConversationId, StringComparison.Ordinal) &&
            string.Equals(command.SourceMessageId, existing.SourceMessageId, StringComparison.Ordinal) &&
            string.Equals(command.SourceConversationItemId, existing.SourceConversationItemId, StringComparison.Ordinal) &&
            SameRefs(command.RecipientRefs, existing.RecipientRefs) &&
            SameRefs(command.ContextRefs, existing.ContextRefs) &&
            string.Equals(command.PolicySnapshotId, existing.PolicySnapshotId, StringComparison.Ordinal) &&
            command.SenderAuthorityClass == existing.SenderAuthorityClass &&
            string.Equals(command.GovernedContent.Subject, existing.GovernedContent.Subject, StringComparison.Ordinal) &&
            string.Equals(command.GovernedContent.ContentText, existing.GovernedContent.ContentText, StringComparison.Ordinal) &&
            string.Equals(command.GovernedContent.ContentFormat, existing.GovernedContent.ContentFormat, StringComparison.Ordinal) &&
            string.Equals(command.RedactionState, existing.RedactionState, StringComparison.Ordinal) &&
            string.Equals(command.RetentionClass, existing.RetentionClass, StringComparison.Ordinal);

    private static DomainResult RejectOutboundApprovalRequest(RequestOutboundSendApproval command, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new OutboundApprovalRequestRejected(
                SafeRejectionToken(command.ApprovalId),
                SafeRejectionToken(command.DraftId),
                SafeRejectionToken(command.ProjectId),
                SafeRejectionToken(reasonCode),
                SafeRejectionToken(command.CorrelationId)),
        });

    private static DomainResult RejectOutboundApprovalDecision(DecideOutboundApproval command, string reasonCode, long? sourceVersion)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new OutboundApprovalDecisionRejected(
                SafeRejectionToken(command.ApprovalId),
                SafeRejectionToken(command.DraftId),
                SafeRejectionToken(reasonCode),
                sourceVersion,
                SafeRejectionToken(command.CorrelationId)),
        });

    private static DomainResult RejectOutboundSend(
        ExecuteApprovedOutboundDraft command,
        string reasonCode,
        long? expectedApprovalSourceVersion,
        long? expectedDraftSourceVersion)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new OutboundSendRejected(
                SafeRejectionToken(command.SendId),
                SafeRejectionToken(command.ApprovalId),
                SafeRejectionToken(command.DraftId),
                SafeRejectionToken(command.ProjectId),
                SafeRejectionToken(command.RequesterId),
                SafeRejectionToken(command.SendActorId),
                SafeRejectionToken(reasonCode),
                SafeRejectionToken(command.CorrelationId),
                expectedApprovalSourceVersion,
                expectedDraftSourceVersion,
                SafeRejectionToken(command.RedactionState),
                SafeRejectionToken(command.RetentionClass)),
        });

    private static bool IsValidOutboundApprovalRequest(RequestOutboundSendApproval command)
        => IsSafeMetadataToken(command.ApprovalId) &&
            IsSafeMetadataToken(command.DraftId) &&
            IsSafeMetadataToken(command.ProjectId) &&
            IsSafeMetadataToken(command.RequesterId) &&
            IsSafeOptionalMetadataToken(command.SourceConversationId) &&
            IsSafeOptionalMetadataToken(command.SourceMessageId) &&
            IsSafeOptionalMetadataToken(command.SourceConversationItemId) &&
            command.RecipientRefs is { Count: > 0 } &&
            AllSafeMetadataTokens(command.RecipientRefs) &&
            command.ContextRefs is not null &&
            AllSafeMetadataTokens(command.ContextRefs) &&
            IsSafeMetadataToken(command.PolicySnapshotId) &&
            IsSafeMetadataToken(command.PolicySnapshotVisibility) &&
            IsSafeMetadataToken(command.CommandName) &&
            IsSafeMetadataToken(command.CommandAllowlistVersion) &&
            IsValidOutboundApprovalContentSnapshot(command.ContentSnapshot) &&
            command.SenderAuthorityClass is not SenderAuthorityClass.DraftOnly &&
            IsSafeMetadataToken(command.ExpectedPostStateRedactionState) &&
            command.ExpectedDraftSourceVersion > 0 &&
            IsSafeMetadataToken(command.CorrelationId) &&
            IsMetadataOnly(command.RedactionState, command.RetentionClass) &&
            IsSafeMetadataToken(command.SchemaVersion) &&
            (command.AuthorityResult is null || IsValidAuthorityResult(command.AuthorityResult));

    private static bool IsValidOutboundApprovalContentSnapshot(OutboundApprovalContentSnapshot? snapshot)
        => snapshot is not null &&
            IsValidOutboundDraftContent(snapshot.ProposedContent) &&
            (snapshot.ApprovedContent is null || IsValidOutboundDraftContent(snapshot.ApprovedContent)) &&
            string.Equals(snapshot.ProposedContentRedactionState, "governed_content", StringComparison.Ordinal) &&
            (snapshot.ApprovedContentRedactionState is null || string.Equals(snapshot.ApprovedContentRedactionState, "governed_content", StringComparison.Ordinal)) &&
            string.Equals(snapshot.PublicRedactionState, "metadata_only", StringComparison.Ordinal) &&
            IsSafeMetadataToken(snapshot.SchemaVersion);

    private static bool IsEquivalentOutboundApprovalRequest(RequestOutboundSendApproval command, OutboundApprovalRequested existing)
        => string.Equals(command.DraftId, existing.DraftId, StringComparison.Ordinal) &&
            string.Equals(command.ProjectId, existing.ProjectId, StringComparison.Ordinal) &&
            string.Equals(command.RequesterId, existing.RequesterId, StringComparison.Ordinal) &&
            string.Equals(command.SourceConversationId, existing.SourceConversationId, StringComparison.Ordinal) &&
            string.Equals(command.SourceMessageId, existing.SourceMessageId, StringComparison.Ordinal) &&
            string.Equals(command.SourceConversationItemId, existing.SourceConversationItemId, StringComparison.Ordinal) &&
            SameRefs(command.RecipientRefs, existing.RecipientRefs) &&
            SameRefs(command.ContextRefs, existing.ContextRefs) &&
            string.Equals(command.PolicySnapshotId, existing.PolicySnapshotId, StringComparison.Ordinal) &&
            string.Equals(command.CommandName, existing.CommandName, StringComparison.Ordinal) &&
            string.Equals(command.CommandAllowlistVersion, existing.CommandAllowlistVersion, StringComparison.Ordinal) &&
            command.SenderAuthorityClass == existing.SenderAuthorityClass &&
            command.EvidenceFreshness == existing.EvidenceFreshness &&
            command.ExpectedDraftSourceVersion == existing.ExpectedDraftSourceVersion;

    private static bool IsValidOutboundApprovalDecision(DecideOutboundApproval command)
        => IsSafeMetadataToken(command.ApprovalId) &&
            IsSafeMetadataToken(command.DraftId) &&
            IsSafeMetadataToken(command.ProjectId) &&
            IsSafeMetadataToken(command.DecisionId) &&
            command.ExpectedApprovalSourceVersion > 0 &&
            IsSafeMetadataToken(command.CorrelationId) &&
            (command.ApprovedContent is null || IsValidOutboundDraftContent(command.ApprovedContent)) &&
            IsSafeMetadataToken(command.DecisionRationaleRedactionState) &&
            IsSafeMetadataToken(command.SchemaVersion);

    private static bool IsValidOutboundSend(ExecuteApprovedOutboundDraft command)
        => IsSafeMetadataToken(command.SendId) &&
            IsSafeMetadataToken(command.ApprovalId) &&
            IsSafeMetadataToken(command.DraftId) &&
            IsSafeMetadataToken(command.ProjectId) &&
            IsSafeMetadataToken(command.RequesterId) &&
            IsSafeMetadataToken(command.SendActorId) &&
            IsSafeOptionalMetadataToken(command.SourceConversationId) &&
            IsSafeOptionalMetadataToken(command.SourceMessageId) &&
            IsSafeOptionalMetadataToken(command.SourceConversationItemId) &&
            command.RecipientRefs is { Count: > 0 } &&
            AllSafeMetadataTokens(command.RecipientRefs) &&
            command.ContextRefs is not null &&
            AllSafeMetadataTokens(command.ContextRefs) &&
            IsSafeMetadataToken(command.PolicySnapshotId) &&
            IsSafeMetadataToken(command.CommandName) &&
            IsSafeMetadataToken(command.CommandAllowlistVersion) &&
            command.SenderAuthorityClass is not SenderAuthorityClass.DraftOnly &&
            command.ExpectedApprovalSourceVersion > 0 &&
            command.ExpectedDraftSourceVersion > 0 &&
            IsSafeMetadataToken(command.CorrelationId) &&
            IsSafeMetadataToken(command.AdapterMode) &&
            IsSafeMetadataToken(command.AdapterStatus) &&
            IsSafeMetadataToken(command.AdapterRef) &&
            (command.AuthorityResult is null || IsValidAuthorityResult(command.AuthorityResult)) &&
            IsMetadataOnly(command.RedactionState, command.RetentionClass) &&
            IsSafeMetadataToken(command.SchemaVersion);

    private static bool IsValidAuthorityResult(SenderAuthorityClassificationResult result)
        => IsSafeMetadataToken(result.RequesterRef) &&
            IsSafeOptionalMetadataToken(result.MailboxRef) &&
            IsSafeOptionalMetadataToken(result.ServiceClientRef) &&
            IsSafeOptionalMetadataToken(result.PrincipalForRef) &&
            IsSafeOptionalMetadataToken(result.ApprovalRef) &&
            IsSafeMetadataToken(result.PolicySnapshotRef) &&
            IsSafeMetadataToken(result.EvidenceFreshness) &&
            result.AuditEvidenceRefs is { Count: > 0 } &&
            result.AuditEvidenceRefs.All(IsSafeAuthorityEvidenceRef) &&
            IsSafeOptionalMetadataToken(result.DenialReason);

    private static bool IsSafeAuthorityEvidenceRef(string value)
        => IsSafeMetadataToken(value) ||
            value.StartsWith("sender-authority:", StringComparison.Ordinal) &&
            SenderAuthorityClasses.AllWireValues.Any(authority => string.Equals(value, $"sender-authority:{authority}", StringComparison.Ordinal));

    private static string? ValidateOutboundSendApproval(
        ExecuteApprovedOutboundDraft command,
        OutboundApprovalRequested request,
        OutboundApprovalDecisionRecorded decision)
    {
        if (decision.DecisionKind is not ApprovalDecisionKind.Approve ||
            request.EvidenceFreshness is not ApprovalEvidenceFreshness.Fresh ||
            command.EvidenceFreshness is not ApprovalEvidenceFreshness.Fresh)
        {
            return ChatBotRefusalReasonCodes.ApprovalStateInvalid;
        }

        if (decision.SourceVersion != command.ExpectedApprovalSourceVersion ||
            request.ExpectedDraftSourceVersion != command.ExpectedDraftSourceVersion)
        {
            return "outbound_send_source_version_mismatch";
        }

        if (!string.Equals(request.DraftId, command.DraftId, StringComparison.Ordinal) ||
            !string.Equals(request.ProjectId, command.ProjectId, StringComparison.Ordinal) ||
            !string.Equals(request.RequesterId, command.RequesterId, StringComparison.Ordinal) ||
            !string.Equals(request.SourceConversationId, command.SourceConversationId, StringComparison.Ordinal) ||
            !string.Equals(request.SourceMessageId, command.SourceMessageId, StringComparison.Ordinal) ||
            !string.Equals(request.SourceConversationItemId, command.SourceConversationItemId, StringComparison.Ordinal) ||
            !string.Equals(request.PolicySnapshotId, command.PolicySnapshotId, StringComparison.Ordinal) ||
            !string.Equals(request.CommandName, command.CommandName, StringComparison.Ordinal) ||
            !string.Equals(request.CommandAllowlistVersion, command.CommandAllowlistVersion, StringComparison.Ordinal) ||
            request.SenderAuthorityClass != command.SenderAuthorityClass ||
            !SameRefs(request.RecipientRefs, command.RecipientRefs) ||
            !SameRefs(request.ContextRefs, command.ContextRefs))
        {
            return ChatBotRefusalReasonCodes.ApprovalStateInvalid;
        }

        return null;
    }

    private static string OutboundSendKey(string tenantId, string draftId, string sendActorId)
        => $"{tenantId}:{draftId}:{sendActorId}";

    private static bool SameRefs(IReadOnlyList<string> left, IReadOnlyList<string> right)
        => left.Order(StringComparer.Ordinal).SequenceEqual(right.Order(StringComparer.Ordinal), StringComparer.Ordinal);

    private static bool IsValidScore(double score)
        => double.IsFinite(score) && score >= 0.0 && score <= 1.0;

    private static bool IsValidPropagationCommand(
        string? associationId,
        string? intakeId,
        string? correctionId,
        string? workflowInstanceId,
        long sourceVersion,
        string? schemaVersion)
        => AssociationWorkflowId.TryParse(associationId, out _) &&
            MailboxMessageIntakeId.TryParse(intakeId, out _) &&
            !string.IsNullOrWhiteSpace(correctionId) &&
            !string.IsNullOrWhiteSpace(workflowInstanceId) &&
            sourceVersion > 0 &&
            !string.IsNullOrWhiteSpace(schemaVersion);

    private static bool IsMetadataOnly(string redactionState, string retentionClass)
        => string.Equals(redactionState, "metadata_only", StringComparison.Ordinal) &&
            string.Equals(retentionClass, "collaboration_input", StringComparison.Ordinal);

    private static bool RoutesToReview(AssociationScoringResult result)
        => result.Outcome is AssociationScoringOutcome.CandidatesGenerated or AssociationScoringOutcome.FailedClosed;

    private static bool IsValidReviewTransition()
        => LifecycleTransitionValidator
            .Validate(new LifecycleTransitionDefinition(LifecycleStates.Received, LifecycleStates.NeedsReview))
            .IsValid;

    private static bool IsAutoAssociatedButInvalid(
        ScoreMailboxMessageAssociation command,
        AssociationScoringResult result)
        => result.Outcome == AssociationScoringOutcome.AutoAssociated && !IsValidAutoAssociation(command, result);

    private static bool IsValidAutoAssociation(
        ScoreMailboxMessageAssociation command,
        AssociationScoringResult result)
    {
        if (command.ThresholdPolicy is null ||
            result.Outcome != AssociationScoringOutcome.AutoAssociated ||
            result.ThresholdBand != AssociationThresholdBand.Auto ||
            command.Candidates is not { Count: 1 })
        {
            return false;
        }

        AssociationCandidate candidate = command.Candidates[0];
        return candidate.RequiredEvidenceComplete &&
            IsValidScore(candidate.ConfidenceScore) &&
            candidate.ConfidenceScore >= command.ThresholdPolicy.THigh &&
            result.ConfidenceScore >= command.ThresholdPolicy.THigh &&
            Math.Abs(result.ConfidenceScore - candidate.ConfidenceScore) <= 0.000001 &&
            result.ReasonCodes.Contains(AssociationReasonCode.RequiredEvidencePresent);
    }

    private static bool IsConsistentAssociationResult(ScoreMailboxMessageAssociation command, CommandEnvelope envelope)
    {
        AssociationScoringResult result = command.Result!;
        return string.Equals(result.IntakeId, command.IntakeId, StringComparison.Ordinal) &&
            string.Equals(result.SourceMailboxId, command.SourceMailboxId, StringComparison.Ordinal) &&
            string.Equals(result.SourceConversationId, command.SourceConversationId, StringComparison.Ordinal) &&
            string.Equals(result.SourceThreadId, command.SourceThreadId, StringComparison.Ordinal) &&
            string.Equals(result.CorrelationId, envelope.CorrelationId, StringComparison.Ordinal) &&
            string.Equals(result.KernelVersion, command.ScoringKernelVersion, StringComparison.Ordinal) &&
            result.ReasonCodes is { Count: > 0 } &&
            !string.IsNullOrWhiteSpace(result.RedactionState) &&
            !string.IsNullOrWhiteSpace(result.RetentionClass) &&
            !string.IsNullOrWhiteSpace(result.SchemaVersion) &&
            result.DetectedAt != default &&
            IsValidExternalSender(result.ExternalSender) &&
            IsSafeOptionalMetadataToken(result.RoutingReason) &&
            (result.StrictnessPolicy is null ||
                IsSafeMetadataToken(result.StrictnessPolicy.PolicyVersion) &&
                IsSafeMetadataToken(result.StrictnessPolicy.ReasonCode));
    }

    private static AssociationDecisionValidation ValidateDecision(
        string associationId,
        string intakeId,
        AssociationDecisionKind actualKind,
        AssociationDecisionKind expectedKind,
        string candidateEvidenceFingerprint,
        long sourceVersion,
        string schemaVersion,
        string? decisionNote,
        GovernedOperationState? state)
    {
        if (!AssociationWorkflowId.TryParse(associationId, out _) ||
            !MailboxMessageIntakeId.TryParse(intakeId, out _) ||
            actualKind != expectedKind ||
            sourceVersion <= 0 ||
            string.IsNullOrWhiteSpace(schemaVersion) ||
            string.IsNullOrWhiteSpace(candidateEvidenceFingerprint))
        {
            return AssociationDecisionValidation.Invalid("invalid_association_decision_payload");
        }

        AssociationDecisionSourceSnapshot? source = state?.AssociationDecisionSource;
        if (source is null ||
            !string.Equals(source.AssociationId, associationId, StringComparison.Ordinal) ||
            !string.Equals(source.IntakeId, intakeId, StringComparison.Ordinal))
        {
            return AssociationDecisionValidation.Invalid("missing_association_evidence");
        }

        if (state!.AssociationDecisionIds.Contains(associationId))
        {
            return AssociationDecisionValidation.Invalid("association_already_decided");
        }

        if (state.AssociationLifecycleState != LifecycleState.NeedsReview)
        {
            return AssociationDecisionValidation.Invalid("invalid_association_lifecycle_transition");
        }

        if (source.SourceVersion != sourceVersion)
        {
            return AssociationDecisionValidation.Invalid("stale_evidence");
        }

        if (!EvidenceFingerprintMatches(AllEvidenceRefs(source), candidateEvidenceFingerprint))
        {
            return AssociationDecisionValidation.Invalid("stale_evidence");
        }

        if (!TrySanitizeDecisionNote(decisionNote, out string? sanitizedNote))
        {
            return AssociationDecisionValidation.Invalid("invalid_decision_note");
        }

        return AssociationDecisionValidation.Valid(source, sanitizedNote);
    }

    private static IReadOnlyList<AssociationEvidenceReference> AllEvidenceRefs(AssociationDecisionSourceSnapshot source)
        => source.Candidates
            .SelectMany(static candidate => candidate.EvidenceRefs)
            .Concat(source.Exclusions.Select(static exclusion => new AssociationEvidenceReference(
                exclusion.EvidenceReference,
                exclusion.EvidenceFingerprint,
                "association-exclusion")))
            .GroupBy(static evidence => evidence.EvidenceFingerprint, StringComparer.Ordinal)
            .Select(static group => group.First())
            .ToArray();

    private static bool EvidenceFingerprintMatches(
        IReadOnlyList<AssociationEvidenceReference> evidenceRefs,
        string fingerprint)
        => evidenceRefs.Any(evidence => string.Equals(evidence.EvidenceFingerprint, fingerprint, StringComparison.Ordinal));

    private static bool TrySanitizeDecisionNote(string? note, out string? sanitized)
    {
        sanitized = null;
        if (string.IsNullOrWhiteSpace(note))
        {
            return true;
        }

        string normalized = Regex.Replace(note.Normalize(NormalizationForm.FormC).Trim(), @"\s+", " ");
        if (normalized.Length > 1024 ||
            normalized.Any(char.IsControl) ||
            ContainsUnsafeNoteMarker(normalized))
        {
            return false;
        }

        sanitized = normalized;
        return true;
    }

    private static bool ContainsUnsafeNoteMarker(string value)
    {
        string[] markers = ["secret", "bearer ", "raw-body", "provider payload", "sender@", "/home/", "C:\\"];
        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static string ActorType(CommandEnvelope envelope, string fallback = "human")
        => envelope.Extensions is not null &&
            envelope.Extensions.TryGetValue("actorType", out string? actorType) &&
            !string.IsNullOrWhiteSpace(actorType)
                ? actorType
                : fallback;

    private static string SurfaceOrigin(CommandEnvelope envelope, string fallback = "api")
        => envelope.Extensions is not null &&
            envelope.Extensions.TryGetValue("surfaceOrigin", out string? origin) &&
            !string.IsNullOrWhiteSpace(origin)
                ? origin
                : fallback;

    private static DateTimeOffset DecisionTimestamp(CommandEnvelope envelope, DateTimeOffset fallback)
        => envelope.Extensions is not null &&
            envelope.Extensions.TryGetValue("decidedAt", out string? decidedAt) &&
            DateTimeOffset.TryParse(decidedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed)
                ? parsed.ToUniversalTime()
                : fallback.ToUniversalTime();

    private static bool RequiresApproval(AiActionProposalRecord proposal)
        => proposal.RiskClass is AiActionRiskClass.ApprovalRequired ||
            proposal.RiskActionClasses is { Count: > 0 };

    private static AiActionApprovalRequested ApprovalRequestedFromProposal(
        AiActionProposalRecord proposal,
        string taskIntentId,
        string requesterActorType,
        DateTimeOffset requestedAt)
    {
        string approvalId = ApprovalIdFor(proposal.ProposalId);
        return new AiActionApprovalRequested(
            approvalId,
            ProjectIdFromResources(proposal.AffectedResourceReferences),
            proposal.ProposalId,
            taskIntentId,
            proposal.SourceMessageId,
            proposal.SourceConversationItemId,
            proposal.RequesterId,
            requesterActorType,
            requestedAt.ToUniversalTime(),
            proposal.IntendedCommandName,
            proposal.CommandAllowlistVersion ?? "unavailable",
            proposal.RiskClass ?? AiActionRiskClass.ApprovalRequired,
            (proposal.RiskActionClasses ?? []).Select(AiRiskActionClassToken).ToArray(),
            RiskInputTupleRef(proposal.RiskInputTuple),
            proposal.PolicySnapshotId ?? "unavailable",
            "authorized",
            proposal.EvidenceReferences,
            EvidenceFreshnessFor(proposal.EvidenceReferences, proposal.ProposalInputMetadata),
            proposal.AffectedResourceReferences,
            proposal.RecipientReferences,
            proposal.RequesterAuthorityClass ?? "undeclared",
            "metadata_only",
            proposal.RedactionState,
            proposal.SourceVersion + 1,
            proposal.CorrelationId,
            proposal.RedactionState,
            proposal.RetentionClass);
    }

    private static AiActionApprovalRequested ApprovalRequestedFromLowRiskRoute(
        ExecuteLowRiskAIAssistance command,
        CommandEnvelope envelope,
        LowRiskAiAssistanceExecutionRecord record)
    {
        AiActionRiskClassificationRecord classification = command.RiskClassification!;
        return new AiActionApprovalRequested(
            ApprovalIdFor(command.ProposalId),
            command.ProjectId,
            command.ProposalId,
            command.TaskIntentId,
            command.SourceMessageId,
            command.SourceConversationItemId,
            command.RequesterId,
            ActorType(envelope, "human"),
            record.GeneratedAtUtc.ToUniversalTime(),
            AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName,
            classification.CommandAllowlistVersion,
            classification.RiskClass,
            classification.RiskActionClasses.Select(AiRiskActionClassToken).ToArray(),
            RiskInputTupleRef(classification.InputTuple),
            record.PolicySnapshotId,
            "authorized",
            command.SourceEvidenceReferences,
            Enumerable.Repeat(ApprovalEvidenceFreshness.Fresh, command.SourceEvidenceReferences.Count).ToArray(),
            command.AuthorizedContextReferences,
            [],
            classification.RequesterAuthorityClass,
            "metadata_only",
            record.GeneratedSummaryRedactionState,
            command.ExpectedProposalSourceVersion + 1,
            command.CorrelationId,
            command.RedactionState,
            command.RetentionClass);
    }

    private static string ApprovalIdFor(string proposalId)
        => $"approval:{proposalId}";

    private static string ProjectIdFromResources(IReadOnlyList<string> affectedResources)
    {
        string? project = affectedResources.FirstOrDefault(static value => value.StartsWith("project:", StringComparison.Ordinal));
        return string.IsNullOrWhiteSpace(project) ? "unavailable" : project["project:".Length..];
    }

    private static IReadOnlyList<ApprovalEvidenceFreshness> EvidenceFreshnessFor(
        IReadOnlyList<string> evidenceReferences,
        IReadOnlyDictionary<string, string>? metadata)
        => evidenceReferences
            .Select(reference => EvidenceFreshnessFor(reference, metadata))
            .ToArray();

    private static ApprovalEvidenceFreshness EvidenceFreshnessFor(
        string evidenceReference,
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null ||
            !metadata.TryGetValue($"evidence:{evidenceReference}:freshness", out string? value))
        {
            return ApprovalEvidenceFreshness.Expired;
        }

        return value switch
        {
            "fresh" => ApprovalEvidenceFreshness.Fresh,
            "stale" => ApprovalEvidenceFreshness.Stale,
            "expired" => ApprovalEvidenceFreshness.Expired,
            _ => ApprovalEvidenceFreshness.Expired,
        };
    }

    private static string? ApprovalDisabledReason(ApprovalDecisionKind decision, AiActionApprovalRequested request)
        => decision is ApprovalDecisionKind.Approve &&
            (request.EvidenceFreshnessStates.Count != request.EvidenceReferences.Count ||
                request.EvidenceFreshnessStates.Any(static state => state is ApprovalEvidenceFreshness.Expired))
                ? "evidence-expired"
                : null;

    private static string? ValidateApprovedExecutionApproval(
        ExecuteApprovedAIAction command,
        AiActionApprovalRequested request,
        AiActionApprovalDecisionRecorded decision)
    {
        if (!string.Equals(request.ProjectId, command.ProjectId, StringComparison.Ordinal) ||
            !string.Equals(request.ProposalId, command.ProposalId, StringComparison.Ordinal) ||
            !string.Equals(request.TaskIntentId, command.TaskIntentId, StringComparison.Ordinal) ||
            !string.Equals(request.SourceMessageId, command.SourceMessageId, StringComparison.Ordinal) ||
            !string.Equals(request.RequesterId, command.RequesterId, StringComparison.Ordinal))
        {
            return ChatBotRefusalReasonCodes.ApprovedCommandScopeExceeded;
        }

        if (!string.Equals(request.CommandName, command.CommandName, StringComparison.Ordinal))
        {
            return ChatBotRefusalReasonCodes.ApprovedCommandScopeExceeded;
        }

        if (!string.Equals(request.CommandAllowlistVersion, command.CommandAllowlistVersion, StringComparison.Ordinal))
        {
            return ChatBotRefusalReasonCodes.CommandNotAllowlisted;
        }

        if (request.SourceVersion != command.ExpectedProposalSourceVersion)
        {
            return ChatBotRefusalReasonCodes.ApprovalStateInvalid;
        }

        if (decision.SourceVersion != command.ExpectedApprovalSourceVersion)
        {
            return ChatBotRefusalReasonCodes.ApprovalStateInvalid;
        }

        if (!string.Equals(decision.ProjectId, command.ProjectId, StringComparison.Ordinal) ||
            !string.Equals(decision.ProposalId, command.ProposalId, StringComparison.Ordinal) ||
            !string.Equals(decision.SourceMessageId, command.SourceMessageId, StringComparison.Ordinal))
        {
            return ChatBotRefusalReasonCodes.ApprovalStateInvalid;
        }

        if (decision.DecisionKind is not ApprovalDecisionKind.Approve)
        {
            return ChatBotRefusalReasonCodes.ApprovalStateInvalid;
        }

        if (request.EvidenceFreshnessStates.Count != request.EvidenceReferences.Count ||
            request.EvidenceFreshnessStates.Any(static state => state is not ApprovalEvidenceFreshness.Fresh))
        {
            return ChatBotRefusalReasonCodes.EvidenceExpired;
        }

        if (!EquivalentMetadata(command.SourceEvidenceReferences, request.EvidenceReferences) ||
            !EquivalentMetadata(command.AffectedResourceReferences, request.AffectedResourceReferences) ||
            !EquivalentMetadata(command.RecipientReferences, request.RecipientReferences))
        {
            return ChatBotRefusalReasonCodes.ApprovedCommandScopeExceeded;
        }

        return null;
    }

    private static bool EquivalentMetadata(IReadOnlyList<string> left, IReadOnlyList<string> right)
        => left.Order(StringComparer.Ordinal).SequenceEqual(right.Order(StringComparer.Ordinal), StringComparer.Ordinal);

    private static bool IsEquivalentApprovedExecution(
        ExecuteApprovedAIAction command,
        ApprovedAiActionExecutionStarted existing)
        => string.Equals(existing.ProjectId, command.ProjectId, StringComparison.Ordinal) &&
            string.Equals(existing.ProposalId, command.ProposalId, StringComparison.Ordinal) &&
            string.Equals(existing.ApprovalId, command.ApprovalId, StringComparison.Ordinal) &&
            string.Equals(existing.TaskIntentId, command.TaskIntentId, StringComparison.Ordinal) &&
            string.Equals(existing.SourceMessageId, command.SourceMessageId, StringComparison.Ordinal) &&
            string.Equals(existing.RequesterId, command.RequesterId, StringComparison.Ordinal) &&
            string.Equals(existing.CommandName, command.CommandName, StringComparison.Ordinal) &&
            string.Equals(existing.CommandAllowlistVersion, command.CommandAllowlistVersion, StringComparison.Ordinal) &&
            existing.ExpectedApprovalSourceVersion == command.ExpectedApprovalSourceVersion &&
            existing.ExpectedProposalSourceVersion == command.ExpectedProposalSourceVersion;

    private static bool EquivalentInvalidation(
        AiActionProposalInvalidatedByCorrection existing,
        AiActionProposalInvalidatedByCorrection incoming)
        => string.Equals(existing.ProposalId, incoming.ProposalId, StringComparison.Ordinal) &&
            string.Equals(existing.ApprovalId, incoming.ApprovalId, StringComparison.Ordinal) &&
            string.Equals(existing.TaskIntentId, incoming.TaskIntentId, StringComparison.Ordinal) &&
            string.Equals(existing.SourceMessageId, incoming.SourceMessageId, StringComparison.Ordinal) &&
            string.Equals(existing.SourceConversationItemId, incoming.SourceConversationItemId, StringComparison.Ordinal) &&
            string.Equals(existing.RequesterId, incoming.RequesterId, StringComparison.Ordinal) &&
            string.Equals(existing.ProjectId, incoming.ProjectId, StringComparison.Ordinal) &&
            string.Equals(existing.AssociationId, incoming.AssociationId, StringComparison.Ordinal) &&
            string.Equals(existing.CorrectionId, incoming.CorrectionId, StringComparison.Ordinal) &&
            string.Equals(existing.CorrectedEvidenceState, incoming.CorrectedEvidenceState, StringComparison.Ordinal) &&
            existing.EvidenceSnapshotSourceVersion == incoming.EvidenceSnapshotSourceVersion &&
            string.Equals(existing.CorrelationId, incoming.CorrelationId, StringComparison.Ordinal) &&
            string.Equals(existing.RedactionState, incoming.RedactionState, StringComparison.Ordinal) &&
            string.Equals(existing.RetentionClass, incoming.RetentionClass, StringComparison.Ordinal);

    private static DomainResult RejectApprovalDecision(
        string approvalId,
        string proposalId,
        string reasonCode,
        long? expectedSourceVersion,
        string correlationId)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new AiActionApprovalDecisionRejected(approvalId, proposalId, reasonCode, expectedSourceVersion, correlationId),
        });

    private static DomainResult RejectProposalInvalidation(
        MarkAiActionProposalInvalidatedByCorrection command,
        string reasonCode,
        long? evidenceSnapshotSourceVersion)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new AiActionProposalInvalidationRejected(
                SafeRejectionToken(command.ProposalId),
                SafeOptionalRejectionToken(command.ApprovalId),
                reasonCode,
                evidenceSnapshotSourceVersion,
                SafeRejectionToken(command.CorrelationId)),
        });

    private static DomainResult RejectApprovedAiExecution(
        ExecuteApprovedAIAction command,
        string reasonCode,
        long? expectedApprovalSourceVersion)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new ApprovedAiActionExecutionRejected(
                SafeRejectionToken(command.ExecutionId),
                SafeRejectionToken(command.ProposalId),
                SafeRejectionToken(command.ApprovalId),
                SafeRejectionToken(command.ProjectId),
                SafeRejectionToken(command.TaskIntentId),
                SafeRejectionToken(command.SourceMessageId),
                SafeOptionalRejectionToken(command.SourceConversationItemId),
                SafeRejectionToken(command.RequesterId),
                SafeRejectionToken(command.CommandName),
                SafeRejectionToken(command.CommandAllowlistVersion),
                reasonCode,
                expectedApprovalSourceVersion,
                SafeRejectionToken(command.CorrelationId),
                SafeOptionalRejectionToken(command.PolicySnapshotId),
                SafeRejectionToken(command.RedactionState),
                SafeRejectionToken(command.RetentionClass)),
        });

    private static string RiskInputTupleRef(AiActionRiskInputTuple? tuple)
        => tuple is null
            ? "tuple:unavailable"
            : $"tuple:{tuple.IntendedCommandName}:{tuple.EffectSurface}:{tuple.RequesterAuthorityClass}:{tuple.TenantPolicyClassification}";

    private static string AiRiskActionClassToken(AiActionRiskActionClass value)
        => value switch
        {
            AiActionRiskActionClass.ModifiesState => "modifies-state",
            AiActionRiskActionClass.ExposesFiles => "exposes-files",
            AiActionRiskActionClass.SendsExternal => "sends-external",
            AiActionRiskActionClass.CreatesTasks => "creates-tasks",
            AiActionRiskActionClass.InvokesTools => "invokes-tools",
            AiActionRiskActionClass.ActsOnBehalf => "acts-on-behalf",
            _ => "unknown",
        };

    private static bool IsValidTransitionIdentity(string projectId, string taskIntentId, string sourceMessageId, string transitionId, string correlationId)
        => IsSafeMetadataToken(projectId) &&
            IsSafeMetadataToken(taskIntentId) &&
            IsSafeMetadataToken(sourceMessageId) &&
            IsSafeMetadataToken(transitionId) &&
            IsSafeMetadataToken(correlationId);

    private static bool IsValidTenantPolicyChange(SubmitTenantPolicyChange command)
        => IsSafeMetadataToken(command.PolicyChangeId) &&
            IsSafeMetadataToken(command.SourcePolicySnapshotId) &&
            IsSafeMetadataToken(command.ProposedPolicySnapshotId) &&
            command.SourceVersion >= 0 &&
            command.ChangedKnobIds is { Count: > 0 } &&
            command.ChangedKnobIds.All(static knob => TenantPolicySchema.TryGetDefinition(knob, out _)) &&
            command.ChangeSet?.Values is { Count: > 0 } &&
            command.ChangedKnobIds.ToHashSet(StringComparer.Ordinal).SetEquals(command.ChangeSet.Values.Select(static value => value.KnobId)) &&
            TenantPolicySchema.Validate(command.ChangeSet).IsValid &&
            IsSafeMetadataToken(command.ReasonCode) &&
            IsSafeMetadataToken(command.RequesterRef) &&
            TenantPolicySchemaVersions.IsKnown(command.SchemaVersion) &&
            IsSafeMetadataToken(command.CorrelationId) &&
            IsSafeMetadataToken(command.OldValueFingerprint) &&
            IsSafeMetadataToken(command.NewValueFingerprint);

    private static bool IsValidTenantPolicyApproval(ApproveTenantPolicyChange command)
        => IsSafeMetadataToken(command.PolicyChangeId) &&
            IsSafeMetadataToken(command.PendingPolicySnapshotId) &&
            IsSafeMetadataToken(command.ActivatedPolicySnapshotId) &&
            command.SourceVersion >= 0 &&
            command.ChangedKnobIds is { Count: > 0 } &&
            command.ChangedKnobIds.All(static knob => TenantPolicySchema.TryGetDefinition(knob, out _)) &&
            IsSafeMetadataToken(command.ReasonCode) &&
            IsSafeMetadataToken(command.RequesterRef) &&
            IsSafeMetadataToken(command.ApproverRef) &&
            !string.Equals(command.RequesterRef, command.ApproverRef, StringComparison.Ordinal) &&
            TenantPolicySchemaVersions.IsKnown(command.SchemaVersion) &&
            IsSafeMetadataToken(command.CorrelationId);

    private static bool IsValidMailboxSourceDisable(SubmitMailboxSourceDisable command)
        => IsSafeMetadataToken(command.DisableChangeId) &&
            IsSafeMetadataToken(command.MailboxSourceRef) &&
            IsSafeMetadataToken(command.ReasonCode) &&
            IsSafeMetadataToken(command.PolicySnapshotId) &&
            IsSafeMetadataToken(command.RequesterRef) &&
            command.SourceVersion >= 0 &&
            command.OldState == MailboxSourceControlState.Active &&
            command.NewState == MailboxSourceControlState.Disabled &&
            MailboxSourceControlSchemaVersions.IsKnown(command.SchemaVersion) &&
            IsSafeMetadataToken(command.CorrelationId);

    private static bool IsValidMailboxSourceDisableApproval(ApproveMailboxSourceDisable command)
        => IsSafeMetadataToken(command.DisableChangeId) &&
            IsSafeMetadataToken(command.MailboxSourceRef) &&
            IsSafeMetadataToken(command.ReasonCode) &&
            IsSafeMetadataToken(command.PolicySnapshotId) &&
            IsSafeMetadataToken(command.RequesterRef) &&
            IsSafeMetadataToken(command.ApproverRef) &&
            !string.Equals(command.RequesterRef, command.ApproverRef, StringComparison.Ordinal) &&
            command.SourceVersion >= 0 &&
            command.OldState == MailboxSourceControlState.Active &&
            command.NewState == MailboxSourceControlState.Disabled &&
            MailboxSourceControlSchemaVersions.IsKnown(command.SchemaVersion) &&
            IsSafeMetadataToken(command.CorrelationId);

    private static bool IsValidServiceClientDisable(SubmitServiceClientDisable command)
        => IsSafeMetadataToken(command.DisableChangeId) &&
            IsSafeMetadataToken(command.ServiceClientRef) &&
            IsSafeMetadataToken(command.ReasonCode) &&
            IsSafeMetadataToken(command.PolicySnapshotId) &&
            IsSafeMetadataToken(command.RequesterRef) &&
            command.SourceVersion >= 0 &&
            command.OldState == ServiceClientControlState.Active &&
            command.NewState == ServiceClientControlState.Disabled &&
            ServiceClientControlSchemaVersions.IsKnown(command.SchemaVersion) &&
            IsSafeMetadataToken(command.CorrelationId);

    private static bool IsValidServiceClientDisableApproval(ApproveServiceClientDisable command)
        => IsSafeMetadataToken(command.DisableChangeId) &&
            IsSafeMetadataToken(command.ServiceClientRef) &&
            IsSafeMetadataToken(command.ReasonCode) &&
            IsSafeMetadataToken(command.PolicySnapshotId) &&
            IsSafeMetadataToken(command.RequesterRef) &&
            IsSafeMetadataToken(command.ApproverRef) &&
            !string.Equals(command.RequesterRef, command.ApproverRef, StringComparison.Ordinal) &&
            command.SourceVersion >= 0 &&
            command.OldState == ServiceClientControlState.Active &&
            command.NewState == ServiceClientControlState.Disabled &&
            ServiceClientControlSchemaVersions.IsKnown(command.SchemaVersion) &&
            IsSafeMetadataToken(command.CorrelationId);

    private static bool IsValidServiceClientQuarantine(SubmitServiceClientQuarantine command)
        => IsSafeMetadataToken(command.QuarantineChangeId) &&
            IsSafeMetadataToken(command.ServiceClientRef) &&
            IsSafeMetadataToken(command.ReasonCode) &&
            IsSafeMetadataToken(command.PolicySnapshotId) &&
            IsSafeMetadataToken(command.RequesterRef) &&
            command.SourceVersion >= 0 &&
            command.OldState == ServiceClientControlState.Active &&
            command.NewState == ServiceClientControlState.Quarantined &&
            ServiceClientControlSchemaVersions.IsKnown(command.SchemaVersion) &&
            IsSafeMetadataToken(command.CorrelationId);

    private static bool IsValidServiceClientQuarantineApproval(ApproveServiceClientQuarantine command)
        => IsSafeMetadataToken(command.QuarantineChangeId) &&
            IsSafeMetadataToken(command.ServiceClientRef) &&
            IsSafeMetadataToken(command.ReasonCode) &&
            IsSafeMetadataToken(command.PolicySnapshotId) &&
            IsSafeMetadataToken(command.RequesterRef) &&
            IsSafeMetadataToken(command.ApproverRef) &&
            !string.Equals(command.RequesterRef, command.ApproverRef, StringComparison.Ordinal) &&
            command.SourceVersion >= 0 &&
            command.OldState == ServiceClientControlState.Active &&
            command.NewState == ServiceClientControlState.Quarantined &&
            ServiceClientControlSchemaVersions.IsKnown(command.SchemaVersion) &&
            IsSafeMetadataToken(command.CorrelationId);

    private static bool IsValidMailboxSourceQuarantine(SubmitMailboxSourceQuarantine command)
        => IsSafeMetadataToken(command.QuarantineChangeId) &&
            IsSafeMetadataToken(command.MailboxSourceRef) &&
            IsSafeMetadataToken(command.ReasonCode) &&
            IsSafeMetadataToken(command.PolicySnapshotId) &&
            IsSafeMetadataToken(command.RequesterRef) &&
            command.SourceVersion >= 0 &&
            command.OldState == MailboxSourceControlState.Active &&
            command.NewState == MailboxSourceControlState.Quarantined &&
            MailboxSourceControlSchemaVersions.IsKnown(command.SchemaVersion) &&
            IsSafeMetadataToken(command.CorrelationId);

    private static bool IsValidMailboxSourceQuarantineApproval(ApproveMailboxSourceQuarantine command)
        => IsSafeMetadataToken(command.QuarantineChangeId) &&
            IsSafeMetadataToken(command.MailboxSourceRef) &&
            IsSafeMetadataToken(command.ReasonCode) &&
            IsSafeMetadataToken(command.PolicySnapshotId) &&
            IsSafeMetadataToken(command.RequesterRef) &&
            IsSafeMetadataToken(command.ApproverRef) &&
            !string.Equals(command.RequesterRef, command.ApproverRef, StringComparison.Ordinal) &&
            command.SourceVersion >= 0 &&
            command.OldState == MailboxSourceControlState.Active &&
            command.NewState == MailboxSourceControlState.Quarantined &&
            MailboxSourceControlSchemaVersions.IsKnown(command.SchemaVersion) &&
            IsSafeMetadataToken(command.CorrelationId);

    private static bool IsValidMailboxSourceRateLimit(SubmitMailboxSourceRateLimit command)
        => IsSafeMetadataToken(command.RateLimitChangeId) &&
            IsSafeMetadataToken(command.MailboxSourceRef) &&
            IsSafeMetadataToken(command.ReasonCode) &&
            IsSafeMetadataToken(command.PolicySnapshotId) &&
            IsSafeMetadataToken(command.RequesterRef) &&
            command.SourceVersion >= 0 &&
            command.OldBudget >= MailboxRateLimitBounds.Minimum &&
            command.NewBudget >= MailboxRateLimitBounds.Minimum &&
            Enum.IsDefined(command.Window) &&
            MailboxSourceRateLimitSchemaVersions.IsKnown(command.SchemaVersion) &&
            IsSafeMetadataToken(command.CorrelationId);

    private static bool IsValidServiceClientRateLimit(SubmitServiceClientRateLimit command)
        => IsSafeMetadataToken(command.RateLimitChangeId) &&
            IsSafeMetadataToken(command.ServiceClientRef) &&
            IsSafeMetadataToken(command.ReasonCode) &&
            IsSafeMetadataToken(command.PolicySnapshotId) &&
            IsSafeMetadataToken(command.RequesterRef) &&
            command.SourceVersion >= 0 &&
            command.OldBudget >= ServiceClientRateLimitBounds.Minimum &&
            command.NewBudget >= ServiceClientRateLimitBounds.Minimum &&
            Enum.IsDefined(command.Window) &&
            ServiceClientRateLimitSchemaVersions.IsKnown(command.SchemaVersion) &&
            IsSafeMetadataToken(command.CorrelationId);

    private static string? ValidateCapturedRecord(string tenantId, string projectId, string sourceMessageId, long expectedSourceVersion, TaskIntentRecord record)
    {
        if (!string.Equals(record.TenantId, tenantId, StringComparison.Ordinal) ||
            !string.Equals(record.ProjectId, projectId, StringComparison.Ordinal) ||
            !string.Equals(record.SourceMessageId, sourceMessageId, StringComparison.Ordinal))
        {
            return TaskIntentReasonCodes.MissingCapturedIntent;
        }

        if (record.SourceVersion != expectedSourceVersion)
        {
            return TaskIntentReasonCodes.SourceVersionMismatch;
        }

        if (record.State is TaskIntentState.Converted)
        {
            return TaskIntentReasonCodes.AlreadyConverted;
        }

        if (record.State is TaskIntentState.NotActionable or TaskIntentState.Duplicate or TaskIntentState.AlreadyHandled or TaskIntentState.OutOfScope)
        {
            return TaskIntentReasonCodes.TerminalState;
        }

        if (record.State is not TaskIntentState.Captured)
        {
            return TaskIntentReasonCodes.UnsupportedTransition;
        }

        if (record.ConversionReadinessBlocked)
        {
            return TaskIntentReasonCodes.StaleCorrectedContext;
        }

        return null;
    }

    private static bool IsValidDuplicatePredecessor(MarkTaskIntentDisposition command, GovernedOperationState state, string tenantId)
        => !string.IsNullOrWhiteSpace(command.PredecessorTaskIntentId) &&
            !string.Equals(command.PredecessorTaskIntentId, command.TaskIntentId, StringComparison.Ordinal) &&
            state.TaskIntents.TryGetValue(command.PredecessorTaskIntentId, out TaskIntentRecord? predecessor) &&
            string.Equals(predecessor.TenantId, tenantId, StringComparison.Ordinal) &&
            string.Equals(predecessor.ProjectId, command.ProjectId, StringComparison.Ordinal);

    private static bool TryDispositionState(string value, out TaskIntentState state)
    {
        state = value switch
        {
            "not-actionable" => TaskIntentState.NotActionable,
            "duplicate" => TaskIntentState.Duplicate,
            "already-handled" => TaskIntentState.AlreadyHandled,
            "out-of-scope" => TaskIntentState.OutOfScope,
            _ => default,
        };
        return state is TaskIntentState.NotActionable or TaskIntentState.Duplicate or TaskIntentState.AlreadyHandled or TaskIntentState.OutOfScope;
    }

    private static DomainResult RejectTransition(string taskIntentId, string projectId, string transitionId, string reasonCode, long? sourceVersion, string correlationId)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new TaskIntentTransitionRejected(taskIntentId, projectId, transitionId, reasonCode, sourceVersion, correlationId),
        });

    private static DomainResult RejectTenantPolicyChange(string? policyChangeId, string reasonCode, long? sourceVersion, string? correlationId)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new TenantPolicyChangeRejected(
                SafeRejectionToken(policyChangeId),
                reasonCode,
                sourceVersion,
                SafeRejectionToken(correlationId)),
        });

    private static DomainResult RejectMailboxSourceDisable(string? disableChangeId, string reasonCode, long? sourceVersion, string? correlationId)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new MailboxSourceDisableRejected(
                SafeRejectionToken(disableChangeId),
                reasonCode,
                sourceVersion,
                SafeRejectionToken(correlationId)),
        });

    private static DomainResult RejectServiceClientDisable(string? disableChangeId, string reasonCode, long? sourceVersion, string? correlationId)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new ServiceClientDisableRejected(
                SafeRejectionToken(disableChangeId),
                reasonCode,
                sourceVersion,
                SafeRejectionToken(correlationId)),
        });

    private static DomainResult RejectServiceClientQuarantine(string? quarantineChangeId, string reasonCode, long? sourceVersion, string? correlationId)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new ServiceClientQuarantineRejected(
                SafeRejectionToken(quarantineChangeId),
                reasonCode,
                sourceVersion,
                SafeRejectionToken(correlationId)),
        });

    private static DomainResult RejectMailboxSourceQuarantine(string? quarantineChangeId, string reasonCode, long? sourceVersion, string? correlationId)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new MailboxSourceQuarantineRejected(
                SafeRejectionToken(quarantineChangeId),
                reasonCode,
                sourceVersion,
                SafeRejectionToken(correlationId)),
        });

    private static DomainResult RejectMailboxSourceRateLimit(string? rateLimitChangeId, string reasonCode, long? sourceVersion, string? correlationId)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new MailboxSourceRateLimitRejected(
                SafeRejectionToken(rateLimitChangeId),
                reasonCode,
                sourceVersion,
                SafeRejectionToken(correlationId)),
        });

    private static DomainResult RejectServiceClientRateLimit(string? rateLimitChangeId, string reasonCode, long? sourceVersion, string? correlationId)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new ServiceClientRateLimitRejected(
                SafeRejectionToken(rateLimitChangeId),
                reasonCode,
                sourceVersion,
                SafeRejectionToken(correlationId)),
        });

    private static bool IsSafeMetadataToken(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
            value.Length <= 280 &&
            value.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');

    private static bool IsSafeOptionalMetadataToken(string? value)
        => value is null || IsSafeMetadataToken(value);

    private static string SafeRejectionToken(string? value)
        => IsSafeMetadataToken(value) ? value! : "unavailable";

    private static string? SafeOptionalRejectionToken(string? value)
        => IsSafeOptionalMetadataToken(value) ? value : null;

    private static bool AllSafeMetadataTokens(IReadOnlyList<string> values)
        => values.All(static value => IsSafeMetadataToken(value));

    private static bool IsValidAuthenticity(MailboxAuthenticityMetadata? authenticity)
        => authenticity is null ||
            authenticity.AuthenticationResults is not null &&
            authenticity.HeaderInspection is not null &&
            IsSafeOptionalMetadataToken(authenticity.AuthenticationResults.CompositeAuthenticationReason) &&
            IsValidSelectedHeaders(authenticity.AuthenticationResults.AuthenticationResultsHeaders, "Authentication-Results") &&
            IsValidSelectedHeaders(authenticity.HeaderInspection.AuthenticationResultsHeaders, "Authentication-Results") &&
            IsValidSelectedHeaders(authenticity.HeaderInspection.ReceivedHeaders, "Received") &&
            authenticity.HeaderInspection.Discrepancies is not null &&
            authenticity.HeaderInspection.Discrepancies.Count <= 32 &&
            authenticity.HeaderInspection.Discrepancies.Distinct().Count() == authenticity.HeaderInspection.Discrepancies.Count &&
            (authenticity.StrictnessPolicy is null ||
                IsSafeMetadataToken(authenticity.StrictnessPolicy.PolicyVersion) &&
                IsSafeMetadataToken(authenticity.StrictnessPolicy.ReasonCode));

    private static bool IsValidDelegatedSender(MailboxDelegatedSenderSnapshot? delegatedSender)
        => delegatedSender is null ||
            delegatedSender.EvidenceRefs is not null &&
            delegatedSender.Discrepancies is not null &&
            Enum.IsDefined(delegatedSender.State) &&
            delegatedSender.EvidenceRefs.Count > 0 &&
            delegatedSender.EvidenceRefs.Count <= 32 &&
            delegatedSender.Discrepancies.Count <= 32 &&
            delegatedSender.EvidenceRefs.Distinct(StringComparer.Ordinal).Count() == delegatedSender.EvidenceRefs.Count &&
            delegatedSender.Discrepancies.Distinct().Count() == delegatedSender.Discrepancies.Count &&
            delegatedSender.EvidenceRefs.All(IsSafeMetadataToken) &&
            IsSafeParticipant(delegatedSender.Delegate) &&
            IsSafeParticipant(delegatedSender.PrincipalFor) &&
            IsConsistentDelegatedSenderState(delegatedSender);

    private static bool IsValidExternalSender(MailboxExternalSenderPosture? externalSender)
        => externalSender is null ||
            externalSender.EvidenceRefs is not null &&
            Enum.IsDefined(externalSender.PartyResolutionState) &&
            externalSender.EvidenceRefs.Count > 0 &&
            externalSender.EvidenceRefs.Count <= 32 &&
            externalSender.EvidenceRefs.Distinct(StringComparer.Ordinal).Count() == externalSender.EvidenceRefs.Count &&
            externalSender.EvidenceRefs.All(IsSafeMetadataToken) &&
            IsSafeOptionalMetadataToken(externalSender.ResolvedPartyRef) &&
            IsConsistentExternalSenderState(externalSender);

    private static bool IsConsistentDelegatedSenderState(MailboxDelegatedSenderSnapshot delegatedSender)
        => delegatedSender.State switch
        {
            MailboxDelegatedSenderState.Delegated or MailboxDelegatedSenderState.Ambiguous =>
                delegatedSender.Delegate is not null &&
                delegatedSender.PrincipalFor is not null &&
                !string.Equals(delegatedSender.Delegate.Address, delegatedSender.PrincipalFor.Address, StringComparison.OrdinalIgnoreCase),
            MailboxDelegatedSenderState.NotDelegated or MailboxDelegatedSenderState.NotSupplied =>
                delegatedSender.Delegate is null &&
                delegatedSender.PrincipalFor is null,
            _ => false,
        };

    private static bool IsConsistentExternalSenderState(MailboxExternalSenderPosture externalSender)
        => externalSender.ExternalSender
            ? externalSender.PartyResolutionState is
                MailboxPartyResolutionState.ResolvedExternal or
                MailboxPartyResolutionState.Unresolved or
                MailboxPartyResolutionState.Ambiguous or
                MailboxPartyResolutionState.Unavailable
            : externalSender.PartyResolutionState == MailboxPartyResolutionState.ResolvedInternal &&
                !string.IsNullOrWhiteSpace(externalSender.ResolvedPartyRef);

    private static bool IsSafeParticipant(MailboxParticipantIdentity? participant)
        => participant is null ||
            IsSafeMailboxAddress(participant.Address) &&
            IsSafeOptionalDisplayName(participant.DisplayName);

    private static bool IsSafeMailboxAddress(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
            value.Length <= 320 &&
            value.All(static character =>
                char.IsAsciiLetterOrDigit(character) ||
                character is '@' or '.' or '-' or '_' or '+');

    private static bool IsSafeOptionalDisplayName(string? value)
        => value is null ||
            value.Length <= 320 &&
            value.All(static character => !char.IsControl(character));

    private static bool IsValidSelectedHeaders(IReadOnlyList<MailboxSelectedHeaderSnapshot>? headers, string expectedName)
        => headers is not null &&
            headers.Count <= 64 &&
            headers.Select(static (header, index) => (header, index))
                .All(item => item.header is not null &&
                    item.header.Ordinal == item.index &&
                    string.Equals(item.header.Name, expectedName, StringComparison.Ordinal));

    private static bool IsSafeOptionalMetadataMap(IReadOnlyDictionary<string, string>? values)
        => values is null ||
            values.Count <= 32 &&
            values.All(static item => IsSafeMetadataToken(item.Key) && IsSafeMetadataToken(item.Value));

    private static string? MetadataValue(IReadOnlyDictionary<string, string>? values, string key)
        => values is not null &&
            values.TryGetValue(key, out string? value) &&
            IsSafeMetadataToken(value)
                ? value
                : null;

    private static long? MetadataLong(IReadOnlyDictionary<string, string>? values, string key)
        => values is not null &&
            values.TryGetValue(key, out string? value) &&
            long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out long parsed) &&
            parsed > 0
                ? parsed
                : null;

    private static bool IsSafeClassification(AiActionRiskClassificationRecord? classification, ProposeAIAction command)
        => classification is not null &&
            !classification.Rejected &&
            classification.RiskClass is AiActionRiskClass.LowRisk or AiActionRiskClass.ApprovalRequired &&
            classification.RiskActionClasses is not null &&
            classification.RiskActionClasses.Count <= 16 &&
            classification.RiskActionClasses.All(IsKnownAiActionRiskActionClass) &&
            IsSafeMetadataToken(classification.ClassifierVersion) &&
            classification.InputTuple is not null &&
            IsSafeMetadataToken(classification.InputTuple.IntendedCommandName) &&
            string.Equals(classification.InputTuple.IntendedCommandName, command.IntendedCommandName, StringComparison.Ordinal) &&
            IsSafeOptionalMetadataToken(classification.InputTuple.EffectSurface) &&
            IsSafeOptionalMetadataToken(classification.InputTuple.TenantPolicyClassification) &&
            IsSafeOptionalMetadataToken(classification.InputTuple.RequesterAuthorityClass) &&
            IsSafeOptionalMetadataToken(classification.InputTuple.PolicySnapshotId) &&
            IsSafeOptionalMetadataToken(classification.InputTuple.CommandAllowlistVersion) &&
            IsSafeOptionalMetadataToken(classification.InputTuple.AllowlistMetadataState) &&
            IsSafeOptionalMetadataToken(classification.InputTuple.ProjectAuthorizationState) &&
            IsSafeMetadataToken(classification.InputTuple.CorrelationId) &&
            IsSafeOptionalMetadataToken(classification.PolicySnapshotId) &&
            IsSafeMetadataToken(classification.CommandAllowlistVersion) &&
            IsSafeMetadataToken(classification.RequesterAuthorityClass) &&
            IsSafeMetadataToken(classification.ReasonCode) &&
            IsSafeMetadataToken(classification.RedactionState) &&
            IsSafeMetadataToken(classification.RetentionClass) &&
            IsSafeMetadataToken(classification.SchemaVersion) &&
            IsSafeMetadataToken(classification.CorrelationId) &&
            IsSafeOptionalMetadataToken(classification.IndeterminateReason) &&
            classification.ProducedAtUtc != default;

    private static bool IsSafeExecutionClassification(AiActionRiskClassificationRecord? classification)
        => classification is not null &&
            !classification.Rejected &&
            classification.RiskClass is AiActionRiskClass.LowRisk &&
            classification.RiskActionClasses is not null &&
            classification.RiskActionClasses.Count == 0 &&
            IsSafeMetadataToken(classification.ClassifierVersion) &&
            classification.InputTuple is not null &&
            string.Equals(classification.InputTuple.IntendedCommandName, AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName, StringComparison.Ordinal) &&
            string.Equals(classification.InputTuple.EffectSurface, "read-only", StringComparison.Ordinal) &&
            IsSafeOptionalMetadataToken(classification.InputTuple.TenantPolicyClassification) &&
            IsSafeOptionalMetadataToken(classification.InputTuple.RequesterAuthorityClass) &&
            IsSafeOptionalMetadataToken(classification.InputTuple.PolicySnapshotId) &&
            IsSafeMetadataToken(classification.InputTuple.CorrelationId) &&
            IsSafeMetadataToken(classification.CommandAllowlistVersion) &&
            IsSafeMetadataToken(classification.RequesterAuthorityClass) &&
            IsSafeMetadataToken(classification.ReasonCode) &&
            IsSafeMetadataToken(classification.RedactionState) &&
            IsSafeMetadataToken(classification.RetentionClass) &&
            IsSafeMetadataToken(classification.SchemaVersion) &&
            IsSafeMetadataToken(classification.CorrelationId) &&
            classification.ProducedAtUtc != default;

    private static bool IsSafeExecutionRecord(LowRiskAiAssistanceExecutionRecord? record, ExecuteLowRiskAIAssistance command)
        => record is not null &&
            string.Equals(record.ExecutionId, command.ExecutionId, StringComparison.Ordinal) &&
            string.Equals(record.ProposalId, command.ProposalId, StringComparison.Ordinal) &&
            record.Outcome is "success" or "failed" or "pending-approval" &&
            IsSafeMetadataToken(record.ProviderName) &&
            IsSafeMetadataToken(record.ModelVersion) &&
            record.GeneratedAtUtc != default &&
            record.SourceEvidenceIds is not null &&
            AllSafeMetadataTokens(record.SourceEvidenceIds) &&
            string.Equals(record.ContextPackageId, command.ContextPackageId, StringComparison.Ordinal) &&
            string.Equals(record.ContextPackageVersion, command.ContextPackageVersion, StringComparison.Ordinal) &&
            IsSafeMetadataToken(record.ContextRedactionState) &&
            IsSafeMetadataToken(record.PolicySnapshotId) &&
            IsSafeMetadataToken(record.PolicyReasonCode) &&
            IsSafeMetadataToken(record.AuditOperationId) &&
            IsSafeMetadataToken(record.AuditStatus) &&
            IsSafeMetadataToken(record.CorrelationId) &&
            IsSafeMetadataToken(record.GeneratedSummaryRedactionState) &&
            IsSafeMetadataToken(record.GeneratedContentVisibility) &&
            IsValidExecutionNextAction(record) &&
            IsSafeOptionalMetadataToken(record.FailureCode) &&
            IsSafeOptionalMetadataToken(record.Retryability) &&
            IsSafeMetadataToken(record.RedactionState) &&
            IsSafeMetadataToken(record.RetentionClass) &&
            IsSafeMetadataToken(record.SchemaVersion);

    private static bool IsValidApprovedExecutionPayload(ExecuteApprovedAIAction command)
        => IsSafeMetadataToken(command.ProjectId) &&
            IsSafeMetadataToken(command.ProposalId) &&
            IsSafeMetadataToken(command.ApprovalId) &&
            IsSafeMetadataToken(command.TaskIntentId) &&
            IsSafeMetadataToken(command.SourceMessageId) &&
            IsSafeMetadataToken(command.RequesterId) &&
            IsSafeMetadataToken(command.CommandName) &&
            IsSafeMetadataToken(command.CommandAllowlistVersion) &&
            command.ExpectedApprovalSourceVersion > 0 &&
            command.ExpectedProposalSourceVersion > 0 &&
            IsSafeMetadataToken(command.CorrelationId) &&
            IsSafeMetadataToken(command.ExecutionId) &&
            IsSafeMetadataToken(command.TransitionId) &&
            command.SourceEvidenceReferences is { Count: > 0 } &&
            AllSafeMetadataTokens(command.SourceEvidenceReferences) &&
            command.AffectedResourceReferences is not null &&
            AllSafeMetadataTokens(command.AffectedResourceReferences) &&
            command.RecipientReferences is not null &&
            AllSafeMetadataTokens(command.RecipientReferences) &&
            IsSafeOptionalMetadataToken(command.SourceConversationItemId) &&
            IsSafeOptionalMetadataToken(command.PolicySnapshotId) &&
            IsSafeMetadataToken(command.ActionSummaryRedactionState) &&
            IsSafeMetadataToken(command.RedactionState) &&
            IsSafeMetadataToken(command.RetentionClass) &&
            IsSafeMetadataToken(command.SchemaVersion) &&
            IsSafeApprovedExecutionRecord(command.ExecutionRecord, command);

    private static bool IsSafeApprovedExecutionRecord(ApprovedAiActionExecutionRecord? record, ExecuteApprovedAIAction command)
        => record is not null &&
            string.Equals(record.ExecutionId, command.ExecutionId, StringComparison.Ordinal) &&
            string.Equals(record.ProposalId, command.ProposalId, StringComparison.Ordinal) &&
            string.Equals(record.ApprovalId, command.ApprovalId, StringComparison.Ordinal) &&
            string.Equals(record.CommandName, command.CommandName, StringComparison.Ordinal) &&
            string.Equals(record.CommandAllowlistVersion, command.CommandAllowlistVersion, StringComparison.Ordinal) &&
            record.Outcome is "success" or "failed" &&
            record.ExecutedAtUtc != default &&
            IsSafeMetadataToken(record.AuditOperationId) &&
            IsSafeMetadataToken(record.AuditStatus) &&
            IsSafeMetadataToken(record.CorrelationId) &&
            IsSafeMetadataToken(record.GeneratedContentVisibility) &&
            IsValidApprovedExecutionNextAction(record) &&
            IsSafeOptionalMetadataToken(record.FailureCode) &&
            IsSafeOptionalMetadataToken(record.Retryability) &&
            IsSafeMetadataToken(record.RedactionState) &&
            IsSafeMetadataToken(record.RetentionClass) &&
            IsSafeMetadataToken(record.SchemaVersion);

    private static bool IsValidApprovedExecutionNextAction(ApprovedAiActionExecutionRecord record)
        => record.Outcome switch
        {
            "success" => string.Equals(record.SafeNextAction, "none", StringComparison.Ordinal),
            "failed" => record.SafeNextAction is "review-ai-action" or "retry-later",
            _ => false,
        };

    private static bool IsValidExecutionNextAction(LowRiskAiAssistanceExecutionRecord record)
        => record.Outcome switch
        {
            "success" => string.Equals(record.SafeNextAction, "none", StringComparison.Ordinal),
            "failed" or "pending-approval" => string.Equals(record.SafeNextAction, "review-ai-action", StringComparison.Ordinal),
            _ => false,
        };

    private static string AssistanceKindToken(LowRiskAiAssistanceKind kind)
        => kind switch
        {
            LowRiskAiAssistanceKind.SummarizeVisibleContext => "summarize-visible-context",
            LowRiskAiAssistanceKind.ExplainVisibleEvidence => "explain-visible-evidence",
            _ => "unknown",
        };

    private static bool IsKnownAiActionRiskActionClass(AiActionRiskActionClass value)
        => value is AiActionRiskActionClass.ModifiesState
            or AiActionRiskActionClass.ExposesFiles
            or AiActionRiskActionClass.SendsExternal
            or AiActionRiskActionClass.CreatesTasks
            or AiActionRiskActionClass.InvokesTools
            or AiActionRiskActionClass.ActsOnBehalf;

    private sealed record AssociationDecisionValidation(
        bool IsValid,
        string ReasonCode,
        AssociationDecisionSourceSnapshot? Source,
        string? SanitizedNote)
    {
        public static AssociationDecisionValidation Valid(AssociationDecisionSourceSnapshot source, string? sanitizedNote)
            => new(true, string.Empty, source, sanitizedNote);

        public static AssociationDecisionValidation Invalid(string reasonCode)
            => new(false, reasonCode, null, null);
    }
}
