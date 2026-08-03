using System.Diagnostics;
using System.Text;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Executes the gate's allowlisted read-only Git operations.
/// </summary>
public static class GitReader
{
    private static readonly IReadOnlySet<string> AllowedCommands = new HashSet<string>(StringComparer.Ordinal)
    {
        "diff",
        "cat-file",
        "ls-files",
        "ls-tree",
        "rev-parse",
        "show",
    };

    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(5);

    /// <summary>Resolves and verifies an exact full commit identifier.</summary>
    /// <param name="repositoryPath">The repository path.</param>
    /// <param name="revision">The supplied revision.</param>
    /// <returns>The canonical full revision.</returns>
    public static string ResolveExactCommit(string repositoryPath, string revision)
    {
        GitCommandResult result = Run(repositoryPath, "rev-parse", "--verify", $"{revision}^{{commit}}");
        string resolved = result.StandardOutput.Trim();
        if (result.ExitCode != 0
            || resolved.Length < 40
            || !resolved.Equals(revision, StringComparison.OrdinalIgnoreCase))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "revision");
        }

        return resolved.ToLowerInvariant();
    }

    /// <summary>Gets the current full HEAD revision.</summary>
    /// <param name="repositoryPath">The repository path.</param>
    /// <returns>The canonical revision.</returns>
    public static string Head(string repositoryPath)
    {
        GitCommandResult result = Run(repositoryPath, "rev-parse", "HEAD");
        if (result.ExitCode != 0)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "head");
        }

        return result.StandardOutput.Trim().ToLowerInvariant();
    }

    /// <summary>Gets changed path statuses between two revisions.</summary>
    /// <param name="repositoryPath">The repository path.</param>
    /// <param name="baseCommit">The exact base revision.</param>
    /// <param name="headCommit">The exact head revision.</param>
    /// <returns>Normalized status/path pairs.</returns>
    public static IReadOnlyDictionary<string, string> Diff(
        string repositoryPath,
        string baseCommit,
        string headCommit)
    {
        GitCommandResult result = Run(
            repositoryPath,
            "diff",
            "--name-status",
            "-z",
            "--no-renames",
            baseCommit,
            headCommit,
            "--");
        if (result.ExitCode != 0)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "diff");
        }

        return ParseNameStatus(result.StandardOutput);
    }

    /// <summary>Gets all index/worktree changes relative to a revision, including untracked files.</summary>
    /// <param name="repositoryPath">The repository path.</param>
    /// <param name="headCommit">The exact head revision.</param>
    /// <returns>Normalized status/path pairs.</returns>
    public static IReadOnlyDictionary<string, string> WorktreeDiff(string repositoryPath, string headCommit)
    {
        Dictionary<string, string> paths = new(Diff(repositoryPath, headCommit, Head(repositoryPath)), StringComparer.Ordinal);
        GitCommandResult tracked = Run(
            repositoryPath,
            "diff",
            "--name-status",
            "-z",
            "--no-renames",
            headCommit,
            "--");
        if (tracked.ExitCode != 0)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "worktree-diff");
        }

        foreach ((string path, string status) in ParseNameStatus(tracked.StandardOutput))
        {
            paths[path] = status;
        }

        GitCommandResult untracked = Run(repositoryPath, "ls-files", "--others", "--exclude-standard", "-z");
        if (untracked.ExitCode != 0)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "untracked-files");
        }

        foreach (string path in untracked.StandardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            paths[NormalizePath(path)] = "A";
        }

        return paths;
    }

    /// <summary>Reads a file as it existed at an exact revision.</summary>
    /// <param name="repositoryPath">The repository path.</param>
    /// <param name="revision">The exact revision.</param>
    /// <param name="path">The repository-relative path.</param>
    /// <returns>The file text, or null when absent.</returns>
    public static string? Show(string repositoryPath, string revision, string path)
    {
        GitCommandResult result = Run(repositoryPath, "show", $"{revision}:{path}");
        return result.ExitCode == 0 ? result.StandardOutput : null;
    }

    /// <summary>Reads the exact gitlink object at a path in a committed tree.</summary>
    /// <param name="repositoryPath">The root repository path.</param>
    /// <param name="revision">The exact tree revision.</param>
    /// <param name="path">The root-relative submodule path.</param>
    /// <returns>The full gitlink commit, or null when the path is absent or is not a gitlink.</returns>
    public static string? Gitlink(string repositoryPath, string revision, string path)
    {
        GitCommandResult result = Run(repositoryPath, "ls-tree", "-z", revision, "--", path);
        if (result.ExitCode != 0 || string.IsNullOrEmpty(result.StandardOutput))
        {
            return null;
        }

        string record = result.StandardOutput.TrimEnd('\0');
        int tab = record.IndexOf('\t');
        string[] metadata = (tab < 0 ? record : record[..tab]).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return metadata.Length == 3
            && metadata[0].Equals("160000", StringComparison.Ordinal)
            && metadata[1].Equals("commit", StringComparison.Ordinal)
            ? metadata[2].ToLowerInvariant()
            : null;
    }

    /// <summary>Reads an exact path entry from a committed tree.</summary>
    public static (string Mode, string ObjectId)? TreeEntry(string repositoryPath, string revision, string path)
    {
        GitCommandResult result = Run(repositoryPath, "ls-tree", "-z", revision, "--", path);
        if (result.ExitCode != 0 || string.IsNullOrEmpty(result.StandardOutput))
        {
            return null;
        }

        string record = result.StandardOutput.TrimEnd('\0');
        int tab = record.IndexOf('\t');
        string[] metadata = (tab < 0 ? record : record[..tab]).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return metadata.Length == 3 ? (metadata[0], metadata[2].ToLowerInvariant()) : null;
    }

    /// <summary>Reads exact blob bytes without checkout encoding or line-ending transformation.</summary>
    public static byte[] BlobBytes(string repositoryPath, string objectId)
    {
        ProcessStartInfo startInfo = StartInfo(repositoryPath);
        startInfo.ArgumentList.Add("cat-file");
        startInfo.ArgumentList.Add("blob");
        startInfo.ArgumentList.Add(objectId);
        try
        {
            using Process process = Process.Start(startInfo)
                ?? throw new GateValidationException(GateReason.ScopeDigestMismatch, "git-process");
            using MemoryStream output = new();
            process.StandardOutput.BaseStream.CopyTo(output);
            _ = process.StandardError.ReadToEnd();
            WaitForExitOrFail(process);
            if (process.ExitCode != 0)
            {
                throw new GateValidationException(GateReason.ScopeDigestMismatch, "git-blob");
            }

            return output.ToArray();
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception
            or IOException
            or UnauthorizedAccessException)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "git-process");
        }
    }

    /// <summary>Reads the exact index mode for a tracked path.</summary>
    public static string? IndexMode(string repositoryPath, string path)
    {
        GitCommandResult result = Run(repositoryPath, "ls-files", "--stage", "-z", "--", path);
        if (result.ExitCode != 0 || string.IsNullOrEmpty(result.StandardOutput))
        {
            return null;
        }

        string record = result.StandardOutput.TrimEnd('\0');
        int space = record.IndexOf(' ');
        return space > 0 ? record[..space] : null;
    }

    /// <summary>Runs one allowlisted Git command using ArgumentList.</summary>
    /// <param name="repositoryPath">The repository path.</param>
    /// <param name="arguments">The Git arguments.</param>
    /// <returns>The command result.</returns>
    public static GitCommandResult Run(string repositoryPath, params string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Length == 0 || !AllowedCommands.Contains(arguments[0]))
        {
            throw new InvalidOperationException("Only allowlisted read-only Git commands may execute.");
        }

        ProcessStartInfo startInfo = StartInfo(repositoryPath);
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using Process process = Process.Start(startInfo)
                ?? throw new GateValidationException(GateReason.ScopeDigestMismatch, "git-process");
            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            WaitForExitOrFail(process);
            return new GitCommandResult(process.ExitCode, standardOutput, standardError);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception
            or IOException
            or UnauthorizedAccessException)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "git-process");
        }
    }

    private static void WaitForExitOrFail(Process process)
    {
        if (!process.WaitForExit((int)CommandTimeout.TotalMilliseconds))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception exception) when (exception is InvalidOperationException
                or System.ComponentModel.Win32Exception
                or NotSupportedException)
            {
                // Best-effort kill; still fail closed on timeout.
            }

            throw new GateValidationException(GateReason.ScopeDigestMismatch, "git-timeout");
        }
    }

    private static ProcessStartInfo StartInfo(string repositoryPath) => new("git")
    {
        WorkingDirectory = repositoryPath,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8,
    };

    private static IReadOnlyDictionary<string, string> ParseNameStatus(string output)
    {
        string[] tokens = output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length % 2 != 0)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "diff-parse");
        }

        Dictionary<string, string> result = new(StringComparer.Ordinal);
        for (int index = 0; index < tokens.Length; index += 2)
        {
            result[NormalizePath(tokens[index + 1])] = tokens[index].Length == 0 ? "M" : tokens[index][..1];
        }

        return result;
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('/');
}
