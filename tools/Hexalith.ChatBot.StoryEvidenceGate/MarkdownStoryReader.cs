using System.Text.RegularExpressions;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Reads the supported TE-spec and canonical BMAD product-story grammars without inferring identity.
/// </summary>
public static partial class MarkdownStoryReader
{
    /// <summary>Reads a story record from a Markdown file.</summary>
    /// <param name="path">The explicit story path.</param>
    /// <returns>The normalized story record.</returns>
    public static StoryRecord Read(string path)
    {
        if (!File.Exists(path))
        {
            throw new GateValidationException(GateReason.StatusMismatch, Path.GetFileName(path));
        }

        string[] lines = VisibleLines(File.ReadAllLines(path));
        bool canonical = lines.Any(static line => CanonicalTitle().IsMatch(line));
        string title = canonical
            ? SingleMatch(lines, CanonicalTitle(), "title")
            : Frontmatter(lines, "title");
        string status = canonical
            ? SingleMatch(lines, CanonicalStatus(), "status")
            : Frontmatter(lines, "status");
        HashSet<string> fileList = new(StringComparer.Ordinal);
        HashSet<string> checkedItems = new(StringComparer.Ordinal);
        HashSet<string> mandatoryItems = new(StringComparer.Ordinal);
        bool inFileList = false;
        bool inTasks = false;
        bool inAcceptance = false;
        bool sawFileList = false;
        bool sawTasks = false;
        bool sawAcceptance = false;
        bool inTeTasksAndAcceptance = false;
        bool sawTeTasksAndAcceptance = false;
        int taskIndex = 0;
        int acceptanceIndex = 0;

        foreach (string line in lines)
        {
            if ((!canonical && line.Equals("## File List", StringComparison.Ordinal))
                || (canonical && line.Equals("### File List", StringComparison.Ordinal)))
            {
                if (sawFileList)
                {
                    throw new GateValidationException(GateReason.FileListDiffMismatch, "file-list-section");
                }

                inFileList = true;
                inTasks = false;
                inAcceptance = false;
                sawFileList = true;
                continue;
            }

            if (!canonical && line.Equals("## Tasks & Acceptance", StringComparison.Ordinal))
            {
                if (sawTeTasksAndAcceptance)
                {
                    throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, "tasks-section");
                }

                inTeTasksAndAcceptance = true;
                sawTeTasksAndAcceptance = true;
                inFileList = false;
                continue;
            }

            if (!canonical && line.Equals("**Execution:**", StringComparison.Ordinal))
            {
                if (!inTeTasksAndAcceptance || sawTasks)
                {
                    throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, "execution-section");
                }

                inTasks = true;
                inAcceptance = false;
                sawTasks = true;
                continue;
            }

            if (!canonical && line.Equals("**Acceptance Criteria:**", StringComparison.Ordinal))
            {
                if (!inTeTasksAndAcceptance || sawAcceptance)
                {
                    throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, "acceptance-section");
                }

                inTasks = false;
                inAcceptance = true;
                sawAcceptance = true;
                continue;
            }

            if (canonical && line.Equals("## Tasks / Subtasks", StringComparison.Ordinal))
            {
                if (sawTasks)
                {
                    throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, "tasks-section");
                }

                inFileList = false;
                inTasks = true;
                inAcceptance = false;
                sawTasks = true;
                continue;
            }

            if (canonical && line.Equals("## Acceptance Criteria", StringComparison.Ordinal))
            {
                if (sawAcceptance)
                {
                    throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, "acceptance-section");
                }

                inFileList = false;
                inTasks = false;
                inAcceptance = true;
                sawAcceptance = true;
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal)
                || (inFileList && line.StartsWith("### ", StringComparison.Ordinal)))
            {
                if (!line.Equals("## Tasks & Acceptance", StringComparison.Ordinal))
                {
                    inTeTasksAndAcceptance = false;
                }

                inFileList = false;
                inTasks = false;
                inAcceptance = false;
            }

            if (inFileList && line.TrimStart().StartsWith("- ", StringComparison.Ordinal))
            {
                if (!TryFileListPath(line, out string filePath))
                {
                    throw new GateValidationException(GateReason.FileListDiffMismatch, "file-list-entry");
                }

                if (!fileList.Add(filePath))
                {
                    throw new GateValidationException(GateReason.FileListDiffMismatch, filePath);
                }
            }

            Match task = TaskCheckbox().Match(line);
            if (inTasks && task.Success)
            {
                string id = $"task-{++taskIndex}";
                mandatoryItems.Add(id);
                if (task.Groups[1].Value.Equals("x", StringComparison.OrdinalIgnoreCase))
                {
                    checkedItems.Add(id);
                }
            }

            bool acceptanceItem = canonical
                ? CanonicalAcceptance().IsMatch(line)
                : line.TrimStart().StartsWith("- Given ", StringComparison.Ordinal);
            if (inAcceptance && acceptanceItem)
            {
                string id = $"ac-{++acceptanceIndex}";
                mandatoryItems.Add(id);
                checkedItems.Add(id);
            }
        }

        if (!sawFileList || fileList.Count == 0)
        {
            throw new GateValidationException(GateReason.FileListDiffMismatch, "file-list-section");
        }

        if (!sawTasks || taskIndex == 0 || !sawAcceptance || acceptanceIndex == 0)
        {
            throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, "mandatory-sections");
        }

        return new StoryRecord(
            title,
            status,
            fileList,
            checkedItems,
            mandatoryItems,
            string.Join('\n', lines));
    }

    /// <summary>Reads an explicit supported status from Markdown text.</summary>
    /// <param name="text">The Markdown text.</param>
    /// <returns>The status, or an empty string when absent.</returns>
    public static string ReadStatus(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string[] lines = VisibleLines(text);
        Match[] canonical = lines.Select(static line => CanonicalStatus().Match(line)).Where(static match => match.Success).ToArray();
        if (canonical.Length > 1)
        {
            throw new GateValidationException(GateReason.StatusMismatch, "status");
        }

        return canonical.Length == 1 ? canonical[0].Groups[1].Value : Frontmatter(lines, "status", required: false);
    }

    private static string SingleMatch(string[] lines, Regex expression, string subject)
    {
        Match[] matches = lines.Select(line => expression.Match(line)).Where(static match => match.Success).ToArray();
        if (matches.Length != 1)
        {
            throw new GateValidationException(GateReason.StatusMismatch, subject);
        }

        return matches[0].Groups[1].Value.Trim();
    }

    private static string Frontmatter(string[] lines, string name, bool required = true)
    {
        if (lines.Length == 0 || !lines[0].Equals("---", StringComparison.Ordinal))
        {
            if (!required)
            {
                return string.Empty;
            }

            throw new GateValidationException(GateReason.StatusMismatch, name);
        }

        List<string> matches = [];
        for (int index = 1; index < lines.Length && !lines[index].Equals("---", StringComparison.Ordinal); index++)
        {
            Match match = FrontmatterField().Match(lines[index]);
            if (match.Success && match.Groups[1].Value.Equals(name, StringComparison.Ordinal))
            {
                matches.Add(match.Groups[2].Value.Trim().Trim('\'', '"'));
            }
        }

        if (matches.Count == 1)
        {
            return matches[0];
        }

        if (matches.Count > 1)
        {
            throw new GateValidationException(GateReason.StatusMismatch, name);
        }

        if (!required)
        {
            return string.Empty;
        }

        throw new GateValidationException(GateReason.StatusMismatch, name);
    }

    private static string[] VisibleLines(IEnumerable<string> source)
    {
        List<string> result = [];
        string? fence = null;
        foreach (string line in source)
        {
            string trimmed = line.TrimStart();
            string? marker = trimmed.StartsWith("```", StringComparison.Ordinal)
                ? "```"
                : trimmed.StartsWith("~~~", StringComparison.Ordinal) ? "~~~" : null;
            if (marker is not null)
            {
                if (fence is null)
                {
                    fence = marker;
                }
                else if (fence.Equals(marker, StringComparison.Ordinal))
                {
                    fence = null;
                }

                result.Add(string.Empty);
                continue;
            }

            result.Add(fence is null ? line : string.Empty);
        }

        return result.ToArray();
    }

    /// <summary>Removes fenced example content before structural Markdown parsing.</summary>
    internal static string[] VisibleLines(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return VisibleLines(text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'));
    }

    private static bool TryFileListPath(string line, out string path)
    {
        path = string.Empty;
        Match match = FileListEntry().Match(line);
        if (!match.Success)
        {
            return false;
        }

        string candidate = match.Groups[1].Value.Trim().Replace('\\', '/');
        if (candidate.Length == 0 || candidate.Contains('*', StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.FileListDiffMismatch, "file-list-entry");
        }

        path = candidate;
        return true;
    }

    [GeneratedRegex("^([A-Za-z][A-Za-z0-9_-]*):\\s*(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex FrontmatterField();

    [GeneratedRegex("^# (Story .+)$", RegexOptions.CultureInvariant)]
    private static partial Regex CanonicalTitle();

    [GeneratedRegex("^Status:\\s*(\\S+)\\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex CanonicalStatus();

    [GeneratedRegex("^\\s*\\d+\\.\\s+\\S", RegexOptions.CultureInvariant)]
    private static partial Regex CanonicalAcceptance();

    [GeneratedRegex("^\\s*- \\[([ xX])\\]\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex TaskCheckbox();

    [GeneratedRegex("^\\s*-\\s+`([^`]+)`(?:\\s+(?:\\([^)]*\\)|--\\s+.+))?\\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex FileListEntry();
}
