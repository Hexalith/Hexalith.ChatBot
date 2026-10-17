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

    private static OperationStatusRecord Legacy()
        => OperationStatusRecord.Accepted("tenant-alpha", new Hexalith.ChatBot.Client.Generated.CommandSubmissionResponse
        {
            CommandId = CommandOperationContractTests.OperationId, OperationId = CommandOperationContractTests.OperationId,
            CorrelationId = CommandOperationContractTests.CorrelationId, AcceptedAt = CommandOperationContractTests.CreatedAt,
            LifecycleState = Hexalith.ChatBot.Client.Generated.LifecycleState.Proposed,
        }, false, CommandOperationContractTests.CreatedAt);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyOlderWritesShouldPreserveNewerStatusAndRejectIdentityDrift(bool sdk)
    {
        IOperationStatusStore store = Store(sdk);
        OperationStatusRecord old = Legacy();
        OperationStatusRecord latest = old with { LastUpdatedAt = old.LastUpdatedAt.AddMinutes(1),
            LifecycleState = Hexalith.ChatBot.Client.Generated.LifecycleState.Failed, CompletionStatus = OperationStatusRecord.Failed,
            ReasonCode = ChatBotMessageCodes.AuditUnavailable };
        await store.UpsertAsync(latest, Token);
        await store.UpsertAsync(old, Token);
        OperationStatusRecord persisted = (await store.TryGetAsync(old.TenantId, old.OperationId, Token))!;
        persisted.LifecycleState.ShouldBe(latest.LifecycleState);
        persisted.ReasonCode.ShouldBe(latest.ReasonCode);
        persisted.LastUpdatedAt.ShouldBe(latest.LastUpdatedAt);
        foreach (OperationStatusRecord invalid in new[]
        {
            old with { CommandId = "different-command" }, old with { CorrelationId = "different-correlation" },
            old with { AcceptedAt = old.AcceptedAt.AddSeconds(1) }, old with { OperationClass = "different-class" },
        })
        {
            await Should.ThrowAsync<OperationStatusUpdateConflictException>(() => store.UpsertAsync(invalid, Token).AsTask());
            JsonSerializer.Serialize(await store.TryGetAsync(old.TenantId, old.OperationId, Token)).ShouldBe(JsonSerializer.Serialize(persisted));
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task LegacyReceiptCommitShouldSurviveLaterReplayTimestampInEitherArrivalOrder(bool sdk, bool receiptFirst)
    {
        IOperationStatusStore store = Store(sdk);
        OperationStatusRecord receipt = Legacy();
        OperationStatusRecord replay = receipt with { AuditStatus = OperationStatusRecord.AuditReconciling,
            LastUpdatedAt = receipt.LastUpdatedAt.AddSeconds(1), DuplicateAttemptCount = 3,
            PriorOutcome = new() { CommandId = receipt.CommandId, OperationId = receipt.OperationId,
                CorrelationId = receipt.CorrelationId, AcceptedAt = receipt.AcceptedAt } };
        await store.UpsertAsync(receiptFirst ? receipt : replay, Token);
        await store.UpsertAsync(receiptFirst ? replay : receipt, Token);
        await store.UpsertAsync(receipt with { LastUpdatedAt = replay.LastUpdatedAt.AddSeconds(1) }, Token);
        OperationStatusRecord persisted = (await store.TryGetAsync(receipt.TenantId, receipt.OperationId, Token))!;
        persisted.AuditStatus.ShouldBe(OperationStatusRecord.AuditCommitted);
        persisted.DuplicateAttemptCount.ShouldBe(3);
        persisted.PriorOutcome.ShouldNotBeNull().OperationId.ShouldBe(receipt.OperationId);
        persisted.LastUpdatedAt.ShouldBe(replay.LastUpdatedAt.AddSeconds(1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StatusMetadataShouldRejectImplicitLegacyUpgradeAndUnnormalizedCanonicalIdentities(bool sdk)
    {
        IOperationStatusStore store = Store(sdk);
        OperationStatusRecord legacy = Legacy();
        OperationStatusRecord canonical = OperationStatusRecord.FromCanonical(Committed());
        foreach (OperationStatusRecord invalid in new[]
        {
            legacy with { Origin = "api" }, legacy with { ResourceIds = ["private resource"] },
            legacy with { CreatedAt = legacy.AcceptedAt }, legacy with { EquivalenceVersion = "v1" },
            legacy with { PredecessorOperationId = "private predecessor" },
            canonical with { OperationId = canonical.OperationId.ToLowerInvariant(), CommandId = canonical.CommandId.ToLowerInvariant() },
            canonical with { CorrelationId = canonical.CorrelationId.ToLowerInvariant() },
            canonical with { PredecessorOperationId = "not-a-ulid", PriorOutcome = new() { OperationId = "not-a-ulid" } },
            canonical with { PredecessorOperationId = CommandOperationContractTests.CorrelationId.ToLowerInvariant(),
                PriorOutcome = new() { OperationId = CommandOperationContractTests.CorrelationId.ToLowerInvariant() } },
            canonical with { PredecessorOperationId = CommandOperationContractTests.CorrelationId },
            canonical with { PriorOutcome = new() },
            canonical with { ReasonCode = ChatBotMessageCodes.AuditUnavailable },
            canonical with { SafeNextActions = [ChatBotMessageNextActions.RetryLater] },
            canonical with { NextRetryAt = canonical.LastUpdatedAt },
        })
        {
            await Should.ThrowAsync<OperationStatusUpdateConflictException>(() => store.UpsertAsync(invalid, Token).AsTask());
            (await store.TryGetAsync(invalid.TenantId, invalid.OperationId, Token)).ShouldBeNull();
            JsonElement wire = OperationStatusHttpResults.ToJsonElement(invalid);
            wire.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.GenericDenial);
            wire.TryGetProperty("resourceIds", out _).ShouldBeFalse();
            wire.TryGetProperty("predecessorOperationId", out _).ShouldBeFalse();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LowercaseTransitionCorrelationShouldFoldNormalizeReplayAndPersistValidStatus(bool sdk)
    {
        CommandOperationState received = CommandOperationContractTests.Received();
        CommandOperationTransition original = CommandOperationContractTests.Advance(received, CommandExecutionState.Admitted).Transitions[^1]
            with { CorrelationId = CommandOperationContractTests.CorrelationId.ToLowerInvariant() };
        CommandOperationResult applied = CommandOperationReducer.Apply(received, original);
        applied.Succeeded.ShouldBeTrue();
        CommandOperationState admitted = applied.State.ShouldNotBeNull();
        admitted.Transitions[^1].CorrelationId.ShouldBe(CommandOperationContractTests.CorrelationId);
        CommandOperationResult replay = CommandOperationReducer.Apply(admitted, original);
        replay.IsReplay.ShouldBeTrue();
        replay.State.ShouldBeSameAs(admitted);
        CommandOperationReducer.Apply(admitted, admitted.Transitions[^1]).IsReplay.ShouldBeTrue();
        CommandOperationResult invalid = CommandOperationReducer.Apply(received, original with { CorrelationId = "invalid-correlation" });
        invalid.Conflict.ShouldBe(CommandOperationConflict.InvalidTransition);
        invalid.State.ShouldBeSameAs(received);
        OperationStatusRecord status = OperationStatusRecord.FromCanonical(admitted);
        OperationStatusUpdatePolicy.HasValidMetadata(status).ShouldBeTrue();
        IOperationStatusStore store = Store(sdk);
        await store.UpsertAsync(status, Token);
        OperationStatusRecord persisted = (await store.TryGetAsync(status.TenantId, status.OperationId, Token)).ShouldNotBeNull();
        persisted.LastTransition!.CorrelationId.ShouldBe(CommandOperationContractTests.CorrelationId);
        JsonElement wire = OperationStatusHttpResults.ToJsonElement(persisted);
        wire.GetProperty("executionState").GetString().ShouldBe("Admitted");
        wire.GetProperty("lastTransition").GetProperty("correlationId").GetString().ShouldBe(CommandOperationContractTests.CorrelationId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanonicalStatusShouldRejectMalformedOperationClassAndSuppressItOverHttp(bool sdk)
    {
        IOperationStatusStore store = Store(sdk);
        OperationStatusRecord original = OperationStatusRecord.FromCanonical(Committed());
        foreach (string invalid in new[] { "", " ", "Command-execution", "1command", "command_execution", "command.execution", "private/class", "é", new string('a', 81) })
        {
            OperationStatusRecord malformed = original with { OperationClass = invalid };
            await Should.ThrowAsync<OperationStatusUpdateConflictException>(() => store.UpsertAsync(malformed, Token).AsTask());
            (await store.TryGetAsync(original.TenantId, original.OperationId, Token)).ShouldBeNull();
            JsonElement wire = OperationStatusHttpResults.ToJsonElement(malformed);
            wire.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.GenericDenial);
            wire.GetProperty("operationClass").GetString().ShouldBe("command-execution");
            wire.TryGetProperty("lastTransition", out _).ShouldBeFalse();
        }
        OperationStatusRecord bounded = original with { OperationClass = new string('a', 80) };
        await store.UpsertAsync(bounded, Token);
        (await store.TryGetAsync(original.TenantId, original.OperationId, Token))!.OperationClass.ShouldBe(bounded.OperationClass);
        OperationStatusHttpResults.ToJsonElement(bounded).GetProperty("operationClass").GetString().ShouldBe(bounded.OperationClass);
    }

    [Theory]
    [InlineData(CommandExecutionState.Received)]
    [InlineData(CommandExecutionState.Admitted)]
    [InlineData(CommandExecutionState.Rejected)]
    [InlineData(CommandExecutionState.Failed)]
    [InlineData(CommandExecutionState.Committed)]
    public void CanonicalStatusShouldUseFoldedExecutionAndProjectionAsAuthority(CommandExecutionState state)
    {
        CommandOperationState folded = CommandOperationContractTests.Received(2);
        if (state != CommandExecutionState.Received) { folded = CommandOperationContractTests.Advance(folded, CommandExecutionState.Admitted); }
        if (state is not (CommandExecutionState.Received or CommandExecutionState.Admitted))
        {
            folded = CommandOperationContractTests.Advance(folded, state, state == CommandExecutionState.Committed ? 1 : null, state == CommandExecutionState.Failed);
        }
        OperationStatusRecord record = OperationStatusRecord.FromCanonical(folded);
        OperationStatusUpdatePolicy.HasValidMetadata(record).ShouldBeTrue();
        JsonElement wire = OperationStatusHttpResults.ToJsonElement(record);
        wire.GetProperty("executionState").GetString().ShouldBe(state.ToString());
        wire.GetProperty("projectionState").GetString().ShouldBe("ProjectionPending");
        wire.GetProperty("reasonCode").GetString().ShouldBe(folded.Transitions[^1].ReasonCode);
        wire.GetProperty("safeNextActions").EnumerateArray().Single().GetString().ShouldBe(ChatBotMessageCatalog.Resolve(record.ReasonCode).NextAction);
        wire.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        wire.GetProperty("lastTransition").TryGetProperty("actorId", out _).ShouldBeFalse();
        if (state != CommandExecutionState.Committed) { wire.TryGetProperty("committedRevision", out _).ShouldBeFalse(); }
    }

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

    [Fact]
    public void CanonicalRetryShouldRemainClosedUntilProfileApprovalWhileLegacySchedulingRemainsCompatible()
    {
        CommandOperationState failed = CommandOperationContractTests.Advance(
            CommandOperationContractTests.Advance(CommandOperationContractTests.Received(2), CommandExecutionState.Admitted),
            CommandExecutionState.Failed, preEffectFailure: true);
        failed.RetryEligible.ShouldBeTrue();
        OperationStatusRecord canonical = OperationStatusRecord.FromCanonical(failed);
        JsonElement wire = OperationStatusHttpResults.ToJsonElement(canonical, canonical.LastUpdatedAt);
        wire.GetProperty("executionState").GetString().ShouldBe("Failed");
        wire.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        wire.GetProperty("hasPartialOutputs").GetBoolean().ShouldBeFalse();
        wire.TryGetProperty("nextRetryAt", out _).ShouldBeFalse();
        OperationStatusRecord legacy = Legacy() with
        {
            RetryCount = 1, MaxAttempts = 2, ReasonCode = ChatBotMessageCodes.AuditUnavailable,
            SafeNextActions = [ChatBotMessageNextActions.RetryLater], NextRetryAt = failed.Transitions[^1].OccurredAt,
        };
        OperationStatusHttpResults.ToJsonElement(legacy, legacy.NextRetryAt).GetProperty("retryEligible").GetBoolean().ShouldBeTrue();
        OperationStatusHttpResults.ToJsonElement(legacy with { RetryCount = 2 }, legacy.NextRetryAt)
            .GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
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
        CommandOperationState failed = CommandOperationContractTests.Advance(
            CommandOperationContractTests.Advance(CommandOperationContractTests.Received(2), CommandExecutionState.Admitted),
            CommandExecutionState.Failed, preEffectFailure: true);
        var retryIdentity = CommandOperationContractTests.Identity(Hexalith.ChatBot.Contracts.Identities.ChatBotIdentity.NewUlid(),
            createdAt: failed.Transitions[^1].OccurredAt.AddSeconds(1), predecessor: failed.Identity.OperationId);
        CommandOperationState retry = CommandOperationReducer.Retry(failed, retryIdentity, CommandOperationContractTests.Input(), 0).State!;
        OperationStatusRecord original = OperationStatusRecord.FromCanonical(CommandOperationContractTests.Advance(
            CommandOperationContractTests.Advance(retry, CommandExecutionState.Admitted), CommandExecutionState.Committed, 1));
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
        fact.TryGetProperty("actorId", out _).ShouldBeFalse();
        record.LastTransition!.ActorId.ShouldBe("actor-alpha");
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
