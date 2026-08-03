using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Describes machine-derived result counts for one lane.
/// </summary>
/// <param name="Lane">The contract lane identifier.</param>
/// <param name="PrimaryPathClass">The contract-bound primary path class, when this is a primary lane.</param>
/// <param name="Source">The exact current-run or retained source.</param>
/// <param name="Total">The total test count.</param>
/// <param name="Executed">The executed test count.</param>
/// <param name="Passed">The passing test count.</param>
/// <param name="Failed">The failing test count.</param>
/// <param name="Skipped">The skipped or not-executed test count.</param>
/// <param name="ArtifactLocator">The metadata-only artifact locator.</param>
/// <param name="ChecksumSha256">The TRX SHA-256 checksum.</param>
/// <param name="PassedTests">The names of passing test assertions.</param>
/// <param name="Selectors">The contract-bound result selectors.</param>
public sealed record LaneResult(
    string Lane,
    string? PrimaryPathClass,
    string Source,
    int Total,
    int Executed,
    int Passed,
    int Failed,
    int Skipped,
    string ArtifactLocator,
    string ChecksumSha256,
    [property: JsonIgnore] IReadOnlySet<string> PassedTests,
    [property: JsonIgnore] IReadOnlyList<string> Selectors);
