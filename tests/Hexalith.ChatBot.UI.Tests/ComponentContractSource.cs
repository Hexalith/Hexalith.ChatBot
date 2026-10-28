using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Reads production sources for component architecture contracts.</summary>
internal static class ComponentContractSource
{
    /// <summary>Reads a source file relative to the repository root.</summary>
    /// <param name="relativePath">The repository-relative file path.</param>
    /// <returns>The file contents.</returns>
    internal static string ReadProjectFile(string relativePath)
        => File.ReadAllText(ProjectPath(relativePath));

    /// <summary>Resolves a repository-relative path from the test output directory.</summary>
    /// <param name="relativePath">The repository-relative file path.</param>
    /// <returns>The absolute file path.</returns>
    internal static string ProjectPath(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return Path.Combine(directory.FullName, relativePath);
    }

    /// <summary>Counts non-overlapping ordinal occurrences of a source marker.</summary>
    /// <param name="value">The source text.</param>
    /// <param name="marker">The non-empty marker to count.</param>
    /// <returns>The number of occurrences.</returns>
    internal static int CountOccurrences(string value, string marker)
    {
        int count = 0;
        int startIndex = 0;
        while ((startIndex = value.IndexOf(marker, startIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += marker.Length;
        }

        return count;
    }

    /// <summary>
    /// Removes Razor and C# comments so a source-text assertion cannot be satisfied by prose describing the
    /// very markup it is meant to prove exists.
    /// </summary>
    /// <param name="source">The component source text.</param>
    /// <returns>The source without Razor or C# comments.</returns>
    internal static string StripComments(string source)
    {
        string withoutRazor = System.Text.RegularExpressions.Regex.Replace(
            source,
            @"@\*.*?\*@",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.Singleline);
        string withoutBlock = System.Text.RegularExpressions.Regex.Replace(
            withoutRazor,
            @"/\*.*?\*/",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.Singleline);
        return System.Text.RegularExpressions.Regex.Replace(
            withoutBlock,
            @"^\s*//.*$",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.Multiline);
    }
}
