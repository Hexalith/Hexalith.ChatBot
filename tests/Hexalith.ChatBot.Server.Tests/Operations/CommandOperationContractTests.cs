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
        JsonSerializer.Serialize<CommandOperationConcurrency>(null!, options).ShouldBe("null");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConcurrencyJsonShouldRejectUnknownDuplicateAndMalformedMembersAsJsonErrors(bool webDefaults)
    {
        JsonSerializerOptions options = new(webDefaults ? JsonSerializerDefaults.Web : JsonSerializerDefaults.General);
        string revision = webDefaults ? "expectedRevision" : "ExpectedRevision";
        string slot = webDefaults ? "decisionSlotId" : "DecisionSlotId";
        string prefix = "{\"" + revision + "\":0,";
        foreach (string suffix in new[]
        {
            "\"unknown\":true}",
            "\"" + revision + "\":1}",
            "\"" + slot + "\":\"first\",\"" + slot + "\":\"second\"}",
            "\"" + slot + "\":null,\"" + slot + "\":\"second\"}",
            "\"" + slot + "\":\"\"}", "\"" + slot + "\":\" \"}",
            "\"" + slot + "\":5}", "\"" + slot + "\":{}}",
            "\"" + slot + "\":\"first\",\"" + slot.ToUpperInvariant() + "\":\"second\"}",
        })
        {
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<CommandOperationConcurrency>(prefix + suffix, options));
        }
        JsonSerializer.Deserialize<CommandOperationConcurrency>(prefix + "\"" + slot + "\":null}", options).ShouldBe(new(0));
        // These pre-existing constructor/reducer slot decisions are deliberately narrower in the live adapter.
        foreach (string accepted in new[] { "decision slot", "decision/slot", new string('s', 241) })
        {
            CommandOperationConcurrency guard = new(0, accepted);
            JsonSerializer.Deserialize<CommandOperationConcurrency>(JsonSerializer.Serialize(guard, options), options).ShouldBe(guard);
        }
        Should.Throw<ArgumentException>(() => new CommandOperationConcurrency(0, " "));
    }

    [Fact]
    public void IdentityShouldRejectOperationClassesOutsidePublishedTokenBounds()
    {
        foreach (string invalid in new[] { "", " ", "Command-execution", "1command", "command_execution", "command.execution", "command/class", "é", new string('a', 81) })
        {
            Should.Throw<ArgumentException>(() => new CommandOperationIdentity("tenant", OperationId, "RecordGovernedNote", "actor",
                ChatBotSurfaceOrigin.Api, ["resource"], CreatedAt, CorrelationId, operationClass: invalid));
        }
        foreach (string valid in new[] { "a", "command-execution-2", new string('a', 80) })
        {
            new CommandOperationIdentity("tenant", OperationId, "RecordGovernedNote", "actor", ChatBotSurfaceOrigin.Api,
                ["resource"], CreatedAt, CorrelationId, operationClass: valid).OperationClass.ShouldBe(valid);
        }
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
    public void ReceiveShouldRejectPredecessorIdentityOutsideEligibleRetry()
    {
        CommandOperationResult result = CommandOperationReducer.Receive(
            Identity(Hexalith.ChatBot.Contracts.Identities.ChatBotIdentity.NewUlid(), predecessor: OperationId), Input(), 0);
        result.Conflict.ShouldBe(CommandOperationConflict.RetryNotEligible);
        result.State.ShouldBeNull();
    }

    [Fact]
    public void ProjectShouldRejectNullStateBeforeExaminingRevision()
    {
        Should.Throw<ArgumentNullException>(() => CommandOperationReducer.Project(null!, null,
            CommandProjectionState.Projected, "projector", CreatedAt, ChatBotMessageCodes.CommandAccepted, CorrelationId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1L)]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(3L)]
    public void ProjectionGuardShouldRejectInvalidOrMismatchedRevisionWithoutChangingFold(long? revision)
    {
        CommandOperationState committed = Advance(Advance(Received(), CommandExecutionState.Admitted), CommandExecutionState.Committed, 1);
        string retained = JsonSerializer.Serialize(committed);
        CommandOperationResult result = CommandOperationReducer.Project(committed, revision, CommandProjectionState.Projected,
            "projector", committed.Transitions[^1].OccurredAt.AddSeconds(1), ChatBotMessageCodes.CommandAccepted, CorrelationId);
        result.Conflict.ShouldBe(revision is null or < 0 ? CommandOperationConflict.InvalidConcurrency : CommandOperationConflict.RevisionConflict);
        result.State.ShouldBeSameAs(committed);
        result.State!.ExecutionState.ShouldBe(CommandExecutionState.Committed);
        result.State.ProjectionState.ShouldBe(CommandProjectionState.ProjectionPending);
        result.State.CommittedRevision.ShouldBe(1);
        JsonSerializer.Serialize(committed).ShouldBe(retained);
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

    [Theory]
    [InlineData("tenant")]
    [InlineData("actor")]
    [InlineData("resources")]
    [InlineData("operation-class")]
    public void ReplayAndRetryShouldRejectChangedIdentityBindings(string binding)
    {
        CommandOperationState original = Advance(Advance(Received(2), CommandExecutionState.Admitted),
            CommandExecutionState.Failed, preEffectFailure: true);
        string retainedFold = JsonSerializer.Serialize(original);
        CommandOperationIdentity ChangeBinding(CommandOperationIdentity identity) => new(
            binding == "tenant" ? "tenant-other" : identity.TenantId,
            identity.OperationId, identity.CommandType,
            binding == "actor" ? "actor-other" : identity.ActorId,
            identity.Origin,
            binding == "resources" ? ["resource-other"] : identity.ResourceIds,
            identity.CreatedAt, identity.CorrelationId,
            binding == "operation-class" ? "other-execution" : identity.OperationClass,
            identity.EquivalenceVersion, identity.PredecessorOperationId);

        CommandOperationResult replay = CommandOperationReducer.Receive(ChangeBinding(original.Identity), Input(), 0,
            current: original);
        replay.Conflict.ShouldBe(CommandOperationConflict.OperationConflict);
        replay.State.ShouldBeSameAs(original);

        CommandOperationIdentity retryIdentity = Identity(ChatBotIdentity.NewUlid(),
            createdAt: original.Transitions[^1].OccurredAt.AddSeconds(1), predecessor: original.Identity.OperationId);
        CommandOperationResult retry = CommandOperationReducer.Retry(original, ChangeBinding(retryIdentity), Input(), 0);
        retry.Conflict.ShouldBe(CommandOperationConflict.RetryNotEligible);
        retry.State.ShouldBeSameAs(original);
        JsonSerializer.Serialize(original).ShouldBe(retainedFold);
    }

    [Theory]
    [InlineData("command-type")]
    [InlineData("input")]
    [InlineData("missing-predecessor")]
    [InlineData("different-predecessor")]
    [InlineData("reused-operation")]
    [InlineData("backdated")]
    public void RetryShouldRejectChangedAttemptSemanticsWithoutChangingPredecessor(string change)
    {
        CommandOperationState original = Advance(Advance(Received(2), CommandExecutionState.Admitted), CommandExecutionState.Failed, preEffectFailure: true);
        string retained = JsonSerializer.Serialize(original);
        DateTimeOffset retryAt = original.Transitions[^1].OccurredAt.AddSeconds(1);
        CommandOperationIdentity valid = Identity(ChatBotIdentity.NewUlid(), createdAt: retryAt, predecessor: OperationId);
        CommandOperationReducer.Retry(original, valid, Input(), 0).Succeeded.ShouldBeTrue();
        CommandOperationIdentity candidate = Identity(
            change == "reused-operation" ? OperationId : valid.OperationId,
            createdAt: change == "backdated" ? original.Transitions[^1].OccurredAt.AddTicks(-1) : retryAt,
            predecessor: change switch
            {
                "missing-predecessor" => null,
                "different-predecessor" => ChatBotIdentity.NewUlid(),
                _ => OperationId,
            },
            commandType: change == "command-type" ? "OtherOperation" : valid.CommandType);
        CommandOperationResult result = CommandOperationReducer.Retry(original, candidate,
            change == "input" ? Input("{\"value\":2}") : Input(), 0);
        result.Conflict.ShouldBe(CommandOperationConflict.RetryNotEligible);
        result.State.ShouldBeSameAs(original);
        original.RetryEligible.ShouldBeTrue();
        JsonSerializer.Serialize(original).ShouldBe(retained);
    }

    [Theory]
    [InlineData(CommandExecutionState.Received)]
    [InlineData(CommandExecutionState.Admitted)]
    [InlineData(CommandExecutionState.Committed)]
    [InlineData(CommandExecutionState.Rejected)]
    [InlineData(CommandExecutionState.Failed)]
    public void LinkedRetryShouldReplayItsStoredFoldWithoutReopeningTheAttempt(CommandExecutionState state)
    {
        CommandOperationState previous = Advance(Advance(Received(2), CommandExecutionState.Admitted),
            CommandExecutionState.Failed, preEffectFailure: true);
        CommandOperationIdentity identity = Identity(ChatBotIdentity.NewUlid(),
            createdAt: previous.Transitions[^1].OccurredAt.AddSeconds(1), predecessor: previous.Identity.OperationId);
        CommandOperationState stored = CommandOperationReducer.Retry(previous, identity, Input(), 0).State!;
        if (state != CommandExecutionState.Received)
        {
            stored = Advance(stored, CommandExecutionState.Admitted);
            if (state != CommandExecutionState.Admitted)
            {
                stored = Advance(stored, state, state == CommandExecutionState.Committed ? 1 : null);
            }
        }
        string retained = JsonSerializer.Serialize(stored);
        CommandOperationIdentity replay = Identity(identity.OperationId, ChatBotSurfaceOrigin.Mcp,
            ChatBotIdentity.NewUlid(), identity.CreatedAt.AddYears(10), identity.PredecessorOperationId);
        CommandOperationResult result = CommandOperationReducer.Receive(replay, Input(), 0, current: stored);
        result.Succeeded.ShouldBeTrue(result.Conflict.ToString());
        result.IsReplay.ShouldBeTrue();
        result.State.ShouldBeSameAs(stored);
        result.State!.PriorOutcome.ShouldBeSameAs(stored.PriorOutcome);
        CommandOperationReducer.Receive(replay, Input(), null, current: stored).Conflict.ShouldBe(CommandOperationConflict.InvalidConcurrency);
        CommandOperationReducer.Receive(replay, Input(), -1, current: stored).Conflict.ShouldBe(CommandOperationConflict.InvalidConcurrency);
        CommandOperationReducer.Receive(replay, Input(), 1, current: stored).Conflict.ShouldBe(CommandOperationConflict.OperationConflict);
        CommandOperationReducer.Receive(replay, Input("{\"value\":2}"), 0, current: stored).Conflict.ShouldBe(CommandOperationConflict.OperationConflict);
        CommandOperationReducer.Receive(replay, Input(), 0).Conflict.ShouldBe(CommandOperationConflict.RetryNotEligible);
        JsonSerializer.Serialize(stored).ShouldBe(retained);
    }

    [Fact]
    public void MalformedDirectTransitionFactsShouldRejectWithoutChangingTheFold()
    {
        CommandOperationState current = Received();
        CommandOperationTransition valid = new(1, CommandExecutionState.Admitted, CommandProjectionState.ProjectionPending,
            "actor-alpha", CreatedAt.AddSeconds(1), ChatBotMessageCodes.CommandAccepted, CorrelationId);
        string retained = JsonSerializer.Serialize(current);
        foreach (CommandOperationTransition invalid in new[]
        {
            valid with { OccurredAt = CreatedAt.AddTicks(-1) },
            valid with { OccurredAt = valid.OccurredAt.ToOffset(TimeSpan.FromHours(1)) },
            valid with { ReasonCode = "unknown-private-reason" },
            valid with { ReasonCode = ChatBotMessageCodes.GenericDenial },
            valid with { CorrelationId = "not-a-ulid" },
            valid with { State = (CommandExecutionState)int.MaxValue },
            valid with { ProjectionState = (CommandProjectionState)int.MaxValue },
        })
        {
            CommandOperationResult result = CommandOperationReducer.Apply(current, invalid);
            result.Conflict.ShouldBe(CommandOperationConflict.InvalidTransition);
            result.State.ShouldBeSameAs(current);
            JsonSerializer.Serialize(current).ShouldBe(retained);
        }
    }

    [Theory]
    [InlineData(CommandExecutionState.Received)]
    [InlineData(CommandExecutionState.Admitted)]
    [InlineData(CommandExecutionState.Rejected)]
    [InlineData(CommandExecutionState.Failed)]
    public void ProjectionShouldRejectEveryUncommittedExecutionStateWithoutChangingTheFold(CommandExecutionState state)
    {
        CommandOperationState current = Received();
        if (state != CommandExecutionState.Received)
        {
            current = Advance(current, CommandExecutionState.Admitted);
            if (state != CommandExecutionState.Admitted)
            {
                current = Advance(current, state);
            }
        }
        string retained = JsonSerializer.Serialize(current);
        foreach (CommandProjectionState projection in new[] { CommandProjectionState.ProjectionLagging, CommandProjectionState.Projected })
        {
            CommandOperationResult result = CommandOperationReducer.Project(current, current.Revision, projection,
                "projector", current.Transitions[^1].OccurredAt.AddSeconds(1), ChatBotMessageCodes.ProjectionRetryable, CorrelationId);
            result.Conflict.ShouldBe(CommandOperationConflict.InvalidTransition);
            result.State.ShouldBeSameAs(current);
            JsonSerializer.Serialize(current).ShouldBe(retained);
        }
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
    public void ForeignTenantDecisionSlotShouldNotBlockNewOperation()
    {
        CommandOperationState occupied = CommandOperationReducer.Receive(Identity(), Input(), 4, "decision:alpha").State!;
        string retained = JsonSerializer.Serialize(occupied);
        CommandOperationIdentity sameTenant = Identity(ChatBotIdentity.NewUlid());
        CommandOperationReducer.Receive(sameTenant, Input(), 4, "decision:alpha", occupiedDecision: occupied)
            .Conflict.ShouldBe(CommandOperationConflict.DecisionConflict);
        CommandOperationIdentity foreignTenant = new("tenant-other", sameTenant.OperationId, sameTenant.CommandType,
            sameTenant.ActorId, sameTenant.Origin, sameTenant.ResourceIds, sameTenant.CreatedAt, sameTenant.CorrelationId);
        CommandOperationResult result = CommandOperationReducer.Receive(foreignTenant, Input(), 4, "decision:alpha", occupiedDecision: occupied);
        result.Succeeded.ShouldBeTrue();
        result.IsReplay.ShouldBeFalse();
        result.State.ShouldNotBeNull().Identity.ShouldBe(foreignTenant);
        result.State.Concurrency.DecisionSlotId.ShouldBe("decision:alpha");
        JsonSerializer.Serialize(occupied).ShouldBe(retained);
    }

    [Theory]
    [InlineData("admitted")]
    [InlineData("committed")]
    [InlineData("projection-lagging")]
    [InlineData("projected")]
    public void NonFailureEventShouldRejectPreEffectFailureWithoutChangingFold(string target)
    {
        CommandOperationState current = Received();
        if (target != "admitted")
        {
            current = Advance(current, CommandExecutionState.Admitted);
            if (target != "committed")
            {
                current = Advance(current, CommandExecutionState.Committed, 1);
            }
        }
        CommandExecutionState execution = target == "admitted" ? CommandExecutionState.Admitted : CommandExecutionState.Committed;
        CommandProjectionState projection = target switch
        {
            "projection-lagging" => CommandProjectionState.ProjectionLagging,
            "projected" => CommandProjectionState.Projected,
            _ => CommandProjectionState.ProjectionPending,
        };
        CommandOperationTransition valid = new(current.Revision + 1, execution, projection, "actor-alpha",
            current.Transitions[^1].OccurredAt.AddSeconds(1), ChatBotMessageCodes.CommandAccepted, CorrelationId,
            execution == CommandExecutionState.Committed ? 1 : null);
        string retained = JsonSerializer.Serialize(current);
        CommandOperationReducer.Apply(current, valid).Succeeded.ShouldBeTrue();
        CommandOperationResult result = CommandOperationReducer.Apply(current, valid with { PreEffectFailure = true });
        result.Conflict.ShouldBe(CommandOperationConflict.InvalidTransition);
        result.State.ShouldBeSameAs(current);
        JsonSerializer.Serialize(current).ShouldBe(retained);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void EarlierEventShouldReplayAfterProjectionWithoutLosingLaterProgress(int revision)
    {
        CommandOperationState committed = Advance(Advance(Received(), CommandExecutionState.Admitted), CommandExecutionState.Committed, 1);
        CommandOperationState lagging = CommandOperationReducer.Project(committed, 2, CommandProjectionState.ProjectionLagging,
            "projector", CreatedAt.AddSeconds(3), ChatBotMessageCodes.ProjectionRetryable, CorrelationId).State!;
        CommandOperationState projected = CommandOperationReducer.Project(lagging, 3, CommandProjectionState.Projected,
            "projector", CreatedAt.AddSeconds(4), ChatBotMessageCodes.CommandAccepted, CorrelationId).State!;
        CommandOperationTransition historical = projected.Transitions[revision];
        string retained = JsonSerializer.Serialize(projected);
        CommandOperationResult replay = CommandOperationReducer.Apply(projected, historical);
        replay.Succeeded.ShouldBeTrue();
        replay.IsReplay.ShouldBeTrue();
        replay.State.ShouldBeSameAs(projected);
        CommandOperationResult conflict = CommandOperationReducer.Apply(projected, historical with { ActorId = "actor-other" });
        conflict.Conflict.ShouldBe(CommandOperationConflict.RevisionConflict);
        conflict.IsReplay.ShouldBeFalse();
        conflict.State.ShouldBeSameAs(projected);
        projected.Revision.ShouldBe(4);
        projected.ProjectionState.ShouldBe(CommandProjectionState.Projected);
        projected.CommittedRevision.ShouldBe(1);
        JsonSerializer.Serialize(projected).ShouldBe(retained);
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
