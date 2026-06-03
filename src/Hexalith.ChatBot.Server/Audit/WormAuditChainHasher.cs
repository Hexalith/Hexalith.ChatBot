using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Deterministic, tamper-evidence hashing for the WORM audit chain (Story 9.1, NFR49a). Produces a byte-stable SHA-256
/// digest over a canonical serialization of the <see cref="AuditEnvelope"/> plus its predecessor hash and per-tenant
/// sequence. Determinism is a correctness requirement, not a convenience (Epic 8 carry-forward): the canonical form uses
/// a fixed field order, the invariant culture, and a UTC timestamp, so re-hashing the same logical record always yields
/// the same digest. The envelope's own <see cref="AuditEnvelope.PredecessorHash"/> field is excluded from the canonical
/// envelope body and fed in explicitly so the predecessor linkage is counted exactly once.
/// </summary>
internal static class WormAuditChainHasher
{
    /// <summary>
    /// The original Story 9.1 canonical-serialization contract: the field set that does NOT include
    /// <see cref="AuditEnvelope.ReplayRunId"/>. Records stamped with this version re-canonicalize the original way, so
    /// chains written before Story 9.2 stay verifiable byte-for-byte under their own stamped version.
    /// </summary>
    public const string CanonicalSerializationVersionV1 = "chatbot.worm-chain.v1";

    /// <summary>
    /// The current canonical-serialization contract; stored on every newly chained record. Story 9.2 (FR95a) bumped
    /// this from <see cref="CanonicalSerializationVersionV1"/> to v2 to fold the new
    /// <see cref="AuditEnvelope.ReplayRunId"/> into the hash input: a replay record masquerading as a production record
    /// (or vice-versa) must change the digest, so the replay marker is tamper-evident. The bump is deliberate — silently
    /// changing the canonical form would invalidate every Story 9.1 chain; instead canonicalization is version-aware
    /// (see <see cref="CanonicalizeEnvelope(AuditEnvelope, string)"/>) and the verifier re-hashes each record under the
    /// version it was stamped with.
    /// </summary>
    public const string CanonicalSerializationVersion = "chatbot.worm-chain.v2";

    /// <summary>
    /// The fixed sentinel predecessor for the genesis record (sequence 0): a 64-character all-zero hex string. A
    /// distinct sentinel — never <c>null</c> — keeps a genuine chain head unambiguously distinguishable from an
    /// untracked/absent predecessor (which would read as a gap to the verifier).
    /// </summary>
    public const string GenesisPredecessorHash = "0000000000000000000000000000000000000000000000000000000000000000";

    // Control characters that cannot appear inside an AuditMetadata-safe token, so they are unambiguous separators:
    // unit separator between fields, record separator between the three hash-input components.
    private const char FieldSeparator = '\u001F';
    private const char ComponentSeparator = '\u001E';

    /// <summary>
    /// Computes the lowercase-hex SHA-256 record hash for a chained record: the canonical envelope body, the
    /// predecessor hash, and the per-tenant sequence, in that fixed order.
    /// </summary>
    public static string ComputeRecordHash(AuditEnvelope envelope, string predecessorHash, long sequence)
        => ComputeRecordHash(envelope, predecessorHash, sequence, CanonicalSerializationVersion);

    /// <summary>
    /// Computes the record hash under a specific canonical-serialization version, so a stored record is re-hashed under
    /// the version it was stamped with (the verifier passes <see cref="WormAuditChainRecord.CanonicalSerializationVersion"/>).
    /// This is what keeps pre-Story-9.2 (v1) chains verifiable after the <see cref="AuditEnvelope.ReplayRunId"/> bump.
    /// </summary>
    public static string ComputeRecordHash(AuditEnvelope envelope, string predecessorHash, long sequence, string canonicalVersion)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(predecessorHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalVersion);

        string input = string.Concat(
            CanonicalizeEnvelope(envelope, canonicalVersion),
            ComponentSeparator,
            predecessorHash,
            ComponentSeparator,
            sequence.ToString(CultureInfo.InvariantCulture));

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(digest);
    }

    /// <summary>
    /// Builds the canonical serialization of an envelope under the current version
    /// (<see cref="CanonicalSerializationVersion"/>).
    /// </summary>
    public static string CanonicalizeEnvelope(AuditEnvelope envelope)
        => CanonicalizeEnvelope(envelope, CanonicalSerializationVersion);

    /// <summary>
    /// Builds the canonical, stable serialization of an envelope (fixed field order, invariant culture, UTC timestamp)
    /// under a specific contract version. The envelope's <see cref="AuditEnvelope.PredecessorHash"/> is intentionally
    /// omitted — the chain's predecessor linkage is hashed separately via <see cref="ComputeRecordHash(AuditEnvelope, string, long)"/>.
    /// <para>
    /// Version-aware (Story 9.2): <see cref="CanonicalSerializationVersionV1"/> reproduces the original Story 9.1 field
    /// set exactly (no <see cref="AuditEnvelope.ReplayRunId"/>); v2 and later append the replay marker so it is covered
    /// by tamper-evidence. The marker is appended before <see cref="AuditEnvelope.SurfaceOrigin"/> stays the trailing
    /// field, so a v1 canonical form is a byte-for-byte prefix-compatible subset and re-verifies unchanged.
    /// </para>
    /// </summary>
    public static string CanonicalizeEnvelope(AuditEnvelope envelope, string canonicalVersion)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalVersion);

        StringBuilder builder = new();
        Append(builder, envelope.TenantId);
        Append(builder, envelope.ActorId);
        Append(builder, envelope.ActorType);
        Append(builder, envelope.CommandName);
        Append(builder, envelope.ResourceId);
        Append(builder, envelope.Decision);
        Append(builder, envelope.ReasonCode);
        Append(builder, envelope.CorrelationId);
        Append(builder, envelope.Timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture));
        Append(builder, envelope.PolicySnapshotId);

        // Source-evidence refs are order-significant and already AuditMetadata-safe tokens; join them with the unit
        // separator so a record's ref set is canonical and re-hashes byte-for-byte.
        Append(builder, string.Join(FieldSeparator, envelope.SourceEvidenceRefs));
        Append(builder, envelope.IdempotencyKey ?? string.Empty);
        Append(builder, envelope.StateTransition);
        Append(builder, envelope.RedactionDecision);
        Append(builder, envelope.Outcome);
        Append(builder, envelope.Phase.ToString());
        Append(builder, envelope.EnvelopeSchemaVersion);

        // v2+ (Story 9.2, FR95a): the replay marker is part of the hashed body so a replay record is tamper-evidently
        // distinct from a production record. v1 omits it entirely, reproducing the original Story 9.1 canonical form.
        if (!string.Equals(canonicalVersion, CanonicalSerializationVersionV1, StringComparison.Ordinal))
        {
            Append(builder, envelope.ReplayRunId ?? string.Empty);
        }

        builder.Append(envelope.SurfaceOrigin);

        return builder.ToString();
    }

    private static void Append(StringBuilder builder, string value)
    {
        builder.Append(value);
        builder.Append(FieldSeparator);
    }
}
