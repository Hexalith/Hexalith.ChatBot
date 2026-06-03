using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.Mailbox;
using Hexalith.ChatBot.Server.Governance.Outbound;
using Hexalith.ChatBot.Server.Governance.Policy;
using Hexalith.ChatBot.Server.Governance.AiActor;
using Hexalith.ChatBot.Server.Governance.CommandCapability;
using Hexalith.ChatBot.Server.Governance.ServiceClient;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Operations;

public static class GovernedOperationAggregateTests
{
    private const string NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string IntakeId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";

    [Fact]
    public static void HandleLowRiskAiExecutionShouldEmitStartedAndTerminalOutcomeEvents()
    {
        ExecuteLowRiskAIAssistance command = LowRiskExecutionCommand("success");

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(2);
        LowRiskAiAssistanceExecutionStarted started = result.Events[0].ShouldBeOfType<LowRiskAiAssistanceExecutionStarted>();
        started.ExecutionId.ShouldBe(command.ExecutionId);
        started.PolicyReasonCode.ShouldBe("low-risk-execute-allowed");
        LowRiskAiAssistanceExecutionSucceeded succeeded = result.Events[1].ShouldBeOfType<LowRiskAiAssistanceExecutionSucceeded>();
        succeeded.Record.SafeNextAction.ShouldBe("none");
    }

    [Fact]
    public static void HandleTenantPolicySensitiveChangeShouldCreatePendingApproval()
    {
        SubmitTenantPolicyChange command = TenantPolicyChange(TenantPolicyKnobIds.AssociationTHigh);

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        TenantPolicyChangePendingApproval pending = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantPolicyChangePendingApproval>();
        pending.PolicyChangeId.ShouldBe(command.PolicyChangeId);
        pending.SourcePolicySnapshotId.ShouldBe(command.SourcePolicySnapshotId);
        pending.ProposedPolicySnapshotId.ShouldBe(command.ProposedPolicySnapshotId);
        pending.ChangedKnobIds.ShouldBe(command.ChangedKnobIds, ignoreOrder: false);
        pending.RequesterActorId.ShouldBe("actor-alpha");
        pending.SourceVersion.ShouldBe(command.SourceVersion + 1);
    }

    [Fact]
    public static void HandleTenantPolicyStandardChangeShouldActivateSnapshotDirectly()
    {
        SubmitTenantPolicyChange command = TenantPolicyChange(
            TenantPolicyKnobIds.MailboxRoutingRules,
            new TenantPolicyChangeSet([new(TenantPolicyKnobIds.MailboxRoutingRules, StringListValue: ["routing-rule-001"])]));

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        TenantPolicySnapshotActivated activated = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantPolicySnapshotActivated>();
        activated.ApprovalStatus.ShouldBe(TenantPolicyApprovalStatus.NotRequired);
        activated.ActivatedPolicySnapshotId.ShouldBe(command.ProposedPolicySnapshotId);
    }

    [Fact]
    public static void HandleTenantPolicyChangeShouldRejectUnknownSchemaVersion()
    {
        SubmitTenantPolicyChange command = TenantPolicyChange(TenantPolicyKnobIds.AssociationTHigh) with
        {
            SchemaVersion = "tenant-policy-schema.custom.v1",
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantPolicyChangeRejected>().ReasonCode
            .ShouldBe("invalid_tenant_policy_change");
    }

    [Fact]
    public static void HandleTenantPolicyApprovalShouldRequirePendingChangeAndSecondActor()
    {
        SubmitTenantPolicyChange change = TenantPolicyChange(TenantPolicyKnobIds.AssociationTHigh);
        TenantPolicyChangePendingApproval pending = GovernedOperationAggregate
            .Handle(change, null, Envelope(change))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<TenantPolicyChangePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveTenantPolicyChange approval = new(
            change.PolicyChangeId,
            change.ProposedPolicySnapshotId,
            "policy-snapshot-active",
            pending.SourceVersion,
            change.ChangedKnobIds,
            "second-admin-approval",
            change.RequesterRef,
            "admin-approver",
            TenantPolicySchemaVersions.M0,
            change.CorrelationId);

        DomainResult selfApproval = GovernedOperationAggregate.Handle(approval, state, Envelope(approval));
        DomainResult secondActorApproval = GovernedOperationAggregate.Handle(approval, state, Envelope(approval, "actor-beta"));

        selfApproval.IsRejection.ShouldBeTrue();
        TenantPolicySnapshotActivated activated = secondActorApproval.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantPolicySnapshotActivated>();
        activated.ApprovalStatus.ShouldBe(TenantPolicyApprovalStatus.Approved);
        activated.ApproverRef.ShouldBe("admin-approver");
    }

    [Fact]
    public static void HandleMailboxSourceDisableProposalShouldCreatePendingWithoutDisabling()
    {
        SubmitMailboxSourceDisable command = MailboxSourceDisableSubmit();

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        MailboxSourceDisablePendingApproval pending = result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxSourceDisablePendingApproval>();
        pending.DisableChangeId.ShouldBe(command.DisableChangeId);
        pending.MailboxSourceRef.ShouldBe(command.MailboxSourceRef);
        pending.RequesterActorId.ShouldBe("actor-alpha");
        pending.OldState.ShouldBe(MailboxSourceControlState.Active);
        pending.NewState.ShouldBe(MailboxSourceControlState.Disabled);
        pending.SourceVersion.ShouldBe(command.SourceVersion + 1);

        // The proposal alone never disables the source: applying the pending event leaves no disabled record.
        GovernedOperationState state = new();
        state.Apply(pending);
        state.DisabledMailboxSources.ShouldBeEmpty();
        state.MailboxSourceDisablePendingApprovals.ShouldContainKey(command.DisableChangeId);
    }

    [Fact]
    public static void HandleMailboxSourceDisableApprovalShouldRequirePendingAndDistinctSecondActor()
    {
        SubmitMailboxSourceDisable submit = MailboxSourceDisableSubmit();
        MailboxSourceDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<MailboxSourceDisablePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveMailboxSourceDisable approval = MailboxSourceDisableApproval();

        // Same requester ref as approver ref is rejected at the aggregate (defense in depth).
        DomainResult selfApprovalByRef = GovernedOperationAggregate.Handle(
            approval with { ApproverRef = submit.RequesterRef },
            state,
            Envelope(approval, "actor-beta"));
        // Same human actor (envelope.UserId) as the proposer is rejected even with a distinct approver ref.
        DomainResult selfApprovalByActor = GovernedOperationAggregate.Handle(approval, state, Envelope(approval));
        // A distinct second human actor applies the disable.
        DomainResult secondActorApproval = GovernedOperationAggregate.Handle(approval, state, Envelope(approval, "actor-beta"));

        selfApprovalByRef.IsRejection.ShouldBeTrue();
        selfApprovalByActor.IsRejection.ShouldBeTrue();
        MailboxSourceDisabled disabled = secondActorApproval.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxSourceDisabled>();
        disabled.MailboxSourceRef.ShouldBe(submit.MailboxSourceRef);
        disabled.RequesterRef.ShouldBe(submit.RequesterRef);
        disabled.ApproverRef.ShouldBe(approval.ApproverRef);
        disabled.OldState.ShouldBe(MailboxSourceControlState.Active);
        disabled.NewState.ShouldBe(MailboxSourceControlState.Disabled);

        state.Apply(disabled);
        state.DisabledMailboxSources.ShouldContainKey(submit.MailboxSourceRef);
        state.MailboxSourceDisablePendingApprovals.ShouldNotContainKey(submit.DisableChangeId);
    }

    [Fact]
    public static void HandleMailboxSourceDisableApprovalShouldRejectSubjectVersionOrReasonMismatch()
    {
        SubmitMailboxSourceDisable submit = MailboxSourceDisableSubmit();
        MailboxSourceDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<MailboxSourceDisablePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveMailboxSourceDisable approval = MailboxSourceDisableApproval();

        GovernedOperationAggregate.Handle(approval with { MailboxSourceRef = "mailbox-source:other" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { SourceVersion = approval.SourceVersion + 5 }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { ReasonCode = "different-reason" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();

        // An approval for an unknown pending change is rejected (no durable disable).
        GovernedOperationAggregate.Handle(
            approval with { DisableChangeId = "mailbox-disable-unknown" },
            state,
            Envelope(approval, "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxSourceDisableRejected>().ReasonCode
            .ShouldBe("mailbox_source_disable_unavailable");
    }

    [Fact]
    public static void HandleServiceClientDisableProposalShouldCreatePendingWithoutDisabling()
    {
        SubmitServiceClientDisable command = ServiceClientDisableSubmit();

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        ServiceClientDisablePendingApproval pending = result.Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientDisablePendingApproval>();
        pending.DisableChangeId.ShouldBe(command.DisableChangeId);
        pending.ServiceClientRef.ShouldBe(command.ServiceClientRef);
        pending.RequesterActorId.ShouldBe("actor-alpha");
        pending.OldState.ShouldBe(ServiceClientControlState.Active);
        pending.NewState.ShouldBe(ServiceClientControlState.Disabled);
        pending.SourceVersion.ShouldBe(command.SourceVersion + 1);

        // The proposal alone never disables the client: applying the pending event leaves no disabled record.
        GovernedOperationState state = new();
        state.Apply(pending);
        state.DisabledServiceClients.ShouldBeEmpty();
        state.ServiceClientDisablePendingApprovals.ShouldContainKey(command.DisableChangeId);
    }

    [Fact]
    public static void HandleServiceClientDisableApprovalShouldRequirePendingAndDistinctSecondActor()
    {
        SubmitServiceClientDisable submit = ServiceClientDisableSubmit();
        ServiceClientDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ServiceClientDisablePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveServiceClientDisable approval = ServiceClientDisableApproval();

        // Same requester ref as approver ref is rejected at the aggregate (defense in depth).
        DomainResult selfApprovalByRef = GovernedOperationAggregate.Handle(
            approval with { ApproverRef = submit.RequesterRef },
            state,
            Envelope(approval, "actor-beta"));
        // Same human actor (envelope.UserId) as the proposer is rejected even with a distinct approver ref.
        DomainResult selfApprovalByActor = GovernedOperationAggregate.Handle(approval, state, Envelope(approval));
        // A distinct second human actor applies the disable.
        DomainResult secondActorApproval = GovernedOperationAggregate.Handle(approval, state, Envelope(approval, "actor-beta"));

        selfApprovalByRef.IsRejection.ShouldBeTrue();
        selfApprovalByActor.IsRejection.ShouldBeTrue();
        ServiceClientDisabled disabled = secondActorApproval.Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientDisabled>();
        disabled.ServiceClientRef.ShouldBe(submit.ServiceClientRef);
        disabled.RequesterRef.ShouldBe(submit.RequesterRef);
        disabled.ApproverRef.ShouldBe(approval.ApproverRef);
        disabled.OldState.ShouldBe(ServiceClientControlState.Active);
        disabled.NewState.ShouldBe(ServiceClientControlState.Disabled);

        state.Apply(disabled);
        state.DisabledServiceClients.ShouldContainKey(submit.ServiceClientRef);
        state.ServiceClientDisablePendingApprovals.ShouldNotContainKey(submit.DisableChangeId);
    }

    [Fact]
    public static void HandleServiceClientDisableApprovalShouldRejectSubjectVersionOrReasonMismatch()
    {
        SubmitServiceClientDisable submit = ServiceClientDisableSubmit();
        ServiceClientDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ServiceClientDisablePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveServiceClientDisable approval = ServiceClientDisableApproval();

        GovernedOperationAggregate.Handle(approval with { ServiceClientRef = "service-client:other" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { SourceVersion = approval.SourceVersion + 5 }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { ReasonCode = "different-reason" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();

        // An approval for an unknown pending change is rejected (no durable disable).
        GovernedOperationAggregate.Handle(
            approval with { DisableChangeId = "service-client-disable-unknown" },
            state,
            Envelope(approval, "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientDisableRejected>().ReasonCode
            .ShouldBe("service_client_disable_unavailable");
    }

    [Fact]
    public static void HandleServiceClientDisableProposalShouldNoOpForAlreadyDisabledOrDuplicate()
    {
        SubmitServiceClientDisable submit = ServiceClientDisableSubmit();
        ServiceClientDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ServiceClientDisablePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);

        // A re-submit of the same pending disable change is a no-op (idempotency on the pending set).
        GovernedOperationAggregate.Handle(submit, state, Envelope(submit)).IsNoOp.ShouldBeTrue();
        GovernedOperationAggregate.Handle(submit, state, Envelope(submit)).Events.ShouldBeEmpty();

        ServiceClientDisabled disabled = GovernedOperationAggregate
            .Handle(ServiceClientDisableApproval(), state, Envelope(ServiceClientDisableApproval(), "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientDisabled>();
        state.Apply(disabled);

        // A fresh proposal for an already-disabled subject is a no-op (idempotency on the disabled set).
        GovernedOperationAggregate.Handle(
            submit with { DisableChangeId = "service-client-disable-002" },
            state,
            Envelope(submit)).Events.ShouldBeEmpty();
    }

    [Fact]
    public static void HandleAiActorDisableProposalShouldCreatePendingWithoutDisabling()
    {
        SubmitAiActorDisable command = AiActorDisableSubmit();

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AiActorDisablePendingApproval pending = result.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorDisablePendingApproval>();
        pending.DisableChangeId.ShouldBe(command.DisableChangeId);
        pending.AiActorRef.ShouldBe(command.AiActorRef);
        pending.RequesterActorId.ShouldBe("actor-alpha");
        pending.OldState.ShouldBe(AiActorControlState.Active);
        pending.NewState.ShouldBe(AiActorControlState.Disabled);
        pending.SourceVersion.ShouldBe(command.SourceVersion + 1);

        // The proposal alone never disables the AI actor: applying the pending event leaves no disabled record.
        GovernedOperationState state = new();
        state.Apply(pending);
        state.DisabledAiActors.ShouldBeEmpty();
        state.AiActorDisablePendingApprovals.ShouldContainKey(command.DisableChangeId);
    }

    [Fact]
    public static void HandleAiActorDisableApprovalShouldRequirePendingAndDistinctSecondActor()
    {
        SubmitAiActorDisable submit = AiActorDisableSubmit();
        AiActorDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<AiActorDisablePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveAiActorDisable approval = AiActorDisableApproval();

        // Same requester ref as approver ref is rejected at the aggregate (defense in depth).
        DomainResult selfApprovalByRef = GovernedOperationAggregate.Handle(
            approval with { ApproverRef = submit.RequesterRef },
            state,
            Envelope(approval, "actor-beta"));
        // Same human actor (envelope.UserId) as the proposer is rejected even with a distinct approver ref.
        DomainResult selfApprovalByActor = GovernedOperationAggregate.Handle(approval, state, Envelope(approval));
        // A distinct second human actor applies the disable.
        DomainResult secondActorApproval = GovernedOperationAggregate.Handle(approval, state, Envelope(approval, "actor-beta"));

        selfApprovalByRef.IsRejection.ShouldBeTrue();
        selfApprovalByActor.IsRejection.ShouldBeTrue();
        AiActorDisabled disabled = secondActorApproval.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorDisabled>();
        disabled.AiActorRef.ShouldBe(submit.AiActorRef);
        disabled.RequesterRef.ShouldBe(submit.RequesterRef);
        disabled.ApproverRef.ShouldBe(approval.ApproverRef);
        disabled.OldState.ShouldBe(AiActorControlState.Active);
        disabled.NewState.ShouldBe(AiActorControlState.Disabled);

        state.Apply(disabled);
        state.DisabledAiActors.ShouldContainKey(submit.AiActorRef);
        state.AiActorDisablePendingApprovals.ShouldNotContainKey(submit.DisableChangeId);
    }

    [Fact]
    public static void HandleAiActorDisableApprovalShouldRejectSubjectVersionOrReasonMismatch()
    {
        SubmitAiActorDisable submit = AiActorDisableSubmit();
        AiActorDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<AiActorDisablePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveAiActorDisable approval = AiActorDisableApproval();

        GovernedOperationAggregate.Handle(approval with { AiActorRef = "ai-actor:other" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { SourceVersion = approval.SourceVersion + 5 }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { ReasonCode = "different-reason" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();

        // An approval for an unknown pending change is rejected (no durable disable).
        GovernedOperationAggregate.Handle(
            approval with { DisableChangeId = "ai-actor-disable-unknown" },
            state,
            Envelope(approval, "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorDisableRejected>().ReasonCode
            .ShouldBe("ai_actor_disable_unavailable");
    }

    [Fact]
    public static void HandleCommandCapabilityDisableProposalShouldCreatePendingWithoutDisabling()
    {
        SubmitCommandCapabilityDisable command = CommandCapabilityDisableSubmit();

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        CommandCapabilityDisablePendingApproval pending = result.Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityDisablePendingApproval>();
        pending.DisableChangeId.ShouldBe(command.DisableChangeId);
        pending.CommandCapabilityRef.ShouldBe(command.CommandCapabilityRef);
        pending.RequesterActorId.ShouldBe("actor-alpha");
        pending.OldState.ShouldBe(CommandCapabilityControlState.Active);
        pending.NewState.ShouldBe(CommandCapabilityControlState.Disabled);
        pending.SourceVersion.ShouldBe(command.SourceVersion + 1);

        // The proposal alone never disables the capability: applying the pending event leaves no disabled record.
        GovernedOperationState state = new();
        state.Apply(pending);
        state.DisabledCommandCapabilities.ShouldBeEmpty();
        state.CommandCapabilityDisablePendingApprovals.ShouldContainKey(command.DisableChangeId);
    }

    [Fact]
    public static void HandleCommandCapabilityDisableApprovalShouldRequirePendingAndDistinctSecondActor()
    {
        SubmitCommandCapabilityDisable submit = CommandCapabilityDisableSubmit();
        CommandCapabilityDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<CommandCapabilityDisablePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveCommandCapabilityDisable approval = CommandCapabilityDisableApproval();

        // Same requester ref as approver ref is rejected at the aggregate (defense in depth).
        DomainResult selfApprovalByRef = GovernedOperationAggregate.Handle(
            approval with { ApproverRef = submit.RequesterRef },
            state,
            Envelope(approval, "actor-beta"));
        // Same human actor (envelope.UserId) as the proposer is rejected even with a distinct approver ref.
        DomainResult selfApprovalByActor = GovernedOperationAggregate.Handle(approval, state, Envelope(approval));
        // A distinct second human actor applies the disable.
        DomainResult secondActorApproval = GovernedOperationAggregate.Handle(approval, state, Envelope(approval, "actor-beta"));

        selfApprovalByRef.IsRejection.ShouldBeTrue();
        selfApprovalByActor.IsRejection.ShouldBeTrue();
        CommandCapabilityDisabled disabled = secondActorApproval.Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityDisabled>();
        disabled.CommandCapabilityRef.ShouldBe(submit.CommandCapabilityRef);
        disabled.RequesterRef.ShouldBe(submit.RequesterRef);
        disabled.ApproverRef.ShouldBe(approval.ApproverRef);
        disabled.OldState.ShouldBe(CommandCapabilityControlState.Active);
        disabled.NewState.ShouldBe(CommandCapabilityControlState.Disabled);

        state.Apply(disabled);
        state.DisabledCommandCapabilities.ShouldContainKey(submit.CommandCapabilityRef);
        state.CommandCapabilityDisablePendingApprovals.ShouldNotContainKey(submit.DisableChangeId);
    }

    [Fact]
    public static void HandleCommandCapabilityDisableApprovalShouldRejectSubjectVersionOrReasonMismatch()
    {
        SubmitCommandCapabilityDisable submit = CommandCapabilityDisableSubmit();
        CommandCapabilityDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<CommandCapabilityDisablePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveCommandCapabilityDisable approval = CommandCapabilityDisableApproval();

        GovernedOperationAggregate.Handle(approval with { CommandCapabilityRef = nameof(RejectEmailProjectAssociation) }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { SourceVersion = approval.SourceVersion + 5 }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { ReasonCode = "different-reason" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();

        // An approval for an unknown pending change is rejected (no durable disable).
        GovernedOperationAggregate.Handle(
            approval with { DisableChangeId = "command-capability-disable-unknown" },
            state,
            Envelope(approval, "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityDisableRejected>().ReasonCode
            .ShouldBe("command_capability_disable_unavailable");
    }

    [Fact]
    public static void HandleCommandCapabilityDisableProposalShouldNoOpForAlreadyDisabledOrDuplicate()
    {
        SubmitCommandCapabilityDisable submit = CommandCapabilityDisableSubmit();
        CommandCapabilityDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<CommandCapabilityDisablePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);

        // A re-submit of the same pending disable change is a no-op (IsNoOp, not IsSuccess with an activate event).
        GovernedOperationAggregate.Handle(submit, state, Envelope(submit)).IsNoOp.ShouldBeTrue();
        GovernedOperationAggregate.Handle(submit, state, Envelope(submit)).Events.ShouldBeEmpty();

        CommandCapabilityDisabled disabled = GovernedOperationAggregate
            .Handle(CommandCapabilityDisableApproval(), state, Envelope(CommandCapabilityDisableApproval(), "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityDisabled>();
        state.Apply(disabled);

        // A fresh proposal for an already-disabled subject is a no-op (idempotency on the disabled set).
        GovernedOperationAggregate.Handle(
            submit with { DisableChangeId = "command-capability-disable-002" },
            state,
            Envelope(submit)).Events.ShouldBeEmpty();
    }

    [Fact]
    public static void HandleOutboundChannelDisableProposalShouldCreatePendingWithoutDisabling()
    {
        SubmitOutboundChannelDisable command = OutboundChannelDisableSubmit();

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        OutboundChannelDisablePendingApproval pending = result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundChannelDisablePendingApproval>();
        pending.DisableChangeId.ShouldBe(command.DisableChangeId);
        pending.OutboundChannelRef.ShouldBe(command.OutboundChannelRef);
        pending.RequesterActorId.ShouldBe("actor-alpha");
        pending.OldState.ShouldBe(OutboundChannelControlState.Active);
        pending.NewState.ShouldBe(OutboundChannelControlState.Disabled);
        pending.SourceVersion.ShouldBe(command.SourceVersion + 1);

        // The proposal alone never disables the channel: applying the pending event leaves no disabled record.
        GovernedOperationState state = new();
        state.Apply(pending);
        state.DisabledOutboundChannels.ShouldBeEmpty();
        state.OutboundChannelDisablePendingApprovals.ShouldContainKey(command.DisableChangeId);
    }

    [Fact]
    public static void HandleOutboundChannelDisableApprovalShouldRequirePendingAndDistinctSecondActor()
    {
        SubmitOutboundChannelDisable submit = OutboundChannelDisableSubmit();
        OutboundChannelDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<OutboundChannelDisablePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveOutboundChannelDisable approval = OutboundChannelDisableApproval();

        // Same requester ref as approver ref is rejected at the aggregate (defense in depth).
        DomainResult selfApprovalByRef = GovernedOperationAggregate.Handle(
            approval with { ApproverRef = submit.RequesterRef },
            state,
            Envelope(approval, "actor-beta"));
        // Same human actor (envelope.UserId) as the proposer is rejected even with a distinct approver ref.
        DomainResult selfApprovalByActor = GovernedOperationAggregate.Handle(approval, state, Envelope(approval));
        // A distinct second human actor applies the disable.
        DomainResult secondActorApproval = GovernedOperationAggregate.Handle(approval, state, Envelope(approval, "actor-beta"));

        selfApprovalByRef.IsRejection.ShouldBeTrue();
        selfApprovalByActor.IsRejection.ShouldBeTrue();
        OutboundChannelDisabled disabled = secondActorApproval.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundChannelDisabled>();
        disabled.OutboundChannelRef.ShouldBe(submit.OutboundChannelRef);
        disabled.RequesterRef.ShouldBe(submit.RequesterRef);
        disabled.ApproverRef.ShouldBe(approval.ApproverRef);
        disabled.OldState.ShouldBe(OutboundChannelControlState.Active);
        disabled.NewState.ShouldBe(OutboundChannelControlState.Disabled);

        state.Apply(disabled);
        state.DisabledOutboundChannels.ShouldContainKey(submit.OutboundChannelRef);
        state.OutboundChannelDisablePendingApprovals.ShouldNotContainKey(submit.DisableChangeId);
    }

    [Fact]
    public static void HandleOutboundChannelDisableApprovalShouldRejectSubjectVersionOrReasonMismatch()
    {
        SubmitOutboundChannelDisable submit = OutboundChannelDisableSubmit();
        OutboundChannelDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<OutboundChannelDisablePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveOutboundChannelDisable approval = OutboundChannelDisableApproval();

        GovernedOperationAggregate.Handle(approval with { OutboundChannelRef = "adapter:other-outbound" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { SourceVersion = approval.SourceVersion + 5 }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { ReasonCode = "different-reason" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();

        // An approval for an unknown pending change is rejected (no durable disable).
        GovernedOperationAggregate.Handle(
            approval with { DisableChangeId = "outbound-channel-disable-unknown" },
            state,
            Envelope(approval, "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundChannelDisableRejected>().ReasonCode
            .ShouldBe("outbound_channel_disable_unavailable");
    }

    [Fact]
    public static void HandleOutboundChannelDisableProposalShouldNoOpForAlreadyDisabledOrDuplicate()
    {
        SubmitOutboundChannelDisable submit = OutboundChannelDisableSubmit();
        OutboundChannelDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<OutboundChannelDisablePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);

        // A re-submit of the same pending disable change is a no-op (IsNoOp, not IsSuccess with an activate event).
        GovernedOperationAggregate.Handle(submit, state, Envelope(submit)).IsNoOp.ShouldBeTrue();
        GovernedOperationAggregate.Handle(submit, state, Envelope(submit)).Events.ShouldBeEmpty();

        OutboundChannelDisabled disabled = GovernedOperationAggregate
            .Handle(OutboundChannelDisableApproval(), state, Envelope(OutboundChannelDisableApproval(), "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundChannelDisabled>();
        state.Apply(disabled);

        // A fresh proposal for an already-disabled subject is a no-op (idempotency on the disabled set).
        GovernedOperationAggregate.Handle(
            submit with { DisableChangeId = "outbound-channel-disable-002" },
            state,
            Envelope(submit)).Events.ShouldBeEmpty();
    }

    [Fact]
    public static void HandleCommandCapabilityDisableShouldNotMutatePriorCommittedOrPendingRecords()
    {
        // AC5 / NFR17 / FR75c: disabling a command capability affects only FUTURE admission. Committing a disable
        // for one command type must never rewrite or remove already-committed records — a prior committed disable
        // for a DIFFERENT command type and an unrelated PENDING disable for a THIRD command type both remain
        // intact and reconstructable (admins cannot mutate prior project-level records; per-subject isolation).
        GovernedOperationState state = new();

        // Prior committed disable for a different command capability (an already-committed record).
        SubmitCommandCapabilityDisable priorSubmit = CommandCapabilityDisableSubmit() with
        {
            DisableChangeId = "command-capability-disable-900",
            CommandCapabilityRef = nameof(MarkEmailAssociationNeedsReview),
        };
        CommandCapabilityDisablePendingApproval priorPending = GovernedOperationAggregate
            .Handle(priorSubmit, state, Envelope(priorSubmit))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityDisablePendingApproval>();
        state.Apply(priorPending);
        ApproveCommandCapabilityDisable priorApproval = CommandCapabilityDisableApproval() with
        {
            DisableChangeId = "command-capability-disable-900",
            CommandCapabilityRef = nameof(MarkEmailAssociationNeedsReview),
        };
        CommandCapabilityDisabled priorDisabled = GovernedOperationAggregate
            .Handle(priorApproval, state, Envelope(priorApproval, "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityDisabled>();
        state.Apply(priorDisabled);

        // An unrelated pending disable for a third command capability (an uncommitted record that must survive).
        SubmitCommandCapabilityDisable unrelatedSubmit = CommandCapabilityDisableSubmit() with
        {
            DisableChangeId = "command-capability-disable-700",
            CommandCapabilityRef = nameof(RejectEmailProjectAssociation),
        };
        CommandCapabilityDisablePendingApproval unrelatedPending = GovernedOperationAggregate
            .Handle(unrelatedSubmit, state, Envelope(unrelatedSubmit))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityDisablePendingApproval>();
        state.Apply(unrelatedPending);

        // Disable the target command capability through the two-person flow.
        SubmitCommandCapabilityDisable submit = CommandCapabilityDisableSubmit();
        CommandCapabilityDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, state, Envelope(submit))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityDisablePendingApproval>();
        state.Apply(pending);
        CommandCapabilityDisabled disabled = GovernedOperationAggregate
            .Handle(CommandCapabilityDisableApproval(), state, Envelope(CommandCapabilityDisableApproval(), "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityDisabled>();
        state.Apply(disabled);

        // The target is now disabled...
        state.DisabledCommandCapabilities.ShouldContainKey(nameof(AssociateEmailToProject));
        // ...while every prior record remains intact: the committed disable for the different command type is
        // untouched, and the unrelated pending disable still awaits its own distinct second approver.
        state.DisabledCommandCapabilities.ShouldContainKey(nameof(MarkEmailAssociationNeedsReview));
        state.CommandCapabilityDisablePendingApprovals.ShouldContainKey("command-capability-disable-700");
        state.DisabledCommandCapabilities.ShouldNotContainKey(nameof(RejectEmailProjectAssociation));
    }

    [Fact]
    public static void HandleCommandCapabilityQuarantineProposalShouldCreatePendingWithoutQuarantining()
    {
        SubmitCommandCapabilityQuarantine command = CommandCapabilityQuarantineSubmit();

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        CommandCapabilityQuarantinePendingApproval pending = result.Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityQuarantinePendingApproval>();
        pending.QuarantineChangeId.ShouldBe(command.QuarantineChangeId);
        pending.CommandCapabilityRef.ShouldBe(command.CommandCapabilityRef);
        pending.RequesterActorId.ShouldBe("actor-alpha");
        pending.OldState.ShouldBe(CommandCapabilityControlState.Active);
        pending.NewState.ShouldBe(CommandCapabilityControlState.Quarantined);
        pending.SourceVersion.ShouldBe(command.SourceVersion + 1);

        // The proposal alone never quarantines the capability: applying the pending event leaves no quarantined record.
        GovernedOperationState state = new();
        state.Apply(pending);
        state.QuarantinedCommandCapabilities.ShouldBeEmpty();
        state.CommandCapabilityQuarantinePendingApprovals.ShouldContainKey(command.QuarantineChangeId);
    }

    [Fact]
    public static void HandleCommandCapabilityQuarantineApprovalShouldRequirePendingAndDistinctSecondActor()
    {
        SubmitCommandCapabilityQuarantine submit = CommandCapabilityQuarantineSubmit();
        CommandCapabilityQuarantinePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<CommandCapabilityQuarantinePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveCommandCapabilityQuarantine approval = CommandCapabilityQuarantineApproval();

        // Same requester ref as approver ref is rejected at the aggregate (defense in depth).
        DomainResult selfApprovalByRef = GovernedOperationAggregate.Handle(
            approval with { ApproverRef = submit.RequesterRef },
            state,
            Envelope(approval, "actor-beta"));
        // Same human actor (envelope.UserId) as the proposer is rejected even with a distinct approver ref.
        DomainResult selfApprovalByActor = GovernedOperationAggregate.Handle(approval, state, Envelope(approval));
        // A distinct second human actor applies the quarantine.
        DomainResult secondActorApproval = GovernedOperationAggregate.Handle(approval, state, Envelope(approval, "actor-beta"));

        selfApprovalByRef.IsRejection.ShouldBeTrue();
        selfApprovalByActor.IsRejection.ShouldBeTrue();
        CommandCapabilityQuarantined quarantined = secondActorApproval.Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityQuarantined>();
        quarantined.CommandCapabilityRef.ShouldBe(submit.CommandCapabilityRef);
        quarantined.RequesterRef.ShouldBe(submit.RequesterRef);
        quarantined.ApproverRef.ShouldBe(approval.ApproverRef);
        quarantined.OldState.ShouldBe(CommandCapabilityControlState.Active);
        quarantined.NewState.ShouldBe(CommandCapabilityControlState.Quarantined);

        state.Apply(quarantined);
        state.QuarantinedCommandCapabilities.ShouldContainKey(submit.CommandCapabilityRef);
        state.CommandCapabilityQuarantinePendingApprovals.ShouldNotContainKey(submit.QuarantineChangeId);
    }

    [Fact]
    public static void HandleCommandCapabilityQuarantineApprovalShouldRejectSubjectVersionOrReasonMismatch()
    {
        SubmitCommandCapabilityQuarantine submit = CommandCapabilityQuarantineSubmit();
        CommandCapabilityQuarantinePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<CommandCapabilityQuarantinePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveCommandCapabilityQuarantine approval = CommandCapabilityQuarantineApproval();

        GovernedOperationAggregate.Handle(approval with { CommandCapabilityRef = nameof(RejectEmailProjectAssociation) }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { SourceVersion = approval.SourceVersion + 5 }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { ReasonCode = "different-reason" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();

        // An approval for an unknown pending change is rejected (no durable quarantine).
        GovernedOperationAggregate.Handle(
            approval with { QuarantineChangeId = "command-capability-quarantine-unknown" },
            state,
            Envelope(approval, "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityQuarantineRejected>().ReasonCode
            .ShouldBe("command_capability_quarantine_unavailable");
    }

    [Fact]
    public static void HandleCommandCapabilityQuarantineProposalShouldNoOpForAlreadyQuarantinedOrDuplicate()
    {
        SubmitCommandCapabilityQuarantine submit = CommandCapabilityQuarantineSubmit();
        CommandCapabilityQuarantinePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<CommandCapabilityQuarantinePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);

        // A re-submit of the same pending quarantine change is a no-op (IsNoOp, not IsSuccess with an activate event).
        GovernedOperationAggregate.Handle(submit, state, Envelope(submit)).IsNoOp.ShouldBeTrue();
        GovernedOperationAggregate.Handle(submit, state, Envelope(submit)).Events.ShouldBeEmpty();

        CommandCapabilityQuarantined quarantined = GovernedOperationAggregate
            .Handle(CommandCapabilityQuarantineApproval(), state, Envelope(CommandCapabilityQuarantineApproval(), "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityQuarantined>();
        state.Apply(quarantined);

        // A fresh proposal for an already-quarantined subject is a no-op (idempotency on the quarantined set).
        GovernedOperationAggregate.Handle(
            submit with { QuarantineChangeId = "command-capability-quarantine-002" },
            state,
            Envelope(submit)).Events.ShouldBeEmpty();
    }

    [Fact]
    public static void HandleCommandCapabilityQuarantineShouldNotMutatePriorCommittedOrPendingRecords()
    {
        // AC5 / NFR17 / FR75c: quarantining a command capability affects only FUTURE admission. Committing a
        // quarantine for one command type must never rewrite or remove already-committed records — a prior committed
        // quarantine for a DIFFERENT command type and an unrelated PENDING quarantine for a THIRD command type both
        // remain intact and reconstructable (admins cannot mutate prior project-level records; per-subject isolation).
        GovernedOperationState state = new();

        // Prior committed quarantine for a different command capability (an already-committed record).
        SubmitCommandCapabilityQuarantine priorSubmit = CommandCapabilityQuarantineSubmit() with
        {
            QuarantineChangeId = "command-capability-quarantine-900",
            CommandCapabilityRef = nameof(MarkEmailAssociationNeedsReview),
        };
        CommandCapabilityQuarantinePendingApproval priorPending = GovernedOperationAggregate
            .Handle(priorSubmit, state, Envelope(priorSubmit))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityQuarantinePendingApproval>();
        state.Apply(priorPending);
        ApproveCommandCapabilityQuarantine priorApproval = CommandCapabilityQuarantineApproval() with
        {
            QuarantineChangeId = "command-capability-quarantine-900",
            CommandCapabilityRef = nameof(MarkEmailAssociationNeedsReview),
        };
        CommandCapabilityQuarantined priorQuarantined = GovernedOperationAggregate
            .Handle(priorApproval, state, Envelope(priorApproval, "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityQuarantined>();
        state.Apply(priorQuarantined);

        // An unrelated pending quarantine for a third command capability (an uncommitted record that must survive).
        SubmitCommandCapabilityQuarantine unrelatedSubmit = CommandCapabilityQuarantineSubmit() with
        {
            QuarantineChangeId = "command-capability-quarantine-700",
            CommandCapabilityRef = nameof(RejectEmailProjectAssociation),
        };
        CommandCapabilityQuarantinePendingApproval unrelatedPending = GovernedOperationAggregate
            .Handle(unrelatedSubmit, state, Envelope(unrelatedSubmit))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityQuarantinePendingApproval>();
        state.Apply(unrelatedPending);

        // Quarantine the target command capability through the two-person flow.
        SubmitCommandCapabilityQuarantine submit = CommandCapabilityQuarantineSubmit();
        CommandCapabilityQuarantinePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, state, Envelope(submit))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityQuarantinePendingApproval>();
        state.Apply(pending);
        CommandCapabilityQuarantined quarantined = GovernedOperationAggregate
            .Handle(CommandCapabilityQuarantineApproval(), state, Envelope(CommandCapabilityQuarantineApproval(), "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityQuarantined>();
        state.Apply(quarantined);

        // The target is now quarantined...
        state.QuarantinedCommandCapabilities.ShouldContainKey(nameof(AssociateEmailToProject));
        // ...while every prior record remains intact: the committed quarantine for the different command type is
        // untouched, and the unrelated pending quarantine still awaits its own distinct second approver.
        state.QuarantinedCommandCapabilities.ShouldContainKey(nameof(MarkEmailAssociationNeedsReview));
        state.CommandCapabilityQuarantinePendingApprovals.ShouldContainKey("command-capability-quarantine-700");
        state.QuarantinedCommandCapabilities.ShouldNotContainKey(nameof(RejectEmailProjectAssociation));
    }

    [Fact]
    public static void HandleAiActorDisableProposalShouldNoOpForAlreadyDisabledOrDuplicate()
    {
        SubmitAiActorDisable submit = AiActorDisableSubmit();
        AiActorDisablePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<AiActorDisablePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);

        // A re-submit of the same pending disable change is a no-op (idempotency on the pending set).
        GovernedOperationAggregate.Handle(submit, state, Envelope(submit)).IsNoOp.ShouldBeTrue();
        GovernedOperationAggregate.Handle(submit, state, Envelope(submit)).Events.ShouldBeEmpty();

        AiActorDisabled disabled = GovernedOperationAggregate
            .Handle(AiActorDisableApproval(), state, Envelope(AiActorDisableApproval(), "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorDisabled>();
        state.Apply(disabled);

        // A fresh proposal for an already-disabled subject is a no-op (idempotency on the disabled set).
        GovernedOperationAggregate.Handle(
            submit with { DisableChangeId = "ai-actor-disable-002" },
            state,
            Envelope(submit)).Events.ShouldBeEmpty();
    }

    [Fact]
    public static void HandleAiActorQuarantineProposalShouldCreatePendingWithoutQuarantining()
    {
        SubmitAiActorQuarantine command = AiActorQuarantineSubmit();

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AiActorQuarantinePendingApproval pending = result.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorQuarantinePendingApproval>();
        pending.QuarantineChangeId.ShouldBe(command.QuarantineChangeId);
        pending.AiActorRef.ShouldBe(command.AiActorRef);
        pending.RequesterActorId.ShouldBe("actor-alpha");
        pending.OldState.ShouldBe(AiActorControlState.Active);
        pending.NewState.ShouldBe(AiActorControlState.Quarantined);
        pending.SourceVersion.ShouldBe(command.SourceVersion + 1);

        // The proposal alone never quarantines the AI actor: applying the pending event leaves no quarantined record.
        GovernedOperationState state = new();
        state.Apply(pending);
        state.QuarantinedAiActors.ShouldBeEmpty();
        state.AiActorQuarantinePendingApprovals.ShouldContainKey(command.QuarantineChangeId);
    }

    [Fact]
    public static void HandleAiActorQuarantineApprovalShouldRequirePendingAndDistinctSecondActor()
    {
        SubmitAiActorQuarantine submit = AiActorQuarantineSubmit();
        AiActorQuarantinePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<AiActorQuarantinePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveAiActorQuarantine approval = AiActorQuarantineApproval();

        // Same requester ref as approver ref is rejected at the aggregate (defense in depth).
        DomainResult selfApprovalByRef = GovernedOperationAggregate.Handle(
            approval with { ApproverRef = submit.RequesterRef },
            state,
            Envelope(approval, "actor-beta"));
        // Same human actor (envelope.UserId) as the proposer is rejected even with a distinct approver ref.
        DomainResult selfApprovalByActor = GovernedOperationAggregate.Handle(approval, state, Envelope(approval));
        // A distinct second human actor applies the quarantine.
        DomainResult secondActorApproval = GovernedOperationAggregate.Handle(approval, state, Envelope(approval, "actor-beta"));

        selfApprovalByRef.IsRejection.ShouldBeTrue();
        selfApprovalByActor.IsRejection.ShouldBeTrue();
        AiActorQuarantined quarantined = secondActorApproval.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorQuarantined>();
        quarantined.AiActorRef.ShouldBe(submit.AiActorRef);
        quarantined.RequesterRef.ShouldBe(submit.RequesterRef);
        quarantined.ApproverRef.ShouldBe(approval.ApproverRef);
        quarantined.OldState.ShouldBe(AiActorControlState.Active);
        quarantined.NewState.ShouldBe(AiActorControlState.Quarantined);

        state.Apply(quarantined);
        state.QuarantinedAiActors.ShouldContainKey(submit.AiActorRef);
        state.AiActorQuarantinePendingApprovals.ShouldNotContainKey(submit.QuarantineChangeId);
    }

    [Fact]
    public static void HandleAiActorQuarantineApprovalShouldRejectSubjectVersionOrReasonMismatch()
    {
        SubmitAiActorQuarantine submit = AiActorQuarantineSubmit();
        AiActorQuarantinePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<AiActorQuarantinePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveAiActorQuarantine approval = AiActorQuarantineApproval();

        GovernedOperationAggregate.Handle(approval with { AiActorRef = "ai-actor:other" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { SourceVersion = approval.SourceVersion + 5 }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { ReasonCode = "different-reason" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();

        // An approval for an unknown pending change is rejected (no durable quarantine).
        GovernedOperationAggregate.Handle(
            approval with { QuarantineChangeId = "ai-actor-quarantine-unknown" },
            state,
            Envelope(approval, "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorQuarantineRejected>().ReasonCode
            .ShouldBe("ai_actor_quarantine_unavailable");
    }

    [Fact]
    public static void HandleAiActorQuarantineProposalShouldNoOpForAlreadyQuarantinedOrDuplicate()
    {
        SubmitAiActorQuarantine submit = AiActorQuarantineSubmit();
        AiActorQuarantinePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<AiActorQuarantinePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);

        // A re-submit of the same pending quarantine change is a no-op (idempotency on the pending set).
        GovernedOperationAggregate.Handle(submit, state, Envelope(submit)).IsNoOp.ShouldBeTrue();
        GovernedOperationAggregate.Handle(submit, state, Envelope(submit)).Events.ShouldBeEmpty();

        AiActorQuarantined quarantined = GovernedOperationAggregate
            .Handle(AiActorQuarantineApproval(), state, Envelope(AiActorQuarantineApproval(), "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorQuarantined>();
        state.Apply(quarantined);

        // A fresh proposal for an already-quarantined subject is a no-op (idempotency on the quarantined set).
        GovernedOperationAggregate.Handle(
            submit with { QuarantineChangeId = "ai-actor-quarantine-002" },
            state,
            Envelope(submit)).Events.ShouldBeEmpty();
    }

    [Fact]
    public static void HandleAiActorQuarantineShouldNotMutatePriorCommittedRecords()
    {
        // AC5 / NFR17: quarantining an AI actor affects only FUTURE admission. Committing a quarantine for one
        // AI actor must never rewrite or remove already-committed records — a prior committed disable for a
        // different AI actor and an unrelated pending quarantine for a third AI actor both remain intact and
        // reconstructable (FR75c: admins cannot mutate prior project-level records).
        GovernedOperationState state = new();

        // Prior committed disable for a different AI actor (an already-committed record).
        SubmitAiActorDisable disableSubmit = AiActorDisableSubmit() with
        {
            DisableChangeId = "ai-actor-disable-900",
            AiActorRef = "ai-actor:legacy-actor",
        };
        AiActorDisablePendingApproval disablePending = GovernedOperationAggregate
            .Handle(disableSubmit, state, Envelope(disableSubmit))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorDisablePendingApproval>();
        state.Apply(disablePending);
        ApproveAiActorDisable disableApproval = AiActorDisableApproval() with
        {
            DisableChangeId = "ai-actor-disable-900",
            AiActorRef = "ai-actor:legacy-actor",
        };
        AiActorDisabled disabled = GovernedOperationAggregate
            .Handle(disableApproval, state, Envelope(disableApproval, "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorDisabled>();
        state.Apply(disabled);

        // An unrelated pending quarantine for a third AI actor (an uncommitted record that must survive).
        SubmitAiActorQuarantine unrelatedSubmit = AiActorQuarantineSubmit() with
        {
            QuarantineChangeId = "ai-actor-quarantine-700",
            AiActorRef = "ai-actor:third-actor",
        };
        AiActorQuarantinePendingApproval unrelatedPending = GovernedOperationAggregate
            .Handle(unrelatedSubmit, state, Envelope(unrelatedSubmit))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorQuarantinePendingApproval>();
        state.Apply(unrelatedPending);

        // Quarantine the target AI actor through the two-person flow.
        SubmitAiActorQuarantine submit = AiActorQuarantineSubmit();
        AiActorQuarantinePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, state, Envelope(submit))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorQuarantinePendingApproval>();
        state.Apply(pending);
        AiActorQuarantined quarantined = GovernedOperationAggregate
            .Handle(AiActorQuarantineApproval(), state, Envelope(AiActorQuarantineApproval(), "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorQuarantined>();
        state.Apply(quarantined);

        // The target is now quarantined...
        state.QuarantinedAiActors.ShouldContainKey(submit.AiActorRef);
        // ...while every prior record remains intact: the committed disable is untouched (not rewritten to
        // quarantined), and the unrelated pending quarantine still awaits its own distinct second approver.
        state.DisabledAiActors.ShouldContainKey("ai-actor:legacy-actor");
        state.QuarantinedAiActors.ShouldNotContainKey("ai-actor:legacy-actor");
        state.AiActorQuarantinePendingApprovals.ShouldContainKey("ai-actor-quarantine-700");
    }

    [Fact]
    public static void HandleServiceClientQuarantineProposalShouldCreatePendingWithoutQuarantining()
    {
        SubmitServiceClientQuarantine command = ServiceClientQuarantineSubmit();

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        ServiceClientQuarantinePendingApproval pending = result.Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientQuarantinePendingApproval>();
        pending.QuarantineChangeId.ShouldBe(command.QuarantineChangeId);
        pending.ServiceClientRef.ShouldBe(command.ServiceClientRef);
        pending.RequesterActorId.ShouldBe("actor-alpha");
        pending.OldState.ShouldBe(ServiceClientControlState.Active);
        pending.NewState.ShouldBe(ServiceClientControlState.Quarantined);
        pending.SourceVersion.ShouldBe(command.SourceVersion + 1);

        // The proposal alone never quarantines the client: applying the pending event leaves no quarantined record.
        GovernedOperationState state = new();
        state.Apply(pending);
        state.QuarantinedServiceClients.ShouldBeEmpty();
        state.ServiceClientQuarantinePendingApprovals.ShouldContainKey(command.QuarantineChangeId);
    }

    [Fact]
    public static void HandleServiceClientQuarantineApprovalShouldRequirePendingAndDistinctSecondActor()
    {
        SubmitServiceClientQuarantine submit = ServiceClientQuarantineSubmit();
        ServiceClientQuarantinePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ServiceClientQuarantinePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveServiceClientQuarantine approval = ServiceClientQuarantineApproval();

        // Same requester ref as approver ref is rejected at the aggregate (defense in depth).
        DomainResult selfApprovalByRef = GovernedOperationAggregate.Handle(
            approval with { ApproverRef = submit.RequesterRef },
            state,
            Envelope(approval, "actor-beta"));
        // Same human actor (envelope.UserId) as the proposer is rejected even with a distinct approver ref.
        DomainResult selfApprovalByActor = GovernedOperationAggregate.Handle(approval, state, Envelope(approval));
        // A distinct second human actor applies the quarantine.
        DomainResult secondActorApproval = GovernedOperationAggregate.Handle(approval, state, Envelope(approval, "actor-beta"));

        selfApprovalByRef.IsRejection.ShouldBeTrue();
        selfApprovalByActor.IsRejection.ShouldBeTrue();
        ServiceClientQuarantined quarantined = secondActorApproval.Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientQuarantined>();
        quarantined.ServiceClientRef.ShouldBe(submit.ServiceClientRef);
        quarantined.RequesterRef.ShouldBe(submit.RequesterRef);
        quarantined.ApproverRef.ShouldBe(approval.ApproverRef);
        quarantined.OldState.ShouldBe(ServiceClientControlState.Active);
        quarantined.NewState.ShouldBe(ServiceClientControlState.Quarantined);

        state.Apply(quarantined);
        state.QuarantinedServiceClients.ShouldContainKey(submit.ServiceClientRef);
        state.ServiceClientQuarantinePendingApprovals.ShouldNotContainKey(submit.QuarantineChangeId);
    }

    [Fact]
    public static void HandleServiceClientQuarantineApprovalShouldRejectSubjectVersionOrReasonMismatch()
    {
        SubmitServiceClientQuarantine submit = ServiceClientQuarantineSubmit();
        ServiceClientQuarantinePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ServiceClientQuarantinePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveServiceClientQuarantine approval = ServiceClientQuarantineApproval();

        GovernedOperationAggregate.Handle(approval with { ServiceClientRef = "service-client:other" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { SourceVersion = approval.SourceVersion + 5 }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { ReasonCode = "different-reason" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();

        // An approval for an unknown pending change is rejected (no durable quarantine).
        GovernedOperationAggregate.Handle(
            approval with { QuarantineChangeId = "service-client-quarantine-unknown" },
            state,
            Envelope(approval, "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientQuarantineRejected>().ReasonCode
            .ShouldBe("service_client_quarantine_unavailable");
    }

    [Fact]
    public static void HandleServiceClientQuarantineProposalShouldNoOpForAlreadyQuarantinedOrDuplicate()
    {
        SubmitServiceClientQuarantine submit = ServiceClientQuarantineSubmit();
        ServiceClientQuarantinePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ServiceClientQuarantinePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);

        // A re-submit of the same pending quarantine change is a no-op (idempotency on the pending set).
        GovernedOperationAggregate.Handle(submit, state, Envelope(submit)).IsNoOp.ShouldBeTrue();
        GovernedOperationAggregate.Handle(submit, state, Envelope(submit)).Events.ShouldBeEmpty();

        ServiceClientQuarantined quarantined = GovernedOperationAggregate
            .Handle(ServiceClientQuarantineApproval(), state, Envelope(ServiceClientQuarantineApproval(), "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientQuarantined>();
        state.Apply(quarantined);

        // A fresh proposal for an already-quarantined subject is a no-op (idempotency on the quarantined set).
        GovernedOperationAggregate.Handle(
            submit with { QuarantineChangeId = "service-client-quarantine-002" },
            state,
            Envelope(submit)).Events.ShouldBeEmpty();
    }

    [Fact]
    public static void HandleServiceClientQuarantineShouldNotMutatePriorCommittedRecords()
    {
        // AC5 / NFR17: quarantine affects only FUTURE admission. Committing a quarantine for one subject must
        // never rewrite or remove already-committed records — a prior committed disable for a different service
        // client and an unrelated pending quarantine for a third subject both remain intact and reconstructable.
        GovernedOperationState state = new();

        // Prior committed disable for a different service client (an already-committed record).
        SubmitServiceClientDisable disableSubmit = ServiceClientDisableSubmit() with
        {
            DisableChangeId = "service-client-disable-900",
            ServiceClientRef = "service-client:legacy-client",
        };
        ServiceClientDisablePendingApproval disablePending = GovernedOperationAggregate
            .Handle(disableSubmit, state, Envelope(disableSubmit))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientDisablePendingApproval>();
        state.Apply(disablePending);
        ApproveServiceClientDisable disableApproval = ServiceClientDisableApproval() with
        {
            DisableChangeId = "service-client-disable-900",
            ServiceClientRef = "service-client:legacy-client",
        };
        ServiceClientDisabled disabled = GovernedOperationAggregate
            .Handle(disableApproval, state, Envelope(disableApproval, "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientDisabled>();
        state.Apply(disabled);

        // An unrelated pending quarantine for a third subject (an uncommitted record that must survive).
        SubmitServiceClientQuarantine unrelatedSubmit = ServiceClientQuarantineSubmit() with
        {
            QuarantineChangeId = "service-client-quarantine-700",
            ServiceClientRef = "service-client:third-client",
        };
        ServiceClientQuarantinePendingApproval unrelatedPending = GovernedOperationAggregate
            .Handle(unrelatedSubmit, state, Envelope(unrelatedSubmit))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientQuarantinePendingApproval>();
        state.Apply(unrelatedPending);

        // Quarantine the target subject through the two-person flow.
        SubmitServiceClientQuarantine submit = ServiceClientQuarantineSubmit();
        ServiceClientQuarantinePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, state, Envelope(submit))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientQuarantinePendingApproval>();
        state.Apply(pending);
        ServiceClientQuarantined quarantined = GovernedOperationAggregate
            .Handle(ServiceClientQuarantineApproval(), state, Envelope(ServiceClientQuarantineApproval(), "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientQuarantined>();
        state.Apply(quarantined);

        // The target is now quarantined...
        state.QuarantinedServiceClients.ShouldContainKey(submit.ServiceClientRef);
        // ...while every prior record remains intact: the committed disable is untouched (not rewritten to
        // quarantined), and the unrelated pending quarantine still awaits its own distinct second approver.
        state.DisabledServiceClients.ShouldContainKey("service-client:legacy-client");
        state.QuarantinedServiceClients.ShouldNotContainKey("service-client:legacy-client");
        state.ServiceClientQuarantinePendingApprovals.ShouldContainKey("service-client-quarantine-700");
    }

    [Fact]
    public static void HandleMailboxSourceQuarantineProposalShouldCreatePendingWithoutQuarantining()
    {
        SubmitMailboxSourceQuarantine command = MailboxSourceQuarantineSubmit();

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        MailboxSourceQuarantinePendingApproval pending = result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxSourceQuarantinePendingApproval>();
        pending.QuarantineChangeId.ShouldBe(command.QuarantineChangeId);
        pending.MailboxSourceRef.ShouldBe(command.MailboxSourceRef);
        pending.RequesterActorId.ShouldBe("actor-alpha");
        pending.OldState.ShouldBe(MailboxSourceControlState.Active);
        pending.NewState.ShouldBe(MailboxSourceControlState.Quarantined);
        pending.SourceVersion.ShouldBe(command.SourceVersion + 1);

        // The proposal alone never quarantines the source: applying the pending event leaves no quarantined record.
        GovernedOperationState state = new();
        state.Apply(pending);
        state.QuarantinedMailboxSources.ShouldBeEmpty();
        state.MailboxSourceQuarantinePendingApprovals.ShouldContainKey(command.QuarantineChangeId);
    }

    [Fact]
    public static void HandleMailboxSourceQuarantineApprovalShouldRequirePendingAndDistinctSecondActor()
    {
        SubmitMailboxSourceQuarantine submit = MailboxSourceQuarantineSubmit();
        MailboxSourceQuarantinePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<MailboxSourceQuarantinePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveMailboxSourceQuarantine approval = MailboxSourceQuarantineApproval();

        // Same requester ref as approver ref is rejected at the aggregate (defense in depth).
        DomainResult selfApprovalByRef = GovernedOperationAggregate.Handle(
            approval with { ApproverRef = submit.RequesterRef },
            state,
            Envelope(approval, "actor-beta"));
        // Same human actor (envelope.UserId) as the proposer is rejected even with a distinct approver ref.
        DomainResult selfApprovalByActor = GovernedOperationAggregate.Handle(approval, state, Envelope(approval));
        // A distinct second human actor applies the quarantine.
        DomainResult secondActorApproval = GovernedOperationAggregate.Handle(approval, state, Envelope(approval, "actor-beta"));

        selfApprovalByRef.IsRejection.ShouldBeTrue();
        selfApprovalByActor.IsRejection.ShouldBeTrue();
        MailboxSourceQuarantined quarantined = secondActorApproval.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxSourceQuarantined>();
        quarantined.MailboxSourceRef.ShouldBe(submit.MailboxSourceRef);
        quarantined.RequesterRef.ShouldBe(submit.RequesterRef);
        quarantined.ApproverRef.ShouldBe(approval.ApproverRef);
        quarantined.OldState.ShouldBe(MailboxSourceControlState.Active);
        quarantined.NewState.ShouldBe(MailboxSourceControlState.Quarantined);

        state.Apply(quarantined);
        state.QuarantinedMailboxSources.ShouldContainKey(submit.MailboxSourceRef);
        state.MailboxSourceQuarantinePendingApprovals.ShouldNotContainKey(submit.QuarantineChangeId);
    }

    [Fact]
    public static void HandleMailboxSourceQuarantineApprovalShouldRejectSubjectVersionOrReasonMismatch()
    {
        SubmitMailboxSourceQuarantine submit = MailboxSourceQuarantineSubmit();
        MailboxSourceQuarantinePendingApproval pending = GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<MailboxSourceQuarantinePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveMailboxSourceQuarantine approval = MailboxSourceQuarantineApproval();

        GovernedOperationAggregate.Handle(approval with { MailboxSourceRef = "mailbox-source:other" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { SourceVersion = approval.SourceVersion + 5 }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();
        GovernedOperationAggregate.Handle(approval with { ReasonCode = "different-reason" }, state, Envelope(approval, "actor-beta"))
            .IsRejection.ShouldBeTrue();

        // An approval for an unknown pending change is rejected (no durable quarantine).
        GovernedOperationAggregate.Handle(
            approval with { QuarantineChangeId = "mailbox-quarantine-unknown" },
            state,
            Envelope(approval, "actor-beta"))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxSourceQuarantineRejected>().ReasonCode
            .ShouldBe("mailbox_source_quarantine_unavailable");
    }

    [Fact]
    public static void HandleMailboxSourceQuarantineShouldNoOpForDuplicateOrAlreadyQuarantined()
    {
        SubmitMailboxSourceQuarantine submit = MailboxSourceQuarantineSubmit();

        // Duplicate pending proposal (same change id) is a no-op.
        GovernedOperationState pendingState = new();
        pendingState.Apply(GovernedOperationAggregate
            .Handle(submit, null, Envelope(submit))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<MailboxSourceQuarantinePendingApproval>());
        GovernedOperationAggregate.Handle(submit, pendingState, Envelope(submit)).IsNoOp.ShouldBeTrue();

        // An already-quarantined source short-circuits a fresh proposal to a no-op.
        GovernedOperationState quarantinedState = new();
        quarantinedState.Apply(new MailboxSourceQuarantined(
            submit.QuarantineChangeId,
            "tenant-alpha",
            submit.MailboxSourceRef,
            submit.RequesterRef,
            "admin-approver",
            submit.ReasonCode,
            submit.PolicySnapshotId,
            MailboxSourceControlState.Active,
            MailboxSourceControlState.Quarantined,
            DateTimeOffset.UtcNow,
            submit.SourceVersion + 1,
            submit.CorrelationId));
        GovernedOperationAggregate.Handle(submit with { QuarantineChangeId = "mailbox-quarantine-002" }, quarantinedState, Envelope(submit))
            .IsNoOp.ShouldBeTrue();
    }

    [Fact]
    public static void HandleMailboxSourceRateLimitShouldConfigureDirectlyWithoutPendingEvent()
    {
        SubmitMailboxSourceRateLimit command = MailboxSourceRateLimitSubmit();

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        // Single-actor direct activation: a single submit configures the budget — no pending-approval event.
        MailboxSourceRateLimitConfigured configured = result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxSourceRateLimitConfigured>();
        configured.RateLimitChangeId.ShouldBe(command.RateLimitChangeId);
        configured.MailboxSourceRef.ShouldBe(command.MailboxSourceRef);
        configured.RequesterActorId.ShouldBe("actor-alpha");
        configured.RequesterRef.ShouldBe(command.RequesterRef);
        configured.OldBudget.ShouldBe(command.OldBudget);
        configured.NewBudget.ShouldBe(command.NewBudget);
        configured.Window.ShouldBe(command.Window);
        configured.SourceVersion.ShouldBe(command.SourceVersion + 1);

        GovernedOperationState state = new();
        state.Apply(configured);
        state.MailboxSourceRateLimits.ShouldContainKey(command.MailboxSourceRef);
        state.MailboxSourceRateLimits[command.MailboxSourceRef].NewBudget.ShouldBe(command.NewBudget);
    }

    [Fact]
    public static void HandleMailboxSourceRateLimitShouldNoOpForIdenticalBudgetResubmit()
    {
        SubmitMailboxSourceRateLimit command = MailboxSourceRateLimitSubmit();
        GovernedOperationState state = new();
        state.Apply(GovernedOperationAggregate
            .Handle(command, null, Envelope(command))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<MailboxSourceRateLimitConfigured>());

        // Re-submitting the identical budget for the same source is a no-op (single durable effect).
        GovernedOperationAggregate.Handle(command with { RateLimitChangeId = "mailbox-rate-limit-002" }, state, Envelope(command))
            .IsNoOp.ShouldBeTrue();

        // A different budget for the same source configures a new value (not a no-op).
        GovernedOperationAggregate.Handle(command with { RateLimitChangeId = "mailbox-rate-limit-003", NewBudget = 50 }, state, Envelope(command))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxSourceRateLimitConfigured>().NewBudget.ShouldBe(50);
    }

    [Fact]
    public static void HandleMailboxSourceRateLimitShouldRejectOutOfBoundsBudget()
    {
        SubmitMailboxSourceRateLimit command = MailboxSourceRateLimitSubmit() with { NewBudget = MailboxRateLimitBounds.Maximum + 1 };

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxSourceRateLimitRejected>().ReasonCode
            .ShouldBe("mailbox_source_rate_limit_out_of_bounds");
    }

    [Fact]
    public static void HandleServiceClientRateLimitShouldConfigureDirectlyWithoutPendingEvent()
    {
        SubmitServiceClientRateLimit command = ServiceClientRateLimitSubmit();

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        // Single-actor direct activation: a single submit configures the budget — no pending-approval event.
        ServiceClientRateLimitConfigured configured = result.Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientRateLimitConfigured>();
        configured.RateLimitChangeId.ShouldBe(command.RateLimitChangeId);
        configured.ServiceClientRef.ShouldBe(command.ServiceClientRef);
        configured.RequesterActorId.ShouldBe("actor-alpha");
        configured.RequesterRef.ShouldBe(command.RequesterRef);
        configured.OldBudget.ShouldBe(command.OldBudget);
        configured.NewBudget.ShouldBe(command.NewBudget);
        configured.Window.ShouldBe(command.Window);
        configured.SourceVersion.ShouldBe(command.SourceVersion + 1);

        GovernedOperationState state = new();
        state.Apply(configured);
        state.ServiceClientRateLimits.ShouldContainKey(command.ServiceClientRef);
        state.ServiceClientRateLimits[command.ServiceClientRef].NewBudget.ShouldBe(command.NewBudget);
    }

    [Fact]
    public static void HandleServiceClientRateLimitShouldNoOpForIdenticalBudgetResubmit()
    {
        SubmitServiceClientRateLimit command = ServiceClientRateLimitSubmit();
        GovernedOperationState state = new();
        state.Apply(GovernedOperationAggregate
            .Handle(command, null, Envelope(command))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ServiceClientRateLimitConfigured>());

        // Re-submitting the identical budget for the same client is a no-op (single durable effect).
        GovernedOperationAggregate.Handle(command with { RateLimitChangeId = "service-client-rate-limit-002" }, state, Envelope(command))
            .IsNoOp.ShouldBeTrue();

        // A different budget for the same client configures a new value (not a no-op).
        GovernedOperationAggregate.Handle(command with { RateLimitChangeId = "service-client-rate-limit-003", NewBudget = 50 }, state, Envelope(command))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientRateLimitConfigured>().NewBudget.ShouldBe(50);
    }

    [Fact]
    public static void HandleServiceClientRateLimitShouldRejectOutOfBoundsBudget()
    {
        SubmitServiceClientRateLimit command = ServiceClientRateLimitSubmit() with { NewBudget = ServiceClientRateLimitBounds.Maximum + 1 };

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientRateLimitRejected>().ReasonCode
            .ShouldBe("service_client_rate_limit_out_of_bounds");
    }

    [Fact]
    public static void HandleServiceClientRateLimitShouldRejectInvalidMetadata()
    {
        SubmitServiceClientRateLimit command = ServiceClientRateLimitSubmit() with { ReasonCode = "unsafe reason" };

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientRateLimitRejected>().ReasonCode
            .ShouldBe("invalid_service_client_rate_limit");
    }

    [Fact]
    public static void ServiceClientRateLimitsShouldKeepEachServiceClientBudgetIndependent()
    {
        // NFR30/AC10 isolation at the state-projection level: two different service clients each carry their own
        // budget. Configuring one client's limit never overwrites a sibling's, and re-configuring the first leaves
        // the second's prior committed budget untouched (admins mutate only the targeted client — NFR17/FR75c).
        SubmitServiceClientRateLimit noisy = ServiceClientRateLimitSubmit() with
        {
            ServiceClientRef = "service-client:noisy-client",
            NewBudget = 100,
        };
        SubmitServiceClientRateLimit quiet = ServiceClientRateLimitSubmit() with
        {
            RateLimitChangeId = "service-client-rate-limit-009",
            ServiceClientRef = "service-client:quiet-client",
            NewBudget = 9000,
        };

        GovernedOperationState state = new();
        state.Apply(GovernedOperationAggregate.Handle(noisy, null, Envelope(noisy))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientRateLimitConfigured>());
        state.Apply(GovernedOperationAggregate.Handle(quiet, state, Envelope(quiet))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientRateLimitConfigured>());

        // Both budgets coexist independently.
        state.ServiceClientRateLimits[noisy.ServiceClientRef].NewBudget.ShouldBe(100);
        state.ServiceClientRateLimits[quiet.ServiceClientRef].NewBudget.ShouldBe(9000);

        // Re-tightening the noisy client to 0 must not disturb the quiet client's committed budget.
        SubmitServiceClientRateLimit retighten = noisy with
        {
            RateLimitChangeId = "service-client-rate-limit-010",
            OldBudget = 100,
            NewBudget = 0,
        };
        state.Apply(GovernedOperationAggregate.Handle(retighten, state, Envelope(retighten))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ServiceClientRateLimitConfigured>());

        state.ServiceClientRateLimits[noisy.ServiceClientRef].NewBudget.ShouldBe(0);
        state.ServiceClientRateLimits[quiet.ServiceClientRef].NewBudget.ShouldBe(9000);
    }

    [Fact]
    public static void HandleAiActorRateLimitShouldConfigureDirectlyWithoutPendingEvent()
    {
        SubmitAiActorRateLimit command = AiActorRateLimitSubmit();

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        // Single-actor direct activation: a single submit configures the budget — no pending-approval event.
        AiActorRateLimitConfigured configured = result.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorRateLimitConfigured>();
        configured.RateLimitChangeId.ShouldBe(command.RateLimitChangeId);
        configured.AiActorRef.ShouldBe(command.AiActorRef);
        configured.RequesterActorId.ShouldBe("actor-alpha");
        configured.RequesterRef.ShouldBe(command.RequesterRef);
        configured.OldBudget.ShouldBe(command.OldBudget);
        configured.NewBudget.ShouldBe(command.NewBudget);
        configured.Window.ShouldBe(command.Window);
        configured.SourceVersion.ShouldBe(command.SourceVersion + 1);

        GovernedOperationState state = new();
        state.Apply(configured);
        state.AiActorRateLimits.ShouldContainKey(command.AiActorRef);
        state.AiActorRateLimits[command.AiActorRef].NewBudget.ShouldBe(command.NewBudget);
    }

    [Fact]
    public static void HandleAiActorRateLimitShouldNoOpForIdenticalBudgetResubmit()
    {
        SubmitAiActorRateLimit command = AiActorRateLimitSubmit();
        GovernedOperationState state = new();
        state.Apply(GovernedOperationAggregate
            .Handle(command, null, Envelope(command))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<AiActorRateLimitConfigured>());

        // Re-submitting the identical budget for the same AI actor is a no-op (single durable effect).
        GovernedOperationAggregate.Handle(command with { RateLimitChangeId = "ai-actor-rate-limit-002" }, state, Envelope(command))
            .IsNoOp.ShouldBeTrue();

        // A different budget for the same AI actor configures a new value (not a no-op).
        GovernedOperationAggregate.Handle(command with { RateLimitChangeId = "ai-actor-rate-limit-003", NewBudget = 50 }, state, Envelope(command))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorRateLimitConfigured>().NewBudget.ShouldBe(50);
    }

    [Fact]
    public static void HandleAiActorRateLimitShouldRejectOutOfBoundsBudget()
    {
        SubmitAiActorRateLimit command = AiActorRateLimitSubmit() with { NewBudget = AiActorRateLimitBounds.Maximum + 1 };

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorRateLimitRejected>().ReasonCode
            .ShouldBe("ai_actor_rate_limit_out_of_bounds");
    }

    [Fact]
    public static void HandleAiActorRateLimitShouldRejectInvalidMetadata()
    {
        SubmitAiActorRateLimit command = AiActorRateLimitSubmit() with { ReasonCode = "unsafe reason" };

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorRateLimitRejected>().ReasonCode
            .ShouldBe("invalid_ai_actor_rate_limit");
    }

    [Fact]
    public static void AiActorRateLimitsShouldKeepEachAiActorBudgetIndependent()
    {
        // NFR30/AC10 isolation at the state-projection level: two different AI actors each carry their own budget.
        // Configuring one AI actor's limit never overwrites a sibling's, and re-configuring the first leaves the
        // second's prior committed budget untouched (admins mutate only the targeted AI actor — NFR17/FR75c).
        SubmitAiActorRateLimit noisy = AiActorRateLimitSubmit() with
        {
            AiActorRef = "ai-actor:noisy-actor",
            NewBudget = 100,
        };
        SubmitAiActorRateLimit quiet = AiActorRateLimitSubmit() with
        {
            RateLimitChangeId = "ai-actor-rate-limit-009",
            AiActorRef = "ai-actor:quiet-actor",
            NewBudget = 900,
        };

        GovernedOperationState state = new();
        state.Apply(GovernedOperationAggregate.Handle(noisy, null, Envelope(noisy))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorRateLimitConfigured>());
        state.Apply(GovernedOperationAggregate.Handle(quiet, state, Envelope(quiet))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorRateLimitConfigured>());

        // Both budgets coexist independently.
        state.AiActorRateLimits[noisy.AiActorRef].NewBudget.ShouldBe(100);
        state.AiActorRateLimits[quiet.AiActorRef].NewBudget.ShouldBe(900);

        // Re-tightening the noisy AI actor to 0 must not disturb the quiet AI actor's committed budget.
        SubmitAiActorRateLimit retighten = noisy with
        {
            RateLimitChangeId = "ai-actor-rate-limit-010",
            OldBudget = 100,
            NewBudget = 0,
        };
        state.Apply(GovernedOperationAggregate.Handle(retighten, state, Envelope(retighten))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<AiActorRateLimitConfigured>());

        state.AiActorRateLimits[noisy.AiActorRef].NewBudget.ShouldBe(0);
        state.AiActorRateLimits[quiet.AiActorRef].NewBudget.ShouldBe(900);
    }

    [Fact]
    public static void HandleCommandCapabilityRateLimitShouldConfigureDirectlyWithoutPendingEvent()
    {
        SubmitCommandCapabilityRateLimit command = CommandCapabilityRateLimitSubmit();

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        // Single-actor direct activation: a single submit configures the budget — no pending-approval event.
        CommandCapabilityRateLimitConfigured configured = result.Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityRateLimitConfigured>();
        configured.RateLimitChangeId.ShouldBe(command.RateLimitChangeId);
        configured.CommandCapabilityRef.ShouldBe(command.CommandCapabilityRef);
        configured.RequesterActorId.ShouldBe("actor-alpha");
        configured.RequesterRef.ShouldBe(command.RequesterRef);
        configured.OldBudget.ShouldBe(command.OldBudget);
        configured.NewBudget.ShouldBe(command.NewBudget);
        configured.Window.ShouldBe(command.Window);
        configured.SourceVersion.ShouldBe(command.SourceVersion + 1);

        GovernedOperationState state = new();
        state.Apply(configured);
        state.CommandCapabilityRateLimits.ShouldContainKey(command.CommandCapabilityRef);
        state.CommandCapabilityRateLimits[command.CommandCapabilityRef].NewBudget.ShouldBe(command.NewBudget);
    }

    [Fact]
    public static void HandleCommandCapabilityRateLimitShouldNoOpForIdenticalBudgetResubmit()
    {
        SubmitCommandCapabilityRateLimit command = CommandCapabilityRateLimitSubmit();
        GovernedOperationState state = new();
        state.Apply(GovernedOperationAggregate
            .Handle(command, null, Envelope(command))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<CommandCapabilityRateLimitConfigured>());

        // Re-submitting the identical budget for the same command type is a no-op (single durable effect, zero events).
        GovernedOperationAggregate.Handle(command with { RateLimitChangeId = "command-capability-rate-limit-002" }, state, Envelope(command))
            .IsNoOp.ShouldBeTrue();

        // A different budget for the same command type configures a new value (not a no-op).
        GovernedOperationAggregate.Handle(command with { RateLimitChangeId = "command-capability-rate-limit-003", NewBudget = 50 }, state, Envelope(command))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityRateLimitConfigured>().NewBudget.ShouldBe(50);
    }

    [Fact]
    public static void HandleCommandCapabilityRateLimitShouldRejectOutOfBoundsBudget()
    {
        SubmitCommandCapabilityRateLimit command = CommandCapabilityRateLimitSubmit() with { NewBudget = CommandCapabilityRateLimitBounds.Maximum + 1 };

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityRateLimitRejected>().ReasonCode
            .ShouldBe("command_capability_rate_limit_out_of_bounds");
    }

    [Fact]
    public static void HandleCommandCapabilityRateLimitShouldRejectInvalidMetadata()
    {
        SubmitCommandCapabilityRateLimit command = CommandCapabilityRateLimitSubmit() with { ReasonCode = "unsafe reason" };

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityRateLimitRejected>().ReasonCode
            .ShouldBe("invalid_command_capability_rate_limit");
    }

    [Fact]
    public static void CommandCapabilityRateLimitsShouldKeepEachCommandTypeBudgetIndependent()
    {
        // NFR30/AC10 isolation at the state-projection level: two different command types each carry their own budget.
        // Configuring one type's limit never overwrites a sibling's, and re-configuring the first leaves the second's
        // prior committed budget untouched (admins mutate only the targeted command type — NFR17/FR75c).
        SubmitCommandCapabilityRateLimit noisy = CommandCapabilityRateLimitSubmit() with
        {
            CommandCapabilityRef = "AssociateEmailToProject",
            NewBudget = 100,
        };
        SubmitCommandCapabilityRateLimit quiet = CommandCapabilityRateLimitSubmit() with
        {
            RateLimitChangeId = "command-capability-rate-limit-009",
            CommandCapabilityRef = "MarkEmailAssociationNeedsReview",
            NewBudget = 900,
        };

        GovernedOperationState state = new();
        state.Apply(GovernedOperationAggregate.Handle(noisy, null, Envelope(noisy))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityRateLimitConfigured>());
        state.Apply(GovernedOperationAggregate.Handle(quiet, state, Envelope(quiet))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityRateLimitConfigured>());

        state.CommandCapabilityRateLimits[noisy.CommandCapabilityRef].NewBudget.ShouldBe(100);
        state.CommandCapabilityRateLimits[quiet.CommandCapabilityRef].NewBudget.ShouldBe(900);

        // Re-tightening the noisy command type to 0 must not disturb the quiet command type's committed budget.
        SubmitCommandCapabilityRateLimit retighten = noisy with
        {
            RateLimitChangeId = "command-capability-rate-limit-010",
            OldBudget = 100,
            NewBudget = 0,
        };
        state.Apply(GovernedOperationAggregate.Handle(retighten, state, Envelope(retighten))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<CommandCapabilityRateLimitConfigured>());

        state.CommandCapabilityRateLimits[noisy.CommandCapabilityRef].NewBudget.ShouldBe(0);
        state.CommandCapabilityRateLimits[quiet.CommandCapabilityRef].NewBudget.ShouldBe(900);
    }

    [Fact]
    public static void HandleLowRiskAiExecutionRoutedToApprovalShouldNotEmitExecutionStarted()
    {
        ExecuteLowRiskAIAssistance command = LowRiskExecutionCommand("pending-approval", "low_risk_policy_false");

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(2);
        LowRiskAiAssistanceRoutedToApproval routed = result.Events[0].ShouldBeOfType<LowRiskAiAssistanceRoutedToApproval>();
        routed.Record.SafeNextAction.ShouldBe("review-ai-action");
        routed.Record.PolicyReasonCode.ShouldBe("low_risk_policy_false");
        AiActionApprovalRequested approval = result.Events[1].ShouldBeOfType<AiActionApprovalRequested>();
        approval.ProposalId.ShouldBe(command.ProposalId);
        approval.SourceMessageId.ShouldBe(command.SourceMessageId);
    }

    [Fact]
    public static void HandleLowRiskAiExecutionShouldRejectSuccessWithApprovalNextAction()
    {
        ExecuteLowRiskAIAssistance command = LowRiskExecutionCommand("success") with
        {
            ExecutionRecord = LowRiskExecutionCommand("success").ExecutionRecord! with
            {
                SafeNextAction = "review-ai-action",
            },
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
    }

    [Fact]
    public static void HandleLowRiskAiExecutionShouldBeIdempotentForExistingExecutionId()
    {
        ExecuteLowRiskAIAssistance command = LowRiskExecutionCommand("failed");
        GovernedOperationState state = new();
        state.Apply(new LowRiskAiAssistanceExecutionStarted(
            command.ExecutionId,
            command.ProposalId,
            command.ProjectId,
            command.TaskIntentId,
            command.SourceMessageId,
            command.RequesterId,
            "summarize-visible-context",
            command.ContextPackageId,
            command.ContextPackageVersion,
            "policy-snap-001",
            "low-risk-execute-allowed",
            command.ExpectedProposalSourceVersion,
            command.CorrelationId,
            DateTimeOffset.UtcNow));

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public static void HandleApprovedAiActionExecutionShouldRequireApprovedAllowlistedCommand()
    {
        ExecuteApprovedAIAction command = ApprovedExecutionCommand();
        GovernedOperationState state = ApprovedExecutionState();

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(2);
        ApprovedAiActionExecutionStarted started = result.Events[0].ShouldBeOfType<ApprovedAiActionExecutionStarted>();
        started.CommandName.ShouldBe(AiActionCommandMetadataProvider.AppendConversationMessageCommandName);
        started.CommandAllowlistVersion.ShouldBe(AiActionCommandMetadataProvider.M0AllowlistVersion);
        ApprovedAiActionExecutionSucceeded succeeded = result.Events[1].ShouldBeOfType<ApprovedAiActionExecutionSucceeded>();
        succeeded.Record.SafeNextAction.ShouldBe("none");
    }

    [Fact]
    public static void HandleApprovedAiActionExecutionShouldRejectNonAllowlistedCommand()
    {
        ExecuteApprovedAIAction command = ApprovedExecutionCommand() with
        {
            CommandName = "Project.SendEmail",
            ExecutionRecord = ApprovedExecutionRecord(commandName: "Project.SendEmail"),
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, ApprovedExecutionState(), Envelope(command));

        result.IsRejection.ShouldBeTrue();
        ApprovedAiActionExecutionRejected rejection = result.Events.ShouldHaveSingleItem().ShouldBeOfType<ApprovedAiActionExecutionRejected>();
        rejection.ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.CommandNotAllowlisted);
        rejection.ProjectId.ShouldBe("project-001");
        rejection.RequesterId.ShouldBe("party-001");
        rejection.SourceMessageId.ShouldBe("graph-message-001");
    }

    [Fact]
    public static void HandleApprovedAiActionExecutionShouldRejectNonApproveDecision()
    {
        ExecuteApprovedAIAction command = ApprovedExecutionCommand();
        GovernedOperationState state = ApprovedExecutionState(ApprovalDecisionKind.Reject);

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<ApprovedAiActionExecutionRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.ApprovalStateInvalid);
    }

    [Fact]
    public static void HandleApprovedAiActionExecutionShouldRejectStaleApprovalEvidence()
    {
        ExecuteApprovedAIAction command = ApprovedExecutionCommand();
        GovernedOperationState state = ApprovedExecutionState(
            ApprovalDecisionKind.Approve,
            [ApprovalEvidenceFreshness.Stale]);

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<ApprovedAiActionExecutionRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.EvidenceExpired);
    }

    [Fact]
    public static void HandleApprovedAiActionExecutionShouldTreatEquivalentReplayAsNoOpAndConflictAsRejection()
    {
        ExecuteApprovedAIAction command = ApprovedExecutionCommand();
        GovernedOperationState state = ApprovedExecutionState();
        state.Apply(new ApprovedAiActionExecutionStarted(
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
            command.PolicySnapshotId!,
            command.CorrelationId,
            DateTimeOffset.UtcNow));

        DomainResult replay = GovernedOperationAggregate.Handle(command, state, Envelope(command));
        DomainResult conflict = GovernedOperationAggregate.Handle(command with { ExpectedProposalSourceVersion = 8 }, state, Envelope(command));

        replay.IsNoOp.ShouldBeTrue();
        conflict.IsRejection.ShouldBeTrue();
        conflict.Events.ShouldHaveSingleItem().ShouldBeOfType<ApprovedAiActionExecutionRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.ApprovalStateInvalid);
    }

    [Fact]
    public static void HandleOnNewAggregateShouldRecordTheNote()
    {
        DomainResult result = GovernedOperationAggregate.Handle(new RecordGovernedNote(NoteId), state: null);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        GovernedNoteRecorded recorded = result.Events[0].ShouldBeOfType<GovernedNoteRecorded>();
        recorded.NoteId.ShouldBe(NoteId);
    }

    [Fact]
    public static void HandleCreateOutboundDraftShouldCreateLocalDraftWithoutExternalOutcome()
    {
        CreateOutboundDraft command = OutboundDraftCommand();

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        OutboundDraftCreated created = result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundDraftCreated>();
        created.DraftId.ShouldBe(command.DraftId);
        created.ProjectId.ShouldBe(command.ProjectId);
        created.SenderAuthorityClass.ShouldBe(SenderAuthorityClass.DraftOnly);
        created.RecipientRefs.ShouldBe(command.RecipientRefs);
        created.GovernedContent.ContentText.ShouldBe("Governed draft content.");
    }

    [Fact]
    public static void HandleCreateOutboundDraftShouldReplayEquivalentAndRejectConflictingDuplicate()
    {
        CreateOutboundDraft command = OutboundDraftCommand();
        GovernedOperationState state = new();
        state.Apply(new OutboundDraftCreated(
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
            command.RetentionClass));

        DomainResult replay = GovernedOperationAggregate.Handle(command, state, Envelope(command));
        DomainResult conflict = GovernedOperationAggregate.Handle(
            command with { GovernedContent = command.GovernedContent with { ContentText = "Changed content." } },
            state,
            Envelope(command));

        replay.IsNoOp.ShouldBeTrue();
        conflict.IsRejection.ShouldBeTrue();
        conflict.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundDraftCreationRejected>().ReasonCode
            .ShouldBe("idempotency_conflict_outbound_draft_creation");
    }

    [Fact]
    public static void HandleCreateOutboundDraftShouldRejectNonDraftAuthorityAndSendPosture()
    {
        CreateOutboundDraft command = OutboundDraftCommand() with
        {
            SenderAuthorityClass = SenderAuthorityClass.AuthenticatedUserSend,
            HasM365SendPosture = true,
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundDraftCreationRejected>().ReasonCode
            .ShouldBe(ChatBotDisabledActionReasons.PolicyBlocked);
    }

    [Fact]
    public static void HandleOutboundApprovalRequestShouldPreserveDraftContentAndProjectApprovalMetadata()
    {
        GovernedOperationState state = OutboundApprovalState(includeRequest: false, includeDecision: false);
        RequestOutboundSendApproval command = OutboundApprovalRequest();

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        OutboundApprovalRequested requested = result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundApprovalRequested>();
        requested.ApprovalId.ShouldBe(command.ApprovalId);
        requested.DraftId.ShouldBe(command.DraftId);
        requested.RecipientRefs.ShouldBe(command.RecipientRefs);
        requested.SenderAuthorityClass.ShouldBe(SenderAuthorityClass.AuthenticatedUserSend);
        requested.EvidenceFreshness.ShouldBe(ApprovalEvidenceFreshness.Fresh);
        requested.ContentSnapshot.ProposedContent.ContentText.ShouldBe("Governed draft content.");
        requested.ContentSnapshot.PublicRedactionState.ShouldBe("metadata_only");

        state.Apply(requested);
        GovernedOperationAggregate.Handle(command, state, Envelope(command)).IsNoOp.ShouldBeTrue();
    }

    [Theory]
    [InlineData(ApprovalDecisionKind.Approve, "send-approved-outbound-draft")]
    [InlineData(ApprovalDecisionKind.Reject, "none")]
    [InlineData(ApprovalDecisionKind.RequestRevision, "revise-outbound-draft")]
    [InlineData(ApprovalDecisionKind.Cancel, "none")]
    public static void HandleOutboundApprovalDecisionShouldRecordAllDecisionKindsAppendOnly(
        ApprovalDecisionKind decision,
        string expectedNextAction)
    {
        GovernedOperationState state = OutboundApprovalState(includeRequest: true, includeDecision: false);
        DecideOutboundApproval command = OutboundApprovalDecision(decision);

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        OutboundApprovalDecisionRecorded recorded = result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundApprovalDecisionRecorded>();
        recorded.DecisionKind.ShouldBe(decision);
        recorded.SafeNextAction.ShouldBe(expectedNextAction);
        recorded.ContentSnapshot.ApprovedContent.ShouldBe(decision is ApprovalDecisionKind.Approve ? command.ApprovedContent : null);

        state.Apply(recorded);
        GovernedOperationAggregate.Handle(command, state, Envelope(command)).IsNoOp.ShouldBeTrue();
        ApprovalDecisionKind conflictingDecision = decision is ApprovalDecisionKind.Cancel
            ? ApprovalDecisionKind.Approve
            : ApprovalDecisionKind.Cancel;
        GovernedOperationAggregate
            .Handle(command with { Decision = conflictingDecision, DecisionId = "decision-002" }, state, Envelope(command))
            .IsRejection
            .ShouldBeTrue();
    }

    [Fact]
    public static void HandleOutboundApprovalDecisionShouldRejectApproveWhenEvidenceExpiredButAllowReject()
    {
        GovernedOperationState state = OutboundApprovalState(
            includeRequest: true,
            includeDecision: false,
            freshness: ApprovalEvidenceFreshness.Expired);

        DomainResult approve = GovernedOperationAggregate.Handle(
            OutboundApprovalDecision(ApprovalDecisionKind.Approve),
            state,
            Envelope(OutboundApprovalDecision(ApprovalDecisionKind.Approve)));
        DomainResult reject = GovernedOperationAggregate.Handle(
            OutboundApprovalDecision(ApprovalDecisionKind.Reject),
            state,
            Envelope(OutboundApprovalDecision(ApprovalDecisionKind.Reject)));

        approve.IsRejection.ShouldBeTrue();
        approve.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundApprovalDecisionRejected>().ReasonCode.ShouldBe("evidence-expired");
        reject.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public static void HandleOutboundSendShouldRequireApprovedDecisionAndRecordSingleShotOutcome()
    {
        GovernedOperationState state = OutboundApprovalState(includeRequest: true, includeDecision: true);
        ExecuteApprovedOutboundDraft command = OutboundSendCommand();

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(3);
        OutboundSendStarted started = result.Events[0].ShouldBeOfType<OutboundSendStarted>();
        started.SendKey.ShouldBe("tenant-alpha:draft-001:actor-alpha");
        started.AuthorityResult.DenialReason.ShouldBeNull();
        result.Events[1].ShouldBeOfType<OutboundSendSucceeded>().AdapterRef.ShouldBe("adapter:mailbox-outbound");
        result.Events[2].ShouldBeOfType<OutboundApprovalOutcomeRecorded>().CommandOutcomeStatus.ShouldBe("sent");

        state.Apply(started);
        DomainResult duplicate = GovernedOperationAggregate.Handle(command with { SendId = "send-002" }, state, Envelope(command));

        duplicate.IsRejection.ShouldBeTrue();
        duplicate.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundSendRejected>().ReasonCode
            .ShouldBe("idempotency_conflict_outbound_send");
    }

    [Fact]
    public static void HandleOutboundSendShouldRejectApprovalScopeMismatch()
    {
        GovernedOperationState state = OutboundApprovalState(includeRequest: true, includeDecision: true);
        ExecuteApprovedOutboundDraft command = OutboundSendCommand() with
        {
            PolicySnapshotId = "policy-snapshot-other",
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundSendRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.ApprovalStateInvalid);
    }

    [Theory]
    [InlineData(ApprovalEvidenceFreshness.Stale)]
    [InlineData(ApprovalEvidenceFreshness.Expired)]
    public static void HandleOutboundSendShouldRejectNonFreshEvidenceAtSendTime(ApprovalEvidenceFreshness freshness)
    {
        GovernedOperationState state = OutboundApprovalState(includeRequest: true, includeDecision: true);
        ExecuteApprovedOutboundDraft command = OutboundSendCommand() with { EvidenceFreshness = freshness };

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundSendRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.ApprovalStateInvalid);
    }

    [Fact]
    public static void HandleOutboundSendShouldFailClosedWithOutboundChannelDisabledReasonWhenChannelBlocked()
    {
        // Story 7.24: when the dispatcher short-circuits a Disabled outbound channel it marks the send "blocked". The
        // aggregate maps that to the finite outbound_channel_disabled reason — distinct from outbound_adapter_unavailable
        // — via the same RejectOutboundSend fail-closed path (no OutboundSendSucceeded, no external dispatch recorded).
        GovernedOperationState state = OutboundApprovalState(includeRequest: true, includeDecision: true);
        ExecuteApprovedOutboundDraft command = OutboundSendCommand() with { AdapterStatus = "blocked" };

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        string reason = result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundSendRejected>().ReasonCode;
        reason.ShouldBe("outbound_channel_disabled");
        reason.ShouldNotBe("outbound_adapter_unavailable");

        // The adapter-not-configured/unreachable case still maps to the distinct outbound_adapter_unavailable reason.
        DomainResult unavailable = GovernedOperationAggregate.Handle(
            OutboundSendCommand() with { AdapterStatus = "unavailable" },
            OutboundApprovalState(includeRequest: true, includeDecision: true),
            Envelope(command));
        unavailable.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundSendRejected>().ReasonCode
            .ShouldBe("outbound_adapter_unavailable");
    }

    [Theory]
    [InlineData(ApprovalDecisionKind.Reject, ChatBotRefusalReasonCodes.ApprovalStateInvalid)]
    [InlineData(ApprovalDecisionKind.RequestRevision, ChatBotRefusalReasonCodes.ApprovalStateInvalid)]
    [InlineData(ApprovalDecisionKind.Cancel, ChatBotRefusalReasonCodes.ApprovalStateInvalid)]
    public static void HandleOutboundSendShouldNeverSendForNonApproveDecisions(
        ApprovalDecisionKind decision,
        string expectedReason)
    {
        GovernedOperationState state = OutboundApprovalState(includeRequest: true, includeDecision: true, decision: decision);
        ExecuteApprovedOutboundDraft command = OutboundSendCommand();

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundSendRejected>().ReasonCode.ShouldBe(expectedReason);
    }

    [Fact]
    public static void HandleOnAlreadyRecordedAggregateShouldRejectWithoutThrowing()
    {
        GovernedOperationState state = new();
        state.Apply(new GovernedNoteRecorded(NoteId));

        DomainResult result = GovernedOperationAggregate.Handle(new RecordGovernedNote(NoteId), state);

        result.IsRejection.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        GovernedNoteAlreadyRecordedRejection rejection = result.Events[0].ShouldBeOfType<GovernedNoteAlreadyRecordedRejection>();
        rejection.NoteId.ShouldBe(NoteId);
        result.Events[0].ShouldBeAssignableTo<IRejectionEvent>();
    }

    [Fact]
    public static void ApplyShouldBeIdempotentOnReplay()
    {
        GovernedOperationState state = new();
        state.IsRecorded.ShouldBeFalse();

        state.Apply(new GovernedNoteRecorded(NoteId));
        state.IsRecorded.ShouldBeTrue();
        state.NoteId.ShouldBe(NoteId);

        // A duplicate event during replay must leave state unchanged (order-tolerant, idempotent).
        state.Apply(new GovernedNoteRecorded(NoteId));
        state.IsRecorded.ShouldBeTrue();
        state.NoteId.ShouldBe(NoteId);
    }

    [Fact]
    public static async Task ProcessAsyncShouldDiscoverHandleByReflectionAndProduceTheEvent()
    {
        GovernedOperationAggregate aggregate = new();
        CommandEnvelope command = Envelope(new RecordGovernedNote(NoteId));

        DomainResult result = await aggregate.ProcessAsync(command, currentState: null);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<GovernedNoteRecorded>().NoteId.ShouldBe(NoteId);
    }

    [Fact]
    public static void HandleMailboxIntakeShouldCaptureSourceIdentityAndNormalizeTimestampsToUtc()
    {
        CaptureMailboxMessageIntake command = MailboxCommand();

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null);

        result.IsSuccess.ShouldBeTrue();
        MailboxMessageIntakeCaptured captured = result.Events[0].ShouldBeOfType<MailboxMessageIntakeCaptured>();
        captured.IntakeId.ShouldBe(IntakeId);
        captured.ProviderMessageId.ShouldBe("graph-message-001");
        captured.InternetMessageId.ShouldBe("<message-001@example.test>");
        captured.ConversationId.ShouldBe("graph-conversation-001");
        captured.MailboxId.ShouldBe("controlled-mailbox-001");
        captured.Sender.Address.ShouldBe("sender@example.test");
        captured.Recipients.Single().Address.ShouldBe("project@example.test");
        captured.AttachmentReferences.Single().ProviderAttachmentId.ShouldBe("attachment-001");
        captured.ReceivedAtUtc.Offset.ShouldBe(TimeSpan.Zero);
        captured.ReceivedAtUtc.ShouldBe(new DateTimeOffset(2026, 5, 30, 8, 15, 0, TimeSpan.Zero));
        captured.SourceTimezone.ShouldBe("W. Europe Standard Time");
        captured.SourceProvenance.ShouldBe("m365-mailbox-intake");
        captured.RedactionState.ShouldBe("metadata_only");
        captured.RetentionClass.ShouldBe("collaboration_input");
    }

    [Fact]
    public static void HandleMailboxIntakeShouldPersistAuthenticityMetadataWithoutBlockingMalformedVerdicts()
    {
        CaptureMailboxMessageIntake command = MailboxCommand() with
        {
            Authenticity = MailboxAuthenticity(),
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null);

        result.IsSuccess.ShouldBeTrue();
        MailboxAuthenticityMetadata authenticity = result.Events[0]
            .ShouldBeOfType<MailboxMessageIntakeCaptured>()
            .Authenticity
            .ShouldNotBeNull();
        authenticity.AuthenticationResults.Spf.ShouldBe(MailboxAuthenticationVerdictKind.Malformed);
        authenticity.AuthenticationResults.Dkim.ShouldBe(MailboxAuthenticationVerdictKind.NotSupplied);
        authenticity.HeaderInspection.From.ShouldBe(MailboxHeaderValueState.Malformed);
        authenticity.HeaderInspection.Discrepancies.ShouldContain(MailboxHeaderDiscrepancyKind.MalformedFrom);
    }

    [Fact]
    public static void HandleMailboxIntakeShouldRejectUnboundedAuthenticityDiscrepancyShape()
    {
        CaptureMailboxMessageIntake command = MailboxCommand() with
        {
            Authenticity = MailboxAuthenticity() with
            {
                HeaderInspection = MailboxAuthenticity().HeaderInspection with
                {
                    Discrepancies = Enumerable.Repeat(MailboxHeaderDiscrepancyKind.MalformedFrom, 33).ToArray(),
                },
            },
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null);

        result.IsSuccess.ShouldBeFalse();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxMessageIntakeInvalidRejection>();
    }

    [Fact]
    public static void HandleMailboxIntakeShouldRejectDuplicateAuthenticityDiscrepancyCodes()
    {
        CaptureMailboxMessageIntake command = MailboxCommand() with
        {
            Authenticity = MailboxAuthenticity() with
            {
                HeaderInspection = MailboxAuthenticity().HeaderInspection with
                {
                    Discrepancies =
                    [
                        MailboxHeaderDiscrepancyKind.MalformedFrom,
                        MailboxHeaderDiscrepancyKind.MalformedFrom,
                    ],
                },
            },
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null);

        result.IsSuccess.ShouldBeFalse();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxMessageIntakeInvalidRejection>();
    }

    [Fact]
    public static void HandleMailboxIntakeShouldRejectInconsistentDelegatedSenderPosture()
    {
        CaptureMailboxMessageIntake missingPrincipal = MailboxCommand() with
        {
            Source = MailboxCommand().Source with
            {
                Sender = new MailboxParticipantIdentity("delegate@example.test", "Delegate"),
                DelegatedSender = new MailboxDelegatedSenderSnapshot(
                    MailboxDelegatedSenderState.Delegated,
                    new MailboxParticipantIdentity("delegate@example.test", "Delegate"),
                    PrincipalFor: null,
                    ["provider:sender", "provider:from"],
                    []),
            },
        };
        CaptureMailboxMessageIntake notDelegatedWithPrincipal = MailboxCommand() with
        {
            Source = MailboxCommand().Source with
            {
                DelegatedSender = new MailboxDelegatedSenderSnapshot(
                    MailboxDelegatedSenderState.NotDelegated,
                    Delegate: null,
                    new MailboxParticipantIdentity("principal@example.test", "Principal"),
                    ["provider:from"],
                    []),
            },
        };

        DomainResult missingPrincipalResult = GovernedOperationAggregate.Handle(missingPrincipal, state: null);
        DomainResult notDelegatedResult = GovernedOperationAggregate.Handle(notDelegatedWithPrincipal, state: null);

        missingPrincipalResult.IsSuccess.ShouldBeFalse();
        missingPrincipalResult.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxMessageIntakeInvalidRejection>();
        notDelegatedResult.IsSuccess.ShouldBeFalse();
        notDelegatedResult.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxMessageIntakeInvalidRejection>();
    }

    [Fact]
    public static void HandleMailboxIntakeShouldRejectContradictoryExternalSenderPosture()
    {
        CaptureMailboxMessageIntake internalMarkedExternal = MailboxCommand() with
        {
            Source = MailboxCommand().Source with
            {
                ExternalSender = new MailboxExternalSenderPosture(
                    ExternalSender: true,
                    MailboxPartyResolutionState.ResolvedInternal,
                    "party:internal-001",
                    ["external-sender:true", "party-resolution:resolved-internal"]),
            },
        };
        CaptureMailboxMessageIntake internalWithoutPartyRef = MailboxCommand() with
        {
            Source = MailboxCommand().Source with
            {
                ExternalSender = new MailboxExternalSenderPosture(
                    ExternalSender: false,
                    MailboxPartyResolutionState.ResolvedInternal,
                    ResolvedPartyRef: null,
                    ["external-sender:false", "party-resolution:resolved-internal"]),
            },
        };

        DomainResult internalMarkedExternalResult = GovernedOperationAggregate.Handle(internalMarkedExternal, state: null);
        DomainResult internalWithoutPartyRefResult = GovernedOperationAggregate.Handle(internalWithoutPartyRef, state: null);

        internalMarkedExternalResult.IsSuccess.ShouldBeFalse();
        internalMarkedExternalResult.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxMessageIntakeInvalidRejection>();
        internalWithoutPartyRefResult.IsSuccess.ShouldBeFalse();
        internalWithoutPartyRefResult.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxMessageIntakeInvalidRejection>();
    }

    [Fact]
    public static void HandleMailboxIntakeOnCapturedAggregateShouldReturnStructuredRejection()
    {
        GovernedOperationState state = new();
        state.Apply(new MailboxMessageIntakeCaptured(
            IntakeId,
            "graph-message-001",
            "<message-001@example.test>",
            "graph-conversation-001",
            null,
            "controlled-mailbox-001",
            new MailboxParticipantIdentity("sender@example.test", null),
            [new MailboxRecipientIdentity("project@example.test", null, "to")],
            DateTimeOffset.UtcNow,
            null,
            null,
            [],
            null,
            "graph-message-v1",
            "m365-mailbox-intake",
            "mailbox-intake.kernel.v1",
            "metadata_only",
            "collaboration_input",
            1));

        DomainResult result = GovernedOperationAggregate.Handle(MailboxCommand(), state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<MailboxMessageIntakeAlreadyCapturedRejection>().IntakeId.ShouldBe(IntakeId);
        result.Events[0].ShouldBeAssignableTo<IRejectionEvent>();
    }

    [Fact]
    public static async Task ProcessAsyncShouldHandleWorkflowRetryThroughAggregateReflection()
    {
        GovernedOperationAggregate aggregate = new();
        RequestFailedWorkflowRetry command = RetryCommand();
        CommandEnvelope envelope = new(
            MessageId: command.RetryId,
            TenantId: "tenant-alpha",
            Domain: "chatbot",
            AggregateId: command.RetryId,
            CommandType: nameof(RequestFailedWorkflowRetry),
            Payload: JsonSerializer.SerializeToUtf8Bytes(command),
            CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            CausationId: null,
            UserId: "actor-alpha",
            Extensions: null);

        DomainResult result = await aggregate.ProcessAsync(envelope, currentState: null);

        result.IsSuccess.ShouldBeTrue();
        WorkflowRetryRequested retry = result.Events.ShouldHaveSingleItem().ShouldBeOfType<WorkflowRetryRequested>();
        retry.RetryId.ShouldBe(command.RetryId);
        retry.FailedEventId.ShouldBe(command.FailedEventId);
        retry.FailedOperationClass.ShouldBe("message-intake");
        retry.FailureReasonCode.ShouldBe("graph_throttled");
    }

    [Fact]
    public static void HandleWorkflowRetryShouldRejectInvalidPayloadWithoutThrowing()
    {
        RequestFailedWorkflowRetry command = RetryCommand() with { FailedEventId = "raw-provider-message-id" };

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null);

        result.IsRejection.ShouldBeTrue();
        WorkflowRetryInvalidRejection rejection = result.Events.ShouldHaveSingleItem().ShouldBeOfType<WorkflowRetryInvalidRejection>();
        rejection.RetryId.ShouldBe(command.RetryId);
        rejection.ReasonCode.ShouldBe("invalid_workflow_retry_payload");
        result.Events[0].ShouldBeAssignableTo<IRejectionEvent>();
    }

    [Fact]
    public static void HandleCaptureTaskIntentShouldCaptureAndTreatReplayAsNoOp()
    {
        CaptureTaskIntent command = TaskIntentCommand();
        CommandEnvelope envelope = TaskIntentEnvelope(command);

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null, envelope);

        result.IsSuccess.ShouldBeTrue();
        TaskIntentCaptured captured = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentCaptured>();
        captured.Record.TenantId.ShouldBe("tenant-alpha");
        captured.Record.TaskIntentId.ShouldBe(TaskIntentIdempotency.ComposeKey(
            "tenant-alpha",
            command.ProjectId,
            command.SourceMessageId,
            command.RequesterPartyId,
            command.KernelVersion,
            command.DetectedActionKind,
            command.SourceEvidenceOffsets));

        GovernedOperationState state = new();
        state.Apply(captured);

        DomainResult replay = GovernedOperationAggregate.Handle(command, state, envelope);

        replay.IsNoOp.ShouldBeTrue();
    }

    [Fact]
    public static void HandleProposeAiActionShouldConvertCapturedTaskIntentAndRejectSecondConversion()
    {
        CaptureTaskIntent capture = TaskIntentCommand();
        CommandEnvelope envelope = TaskIntentEnvelope(capture);
        TaskIntentCaptured captured = GovernedOperationAggregate
            .Handle(capture, state: null, envelope)
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<TaskIntentCaptured>();
        GovernedOperationState state = new();
        state.Apply(captured);

        ProposeAIAction command = new(
            capture.ProjectId,
            captured.Record.TaskIntentId,
            capture.SourceMessageId,
            capture.RequesterPartyId,
            "CreateProjectTask",
            "project-task",
            capture.SourceVersion,
            ["message:offset:001"],
            ["project:project-001"],
            ["party-001"],
            capture.PolicySnapshotId,
            capture.CorrelationId,
            "transition-001",
            SourceConversationItemId: capture.SourceMessageId,
            RiskClassification: Classification("CreateProjectTask", capture.CorrelationId, capture.PolicySnapshotId));

        DomainResult result = GovernedOperationAggregate.Handle(command, state, envelope);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(2);
        TaskIntentConvertedToAiActionProposal converted = result.Events.OfType<TaskIntentConvertedToAiActionProposal>().ShouldHaveSingleItem();
        converted.TaskIntent.State.ShouldBe(TaskIntentState.Converted);
        converted.TaskIntent.ConvertedProposalId.ShouldBe(converted.Proposal.ProposalId);
        converted.Proposal.SafeNextAction.ShouldBe("review-ai-action");
        AiActionApprovalRequested requested = result.Events.OfType<AiActionApprovalRequested>().ShouldHaveSingleItem();
        requested.ApprovalId.ShouldBe($"approval:{converted.Proposal.ProposalId}");
        requested.AiRiskClass.ShouldBe(AiActionRiskClass.ApprovalRequired);
        requested.AiRiskActionClasses.ShouldBe(["creates-tasks"], ignoreOrder: false);
        requested.EvidenceFreshnessStates.ShouldBe([ApprovalEvidenceFreshness.Expired], ignoreOrder: false);

        state.Apply(converted);
        state.Apply(requested);
        DomainResult replay = GovernedOperationAggregate.Handle(command, state, envelope);
        replay.IsNoOp.ShouldBeTrue();

        ProposeAIAction conflicting = command with { TransitionId = "transition-002" };
        DomainResult rejected = GovernedOperationAggregate.Handle(conflicting, state, envelope);
        rejected.IsRejection.ShouldBeTrue();
        rejected.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentTransitionRejected>().ReasonCode.ShouldBe("task_intent_already_converted");
    }

    [Fact]
    public static void HandleApproveAiActionShouldRecordDecisionAndPermissionForLaterExecution()
    {
        AiActionApprovalRequested requested = ApprovalRequest([ApprovalEvidenceFreshness.Fresh]);
        GovernedOperationState state = new();
        state.Apply(requested);
        DecideAiActionApproval command = ApprovalDecision(ApprovalDecisionKind.Approve, requested);

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AiActionApprovalDecisionRecorded recorded = result.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionApprovalDecisionRecorded>();
        recorded.DecisionKind.ShouldBe(ApprovalDecisionKind.Approve);
        recorded.SafeNextAction.ShouldBe("execute-approved-ai-action");
        recorded.AuditOperationId.ShouldBe($"audit:{command.DecisionId}");

        state.Apply(recorded);
        GovernedOperationAggregate.Handle(command, state, Envelope(command)).IsNoOp.ShouldBeTrue();
        GovernedOperationAggregate
            .Handle(command with { Decision = ApprovalDecisionKind.Reject, DecisionId = "approval-decision-002" }, state, Envelope(command))
            .IsRejection
            .ShouldBeTrue();
    }

    [Fact]
    public static void HandleApproveAiActionShouldRejectExpiredEvidenceButAllowRejectDecision()
    {
        AiActionApprovalRequested requested = ApprovalRequest([ApprovalEvidenceFreshness.Expired]);
        GovernedOperationState state = new();
        state.Apply(requested);
        DecideAiActionApproval approveCommand = ApprovalDecision(ApprovalDecisionKind.Approve, requested);
        DecideAiActionApproval rejectCommand = ApprovalDecision(ApprovalDecisionKind.Reject, requested);

        DomainResult approve = GovernedOperationAggregate.Handle(approveCommand, state, Envelope(approveCommand));
        DomainResult reject = GovernedOperationAggregate.Handle(rejectCommand, state, Envelope(rejectCommand));

        approve.IsRejection.ShouldBeTrue();
        approve.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionApprovalDecisionRejected>().ReasonCode.ShouldBe("evidence-expired");
        reject.IsSuccess.ShouldBeTrue();
        reject.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionApprovalDecisionRecorded>().SafeNextAction.ShouldBe("none");
    }

    [Fact]
    public static void HandleProposalInvalidationShouldRecordCorrectionLineageAndRejectConflictingReplay()
    {
        GovernedOperationState state = ProposalApprovalState();
        MarkAiActionProposalInvalidatedByCorrection command = ProposalInvalidationCommand();

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AiActionProposalInvalidatedByCorrection invalidated = result.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionProposalInvalidatedByCorrection>();
        invalidated.ProposalId.ShouldBe(command.ProposalId);
        invalidated.ApprovalId.ShouldBe(command.ApprovalId);
        invalidated.AssociationId.ShouldBe(command.AssociationId);
        invalidated.CorrectionId.ShouldBe(command.CorrectionId);
        invalidated.CorrectedEvidenceState.ShouldBe("corrected");
        invalidated.EvidenceSnapshotSourceVersion.ShouldBe(11);

        state.Apply(invalidated);
        GovernedOperationAggregate.Handle(command, state, Envelope(command)).IsNoOp.ShouldBeTrue();

        DomainResult conflict = GovernedOperationAggregate.Handle(
            command with { CorrectedEvidenceState = "conflicting" },
            state,
            Envelope(command));

        conflict.IsRejection.ShouldBeTrue();
        conflict.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionProposalInvalidationRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.CorrectedContextInvalidated);
    }

    [Fact]
    public static void HandleProposalInvalidationShouldRejectAssociationOrSourceVersionMismatch()
    {
        GovernedOperationState state = ProposalApprovalState();
        MarkAiActionProposalInvalidatedByCorrection command = ProposalInvalidationCommand();

        DomainResult wrongAssociation = GovernedOperationAggregate.Handle(
            command with { AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAA" },
            state,
            Envelope(command));
        DomainResult staleCorrection = GovernedOperationAggregate.Handle(
            command with { EvidenceSnapshotSourceVersion = 10 },
            state,
            Envelope(command));

        wrongAssociation.IsRejection.ShouldBeTrue();
        wrongAssociation.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionProposalInvalidationRejected>().ReasonCode
            .ShouldBe("proposal_unavailable");
        staleCorrection.IsRejection.ShouldBeTrue();
        staleCorrection.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionProposalInvalidationRejected>().ReasonCode
            .ShouldBe("proposal_unavailable");
    }

    [Fact]
    public static void HandleApproveAiActionShouldRejectInvalidatedProposal()
    {
        AiActionApprovalRequested requested = ApprovalRequest([ApprovalEvidenceFreshness.Fresh]);
        GovernedOperationState state = ProposalApprovalState();
        state.Apply(ProposalInvalidated());
        DecideAiActionApproval command = ApprovalDecision(ApprovalDecisionKind.Approve, requested);

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionApprovalDecisionRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.CorrectedContextInvalidated);
    }

    [Fact]
    public static void HandleApprovedAiActionExecutionShouldRejectInvalidatedProposal()
    {
        ExecuteApprovedAIAction command = ApprovedExecutionCommand();
        GovernedOperationState state = ProposalApprovalState(withApprovedDecision: true);
        state.Apply(ProposalInvalidated());

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        ApprovedAiActionExecutionRejected rejected = result.Events.ShouldHaveSingleItem().ShouldBeOfType<ApprovedAiActionExecutionRejected>();
        rejected.ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.CorrectedContextInvalidated);
        rejected.ProjectId.ShouldBe(command.ProjectId);
        rejected.SourceMessageId.ShouldBe(command.SourceMessageId);
    }

    [Fact]
    public static void HandleLowRiskAiExecutionShouldRejectInvalidatedProposal()
    {
        ExecuteLowRiskAIAssistance command = LowRiskExecutionCommand("success");
        GovernedOperationState state = ProposalApprovalState();
        state.Apply(ProposalInvalidated());

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentTransitionRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.CorrectedContextInvalidated);
    }

    [Fact]
    public static void HandleProposeAiActionShouldRejectTenantRequesterAndUnsafeMetadataMismatches()
    {
        CaptureTaskIntent capture = TaskIntentCommand();
        CommandEnvelope envelope = TaskIntentEnvelope(capture);
        TaskIntentCaptured captured = GovernedOperationAggregate
            .Handle(capture, state: null, envelope)
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<TaskIntentCaptured>();
        GovernedOperationState state = new();
        state.Apply(captured);
        ProposeAIAction command = new(
            capture.ProjectId,
            captured.Record.TaskIntentId,
            capture.SourceMessageId,
            capture.RequesterPartyId,
            "CreateProjectTask",
            "project-task",
            capture.SourceVersion,
            ["message:offset:001"],
            ["project:project-001"],
            ["party-001"],
            capture.PolicySnapshotId,
            capture.CorrelationId,
            "transition-001",
            SourceConversationItemId: capture.SourceMessageId,
            RiskClassification: Classification("CreateProjectTask", capture.CorrelationId, capture.PolicySnapshotId));

        DomainResult tenantRejected = GovernedOperationAggregate.Handle(
            command,
            state,
            envelope with { TenantId = "tenant-beta" });
        DomainResult requesterRejected = GovernedOperationAggregate.Handle(
            command with { RequesterId = "party-foreign" },
            state,
            envelope);
        DomainResult metadataRejected = GovernedOperationAggregate.Handle(
            command with { AffectedResourceReferences = ["project:project-001/raw-path"] },
            state,
            envelope);

        tenantRejected.IsRejection.ShouldBeTrue();
        tenantRejected.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentTransitionRejected>().ReasonCode
            .ShouldBe("task_intent_unavailable");
        requesterRejected.IsRejection.ShouldBeTrue();
        requesterRejected.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentTransitionRejected>().ReasonCode
            .ShouldBe("task_intent_transition_metadata_invalid");
        metadataRejected.IsRejection.ShouldBeTrue();
        metadataRejected.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentTransitionRejected>().ReasonCode
            .ShouldBe("task_intent_transition_metadata_invalid");
    }

    [Fact]
    public static void StateReplayShouldNotLetCapturedEventOverwriteTerminalTaskIntentWithSameSourceVersion()
    {
        CaptureTaskIntent capture = TaskIntentCommand();
        CommandEnvelope envelope = TaskIntentEnvelope(capture);
        TaskIntentCaptured captured = GovernedOperationAggregate
            .Handle(capture, state: null, envelope)
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<TaskIntentCaptured>();
        GovernedOperationState state = new();
        state.Apply(captured);
        ProposeAIAction command = new(
            capture.ProjectId,
            captured.Record.TaskIntentId,
            capture.SourceMessageId,
            capture.RequesterPartyId,
            "CreateProjectTask",
            "project-task",
            capture.SourceVersion,
            ["message:offset:001"],
            ["project:project-001"],
            ["party-001"],
            capture.PolicySnapshotId,
            capture.CorrelationId,
            "transition-001",
            SourceConversationItemId: capture.SourceMessageId,
            RiskClassification: Classification("CreateProjectTask", capture.CorrelationId, capture.PolicySnapshotId));
        IReadOnlyList<object> conversionEvents = GovernedOperationAggregate
            .Handle(command, state, envelope)
            .Events;
        conversionEvents.Count.ShouldBe(2);
        TaskIntentConvertedToAiActionProposal converted = conversionEvents[0].ShouldBeOfType<TaskIntentConvertedToAiActionProposal>();
        conversionEvents[1].ShouldBeOfType<AiActionApprovalRequested>();

        state.Apply(converted);
        state.Apply(captured);

        state.TaskIntents[captured.Record.TaskIntentId].State.ShouldBe(TaskIntentState.Converted);
    }

    [Theory]
    [InlineData("not-actionable", TaskIntentState.NotActionable)]
    [InlineData("already-handled", TaskIntentState.AlreadyHandled)]
    [InlineData("out-of-scope", TaskIntentState.OutOfScope)]
    public static void HandleDispositionShouldMarkTerminalState(string disposition, TaskIntentState expectedState)
    {
        CaptureTaskIntent capture = TaskIntentCommand();
        CommandEnvelope envelope = TaskIntentEnvelope(capture);
        TaskIntentCaptured captured = GovernedOperationAggregate
            .Handle(capture, state: null, envelope)
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<TaskIntentCaptured>();
        GovernedOperationState state = new();
        state.Apply(captured);

        MarkTaskIntentDisposition command = new(
            capture.ProjectId,
            captured.Record.TaskIntentId,
            capture.SourceMessageId,
            disposition,
            capture.SourceVersion,
            ["message:offset:001"],
            capture.PolicySnapshotId,
            capture.CorrelationId,
            $"transition-{disposition}");

        DomainResult result = GovernedOperationAggregate.Handle(command, state, envelope);

        result.IsSuccess.ShouldBeTrue();
        TaskIntentDispositionMarked marked = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentDispositionMarked>();
        marked.TaskIntent.State.ShouldBe(expectedState);
        marked.TaskIntent.SafeNextAction.ShouldBe("none");
        marked.TaskIntent.ReviewerActorId.ShouldBe("actor-alpha");
    }

    [Fact]
    public static void DuplicateDispositionShouldRequireSameProjectPredecessor()
    {
        CaptureTaskIntent capture = TaskIntentCommand();
        CommandEnvelope envelope = TaskIntentEnvelope(capture);
        TaskIntentCaptured captured = GovernedOperationAggregate
            .Handle(capture, state: null, envelope)
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<TaskIntentCaptured>();
        GovernedOperationState state = new();
        state.Apply(captured);

        MarkTaskIntentDisposition command = new(
            capture.ProjectId,
            captured.Record.TaskIntentId,
            capture.SourceMessageId,
            "duplicate",
            capture.SourceVersion,
            ["message:offset:001"],
            capture.PolicySnapshotId,
            capture.CorrelationId,
            "transition-duplicate");

        DomainResult result = GovernedOperationAggregate.Handle(command, state, envelope);

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentTransitionRejected>().ReasonCode
            .ShouldBe("task_intent_duplicate_predecessor_unavailable");
    }

    [Fact]
    public static void DuplicateDispositionShouldRejectForeignTenantPredecessorAndAcceptSameScopePredecessor()
    {
        CaptureTaskIntent capture = TaskIntentCommand();
        CommandEnvelope envelope = TaskIntentEnvelope(capture);
        TaskIntentCaptured captured = GovernedOperationAggregate
            .Handle(capture, state: null, envelope)
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<TaskIntentCaptured>();
        GovernedOperationState state = new();
        state.Apply(captured);
        state.Apply(new TaskIntentCaptured(captured.Record with
        {
            TaskIntentId = "task-intent:predecessor-foreign",
            TenantId = "tenant-beta",
        }));
        state.Apply(new TaskIntentCaptured(captured.Record with
        {
            TaskIntentId = "task-intent:predecessor-alpha",
        }));

        MarkTaskIntentDisposition foreign = new(
            capture.ProjectId,
            captured.Record.TaskIntentId,
            capture.SourceMessageId,
            "duplicate",
            capture.SourceVersion,
            ["message:offset:001"],
            capture.PolicySnapshotId,
            capture.CorrelationId,
            "transition-duplicate-foreign",
            "task-intent:predecessor-foreign");
        MarkTaskIntentDisposition sameScope = foreign with
        {
            TransitionId = "transition-duplicate-alpha",
            PredecessorTaskIntentId = "task-intent:predecessor-alpha",
        };

        DomainResult foreignResult = GovernedOperationAggregate.Handle(foreign, state, envelope);
        DomainResult sameScopeResult = GovernedOperationAggregate.Handle(sameScope, state, envelope);

        foreignResult.IsRejection.ShouldBeTrue();
        foreignResult.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentTransitionRejected>().ReasonCode
            .ShouldBe("task_intent_duplicate_predecessor_unavailable");
        sameScopeResult.IsSuccess.ShouldBeTrue();
        sameScopeResult.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentDispositionMarked>().TaskIntent.State
            .ShouldBe(TaskIntentState.Duplicate);
    }

    private static CommandEnvelope Envelope(RecordGovernedNote command)
        => new(
            MessageId: NoteId,
            TenantId: "tenant-alpha",
            Domain: "chatbot",
            AggregateId: command.NoteId,
            CommandType: nameof(RecordGovernedNote),
            // The aggregate base deserializes the payload with default (PascalCase, case-sensitive)
            // JsonSerializer options, so serialize the same way here.
            Payload: JsonSerializer.SerializeToUtf8Bytes(command),
            CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            CausationId: null,
            UserId: "actor-alpha",
            Extensions: null);

    private static CommandEnvelope Envelope(IChatBotCommand command)
        => Envelope(command, "actor-alpha");

    private static CommandEnvelope Envelope(IChatBotCommand command, string userId)
        => new(
            MessageId: "01ARZ3NDEKTSV4RRFFQ69G5FAL",
            TenantId: "tenant-alpha",
            Domain: "chatbot",
            AggregateId: "graph-message-001",
            CommandType: command.GetType().Name,
            Payload: JsonSerializer.SerializeToUtf8Bytes(command),
            CorrelationId: "correlation-001",
            CausationId: null,
            UserId: userId,
            Extensions: null);

    private static SubmitTenantPolicyChange TenantPolicyChange(string knobId, TenantPolicyChangeSet? changeSet = null)
        => new(
            "policy-change-001",
            "policy-snapshot-current",
            "policy-snapshot-proposed",
            4,
            [knobId],
            changeSet ?? new TenantPolicyChangeSet([new(knobId, NumberValue: 0.92)]),
            "security-owner-request",
            "admin-requester",
            TenantPolicySchemaVersions.M0,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "old-fingerprint-001",
            "new-fingerprint-001");

    private static SubmitMailboxSourceDisable MailboxSourceDisableSubmit()
        => new(
            "mailbox-disable-001",
            "mailbox-source:controlled-mailbox-001",
            "mailbox-source-unsafe-activity",
            "policy-snapshot:mailbox:v1",
            MailboxSourceControlState.Active,
            MailboxSourceControlState.Disabled,
            4,
            "admin-requester",
            MailboxSourceControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveMailboxSourceDisable MailboxSourceDisableApproval()
        => new(
            "mailbox-disable-001",
            "mailbox-source:controlled-mailbox-001",
            "mailbox-source-unsafe-activity",
            "policy-snapshot:mailbox:v1",
            MailboxSourceControlState.Active,
            MailboxSourceControlState.Disabled,
            5,
            "admin-requester",
            "admin-approver",
            MailboxSourceControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static SubmitServiceClientDisable ServiceClientDisableSubmit()
        => new(
            "service-client-disable-001",
            "service-client:cli-automation-client",
            "service-client-unsafe-activity",
            "policy-snapshot:tenant-admin:v1",
            ServiceClientControlState.Active,
            ServiceClientControlState.Disabled,
            4,
            "admin-requester",
            ServiceClientControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveServiceClientDisable ServiceClientDisableApproval()
        => new(
            "service-client-disable-001",
            "service-client:cli-automation-client",
            "service-client-unsafe-activity",
            "policy-snapshot:tenant-admin:v1",
            ServiceClientControlState.Active,
            ServiceClientControlState.Disabled,
            5,
            "admin-requester",
            "admin-approver",
            ServiceClientControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static SubmitAiActorDisable AiActorDisableSubmit()
        => new(
            "ai-actor-disable-001",
            "ai-actor:gpt-mediation-actor",
            "ai-actor-unsafe-proposals",
            "policy-snapshot:policy-admin:v1",
            AiActorControlState.Active,
            AiActorControlState.Disabled,
            4,
            "admin-requester",
            AiActorControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveAiActorDisable AiActorDisableApproval()
        => new(
            "ai-actor-disable-001",
            "ai-actor:gpt-mediation-actor",
            "ai-actor-unsafe-proposals",
            "policy-snapshot:policy-admin:v1",
            AiActorControlState.Active,
            AiActorControlState.Disabled,
            5,
            "admin-requester",
            "admin-approver",
            AiActorControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static SubmitCommandCapabilityDisable CommandCapabilityDisableSubmit()
        => new(
            "command-capability-disable-001",
            nameof(AssociateEmailToProject),
            "command-capability-unsafe-execution",
            "policy-snapshot:policy-admin:v1",
            CommandCapabilityControlState.Active,
            CommandCapabilityControlState.Disabled,
            4,
            "admin-requester",
            CommandCapabilityControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveCommandCapabilityDisable CommandCapabilityDisableApproval()
        => new(
            "command-capability-disable-001",
            nameof(AssociateEmailToProject),
            "command-capability-unsafe-execution",
            "policy-snapshot:policy-admin:v1",
            CommandCapabilityControlState.Active,
            CommandCapabilityControlState.Disabled,
            5,
            "admin-requester",
            "admin-approver",
            CommandCapabilityControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static SubmitOutboundChannelDisable OutboundChannelDisableSubmit()
        => new(
            "outbound-channel-disable-001",
            "adapter:mailbox-outbound",
            "outbound-channel-policy-violation",
            "policy-snapshot:policy-admin:v1",
            OutboundChannelControlState.Active,
            OutboundChannelControlState.Disabled,
            4,
            "admin-requester",
            OutboundChannelControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveOutboundChannelDisable OutboundChannelDisableApproval()
        => new(
            "outbound-channel-disable-001",
            "adapter:mailbox-outbound",
            "outbound-channel-policy-violation",
            "policy-snapshot:policy-admin:v1",
            OutboundChannelControlState.Active,
            OutboundChannelControlState.Disabled,
            5,
            "admin-requester",
            "admin-approver",
            OutboundChannelControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static SubmitCommandCapabilityQuarantine CommandCapabilityQuarantineSubmit()
        => new(
            "command-capability-quarantine-001",
            nameof(AssociateEmailToProject),
            "command-capability-unsafe-execution",
            "policy-snapshot:policy-admin:v1",
            CommandCapabilityControlState.Active,
            CommandCapabilityControlState.Quarantined,
            4,
            "admin-requester",
            CommandCapabilityControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveCommandCapabilityQuarantine CommandCapabilityQuarantineApproval()
        => new(
            "command-capability-quarantine-001",
            nameof(AssociateEmailToProject),
            "command-capability-unsafe-execution",
            "policy-snapshot:policy-admin:v1",
            CommandCapabilityControlState.Active,
            CommandCapabilityControlState.Quarantined,
            5,
            "admin-requester",
            "admin-approver",
            CommandCapabilityControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static SubmitAiActorQuarantine AiActorQuarantineSubmit()
        => new(
            "ai-actor-quarantine-001",
            "ai-actor:gpt-mediation-actor",
            "ai-actor-unsafe-proposals",
            "policy-snapshot:policy-admin:v1",
            AiActorControlState.Active,
            AiActorControlState.Quarantined,
            4,
            "admin-requester",
            AiActorControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveAiActorQuarantine AiActorQuarantineApproval()
        => new(
            "ai-actor-quarantine-001",
            "ai-actor:gpt-mediation-actor",
            "ai-actor-unsafe-proposals",
            "policy-snapshot:policy-admin:v1",
            AiActorControlState.Active,
            AiActorControlState.Quarantined,
            5,
            "admin-requester",
            "admin-approver",
            AiActorControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static SubmitServiceClientQuarantine ServiceClientQuarantineSubmit()
        => new(
            "service-client-quarantine-001",
            "service-client:cli-automation-client",
            "service-client-unsafe-activity",
            "policy-snapshot:tenant-admin:v1",
            ServiceClientControlState.Active,
            ServiceClientControlState.Quarantined,
            4,
            "admin-requester",
            ServiceClientControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveServiceClientQuarantine ServiceClientQuarantineApproval()
        => new(
            "service-client-quarantine-001",
            "service-client:cli-automation-client",
            "service-client-unsafe-activity",
            "policy-snapshot:tenant-admin:v1",
            ServiceClientControlState.Active,
            ServiceClientControlState.Quarantined,
            5,
            "admin-requester",
            "admin-approver",
            ServiceClientControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static SubmitMailboxSourceQuarantine MailboxSourceQuarantineSubmit()
        => new(
            "mailbox-quarantine-001",
            "mailbox-source:controlled-mailbox-001",
            "mailbox-source-unsafe-activity",
            "policy-snapshot:mailbox:v1",
            MailboxSourceControlState.Active,
            MailboxSourceControlState.Quarantined,
            4,
            "admin-requester",
            MailboxSourceControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveMailboxSourceQuarantine MailboxSourceQuarantineApproval()
        => new(
            "mailbox-quarantine-001",
            "mailbox-source:controlled-mailbox-001",
            "mailbox-source-unsafe-activity",
            "policy-snapshot:mailbox:v1",
            MailboxSourceControlState.Active,
            MailboxSourceControlState.Quarantined,
            5,
            "admin-requester",
            "admin-approver",
            MailboxSourceControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static SubmitMailboxSourceRateLimit MailboxSourceRateLimitSubmit()
        => new(
            "mailbox-rate-limit-001",
            "mailbox-source:controlled-mailbox-001",
            "mailbox-source-noisy-intake",
            "policy-snapshot:mailbox:v1",
            OldBudget: 0,
            NewBudget: 200,
            MailboxRateLimitWindow.RollingHour,
            4,
            "admin-requester",
            MailboxSourceRateLimitSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static SubmitServiceClientRateLimit ServiceClientRateLimitSubmit()
        => new(
            "service-client-rate-limit-001",
            "service-client:cli-automation-client",
            "service-client-noisy-automation",
            "policy-snapshot:tenant-admin:v1",
            OldBudget: 0,
            NewBudget: 2000,
            ServiceClientRateLimitWindow.RollingHour,
            4,
            "admin-requester",
            ServiceClientRateLimitSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static SubmitAiActorRateLimit AiActorRateLimitSubmit()
        => new(
            "ai-actor-rate-limit-001",
            "ai-actor:gpt-mediation-actor",
            "ai-actor-noisy-proposals",
            "policy-snapshot:policy-admin:v1",
            OldBudget: 0,
            NewBudget: 200,
            AiActorRateLimitWindow.RollingHour,
            4,
            "admin-requester",
            AiActorRateLimitSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static SubmitCommandCapabilityRateLimit CommandCapabilityRateLimitSubmit()
        => new(
            "command-capability-rate-limit-001",
            "AssociateEmailToProject",
            "command-capability-noisy-submissions",
            "policy-snapshot:policy-admin:v1",
            OldBudget: 0,
            NewBudget: 500,
            CommandCapabilityRateLimitWindow.RollingHour,
            4,
            "admin-requester",
            CommandCapabilityRateLimitSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static CreateOutboundDraft OutboundDraftCommand()
        => new(
            "draft-001",
            "project-001",
            "requester-001",
            "actor-001",
            "conv-001",
            "msg-001",
            "item-001",
            ["recipient:party-001"],
            ["conversation:conv-001", "source-message:msg-001", "file:file-001"],
            "policy-snap-001",
            "correlation-001",
            new OutboundDraftContent("Status update", "Governed draft content.", "text/plain"));

    private static RequestOutboundSendApproval OutboundApprovalRequest(
        ApprovalEvidenceFreshness freshness = ApprovalEvidenceFreshness.Fresh)
        => new(
            "approval-001",
            "draft-001",
            "project-001",
            "requester-001",
            "conv-001",
            "msg-001",
            "item-001",
            ["recipient:party-001"],
            ["conversation:conv-001", "source-message:msg-001", "file:file-001"],
            "policy-snap-001",
            "authorized",
            nameof(ExecuteApprovedOutboundDraft),
            "chatbot-spine.v1",
            "metadata_only",
            new OutboundApprovalContentSnapshot(
                new OutboundDraftContent("Status update", "Governed draft content.", "text/plain"),
                null,
                "governed_content",
                null),
            SenderAuthorityClass.AuthenticatedUserSend,
            freshness,
            1,
            "correlation-001");

    private static DecideOutboundApproval OutboundApprovalDecision(ApprovalDecisionKind decision)
        => new(
            "approval-001",
            "draft-001",
            "project-001",
            decision,
            "decision-001",
            2,
            "correlation-001",
            decision is ApprovalDecisionKind.Approve
                ? new OutboundDraftContent("Approved status update", "Approved governed content.", "text/plain")
                : null);

    private static ExecuteApprovedOutboundDraft OutboundSendCommand()
        => new(
            "send-001",
            "approval-001",
            "draft-001",
            "project-001",
            "requester-001",
            "actor-alpha",
            "conv-001",
            "msg-001",
            "item-001",
            ["recipient:party-001"],
            ["conversation:conv-001", "source-message:msg-001", "file:file-001"],
            "policy-snap-001",
            nameof(ExecuteApprovedOutboundDraft),
            "chatbot-spine.v1",
            SenderAuthorityClass.AuthenticatedUserSend,
            ApprovalEvidenceFreshness.Fresh,
            3,
            1,
            "correlation-001",
            AuthorityResult: OutboundAuthorityResult());

    private static GovernedOperationState OutboundApprovalState(
        bool includeRequest,
        bool includeDecision,
        ApprovalEvidenceFreshness freshness = ApprovalEvidenceFreshness.Fresh,
        ApprovalDecisionKind decision = ApprovalDecisionKind.Approve)
    {
        GovernedOperationState state = new();
        CreateOutboundDraft draft = OutboundDraftCommand();
        state.Apply(new OutboundDraftCreated(
            draft.DraftId,
            draft.ProjectId,
            draft.RequesterId,
            draft.SourceActorId,
            draft.SourceConversationId,
            draft.SourceMessageId,
            draft.SourceConversationItemId,
            draft.RecipientRefs,
            draft.ContextRefs,
            draft.PolicySnapshotId,
            draft.CorrelationId,
            SenderAuthorityClass.DraftOnly,
            draft.GovernedContent,
            DateTimeOffset.UtcNow,
            draft.RedactionState,
            draft.RetentionClass));

        if (!includeRequest)
        {
            return state;
        }

        RequestOutboundSendApproval requestCommand = OutboundApprovalRequest(freshness);
        OutboundApprovalRequested request = new(
            requestCommand.ApprovalId,
            requestCommand.DraftId,
            requestCommand.ProjectId,
            requestCommand.RequesterId,
            "human",
            requestCommand.SourceConversationId,
            requestCommand.SourceMessageId,
            requestCommand.SourceConversationItemId,
            requestCommand.RecipientRefs,
            requestCommand.ContextRefs,
            requestCommand.PolicySnapshotId,
            requestCommand.PolicySnapshotVisibility,
            requestCommand.CommandName,
            requestCommand.CommandAllowlistVersion,
            requestCommand.ContentSnapshot,
            requestCommand.SenderAuthorityClass,
            requestCommand.EvidenceFreshness,
            requestCommand.ExpectedPostStateRedactionState,
            requestCommand.ExpectedDraftSourceVersion,
            2,
            DateTimeOffset.UtcNow,
            requestCommand.CorrelationId,
            requestCommand.RedactionState,
            requestCommand.RetentionClass);
        state.Apply(request);

        if (includeDecision)
        {
            DecideOutboundApproval decisionCommand = OutboundApprovalDecision(decision);
            state.Apply(new OutboundApprovalDecisionRecorded(
                decisionCommand.ApprovalId,
                decisionCommand.DraftId,
                decisionCommand.ProjectId,
                decision,
                "approver-001",
                "human",
                DateTimeOffset.UtcNow,
                request.SourceVersion,
                "authorized",
                null,
                "metadata_only",
                "audit:decision-001",
                "available",
                request.PolicySnapshotId,
                decision is ApprovalDecisionKind.Approve ? "send-approved-outbound-draft" : "none",
                decision is ApprovalDecisionKind.Approve
                    ? request.ContentSnapshot with
                    {
                        ApprovedContent = decisionCommand.ApprovedContent,
                        ApprovedContentRedactionState = "governed_content",
                    }
                    : request.ContentSnapshot,
                3,
                decisionCommand.CorrelationId));
        }

        return state;
    }

    private static SenderAuthorityClassificationResult OutboundAuthorityResult()
        => new(
            SenderAuthorityClass.AuthenticatedUserSend,
            "requester:requester-001",
            "mailbox:mailbox-001",
            null,
            null,
            "approval:approval-001",
            "policy-snapshot:policy-snap-001",
            "fresh",
            [
                "sender-authority:authenticated-user-send",
                "requester:requester-001",
                "mailbox:mailbox-001",
                "approval:approval-001",
                "policy-snapshot:policy-snap-001",
            ],
            null);

    private static AiActionApprovalRequested ApprovalRequest(IReadOnlyList<ApprovalEvidenceFreshness> freshness)
        => new(
            "approval:ai-proposal-001",
            "project-001",
            "ai-proposal-001",
            "task-intent-001",
            "graph-message-001",
            "graph-message-001",
            "party-001",
            "human",
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            AiActionCommandMetadataProvider.AppendConversationMessageCommandName,
            AiActionCommandMetadataProvider.M0AllowlistVersion,
            AiActionRiskClass.ApprovalRequired,
            ["modifies-state"],
            "tuple:Project.AppendConversationMessage:project-conversation:project-contributor:approval-required",
            "policy-snap-001",
            "authorized",
            ["evidence-001"],
            freshness,
            ["project:project-001"],
            ["party-001"],
            "project-contributor",
            "metadata_only",
            "metadata_only",
            9,
            "correlation-001");

    private static DecideAiActionApproval ApprovalDecision(ApprovalDecisionKind decision, AiActionApprovalRequested request)
        => new(
            request.ProjectId,
            request.ApprovalId,
            request.ProposalId,
            request.SourceMessageId,
            decision,
            request.SourceVersion,
            request.CorrelationId,
            "approval-decision-001",
            decision is ApprovalDecisionKind.Reject ? "redacted" : "metadata_only");

    private static ExecuteLowRiskAIAssistance LowRiskExecutionCommand(
        string outcome,
        string policyReasonCode = "low-risk-execute-allowed")
        => new(
            "project-001",
            "ai-proposal-001",
            "task-intent-001",
            "graph-message-001",
            "party-001",
            LowRiskAiAssistanceKind.SummarizeVisibleContext,
            "context-package-001",
            "v1",
            "metadata_only",
            "collaboration_input",
            "disabled",
            ["evidence-001"],
            ["evidence-001"],
            ["redacted"],
            8,
            "policy-snap-001",
            "correlation-001",
            "ai-execution-001",
            "transition-001",
            RiskClassification: AiActionRiskClassifier.Classify(new AiActionRiskInputTuple(
                AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName,
                [],
                "read-only",
                "low-risk",
                "project-contributor",
                "policy-snap-001",
                AiActionCommandMetadataProvider.M0AllowlistVersion,
                AiActionRiskClass.LowRisk,
                "declared",
                "authorized",
                "correlation-001")),
            ExecutionRecord: new LowRiskAiAssistanceExecutionRecord(
                "ai-execution-001",
                "ai-proposal-001",
                "summarize-visible-context",
                outcome,
                outcome == "success" ? "deterministic-test" : "disabled",
                outcome == "success" ? "test-model-v1" : "disabled",
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                ["evidence-001"],
                "context-package-001",
                "v1",
                "metadata_only",
                "policy-snap-001",
                policyReasonCode,
                "audit:ai-execution-001",
                "available",
                "correlation-001",
                "metadata_only",
                "metadata_only",
                outcome == "success" ? "none" : "review-ai-action",
                FailureCode: outcome == "success" ? null : policyReasonCode,
                Retryability: outcome == "failed" ? "retryable" : null));

    private static ExecuteApprovedAIAction ApprovedExecutionCommand()
        => new(
            "project-001",
            "ai-proposal-001",
            "approval:ai-proposal-001",
            "task-intent-001",
            "graph-message-001",
            "party-001",
            AiActionCommandMetadataProvider.AppendConversationMessageCommandName,
            AiActionCommandMetadataProvider.M0AllowlistVersion,
            10,
            9,
            "correlation-001",
            "ai-approved-execution-001",
            "approved-execution-transition-001",
            ["evidence-001"],
            ["project:project-001"],
            ["party-001"],
            "graph-message-001",
            "policy-snap-001",
            ExecutionRecord: ApprovedExecutionRecord());

    private static ApprovedAiActionExecutionRecord ApprovedExecutionRecord(string commandName = AiActionCommandMetadataProvider.AppendConversationMessageCommandName)
        => new(
            "ai-approved-execution-001",
            "ai-proposal-001",
            "approval:ai-proposal-001",
            commandName,
            AiActionCommandMetadataProvider.M0AllowlistVersion,
            "success",
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "audit:ai-approved-execution-001",
            "available",
            "correlation-001",
            "metadata_only",
            "none");

    private static GovernedOperationState ProposalApprovalState(bool withApprovedDecision = false)
    {
        GovernedOperationState state = new();
        TaskIntentRecord taskIntent = new(
            "task-intent-001",
            "tenant-alpha",
            "project-001",
            "graph-message-001",
            "party-001",
            "authorized conversation item requests action",
            ProjectConversationDetectedActionKind.RequestAction,
            [new TaskIntentSourceEvidenceOffset("evidence-001", 0, 10, "safe-token")],
            DeterministicTaskIntentKernel.CurrentKernelVersion,
            0.82,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            TaskIntentState.Converted,
            DeterministicTaskIntentKernel.CurrentSchemaVersion,
            TaskIntentReasonCodes.Converted,
            "authorized-project-conversation",
            "metadata_only",
            "collaboration_input",
            8,
            "correlation-001",
            "policy-snap-001",
            "correction-lineage-001",
            ConvertedProposalId: "ai-proposal-001",
            ReviewerActorId: "actor-alpha",
            DecidedAtUtc: new DateTimeOffset(2026, 6, 1, 0, 1, 0, TimeSpan.Zero),
            AuditOperationId: "audit:transition-001",
            TransitionId: "transition-001");
        AiActionProposalRecord proposal = new(
            "ai-proposal-001",
            taskIntent.TaskIntentId,
            taskIntent.SourceMessageId,
            "graph-message-001",
            taskIntent.RequesterPartyId,
            "actor-alpha",
            ["evidence-001"],
            AiActionCommandMetadataProvider.AppendConversationMessageCommandName,
            "append-conversation-message",
            ["project:project-001"],
            ["party-001"],
            "policy-snap-001",
            9,
            "correlation-001",
            "metadata_only",
            "collaboration_input",
            "chatbot.ai-action-proposal.v1",
            "review-ai-action",
            new Dictionary<string, string>
            {
                ["associationId"] = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                ["evidenceSnapshotSourceVersion"] = "11",
                ["contextPackageId"] = "context-package-001",
                ["contextPackageVersion"] = "v1",
            },
            AiActionRiskClass.ApprovalRequired,
            [AiActionRiskActionClass.ModifiesState],
            "chatbot.ai-action-risk-classifier.m0.v1",
            null,
            "approval-required",
            AiActionCommandMetadataProvider.M0AllowlistVersion,
            AiActionRiskClass.ApprovalRequired,
            "project-contributor",
            new DateTimeOffset(2026, 6, 1, 0, 1, 0, TimeSpan.Zero),
            null,
            taskIntent.CorrectionLineageId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            11,
            "context-package-001",
            "v1");
        state.Apply(new TaskIntentConvertedToAiActionProposal(
            taskIntent,
            proposal,
            "actor-alpha",
            new DateTimeOffset(2026, 6, 1, 0, 1, 0, TimeSpan.Zero),
            "audit:transition-001"));

        AiActionApprovalRequested request = ApprovalRequest([ApprovalEvidenceFreshness.Fresh]);
        state.Apply(request);
        if (withApprovedDecision)
        {
            state.Apply(new AiActionApprovalDecisionRecorded(
                request.ApprovalId,
                request.ProjectId,
                request.ProposalId,
                request.SourceMessageId,
                ApprovalDecisionKind.Approve,
                "approver-001",
                "human",
                new DateTimeOffset(2026, 6, 1, 0, 2, 0, TimeSpan.Zero),
                request.SourceVersion,
                "authorized",
                null,
                "metadata_only",
                "audit:approval-decision-001",
                "available",
                request.PolicySnapshotId,
                "execute-approved-ai-action",
                request.SourceVersion + 1,
                request.CorrelationId));
        }

        return state;
    }

    private static MarkAiActionProposalInvalidatedByCorrection ProposalInvalidationCommand()
        => new(
            "project-001",
            "ai-proposal-001",
            "approval:ai-proposal-001",
            "task-intent-001",
            "graph-message-001",
            "graph-message-001",
            "party-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV:correction:11",
            "corrected",
            11,
            "correlation-001");

    private static AiActionProposalInvalidatedByCorrection ProposalInvalidated()
        => new(
            "ai-proposal-001",
            "approval:ai-proposal-001",
            "task-intent-001",
            "graph-message-001",
            "graph-message-001",
            "party-001",
            "project-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV:correction:11",
            "corrected",
            11,
            "correlation-001",
            "metadata_only",
            "collaboration_input");

    private static GovernedOperationState ApprovedExecutionState(
        ApprovalDecisionKind decision = ApprovalDecisionKind.Approve,
        IReadOnlyList<ApprovalEvidenceFreshness>? freshness = null)
    {
        GovernedOperationState state = new();
        AiActionApprovalRequested request = ApprovalRequest(freshness ?? [ApprovalEvidenceFreshness.Fresh]);
        state.Apply(request);
        state.Apply(new AiActionApprovalDecisionRecorded(
            request.ApprovalId,
            request.ProjectId,
            request.ProposalId,
            request.SourceMessageId,
            decision,
            "approver-001",
            "human",
            new DateTimeOffset(2026, 6, 1, 0, 1, 0, TimeSpan.Zero),
            request.SourceVersion,
            "authorized",
            null,
            "metadata_only",
            "audit:approval-decision-001",
            "available",
            request.PolicySnapshotId,
            decision is ApprovalDecisionKind.Approve ? "execute-approved-ai-action" : "none",
            request.SourceVersion + 1,
            request.CorrelationId));
        return state;
    }

    private static CaptureMailboxMessageIntake MailboxCommand()
        => new(
            IntakeId,
            new MailboxMessageSourceIdentity(
                "graph-message-001",
                "<message-001@example.test>",
                "graph-conversation-001",
                "graph-thread-001",
                "controlled-mailbox-001",
                new MailboxParticipantIdentity("sender@example.test", "Sender"),
                new DateTimeOffset(2026, 5, 30, 10, 15, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2026, 5, 30, 10, 10, 0, TimeSpan.FromHours(2)),
                null,
                "W. Europe Standard Time",
                "graph-message-v1",
                1),
            [new MailboxRecipientIdentity("project@example.test", "Project", "to")],
            [new MailboxAttachmentReference("attachment-001", "evidence.pdf", "application/pdf", 1024)]);

    private static MailboxAuthenticityMetadata MailboxAuthenticity()
        => new(
            new MailboxAuthenticationResultSnapshot(
                MailboxAuthenticationVerdictKind.Malformed,
                MailboxAuthenticationVerdictKind.NotSupplied,
                MailboxAuthenticationVerdictKind.NotSupplied,
                MailboxAuthenticationVerdictKind.NotSupplied,
                null,
                [new MailboxSelectedHeaderSnapshot("Authentication-Results", 0, MailboxHeaderValueState.Malformed)]),
            new MailboxHeaderInspectionSnapshot(
                [],
                [new MailboxSelectedHeaderSnapshot("Authentication-Results", 0, MailboxHeaderValueState.Malformed)],
                MailboxHeaderValueState.Malformed,
                MailboxHeaderValueState.NotSupplied,
                MailboxHeaderValueState.NotSupplied,
                MailboxHeaderValueState.NotSupplied,
                [MailboxHeaderDiscrepancyKind.MalformedFrom]));

    private static RequestFailedWorkflowRetry RetryCommand()
        => new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAA",
            "01ARZ3NDEKTSV4RRFFQ69G5FAB",
            "message-intake",
            "graph_throttled",
            ExpectedFailedSourceVersion: 7,
            Rationale: "safe metadata retry");

    private static CaptureTaskIntent TaskIntentCommand()
        => new(
            "project-001",
            "graph-message-001",
            "party-001",
            "authorized conversation item requests action",
            ProjectConversationDetectedActionKind.RequestAction,
            [new TaskIntentSourceEvidenceOffset("message:offset:001", 10, 40, "safe-token")],
            DeterministicTaskIntentKernel.CurrentKernelVersion,
            0.82,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "metadata_only",
            "collaboration_input",
            8,
            "correlation-001",
            "policy-001",
            CorrectedContextReady: true,
            DeterministicTaskIntentKernel.CurrentSchemaVersion);

    private static CommandEnvelope TaskIntentEnvelope(CaptureTaskIntent command)
        => new(
            MessageId: "01ARZ3NDEKTSV4RRFFQ69G5FAC",
            TenantId: "tenant-alpha",
            Domain: "chatbot",
            AggregateId: "01ARZ3NDEKTSV4RRFFQ69G5FAD",
            CommandType: nameof(CaptureTaskIntent),
            Payload: JsonSerializer.SerializeToUtf8Bytes(command),
            CorrelationId: "correlation-001",
            CausationId: null,
            UserId: "actor-alpha",
            Extensions: null);

    private static AiActionRiskClassificationRecord Classification(
        string intendedCommandName,
        string correlationId,
        string? policySnapshotId)
        => AiActionRiskClassifier.Classify(new AiActionRiskInputTuple(
            intendedCommandName,
            [AiActionRiskActionClass.CreatesTasks],
            "project-conversation",
            "approval-required",
            "project-contributor",
            policySnapshotId,
            "ai-action-command-allowlist.m0",
            AiActionRiskClass.ApprovalRequired,
            "declared",
            "authorized",
            correlationId));
}
