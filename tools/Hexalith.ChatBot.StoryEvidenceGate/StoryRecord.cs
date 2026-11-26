namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Represents normalized story metadata and mandatory evidence items.
/// </summary>
/// <param name="Title">The explicit frontmatter title.</param>
/// <param name="Status">The explicit frontmatter status.</param>
/// <param name="FileList">The normalized File List.</param>
/// <param name="CheckedItems">The checked task identifiers.</param>
/// <param name="MandatoryItems">All mandatory task and acceptance identifiers.</param>
/// <param name="EvidenceText">Fence-free text used for explicit claim-class matching.</param>
public sealed record StoryRecord(
    string Title,
    string Status,
    IReadOnlySet<string> FileList,
    IReadOnlySet<string> CheckedItems,
    IReadOnlySet<string> MandatoryItems,
    string EvidenceText);
