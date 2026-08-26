using System.Diagnostics;

using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Resolves the exact repository commit attributed to a live recovery marker.</summary>
internal static class RecoveryRepositoryCommitResolver
{
    /// <summary>Resolves the commit for the repository containing the running test assembly.</summary>
    /// <returns>A safe stable commit identifier.</returns>
    public static string Resolve()
        => Resolve(RepositoryRoot());

    /// <summary>Resolves the hosted event SHA or the current repository HEAD without inventing provenance.</summary>
    /// <param name="repositoryRoot">The repository whose commit is being exercised.</param>
    /// <returns>A safe stable commit identifier.</returns>
    public static string Resolve(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        string? ambient = Environment.GetEnvironmentVariable("GITHUB_SHA");
        if (!string.IsNullOrWhiteSpace(ambient) && AuditMetadata.IsSafeStableIdentifier(ambient))
        {
            return ambient;
        }

        try
        {
            using Process? process = Process.Start(new ProcessStartInfo("git", "rev-parse HEAD")
            {
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            if (process is not null)
            {
                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                Task<string> stderrTask = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(10_000))
                {
                    process.Kill(entireProcessTree: true);
                    throw new InvalidOperationException(
                        "git did not resolve the repository commit within ten seconds.");
                }

                Task.WaitAll(stdoutTask, stderrTask);
                string head = stdoutTask.Result.Trim();
                if (process.ExitCode == 0
                    && AuditMetadata.IsSafeStableIdentifier(head))
                {
                    return head;
                }
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // git is unavailable; fail closed below.
        }

        throw new InvalidOperationException(
            "The repository commit for live-recovery evidence could not be resolved from GITHUB_SHA or git.");
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}
