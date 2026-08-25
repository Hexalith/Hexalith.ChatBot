using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.ChatBot.Server.Governance.Conversations;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Operations;

/// <summary>
/// Coverage for the governed Stop/Cancel command handler. Before these tests the handler had none anywhere in the
/// repository: deleting its whole validation block, or disabling the duplicate-cancellation guard, broke nothing. The
/// projection tests hand-construct a well-formed <see cref="AiResponseGenerationCancellationRequested"/> and never
/// reach the aggregate, and the UI tests stop at the command the client builds. [Story 13.2 AC3, AC4]
/// </summary>
public sealed class CancelAiResponseGenerationAggregateTests
{
    [Fact]
    public void ValidCancellationShouldEmitTheGovernedCancellationEventWithServerOwnedActorAndVersion()
    {
        CancelAiResponseGeneration command = Command();

        DomainResult result = GovernedOperationAggregate.Handle(command, StateWithActiveGeneration(), Envelope(command));

        AiResponseGenerationCancellationRequested emitted = result.Events
            .OfType<AiResponseGenerationCancellationRequested>()
            .ShouldHaveSingleItem();
        emitted.TenantId.ShouldBe("tenant-alpha");
        emitted.ProjectId.ShouldBe("project-001");
        emitted.ConversationId.ShouldBe("conversation-001");
        emitted.ResponseId.ShouldBe("response-001");
        emitted.GenerationId.ShouldBe("generation-001");
        emitted.CancellationId.ShouldBe("cancel-001");

        // The version advance is server-owned, derived from the command's expected version.
        emitted.SourceVersion.ShouldBe(command.ExpectedSourceVersion + 1);
    }

    [Theory]
    [InlineData("", "conversation-001", "response-001", "generation-001", 4L)]
    [InlineData("project-001", "", "response-001", "generation-001", 4L)]
    [InlineData("project-001", "conversation-001", "", "generation-001", 4L)]
    [InlineData("project-001", "conversation-001", "response-001", "", 4L)]
    [InlineData("project-001", "conversation-001", "response-001", "generation-001", 0L)]
    [InlineData("project-001", "conversation-001", "response-001", "generation-001", -1L)]
    [InlineData("project 001", "conversation-001", "response-001", "generation-001", 4L)]
    public void UnsafeOrIncompleteCancellationPayloadShouldFailClosed(
        string projectId,
        string conversationId,
        string responseId,
        string generationId,
        long expectedSourceVersion)
    {
        CancelAiResponseGeneration command = Command() with
        {
            ProjectId = projectId,
            ConversationId = conversationId,
            ResponseId = responseId,
            GenerationId = generationId,
            ExpectedSourceVersion = expectedSourceVersion,
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, StateWithActiveGeneration(), Envelope(command));

        result.Events.OfType<AiResponseGenerationCancellationRequested>().ShouldBeEmpty();
        result.IsRejection.ShouldBeTrue();
    }

    [Fact]
    public void NonMetadataOnlyRedactionShouldFailClosed()
    {
        CancelAiResponseGeneration command = Command() with { RedactionState = "full" };

        DomainResult result = GovernedOperationAggregate.Handle(command, StateWithActiveGeneration(), Envelope(command));

        result.Events.OfType<AiResponseGenerationCancellationRequested>().ShouldBeEmpty();
        result.IsRejection.ShouldBeTrue();
    }

    [Fact]
    public void RepeatedCancellationIdShouldBeBenignAndEmitNoSecondEvent()
    {
        // AC4: a duplicate stop is benign, never a second governed mutation.
        CancelAiResponseGeneration command = Command();
        GovernedOperationState state = StateWithActiveGeneration();
        DomainResult first = GovernedOperationAggregate.Handle(command, state, Envelope(command));
        foreach (AiResponseGenerationCancellationRequested emitted in first.Events.OfType<AiResponseGenerationCancellationRequested>())
        {
            state.Apply(emitted);
        }

        DomainResult second = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        first.Events.OfType<AiResponseGenerationCancellationRequested>().ShouldHaveSingleItem();
        second.Events.OfType<AiResponseGenerationCancellationRequested>().ShouldBeEmpty();
        second.IsNoOp.ShouldBeTrue();
    }

    [Fact]
    public void CancellationTargetingAGenerationThatNeverStartedShouldFailClosed()
    {
        // AC4: "invalid targets fail closed". Previously any well-formed identity was accepted and projected as a
        // governed "stopped" AI outcome for a response that never existed.
        DomainResult result = GovernedOperationAggregate.Handle(Command(), new GovernedOperationState(), Envelope(Command()));

        result.Events.OfType<AiResponseGenerationCancellationRequested>().ShouldBeEmpty();
        result.IsRejection.ShouldBeTrue();
    }

    [Fact]
    public void CancellationOfAnAlreadyTerminalGenerationShouldFailClosed()
    {
        GovernedOperationState state = StateWithActiveGeneration();
        state.Apply(new LowRiskAiAssistanceExecutionSucceeded(
            ExecutionRecord(),
            "project-001",
            "requester-001",
            "message-001",
            null,
            [],
            []));

        DomainResult result = GovernedOperationAggregate.Handle(Command(), state, Envelope(Command()));

        result.Events.OfType<AiResponseGenerationCancellationRequested>().ShouldBeEmpty();
        result.IsRejection.ShouldBeTrue();
    }

    [Fact]
    public void CancellationNamingADifferentProjectThanTheGenerationShouldFailClosed()
    {
        CancelAiResponseGeneration command = Command() with { ProjectId = "project-OTHER" };

        DomainResult result = GovernedOperationAggregate.Handle(command, StateWithActiveGeneration(), Envelope(command));

        result.Events.OfType<AiResponseGenerationCancellationRequested>().ShouldBeEmpty();
        result.IsRejection.ShouldBeTrue();
    }

    [Fact]
    public void CancellationAssertingAVersionBelowTheGenerationBaselineShouldFailClosed()
    {
        CancelAiResponseGeneration command = Command() with { ExpectedSourceVersion = 1 };

        DomainResult result = GovernedOperationAggregate.Handle(command, StateWithActiveGeneration(), Envelope(command));

        result.Events.OfType<AiResponseGenerationCancellationRequested>().ShouldBeEmpty();
        result.IsRejection.ShouldBeTrue();
    }

    private static GovernedOperationState StateWithActiveGeneration()
    {
        GovernedOperationState state = new();
        state.Apply(new LowRiskAiAssistanceExecutionStarted(
            "generation-001",
            "response-001",
            "project-001",
            "task-intent-001",
            "message-001",
            "requester-001",
            "summarize",
            "context-package-001",
            "v1",
            "policy-snapshot-001",
            "low-risk-allowed",
            3,
            "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            new DateTimeOffset(2026, 6, 1, 0, 5, 0, TimeSpan.Zero)));
        return state;
    }

    private static LowRiskAiAssistanceExecutionRecord ExecutionRecord() => new(
        "generation-001",
        "response-001",
        "summarize",
        "success",
        "provider",
        "model-v1",
        new DateTimeOffset(2026, 6, 1, 0, 6, 0, TimeSpan.Zero),
        [],
        "context-package-001",
        "v1",
        "metadata_only",
        "policy-snapshot-001",
        "low-risk-allowed",
        "audit:generation-001",
        "available",
        "01ARZ3NDEKTSV4RRFFQ69G5FAX",
        "metadata_only",
        "authorized",
        "none");

    private static CancelAiResponseGeneration Command() => new(
        "project-001",
        "conversation-001",
        "response-001",
        "generation-001",
        4,
        "01ARZ3NDEKTSV4RRFFQ69G5FAX",
        "cancel-001");

    private static CommandEnvelope Envelope(CancelAiResponseGeneration command)
        => new(
            MessageId: "01ARZ3NDEKTSV4RRFFQ69G5FAL",
            TenantId: "tenant-alpha",
            Domain: "chatbot",
            AggregateId: "project-001",
            CommandType: command.GetType().Name,
            Payload: JsonSerializer.SerializeToUtf8Bytes(command),
            CorrelationId: "correlation-001",
            CausationId: null,
            UserId: "actor-alpha",
            Extensions: null);
}
