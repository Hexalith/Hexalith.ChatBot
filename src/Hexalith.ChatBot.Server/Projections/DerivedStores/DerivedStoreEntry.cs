using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>
/// A metadata-only derived-store entry (Story 9.5, AC1, NFR2/NFR42 no-leak floor). Derived stores hold the system's most
/// sensitive material (embeddings, prompt context, candidate payloads); a <see cref="DerivedStoreEntry"/> carries
/// <b>only</b> safe bounded tokens — a safe <see cref="ResourceId"/> and a bounded <see cref="ContentDigest"/>/sentinel
/// token — <b>never</b> raw vector floats, embedding values, prompt text, or candidate payloads. Every field is reduced
/// to an <see cref="AuditMetadata"/>-safe token via <see cref="Create"/>, so a malformed token can never smuggle content
/// into the store (mirrors <c>OutboundTraceRecord.FromRequest</c>).
/// </summary>
/// <param name="ResourceId">The safe logical resource id this entry is keyed by.</param>
/// <param name="ContentDigest">A bounded metadata-only digest/sentinel token standing in for the (absent) content.</param>
internal sealed record DerivedStoreEntry(string ResourceId, string ContentDigest)
{
    private const string SafeFallback = "redacted-ref";

    /// <summary>Builds a metadata-only entry, sanitizing both fields to safe bounded tokens.</summary>
    /// <param name="resourceId">The logical resource id.</param>
    /// <param name="contentDigest">A metadata-only digest/sentinel token (never raw content).</param>
    /// <returns>The sanitized entry.</returns>
    public static DerivedStoreEntry Create(string resourceId, string? contentDigest)
        => new(Safe(resourceId), Safe(contentDigest));

    private static string Safe(string? value) => AuditMetadata.SafeOptionalToken(value) ?? SafeFallback;
}
