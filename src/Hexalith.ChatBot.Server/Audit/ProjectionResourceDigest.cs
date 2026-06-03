namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The metadata-only per-resource structural digest the <see cref="ProjectionRebuildEquivalenceEvaluator"/> diffs
/// (Story 9.12, AC2, NFR2/NFR42 no-leak floor). A snapshot of a derived projection is an ordered set of these digests;
/// equivalence is proven by comparing the two snapshots' digests, never by comparing raw item content.
/// <para>
/// The <see cref="StructuralStateToken"/> is the <b>same structural-digest discipline</b> as
/// <c>AuditOperationReconstructor.ReconstructedOperationState.ResultingStateToken</c> and the
/// <see cref="Hexalith.ChatBot.Server.Projections.GovernedOperationView"/> structural fields (<c>SchemaVersion</c>,
/// <c>SourceProvenance</c>, <c>DerivationKernelVersion</c>, <c>RedactionState</c>, <c>RetentionClass</c>,
/// <c>SourceVersion</c>) — a path/transition/outcome-style structural fingerprint, <b>never</b> raw email content,
/// recipient PII, subject, body, prompts, payloads, or vector/embedding values.
/// </para>
/// <para>
/// Every field is reduced to an <see cref="AuditMetadata"/>-safe bounded token via <see cref="Create"/> (mirroring
/// <c>DerivedStoreEntry.Create</c>), so a malformed token can never smuggle content into a snapshot.
/// </para>
/// </summary>
/// <param name="ResourceId">The safe logical resource id this digest is keyed by.</param>
/// <param name="StructuralStateToken">A bounded metadata-only structural fingerprint of the resource's derived state.</param>
internal sealed record ProjectionResourceDigest(string ResourceId, string StructuralStateToken)
{
    private const string SafeFallback = "redacted-ref";

    /// <summary>Builds a metadata-only digest, sanitizing both fields to safe bounded tokens.</summary>
    /// <param name="resourceId">The logical resource id.</param>
    /// <param name="structuralStateToken">A metadata-only structural fingerprint token (never raw content).</param>
    /// <returns>The sanitized digest.</returns>
    public static ProjectionResourceDigest Create(string resourceId, string? structuralStateToken)
        => new(Safe(resourceId), Safe(structuralStateToken));

    private static string Safe(string? value) => AuditMetadata.SafeOptionalToken(value) ?? SafeFallback;
}
