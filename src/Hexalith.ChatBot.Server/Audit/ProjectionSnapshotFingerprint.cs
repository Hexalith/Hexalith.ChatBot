using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>Produces the bounded canonical fingerprint retained for projection rebuild snapshots.</summary>
internal static class ProjectionSnapshotFingerprint
{
    /// <summary>The algorithm/version token carried by every measurable projection manifest.</summary>
    public const string AlgorithmVersion = "sha256-length-framed-ordinal-v1";

    /// <summary>The maximum number of metadata-only resources retained in either snapshot.</summary>
    public const int MaximumResources = 2_048;

    /// <summary>Returns a lowercase SHA-256 over ordinally sorted, length-framed resource/token pairs.</summary>
    public static string Compute(IReadOnlyList<ProjectionResourceDigest> snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Count > MaximumResources)
        {
            throw new ArgumentOutOfRangeException(nameof(snapshot), "The projection snapshot exceeds the retained evidence ceiling.");
        }

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (ProjectionResourceDigest digest in snapshot.OrderBy(static item => item.ResourceId, StringComparer.Ordinal))
        {
            ArgumentNullException.ThrowIfNull(digest);
            AppendFramed(hash, digest.ResourceId);
            AppendFramed(hash, digest.StructuralStateToken);
        }

        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    /// <summary>Returns whether a token is a canonical lowercase SHA-256 value.</summary>
    public static bool IsCanonicalSha256(string? value)
        => value is { Length: 64 } && value.All(static character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static void AppendFramed(IncrementalHash hash, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        byte[] length = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
        hash.AppendData(length);
        hash.AppendData(bytes);
    }
}
