using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Writes bounded retention-failure markers to a root independent of the canonical evidence directory.</summary>
internal sealed class FileRecoveryValidationEvidenceRetentionFailureSink :
    IRecoveryValidationEvidenceRetentionFailureSink
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _markerRoot;

    /// <summary>Initializes a contained marker writer and rejects overlapping evidence and marker roots.</summary>
    public FileRecoveryValidationEvidenceRetentionFailureSink(
        string markerDirectory,
        string evidenceDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(markerDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceDirectory);
        if (!Path.IsPathFullyQualified(markerDirectory) || !Path.IsPathFullyQualified(evidenceDirectory))
        {
            throw new ArgumentException("Recovery evidence and retention-failure roots must be absolute paths.");
        }

        _markerRoot = Path.GetFullPath(markerDirectory);
        string evidenceRoot = Path.GetFullPath(evidenceDirectory);
        if (IsSameOrContained(_markerRoot, evidenceRoot) || IsSameOrContained(evidenceRoot, _markerRoot))
        {
            throw new ArgumentException(
                "The retention-failure root must be independent of the canonical evidence directory.");
        }
    }

    /// <inheritdoc />
    public async ValueTask RecordAsync(
        RecoveryValidationEvidenceRetentionFailureMarker marker,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(marker);
        if (!marker.IsValid())
        {
            throw new InvalidOperationException("The retention-failure marker failed bounded metadata validation.");
        }

        string safeJob = Sanitize(marker.JobId);
        string safeScenario = Sanitize(marker.Scenario);
        string markerPath = Path.GetFullPath(
            Path.Combine(_markerRoot, $"{safeJob}-{safeScenario}.retention-failure.json"));
        if (!IsSameOrContained(markerPath, _markerRoot) || string.Equals(markerPath, _markerRoot, PathComparison))
        {
            throw new InvalidOperationException("The retention-failure marker path escaped its configured root.");
        }

        string json = JsonSerializer.Serialize(marker, SerializerOptions);
        if (Utf8NoBom.GetByteCount(json) > RecoveryValidationEvidenceRetentionFailureMarker.MaximumSerializedBytes)
        {
            throw new InvalidOperationException("The retention-failure marker exceeded its serialized size bound.");
        }

        Directory.CreateDirectory(_markerRoot);
        string temporaryPath = $"{markerPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            // The temporary file is a sibling, so the final rename stays on one filesystem. A cancellation or process
            // interruption during the write cannot truncate a marker from an earlier attempt; only a complete file is
            // atomically moved over the deterministic destination.
            await File.WriteAllTextAsync(temporaryPath, json, Utf8NoBom, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, markerPath, overwrite: true);
        }
        finally
        {
            // Cleanup must never replace the real failure. An undeletable or raced sibling would otherwise surface as
            // the reported exception, hiding why the marker write failed in the first place.
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static StringComparison PathComparison
        => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static bool IsSameOrContained(string candidatePath, string rootPath)
    {
        string candidate = Path.GetFullPath(candidatePath);
        string root = Path.GetFullPath(rootPath);
        if (string.Equals(candidate, root, PathComparison))
        {
            return true;
        }

        string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return candidate.StartsWith(rootPrefix, PathComparison);
    }

    private static string Sanitize(string token)
    {
        if (!AuditMetadata.IsSafeStableIdentifier(token))
        {
            throw new InvalidOperationException("The retention-failure marker contained an unsafe filename token.");
        }

        return new string(token.Select(static character =>
            char.IsAsciiLetterOrDigit(character) || character is '-' or '_'
                ? character
                : '-').ToArray());
    }
}
