using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests;

/// <summary>
/// Executes the release-safety shell boundaries against isolated repositories and stub test lanes.
/// </summary>
public static class ReleaseWorkflowSafetyTests
{
    private const int ProcessTimeoutMilliseconds = 30_000;
    private const int TestRunnerTimeoutMilliseconds = 300_000;
    private const int ProcessTerminationTimeoutMilliseconds = 5_000;

    /// <summary>
    /// Verifies that the advertised synthetic merge binds the exact base and head parents in order.
    /// </summary>
    [Fact]
    public static void PullRequestMergeWithAdvertisedParentsShouldPass()
    {
        string temporaryRoot = CreateTemporaryRoot("pull-request-merge-valid");
        try
        {
            (string baseSha, string headSha, string mergeSha) = CreateSyntheticMerge(temporaryRoot);

            (int exitCode, string standardOutput, string standardError) = RunScript(
                "verify-pull-request-merge.sh",
                temporaryRoot,
                null,
                mergeSha,
                baseSha,
                headSha);

            exitCode.ShouldBe(0, standardError);
            standardOutput.ShouldContain("Verified synthetic merge");
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that swapping the advertised merge parents fails closed.
    /// </summary>
    [Fact]
    public static void PullRequestMergeWithMismatchedParentsShouldFail()
    {
        string temporaryRoot = CreateTemporaryRoot("pull-request-merge-mismatch");
        try
        {
            (string baseSha, string headSha, string mergeSha) = CreateSyntheticMerge(temporaryRoot);

            (int exitCode, _, string standardError) = RunScript(
                "verify-pull-request-merge.sh",
                temporaryRoot,
                null,
                mergeSha,
                headSha,
                baseSha);

            exitCode.ShouldNotBe(0);
            standardError.ShouldContain("Synthetic merge parent mismatch");
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that a commit with only one parent cannot impersonate a synthetic pull-request merge.
    /// </summary>
    [Fact]
    public static void PullRequestMergeWithWrongParentCardinalityShouldFail()
    {
        string temporaryRoot = CreateTemporaryRoot("pull-request-merge-cardinality");
        try
        {
            InitializeRepository(temporaryRoot);
            string baseSha = CommitFile(temporaryRoot, "base.txt", "base\n", "test: create cardinality base");
            string oneParentSha = CommitFile(
                temporaryRoot,
                "one-parent.txt",
                "one parent\n",
                "test: create one-parent commit");

            (int exitCode, _, string standardError) = RunScript(
                "verify-pull-request-merge.sh",
                temporaryRoot,
                null,
                oneParentSha,
                baseSha,
                baseSha);

            exitCode.ShouldNotBe(0);
            standardError.ShouldContain("must have exactly two parents; found 1");
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that every discovered ordinary test lane is executed successfully.
    /// </summary>
    [Fact]
    public static void SuccessfulMergeTestLanesShouldAllExecute()
    {
        string temporaryRoot = CreateTemporaryRoot("merge-test-success");
        try
        {
            string testsRoot = Path.Combine(temporaryRoot, "tests");
            Directory.CreateDirectory(Path.Combine(testsRoot, "Alpha"));
            Directory.CreateDirectory(Path.Combine(testsRoot, "Beta"));
            string alphaProject = Path.Combine(testsRoot, "Alpha", "Alpha.Tests.csproj");
            string betaProject = Path.Combine(testsRoot, "Beta", "Beta.Tests.csproj");
            File.WriteAllText(alphaProject, "<Project />\n");
            File.WriteAllText(betaProject, "<Project />\n");
            (string stubPath, string logPath) = CreateDotnetStub(temporaryRoot);
            Dictionary<string, string> environment = MergeTestEnvironment(testsRoot, stubPath, logPath, "2");

            (int exitCode, string standardOutput, string standardError) = RunScript(
                "run-merge-test-lanes.sh",
                temporaryRoot,
                environment);

            exitCode.ShouldBe(0, standardError);
            standardOutput.ShouldContain("Executed 2 ordinary merge test lanes successfully");
            string[] invocations = File.ReadAllLines(logPath);
            string requiredArguments =
                "-m:1 --no-build --configuration Release -- RunConfiguration.TreatNoTestsAsError=true";
            invocations.Length.ShouldBe(2);
            invocations.Count(invocation => invocation == $"test {alphaProject} {requiredArguments}").ShouldBe(1);
            invocations.Count(invocation => invocation == $"test {betaProject} {requiredArguments}").ShouldBe(1);
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that a failing ordinary test command propagates its failure.
    /// </summary>
    [Fact]
    public static void FailingMergeTestLaneShouldFailTheBoundary()
    {
        string temporaryRoot = CreateTemporaryRoot("merge-test-failure");
        try
        {
            string testsRoot = Path.Combine(temporaryRoot, "tests");
            Directory.CreateDirectory(Path.Combine(testsRoot, "Failing"));
            File.WriteAllText(Path.Combine(testsRoot, "Failing", "Failing.Tests.csproj"), "<Project />\n");
            (string stubPath, string logPath) = CreateDotnetStub(temporaryRoot);
            Dictionary<string, string> environment = MergeTestEnvironment(testsRoot, stubPath, logPath, "1");
            environment["MERGE_TEST_FAIL_PATTERN"] = "Failing.Tests.csproj";

            (int exitCode, _, _) = RunScript(
                "run-merge-test-lanes.sh",
                temporaryRoot,
                environment);

            exitCode.ShouldBe(23);
            File.ReadAllLines(logPath).Length.ShouldBe(1);
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that discovering no ordinary test lanes cannot produce a green result.
    /// </summary>
    [Fact]
    public static void EmptyMergeTestLaneSetShouldFailClosed()
    {
        string temporaryRoot = CreateTemporaryRoot("merge-test-empty");
        try
        {
            string testsRoot = Path.Combine(temporaryRoot, "tests");
            Directory.CreateDirectory(testsRoot);
            (string stubPath, string logPath) = CreateDotnetStub(temporaryRoot);
            Dictionary<string, string> environment = MergeTestEnvironment(testsRoot, stubPath, logPath, "1");

            (int exitCode, _, string standardError) = RunScript(
                "run-merge-test-lanes.sh",
                temporaryRoot,
                environment);

            exitCode.ShouldNotBe(0);
            standardError.ShouldContain("No ordinary merge test lanes");
            File.Exists(logPath).ShouldBeFalse();
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that a nonempty but incomplete project discovery fails before executing any lane.
    /// </summary>
    [Fact]
    public static void PartialMergeTestLaneSetShouldFailClosed()
    {
        string temporaryRoot = CreateTemporaryRoot("merge-test-partial");
        try
        {
            string testsRoot = Path.Combine(temporaryRoot, "tests");
            Directory.CreateDirectory(Path.Combine(testsRoot, "Only"));
            File.WriteAllText(Path.Combine(testsRoot, "Only", "Only.Tests.csproj"), "<Project />\n");
            (string stubPath, string logPath) = CreateDotnetStub(temporaryRoot);
            Dictionary<string, string> environment = MergeTestEnvironment(testsRoot, stubPath, logPath, "2");

            (int exitCode, _, string standardError) = RunScript(
                "run-merge-test-lanes.sh",
                temporaryRoot,
                environment);

            exitCode.ShouldNotBe(0);
            standardError.ShouldContain("Expected 2 ordinary merge test lanes, discovered 1");
            File.Exists(logPath).ShouldBeFalse();
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that a failing discovery command is not masked by process substitution or sorting.
    /// </summary>
    [Fact]
    public static void MergeTestProjectDiscoveryFailureShouldPropagate()
    {
        string temporaryRoot = CreateTemporaryRoot("merge-test-discovery-failure");
        try
        {
            string testsRoot = Path.Combine(temporaryRoot, "tests");
            Directory.CreateDirectory(testsRoot);
            (string stubPath, string logPath) = CreateDotnetStub(temporaryRoot);
            string commandDirectory = Path.Combine(temporaryRoot, "commands");
            Directory.CreateDirectory(commandDirectory);
            string failingFind = Path.Combine(commandDirectory, "find");
            File.WriteAllText(failingFind, "#!/usr/bin/env bash\nprintf 'forced find failure\\n' >&2\nexit 29\n");
            MakeExecutable(temporaryRoot, failingFind);
            Dictionary<string, string> environment = MergeTestEnvironment(testsRoot, stubPath, logPath, "1");
            environment["PATH"] = string.Join(
                Path.PathSeparator,
                commandDirectory,
                Environment.GetEnvironmentVariable("PATH") ?? string.Empty);

            (int exitCode, _, string standardError) = RunScript(
                "run-merge-test-lanes.sh",
                temporaryRoot,
                environment);

            exitCode.ShouldNotBe(0);
            standardError.ShouldContain("forced find failure");
            standardError.ShouldContain("Unable to discover merge test projects");
            File.Exists(logPath).ShouldBeFalse();
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that the freshly fetched exact main head remains eligible for publication.
    /// </summary>
    [Fact]
    public static void CurrentMainShouldPublish()
    {
        string temporaryRoot = CreateTemporaryRoot("publication-current");
        try
        {
            (string repository, _, string currentSha) = CreatePublicationGraph(temporaryRoot);
            RunGit(repository, "checkout", "--detach", currentSha);

            (int exitCode, string standardOutput, string standardError, string decision, string shouldPublish) =
                RunPublicationGuard(repository, "main", currentSha);

            exitCode.ShouldBe(0, standardError);
            standardOutput.ShouldContain("freshly fetched remote main head");
            decision.ShouldBe("current");
            shouldPublish.ShouldBe("true");
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that publication blocks when the checkout does not match an otherwise valid validated SHA.
    /// </summary>
    [Fact]
    public static void CheckedOutHeadDifferentFromValidatedShaShouldFailClosed()
    {
        string temporaryRoot = CreateTemporaryRoot("publication-checkout-mismatch");
        try
        {
            (string repository, string ancestorSha, string currentSha) = CreatePublicationGraph(temporaryRoot);
            RunGit(repository, "checkout", "--detach", ancestorSha);

            (int exitCode, _, string standardError, string decision, string shouldPublish) =
                RunPublicationGuard(repository, "main", currentSha);

            exitCode.ShouldNotBe(0);
            standardError.ShouldContain("does not match validated SHA");
            decision.ShouldBe("blocked");
            shouldPublish.ShouldBe("false");
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that an older validated main commit succeeds as superseded without publication.
    /// </summary>
    [Fact]
    public static void AncestorMainShouldSucceedAsSuperseded()
    {
        string temporaryRoot = CreateTemporaryRoot("publication-ancestor");
        try
        {
            (string repository, string ancestorSha, _) = CreatePublicationGraph(temporaryRoot);
            RunGit(repository, "checkout", "--detach", ancestorSha);

            (int exitCode, string standardOutput, string standardError, string decision, string shouldPublish) =
                RunPublicationGuard(repository, "main", ancestorSha);

            exitCode.ShouldBe(0, standardError);
            standardOutput.ShouldContain("is superseded by remote main");
            decision.ShouldBe("superseded");
            shouldPublish.ShouldBe("false");
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that a validated commit divergent from remote main blocks publication.
    /// </summary>
    [Fact]
    public static void DivergentMainShouldFailClosed()
    {
        string temporaryRoot = CreateTemporaryRoot("publication-divergent");
        try
        {
            (string repository, string ancestorSha, _) = CreatePublicationGraph(temporaryRoot);
            RunGit(repository, "checkout", "--detach", ancestorSha);
            File.WriteAllText(Path.Combine(repository, "divergent.txt"), "divergent\n");
            RunGit(repository, "add", "divergent.txt");
            RunGit(repository, "commit", "-m", "test: create divergent publication commit");
            string divergentSha = RunGit(repository, "rev-parse", "HEAD").Trim();

            (int exitCode, _, string standardError, string decision, string shouldPublish) =
                RunPublicationGuard(repository, "main", divergentSha);

            exitCode.ShouldNotBe(0);
            standardError.ShouldContain("diverges from remote main");
            decision.ShouldBe("blocked");
            shouldPublish.ShouldBe("false");
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that a remote without a main branch blocks publication.
    /// </summary>
    [Fact]
    public static void MissingRemoteMainShouldFailClosed()
    {
        string temporaryRoot = CreateTemporaryRoot("publication-missing-main");
        try
        {
            string remote = Path.Combine(temporaryRoot, "remote.git");
            string repository = Path.Combine(temporaryRoot, "repository");
            Directory.CreateDirectory(repository);
            RunGit(temporaryRoot, "init", "--bare", remote);
            InitializeRepository(repository);
            string validatedSha = CommitFile(repository, "validated.txt", "validated\n", "test: create validated commit");
            RunGit(repository, "remote", "add", "origin", remote);

            (int exitCode, _, string standardError, string decision, string shouldPublish) =
                RunPublicationGuard(repository, "main", validatedSha);

            exitCode.ShouldNotBe(0);
            standardError.ShouldContain("Unable to fetch the current remote main");
            decision.ShouldBe("blocked");
            shouldPublish.ShouldBe("false");
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that a remote transport failure blocks publication.
    /// </summary>
    [Fact]
    public static void MainFetchFailureShouldFailClosed()
    {
        string temporaryRoot = CreateTemporaryRoot("publication-fetch-failure");
        try
        {
            string repository = Path.Combine(temporaryRoot, "repository");
            Directory.CreateDirectory(repository);
            InitializeRepository(repository);
            string validatedSha = CommitFile(repository, "validated.txt", "validated\n", "test: create validated commit");
            RunGit(repository, "remote", "add", "origin", Path.Combine(temporaryRoot, "unavailable.git"));

            (int exitCode, _, string standardError, string decision, string shouldPublish) =
                RunPublicationGuard(repository, "main", validatedSha);

            exitCode.ShouldNotBe(0);
            standardError.ShouldContain("Unable to fetch the current remote main");
            decision.ShouldBe("blocked");
            shouldPublish.ShouldBe("false");
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that a missing validated commit blocks publication before release.
    /// </summary>
    [Fact]
    public static void MissingValidatedCommitShouldFailClosed()
    {
        string temporaryRoot = CreateTemporaryRoot("publication-missing-validated");
        try
        {
            (string repository, _, _) = CreatePublicationGraph(temporaryRoot);

            (int exitCode, _, string standardError, string decision, string shouldPublish) =
                RunPublicationGuard(repository, "main", new string('f', 40));

            exitCode.ShouldNotBe(0);
            standardError.ShouldContain("is not an available commit");
            decision.ShouldBe("blocked");
            shouldPublish.ShouldBe("false");
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that configured prerelease branches bypass the main-head comparison unchanged.
    /// </summary>
    [Theory]
    [InlineData("next")]
    [InlineData("alpha")]
    [InlineData("beta")]
    public static void PrereleaseBranchesShouldRemainEligible(string branch)
    {
        string temporaryRoot = CreateTemporaryRoot("publication-prerelease");
        try
        {
            (int exitCode, string standardOutput, string standardError, string decision, string shouldPublish) =
                RunPublicationGuard(temporaryRoot, branch, new string('0', 40));

            exitCode.ShouldBe(0, standardError);
            standardOutput.ShouldContain("preserves semantic-release publication eligibility");
            decision.ShouldBe("prerelease");
            shouldPublish.ShouldBe("true");
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that the run-settings override the merge lanes ship is what makes a zero-test lane fail.
    /// </summary>
    /// <remarks>
    /// The ordinary lanes execute on the VSTest bridge, where a Microsoft.Testing.Platform switch such as
    /// <c>--minimum-expected-tests</c> is accepted and ignored, so a lane that discovers no tests exits zero and the
    /// required merge check goes green on a suite that ran nothing. This runs the real toolchain twice against this
    /// already-built assembly with a filter that matches nothing: once with the shipped override and once without.
    /// </remarks>
    [Fact]
    public static void MergeTestLaneRunSettingsShouldFailAZeroTestLane()
    {
        const string impossibleFilter =
            "FullyQualifiedName=Hexalith.ChatBot.Architecture.Tests.NoSuchClass.NoSuchTest";
        string repositoryRoot = RepositoryRoot();
        string assemblyPath = Assembly.GetExecutingAssembly().Location;
        assemblyPath.ShouldNotBeNullOrWhiteSpace();

        string laneScript = File.ReadAllText(
            Path.Combine(repositoryRoot, ".github", "scripts", "run-merge-test-lanes.sh"));
        laneScript.ShouldContain("-- RunConfiguration.TreatNoTestsAsError=true");

        (int guardedExitCode, string guardedOutput, string guardedError) = RunProcess(
            repositoryRoot,
            "dotnet",
            null,
            TestRunnerTimeoutMilliseconds,
            "test",
            assemblyPath,
            "--filter",
            impossibleFilter,
            "--",
            "RunConfiguration.TreatNoTestsAsError=true");
        string guardedLog = guardedOutput + guardedError;
        guardedLog.ShouldContain("No test matches the given testcase filter", customMessage: guardedLog);
        guardedExitCode.ShouldNotBe(0, guardedLog);

        (int unguardedExitCode, string unguardedOutput, string unguardedError) = RunProcess(
            repositoryRoot,
            "dotnet",
            null,
            TestRunnerTimeoutMilliseconds,
            "test",
            assemblyPath,
            "--filter",
            impossibleFilter);
        string unguardedLog = unguardedOutput + unguardedError;
        unguardedLog.ShouldContain("No test matches the given testcase filter", customMessage: unguardedLog);
        unguardedExitCode.ShouldBe(
            0,
            "the zero-test run must be green without the override, otherwise this scenario proves nothing about it: "
            + unguardedLog);
    }

    /// <summary>
    /// Verifies that a branch outside the configured release channels can never reach semantic-release.
    /// </summary>
    [Fact]
    public static void UnsupportedReleaseBranchShouldFailClosed()
    {
        string temporaryRoot = CreateTemporaryRoot("publication-unsupported-branch");
        try
        {
            (int exitCode, _, string standardError, string decision, string shouldPublish) =
                RunPublicationGuard(temporaryRoot, "feature/unsupported", new string('0', 40));

            exitCode.ShouldNotBe(0);
            standardError.ShouldContain("Unsupported release branch feature/unsupported");
            decision.ShouldBe("blocked");
            shouldPublish.ShouldBe("false");
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that an advertised pull-request revision that is not a full commit SHA stops the merge boundary.
    /// </summary>
    [Fact]
    public static void MalformedAdvertisedMergeRevisionShouldFailClosed()
    {
        string temporaryRoot = CreateTemporaryRoot("pull-request-merge-malformed");
        try
        {
            (string baseSha, string headSha, string mergeSha) = CreateSyntheticMerge(temporaryRoot);

            (int exitCode, _, string standardError) = RunScript(
                "verify-pull-request-merge.sh",
                temporaryRoot,
                null,
                mergeSha,
                baseSha[..12],
                headSha);

            exitCode.ShouldNotBe(0);
            standardError.ShouldContain("Base SHA must be a full 40-character commit SHA.");
            standardError.ShouldNotContain("must have exactly two parents");
            standardError.ShouldNotContain("parent mismatch");
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>
    /// Verifies that every branch the release configuration actually publishes from survives the guard.
    /// </summary>
    /// <remarks>
    /// The guard hardcodes its channel set, while the authoritative set lives in <c>.releaserc.json</c> and the
    /// release workflow's push triggers. Nothing links the three, so a channel added to the configuration would
    /// otherwise fall through to the guard's unsupported arm and turn every release on it into a red job, with no
    /// test pointing at the guard as the cause. This drives the guard once per configured channel instead.
    /// </remarks>
    [Fact]
    public static void EveryConfiguredReleaseChannelShouldRemainPublishable()
    {
        IReadOnlyList<string> configuredChannels = ConfiguredReleaseChannels();
        configuredChannels.ShouldContain("main");

        foreach (string branch in configuredChannels)
        {
            string temporaryRoot = CreateTemporaryRoot($"publication-channel-{branch.Replace('/', '-')}");
            try
            {
                string workingDirectory = temporaryRoot;
                string validatedSha = new('0', 40);
                if (branch == "main")
                {
                    // `main` is the only channel the guard compares against a remote head, so it needs a graph.
                    (string repository, _, string currentSha) = CreatePublicationGraph(temporaryRoot);
                    RunGit(repository, "checkout", "--detach", currentSha);
                    workingDirectory = repository;
                    validatedSha = currentSha;
                }

                (int exitCode, _, string standardError, string decision, string shouldPublish) =
                    RunPublicationGuard(workingDirectory, branch, validatedSha);

                exitCode.ShouldBe(0, $"configured release channel '{branch}' must not fail the guard: {standardError}");
                decision.ShouldNotBe("blocked", $"configured release channel '{branch}' must not be blocked");
                shouldPublish.ShouldBe("true", $"configured release channel '{branch}' must stay publishable");
                standardError.ShouldNotContain("Unsupported release branch");
            }
            finally
            {
                DeleteTemporaryRoot(temporaryRoot);
            }
        }
    }

    /// <summary>
    /// Reads the release channels declared by semantic-release and by the release workflow's push triggers.
    /// </summary>
    private static IReadOnlyList<string> ConfiguredReleaseChannels()
    {
        string repositoryRoot = RepositoryRoot();
        SortedSet<string> channels = new(StringComparer.Ordinal);

        using (JsonDocument configuration =
            JsonDocument.Parse(File.ReadAllText(Path.Combine(repositoryRoot, ".releaserc.json"))))
        {
            foreach (JsonElement branch in configuration.RootElement.GetProperty("branches").EnumerateArray())
            {
                string? name = branch.ValueKind == JsonValueKind.String
                    ? branch.GetString()
                    : branch.GetProperty("name").GetString();
                name.ShouldNotBeNullOrWhiteSpace();
                _ = channels.Add(name);
            }
        }

        string release = File.ReadAllText(
            Path.Combine(repositoryRoot, ".github", "workflows", "release.yml"));
        Match pushBranches = Regex.Match(release, @"(?ms)^on:\n.*?^  push:\n    branches:\n((?:      - \S+\n)+)");
        pushBranches.Success.ShouldBeTrue("release workflow must declare its push branches");
        foreach (Match trigger in Regex.Matches(pushBranches.Groups[1].Value, @"^      - (\S+)$", RegexOptions.Multiline))
        {
            _ = channels.Add(trigger.Groups[1].Value);
        }

        channels.Count.ShouldBeGreaterThan(0);
        return [.. channels];
    }

    private static (string BaseSha, string HeadSha, string MergeSha) CreateSyntheticMerge(string repository)
    {
        InitializeRepository(repository);
        string baseSha = CommitFile(repository, "base.txt", "base\n", "test: create merge base");
        RunGit(repository, "checkout", "-b", "feature");
        string headSha = CommitFile(repository, "head.txt", "head\n", "test: create pull request head");
        RunGit(repository, "checkout", "main");
        RunGit(repository, "merge", "--no-ff", "feature", "-m", "test: create synthetic merge");
        string mergeSha = RunGit(repository, "rev-parse", "HEAD").Trim();
        return (baseSha, headSha, mergeSha);
    }

    private static (string Repository, string AncestorSha, string CurrentSha) CreatePublicationGraph(string root)
    {
        string remote = Path.Combine(root, "remote.git");
        string repository = Path.Combine(root, "repository");
        Directory.CreateDirectory(repository);
        RunGit(root, "init", "--bare", remote);
        InitializeRepository(repository);
        string ancestorSha = CommitFile(repository, "history.txt", "ancestor\n", "test: create publication ancestor");
        RunGit(repository, "remote", "add", "origin", remote);
        RunGit(repository, "push", "-u", "origin", "main");
        string currentSha = CommitFile(repository, "history.txt", "current\n", "test: create current publication head");
        RunGit(repository, "push", "origin", "main");
        return (repository, ancestorSha, currentSha);
    }

    private static void InitializeRepository(string repository)
    {
        RunGit(repository, "init", "--initial-branch=main");
        RunGit(repository, "config", "user.email", "release-safety@example.invalid");
        RunGit(repository, "config", "user.name", "Release Workflow Safety Tests");
        RunGit(repository, "config", "commit.gpgsign", "false");
        RunGit(repository, "config", "core.hooksPath", Path.Combine(repository, ".no-hooks"));
    }

    private static string CommitFile(string repository, string relativePath, string contents, string message)
    {
        File.WriteAllText(Path.Combine(repository, relativePath), contents);
        RunGit(repository, "add", relativePath);
        RunGit(repository, "commit", "-m", message);
        return RunGit(repository, "rev-parse", "HEAD").Trim();
    }

    private static (string StubPath, string LogPath) CreateDotnetStub(string root)
    {
        string stubPath = Path.Combine(root, "dotnet-stub.sh");
        string logPath = Path.Combine(root, "dotnet-stub.log");
        File.WriteAllText(
            stubPath,
            "#!/usr/bin/env bash\n"
            + "set -euo pipefail\n"
            + "printf '%s\\n' \"$*\" >> \"${MERGE_TEST_STUB_LOG:?}\"\n"
            + "if [[ \"$*\" == *\"${MERGE_TEST_FAIL_PATTERN:-__never__}\"* ]]; then exit 23; fi\n");
        MakeExecutable(root, stubPath);
        return (stubPath, logPath);
    }

    private static void MakeExecutable(string workingDirectory, string path)
    {
        (int exitCode, _, string standardError) = RunProcess(workingDirectory, "/bin/chmod", null, "+x", path);
        exitCode.ShouldBe(0, standardError);
    }

    private static Dictionary<string, string> MergeTestEnvironment(
        string testsRoot,
        string stubPath,
        string logPath,
        string expectedLanes)
        => new(StringComparer.Ordinal)
        {
            ["MERGE_TEST_ROOT"] = testsRoot,
            ["MERGE_TEST_DOTNET"] = stubPath,
            ["MERGE_TEST_STUB_LOG"] = logPath,
            ["MERGE_TEST_EXPECTED_LANES"] = expectedLanes,
        };

    private static (int ExitCode, string StandardOutput, string StandardError, string Decision, string ShouldPublish)
        RunPublicationGuard(string workingDirectory, string branch, string validatedSha)
    {
        string outputPath = Path.Combine(workingDirectory, $"github-output-{Guid.NewGuid():N}.txt");
        Dictionary<string, string> environment = new(StringComparer.Ordinal)
        {
            ["GITHUB_OUTPUT"] = outputPath,
        };
        (int exitCode, string standardOutput, string standardError) = RunScript(
            "guard-main-publication.sh",
            workingDirectory,
            environment,
            branch,
            validatedSha);
        return (
            exitCode,
            standardOutput,
            standardError,
            ReadSingleOutput(outputPath, "publication_decision"),
            ReadSingleOutput(outputPath, "should_publish"));
    }

    /// <summary>
    /// Reads the single value published for a step-output key.
    /// </summary>
    /// <remarks>
    /// GitHub does not document how a repeated key in the step output file is resolved, so the guard must publish
    /// each key exactly once. Asserting the cardinality here keeps that contract from silently regressing into a
    /// last-write-wins assumption, where a first-write-wins runner would block every publication with a green job.
    /// </remarks>
    private static string ReadSingleOutput(string outputPath, string key)
    {
        File.Exists(outputPath).ShouldBeTrue(
            $"the guard must publish step outputs; no output file was written for '{key}'");
        string prefix = $"{key}=";
        string[] matches = File.ReadLines(outputPath)
            .Where(line => line.StartsWith(prefix, StringComparison.Ordinal))
            .ToArray();
        matches.Length.ShouldBe(1, $"step output '{key}' must be published exactly once");
        return matches[0][prefix.Length..];
    }

    private static (int ExitCode, string StandardOutput, string StandardError) RunScript(
        string scriptName,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        params string[] arguments)
    {
        string scriptPath = Path.Combine(RepositoryRoot(), ".github", "scripts", scriptName);
        return RunProcess(workingDirectory, "/usr/bin/env", environment, ["bash", scriptPath, .. arguments]);
    }

    private static string RunGit(string repository, params string[] arguments)
    {
        (int exitCode, string standardOutput, string standardError) =
            RunProcess(repository, "git", null, arguments);
        exitCode.ShouldBe(0, standardError);
        return standardOutput;
    }

    private static (int ExitCode, string StandardOutput, string StandardError) RunProcess(
        string workingDirectory,
        string fileName,
        IReadOnlyDictionary<string, string>? environment,
        params string[] arguments)
        => RunProcess(workingDirectory, fileName, environment, ProcessTimeoutMilliseconds, arguments);

    private static (int ExitCode, string StandardOutput, string StandardError) RunProcess(
        string workingDirectory,
        string fileName,
        IReadOnlyDictionary<string, string>? environment,
        int timeoutMilliseconds,
        params string[] arguments)
    {
        ProcessStartInfo startInfo = new(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // The scenarios must observe the scripts, not the developer's Git configuration: a global commit
        // signature, hook path, or credential prompt would otherwise stall or fail these runs for unrelated
        // reasons and burn the bounded wait below.
        startInfo.Environment["GIT_CONFIG_GLOBAL"] = "/dev/null";
        startInfo.Environment["GIT_CONFIG_SYSTEM"] = "/dev/null";
        startInfo.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";

        // The zero-test-lane scenario asserts on the runner's own diagnostic text, so the CLI language is pinned
        // rather than inherited from the developer's or runner's locale.
        startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
        if (environment is not null)
        {
            foreach ((string key, string value) in environment)
            {
                startInfo.Environment[key] = value;
            }
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {fileName}.");
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(timeoutMilliseconds))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process exited between the bounded wait and termination request.
            }

            _ = process.WaitForExit(ProcessTerminationTimeoutMilliseconds);
            _ = Task.WaitAll([standardOutput, standardError], ProcessTerminationTimeoutMilliseconds);
            throw new TimeoutException(
                $"Process {fileName} exceeded the {timeoutMilliseconds}-millisecond test timeout.");
        }

        if (!Task.WaitAll([standardOutput, standardError], ProcessTerminationTimeoutMilliseconds))
        {
            throw new TimeoutException($"Process {fileName} did not close its redirected output streams.");
        }

        return (process.ExitCode, standardOutput.Result, standardError.Result);
    }

    /// <summary>
    /// Removes a scenario's temporary repository without letting cleanup replace the scenario's own failure.
    /// </summary>
    /// <remarks>
    /// These roots hold live Git object stores. A delete that races a lingering Git process throws from the
    /// <c>finally</c> block, which would discard the in-flight assertion failure and report an unrelated
    /// <see cref="IOException"/> instead, hiding which boundary actually regressed.
    /// </remarks>
    private static void DeleteTemporaryRoot(string temporaryRoot)
    {
        try
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
        catch (IOException)
        {
            // Diagnostic residue only; never worth losing the scenario's verdict.
        }
        catch (UnauthorizedAccessException)
        {
            // Same rationale: cleanup is best effort.
        }
    }

    private static string CreateTemporaryRoot(string prefix)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
