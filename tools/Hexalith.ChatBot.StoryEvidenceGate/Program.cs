using System.Text.Json;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Hosts the story-evidence integrity command-line gate.
/// </summary>
public static class Program
{
    /// <summary>Runs the command-line gate.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>Zero on success; non-zero on fail-closed validation.</returns>
    public static int Main(string[] args)
    {
        try
        {
            CommandArguments command = CommandArguments.Parse(args);
            string repositoryRoot = Path.GetFullPath(command.Optional("repository-root") ?? Environment.CurrentDirectory);
            return command.Command switch
            {
                "validate" => Validate(command, repositoryRoot),
                "attest" => Attest(command, repositoryRoot),
                "detect" => Detect(command, repositoryRoot),
                "ci" => RunCi(command, repositoryRoot),
                _ => throw new GateValidationException(GateReason.StatusMismatch, "command"),
            };
        }
        catch (GateValidationException exception)
        {
            GateReport report = new()
            {
                Passed = false,
                EvaluatedAtUtc = DateTimeOffset.UtcNow,
                Issues = [GateIssue.Create(exception.ReasonCode, exception.Subject)],
            };
            Console.Out.WriteLine(JsonReportWriter.Serialize(report));
            return 2;
        }
        catch (Exception exception) when (exception is IOException
            or ArgumentException
            or UnauthorizedAccessException
            or NotSupportedException
            or InvalidOperationException
            or OutOfMemoryException
            or System.Security.SecurityException
            or System.ComponentModel.Win32Exception)
        {
            GateReport report = new()
            {
                Passed = false,
                EvaluatedAtUtc = DateTimeOffset.UtcNow,
                Issues = [GateIssue.Create(GateReason.ScopeDigestMismatch, "io-or-process")],
            };
            Console.Out.WriteLine(JsonReportWriter.Serialize(report));
            return 2;
        }
    }

    private static int Validate(CommandArguments command, string repositoryRoot)
    {
        string storyPath = FullPath(repositoryRoot, command.Required("story"));
        string contractPath = FullPath(repositoryRoot, command.Required("contract"));
        string resultsRoot = FullPath(repositoryRoot, command.Required("results"));
        GateOptions options = new()
        {
            RepositoryRoot = repositoryRoot,
            PolicyPath = FullPath(repositoryRoot, command.Optional("policy") ?? "story-evidence-policy.json"),
            StoryPath = storyPath,
            ContractPath = contractPath,
            TargetStatus = command.Required("target-status"),
            BaseCommit = command.Required("base").ToLowerInvariant(),
            HeadCommit = command.Required("head").ToLowerInvariant(),
            ResultsRoot = resultsRoot,
            ReportPath = command.Optional("report") is string report ? FullPath(repositoryRoot, report) : null,
        };
        GateReport result = StoryEvidenceValidator.Validate(options);
        Console.Out.WriteLine(JsonReportWriter.Serialize(result));
        return result.Passed ? 0 : 1;
    }

    private static int Attest(CommandArguments command, string repositoryRoot)
    {
        ProvenanceAttestor.AttestContract(
            repositoryRoot,
            FullPath(repositoryRoot, command.Required("contract")),
            command.Required("base").ToLowerInvariant(),
            command.Required("head").ToLowerInvariant(),
            FullPath(repositoryRoot, command.Required("results")),
            DateTimeOffset.UtcNow,
            FullPath(repositoryRoot, command.Optional("policy") ?? "story-evidence-policy.json"));
        Console.Out.WriteLine("{\"passed\":true,\"operation\":\"attest\"}");
        return 0;
    }

    private static int Detect(CommandArguments command, string repositoryRoot)
    {
        IReadOnlyList<TransitionRecord> transitions = TransitionDetector.Detect(
            repositoryRoot,
            command.Required("base").ToLowerInvariant(),
            command.Required("head").ToLowerInvariant());
        string json = JsonSerializer.Serialize(transitions, JsonReportWriter.SerializerOptions);
        if (command.Optional("output") is string output)
        {
            string path = FullPath(repositoryRoot, output);
            Directory.CreateDirectory(Path.GetDirectoryName(path)
                ?? throw new InvalidOperationException("Transition output has no parent directory."));
            File.WriteAllText(path, json);
        }

        Console.Out.WriteLine(json);
        return 0;
    }

    private static int RunCi(CommandArguments command, string repositoryRoot)
    {
        string baseCommit = command.Required("base").ToLowerInvariant();
        string headCommit = command.Required("head").ToLowerInvariant();
        string resultsRoot = FullPath(repositoryRoot, command.Required("results"));
        string reportDirectory = FullPath(repositoryRoot, command.Required("report-directory"));
        IReadOnlyList<TransitionRecord> transitions = TransitionDetector.Detect(repositoryRoot, baseCommit, headCommit);
        Directory.CreateDirectory(reportDirectory);
        if (transitions.Count == 0)
        {
            GateReport noTransition = new()
            {
                StoryKey = "no-transition",
                BaseCommit = baseCommit,
                HeadCommit = headCommit,
                PolicyVersion = "1.0",
                Passed = true,
                EvaluatedAtUtc = DateTimeOffset.UtcNow,
            };
            JsonReportWriter.Write(Path.Combine(reportDirectory, "no-transition.json"), noTransition);
            WriteCiSummary([noTransition]);
            Console.Out.WriteLine(JsonReportWriter.Serialize(noTransition));
            return 0;
        }

        string policyPath = FullPath(repositoryRoot, command.Optional("policy") ?? "story-evidence-policy.json");
        bool passed = true;
        List<GateReport> reports = [];
        foreach (TransitionRecord transition in transitions)
        {
            ProvenanceAttestor.AttestContract(
                repositoryRoot,
                transition.ContractPath,
                baseCommit,
                headCommit,
                resultsRoot,
                DateTimeOffset.UtcNow,
                policyPath);
            GateReport report = StoryEvidenceValidator.Validate(new GateOptions
            {
                RepositoryRoot = repositoryRoot,
                PolicyPath = policyPath,
                StoryPath = Path.Combine(repositoryRoot, transition.StoryPath),
                ContractPath = transition.ContractPath,
                TargetStatus = "done",
                BaseCommit = baseCommit,
                HeadCommit = headCommit,
                ResultsRoot = resultsRoot,
                ReportPath = Path.Combine(reportDirectory, $"{transition.StoryKey}.json"),
            });
            reports.Add(report);
            passed &= report.Passed;
            Console.Out.WriteLine(JsonReportWriter.Serialize(report));
        }

        WriteCiSummary(reports);
        return passed ? 0 : 1;
    }

    private static void WriteCiSummary(IReadOnlyList<GateReport> reports)
    {
        string? summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (string.IsNullOrWhiteSpace(summaryPath))
        {
            return;
        }

        IEnumerable<string> rows = reports.Select(report =>
            $"| `{report.StoryKey}` | {(report.Passed ? "pass" : "fail")} | {report.FileListCount} | {report.ScopedDiffCount} | "
            + $"{report.Lanes.Sum(static lane => lane.Executed)}/{report.Lanes.Sum(static lane => lane.Passed)} | `{report.BaseCommit}` | `{report.HeadCommit}` |");
        string markdown = "## Story-evidence integrity\n\n"
            + "| Record | Verdict | File List | Scoped diff | Tests executed/passed | Base | Head |\n"
            + "| --- | --- | ---: | ---: | ---: | --- | --- |\n"
            + string.Join("\n", rows)
            + "\n";
        File.AppendAllText(summaryPath, markdown);
    }

    private static string FullPath(string root, string path) =>
        Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(root, path));
}
