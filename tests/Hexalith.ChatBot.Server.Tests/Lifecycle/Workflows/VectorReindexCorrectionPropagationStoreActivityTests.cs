using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Projections.DerivedStores;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle.Workflows;

/// <summary>
/// Story 9.6 (AC1) coverage for the vector-reindex correction-propagation activity: it reports the
/// <c>vector-reindex</c> store key; a clean reindex maps to <c>success</c>; an idempotent version-guard skip is still
/// <c>success</c> (not an error); a reindex failure maps to <c>failed</c> + its reason code; and a completed-but-late
/// reindex surfaces <c>vector_reindex_slo_exceeded</c> so the coordinator can mark correction-delayed.
/// </summary>
public sealed class VectorReindexCorrectionPropagationStoreActivityTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 6, 3, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ActivityReportsTheVectorReindexStoreKey()
    {
        VectorReindexCorrectionPropagationStoreActivity activity = new(new StubReindexer(CleanOutcome()));

        activity.StoreKey.ShouldBe(CorrectionPropagationStoreKeys.VectorReindex);
        activity.StoreKey.ShouldBe("vector-reindex");
    }

    [Fact]
    public async Task ACleanReindexMapsToSuccess()
    {
        VectorReindexCorrectionPropagationStoreActivity activity = new(new StubReindexer(CleanOutcome()));

        CorrectionPropagationActivityResult result = await activity.InvalidateAndRebuildAsync(Request(), TestContext.Current.CancellationToken);

        result.IsSuccessful.ShouldBeTrue();
        result.Outcome.ShouldBe("success");
        result.FailureReasonCode.ShouldBeNull();
        result.StoreKey.ShouldBe("vector-reindex");
    }

    [Fact]
    public async Task AVersionGuardSkipIsStillSuccessAndNotAnError()
    {
        VectorReindexOutcome skipped = CleanOutcome() with { EntriesInvalidated = 0, EntriesRebuilt = 0, VersionGuardSkipped = true };
        VectorReindexCorrectionPropagationStoreActivity activity = new(new StubReindexer(skipped));

        CorrectionPropagationActivityResult result = await activity.InvalidateAndRebuildAsync(Request(), TestContext.Current.CancellationToken);

        result.IsSuccessful.ShouldBeTrue();
        result.FailureReasonCode.ShouldBeNull();
    }

    [Fact]
    public async Task AReindexFailureMapsToFailedWithItsReasonCode()
    {
        VectorReindexOutcome failed = CleanOutcome() with { FailureReasonCode = InMemoryVectorReindexer.VectorReindexFailedReasonCode };
        VectorReindexCorrectionPropagationStoreActivity activity = new(new StubReindexer(failed));

        CorrectionPropagationActivityResult result = await activity.InvalidateAndRebuildAsync(Request(), TestContext.Current.CancellationToken);

        result.IsSuccessful.ShouldBeFalse();
        result.Outcome.ShouldBe("failed");
        result.FailureReasonCode.ShouldBe("vector_reindex_failed");
    }

    [Fact]
    public async Task CanonicalReindexerReceivesGovernedCaseAndStaysPendingUntilTerminal()
    {
        CanonicalStubReindexer reindexer = new(CleanOutcome() with { IsTerminal = false, RemoteOperationId = "operation-9" });
        VectorReindexCorrectionPropagationStoreActivity activity = new(reindexer);

        CorrectionPropagationActivityResult result = await activity.InvalidateAndRebuildAsync(Request(), TestContext.Current.CancellationToken);

        result.Outcome.ShouldBe("awaiting-completion");
        result.IsPending.ShouldBeTrue();
        result.RemoteOperationId.ShouldBe("operation-9");
        reindexer.CanonicalCalls.ShouldBe(1);
        reindexer.LegacyCalls.ShouldBe(0);
        reindexer.LastIntakeId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        reindexer.LastCorrectedCaseId.ShouldBe("case-corrected-001");
        reindexer.LastRemoteOperationId.ShouldBeNull();
    }

    [Fact]
    public async Task ACompletedButLateReindexSurfacesTheSloExceededReasonCode()
    {
        VectorReindexOutcome late = CleanOutcome() with { SloBreached = true };
        VectorReindexCorrectionPropagationStoreActivity activity = new(new StubReindexer(late));

        CorrectionPropagationActivityResult result = await activity.InvalidateAndRebuildAsync(Request(), TestContext.Current.CancellationToken);

        result.IsSuccessful.ShouldBeFalse();
        result.FailureReasonCode.ShouldBe(VectorReindexCorrectionPropagationStoreActivity.SloExceededReasonCode);
        result.FailureReasonCode.ShouldBe("vector_reindex_slo_exceeded");
    }

    private static VectorReindexOutcome CleanOutcome()
        => new(4, 4, VersionGuardSkipped: false, SloBreached: false, StartedAt.AddMinutes(60), StartedAt.AddMinutes(1), FailureReasonCode: null);

    private static CorrectionPropagationActivityRequest Request()
        => new(
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV:correction:5",
            "wf-001",
            CorrectionPropagationStoreKeys.VectorReindex,
            5,
            "project-001",
            "project-002",
            "case-corrected-001",
            StartedAt,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private sealed class StubReindexer(VectorReindexOutcome outcome) : IVectorReindexer
    {
        public ValueTask<VectorReindexOutcome> ReindexVectorsAsync(
            string tenantId,
            string correctionId,
            long sourceVersion,
            IReadOnlyList<string> affectedResourceIds,
            DateTimeOffset startedAtUtc,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(outcome);
    }

    private sealed class CanonicalStubReindexer(VectorReindexOutcome outcome) : IVectorReindexer, ICanonicalVectorReindexer
    {
        public int CanonicalCalls { get; private set; }

        public int LegacyCalls { get; private set; }

        public string? LastIntakeId { get; private set; }

        public string? LastCorrectedCaseId { get; private set; }

        public string? LastRemoteOperationId { get; private set; }

        public ValueTask<VectorReindexOutcome> ReindexVectorsAsync(
            string tenantId,
            string correctionId,
            long sourceVersion,
            IReadOnlyList<string> affectedResourceIds,
            DateTimeOffset startedAtUtc,
            CancellationToken cancellationToken)
        {
            LegacyCalls++;
            return ValueTask.FromResult(Failed());
        }

        public ValueTask<VectorReindexOutcome> ReindexCanonicalVectorsAsync(
            string tenantId,
            string associationId,
            string intakeId,
            string correctionId,
            long sourceVersion,
            string correctedCaseId,
            string? remoteOperationId,
            DateTimeOffset startedAtUtc,
            CancellationToken cancellationToken)
        {
            CanonicalCalls++;
            LastIntakeId = intakeId;
            LastCorrectedCaseId = correctedCaseId;
            LastRemoteOperationId = remoteOperationId;
            return ValueTask.FromResult(outcome);
        }

        private static VectorReindexOutcome Failed()
            => new(0, 0, false, false, StartedAt, StartedAt, MemoriesVectorReindexer.InvalidStatusReasonCode);
    }
}
