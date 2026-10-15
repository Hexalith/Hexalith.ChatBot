using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts;
using Hexalith.ChatBot.Server.Operations;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Operations;

public sealed class CommandOperationContractTests
{
    internal const string OperationId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    internal const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    internal static readonly DateTimeOffset CreatedAt = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    internal static CommandOperationIdentity Identity(string commandId = OperationId, ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
        string correlation = CorrelationId, DateTimeOffset? createdAt = null, string? predecessor = null, string commandType = "RecordGovernedNote")
        => new("tenant-alpha", commandId, commandType, "actor-alpha", origin, ["resource-alpha"], createdAt ?? CreatedAt,
            correlation, predecessorOperationId: predecessor);

    internal static CommandOperationState Received(int maxAttempts = 1)
        => CommandOperationReducer.Receive(Identity(), Input(), 0, maxAttempts: maxAttempts).State!;

    internal static JsonElement Input(string text = "{\"value\":1,\"other\":true}")
        => JsonDocument.Parse(text).RootElement.Clone();

    internal static CommandOperationState Advance(CommandOperationState state, CommandExecutionState target,
        long? committedRevision = null, bool preEffectFailure = false)
    {
        CommandOperationResult result = CommandOperationReducer.Transition(state, state.Revision, target, "actor-alpha",
            state.Transitions[^1].OccurredAt.AddSeconds(1), ChatBotMessageCodes.CommandAccepted, CorrelationId, committedRevision, preEffectFailure);
        result.Succeeded.ShouldBeTrue(result.Conflict.ToString());
        result.State.ShouldNotBeNull().ExecutionState.ShouldBe(target);
        return result.State;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ImmutableContractsShouldRoundTripWithoutDefaultingMissingOrNegativeRevision(bool webDefaults)
    {
        JsonSerializerOptions options = new(webDefaults ? JsonSerializerDefaults.Web : JsonSerializerDefaults.General);
        CommandOperationIdentity original = Identity(predecessor: "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        CommandOperationIdentity restored = JsonSerializer.Deserialize<CommandOperationIdentity>(JsonSerializer.Serialize(original, options), options).ShouldNotBeNull();
        JsonSerializer.Serialize(restored, options).ShouldBe(JsonSerializer.Serialize(original, options));
        restored.ResourceIds.ShouldBe(original.ResourceIds);
        Should.Throw<NotSupportedException>(() => ((IList<string>)restored.ResourceIds)[0] = "changed");
        CommandOperationConcurrency guard = new(7, "decision:alpha");
        JsonSerializer.Deserialize<CommandOperationConcurrency>(JsonSerializer.Serialize(guard, options), options).ShouldBe(guard);
        string revisionName = webDefaults ? "expectedRevision" : "ExpectedRevision";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<CommandOperationConcurrency>("{}", options));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<CommandOperationConcurrency>("{\"" + revisionName + "\":-1}", options));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<CommandOperationConcurrency>("{\"" + revisionName + "\":null}", options));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<CommandOperationConcurrency>("null", options));
    }

    [Fact]
    public void IdentityShouldEnforcePublishedActorAndResourceMetadataBounds()
    {
        foreach (string invalid in new[] { "private value", "private/path", "é", new string('x', 241) })
        {
            Should.Throw<ArgumentException>(() => new CommandOperationIdentity("tenant", OperationId, "RecordGovernedNote", invalid,
                ChatBotSurfaceOrigin.Api, ["resource"], CreatedAt, CorrelationId));
            Should.Throw<ArgumentException>(() => new CommandOperationIdentity("tenant", OperationId, "RecordGovernedNote", "actor",
                ChatBotSurfaceOrigin.Api, [invalid], CreatedAt, CorrelationId));
        }
        _ = new CommandOperationIdentity("tenant", OperationId, "RecordGovernedNote", "actor@example.test",
            ChatBotSurfaceOrigin.Api, ["resource"], CreatedAt, CorrelationId);
        Should.Throw<ArgumentException>(() => new CommandOperationIdentity("tenant", OperationId, "RecordGovernedNote", "actor",
            ChatBotSurfaceOrigin.Api, ["actor@example.test"], CreatedAt, CorrelationId));
        _ = new CommandOperationIdentity("tenant", OperationId, "RecordGovernedNote", new string('a', 240),
            ChatBotSurfaceOrigin.Api, [new string('r', 240)], CreatedAt, CorrelationId);
    }

    [Fact]
    public void TransitionShouldRejectActorOutsidePublishedTokenBoundsWithoutChangingFold()
    {
        CommandOperationState admitted = Advance(Received(), CommandExecutionState.Admitted);
        foreach (string invalid in new[] { "private actor", "private/path", new string('a', 241) })
        {
            CommandOperationResult result = CommandOperationReducer.Transition(admitted, admitted.Revision, CommandExecutionState.Failed,
                invalid, admitted.Transitions[^1].OccurredAt.AddSeconds(1), ChatBotMessageCodes.CommandAccepted, CorrelationId);
            result.Conflict.ShouldBe(CommandOperationConflict.InvalidTransition);
            result.State.ShouldBeSameAs(admitted);
        }
    }

    [Fact]
    public void EligibleRetryShouldNormalizeLowercasePredecessorIdentity()
    {
        CommandOperationState original = Advance(Advance(Received(2), CommandExecutionState.Admitted), CommandExecutionState.Failed, preEffectFailure: true);
        CommandOperationIdentity retry = Identity(ChatBotIdentity.NewUlid(), createdAt: original.Transitions[^1].OccurredAt.AddSeconds(1),
            predecessor: OperationId.ToLowerInvariant());
        retry.PredecessorOperationId.ShouldBe(OperationId);
        CommandOperationResult result = CommandOperationReducer.Retry(original, retry, Input(), 0);
        result.Succeeded.ShouldBeTrue();
        result.State.ShouldNotBeNull().Identity.PredecessorOperationId.ShouldBe(OperationId);
        original.ExecutionState.ShouldBe(CommandExecutionState.Failed);
    }

    [Fact]
    public void IdentityShouldCopyResourcesNormalizeUtcAndRejectNonUlidCommandIds()
    {
        string[] resources = ["resource-alpha"];
        CommandOperationIdentity identity = new("tenant", OperationId, "RecordGovernedNote", "actor", ChatBotSurfaceOrigin.Ui, resources,
            CreatedAt.ToOffset(TimeSpan.FromHours(2)), CorrelationId);
        resources[0] = "changed";
        identity.ResourceIds.ShouldBe(["resource-alpha"]);
        identity.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
        identity.OperationId.ShouldBe(OperationId);
        ChatBotOperationId.TryParse("not-an-ulid", out _).ShouldBeFalse();
        Should.Throw<ArgumentException>(() => new CommandOperationIdentity("tenant", "invalid", "RecordGovernedNote", "actor", ChatBotSurfaceOrigin.Api,
            [], CreatedAt, CorrelationId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1L)]
    public void MissingOrNegativeRevisionShouldFailBeforeReplay(long? revision)
    {
        CommandOperationState current = Received();
        CommandOperationResult result = CommandOperationReducer.Receive(Identity(), Input(), revision, current: current);
        result.Conflict.ShouldBe(CommandOperationConflict.InvalidConcurrency);
        result.State.ShouldBeSameAs(current);
        Should.Throw<ArgumentOutOfRangeException>(() => new CommandOperationConcurrency(revision));
    }

    [Fact]
    public void LifetimeReplayShouldRetainOriginalOriginResourcesCreationCorrelationAndOutcome()
    {
        CommandOperationState original = Advance(Advance(Received(), CommandExecutionState.Admitted), CommandExecutionState.Committed, 1);
        CommandOperationIdentity replay = Identity(origin: ChatBotSurfaceOrigin.Mcp, correlation: ChatBotIdentity.NewUlid(), createdAt: CreatedAt.AddYears(10));
        CommandOperationResult result = CommandOperationReducer.Receive(replay, Input("{\"other\":true,\"value\":1}"), 0, current: original);
        result.Succeeded.ShouldBeTrue();
        result.IsReplay.ShouldBeTrue();
        result.State.ShouldBeSameAs(original);
        result.State!.Identity.Origin.ShouldBe(ChatBotSurfaceOrigin.Api);
        result.State.Identity.CorrelationId.ShouldBe(CorrelationId);
        result.State.Identity.CreatedAt.ShouldBe(CreatedAt);
        result.State.CommittedRevision.ShouldBe(1);
        result.State.Revision.ShouldBe(2);
    }

    [Fact]
    public void ChangedInputRevisionOrDecisionSlotShouldProduceTypedConflictsWithoutMutation()
    {
        CommandOperationState original = CommandOperationReducer.Receive(Identity(), Input(), 4, "decision:alpha").State!;
        CommandOperationReducer.Receive(Identity(), Input("{\"value\":2}"), 4, "decision:alpha", original).Conflict.ShouldBe(CommandOperationConflict.OperationConflict);
        CommandOperationReducer.Receive(Identity(commandType: "OtherOperation"), Input(), 4, "decision:alpha", original).Conflict.ShouldBe(CommandOperationConflict.OperationConflict);
        CommandOperationReducer.Receive(Identity(), Input(), 5, "decision:alpha", original).Conflict.ShouldBe(CommandOperationConflict.OperationConflict);
        CommandOperationReducer.Receive(Identity(), Input(), 4, "decision:beta", original).Conflict.ShouldBe(CommandOperationConflict.OperationConflict);
        CommandOperationResult occupied = CommandOperationReducer.Receive(Identity(ChatBotIdentity.NewUlid()), Input(), 4, "decision:alpha", occupiedDecision: original);
        occupied.Conflict.ShouldBe(CommandOperationConflict.DecisionConflict);
        original.Revision.ShouldBe(0);
    }

    [Fact]
    public void DeclaredEventsShouldFoldWithActorUtcReasonCorrelationAndSeparateProjection()
    {
        CommandOperationState received = Received();
        CommandOperationState admitted = Advance(received, CommandExecutionState.Admitted);
        CommandOperationState committed = Advance(admitted, CommandExecutionState.Committed, 1);
        committed.ExecutionState.ShouldBe(CommandExecutionState.Committed);
        committed.ProjectionState.ShouldBe(CommandProjectionState.ProjectionPending);
        CommandOperationState folded = received;
        foreach (CommandOperationTransition fact in committed.Transitions.Skip(1))
        {
            folded = CommandOperationReducer.Apply(folded, fact).State!;
            fact.ActorId.ShouldBe("actor-alpha");
            fact.OccurredAt.Offset.ShouldBe(TimeSpan.Zero);
            fact.ReasonCode.ShouldBe(ChatBotMessageCodes.CommandAccepted);
            fact.CorrelationId.ShouldBe(CorrelationId);
        }
        JsonSerializer.Serialize(folded).ShouldBe(JsonSerializer.Serialize(committed));
        CommandOperationReducer.Apply(folded, committed.Transitions[^1]).IsReplay.ShouldBeTrue();
        CommandOperationState lagging = CommandOperationReducer.Project(committed, 2, CommandProjectionState.ProjectionLagging,
            "projector", CreatedAt.AddSeconds(3), ChatBotMessageCodes.CommandAccepted, CorrelationId).State!;
        CommandOperationState projected = CommandOperationReducer.Project(lagging, 3, CommandProjectionState.Projected,
            "projector", CreatedAt.AddSeconds(4), ChatBotMessageCodes.CommandAccepted, CorrelationId).State!;
        projected.ExecutionState.ShouldBe(CommandExecutionState.Committed);
        projected.CommittedRevision.ShouldBe(1);
        projected.Revision.ShouldBe(4);
        CommandOperationReducer.Project(projected, 4, CommandProjectionState.ProjectionPending, "projector", CreatedAt.AddSeconds(5),
            ChatBotMessageCodes.CommandAccepted, CorrelationId).Conflict.ShouldBe(CommandOperationConflict.InvalidTransition);
    }

    [Theory]
    [InlineData(CommandExecutionState.Received)]
    [InlineData(CommandExecutionState.Rejected)]
    [InlineData(CommandExecutionState.Failed)]
    [InlineData(CommandExecutionState.Committed)]
    public void IllegalTransitionShouldPreserveFold(CommandExecutionState target)
    {
        CommandOperationState original = Received();
        CommandOperationResult invalid = CommandOperationReducer.Transition(original, 0, target, "actor", CreatedAt,
            ChatBotMessageCodes.CommandAccepted, CorrelationId, target == CommandExecutionState.Committed ? 1 : null);
        invalid.Conflict.ShouldBe(CommandOperationConflict.InvalidTransition);
        invalid.State.ShouldBeSameAs(original);
    }

    [Fact]
    public void CommitShouldRequireExpectedOwnerRevisionAndCanonicalRevision()
    {
        CommandOperationState admitted = Advance(Received(), CommandExecutionState.Admitted);
        CommandOperationReducer.Transition(admitted, 0, CommandExecutionState.Committed, "actor", CreatedAt.AddSeconds(2),
            ChatBotMessageCodes.CommandAccepted, CorrelationId, 1).Conflict.ShouldBe(CommandOperationConflict.RevisionConflict);
        CommandOperationReducer.Transition(admitted, 1, CommandExecutionState.Committed, "actor", CreatedAt.AddSeconds(2),
            ChatBotMessageCodes.CommandAccepted, CorrelationId, 9).Conflict.ShouldBe(CommandOperationConflict.InvalidTransition);
        admitted.Revision.ShouldBe(1);
    }

    [Theory]
    [InlineData(CommandExecutionState.Committed, false)]
    [InlineData(CommandExecutionState.Rejected, false)]
    [InlineData(CommandExecutionState.Failed, false)]
    [InlineData(CommandExecutionState.Failed, true)]
    public void TerminalAttemptShouldRemainClosedAndRetryShouldBeDistinctAndEligibleOnlyBeforeEffects(CommandExecutionState terminal, bool beforeEffects)
    {
        CommandOperationState admitted = Advance(Received(2), CommandExecutionState.Admitted);
        CommandOperationState original = Advance(admitted, terminal, terminal == CommandExecutionState.Committed ? 1 : null, beforeEffects);
        CommandOperationReducer.Transition(original, 2, CommandExecutionState.Admitted, "actor", CreatedAt.AddSeconds(3),
            ChatBotMessageCodes.CommandAccepted, CorrelationId).Conflict.ShouldBe(CommandOperationConflict.InvalidTransition);
        CommandOperationIdentity retry = Identity(ChatBotIdentity.NewUlid(), createdAt: CreatedAt.AddSeconds(3), predecessor: OperationId);
        CommandOperationResult result = CommandOperationReducer.Retry(original, retry, Input(), 0);
        if (beforeEffects)
        {
            result.Succeeded.ShouldBeTrue();
            result.State!.Identity.OperationId.ShouldNotBe(original.Identity.OperationId);
            result.State.Identity.PredecessorOperationId.ShouldBe(OperationId);
            result.State.Attempt.ShouldBe(2);
            result.State.ExecutionState.ShouldBe(CommandExecutionState.Received);
            result.State.PriorOutcome.ShouldNotBeNull().OperationId.ShouldBe(original.Identity.OperationId);
            result.State.PriorOutcome.State.ShouldBe(CommandExecutionState.Failed);
            Hexalith.ChatBot.Server.Gateway.Status.OperationStatusHttpResults.ToJsonElement(
                Hexalith.ChatBot.Server.Gateway.Status.OperationStatusRecord.FromCanonical(result.State))
                .GetProperty("priorOutcome").GetProperty("operationId").GetString().ShouldBe(original.Identity.OperationId);
            CommandOperationState exhausted = Advance(Advance(result.State, CommandExecutionState.Admitted), CommandExecutionState.Failed, preEffectFailure: true);
            exhausted.ExecutionState.ShouldBe(CommandExecutionState.Failed);
            exhausted.IsTerminal.ShouldBeTrue();
            exhausted.Revision.ShouldBe(2);
            exhausted.Attempt.ShouldBe(2);
            exhausted.Transitions[^1].OccurredAt.ShouldBeGreaterThan(retry.CreatedAt);
            exhausted.RetryEligible.ShouldBeFalse();
        }
        else
        {
            result.Conflict.ShouldBe(CommandOperationConflict.RetryNotEligible);
        }
        original.Revision.ShouldBe(2);
        original.ExecutionState.ShouldBe(terminal);
    }

    [Fact]
    public async Task SyntheticCompetingTransitionsShouldHaveOneWinnerAndOneUnchangedRevisionConflict()
    {
        CommandOperationState current = Advance(Received(), CommandExecutionState.Admitted);
        using Barrier barrier = new(2);
        Task<CommandOperationResult> Compete(CommandExecutionState target) => Task.Run(() =>
        {
            CommandOperationState observed = current;
            barrier.SignalAndWait(TestContext.Current.CancellationToken);
            CommandOperationResult candidate = CommandOperationReducer.Transition(observed, 1, target, "actor", CreatedAt.AddSeconds(2),
                ChatBotMessageCodes.CommandAccepted, CorrelationId, target == CommandExecutionState.Committed ? 1 : null);
            return ReferenceEquals(Interlocked.CompareExchange(ref current, candidate.State!, observed), observed)
                ? candidate : CommandOperationReducer.Transition(current, 1, target, "actor", CreatedAt.AddSeconds(2),
                    ChatBotMessageCodes.CommandAccepted, CorrelationId);
        }, TestContext.Current.CancellationToken);
        CommandOperationResult[] results = await Task.WhenAll(Compete(CommandExecutionState.Committed), Compete(CommandExecutionState.Rejected));
        results.Count(result => result.Succeeded).ShouldBe(1);
        results.Count(result => result.Conflict == CommandOperationConflict.RevisionConflict).ShouldBe(1);
        current.Revision.ShouldBe(2);
        current.Transitions.Count.ShouldBe(3);
        current.ShouldBeSameAs(results.Single(result => result.Succeeded).State);
    }
}
