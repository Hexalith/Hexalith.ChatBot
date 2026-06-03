using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.4 (AC2, FR95a) coverage that the replay marker is threaded from the immutable submission into <b>real</b>
/// audit envelopes via the single <c>AuditEnvelopeFactory.Create</c> point, and that those real marked records fire the
/// Story 9.2/9.3 exclusions (which this story makes operate on real data, not synthetic records). A production submission
/// leaves the marker null; a replay submission marks every command-path envelope; the marked record is excluded from the
/// default compliance search and from the completeness measure; and a marked envelope hashes distinctly from its null
/// twin under the v2 canonical form.
/// </summary>
public sealed class ReplayMarkerThreadingTests
{
    private const string Tenant = "tenant-alpha";
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string ReplayRunId = "replay-run-001";
    private static readonly DateTimeOffset Now = new(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ReplaySubmissionMarksBothPreAndPostCommitEnvelopes()
    {
        ChatBotGatewayContext context = Context(ReplayRunId);

        AuditEnvelope preCommit = AuditEnvelopeFactory.PreCommit(context, Transition, Now);
        AuditEnvelope postCommit = AuditEnvelopeFactory.PostCommit(context, new ChatBotDispatchResult(Now, "res-1"), Transition, Now);

        preCommit.ReplayRunId.ShouldBe(ReplayRunId);
        postCommit.ReplayRunId.ShouldBe(ReplayRunId);
        AuditReplayExclusion.IsReplayEnvelope(preCommit).ShouldBeTrue();
        AuditReplayExclusion.IsReplayEnvelope(postCommit).ShouldBeTrue();
    }

    [Fact]
    public void ReplaySubmissionMarksEveryCommandPathFactoryEnvelope()
    {
        // Task 2 / AC2: BECAUSE every command-path factory funnels through the single Create point, marking the
        // submission marks pre-commit, post-commit, duplicate-suppression AND rejection in one place. This guards
        // against a regression where a new (or relocated) command-path factory bypasses Create and silently drops the
        // marker — the exact failure mode the "single envelope-construction point" discipline exists to prevent.
        ChatBotGatewayContext context = Context(ReplayRunId);

        AuditEnvelope duplicateSuppressed = AuditEnvelopeFactory.DuplicateMailboxIntakeSuppressed(context, Transition, Now);
        AuditEnvelope rejected = AuditEnvelopeFactory.RejectedLifecycleTransition(
            context,
            LifecycleTransitionValidation.Invalid(Transition),
            Now);

        duplicateSuppressed.ReplayRunId.ShouldBe(ReplayRunId);
        rejected.ReplayRunId.ShouldBe(ReplayRunId);
        AuditReplayExclusion.IsReplayEnvelope(duplicateSuppressed).ShouldBeTrue();
        AuditReplayExclusion.IsReplayEnvelope(rejected).ShouldBeTrue();
    }

    [Fact]
    public void ProductionSubmissionLeavesEveryCommandPathFactoryEnvelopeUnmarked()
    {
        // The mirror of the above: a production submission leaves the marker null by omission across the same set of
        // command-path factory methods, so none of them is ever excluded from production audit queries.
        ChatBotGatewayContext context = Context(replayRunId: null);

        AuditEnvelope duplicateSuppressed = AuditEnvelopeFactory.DuplicateMailboxIntakeSuppressed(context, Transition, Now);
        AuditEnvelope rejected = AuditEnvelopeFactory.RejectedLifecycleTransition(
            context,
            LifecycleTransitionValidation.Invalid(Transition),
            Now);

        duplicateSuppressed.ReplayRunId.ShouldBeNull();
        rejected.ReplayRunId.ShouldBeNull();
        AuditReplayExclusion.IsReplayEnvelope(duplicateSuppressed).ShouldBeFalse();
        AuditReplayExclusion.IsReplayEnvelope(rejected).ShouldBeFalse();
    }

    [Fact]
    public void ProductionSubmissionLeavesTheMarkerNull()
    {
        ChatBotGatewayContext context = Context(replayRunId: null);

        AuditEnvelope preCommit = AuditEnvelopeFactory.PreCommit(context, Transition, Now);

        preCommit.ReplayRunId.ShouldBeNull();
        AuditReplayExclusion.IsReplayEnvelope(preCommit).ShouldBeFalse();
    }

    [Fact]
    public void AnUnsafeReplayRunIdIsDroppedToNullNotLeaked()
    {
        // The marker passes through AuditMetadata.SafeOptionalToken, so an unsafe value never lands on the envelope.
        ChatBotGatewayContext context = Context("replay run with spaces");

        AuditEnvelope preCommit = AuditEnvelopeFactory.PreCommit(context, Transition, Now);

        preCommit.ReplayRunId.ShouldBeNull();
    }

    [Fact]
    public void ARealMarkedEnvelopeIsExcludedFromTheDefaultComplianceSearch()
    {
        AuditEnvelope replay = AuditEnvelopeFactory.PreCommit(Context(ReplayRunId), Transition, Now);
        AuditEnvelope production = AuditEnvelopeFactory.PreCommit(Context(replayRunId: null), Transition, Now)
            with { ResourceId = "audit-record-production" };

        Hexalith.ChatBot.Contracts.Queries.ComplianceAuditSearchResult result = ComplianceAuditReadPolicy.Search(
            CompliancePrincipal(),
            new Hexalith.ChatBot.Contracts.Commands.ComplianceAuditQueryFilters(
                "audit-query-001",
                [new Hexalith.ChatBot.Contracts.Commands.ComplianceAuditFilterRef("audit-filter-tenant", "tenant", Tenant)],
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 3, 0, 0, 0, TimeSpan.Zero),
                100),
            [production, replay],
            new DateTimeOffset(2026, 6, 3, 5, 0, 0, TimeSpan.Zero),
            Correlation);

        result.Rows.ShouldHaveSingleItem().AuditRecordRef.ShouldBe("audit-record-production");
    }

    [Fact]
    public async Task ARealMarkedRecordIsExcludedFromTheCompletenessMeasure()
    {
        // A replay-marked record on a tenant's chain counts toward neither numerator nor denominator: a chain of ONE
        // replay record measures as a vacuously-complete window with zero in-scope operations (never an Unmeasurable
        // breach), proving the marker drives the exclusion.
        InMemoryWormAuditStore wormStore = new();
        _ = await wormStore.AppendAsync(
            AuditEnvelopeFactory.PostCommit(Context(ReplayRunId), new ChatBotDispatchResult(Now, "res-1"), Transition, Now),
            CancellationToken.None);

        AuditCompletenessMeasurer measurer = new(wormStore, new InMemoryGovernedOperationProjectionStore(), new WormAuditTestData.FixedClock(Now));
        AuditCompletenessMeasurement measurement = await measurer.MeasureTenantAsync(Tenant, CancellationToken.None);

        measurement.IsMeasurable.ShouldBeTrue();
        measurement.TotalCount.ShouldBe(0);
        measurement.Fraction.ShouldBe(1.0);
    }

    [Fact]
    public void AReplayMarkedEnvelopeHashesDistinctlyFromItsNullTwinUnderV2()
    {
        AuditEnvelope production = AuditEnvelopeFactory.PreCommit(Context(replayRunId: null), Transition, Now);
        AuditEnvelope replay = production with { ReplayRunId = ReplayRunId };

        string productionHash = WormAuditChainHasher.ComputeRecordHash(production, WormAuditChainHasher.GenesisPredecessorHash, 0);
        string replayHash = WormAuditChainHasher.ComputeRecordHash(replay, WormAuditChainHasher.GenesisPredecessorHash, 0);

        replayHash.ShouldNotBe(productionHash);
    }

    [Fact]
    public async Task AReplayRunWritesOnlyToTheTestTenantChainAndLeavesProductionChainsUnmutated()
    {
        // AC1 (mandated assertion): a replay run "mutates no production project state". Because the run executes under a
        // TEST tenant its audit record is partitioned to the test tenant's chain by construction (NFR9a), so NO
        // production tenant's WORM chain grows during the run. AC1 requires the story to ASSERT this, not assume it.
        const string testTenant = ReplayTenantPolicy.ReplayTestTenantPrefix + "tenant-alpha";
        InMemoryWormAuditStore wormStore = new();

        AuditEnvelope replayEnvelope = AuditEnvelopeFactory.PostCommit(
            Context(ReplayRunId, testTenant),
            new ChatBotDispatchResult(Now, "res-1"),
            Transition,
            Now);
        _ = await wormStore.AppendAsync(replayEnvelope, CancellationToken.None);

        // The replay record lands ONLY in the test tenant's partition, carrying the marker.
        wormStore.EnumerateChain(testTenant).ShouldHaveSingleItem().Envelope.ReplayRunId.ShouldBe(ReplayRunId);

        // No production tenant's chain grew during the replay run — isolation by construction, asserted not assumed.
        wormStore.EnumerateChain(Tenant).ShouldBeEmpty();
        wormStore.EnumerateTenants().ShouldBe([testTenant]);
    }

    private static readonly LifecycleTransitionDefinition Transition =
        new(LifecycleStates.Received, LifecycleStates.Proposed);

    private static ChatBotGatewayContext Context(string? replayRunId, string tenant = Tenant)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test"));
        ChatBotCommandSubmission submission = new(
            principal,
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = nameof(RecordGovernedNote),
                Command = new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAX"),
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            Correlation,
            TaskId: null,
            ChatBotSurfaceOrigin.Api,
            replayRunId);
        return new ChatBotGatewayContext(
            submission,
            new ChatBotAuthenticatedActor("actor-alpha", principal),
            new ChatBotTenantBinding(tenant));
    }

    private static ClaimsPrincipal CompliancePrincipal()
        => new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, "human"),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, "compliance-admin"),
            ],
            "test"));
}
