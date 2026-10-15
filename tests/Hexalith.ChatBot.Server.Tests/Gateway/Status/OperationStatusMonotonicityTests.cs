using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Tests.Operations;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Status;

public sealed class OperationStatusMonotonicityTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static IOperationStatusStore Store(bool sdk)
        => sdk ? new ReadModelOperationStatusStore(new AtomicOperationStatusReadModelStore()) : new InMemoryOperationStatusStore();

    private static CommandOperationState Committed()
        => CommandOperationContractTests.Advance(CommandOperationContractTests.Advance(CommandOperationContractTests.Received(), CommandExecutionState.Admitted), CommandExecutionState.Committed, 1);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanonicalSnapshotsShouldRejectUnrelatedCommitRevisionOverflowAndContradictoryLifecycle(bool sdk)
    {
        IOperationStatusStore store = Store(sdk);
        OperationStatusRecord original = OperationStatusRecord.FromCanonical(Committed());
        foreach (OperationStatusRecord invalid in new[]
        {
            original with { CommittedRevision = 9, LastTransition = original.LastTransition! with { CommittedRevision = 9 } },
            original with { ExpectedRevision = long.MaxValue, CommittedRevision = 0, LastTransition = original.LastTransition! with { CommittedRevision = 0 } },
            original with { LifecycleState = Hexalith.ChatBot.Client.Generated.LifecycleState.Received },
            OperationStatusRecord.FromCanonical(CommandOperationContractTests.Received()) with { LifecycleState = Hexalith.ChatBot.Client.Generated.LifecycleState.Proposed },
        })
        {
            await Should.ThrowAsync<OperationStatusUpdateConflictException>(() => store.UpsertAsync(invalid, Token).AsTask());
            (await store.TryGetAsync(original.TenantId, original.OperationId, Token)).ShouldBeNull();
        }
        await store.UpsertAsync(original, Token);
        (await store.TryGetAsync(original.TenantId, original.OperationId, Token))!.CommittedRevision.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PredecessorOutcomeShouldRemainImmutableFromReceivedThroughTerminalAttempt(bool sdk)
    {
        IOperationStatusStore store = Store(sdk);
        CommandOperationState original = CommandOperationContractTests.Advance(
            CommandOperationContractTests.Advance(CommandOperationContractTests.Received(2), CommandExecutionState.Admitted),
            CommandExecutionState.Failed, preEffectFailure: true);
        var identity = CommandOperationContractTests.Identity(Hexalith.ChatBot.Contracts.Identities.ChatBotIdentity.NewUlid(),
            createdAt: original.Transitions[^1].OccurredAt.AddSeconds(1), predecessor: original.Identity.OperationId);
        CommandOperationState received = CommandOperationReducer.Retry(original, identity, CommandOperationContractTests.Input(), 0).State!;
        OperationStatusRecord initial = OperationStatusRecord.FromCanonical(received);
        initial.PriorOutcome.ShouldNotBeNull();
        await store.UpsertAsync(initial, Token);
        CommandOperationState admitted = CommandOperationContractTests.Advance(received, CommandExecutionState.Admitted);
        OperationStatusRecord next = OperationStatusRecord.FromCanonical(admitted);
        OperationStatusRecord changed = OperationStatusUpdatePolicy.Snapshot(next);
        changed.PriorOutcome!.CorrelationId = Hexalith.ChatBot.Contracts.Identities.ChatBotIdentity.NewUlid();
        foreach (OperationStatusRecord invalid in new[] { next with { PriorOutcome = null }, changed })
        {
            await Should.ThrowAsync<OperationStatusUpdateConflictException>(() => store.UpsertAsync(invalid, Token).AsTask());
            OperationStatusRecord persisted = (await store.TryGetAsync(initial.TenantId, initial.OperationId, Token))!;
            persisted.CanonicalRevision.ShouldBe(0);
            JsonSerializer.Serialize(persisted.PriorOutcome).ShouldBe(JsonSerializer.Serialize(initial.PriorOutcome));
        }
        await store.UpsertAsync(next, Token);
        await store.UpsertAsync(OperationStatusRecord.FromCanonical(CommandOperationContractTests.Advance(admitted, CommandExecutionState.Failed,
            preEffectFailure: true)), Token);
        OperationStatusRecord terminal = (await store.TryGetAsync(initial.TenantId, initial.OperationId, Token))!;
        terminal.ExecutionState.ShouldBe(CommandExecutionState.Failed);
        JsonSerializer.Serialize(terminal.PriorOutcome).ShouldBe(JsonSerializer.Serialize(initial.PriorOutcome));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanonicalStatusShouldRejectUnboundedOrMalformedMetadataBeforePersistingOrExposingIt(bool sdk)
    {
        IOperationStatusStore store = Store(sdk);
        OperationStatusRecord original = OperationStatusRecord.FromCanonical(Committed());
        foreach (string invalid in new[] { "private value", "private/path", "é", new string('x', 241) })
        {
            foreach (OperationStatusRecord malformed in new[]
            {
                original with { ResourceIds = [invalid] },
                original with { LastTransition = original.LastTransition! with { ActorId = invalid } },
            })
            {
                await Should.ThrowAsync<OperationStatusUpdateConflictException>(() => store.UpsertAsync(malformed, Token).AsTask());
                (await store.TryGetAsync(original.TenantId, original.OperationId, Token)).ShouldBeNull();
                JsonElement wire = OperationStatusHttpResults.ToJsonElement(malformed);
                wire.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.GenericDenial);
                wire.TryGetProperty("resourceIds", out _).ShouldBeFalse();
                wire.TryGetProperty("lastTransition", out _).ShouldBeFalse();
            }
        }
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void DueCanonicalRetryShouldRespectOneBasedAttemptCeilingWhileLegacyCountRemainsCompatible(int retryCount, bool eligible)
    {
        CommandOperationState failed = CommandOperationContractTests.Advance(
            CommandOperationContractTests.Advance(CommandOperationContractTests.Received(2), CommandExecutionState.Admitted),
            CommandExecutionState.Failed, preEffectFailure: true);
        OperationStatusRecord record = OperationStatusRecord.FromCanonical(failed) with
        {
            RetryCount = retryCount, ReasonCode = ChatBotMessageCodes.AuditUnavailable,
            SafeNextActions = [ChatBotMessageNextActions.RetryLater], NextRetryAt = failed.Transitions[^1].OccurredAt,
        };
        OperationStatusHttpResults.ToJsonElement(record, record.LastUpdatedAt).GetProperty("retryEligible").GetBoolean().ShouldBe(eligible);
        OperationStatusHttpResults.ToJsonElement(record with { CanonicalRevision = null }, record.LastUpdatedAt)
            .GetProperty("retryEligible").GetBoolean().ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MutableInputOrReadArraysShouldNotChangeStoredIdentity(bool sdk)
    {
        IOperationStatusStore store = Store(sdk);
        OperationStatusRecord original = OperationStatusRecord.FromCanonical(Committed());
        await store.UpsertAsync(original, Token);
        original.ResourceIds![0] = "changed-before-read";
        OperationStatusRecord read = (await store.TryGetAsync(original.TenantId, original.OperationId, Token))!;
        read.ResourceIds.ShouldBe(["resource-alpha"]);
        read.ResourceIds![0] = "changed-after-read";
        (await store.TryGetAsync(original.TenantId, original.OperationId, Token))!.ResourceIds.ShouldBe(["resource-alpha"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReorderedAndEquivalentDuplicateUpdatesShouldPreserveCanonicalFoldAndOriginalMetadata(bool sdk)
    {
        IOperationStatusStore store = Store(sdk);
        OperationStatusRecord old = OperationStatusRecord.FromCanonical(CommandOperationContractTests.Received());
        OperationStatusRecord winner = OperationStatusRecord.FromCanonical(Committed());
        await store.UpsertAsync(winner, Token);
        await store.UpsertAsync(old, Token);
        await store.UpsertAsync(winner with { ResourceIds = winner.ResourceIds!.ToArray(), SafeNextActions = winner.SafeNextActions.ToArray() }, Token);
        OperationStatusRecord actual = (await store.TryGetAsync(winner.TenantId, winner.OperationId, Token))!;
        actual.CanonicalRevision.ShouldBe(2);
        actual.ExecutionState.ShouldBe(CommandExecutionState.Committed);
        actual.CommittedRevision.ShouldBe(1);
        actual.Origin.ShouldBe("api");
        actual.CorrelationId.ShouldBe(CommandOperationContractTests.CorrelationId);
        actual.CreatedAt.ShouldBe(CommandOperationContractTests.CreatedAt);
        actual.ResourceIds.ShouldBe(["resource-alpha"]);
        actual.CompletionStatus.ShouldBe(OperationStatusRecord.AcceptedProjectionPending);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EqualRevisionWithCompetingSemanticsShouldConflictWithoutReplacingWinner(bool sdk)
    {
        IOperationStatusStore store = Store(sdk);
        OperationStatusRecord original = OperationStatusRecord.FromCanonical(Committed());
        await store.UpsertAsync(original, Token);
        OperationStatusUpdateConflictException exception = await Should.ThrowAsync<OperationStatusUpdateConflictException>(
            () => store.UpsertAsync(original with { ReasonCode = ChatBotMessageCodes.AuditUnavailable }, Token).AsTask());
        exception.Message.ShouldBe("operation_status_conflict");
        (await store.TryGetAsync(original.TenantId, original.OperationId, Token))!.ReasonCode.ShouldBe(original.ReasonCode);
    }

    [Theory]
    [InlineData("command")]
    [InlineData("origin")]
    [InlineData("resources")]
    [InlineData("creation")]
    [InlineData("correlation")]
    [InlineData("class")]
    [InlineData("equivalence")]
    [InlineData("expected")]
    [InlineData("legacy")]
    public async Task CanonicalIdentityFieldsAndGuardShouldNeverDrift(string field)
    {
        IOperationStatusStore store = Store(true);
        OperationStatusRecord original = OperationStatusRecord.FromCanonical(Committed());
        await store.UpsertAsync(original, Token);
        OperationStatusRecord changed = field switch
        {
            "command" => original with { CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAX" },
            "origin" => original with { Origin = "ui" },
            "resources" => original with { ResourceIds = ["resource-beta"] },
            "creation" => original with { CreatedAt = original.CreatedAt!.Value.AddSeconds(1) },
            "correlation" => original with { CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX" },
            "class" => original with { OperationClass = "other" },
            "equivalence" => original with { EquivalenceVersion = "v2" },
            "expected" => original with { ExpectedRevision = 1 },
            _ => original with { CanonicalRevision = null, ExecutionState = null, ProjectionState = null, ExpectedRevision = null, CommittedRevision = null, LastTransition = null },
        };
        await Should.ThrowAsync<OperationStatusUpdateConflictException>(() => store.UpsertAsync(changed, Token).AsTask());
        (await store.TryGetAsync(original.TenantId, original.OperationId, Token))!.CanonicalRevision.ShouldBe(2);
    }

    [Theory]
    [InlineData("rollback")]
    [InlineData("commit")]
    [InlineData("reason")]
    [InlineData("terminal")]
    [InlineData("prior")]
    [InlineData("completion")]
    [InlineData("actions")]
    public async Task HigherRevisionShouldPreserveTerminalOutcomeAndCommittedFields(string change)
    {
        IOperationStatusStore store = Store(true);
        OperationStatusRecord original = OperationStatusRecord.FromCanonical(Committed()) with
        {
            PriorOutcome = new() { CommandId = CommandOperationContractTests.OperationId, OperationId = CommandOperationContractTests.OperationId,
                CorrelationId = CommandOperationContractTests.CorrelationId, AcceptedAt = CommandOperationContractTests.CreatedAt,
                ReasonCode = Hexalith.ChatBot.Client.Generated.ChatBotMessageCode.Command_accepted },
        };
        await store.UpsertAsync(original, Token);
        OperationStatusRecord candidate = original with { CanonicalRevision = 3, ProjectionState = CommandProjectionState.Projected,
            CompletionStatus = OperationStatusRecord.Completed, LastUpdatedAt = original.LastUpdatedAt.AddSeconds(1),
            LastTransition = original.LastTransition! with { Revision = 3, ProjectionState = CommandProjectionState.Projected, OccurredAt = original.LastUpdatedAt.AddSeconds(1) } };
        candidate = change switch
        {
            "rollback" => candidate with { ExecutionState = CommandExecutionState.Admitted, CommittedRevision = null,
                LastTransition = candidate.LastTransition! with { State = CommandExecutionState.Admitted, CommittedRevision = null } },
            "commit" => candidate with { CommittedRevision = 9, LastTransition = candidate.LastTransition! with { CommittedRevision = 9 } },
            "reason" => candidate with { ReasonCode = ChatBotMessageCodes.AuditUnavailable },
            "terminal" => candidate with { TerminalReasonCode = null },
            "completion" => candidate with { CompletionStatus = OperationStatusRecord.AcceptedProjectionPending },
            "actions" => candidate with { SafeNextActions = ["retry-later"] },
            _ => candidate with { PriorOutcome = null },
        };
        await Should.ThrowAsync<OperationStatusUpdateConflictException>(() => store.UpsertAsync(candidate, Token).AsTask());
        OperationStatusRecord actual = (await store.TryGetAsync(original.TenantId, original.OperationId, Token))!;
        actual.CanonicalRevision.ShouldBe(2);
        actual.CommittedRevision.ShouldBe(1);
        actual.PriorOutcome.ShouldNotBeNull().CorrelationId.ShouldBe(CommandOperationContractTests.CorrelationId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SyntheticRevisionRaceShouldPersistExactlyOneWinningStatus(bool sdk)
    {
        AtomicOperationStatusReadModelStore backing = new();
        IOperationStatusStore store = sdk ? new ReadModelOperationStatusStore(backing) : new InMemoryOperationStatusStore();
        CommandOperationState admitted = CommandOperationContractTests.Advance(CommandOperationContractTests.Received(), CommandExecutionState.Admitted);
        OperationStatusRecord first = OperationStatusRecord.FromCanonical(admitted);
        await store.UpsertAsync(first, Token);
        OperationStatusRecord committed = OperationStatusRecord.FromCanonical(Committed());
        OperationStatusRecord rejected = OperationStatusRecord.FromCanonical(CommandOperationContractTests.Advance(admitted, CommandExecutionState.Rejected));
        backing.RaceNextPair = sdk;
        using Barrier workersReady = new(2);
        int entered = 0;
        Task<bool> Write(OperationStatusRecord record) => Task.Run(async () =>
        {
            Interlocked.Increment(ref entered);
            workersReady.SignalAndWait(Token);
            try { await store.UpsertAsync(record, Token).ConfigureAwait(false); return true; }
            catch (OperationStatusUpdateConflictException) { return false; }
        }, Token);
        bool[] results = await Task.WhenAll(Write(committed), Write(rejected));
        entered.ShouldBe(2);
        results.Count(success => success).ShouldBe(1);
        OperationStatusRecord actual = (await store.TryGetAsync(first.TenantId, first.OperationId, Token))!;
        actual.CanonicalRevision.ShouldBe(2);
        string winningEndState = JsonSerializer.Serialize(results[0] ? committed : rejected);
        JsonSerializer.Serialize(actual).ShouldBe(winningEndState);
        JsonSerializer.Serialize(await store.TryGetAsync(first.TenantId, first.OperationId, Token)).ShouldBe(winningEndState);
        if (sdk) { backing.ConditionalConflicts.ShouldBe(1); }
    }

    [Fact]
    public void CanonicalStatusShouldExposeCompleteSafeFactsAndKeepProjectionPendingSeparateFromCommit()
    {
        CommandOperationState committed = Committed();
        OperationStatusRecord record = OperationStatusRecord.FromCanonical(committed) with { PartialOutputCodes = ["metadata-only"] };
        JsonElement wire = OperationStatusHttpResults.ToJsonElement(record, record.LastUpdatedAt);
        wire.GetProperty("executionState").GetString().ShouldBe("Committed");
        wire.GetProperty("projectionState").GetString().ShouldBe("ProjectionPending");
        wire.GetProperty("completionStatus").GetString().ShouldBe("accepted-projection-pending");
        wire.GetProperty("canonicalRevision").GetInt64().ShouldBe(2);
        wire.GetProperty("expectedRevision").GetInt64().ShouldBe(0);
        wire.GetProperty("committedRevision").GetInt64().ShouldBe(1);
        wire.GetProperty("origin").GetString().ShouldBe("api");
        wire.GetProperty("resourceIds").EnumerateArray().Single().GetString().ShouldBe("resource-alpha");
        wire.GetProperty("maxAttempts").GetInt32().ShouldBe(1);
        wire.GetProperty("hasPartialOutputs").GetBoolean().ShouldBeTrue();
        wire.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        wire.GetProperty("terminalReasonCode").GetString().ShouldBe(ChatBotMessageCodes.CommandAccepted);
        JsonElement fact = wire.GetProperty("lastTransition");
        fact.GetProperty("actorId").GetString().ShouldBe("actor-alpha");
        fact.GetProperty("occurredAt").GetDateTimeOffset().Offset.ShouldBe(TimeSpan.Zero);
        fact.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.CommandAccepted);
        fact.GetProperty("correlationId").GetString().ShouldBe(CommandOperationContractTests.CorrelationId);
        CommandOperationState projected = CommandOperationReducer.Project(committed, 2, CommandProjectionState.Projected,
            "projector", record.LastUpdatedAt.AddSeconds(1), ChatBotMessageCodes.CommandAccepted, CommandOperationContractTests.CorrelationId).State!;
        OperationStatusHttpResults.ToJsonElement(OperationStatusRecord.FromCanonical(projected)).GetProperty("completionStatus").GetString().ShouldBe("completed");
    }

    [Fact]
    public void UnknownSafeReasonShouldSuppressAllNewMetadataAndRetryPermission()
    {
        OperationStatusRecord record = OperationStatusRecord.FromCanonical(Committed()) with { ReasonCode = "private-reason", Origin = "private-origin",
            ResourceIds = ["private-resource"], LastTransition = Committed().Transitions[^1] with { ActorId = "private-actor" } };
        string wire = OperationStatusHttpResults.ToJsonElement(record).GetRawText();
        wire.ShouldContain("generic_denial");
        wire.ShouldNotContain("private-");
        wire.ShouldNotContain("lastTransition");
        wire.ShouldNotContain("committedRevision");
    }
}
