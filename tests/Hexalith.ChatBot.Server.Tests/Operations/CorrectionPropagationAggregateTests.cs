using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Operations;

public static class CorrectionPropagationAggregateTests
{
    private const string AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string IntakeId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string TenantId = "tenant-alpha";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const long CorrectionSourceVersion = 3;
    private static readonly DateTimeOffset StartedAt = new(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public static void StartPropagationShouldMoveCorrectedAssociationToCorrectingWithMetadataOnlyEvent()
    {
        GovernedOperationState state = CorrectedState();

        DomainResult result = GovernedOperationAggregate.Handle(StartCommand(), state, Envelope(nameof(StartMailboxAssociationCorrectionPropagation)));

        result.IsSuccess.ShouldBeTrue();
        MailboxAssociationCorrectionPropagationStarted started = result.Events.ShouldHaveSingleItem()
            .ShouldBeOfType<MailboxAssociationCorrectionPropagationStarted>();
        started.AssociationId.ShouldBe(AssociationId);
        started.IntakeId.ShouldBe(IntakeId);
        started.TenantId.ShouldBe(TenantId);
        started.WorkflowInstanceId.ShouldContain("correction-propagation", Case.Sensitive);
        started.RequiredStoreKeys.ShouldBe(CorrectionPropagationStoreKeys.RequiredM0, ignoreOrder: false);
        started.RedactionState.ShouldBe("metadata_only");
        started.RetentionClass.ShouldBe("collaboration_input");

        state.Apply(started);
        state.AssociationLifecycleState.ShouldBe(LifecycleState.Correcting);
        state.CorrectionPropagationRequiredStoreCount.ShouldBe(4);
    }

    [Fact]
    public static void StoreAcknowledgementsShouldBeIdempotentAndCompletionRequiresAllRequiredStores()
    {
        GovernedOperationState state = CorrectedState();
        state.Apply(StartEvent());

        foreach (string storeKey in CorrectionPropagationStoreKeys.RequiredM0)
        {
            DomainResult ack = GovernedOperationAggregate.Handle(AckCommand(storeKey), state, Envelope(nameof(AcknowledgeMailboxAssociationCorrectionStoreInvalidated)));
            ack.IsSuccess.ShouldBeTrue();
            MailboxAssociationCorrectionStoreInvalidated invalidated = ack.Events.ShouldHaveSingleItem()
                .ShouldBeOfType<MailboxAssociationCorrectionStoreInvalidated>();
            invalidated.StoreKey.ShouldBe(storeKey);
            invalidated.Outcome.ShouldBe("success");
            JsonSerializer.Serialize(invalidated).ShouldNotContain("raw-body", Case.Insensitive);
            state.Apply(invalidated);
        }

        DomainResult duplicate = GovernedOperationAggregate.Handle(
            AckCommand(CorrectionPropagationStoreKeys.AssociationRouting),
            state,
            Envelope(nameof(AcknowledgeMailboxAssociationCorrectionStoreInvalidated)));
        duplicate.IsNoOp.ShouldBeTrue();

        DomainResult complete = GovernedOperationAggregate.Handle(
            new CompleteMailboxAssociationCorrectionPropagation(
                AssociationId,
                CorrectionId(),
                WorkflowId(),
                CorrectionSourceVersion,
                StartedAt.AddMinutes(1),
                CorrectionPropagationStatuses.Complete,
                DaprCorrectionPropagationCoordinator.SchemaVersion),
            state,
            Envelope(nameof(CompleteMailboxAssociationCorrectionPropagation)));

        complete.IsSuccess.ShouldBeTrue();
        MailboxAssociationCorrectionPropagationCompleted completed = complete.Events.ShouldHaveSingleItem()
            .ShouldBeOfType<MailboxAssociationCorrectionPropagationCompleted>();
        completed.CompletedStoreKeys.ShouldBe(CorrectionPropagationStoreKeys.RequiredM0.Order(StringComparer.Ordinal).ToArray(), ignoreOrder: false);

        state.Apply(completed);
        state.AssociationLifecycleState.ShouldBe(LifecycleState.Corrected);
        state.IsCorrectionPropagationDelayed.ShouldBeFalse();
    }

    [Fact]
    public static void DelayedPropagationShouldMoveToCorrectionDelayedAndStillAllowCompletion()
    {
        GovernedOperationState state = CorrectedState();
        state.Apply(StartEvent());

        DomainResult delayed = GovernedOperationAggregate.Handle(
            new DelayMailboxAssociationCorrectionPropagation(
                AssociationId,
                CorrectionId(),
                WorkflowId(),
                CorrectionSourceVersion,
                StartedAt.AddMinutes(11),
                DaprCorrectionPropagationCoordinator.ResponsibleOwnerRole,
                DaprCorrectionPropagationCoordinator.DelayedNextSafeAction,
                "m0_slo_breach",
                DaprCorrectionPropagationCoordinator.SchemaVersion),
            state,
            Envelope(nameof(DelayMailboxAssociationCorrectionPropagation)));

        delayed.IsSuccess.ShouldBeTrue();
        state.Apply(delayed.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxAssociationCorrectionPropagationDelayed>());
        state.AssociationLifecycleState.ShouldBe(LifecycleState.CorrectionDelayed);
        state.CorrectionPropagationNextSafeAction.ShouldBe(DaprCorrectionPropagationCoordinator.DelayedNextSafeAction);
    }

    private static StartMailboxAssociationCorrectionPropagation StartCommand()
        => new(
            AssociationId,
            IntakeId,
            CorrectionId(),
            WorkflowId(),
            "project-001",
            "project-002",
            CorrectionPropagationStoreKeys.RequiredM0,
            CorrectionSourceVersion,
            StartedAt,
            StartedAt.AddMinutes(10),
            DaprCorrectionPropagationCoordinator.ResponsibleOwnerRole,
            DaprCorrectionPropagationCoordinator.PendingNextSafeAction,
            DaprCorrectionPropagationCoordinator.SchemaVersion);

    private static AcknowledgeMailboxAssociationCorrectionStoreInvalidated AckCommand(string storeKey)
        => new(
            AssociationId,
            CorrectionId(),
            WorkflowId(),
            storeKey,
            CorrectionSourceVersion,
            "project-001",
            "project-002",
            StartedAt,
            StartedAt.AddSeconds(5),
            "success",
            null,
            "metadata_only",
            "collaboration_input",
            DaprCorrectionPropagationCoordinator.SchemaVersion);

    private static MailboxAssociationCorrectionPropagationStarted StartEvent()
    {
        DomainResult result = GovernedOperationAggregate.Handle(
            StartCommand(),
            CorrectedState(),
            Envelope(nameof(StartMailboxAssociationCorrectionPropagation)));
        return result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxAssociationCorrectionPropagationStarted>();
    }

    private static GovernedOperationState CorrectedState()
    {
        GovernedOperationState state = new();
        state.Apply(new MailboxEmailAssociatedToProject(
            AssociationId,
            IntakeId,
            TenantId,
            "project-001",
            "Project One",
            "mailbox-001",
            "conversation-001",
            "thread-001",
            [new AssociationEvidenceReference("mailbox:project", "fingerprint-1", "ProjectSignal")],
            [],
            0.91,
            AssociationThresholdBand.Auto,
            [AssociationReasonCode.ExplicitProjectIdentifierMatched],
            "association-thresholds.m0.default.v1",
            "association-deterministic.kernel.m0.v1",
            StartedAt.AddMinutes(-5),
            "metadata_only",
            "collaboration_input",
            1,
            "chatbot.association-event.v1",
            CorrelationId,
            "actor-alpha",
            "human",
            "associate",
            "ui",
            StartedAt.AddMinutes(-4)));
        state.Apply(new MailboxEmailAssociationCorrected(
            AssociationId,
            IntakeId,
            TenantId,
            "actor-alpha",
            "human",
            "mailbox-001",
            "conversation-001",
            "thread-001",
            AssociationCorrectionKind.ProjectReassignment,
            "project-001",
            "project-002",
            "Project Two",
            AssociationId,
            AssociationId,
            ["project-001", "project-002"],
            [new AssociationEvidenceReference("association:correction", "fingerprint-1", "association-correction")],
            [],
            0.91,
            AssociationThresholdBand.Auto,
            [AssociationReasonCode.ExplicitProjectIdentifierMatched],
            "association-thresholds.m0.default.v1",
            "association-deterministic.kernel.m0.v1",
            StartedAt.AddMinutes(-5),
            StartedAt,
            "m365-mailbox-intake",
            "metadata_only",
            "collaboration_input",
            CorrectionSourceVersion,
            "chatbot.association-correction-command.v1",
            CorrelationId,
            "ui",
            "Safe metadata correction.",
            "metadata_only",
            "association-thresholds.m0.default.v1",
            CorrectionPropagationStatuses.Pending));
        return state;
    }

    private static string CorrectionId()
        => DaprCorrectionPropagationCoordinator.CorrectionIdFor(AssociationId, CorrectionSourceVersion);

    private static string WorkflowId()
        => DaprCorrectionPropagationCoordinator.WorkflowInstanceIdFor(TenantId, AssociationId, CorrectionId(), CorrectionSourceVersion);

    private static CommandEnvelope Envelope(string commandType)
        => new(
            MessageId: $"{AssociationId}:{commandType}",
            TenantId,
            Domain: "chatbot",
            AggregateId: AssociationId,
            CommandType: commandType,
            Payload: [],
            CorrelationId,
            CausationId: null,
            UserId: "actor-alpha",
            Extensions: null);
}
