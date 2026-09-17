namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>
/// The metadata-only outcome of a <see cref="IVectorReindexer.ReindexVectorsAsync"/> call (Story 9.6, AC1/AC2). Carries
/// <b>only</b> safe counts and flags — never vector floats, embedding values, prompt text, or candidate payloads
/// (NFR2/NFR42 no-leak floor).
/// </summary>
/// <param name="EntriesInvalidated">How many previously-present entries were structurally removed across the four derived-store classes.</param>
/// <param name="EntriesRebuilt">How many corrected entries were rebuilt across the four derived-store classes.</param>
/// <param name="VersionGuardSkipped">True when the entire reindex was a version-guard no-op (a re-delivered/older correction advanced no partition).</param>
/// <param name="SloBreached">True when the reindex completed after its computed M2 deadline (NFR17a, surfaces correction-delayed).</param>
/// <param name="DeadlineUtc">The effective M2 completion deadline (started-at + 60 min), from <c>CorrectionPropagationSlo</c>.</param>
/// <param name="CompletedAtUtc">When the reindex finished.</param>
/// <param name="FailureReasonCode">A safe reason code when the reindex failed (e.g. <c>vector_reindex_failed</c>), or null on success.</param>
/// <param name="RemoteOperationId">The opaque Memories operation identifier retained by the durable polling workflow.</param>
internal sealed record VectorReindexOutcome(
    int EntriesInvalidated,
    int EntriesRebuilt,
    bool VersionGuardSkipped,
    bool SloBreached,
    DateTimeOffset DeadlineUtc,
    DateTimeOffset CompletedAtUtc,
    string? FailureReasonCode,
    bool IsTerminal = true,
    string? RemoteOperationId = null);
