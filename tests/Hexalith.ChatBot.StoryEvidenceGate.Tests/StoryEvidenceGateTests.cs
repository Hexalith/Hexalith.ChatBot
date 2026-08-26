using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

using Hexalith.ChatBot.StoryEvidenceGate;

using Shouldly;

namespace Hexalith.ChatBot.StoryEvidenceGate.Tests;

/// <summary>
/// Exercises the positive, negative, mutation, primary-path, and bootstrap evidence matrix.
/// </summary>
public static class StoryEvidenceGateTests
{
    /// <summary>Proves the TE-2 prospective bootstrap path.</summary>
    [Fact]
    public static void ValidTechnicalEnablerBootstrapShouldPass()
    {
        using GateFixture fixture = new();

        GateReport report = fixture.Validate();

        report.Passed.ShouldBeTrue();
        report.Issues.ShouldBeEmpty();
        report.Lanes.Single().Total.ShouldBe(1);
        report.CheckedItemCount.ShouldBe(3);
    }

    /// <summary>Proves a clean v2 bootstrap hashes every owned HEAD snapshot path.</summary>
    [Fact]
    public static void ValidSnapshotBootstrapShouldPassWithoutTerminalMutation()
    {
        using GateFixture fixture = new();
        fixture.UseSnapshotBootstrap();

        GateReport report = fixture.Validate();

        report.Passed.ShouldBeTrue(string.Join(", ", report.Issues.Select(static issue => $"{issue.ReasonCode}:{issue.Subject}")));
        report.ScopedDiffCount.ShouldBe(6);
        report.EventPathCount.ShouldBeGreaterThan(0);
    }

    /// <summary>Proves snapshot evaluation rejects index or worktree drift from exact HEAD.</summary>
    [Fact]
    public static void SnapshotWorktreeDriftShouldFailWithScopeReason()
    {
        using GateFixture fixture = new();
        fixture.UseSnapshotBootstrap();
        File.AppendAllText(fixture.SourcePath, "drift\n");

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
    }

    /// <summary>Proves a clean committed bootstrap event cannot carry a path outside snapshot ownership.</summary>
    [Fact]
    public static void SnapshotBootstrapWithCommittedUnownedPathShouldFailWithScopeReason()
    {
        using GateFixture fixture = new();
        fixture.UseSnapshotBootstrap();
        fixture.CommitUnownedSnapshotEvent();

        GateIssue issue = fixture.Validate().Issues.Single();

        issue.ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
        issue.Subject.ShouldBe("root:unowned-bootstrap-event.txt");
    }

    /// <summary>Proves snapshot transition declarations cannot name paths outside root ownership.</summary>
    [Fact]
    public static void SnapshotTransitionPathOutsideRootOwnershipShouldFailClosed()
    {
        using GateFixture fixture = new();
        fixture.UseSnapshotBootstrap();
        fixture.MutateContract(contract =>
        {
            JsonArray paths = EvidenceJson.RequiredArray(
                EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch),
                "transitionPaths",
                GateReason.ScopeDigestMismatch);
            paths.Add("unowned-transition.txt");
        }, refreshEvidence: false);

        GateIssue issue = fixture.Validate().Issues.Single();

        issue.ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
        issue.Subject.ShouldBe("transition-path-ownership");
    }

    /// <summary>Proves the exact four-path delayed completion event passes independently of the snapshot.</summary>
    [Fact]
    public static void ExactSnapshotLifecycleCompletionShouldPass()
    {
        using GateFixture fixture = new();
        fixture.UseSnapshotCompletion();

        GateReport report = fixture.Validate();

        report.Passed.ShouldBeTrue(string.Join(", ", report.Issues.Select(static issue => $"{issue.ReasonCode}:{issue.Subject}")));
        report.EventPathCount.ShouldBe(4);
        report.ScopedDiffCount.ShouldBe(6);
    }

    /// <summary>Proves a fifth event path cannot be hidden by the complete HEAD snapshot.</summary>
    [Fact]
    public static void SnapshotCompletionWithExtraEventPathShouldFailDeterministically()
    {
        using GateFixture fixture = new();
        fixture.UseSnapshotCompletion(() => File.AppendAllText(fixture.SourcePath, "unauthorized\n"));

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.StatusMismatch);
    }

    /// <summary>Proves all story bytes other than the exact status value, including frozen intent, remain immutable.</summary>
    [Fact]
    public static void SnapshotCompletionWithStoryBodyMutationShouldFailDeterministically()
    {
        using GateFixture fixture = new();
        fixture.UseSnapshotCompletion(() => File.AppendAllText(fixture.StoryPath, "\nunauthorized intent mutation\n"));

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.StatusMismatch);
    }

    /// <summary>Proves unrelated ledger bytes cannot ride beside the authorized TE status mutation.</summary>
    [Fact]
    public static void SnapshotCompletionWithUnrelatedLedgerMutationShouldFailDeterministically()
    {
        using GateFixture fixture = new();
        string ledgerPath = Path.Combine(
            fixture.RepositoryRoot,
            "_bmad-output",
            "planning-artifacts",
            "technical-enablers.md");
        fixture.UseSnapshotCompletion(() => File.AppendAllText(ledgerPath, "\nunauthorized ledger mutation\n"));

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.StatusMismatch);
    }

    /// <summary>Proves unrelated sprint bytes cannot ride beside the authorized action status mutation.</summary>
    [Fact]
    public static void SnapshotCompletionWithUnrelatedSprintMutationShouldFailDeterministically()
    {
        using GateFixture fixture = new();
        fixture.UseSnapshotCompletion(() => File.AppendAllText(fixture.SprintPath, "\nunauthorized sprint mutation\n"));

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.StatusMismatch);
    }

    /// <summary>Proves the completion comparator requires the base digest token to be a JSON string.</summary>
    [Fact]
    public static void SnapshotCompletionWithNonStringBaseDigestShouldFailContractTransition()
    {
        using GateFixture fixture = new();
        fixture.UseSnapshotCompletionWithBaseContractMutation(contract =>
            EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch)["implementationDigest"] = 42);

        GateIssue issue = fixture.Validate().Issues.Single();

        issue.ReasonCode.ShouldBe(GateReason.StatusMismatch);
        issue.Subject.ShouldBe("implementationDigest");
    }

    /// <summary>Proves completion cannot change any contract field beyond bootstrap and the digest.</summary>
    [Fact]
    public static void SnapshotCompletionWithExtraContractMutationShouldFailContractTransition()
    {
        using GateFixture fixture = new();
        fixture.UseSnapshotCompletion(() => fixture.MutateContract(contract =>
            EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!
                .AsObject()["allowSkipped"] = true, refreshEvidence: false));

        GateIssue issue = fixture.Validate().Issues.Single();

        issue.ReasonCode.ShouldBe(GateReason.StatusMismatch);
        issue.Subject.ShouldBe("contract-transition");
    }

    /// <summary>Proves diff-mode primary triggers include working-tree-only owned changes.</summary>
    [Fact]
    public static void DiffModeWorkingTreeOnlyPrimaryTriggerShouldRequireAndAcceptBoundLane()
    {
        using (GateFixture negative = new())
        {
            negative.AddOwnedFile("src/Pages/WorkingTreeOnly.razor", "<p>working tree</p>\n");
            GateReport report = negative.Validate();
            report.EventPathCount.ShouldBe(0);
            report.Issues.Single().ReasonCode.ShouldBe(GateReason.PrimaryPathNotExecuted);
        }

        using GateFixture positive = new();
        positive.UseBrowserPrimaryPath("browser-primary");
        GateReport accepted = positive.Validate();
        accepted.EventPathCount.ShouldBe(0);
        accepted.Passed.ShouldBeTrue();
    }

    /// <summary>Proves Git stdout and stderr are drained concurrently instead of pipe-blocking the gate.</summary>
    [Fact]
    public static void GitReaderShouldDrainBothRedirectedStreamsConcurrently()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"git-streams-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            RunGit(temporaryRoot, "init", "--initial-branch=main");
            RunGit(temporaryRoot, "config", "user.email", "gate@example.invalid");
            RunGit(temporaryRoot, "config", "user.name", "Story Evidence Gate Tests");
            string driver = Path.Combine(temporaryRoot, "noisy-textconv.sh");
            File.WriteAllText(
                driver,
                "#!/bin/sh\ni=0\nwhile [ \"$i\" -lt 20000 ]; do\n  printf 'stdout-0123456789abcdef\\n'\n  printf 'stderr-0123456789abcdef\\n' >&2\n  i=$((i + 1))\ndone\n");
            File.SetUnixFileMode(driver, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            File.WriteAllText(Path.Combine(temporaryRoot, ".gitattributes"), "data.txt diff=noisy\n");
            File.WriteAllText(Path.Combine(temporaryRoot, "data.txt"), "base\n");
            RunGit(temporaryRoot, "config", "diff.noisy.textconv", driver);
            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: create stream fixture");
            string baseCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();
            File.WriteAllText(Path.Combine(temporaryRoot, "data.txt"), "head\n");
            RunGit(temporaryRoot, "add", "data.txt");
            RunGit(temporaryRoot, "commit", "-m", "test: change stream fixture");
            string headCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();

            GitCommandResult result = GitReader.Run(temporaryRoot, "diff", baseCommit, headCommit, "--", "data.txt");

            result.ExitCode.ShouldBe(0);
            result.StandardError.Length.ShouldBeGreaterThan(64 * 1024);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Proves a hung allowlisted Git operation is killed and fails with the stable timeout subject.</summary>
    [Fact]
    public static void HungAllowlistedGitOperationShouldFailWithinInjectedTimeout()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"git-timeout-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            RunGit(temporaryRoot, "init", "--initial-branch=main");
            RunGit(temporaryRoot, "config", "user.email", "gate@example.invalid");
            RunGit(temporaryRoot, "config", "user.name", "Story Evidence Gate Tests");
            string driver = Path.Combine(temporaryRoot, "hung-textconv.sh");
            File.WriteAllText(driver, "#!/bin/sh\nwhile :; do sleep 1; done\n");
            File.SetUnixFileMode(driver, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            File.WriteAllText(Path.Combine(temporaryRoot, ".gitattributes"), "data.txt diff=hung\n");
            File.WriteAllText(Path.Combine(temporaryRoot, "data.txt"), "base\n");
            RunGit(temporaryRoot, "config", "diff.hung.textconv", driver);
            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: create timeout fixture");
            string baseCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();
            File.WriteAllText(Path.Combine(temporaryRoot, "data.txt"), "head\n");
            RunGit(temporaryRoot, "add", "data.txt");
            RunGit(temporaryRoot, "commit", "-m", "test: change timeout fixture");
            string headCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();
            Stopwatch stopwatch = Stopwatch.StartNew();

            GateValidationException exception = Should.Throw<GateValidationException>(() =>
                GitReader.RunWithTimeout(
                    temporaryRoot,
                    TimeSpan.FromMilliseconds(250),
                    "diff",
                    baseCommit,
                    headCommit,
                    "--",
                    "data.txt"));
            stopwatch.Stop();

            exception.ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
            exception.Subject.ShouldBe("git-timeout");
            stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(5));
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Proves legal Linux backslash filenames cannot alias slash-normalized evidence paths.</summary>
    [Fact]
    public static void GitReportedBackslashPathShouldFailClosed()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"git-backslash-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            RunGit(temporaryRoot, "init", "--initial-branch=main");
            RunGit(temporaryRoot, "config", "user.email", "gate@example.invalid");
            RunGit(temporaryRoot, "config", "user.name", "Story Evidence Gate Tests");
            File.WriteAllText(Path.Combine(temporaryRoot, "baseline.txt"), "baseline\n");
            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: create path fixture");
            string headCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();
            File.WriteAllText(Path.Combine(temporaryRoot, "unsafe\\path.txt"), "unsafe\n");

            GateValidationException exception = Should.Throw<GateValidationException>(() =>
                GitReader.WorktreeDiff(temporaryRoot, headCommit));

            exception.ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
            exception.Subject.ShouldBe("git-path");
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Proves the repository's exact in-review spelling is an accepted TE bootstrap state.</summary>
    [Fact]
    public static void TechnicalEnablerBootstrapInReviewShouldPass()
    {
        using GateFixture fixture = new();
        fixture.SetBootstrapStoryStatus("in-review");

        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves a valid product story transition passes in local current-run scope.</summary>
    [Fact]
    public static void ValidProductStoryCompletionShouldPass()
    {
        using GateFixture fixture = new();
        fixture.UseProductCompletion();

        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves the canonical BMAD product-story grammar and File List suffixes pass.</summary>
    [Fact]
    public static void ValidCanonicalBmadProductStoryShouldPass()
    {
        using GateFixture fixture = new();
        fixture.UseCanonicalProductCompletion();

        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves normal technical-enabler completion requires the explicit terminal records.</summary>
    [Fact]
    public static void ValidTechnicalEnablerCompletionShouldPass()
    {
        using GateFixture fixture = new();
        fixture.UseTechnicalEnablerCompletion();

        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves the same exact story scope passes as an immutable base/head commit diff.</summary>
    [Fact]
    public static void ValidImmutableProductStoryCompletionShouldPass()
    {
        using GateFixture fixture = new();
        fixture.UseProductCompletion();
        fixture.CommitOwnedCompletion();

        GateReport report = fixture.Validate();
        report.Passed.ShouldBeTrue(string.Join(", ", report.Issues.Select(static issue => $"{issue.ReasonCode}:{issue.Subject}")));
    }

    /// <summary>Proves immutable scope rejects owned-file drift after its exact head is committed.</summary>
    [Fact]
    public static void DirtyOwnedPathAfterImmutableCommitShouldFailWithScopeReason()
    {
        using GateFixture fixture = new();
        fixture.UseProductCompletion();
        fixture.CommitStrictImmutableCompletion();
        File.AppendAllText(fixture.SourcePath, "dirty after immutable head\n");

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
    }

    /// <summary>Proves that reports remain metadata-only.</summary>
    [Fact]
    public static void SuccessReportShouldContainOnlyMetadata()
    {
        using GateFixture fixture = new();

        string json = JsonReportWriter.Serialize(fixture.Validate());

        json.ShouldNotContain("payload", Case.Insensitive);
        json.ShouldNotContain("password", Case.Insensitive);
        json.ShouldNotContain("prompt", Case.Insensitive);
        json.ShouldContain("implementationDigest");
    }

    /// <summary>Proves exact title/status identity reconciliation.</summary>
    [Fact]
    public static void WrongStoryIdentityShouldFailWithStatusReason()
    {
        using GateFixture fixture = new();
        fixture.MutateContract(contract => contract["storyTitle"] = "Another Story", refreshEvidence: false);

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.StatusMismatch);
    }

    /// <summary>Proves a story cannot transition to done when its sprint entry was already done at base.</summary>
    [Fact]
    public static void ProductStoryWithDoneSprintStatusAtBaseShouldFailWithStatusReason()
    {
        using GateFixture fixture = new();
        fixture.UseProductCompletionWithDoneSprintAtBase();

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.StatusMismatch);
    }

    /// <summary>Proves both exact base records must exist and equal review for a product transition.</summary>
    [Theory]
    [InlineData("in-progress", "review")]
    [InlineData(null, "review")]
    [InlineData("review", "in-progress")]
    [InlineData("review", null)]
    public static void ProductCompletionWithoutExactReviewBaseShouldFail(string? storyStatus, string? sprintStatus)
    {
        using GateFixture fixture = new();
        fixture.UseProductCompletionFromBase(storyStatus, sprintStatus);

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.StatusMismatch);
    }

    /// <summary>Proves the technical-enabler planning record must remain in review during bootstrap.</summary>
    [Fact]
    public static void WrongTechnicalEnablerLedgerStatusShouldFailWithStatusReason()
    {
        using GateFixture fixture = new();
        string ledger = Path.Combine(fixture.RepositoryRoot, "_bmad-output", "planning-artifacts", "technical-enablers.md");
        File.WriteAllText(ledger, File.ReadAllText(ledger).Replace("review;", "planned;", StringComparison.Ordinal));

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.StatusMismatch);
    }

    /// <summary>Proves missing File List entries fail closed.</summary>
    [Fact]
    public static void MissingFileListEntryShouldFailWithFileListReason()
    {
        using GateFixture fixture = new();
        File.WriteAllLines(
            fixture.StoryPath,
            File.ReadAllLines(fixture.StoryPath).Where(static line => !line.Equals("- `src/gate.txt`", StringComparison.Ordinal)));

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.FileListDiffMismatch);
    }

    /// <summary>Proves extra or nonexistent File List entries fail closed.</summary>
    [Fact]
    public static void ExtraFileListEntryShouldFailWithFileListReason()
    {
        using GateFixture fixture = new();
        File.AppendAllText(fixture.StoryPath, "\n- `src/does-not-exist.txt`\n");

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.FileListDiffMismatch);
    }

    /// <summary>Proves a declared path deleted at prospective head cannot satisfy the File List.</summary>
    [Fact]
    public static void DeletedFileListPathShouldFailWithFileListReason()
    {
        using GateFixture fixture = new();
        fixture.DeleteOwnedSource();

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.FileListDiffMismatch);
    }

    /// <summary>Proves duplicate File List entries fail closed.</summary>
    [Fact]
    public static void DuplicateFileListEntryShouldFailWithFileListReason()
    {
        using GateFixture fixture = new();
        File.AppendAllText(fixture.StoryPath, "\n## File List\n\n- `src/gate.txt`\n");

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.FileListDiffMismatch);
    }

    /// <summary>Proves renamed paths require an exact File List update.</summary>
    [Fact]
    public static void RenamedPathWithStaleFileListShouldFailWithFileListReason()
    {
        using GateFixture fixture = new();
        fixture.RenameOwnedSourceWithoutUpdatingFileList();

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.FileListDiffMismatch);
    }

    /// <summary>Proves undisclosed local changes cannot hide outside the explicit scope.</summary>
    [Fact]
    public static void UndisclosedUntrackedChangeShouldFailWithScopeReason()
    {
        using GateFixture fixture = new();
        File.WriteAllText(Path.Combine(fixture.RepositoryRoot, "unowned.txt"), "unowned\n");

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
    }

    /// <summary>Proves explicitly pre-existing local changes are visible without becoming owned scope.</summary>
    [Fact]
    public static void DisclosedPreExistingLocalChangeShouldPass()
    {
        using GateFixture fixture = new();
        File.WriteAllText(Path.Combine(fixture.RepositoryRoot, "unowned.txt"), "unowned\n");
        fixture.MutateContract(contract =>
        {
            contract["outOfScopeDisclosures"] = new JsonArray(new JsonObject
            {
                ["repository"] = "root",
                ["path"] = "unowned.txt",
                ["owner"] = "another-story",
                ["reason"] = "Pre-existing synthetic local work.",
                ["classification"] = "preExistingLocalChange",
            });
        });

        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves local disclosures cannot waive unrelated changes committed in immutable scope.</summary>
    [Fact]
    public static void DisclosedCommittedUnrelatedChangeShouldFailWithScopeReason()
    {
        using GateFixture fixture = new();
        fixture.UseProductCompletion();
        fixture.CommitCompletionWithDisclosedUnrelatedChange();

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
    }

    /// <summary>Proves wrong implementation content cannot reuse an old digest.</summary>
    [Fact]
    public static void ChangedImplementationAfterAttestationShouldFailWithDigestReason()
    {
        using GateFixture fixture = new();
        File.AppendAllText(fixture.SourcePath, "changed after evidence\n");

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
    }

    /// <summary>Proves malformed, failed, zero, and skipped machine results fail non-vacuously.</summary>
    /// <param name="kind">The result mutation.</param>
    [Theory]
    [InlineData("malformed")]
    [InlineData("failed")]
    [InlineData("zero")]
    [InlineData("all-skipped")]
    [InlineData("mixed-skipped")]
    public static void InvalidMachineResultsShouldFailWithMachineReason(string kind)
    {
        using GateFixture fixture = new();
        switch (kind)
        {
            case "malformed":
                File.WriteAllText(fixture.TrxPath, "not xml");
                break;
            case "failed":
                fixture.WriteTrx(1, 1, 0, 1, 0, "Failed");
                break;
            case "zero":
                fixture.WriteTrx(0, 0, 0, 0, 0, "Passed");
                break;
            case "all-skipped":
                fixture.WriteTrx(1, 0, 0, 0, 1, "Passed");
                break;
            case "mixed-skipped":
                fixture.WriteTrx(2, 1, 1, 0, 1, "Passed");
                break;
        }

        Should.Throw<GateValidationException>(() => fixture.RefreshEvidence())
            .ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
    }

    /// <summary>Proves TRX counters and individual outcomes must describe the same result set.</summary>
    [Theory]
    [InlineData("counter-total")]
    [InlineData("outcome-mismatch")]
    [InlineData("unknown-outcome")]
    public static void ContradictoryTrxInternalsShouldFailWithMachineReason(string kind)
    {
        using GateFixture fixture = new();
        if (kind == "counter-total")
        {
            fixture.WriteTrx(2, 1, 1, 0, 0, "Completed");
        }
        else
        {
            fixture.WriteTrx(1, 1, 1, 0, 0, "Completed");
            File.WriteAllText(
                fixture.TrxPath,
                File.ReadAllText(fixture.TrxPath).Replace(
                    "outcome=\"Passed\" />",
                    kind == "outcome-mismatch" ? "outcome=\"Failed\" />" : "outcome=\"Other\" />",
                    StringComparison.Ordinal));
        }

        Should.Throw<GateValidationException>(() => fixture.RefreshEvidence())
            .ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
    }

    /// <summary>Proves method selectors accept parameterized result display names.</summary>
    [Fact]
    public static void ParameterizedPassingResultShouldSatisfyMethodSelector()
    {
        using GateFixture fixture = new();
        const string TestName = "GateFixture.Parameterized(value: 1)";
        fixture.MutateContract(contract =>
        {
            EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!.AsObject()["selectors"] =
                new JsonArray("method:GateFixture.Parameterized");
            EvidenceJson.RequiredArray(contract, "mappings", GateReason.CheckedItemEvidenceMismatch)[2]!.AsObject()["assertions"] =
                new JsonArray("GateFixture.Parameterized");
        }, refreshEvidence: false);
        fixture.WriteTrx(2, 2, 2, 0, 0, "Completed", TestName);
        fixture.RefreshEvidence();

        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves class and method selectors are matched against actual passing UnitTestResult names.</summary>
    [Theory]
    [InlineData("class:GateFixture")]
    [InlineData("method:GateFixture.ValidAssertion")]
    public static void ActualPassingResultSelectorShouldPass(string selector)
    {
        using GateFixture fixture = new();
        fixture.MutateContract(contract =>
            EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!.AsObject()["selectors"] =
                new JsonArray(selector));

        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves an absent selector cannot be satisfied by aggregate passing counters.</summary>
    [Fact]
    public static void AbsentPassingResultSelectorShouldFailWithMachineReason()
    {
        using GateFixture fixture = new();
        fixture.MutateContract(contract =>
            EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!.AsObject()["selectors"] =
                new JsonArray("class:MissingFixture"), refreshEvidence: false);

        Should.Throw<GateValidationException>(() => fixture.Attest())
            .ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
    }

    /// <summary>Proves spoofed display names cannot satisfy selectors without the matching testId/TestMethod identity.</summary>
    [Fact]
    public static void SpoofedTrxDisplayNameShouldFailWithMachineReason()
    {
        using GateFixture fixture = new();
        File.WriteAllText(
            fixture.TrxPath,
            File.ReadAllText(fixture.TrxPath).Replace(
                "className=\"GateFixture\"",
                "className=\"Spoofed.Fixture\"",
                StringComparison.Ordinal));
        byte[] goodSidecar = File.ReadAllBytes(fixture.ProvenancePath);

        Should.Throw<GateValidationException>(() => fixture.Attest())
            .ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
        File.ReadAllBytes(fixture.ProvenancePath).ShouldBe(goodSidecar);
    }

    /// <summary>Proves stale and future current-run TRX clocks cannot mint or replace provenance.</summary>
    [Theory]
    [InlineData(-120)]
    [InlineData(10)]
    public static void InvalidCurrentRunTrxTimeShouldFailWithProvenanceReason(int minutesFromNow)
    {
        using GateFixture fixture = new();
        fixture.WriteTrx(1, 1, 1, 0, 0, "Completed", finishedAtUtc: DateTimeOffset.UtcNow.AddMinutes(minutesFromNow));
        byte[] goodSidecar = File.ReadAllBytes(fixture.ProvenancePath);

        Should.Throw<GateValidationException>(() => fixture.Attest())
            .ReasonCode.ShouldBe(GateReason.EvidenceStaleOrUnbound);
        File.ReadAllBytes(fixture.ProvenancePath).ShouldBe(goodSidecar);

        File.Delete(fixture.ProvenancePath);
        Should.Throw<GateValidationException>(() => fixture.Attest())
            .ReasonCode.ShouldBe(GateReason.EvidenceStaleOrUnbound);
        File.Exists(fixture.ProvenancePath).ShouldBeFalse();
    }

    /// <summary>Proves malformed, failed, skipped, and spoofed current-run TRX cannot mint or replace provenance.</summary>
    [Theory]
    [InlineData("malformed")]
    [InlineData("failed")]
    [InlineData("skipped")]
    [InlineData("spoofed")]
    public static void InvalidCurrentRunTrxShouldNotMintOrReplaceProvenance(string kind)
    {
        using GateFixture fixture = new();
        switch (kind)
        {
            case "malformed":
                File.WriteAllText(fixture.TrxPath, "not xml");
                break;
            case "failed":
                fixture.WriteTrx(1, 1, 0, 1, 0, "Failed");
                break;
            case "skipped":
                fixture.WriteTrx(1, 0, 0, 0, 1, "Completed");
                break;
            case "spoofed":
                File.WriteAllText(
                    fixture.TrxPath,
                    File.ReadAllText(fixture.TrxPath).Replace(
                        "className=\"GateFixture\"",
                        "className=\"Spoofed.Fixture\"",
                        StringComparison.Ordinal));
                break;
        }

        byte[] goodSidecar = File.ReadAllBytes(fixture.ProvenancePath);
        Should.Throw<GateValidationException>(() => fixture.Attest())
            .ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
        File.ReadAllBytes(fixture.ProvenancePath).ShouldBe(goodSidecar);

        File.Delete(fixture.ProvenancePath);
        Should.Throw<GateValidationException>(() => fixture.Attest())
            .ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
        File.Exists(fixture.ProvenancePath).ShouldBeFalse();
    }

    /// <summary>Proves only the canonical TeamTest 2010 TRX structure can supply machine evidence.</summary>
    [Theory]
    [InlineData("root")]
    [InlineData("namespace")]
    [InlineData("duplicate-times")]
    [InlineData("duplicate-result-id")]
    [InlineData("foreign-local-name")]
    [InlineData("multiple-test-method")]
    public static void NonCanonicalTrxStructureShouldFailWithMachineReason(string mutation)
    {
        using GateFixture fixture = new();
        string trx = File.ReadAllText(fixture.TrxPath);
        trx = mutation switch
        {
            "root" => trx.Replace("<TestRun xmlns=", "<InjectedTestRun xmlns=", StringComparison.Ordinal)
                .Replace("</TestRun>", "</InjectedTestRun>", StringComparison.Ordinal),
            "namespace" => trx.Replace(
                "http://microsoft.com/schemas/VisualStudio/TeamTest/2010",
                "urn:foreign-teamtest",
                StringComparison.Ordinal),
            "duplicate-times" => trx.Replace(
                "  <Results>",
                $"  <Times start=\"{DateTimeOffset.UtcNow.AddSeconds(-1):O}\" finish=\"{DateTimeOffset.UtcNow:O}\" />\n  <Results>",
                StringComparison.Ordinal),
            "duplicate-result-id" => trx.Replace(
                "  </Results>",
                "    <UnitTestResult testId=\"test-1\" testName=\"GateFixture.ValidAssertion\" outcome=\"Passed\" />\n  </Results>",
                StringComparison.Ordinal),
            "foreign-local-name" => trx.Replace(
                "  </Results>",
                "    <foreign:UnitTestResult xmlns:foreign=\"urn:foreign\" testId=\"foreign\" outcome=\"Passed\" />\n  </Results>",
                StringComparison.Ordinal),
            "multiple-test-method" => trx.Replace(
                "</UnitTest>",
                "<TestMethod className=\"GateFixture\" name=\"Injected\" /></UnitTest>",
                StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };
        File.WriteAllText(fixture.TrxPath, trx);

        GateValidationException exception = Should.Throw<GateValidationException>(() => fixture.Attest());

        exception.ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
    }

    /// <summary>Proves all current-run lanes preflight before any lane sidecar is replaced or minted.</summary>
    [Fact]
    public static void MultiLaneInvalidLaterTrxShouldLeaveEverySidecarAtomic()
    {
        using GateFixture fixture = new();
        string secondTrx = Path.Combine(fixture.ResultsRoot, "gate-second.trx");
        string secondProvenance = Path.Combine(fixture.ResultsRoot, "gate-second.provenance.json");
        File.Copy(fixture.TrxPath, secondTrx);
        fixture.MutateContract(contract =>
        {
            JsonObject secondLane = EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!
                .DeepClone().AsObject();
            secondLane["lane"] = "gate-unit-second";
            secondLane["trx"] = "gate-second.trx";
            secondLane["provenance"] = "gate-second.provenance.json";
            secondLane["artifactLocator"] = "file:gate-second.trx";
            EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid).Add(secondLane);
        }, refreshEvidence: false);
        fixture.RefreshEvidence();
        File.WriteAllText(secondTrx, "not xml");
        byte[] firstBefore = File.ReadAllBytes(fixture.ProvenancePath);
        byte[] secondBefore = File.ReadAllBytes(secondProvenance);

        Should.Throw<GateValidationException>(() => fixture.Attest())
            .ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
        File.ReadAllBytes(fixture.ProvenancePath).ShouldBe(firstBefore);
        File.ReadAllBytes(secondProvenance).ShouldBe(secondBefore);

        File.Delete(fixture.ProvenancePath);
        File.Delete(secondProvenance);
        Should.Throw<GateValidationException>(() => fixture.Attest())
            .ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
        File.Exists(fixture.ProvenancePath).ShouldBeFalse();
        File.Exists(secondProvenance).ShouldBeFalse();
    }

    /// <summary>Proves lane result paths cannot alias or share provenance destinations.</summary>
    [Theory]
    [InlineData("same-lane")]
    [InlineData("shared-provenance")]
    public static void CollidingAttestationPathsShouldFailBeforeWrites(string kind)
    {
        using GateFixture fixture = new();
        byte[] before = File.ReadAllBytes(fixture.ProvenancePath);
        fixture.MutateContract(contract =>
        {
            JsonObject first = EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!.AsObject();
            if (kind.Equals("same-lane", StringComparison.Ordinal))
            {
                first["provenance"] = "gate.trx";
                return;
            }

            File.Copy(fixture.TrxPath, Path.Combine(fixture.ResultsRoot, "gate-second.trx"));
            JsonObject second = first.DeepClone().AsObject();
            second["lane"] = "gate-unit-second";
            second["trx"] = "gate-second.trx";
            second["artifactLocator"] = "file:gate-second.trx";
            EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid).Add(second);
        }, refreshEvidence: false);

        GateValidationException exception = Should.Throw<GateValidationException>(() => fixture.Attest());

        exception.ReasonCode.ShouldBe(GateReason.EvidenceStaleOrUnbound);
        exception.Subject.ShouldBe("result-path-collision");
        File.ReadAllBytes(fixture.ProvenancePath).ShouldBe(before);
    }

    /// <summary>Proves invalid pinned policy or contract versions cannot mint or replace provenance.</summary>
    [Theory]
    [InlineData("policy")]
    [InlineData("contract")]
    public static void InvalidAttestationGrammarShouldNotMintOrReplaceProvenance(string kind)
    {
        ArgumentNullException.ThrowIfNull(kind);
        using GateFixture fixture = new();
        if (kind.Equals("policy", StringComparison.Ordinal))
        {
            fixture.MutatePolicy(policy => policy["maximumCurrentRunAgeMinutes"] = 999);
        }
        else
        {
            fixture.MutateContract(contract => contract["schemaVersion"] = "1.0", refreshEvidence: false);
        }

        byte[] before = File.ReadAllBytes(fixture.ProvenancePath);
        Should.Throw<GateValidationException>(() => fixture.Attest());
        File.ReadAllBytes(fixture.ProvenancePath).ShouldBe(before);
        File.Delete(fixture.ProvenancePath);
        Should.Throw<GateValidationException>(() => fixture.Attest());
        File.Exists(fixture.ProvenancePath).ShouldBeFalse();
    }

    /// <summary>Proves missing machine results fail closed.</summary>
    [Fact]
    public static void MissingMachineResultShouldFailWithMachineReason()
    {
        using GateFixture fixture = new();
        File.Delete(fixture.TrxPath);

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
    }

    /// <summary>Proves stale retained evidence fails provenance binding.</summary>
    [Fact]
    public static void StaleEvidenceShouldFailWithProvenanceReason()
    {
        using GateFixture fixture = new();
        fixture.MutateProvenance(sidecar => sidecar["producedAtUtc"] = DateTimeOffset.UtcNow.AddDays(-31).ToString("O"));

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.EvidenceStaleOrUnbound);
    }

    /// <summary>Proves a wrong head, digest, or checksum cannot reuse results.</summary>
    /// <param name="field">The provenance field to corrupt.</param>
    [Theory]
    [InlineData("headCommit")]
    [InlineData("implementationDigest")]
    [InlineData("trxSha256")]
    public static void WrongProvenanceBindingShouldFailWithProvenanceReason(string field)
    {
        using GateFixture fixture = new();
        fixture.MutateProvenance(sidecar => sidecar[field] = new string('f', field == "headCommit" ? 40 : 64));

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.EvidenceStaleOrUnbound);
    }

    /// <summary>Proves exact-digest retained evidence remains acceptable inside policy age.</summary>
    [Fact]
    public static void RetainedExactDigestEvidenceShouldPass()
    {
        using GateFixture fixture = new();
        fixture.UseRetainedEvidence();

        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves CI attestation leaves retained sidecars immutable.</summary>
    [Fact]
    public static void RetainedProvenanceShouldNotBeOverwrittenByAttestation()
    {
        using GateFixture fixture = new();
        fixture.UseRetainedEvidence();
        string before = File.ReadAllText(fixture.ProvenancePath);

        fixture.Attest();

        File.ReadAllText(fixture.ProvenancePath).ShouldBe(before);
        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves wrong retained SHA/digest bindings stay wrong because attestation cannot mint replacements.</summary>
    [Theory]
    [InlineData("headCommit", 40)]
    [InlineData("implementationDigest", 64)]
    [InlineData("trxSha256", 64)]
    public static void WrongRetainedBindingShouldFailWithoutBeingOverwritten(string field, int length)
    {
        using GateFixture fixture = new();
        fixture.UseRetainedEvidence();
        fixture.MutateProvenance(sidecar => sidecar[field] = new string('f', length));
        string before = File.ReadAllText(fixture.ProvenancePath);

        fixture.Attest();

        File.ReadAllText(fixture.ProvenancePath).ShouldBe(before);
        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.EvidenceStaleOrUnbound);
    }

    /// <summary>Proves retained provenance must bind the exact contract-declared artifact locator.</summary>
    [Fact]
    public static void WrongRetainedArtifactLocatorShouldFailWithProvenanceReason()
    {
        using GateFixture fixture = new();
        fixture.UseRetainedEvidence();
        fixture.MutateProvenance(sidecar => sidecar["artifactLocator"] = "artifact:unapproved/gate.trx");

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.EvidenceStaleOrUnbound);
    }

    /// <summary>Proves current-run locators are exact file locators for their declared TRX path.</summary>
    [Fact]
    public static void WrongCurrentRunArtifactLocatorShouldFailWithProvenanceReason()
    {
        using GateFixture fixture = new();
        fixture.MutateContract(contract =>
            EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!.AsObject()["artifactLocator"] =
                "file:other.trx", refreshEvidence: false);
        Should.Throw<GateValidationException>(() => fixture.RefreshEvidence())
            .ReasonCode.ShouldBe(GateReason.EvidenceStaleOrUnbound);

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.EvidenceStaleOrUnbound);
    }

    /// <summary>Proves a symlink anywhere in the results-root ancestry is rejected.</summary>
    [Fact]
    public static void SymlinkedResultsRootAncestorShouldFailWithProvenanceReason()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using GateFixture fixture = new();
        string aliasParent = Path.Combine(Path.GetTempPath(), $"gate-results-alias-{Guid.NewGuid():N}");
        Directory.CreateDirectory(aliasParent);
        string alias = Path.Combine(aliasParent, "linked-results");
        Directory.CreateSymbolicLink(alias, fixture.ResultsRoot);
        try
        {
            fixture.Validate(resultsRoot: alias).Issues.Single().ReasonCode.ShouldBe(GateReason.EvidenceStaleOrUnbound);
        }
        finally
        {
            Directory.Delete(aliasParent, recursive: true);
        }
    }

    /// <summary>Proves a recognized primary browser lane can satisfy a triggered class.</summary>
    [Fact]
    public static void RecognizedBrowserPrimaryPathShouldPass()
    {
        using GateFixture fixture = new();
        fixture.UseBrowserPrimaryPath("browser-primary");

        GateReport report = fixture.Validate();
        report.Passed.ShouldBeTrue(string.Join(", ", report.Issues.Select(static issue => $"{issue.ReasonCode}:{issue.Subject}")));
    }

    /// <summary>Proves every configured claim-class trigger accepts only its recognized primary lane.</summary>
    /// <param name="relativePath">The trigger path.</param>
    /// <param name="pathClass">The expected class.</param>
    /// <param name="lane">The recognized lane.</param>
    [Theory]
    [InlineData("src/Pages/View.razor", "browser", "browser-primary")]
    [InlineData("src/SignalR/ProjectHub.cs", "signalr", "signalr-primary")]
    [InlineData("src/Assets/site.css", "hosting-assets", "hosting-assets-primary")]
    [InlineData("src/Hexalith.ChatBot.AppHost/Program.cs", "aspire-dapr", "aspire-dapr-primary")]
    [InlineData(
        "tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs",
        "aspire-dapr",
        "aspire-dapr-primary")]
    [InlineData("tests/Module/Recovery/Scenario.cs", "recovery", "recovery-primary")]
    [InlineData(".github/workflows/ci.yml", "recovery", "recovery-primary")]
    public static void EveryConfiguredPrimaryPathClassShouldPassItsRecognizedLane(
        string relativePath,
        string pathClass,
        string lane)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        ArgumentNullException.ThrowIfNull(pathClass);
        ArgumentNullException.ThrowIfNull(lane);
        using GateFixture fixture = new();
        fixture.UsePrimaryPath(relativePath, pathClass, lane);

        GateReport report = fixture.Validate();
        report.Passed.ShouldBeTrue(string.Join(", ", report.Issues.Select(static issue => $"{issue.ReasonCode}:{issue.Subject}")));
    }

    /// <summary>Proves double-star slash patterns also match zero intermediate directories.</summary>
    [Fact]
    public static void PrimaryGlobDoubleStarShouldMatchZeroDirectories()
    {
        using GateFixture fixture = new();
        fixture.UsePrimaryPath("src/View.razor", "browser", "browser-primary");

        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves each policy claim phrase triggers its exact bound primary lane.</summary>
    [Theory]
    [InlineData("browser", "browser-primary")]
    [InlineData("signalr", "signalr-primary")]
    [InlineData("hosting-assets", "hosting-assets-primary")]
    [InlineData("aspire-dapr", "aspire-dapr-primary")]
    [InlineData("recovery", "recovery-primary")]
    public static void FenceFreePrimaryClaimShouldPassItsBoundLane(string pathClass, string lane)
    {
        using GateFixture fixture = new();
        fixture.UseClaimPrimaryPath(pathClass, lane);

        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves a claim cannot pass without its policy-bound declaration and result.</summary>
    [Fact]
    public static void UndeclaredPrimaryClaimShouldFailWithPrimaryReason()
    {
        using GateFixture fixture = new();
        File.AppendAllText(fixture.StoryPath, "\n[claim:browser]\n");
        fixture.RefreshEvidence();

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.PrimaryPathNotExecuted);
    }

    /// <summary>Proves example claims inside Markdown fences do not trigger obligations.</summary>
    [Fact]
    public static void FencedPrimaryClaimShouldBeIgnored()
    {
        using GateFixture fixture = new();
        fixture.AddFencedClaim("browser");

        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves relabeling a passing result to a generic selector cannot satisfy a primary lane.</summary>
    [Fact]
    public static void GenericSelectorShouldNotSatisfyPrimaryBinding()
    {
        using GateFixture fixture = new();
        fixture.UseBrowserPrimaryPath("browser-primary");
        fixture.MutateContract(contract =>
            EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!.AsObject()["selectors"] =
                new JsonArray("class:GateFixture"), refreshEvidence: false);
        fixture.WriteTrx(1, 1, 1, 0, 0, "Completed");
        fixture.RefreshEvidence();

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.PrimaryPathNotExecuted);
    }

    /// <summary>Proves a fallback lane cannot substitute for a triggered primary class.</summary>
    [Fact]
    public static void FallbackOnlyPrimaryPathShouldFailWithPrimaryReason()
    {
        using GateFixture fixture = new();
        fixture.UseBrowserPrimaryPath("browser-fallback");

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.PrimaryPathNotExecuted);
    }

    /// <summary>Proves a result lane must declare the same primary class as its contract declaration.</summary>
    [Fact]
    public static void MismatchedResultPrimaryClassShouldFailWithPrimaryReason()
    {
        using GateFixture fixture = new();
        fixture.UseBrowserPrimaryPath("browser-primary");
        fixture.MutateContract(contract =>
            EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!.AsObject()["primaryPathClass"] =
                "recovery");

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.PrimaryPathNotExecuted);
    }

    /// <summary>Proves required primary lanes reject skips even when the result lane generally allows them.</summary>
    [Fact]
    public static void SkippedRequiredPrimaryLaneShouldFailWithPrimaryReason()
    {
        using GateFixture fixture = new();
        fixture.UseBrowserPrimaryPath("browser-primary");
        fixture.MutateContract(contract =>
            EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!.AsObject()["allowSkipped"] = true);
        fixture.WriteTrx(
            2,
            1,
            1,
            0,
            1,
            "Completed",
            "Hexalith.ChatBot.UI.E2E.Tests.RealRenderCrossSurfaceE2ETests.ValidAssertion");
        fixture.RefreshEvidence();

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.PrimaryPathNotExecuted);
    }

    /// <summary>Proves unchecked mandatory work blocks completion.</summary>
    [Fact]
    public static void UncheckedMandatoryTaskShouldFailWithMappingReason()
    {
        using GateFixture fixture = new();
        File.WriteAllText(
            fixture.StoryPath,
            File.ReadAllText(fixture.StoryPath).Replace("- [x] Prove", "- [ ] Prove", StringComparison.Ordinal));
        fixture.RefreshEvidence();

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.CheckedItemEvidenceMismatch);
    }

    /// <summary>Proves an unchecked mandatory child task also blocks completion.</summary>
    [Fact]
    public static void UncheckedMandatoryChildTaskShouldFailWithMappingReason()
    {
        using GateFixture fixture = new();
        File.WriteAllText(
            fixture.StoryPath,
            File.ReadAllText(fixture.StoryPath).Replace(
                "**Acceptance Criteria:**",
                "  - [ ] Complete mandatory child.\n\n**Acceptance Criteria:**",
                StringComparison.Ordinal));
        fixture.RefreshEvidence();

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.CheckedItemEvidenceMismatch);
    }

    /// <summary>Proves canonical nested unchecked tasks remain mandatory.</summary>
    [Fact]
    public static void CanonicalUncheckedNestedTaskShouldFailWithMappingReason()
    {
        using GateFixture fixture = new();
        fixture.UseCanonicalProductCompletion();
        File.WriteAllText(
            fixture.StoryPath,
            File.ReadAllText(fixture.StoryPath).Replace("  - [x] Prove", "  - [ ] Prove", StringComparison.Ordinal));
        fixture.RefreshEvidence();

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.CheckedItemEvidenceMismatch);
    }

    /// <summary>Proves canonical stories cannot omit their numbered acceptance section.</summary>
    [Fact]
    public static void CanonicalMissingAcceptanceSectionShouldFailWithMappingReason()
    {
        using GateFixture fixture = new();
        fixture.UseCanonicalProductCompletion();
        File.WriteAllText(
            fixture.StoryPath,
            File.ReadAllText(fixture.StoryPath).Replace("## Acceptance Criteria", "## Missing Criteria", StringComparison.Ordinal));
        fixture.RefreshEvidence();

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.CheckedItemEvidenceMismatch);
    }

    /// <summary>Proves missing, stale-path, and failed-assertion mappings fail closed.</summary>
    /// <param name="kind">The mapping mutation.</param>
    [Theory]
    [InlineData("missing")]
    [InlineData("stale-path")]
    [InlineData("failed-assertion")]
    public static void InvalidCheckedItemMappingShouldFailWithMappingReason(string kind)
    {
        using GateFixture fixture = new();
        fixture.MutateContract(contract =>
        {
            JsonArray mappings = EvidenceJson.RequiredArray(contract, "mappings", GateReason.CheckedItemEvidenceMismatch);
            if (kind == "missing")
            {
                mappings.RemoveAt(0);
            }
            else if (kind == "stale-path")
            {
                mappings[0]!.AsObject()["paths"] = new JsonArray("src/stale.txt");
            }
            else
            {
                mappings[2]!.AsObject()["assertions"] = new JsonArray("GateFixture.FailedAssertion");
            }
        });

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.CheckedItemEvidenceMismatch);
    }

    /// <summary>Proves secret-, token-, and payload-shaped evidence fields are rejected.</summary>
    [Theory]
    [InlineData("secretPayload")]
    [InlineData("apiToken")]
    public static void ForbiddenEvidenceFieldShouldFailWithPayloadReason(string field)
    {
        using GateFixture fixture = new();
        fixture.MutateContract(contract => contract[field] = "forbidden", refreshEvidence: false);

        GateIssue issue = fixture.Validate().Issues.Single();
        issue.ReasonCode.ShouldBe(GateReason.EvidencePayloadForbidden);
        issue.Subject.ShouldBe("redacted");
    }

    /// <summary>Proves secret-like or unbounded values are forbidden even in allowed metadata fields.</summary>
    [Theory]
    [InlineData("Bearer abcdef")]
    [InlineData("password=hunter2")]
    public static void UnsafeMetadataValueShouldFailWithoutEchoingValue(string unsafeValue)
    {
        using GateFixture fixture = new();
        fixture.MutateContract(contract => contract["storyTitle"] = unsafeValue, refreshEvidence: false);

        GateIssue issue = fixture.Validate().Issues.Single();
        issue.ReasonCode.ShouldBe(GateReason.EvidencePayloadForbidden);
        JsonReportWriter.Serialize(fixture.Validate()).ShouldNotContain(unsafeValue);
    }

    /// <summary>Proves failure subjects are bounded and redacted before report serialization.</summary>
    [Fact]
    public static void UnsafeFailureSubjectShouldBeRedacted()
    {
        GateIssue.Create(GateReason.ScopeDigestMismatch, $"token={new string('x', 200)}")
            .Subject.ShouldBe("redacted");
    }

    /// <summary>Proves root gitlink and root-declared-submodule inner diffs reconcile together.</summary>
    [Fact]
    public static void RootDeclaredSubmoduleScopeShouldPassWithoutRecursiveDiscovery()
    {
        using GateFixture fixture = new();
        fixture.UseRootDeclaredSubmodule();

        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves root .gitmodules may declare submodules at any path depth.</summary>
    [Theory]
    [InlineData("Synthetic.Module")]
    [InlineData("vendor/deep/Synthetic.Module")]
    public static void RootDeclaredSubmoduleAtAnyDepthShouldPass(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        using GateFixture fixture = new();
        fixture.UseRootDeclaredSubmodule(path);

        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves an owned root gitlink cannot omit its explicit inner repository scope.</summary>
    [Fact]
    public static void OwnedGitlinkWithoutSubmoduleScopeShouldFail()
    {
        using GateFixture fixture = new();
        fixture.UseRootDeclaredSubmodule();
        fixture.RemoveSubmoduleScope();

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.GitlinkScopeMismatch);
    }

    /// <summary>Proves normalization cannot collapse duplicate owned paths after JSON parsing.</summary>
    [Fact]
    public static void DuplicateNormalizedIncludePathsShouldFail()
    {
        using GateFixture fixture = new();
        fixture.MutateContract(contract =>
        {
            JsonArray includePaths = EvidenceJson.RequiredArray(
                EvidenceJson.RequiredArray(
                    EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch),
                    "repositories",
                    GateReason.ScopeDigestMismatch)[0]!.AsObject(),
                "includePaths",
                GateReason.ScopeDigestMismatch);
            includePaths.Add("src\\gate.txt");
        }, refreshEvidence: false);

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
    }

    /// <summary>Proves exact duplicate strings in evidence arrays fail before set conversion.</summary>
    [Fact]
    public static void DuplicateSelectorArrayValuesShouldFail()
    {
        using GateFixture fixture = new();
        fixture.MutateContract(contract =>
            EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!.AsObject()["selectors"] =
                new JsonArray("class:GateFixture", "class:GateFixture"), refreshEvidence: false);
        Should.Throw<GateValidationException>(() => fixture.RefreshEvidence())
            .ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
    }

    /// <summary>Proves lifecycle exclusion applies only to the exact contract scope field.</summary>
    [Fact]
    public static void ImplementationDigestFieldInAnotherJsonFileShouldRemainDigestBound()
    {
        using GateFixture fixture = new();
        fixture.AddOwnedFile("src/other.json", "{\"implementationDigest\":\"first\"}\n");
        File.WriteAllText(
            Path.Combine(fixture.RepositoryRoot, "src", "other.json"),
            "{\"implementationDigest\":\"second\"}\n");

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
    }

    /// <summary>Proves lifecycle masking does not canonicalize unrelated contract bytes.</summary>
    [Fact]
    public static void UnrelatedContractWhitespaceShouldRemainDigestBound()
    {
        using GateFixture fixture = new();
        File.WriteAllText(
            fixture.ContractPath,
            File.ReadAllText(fixture.ContractPath).Replace(
                "\"recordKind\": \"technicalEnabler\"",
                "\"recordKind\":  \"technicalEnabler\"",
                StringComparison.Ordinal));

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
    }

    /// <summary>Proves immutable digests preserve raw line endings, execute mode, and symlink target text.</summary>
    [Fact]
    public static void ImmutableDigestShouldBindExactTreeBytesAndModes()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        static string Digest(Action<GateFixture> mutation)
        {
            using GateFixture fixture = new();
            fixture.UseProductCompletion();
            mutation(fixture);
            fixture.CommitStrictImmutableCompletion();
            GateReport report = fixture.Validate();
            report.Passed.ShouldBeTrue(string.Join(", ", report.Issues.Select(static issue => issue.ReasonCode)));
            return report.ImplementationDigest;
        }

        string lf = Digest(static fixture => fixture.SetOwnedSourceText("same\ncontent\n"));
        string crlf = Digest(static fixture => fixture.SetOwnedSourceText("same\r\ncontent\r\n"));
        string executable = Digest(static fixture =>
        {
            fixture.SetOwnedSourceText("same\ncontent\n");
            fixture.SetOwnedSourceExecutable();
        });
        string symlinkA = Digest(static fixture => fixture.SetOwnedSourceSymlink("target-a"));
        string symlinkB = Digest(static fixture => fixture.SetOwnedSourceSymlink("target-b"));

        lf.ShouldNotBe(crlf);
        lf.ShouldNotBe(executable);
        symlinkA.ShouldNotBe(symlinkB);
    }

    /// <summary>Proves every load-bearing security field remains pinned within policy version 2.0.</summary>
    [Theory]
    [InlineData("age")]
    [InlineData("tree-source")]
    [InlineData("primary-selector")]
    [InlineData("primary-result-path")]
    [InlineData("metadata-bound")]
    public static void SameVersionSecurityPolicyMutationShouldFail(string kind)
    {
        using GateFixture fixture = new();
        fixture.MutatePolicy(policy =>
        {
            if (kind == "age")
            {
                policy["maximumRetainedEvidenceAgeHours"] = 721;
            }
            else if (kind == "tree-source")
            {
                EvidenceJson.RequiredObject(policy, "sourceDigest", GateReason.ScopeDigestMismatch)["immutableContentSource"] =
                    "filesystem";
            }
            else if (kind == "primary-selector")
            {
                EvidenceJson.RequiredArray(
                    EvidenceJson.RequiredArray(policy, "primaryPathTriggers", GateReason.ScopeDigestMismatch)[0]!.AsObject(),
                    "recognizedLaneBindings",
                    GateReason.ScopeDigestMismatch)[0]!.AsObject()["selector"] = "class:Generic";
            }
            else if (kind == "primary-result-path")
            {
                EvidenceJson.RequiredArray(
                    EvidenceJson.RequiredArray(policy, "primaryPathTriggers", GateReason.ScopeDigestMismatch)[4]!.AsObject(),
                    "recognizedLaneBindings",
                    GateReason.ScopeDigestMismatch)[0]!.AsObject()["trx"] = "recovery-primary/other.trx";
            }
            else
            {
                EvidenceJson.RequiredObject(policy, "metadataOnly", GateReason.ScopeDigestMismatch)["maximumStringLength"] = 1024;
            }
        });

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
    }

    /// <summary>Proves ambiguous story identities and structural sections fail closed.</summary>
    [Theory]
    [InlineData("title")]
    [InlineData("status")]
    [InlineData("execution")]
    [InlineData("misplaced-acceptance")]
    public static void AmbiguousStoryStructureShouldFail(string kind)
    {
        using GateFixture fixture = new();
        string text = File.ReadAllText(fixture.StoryPath);
        text = kind switch
        {
            "title" => text.Replace("title: 'Gate Fixture'", "title: 'Gate Fixture'\ntitle: 'Gate Fixture'", StringComparison.Ordinal),
            "status" => text.Replace("status: 'in-progress'", "status: 'in-progress'\nstatus: 'in-progress'", StringComparison.Ordinal),
            "execution" => text.Replace("**Acceptance Criteria:**", "**Execution:**\n- [x] Duplicate.\n\n**Acceptance Criteria:**", StringComparison.Ordinal),
            _ => text.Replace("**Acceptance Criteria:**", "## Outside\n\n**Acceptance Criteria:**", StringComparison.Ordinal),
        };
        File.WriteAllText(fixture.StoryPath, text);

        fixture.Validate().Passed.ShouldBeFalse();
    }

    /// <summary>Proves structural examples inside fences cannot create duplicate Markdown records.</summary>
    [Fact]
    public static void FencedStoryStructureShouldBeIgnored()
    {
        using GateFixture fixture = new();
        File.AppendAllText(
            fixture.StoryPath,
            "\n```markdown\n## Tasks & Acceptance\n**Execution:**\n- [ ] fake\n**Acceptance Criteria:**\n- Given fake\n## File List\n- `fake.txt`\n```\n");
        fixture.RefreshEvidence();

        fixture.Validate().Passed.ShouldBeTrue();
    }

    /// <summary>Proves duplicate sprint, action, and TE records fail while fenced TE examples are ignored.</summary>
    [Fact]
    public static void DuplicateLedgerRecordsShouldFailAndFencedExamplesShouldBeIgnored()
    {
        Should.Throw<GateValidationException>(() =>
            SprintLedgerReader.StoryStatusFromText("development_status:\n  key: review\n  key: done\n", "key"));
        Should.Throw<GateValidationException>(() => SprintLedgerReader.ActionStatusFromText(
            "action_items:\n  - epic: 13\n    action: \"same\"\n    status: open\n  - epic: 13\n    action: \"same\"\n    status: open\n",
            "same"));
        Should.Throw<GateValidationException>(() => TechnicalEnablerLedgerReader.StatusFromText(
            "## TE-X — One\n- **Status:** review; open.\n## TE-X — Two\n- **Status:** review; open.\n",
            "TE-X"));
        TechnicalEnablerLedgerReader.StatusFromText(
            "## TE-X — One\n- **Status:** review; open.\n```markdown\n## TE-X — Fake\n- **Status:** complete; fake.\n```\n",
            "TE-X").ShouldBe("review");
    }

    /// <summary>Proves report write failures produce a stable metadata-only issue.</summary>
    [Fact]
    public static void ReportWriteFailureShouldReturnStableIssue()
    {
        using GateFixture fixture = new();
        string blocker = Path.Combine(fixture.RepositoryRoot, "report-parent-is-file");
        File.WriteAllText(blocker, "blocker");

        GateIssue issue = fixture.Validate(reportPath: Path.Combine(blocker, "report.json")).Issues.Single();
        issue.ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
        issue.Subject.ShouldBe("report-write");
    }

    /// <summary>Proves the submodule base must equal the superproject base gitlink.</summary>
    [Fact]
    public static void WrongSubmoduleBaseGitlinkShouldFailWithGitlinkReason()
    {
        using GateFixture fixture = new();
        fixture.UseRootDeclaredSubmodule();
        fixture.BreakSubmoduleBaseGitlinkBinding();

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.GitlinkScopeMismatch);
    }

    /// <summary>Proves report, scope mode, and event resolution remain policy-constrained.</summary>
    [Theory]
    [InlineData("report")]
    [InlineData("scope-mode")]
    [InlineData("event-resolution")]
    public static void InvalidPolicyBoundScopeExclusionShouldFailWithScopeReason(string kind)
    {
        using GateFixture fixture = new();
        if (kind == "report")
        {
            fixture.MutateContract(contract => contract["reportPath"] = "src/gate.txt", refreshEvidence: false);
        }
        else if (kind == "scope-mode")
        {
            fixture.MutatePolicy(policy => policy["allowedScopeModes"] = new JsonArray("diff"));
        }
        else
        {
            fixture.MutatePolicy(policy =>
                EvidenceJson.RequiredObject(policy, "eventBaseHeadResolution", GateReason.ScopeDigestMismatch)["pullRequestHead"] =
                    "checked-out merge commit");
        }

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
    }

    /// <summary>Proves story identities are validated before they can influence evidence or report paths.</summary>
    [Fact]
    public static void TraversalStoryKeyShouldFailBeforeEscapingEvidenceRoots()
    {
        using GateFixture fixture = new();
        fixture.MutateContract(contract =>
        {
            contract["storyKey"] = "../../escaped";
            contract["reportPath"] = "_bmad-output/implementation-artifacts/evidence/reports/../../escaped.json";
        }, refreshEvidence: false);
        string escaped = Path.Combine(fixture.RepositoryRoot, "_bmad-output", "implementation-artifacts", "escaped.json");

        GateValidationException exception = Should.Throw<GateValidationException>(() => fixture.Attest());

        exception.ReasonCode.ShouldBe(GateReason.StatusMismatch);
        exception.Subject.ShouldBe("story-key");
        File.Exists(escaped).ShouldBeFalse();
    }

    /// <summary>Proves nested repository scopes are rejected with the gitlink reason.</summary>
    [Fact]
    public static void NestedSubmoduleScopeShouldFailWithGitlinkReason()
    {
        using GateFixture fixture = new();
        fixture.UseRootDeclaredSubmodule();
        fixture.MutateContract(contract =>
        {
            JsonObject nested = EvidenceJson.RequiredArray(
                EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch),
                "repositories",
                GateReason.ScopeDigestMismatch)[1]!.AsObject();
            nested["path"] = "references/Synthetic.Module/nested";
        }, refreshEvidence: false);

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.GitlinkScopeMismatch);
    }

    /// <summary>Proves an unchanged malformed inactive contract cannot stall an unrelated event.</summary>
    [Fact]
    public static void UnchangedInactiveMalformedContractShouldBeIgnored()
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"inactive-contract-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            RunGit(temporaryRoot, "init", "--initial-branch=main");
            RunGit(temporaryRoot, "config", "user.email", "gate@example.invalid");
            RunGit(temporaryRoot, "config", "user.name", "Story Evidence Gate Tests");
            string evidence = Path.Combine(temporaryRoot, "_bmad-output", "implementation-artifacts", "evidence");
            Directory.CreateDirectory(evidence);
            File.WriteAllText(Path.Combine(evidence, "inactive.json"), "not-json");
            File.WriteAllText(Path.Combine(temporaryRoot, "README.md"), "base\n");
            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: create inactive malformed contract");
            string baseCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();
            File.WriteAllText(Path.Combine(temporaryRoot, "README.md"), "unrelated\n");
            RunGit(temporaryRoot, "add", "README.md");
            RunGit(temporaryRoot, "commit", "-m", "test: make unrelated change");
            string headCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();

            TransitionDetector.Detect(temporaryRoot, baseCommit, headCommit).ShouldBeEmpty();
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Proves changed or selected malformed evidence fails candidate detection closed.</summary>
    [Theory]
    [InlineData("changed")]
    [InlineData("selected")]
    public static void ActiveMalformedContractShouldFailClosed(string kind)
    {
        ArgumentNullException.ThrowIfNull(kind);
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"active-contract-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            RunGit(temporaryRoot, "init", "--initial-branch=main");
            RunGit(temporaryRoot, "config", "user.email", "gate@example.invalid");
            RunGit(temporaryRoot, "config", "user.name", "Story Evidence Gate Tests");
            string artifacts = Path.Combine(temporaryRoot, "_bmad-output", "implementation-artifacts");
            string evidence = Path.Combine(artifacts, "evidence");
            Directory.CreateDirectory(evidence);
            string contract = Path.Combine(evidence, "active.json");
            string story = Path.Combine(artifacts, "active.md");
            File.WriteAllText(contract, "not-json");
            File.WriteAllText(story, "---\ntitle: 'Active'\nstatus: 'review'\n---\n");
            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: create malformed candidate base");
            string baseCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();
            if (kind.Equals("changed", StringComparison.Ordinal))
            {
                File.WriteAllText(contract, "still-not-json");
            }
            else
            {
                File.WriteAllText(story, "---\ntitle: 'Active'\nstatus: 'done'\n---\n");
            }

            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: activate malformed evidence");
            string headCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();

            Should.Throw<GateValidationException>(() => TransitionDetector.Detect(temporaryRoot, baseCommit, headCommit));
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Proves active duplicate story keys and normalized story paths fail deterministically.</summary>
    [Theory]
    [InlineData("story-key")]
    [InlineData("story-path")]
    public static void DuplicateActiveCandidateIdentityShouldFailClosed(string identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"duplicate-candidate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            RunGit(temporaryRoot, "init", "--initial-branch=main");
            RunGit(temporaryRoot, "config", "user.email", "gate@example.invalid");
            RunGit(temporaryRoot, "config", "user.name", "Story Evidence Gate Tests");
            string artifacts = Path.Combine(temporaryRoot, "_bmad-output", "implementation-artifacts");
            string evidence = Path.Combine(artifacts, "evidence");
            Directory.CreateDirectory(evidence);
            string story = Path.Combine(artifacts, "unrelated-name.md");
            File.WriteAllText(story, "---\ntitle: 'Duplicate Candidate'\nstatus: 'review'\n---\n");
            File.WriteAllText(Path.Combine(evidence, "explicit-transition.json"), TransitionContract());
            File.WriteAllText(Path.Combine(evidence, "malformed-inactive.json"), "not-json");
            JsonObject duplicate = new()
            {
                ["storyKey"] = identity.Equals("story-key", StringComparison.Ordinal)
                    ? "explicit-transition"
                    : "other-transition",
                ["storyPath"] = identity.Equals("story-path", StringComparison.Ordinal)
                    ? "_bmad-output\\implementation-artifacts\\unrelated-name.md"
                    : "_bmad-output/implementation-artifacts/other.md",
            };
            File.WriteAllText(Path.Combine(evidence, "duplicate.json"), duplicate.ToJsonString());
            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: create duplicate candidate base");
            string baseCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();
            File.WriteAllText(story, "---\ntitle: 'Duplicate Candidate'\nstatus: 'done'\n---\n");
            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: activate duplicate candidate");
            string headCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();

            GateValidationException exception = Should.Throw<GateValidationException>(() =>
                TransitionDetector.Detect(temporaryRoot, baseCommit, headCommit));

            exception.ReasonCode.ShouldBe(GateReason.StatusMismatch);
            exception.Subject.ShouldBe("duplicate-contract-identity");
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Proves completed records cannot silently regress or be edited outside a real completion event.</summary>
    [Theory]
    [InlineData("contract-change")]
    [InlineData("bootstrap-regression")]
    [InlineData("story")]
    [InlineData("story-deletion")]
    [InlineData("technical-ledger")]
    [InlineData("sprint-story")]
    [InlineData("sprint-action")]
    public static void HistoricalTerminalRecordRegressionShouldFailClosed(string kind)
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"terminal-regression-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            RunGit(temporaryRoot, "init", "--initial-branch=main");
            RunGit(temporaryRoot, "config", "user.email", "gate@example.invalid");
            RunGit(temporaryRoot, "config", "user.name", "Story Evidence Gate Tests");
            string artifacts = Path.Combine(temporaryRoot, "_bmad-output", "implementation-artifacts");
            string evidence = Path.Combine(artifacts, "evidence");
            string planning = Path.Combine(temporaryRoot, "_bmad-output", "planning-artifacts");
            Directory.CreateDirectory(evidence);
            Directory.CreateDirectory(planning);
            string story = Path.Combine(artifacts, "unrelated-name.md");
            string contract = Path.Combine(evidence, "explicit-transition.json");
            string ledger = Path.Combine(planning, "technical-enablers.md");
            string sprint = Path.Combine(artifacts, "sprint-status.yaml");
            File.WriteAllText(story, "---\ntitle: 'Terminal'\nstatus: 'done'\n---\n");
            File.WriteAllText(contract, TransitionContract());
            File.WriteAllText(ledger, "## TE-X — Terminal\n\n- **Status:** complete; protected.\n");
            File.WriteAllText(
                sprint,
                "development_status:\n  explicit-sprint-key: done\naction_items:\n  - epic: 13\n    action: \"TE-X action\"\n    status: done\n");
            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: create terminal base");
            string baseCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();

            switch (kind)
            {
                case "contract-change":
                    File.WriteAllText(contract, TransitionContract().Replace(
                        "\"storyTitle\": \"Explicit Transition\"",
                        "\"storyTitle\": \"Changed Terminal\"",
                        StringComparison.Ordinal));
                    break;
                case "bootstrap-regression":
                    File.WriteAllText(contract, TransitionContract().Replace(
                        "\"bootstrap\": false",
                        "\"bootstrap\": true",
                        StringComparison.Ordinal));
                    break;
                case "story":
                    File.WriteAllText(story, "---\ntitle: 'Terminal'\nstatus: 'review'\n---\n");
                    break;
                case "story-deletion":
                    File.Delete(story);
                    break;
                case "technical-ledger":
                    File.WriteAllText(ledger, "## TE-X — Terminal\n\n- **Status:** review; regressed.\n");
                    break;
                case "sprint-story":
                    File.WriteAllText(
                        sprint,
                        "development_status:\n  explicit-sprint-key: review\naction_items:\n  - epic: 13\n    action: \"TE-X action\"\n    status: done\n");
                    break;
                case "sprint-action":
                    File.WriteAllText(
                        sprint,
                        "development_status:\n  explicit-sprint-key: done\naction_items:\n  - epic: 13\n    action: \"TE-X action\"\n    status: open\n");
                    break;
            }

            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: regress terminal record");
            string headCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();

            Should.Throw<GateValidationException>(() =>
                TransitionDetector.Detect(temporaryRoot, baseCommit, headCommit))
                .ReasonCode.ShouldBe(GateReason.StatusMismatch);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Proves each explicit technical-enabler terminal record triggers contract-bound evaluation.</summary>
    [Theory]
    [InlineData("spec")]
    [InlineData("ledger")]
    [InlineData("action")]
    public static void TechnicalEnablerCompletionRecordsShouldBeDetected(string changedRecord)
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"te-transition-detector-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            RunGit(temporaryRoot, "init", "--initial-branch=main");
            RunGit(temporaryRoot, "config", "user.email", "gate@example.invalid");
            RunGit(temporaryRoot, "config", "user.name", "Story Evidence Gate Tests");
            string artifacts = Path.Combine(temporaryRoot, "_bmad-output", "implementation-artifacts");
            string planning = Path.Combine(temporaryRoot, "_bmad-output", "planning-artifacts");
            string evidence = Path.Combine(artifacts, "evidence");
            Directory.CreateDirectory(evidence);
            Directory.CreateDirectory(planning);
            string story = Path.Combine(artifacts, "te-x.md");
            string sprint = Path.Combine(artifacts, "sprint-status.yaml");
            string ledger = Path.Combine(planning, "technical-enablers.md");
            File.WriteAllText(story, "---\ntitle: 'TE-X'\nstatus: 'in-progress'\n---\n");
            File.WriteAllText(sprint, "action_items:\n  - epic: 13\n    action: \"TE-X action\"\n    status: open\n");
            File.WriteAllText(ledger, "## TE-X — Fixture\n\n- **Status:** review; pending.\n");
            File.WriteAllText(Path.Combine(evidence, "te-x.json"), TechnicalEnablerTransitionContract());
            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: create technical enabler base");
            string baseCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();

            if (changedRecord == "spec")
            {
                File.WriteAllText(story, "---\ntitle: 'TE-X'\nstatus: 'complete'\n---\n");
            }
            else if (changedRecord == "ledger")
            {
                File.WriteAllText(ledger, "## TE-X — Fixture\n\n- **Status:** complete; protected.\n");
            }
            else
            {
                File.WriteAllText(sprint, "action_items:\n  - epic: 13\n    action: \"TE-X action\"\n    status: done\n");
            }

            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: propose technical enabler completion");
            string headCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();

            IReadOnlyList<TransitionRecord> transitions = TransitionDetector.Detect(temporaryRoot, baseCommit, headCommit);

            transitions.ShouldHaveSingleItem();
            transitions[0].StoryKey.ShouldBe("te-x");
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Proves transition detection binds explicit contract identity instead of filename prefixes.</summary>
    [Fact]
    public static void TransitionDetectionShouldUseExplicitStoryAndSprintKeys()
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"transition-detector-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            RunGit(temporaryRoot, "init", "--initial-branch=main");
            RunGit(temporaryRoot, "config", "user.email", "gate@example.invalid");
            RunGit(temporaryRoot, "config", "user.name", "Story Evidence Gate Tests");
            string artifacts = Path.Combine(temporaryRoot, "_bmad-output", "implementation-artifacts");
            string evidence = Path.Combine(artifacts, "evidence");
            Directory.CreateDirectory(evidence);
            string story = Path.Combine(artifacts, "unrelated-name.md");
            string sprint = Path.Combine(artifacts, "sprint-status.yaml");
            File.WriteAllText(story, "---\ntitle: 'Explicit Transition'\nstatus: 'review'\n---\n");
            File.WriteAllText(sprint, "development_status:\n  explicit-sprint-key: review\n");
            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: create transition base");
            string baseCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();
            File.WriteAllText(story, "---\ntitle: 'Explicit Transition'\nstatus: 'done'\n---\n");
            File.WriteAllText(sprint, "development_status:\n  explicit-sprint-key: done\n");
            File.WriteAllText(Path.Combine(evidence, "explicit-transition.json"), TransitionContract());
            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: propose explicit transition");
            string headCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();

            IReadOnlyList<TransitionRecord> transitions = TransitionDetector.Detect(temporaryRoot, baseCommit, headCommit);

            transitions.ShouldHaveSingleItem();
            transitions[0].StoryPath.ShouldBe("_bmad-output/implementation-artifacts/unrelated-name.md");
            transitions[0].StoryKey.ShouldBe("explicit-transition");
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Proves a newly added TE-2 bootstrap contract is evaluated before a persisted terminal transition.</summary>
    [Fact]
    public static void TransitionDetectionShouldIncludeChangedTechnicalEnablerBootstrapContract()
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"bootstrap-detector-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            RunGit(temporaryRoot, "init", "--initial-branch=main");
            RunGit(temporaryRoot, "config", "user.email", "gate@example.invalid");
            RunGit(temporaryRoot, "config", "user.name", "Story Evidence Gate Tests");
            string artifacts = Path.Combine(temporaryRoot, "_bmad-output", "implementation-artifacts");
            string evidence = Path.Combine(artifacts, "evidence");
            Directory.CreateDirectory(evidence);
            string story = Path.Combine(artifacts, "bootstrap.md");
            File.WriteAllText(story, "---\ntitle: 'Bootstrap'\nstatus: 'in-progress'\n---\n");
            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: create bootstrap base");
            string baseCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();
            File.WriteAllText(Path.Combine(evidence, "bootstrap-contract.json"), BootstrapTransitionContract());
            RunGit(temporaryRoot, "add", ".");
            RunGit(temporaryRoot, "commit", "-m", "test: add bootstrap contract");
            string headCommit = RunGit(temporaryRoot, "rev-parse", "HEAD").Trim();

            IReadOnlyList<TransitionRecord> transitions = TransitionDetector.Detect(temporaryRoot, baseCommit, headCommit);

            transitions.ShouldHaveSingleItem();
            transitions[0].StoryKey.ShouldBe("bootstrap-contract");
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static string TransitionContract()
    {
        return """
            {
              "schemaVersion": "2.0",
              "recordKind": "story",
              "recordLedgerKey": "explicit-sprint-key",
              "storyKey": "explicit-transition",
              "storyTitle": "Explicit Transition",
              "storyPath": "_bmad-output/implementation-artifacts/unrelated-name.md",
              "targetStatus": "done",
              "persistedStatus": "done",
              "sprintStatusKey": "explicit-sprint-key",
              "bootstrap": false,
              "scope": {
                "mode": "diff",
                "implementationDigest": "unused",
                "repositories": [],
                "transitionPaths": []
              },
              "results": [],
              "primaryPaths": [],
              "mappings": [],
              "outOfScopeDisclosures": [],
              "reportPath": "reports/explicit-transition.json"
            }
            """;
    }

    private static string BootstrapTransitionContract()
    {
        return """
            {
              "schemaVersion": "2.0",
              "recordKind": "technicalEnabler",
              "recordLedgerKey": "TE-X",
              "storyKey": "bootstrap-contract",
              "storyTitle": "Bootstrap",
              "storyPath": "_bmad-output/implementation-artifacts/bootstrap.md",
              "targetStatus": "done",
              "persistedStatus": "complete",
              "sprintStatusKey": "bootstrap action",
              "bootstrap": true,
              "scope": {
                "mode": "diff",
                "implementationDigest": "unused",
                "repositories": [],
                "transitionPaths": []
              },
              "results": [],
              "primaryPaths": [],
              "mappings": [],
              "outOfScopeDisclosures": [],
              "reportPath": "reports/bootstrap.json"
            }
            """;
    }

    private static string TechnicalEnablerTransitionContract()
    {
        return """
            {
              "schemaVersion": "2.0",
              "recordKind": "technicalEnabler",
              "recordLedgerKey": "TE-X",
              "storyKey": "te-x",
              "storyTitle": "TE-X",
              "storyPath": "_bmad-output/implementation-artifacts/te-x.md",
              "targetStatus": "done",
              "persistedStatus": "complete",
              "sprintStatusKey": "TE-X action",
              "bootstrap": false,
              "scope": {
                "mode": "diff",
                "implementationDigest": "unused",
                "repositories": [],
                "transitionPaths": []
              },
              "results": [],
              "primaryPaths": [],
              "mappings": [],
              "outOfScopeDisclosures": [],
              "reportPath": "_bmad-output/implementation-artifacts/evidence/reports/te-x.json"
            }
            """;
    }

    /// <summary>Proves duplicate result-lane names fail closed before validation can invent coverage.</summary>
    [Fact]
    public static void DuplicateResultLaneNamesShouldFailWithMachineReason()
    {
        using GateFixture fixture = new();
        File.Copy(fixture.TrxPath, Path.Combine(fixture.ResultsRoot, "gate-dup.trx"));
        File.Copy(fixture.ProvenancePath, Path.Combine(fixture.ResultsRoot, "gate-dup.provenance.json"));
        fixture.MutateContract(contract =>
        {
            JsonArray results = EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid);
            JsonObject first = results[0]!.AsObject();
            results.Add(new JsonObject
            {
                ["lane"] = EvidenceJson.RequiredString(first, "lane", GateReason.MachineResultsInvalid),
                ["trx"] = "gate-dup.trx",
                ["provenance"] = "gate-dup.provenance.json",
                ["artifactLocator"] = "file:gate-dup.trx",
                ["source"] = "current-run",
                ["selectors"] = new JsonArray("class:GateFixture"),
                ["allowSkipped"] = false,
                ["primaryPathClass"] = null,
            });
        }, refreshEvidence: false);
        fixture.RefreshEvidence();

        GateIssue issue = fixture.Validate().Issues.Single();
        issue.ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
        issue.Subject.ShouldBe("duplicate-lane");
    }

    /// <summary>Proves recovery-primary rejects retained evidence even when its provenance is otherwise valid.</summary>
    [Fact]
    public static void RecoveryPrimaryRetainedSourceShouldFail()
    {
        using GateFixture fixture = new();
        fixture.UsePrimaryPath("tests/Module/Recovery/Scenario.cs", "recovery", "recovery-primary");
        fixture.UseRetainedEvidence();

        GateReport report = fixture.Validate();
        report.Passed.ShouldBeFalse();
        report.Issues.Single().ReasonCode.ShouldBe(GateReason.PrimaryPathNotExecuted);
        report.Issues.Single().Subject.ShouldBe("recovery");
    }

    /// <summary>Proves a current-run recovery primary cannot drift from the producer-owned result paths.</summary>
    [Theory]
    [InlineData("trx")]
    [InlineData("provenance")]
    public static void RecoveryPrimaryWrongPinnedResultPathShouldFail(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        using GateFixture fixture = new();
        fixture.UsePrimaryPath("tests/Module/Recovery/Scenario.cs", "recovery", "recovery-primary");
        if (field.Equals("trx", StringComparison.Ordinal))
        {
            File.Copy(
                fixture.TrxPath,
                Path.Combine(fixture.ResultsRoot, "recovery-primary", "other.trx"));
        }

        fixture.MutateContract(contract =>
        {
            JsonObject lane = EvidenceJson.RequiredArray(
                contract,
                "results",
                GateReason.MachineResultsInvalid)[0]!.AsObject();
            lane[field] = field.Equals("trx", StringComparison.Ordinal)
                ? "recovery-primary/other.trx"
                : "recovery-primary/other.provenance.json";
            if (field.Equals("trx", StringComparison.Ordinal))
            {
                lane["artifactLocator"] = "file:recovery-primary/other.trx";
            }
        });

        GateIssue issue = fixture.Validate().Issues.Single();
        issue.ReasonCode.ShouldBe(GateReason.PrimaryPathNotExecuted);
        issue.Subject.ShouldBe("recovery");
    }

    /// <summary>Proves a complete recovery declaration produces the destructive plan only after strict preflight.</summary>
    [Fact]
    public static void CompletionProductionPlannerShouldAuthorizeOneValidRecoveryProducer()
    {
        using GateFixture fixture = new();
        fixture.UseProductCompletion();
        fixture.UseClaimPrimaryPath("recovery", "recovery-primary");
        fixture.CommitStrictImmutableCompletion();

        CompletionProductionPlan plan = CompletionProductionPlanner.Plan(
            fixture.RepositoryRoot,
            fixture.PolicyPath,
            fixture.BaseCommit,
            fixture.HeadCommit,
            fixture.ResultsRoot);

        plan.RequiresRecovery.ShouldBeTrue();
        plan.RequiresTopology.ShouldBeFalse();
        plan.RetainedLocators.ShouldBeEmpty();
    }

    /// <summary>Proves malformed secondary lanes cannot survive preflight and authorize the valid recovery lane.</summary>
    [Fact]
    public static void CompletionProductionPlannerShouldRejectMalformedSecondaryLaneBeforeRecovery()
    {
        using GateFixture fixture = new();
        fixture.UseProductCompletion();
        fixture.UseClaimPrimaryPath("recovery", "recovery-primary");
        fixture.MutateContract(contract => EvidenceJson.RequiredArray(
            contract,
            "results",
            GateReason.MachineResultsInvalid).Add(new JsonObject
            {
                ["lane"] = "malformed-secondary",
                ["trx"] = "secondary/result.trx",
                ["provenance"] = "secondary/result.provenance.json",
                ["artifactLocator"] = "file:wrong-result.trx",
                ["source"] = "current-run",
                ["selectors"] = new JsonArray("class:GateFixture"),
                ["allowSkipped"] = false,
                ["primaryPathClass"] = null,
            }), refreshEvidence: false);
        fixture.CommitStrictImmutableCompletion(attest: false);

        GateValidationException exception = Should.Throw<GateValidationException>(() =>
            CompletionProductionPlanner.Plan(
                fixture.RepositoryRoot,
                fixture.PolicyPath,
                fixture.BaseCommit,
                fixture.HeadCommit,
                fixture.ResultsRoot));

        exception.ReasonCode.ShouldBe(GateReason.EvidenceStaleOrUnbound);
        exception.Subject.ShouldBe("malformed-secondary");
    }

    /// <summary>Proves duplicate recovery declarations fail closed before a producer can be started.</summary>
    [Fact]
    public static void CompletionProductionPlannerShouldRejectRecoveryMultiplicity()
    {
        using GateFixture fixture = new();
        fixture.UseProductCompletion();
        fixture.UseClaimPrimaryPath("recovery", "recovery-primary");
        fixture.MutateContract(contract => EvidenceJson.RequiredArray(
            contract,
            "results",
            GateReason.MachineResultsInvalid).Add(new JsonObject
            {
                ["lane"] = "recovery-primary",
                ["trx"] = "recovery-primary/duplicate.trx",
                ["provenance"] = "recovery-primary/duplicate.provenance.json",
                ["artifactLocator"] = "file:recovery-primary/duplicate.trx",
                ["source"] = "current-run",
                ["selectors"] = new JsonArray(
                    "class:Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests"),
                ["allowSkipped"] = false,
                ["primaryPathClass"] = "recovery",
            }), refreshEvidence: false);
        fixture.CommitStrictImmutableCompletion(attest: false);

        GateValidationException exception = Should.Throw<GateValidationException>(() =>
            CompletionProductionPlanner.Plan(
                fixture.RepositoryRoot,
                fixture.PolicyPath,
                fixture.BaseCommit,
                fixture.HeadCommit,
                fixture.ResultsRoot));

        exception.ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
        exception.Subject.ShouldBe("duplicate-lane");
    }

    /// <summary>Proves two lane fields cannot resolve to the same producer-owned result path.</summary>
    [Fact]
    public static void CompletionProductionPlannerShouldRejectResultPathCollisionBeforeRecovery()
    {
        using GateFixture fixture = new();
        fixture.UseProductCompletion();
        fixture.UseClaimPrimaryPath("recovery", "recovery-primary");
        fixture.MutateContract(contract => EvidenceJson.RequiredArray(
            contract,
            "results",
            GateReason.MachineResultsInvalid).Add(new JsonObject
            {
                ["lane"] = "colliding-secondary",
                ["trx"] = "recovery-primary/live-recovery-validation.trx",
                ["provenance"] = "secondary/result.provenance.json",
                ["artifactLocator"] = "file:recovery-primary/live-recovery-validation.trx",
                ["source"] = "current-run",
                ["selectors"] = new JsonArray("class:GateFixture"),
                ["allowSkipped"] = false,
                ["primaryPathClass"] = null,
            }), refreshEvidence: false);
        fixture.CommitStrictImmutableCompletion(attest: false);

        GateValidationException exception = Should.Throw<GateValidationException>(() =>
            CompletionProductionPlanner.Plan(
                fixture.RepositoryRoot,
                fixture.PolicyPath,
                fixture.BaseCommit,
                fixture.HeadCommit,
                fixture.ResultsRoot));

        exception.ReasonCode.ShouldBe(GateReason.EvidenceStaleOrUnbound);
        exception.Subject.ShouldBe("result-path-collision");
    }

    /// <summary>Proves a triggered recovery class cannot authorize a differently bound lane.</summary>
    [Fact]
    public static void CompletionProductionPlannerShouldRejectWrongPrimaryBindingBeforeRecovery()
    {
        using GateFixture fixture = new();
        fixture.UseProductCompletion();
        fixture.UseClaimPrimaryPath("recovery", "recovery-primary");
        fixture.MutateContract(contract => EvidenceJson.RequiredArray(
            contract,
            "primaryPaths",
            GateReason.PrimaryPathNotExecuted)[0]!.AsObject()["lane"] = "wrong-primary", refreshEvidence: false);
        fixture.CommitStrictImmutableCompletion(attest: false);

        GateValidationException exception = Should.Throw<GateValidationException>(() =>
            CompletionProductionPlanner.Plan(
                fixture.RepositoryRoot,
                fixture.PolicyPath,
                fixture.BaseCommit,
                fixture.HeadCommit,
                fixture.ResultsRoot));

        exception.ReasonCode.ShouldBe(GateReason.PrimaryPathNotExecuted);
        exception.Subject.ShouldBe("recovery");
    }

    /// <summary>Proves retained locators are collected only after their repository and exact artifact path bind.</summary>
    [Fact]
    public static void CompletionProductionPlannerShouldCollectValidatedRetainedLocator()
    {
        using GateFixture fixture = new();
        fixture.UseProductCompletion();
        fixture.UseRetainedEvidence();
        fixture.CommitStrictImmutableCompletion(attest: false);

        CompletionProductionPlan plan = CompletionProductionPlanner.Plan(
            fixture.RepositoryRoot,
            fixture.PolicyPath,
            fixture.BaseCommit,
            fixture.HeadCommit,
            fixture.ResultsRoot);

        plan.RetainedLocators.ShouldBe(
        [
            "github-actions://Hexalith/Hexalith.ChatBot/runs/12345/artifacts/gate-evidence",
        ]);
        plan.RequiresRecovery.ShouldBeFalse();
    }

    /// <summary>Proves raw recovery diagnostics are removed before the completion TRX can be attested or uploaded.</summary>
    [Fact]
    public static void RecoveryTrxSanitizerShouldProjectOnlyBoundMetadata()
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"recovery-sanitize-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            string input = Path.Combine(temporaryRoot, "raw.trx");
            string output = Path.Combine(temporaryRoot, "sanitized.trx");
            DateTimeOffset finish = DateTimeOffset.UtcNow;
            File.WriteAllText(
                input,
                $"""
                <?xml version="1.0" encoding="utf-8"?>
                <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
                  <Times start="{finish.AddSeconds(-1):O}" finish="{finish:O}" />
                  <Results>
                    <UnitTestResult testId="recovery-1" outcome="Passed">
                      <Output><StdOut>Bearer tenant-secret-payload</StdOut></Output>
                    </UnitTestResult>
                  </Results>
                  <TestDefinitions>
                    <UnitTest id="recovery-1">
                      <TestMethod className="Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests" name="LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate" />
                    </UnitTest>
                  </TestDefinitions>
                  <ResultSummary outcome="Completed">
                    <Counters total="1" executed="1" passed="1" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" notExecuted="0" />
                  </ResultSummary>
                </TestRun>
                """);

            RecoveryTrxSanitizer.Sanitize(input, output);

            string sanitized = File.ReadAllText(output);
            sanitized.ShouldContain("LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate");
            sanitized.ShouldNotContain("Output");
            sanitized.ShouldNotContain("Bearer");
            sanitized.ShouldNotContain("secret");
            sanitized.ShouldNotContain("payload");
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Proves hostile or ambiguous TRX structures never become completion evidence.</summary>
    [Theory]
    [InlineData("dtd")]
    [InlineData("duplicate-result")]
    [InlineData("failed-outcome")]
    [InlineData("wrong-class")]
    [InlineData("counter-mismatch")]
    [InlineData("reversed-time")]
    [InlineData("foreign-structure")]
    [InlineData("summary-outcome")]
    [InlineData("broken-id-crosslink")]
    [InlineData("passed-but-run-aborted")]
    [InlineData("not-runnable")]
    [InlineData("disconnected")]
    [InlineData("pending")]
    public static void RecoveryTrxSanitizerShouldRejectAdversarialInput(string mutation)
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"recovery-sanitize-negative-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            DateTimeOffset finish = DateTimeOffset.UtcNow;
            string trx = RecoverySanitizerTrx(finish);
            trx = mutation switch
            {
                "dtd" => trx.Replace(
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?><!DOCTYPE TestRun [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]>",
                    StringComparison.Ordinal),
                "duplicate-result" => trx.Replace(
                    "</Results>",
                    "<UnitTestResult testId=\"recovery-2\" outcome=\"Passed\" /></Results>",
                    StringComparison.Ordinal),
                "failed-outcome" => trx.Replace("outcome=\"Passed\"", "outcome=\"Failed\"", StringComparison.Ordinal),
                "summary-outcome" => trx.Replace(
                    "<ResultSummary outcome=\"Completed\">",
                    "<ResultSummary outcome=\"Failed\">",
                    StringComparison.Ordinal),
                "broken-id-crosslink" => trx.Replace(
                    "<UnitTest id=\"recovery-1\">",
                    "<UnitTest id=\"recovery-9\">",
                    StringComparison.Ordinal),
                "passed-but-run-aborted" => trx.Replace(
                    "notExecuted=\"0\"",
                    "notExecuted=\"0\" passedButRunAborted=\"1\"",
                    StringComparison.Ordinal),
                "not-runnable" => trx.Replace(
                    "notExecuted=\"0\"",
                    "notExecuted=\"0\" notRunnable=\"2\"",
                    StringComparison.Ordinal),

                // The remaining fail-closed outcome counters. Without these rows, deleting either clause from
                // RecoveryTrxSanitizer left the suite green while a TRX carrying that counter became completion
                // evidence.
                "disconnected" => trx.Replace(
                    "notExecuted=\"0\"",
                    "notExecuted=\"0\" disconnected=\"1\"",
                    StringComparison.Ordinal),
                "pending" => trx.Replace(
                    "notExecuted=\"0\"",
                    "notExecuted=\"0\" pending=\"3\"",
                    StringComparison.Ordinal),
                "wrong-class" => trx.Replace(
                    "Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests",
                    "Hexalith.ChatBot.IntegrationTests.Recovery.ImpostorTests",
                    StringComparison.Ordinal),
                "counter-mismatch" => trx.Replace("passed=\"1\"", "passed=\"2\"", StringComparison.Ordinal),
                "reversed-time" => trx.Replace(
                    $"start=\"{finish.AddSeconds(-1):O}\" finish=\"{finish:O}\"",
                    $"start=\"{finish:O}\" finish=\"{finish.AddSeconds(-1):O}\"",
                    StringComparison.Ordinal),
                "foreign-structure" => trx.Replace(
                    "<Results>",
                    "<foreign:Results xmlns:foreign=\"urn:foreign\" /><Results>",
                    StringComparison.Ordinal),
                _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
            };
            string input = Path.Combine(temporaryRoot, "raw.trx");
            string output = Path.Combine(temporaryRoot, "sanitized.trx");
            File.WriteAllText(input, trx);

            GateValidationException exception = Should.Throw<GateValidationException>(() =>
                RecoveryTrxSanitizer.Sanitize(input, output));

            exception.ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
            exception.Subject.ShouldBe("recovery-sanitize");
            File.Exists(output).ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>A warning count is diagnostic metadata, not a non-passing test outcome.</summary>
    [Fact]
    public static void RecoveryTrxSanitizerShouldAcceptWarningsOnAnOtherwisePassingRun()
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"recovery-sanitize-warning-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            string input = Path.Combine(temporaryRoot, "raw.trx");
            string output = Path.Combine(temporaryRoot, "sanitized.trx");
            string trx = RecoverySanitizerTrx(DateTimeOffset.UtcNow).Replace(
                "notExecuted=\"0\"",
                "notExecuted=\"0\" warning=\"1\"",
                StringComparison.Ordinal);
            File.WriteAllText(input, trx);

            RecoveryTrxSanitizer.Sanitize(input, output);

            File.Exists(output).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>
    /// Proves the sanitizer's output is actually admitted by the reader that must consume it.
    /// </summary>
    /// <remarks>
    /// The two ends of the only completion path were never joined by a test: the sanitizer was asserted with
    /// string matches and the reader with fixture-written TRX files, so a shape mismatch would surface only after
    /// a multi-hour hosted run.
    /// </remarks>
    [Fact]
    public static void SanitizedRecoveryTrxShouldBeAcceptedByTheReaderThatConsumesIt()
    {
        // The completion path is: producer TRX -> RecoveryTrxSanitizer -> TrxEvidenceReader. Those two ends were
        // only ever asserted separately -- the sanitizer with string matches, the reader with fixture-written
        // files -- so a shape mismatch between them would surface only after a multi-hour hosted run.
        string resultsRoot = Path.Combine(Path.GetTempPath(), $"recovery-roundtrip-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(resultsRoot, "recovery-primary"));
        try
        {
            string raw = Path.Combine(resultsRoot, "raw.trx");
            string sanitized = Path.Combine(resultsRoot, "recovery-primary", "live-recovery-validation.trx");
            DateTimeOffset producedAtUtc = DateTimeOffset.UtcNow;
            File.WriteAllText(raw, RecoverySanitizerTrx(producedAtUtc));

            RecoveryTrxSanitizer.Sanitize(raw, sanitized);
            File.Delete(raw);

            JsonObject laneContract = new()
            {
                ["lane"] = "recovery-primary",
                ["trx"] = "recovery-primary/live-recovery-validation.trx",
                ["provenance"] = "recovery-primary/live-recovery-validation.provenance.json",
                ["artifactLocator"] = "file:recovery-primary/live-recovery-validation.trx",
                ["source"] = "current-run",
                ["selectors"] = new JsonArray(
                    "class:Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests"),
                ["allowSkipped"] = false,
                ["primaryPathClass"] = "recovery",
            };

            string checksum = TrxEvidenceReader.PreflightCurrentRun(
                laneContract,
                resultsRoot,
                "Hexalith/Hexalith.ChatBot",
                60,
                5,
                producedAtUtc);

            checksum.Length.ShouldBe(64, "the reader must admit the sanitizer's output and checksum it.");
        }
        finally
        {
            Directory.Delete(resultsRoot, recursive: true);
        }
    }

    /// <summary>Proves a failed or absent producer attempt still yields a metadata-only retained record.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public static void RecoveryAttemptSummarizerShouldRetainMetadataForAFailedProducer(bool trxPresent)
    {
        using GateFixture fixture = new();
        JsonObject policy = EvidenceJson.LoadPolicy(fixture.PolicyPath);
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"recovery-summary-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            string input = Path.Combine(temporaryRoot, "raw.trx");
            string output = Path.Combine(temporaryRoot, "summary.json");
            if (trxPresent)
            {
                File.WriteAllText(
                    input,
                    RecoverySanitizerTrx(DateTimeOffset.UtcNow)
                        .Replace("outcome=\"Passed\"", "outcome=\"Failed\"", StringComparison.Ordinal)
                        .Replace(
                            "<UnitTestResult testId=\"recovery-1\" outcome=\"Failed\" />",
                            "<UnitTestResult testId=\"recovery-1\" outcome=\"Failed\"><Output><StdOut>Bearer tenant-secret-payload</StdOut></Output></UnitTestResult>",
                            StringComparison.Ordinal)
                        .Replace(
                            "className=\"Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests\"",
                            "className=\"tenant-secret-payload\"",
                            StringComparison.Ordinal)
                        .Replace(
                            "name=\"LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate\"",
                            "name=\"credential=private-value\"",
                            StringComparison.Ordinal));
            }

            RecoveryAttemptSummarizer.Summarize(input, "failure", output, policy);

            string summary = File.ReadAllText(output);
            summary.ShouldContain("live-recovery-attempt-summary");
            summary.ShouldContain("\"producerOutcome\": \"failure\"");
            summary.ShouldContain(trxPresent ? "\"trxPresent\": true" : "\"trxPresent\": false");
            summary.ShouldContain(trxPresent ? "\"trxState\": \"parsed\"" : "\"trxState\": \"absent\"");
            summary.ShouldNotContain("Bearer");
            summary.ShouldNotContain("secret");
            summary.ShouldNotContain("payload");
            summary.ShouldNotContain("StdOut");
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Proves an unparseable outcome token cannot reach the retained record verbatim.</summary>
    [Fact]
    public static void RecoveryAttemptSummarizerShouldNormalizeAnUnknownOutcome()
    {
        using GateFixture fixture = new();
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"recovery-summary-token-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            string output = Path.Combine(temporaryRoot, "summary.json");
            RecoveryAttemptSummarizer.Summarize(
                Path.Combine(temporaryRoot, "absent.trx"),
                "$(curl evil)",
                output,
                EvidenceJson.LoadPolicy(fixture.PolicyPath));

            string summary = File.ReadAllText(output);
            summary.ShouldContain("\"producerOutcome\": \"unknown\"");
            summary.ShouldNotContain("curl");
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Proves malformed input, zero counters, malformed counters, and truncated methods remain distinct.</summary>
    [Fact]
    public static void RecoveryAttemptSummarizerShouldDescribeMalformedAndTruncatedInputPrecisely()
    {
        using GateFixture fixture = new();
        JsonObject policy = EvidenceJson.LoadPolicy(fixture.PolicyPath);
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"recovery-summary-state-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            string malformedInput = Path.Combine(temporaryRoot, "malformed.trx");
            string malformedOutput = Path.Combine(temporaryRoot, "malformed.json");
            File.WriteAllText(malformedInput, "not xml");
            RecoveryAttemptSummarizer.Summarize(malformedInput, "failure", malformedOutput, policy);
            string malformed = File.ReadAllText(malformedOutput);
            malformed.ShouldContain("\"trxPresent\": true");
            malformed.ShouldContain("\"trxState\": \"malformed\"");

            string input = Path.Combine(temporaryRoot, "raw.trx");
            string output = Path.Combine(temporaryRoot, "summary.json");
            string extraMethods = string.Join(
                string.Empty,
                Enumerable.Range(0, 32).Select(index =>
                    $"<UnitTest id=\"extra-{index}\"><TestMethod className=\"Safe.Class{index}\" name=\"SafeMethod{index}\" /></UnitTest>"));
            File.WriteAllText(
                input,
                RecoverySanitizerTrx(DateTimeOffset.UtcNow)
                    .Replace("notExecuted=\"0\"", "notExecuted=\"0\" warning=\"malformed\"", StringComparison.Ordinal)
                    .Replace("</TestDefinitions>", $"{extraMethods}</TestDefinitions>", StringComparison.Ordinal));

            RecoveryAttemptSummarizer.Summarize(input, "failure", output, policy);

            string parsed = File.ReadAllText(output);
            parsed.ShouldContain("\"trxState\": \"parsed\"");
            parsed.ShouldContain("\"failed\": 0");
            parsed.ShouldContain("\"warning\": null");
            parsed.ShouldContain("\"testMethodCount\": 33");
            parsed.ShouldContain("\"testsTruncated\": true");
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Proves summary retention obeys the policy bound and never masks a producer failure on write errors.</summary>
    [Fact]
    public static void RecoveryAttemptSummarizerShouldUseMetadataPolicyAndRemainBestEffort()
    {
        using GateFixture fixture = new();
        JsonObject policy = EvidenceJson.LoadPolicy(fixture.PolicyPath);
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"recovery-summary-policy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            // A directory cannot be opened as the output file; the producer failure must still remain primary.
            RecoveryAttemptSummarizer.Summarize("absent.trx", "failure", temporaryRoot, policy);

            JsonObject strictPolicy = (JsonObject)policy.DeepClone();
            EvidenceJson.RequiredObject(
                strictPolicy,
                "metadataOnly",
                GateReason.EvidencePayloadForbidden)["maximumStringLength"] = 8;
            string rejectedOutput = Path.Combine(temporaryRoot, "rejected.json");
            RecoveryAttemptSummarizer.Summarize("absent.trx", "failure", rejectedOutput, strictPolicy);

            File.Exists(rejectedOutput).ShouldBeFalse(
                "the retained record must pass the policy's metadata-only validator before it is written");
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Proves byte and character admission limits cannot disagree for an ASCII TRX.</summary>
    [Fact]
    public static void RecoveryTrxSanitizerSizeBoundsShouldBeConsistent()
        => RecoveryTrxSanitizer.MaximumTrxCharacters.ShouldBe(RecoveryTrxSanitizer.MaximumTrxBytes);

    /// <summary>Proves a lane may raise its own current-run ceiling but never lower the global floor.</summary>
    [Fact]
    public static void PerLaneCurrentRunCeilingShouldRaiseOnlyForDeclaringLanes()
    {
        using GateFixture fixture = new();
        JsonObject policy = EvidenceJson.LoadPolicy(fixture.PolicyPath);

        EvidenceJson.ResolveCurrentRunAgeMinutes(policy, "recovery-primary").ShouldBe(360);
        EvidenceJson.ResolveCurrentRunAgeMinutes(policy, "story-evidence-gate").ShouldBe(60);
    }

    /// <summary>Proves both production consumers apply the lane ceiling rather than the flat global value.</summary>
    [Fact]
    public static void RecoveryLaneCeilingShouldFlowThroughAttestationAndValidation()
    {
        using GateFixture fixture = new();
        fixture.UseClaimPrimaryPath("recovery", "recovery-primary");
        DateTimeOffset evaluationTime = DateTimeOffset.UtcNow;
        fixture.WriteTrx(
            1,
            1,
            1,
            0,
            0,
            "Completed",
            "Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests.ValidAssertion",
            evaluationTime.AddMinutes(-120));

        fixture.Attest();
        GateReport report = fixture.Validate(evaluationTime);

        report.Passed.ShouldBeTrue(
            string.Join(", ", report.Issues.Select(static issue => $"{issue.ReasonCode}:{issue.Subject}")));
    }

    /// <summary>Proves duplicate lane bindings cannot disagree by declaring and omitting an override.</summary>
    [Fact]
    public static void PerLaneCurrentRunCeilingShouldRejectPresentVersusAbsentOverrides()
    {
        using GateFixture fixture = new();
        JsonObject policy = EvidenceJson.LoadPolicy(fixture.PolicyPath);
        JsonArray triggers = EvidenceJson.RequiredArray(
            policy,
            "primaryPathTriggers",
            GateReason.ScopeDigestMismatch);
        JsonObject recoveryBinding = triggers
            .SelectMany(static trigger => EvidenceJson.RequiredArray(
                (JsonObject)trigger!,
                "recognizedLaneBindings",
                GateReason.ScopeDigestMismatch))
            .OfType<JsonObject>()
            .Single(static binding => binding["lane"]!.GetValue<string>() == "recovery-primary");
        JsonObject missingOverride = (JsonObject)recoveryBinding.DeepClone();
        _ = missingOverride.Remove("maximumCurrentRunAgeMinutes");
        EvidenceJson.RequiredArray(
            (JsonObject)triggers[0]!,
            "recognizedLaneBindings",
            GateReason.ScopeDigestMismatch).Add(missingOverride);

        GateValidationException exception = Should.Throw<GateValidationException>(
            () => EvidenceJson.ResolveCurrentRunAgeMinutes(policy, "recovery-primary"));

        exception.Subject.ShouldBe("lane-current-run-age-presence");
    }

    /// <summary>Proves a lane ceiling below the global floor, or an absurd one, fails closed.</summary>
    [Theory]
    [InlineData(30, "lane-current-run-age-below-global")]
    [InlineData(4321, "lane-current-run-age-above-maximum")]
    public static void PerLaneCurrentRunCeilingShouldRejectOutOfRangeOverrides(int minutes, string expectedSubject)
    {
        using GateFixture fixture = new();
        fixture.MutatePolicy(policy =>
        {
            foreach (JsonNode? trigger in EvidenceJson.RequiredArray(
                policy,
                "primaryPathTriggers",
                GateReason.ScopeDigestMismatch))
            {
                foreach (JsonNode? binding in EvidenceJson.RequiredArray(
                    (JsonObject)trigger!,
                    "recognizedLaneBindings",
                    GateReason.ScopeDigestMismatch))
                {
                    ((JsonObject)binding!)["maximumCurrentRunAgeMinutes"] = minutes;
                }
            }
        });

        JsonObject mutated = EvidenceJson.LoadPolicy(fixture.PolicyPath);

        GateValidationException exception = Should.Throw<GateValidationException>(
            () => EvidenceJson.ResolveCurrentRunAgeMinutes(mutated, "recovery-primary"));
        exception.Subject.ShouldBe(expectedSubject);
    }

    /// <summary>Proves two declared values for the same lane identify disagreement distinctly.</summary>
    [Fact]
    public static void PerLaneCurrentRunCeilingShouldIdentifyValueDisagreement()
    {
        using GateFixture fixture = new();
        JsonObject policy = EvidenceJson.LoadPolicy(fixture.PolicyPath);
        JsonArray triggers = EvidenceJson.RequiredArray(policy, "primaryPathTriggers", GateReason.ScopeDigestMismatch);
        JsonObject recoveryBinding = triggers
            .SelectMany(static trigger => EvidenceJson.RequiredArray(
                (JsonObject)trigger!,
                "recognizedLaneBindings",
                GateReason.ScopeDigestMismatch))
            .OfType<JsonObject>()
            .Single(static binding => binding["lane"]!.GetValue<string>() == "recovery-primary");
        JsonObject disagreement = (JsonObject)recoveryBinding.DeepClone();
        disagreement["maximumCurrentRunAgeMinutes"] = 359;
        EvidenceJson.RequiredArray(
            (JsonObject)triggers[0]!,
            "recognizedLaneBindings",
            GateReason.ScopeDigestMismatch).Add(disagreement);

        GateValidationException exception = Should.Throw<GateValidationException>(
            () => EvidenceJson.ResolveCurrentRunAgeMinutes(policy, "recovery-primary"));

        exception.Subject.ShouldBe("lane-current-run-age-disagreement");
    }

    /// <summary>Proves the declared compatibility floor is evaluated before exact policy pinning.</summary>
    [Fact]
    public static void PolicyMinimumSupportedVersionShouldRejectANewerFloor()
    {
        using GateFixture fixture = new();
        JsonObject policy = EvidenceJson.LoadPolicy(fixture.PolicyPath);
        policy["minimumSupportedVersion"] = "2.2";

        GateValidationException exception = Should.Throw<GateValidationException>(
            () => StoryEvidenceValidator.ValidatePinnedPolicy(policy));

        exception.Subject.ShouldBe("minimum-supported-version");
    }

    /// <summary>Proves the checked-in policy and the gate implementation declare one supported version.</summary>
    [Fact]
    public static void PolicyVersionShouldMatchTheGateImplementation()
    {
        using GateFixture fixture = new();
        JsonObject policy = EvidenceJson.LoadPolicy(fixture.PolicyPath);

        EvidenceJson.RequiredString(policy, "schemaVersion", GateReason.ScopeDigestMismatch)
            .ShouldBe(StoryEvidenceValidator.SupportedPolicyVersion);
        EvidenceJson.RequiredString(policy, "minimumSupportedVersion", GateReason.ScopeDigestMismatch)
            .ShouldBe(StoryEvidenceValidator.SupportedPolicyVersion);
    }

    /// <summary>Proves a transition-declared current-run topology lane requires its producer.</summary>
    [Fact]
    public static void CompletionProductionPlannerShouldRequireTopologyForAspireDaprPrimary()
    {
        using GateFixture fixture = new();
        fixture.UseProductCompletion();
        fixture.UseClaimPrimaryPath("aspire-dapr", "aspire-dapr-primary");
        fixture.CommitStrictImmutableCompletion();

        CompletionProductionPlan plan = CompletionProductionPlanner.Plan(
            fixture.RepositoryRoot,
            fixture.PolicyPath,
            fixture.BaseCommit,
            fixture.HeadCommit,
            fixture.ResultsRoot);

        plan.RequiresTopology.ShouldBeTrue();
        plan.RequiresRecovery.ShouldBeFalse();
    }

    /// <summary>Proves a mapping naming an unchanged path cannot authorize the destructive producer.</summary>
    [Fact]
    public static void CompletionProductionPlannerShouldRejectUnchangedMappingPathBeforeRecovery()
    {
        using GateFixture fixture = new();
        fixture.UseProductCompletion();
        fixture.UseClaimPrimaryPath("recovery", "recovery-primary");
        fixture.MutateContract(
            contract =>
            {
                JsonArray mappings = EvidenceJson.RequiredArray(
                    contract,
                    "mappings",
                    GateReason.CheckedItemEvidenceMismatch);
                ((JsonObject)mappings[0]!)["paths"] = new JsonArray("src/Never/Changed/Here.cs");
            },
            refreshEvidence: false);
        fixture.CommitStrictImmutableCompletion(attest: false);

        GateValidationException exception = Should.Throw<GateValidationException>(() =>
            CompletionProductionPlanner.Plan(
                fixture.RepositoryRoot,
                fixture.PolicyPath,
                fixture.BaseCommit,
                fixture.HeadCommit,
                fixture.ResultsRoot));

        exception.ReasonCode.ShouldBe(GateReason.CheckedItemEvidenceMismatch);
    }

    private static string RecoverySanitizerTrx(DateTimeOffset finish) =>
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
          <Times start="{finish.AddSeconds(-1):O}" finish="{finish:O}" />
          <Results>
            <UnitTestResult testId="recovery-1" outcome="Passed" />
          </Results>
          <TestDefinitions>
            <UnitTest id="recovery-1">
              <TestMethod className="Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests" name="LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate" />
            </UnitTest>
          </TestDefinitions>
          <ResultSummary outcome="Completed">
            <Counters total="1" executed="1" passed="1" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" notExecuted="0" />
          </ResultSummary>
        </TestRun>
        """;

    /// <summary>Proves a Server *Dapr* path no longer forces the aspire-dapr primary lane.</summary>
    [Fact]
    public static void ServerDaprNamedFileShouldNotTriggerAspireDaprPrimary()
    {
        using GateFixture fixture = new();
        fixture.AddOwnedFile(
            "src/Hexalith.ChatBot.Server/Operations/DaprAppIdHandler.cs",
            "namespace Hexalith.ChatBot.Server.Operations;\n\ninternal static class DaprAppIdHandler;\n");

        GateReport report = fixture.Validate();
        report.Passed.ShouldBeTrue(string.Join(", ", report.Issues.Select(static issue => $"{issue.ReasonCode}:{issue.Subject}")));
        report.PrimaryPaths.ShouldBeEmpty();
    }

    /// <summary>Proves retained locators bind repository identity to policy rather than ambient Git state.</summary>
    [Fact]
    public static void WrongRetainedRepositoryShouldFailWithProvenanceReason()
    {
        using GateFixture fixture = new();
        fixture.UseRetainedEvidence();
        fixture.SetRetainedRepository("Hexalith/Other.Repository");

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.EvidenceStaleOrUnbound);
    }

    /// <summary>Proves the validate CLI returns exit code 0 and JSON on a green fixture.</summary>
    [Fact]
    public static void ValidateCommandShouldReturnZeroOnSuccess()
    {
        using GateFixture fixture = new();
        int exitCode = InvokeMain(
            fixture,
            out string stdout,
            "validate",
            "--repository-root", fixture.RepositoryRoot,
            "--policy", fixture.PolicyPath,
            "--story", fixture.StoryPath,
            "--contract", fixture.ContractPath,
            "--results", fixture.ResultsRoot,
            "--target-status", "done",
            "--base", fixture.BaseCommit,
            "--head", fixture.HeadCommit);

        exitCode.ShouldBe(0);
        stdout.ShouldContain("\"passed\": true");
    }

    /// <summary>Proves the validate CLI returns exit code 1 when validation fails closed.</summary>
    [Fact]
    public static void ValidateCommandShouldReturnOneOnValidationFailure()
    {
        using GateFixture fixture = new();
        fixture.MutateContract(contract => contract["persistedStatus"] = "wrong", refreshEvidence: false);
        int exitCode = InvokeMain(
            fixture,
            out string stdout,
            "validate",
            "--repository-root", fixture.RepositoryRoot,
            "--policy", fixture.PolicyPath,
            "--story", fixture.StoryPath,
            "--contract", fixture.ContractPath,
            "--results", fixture.ResultsRoot,
            "--target-status", "done",
            "--base", fixture.BaseCommit,
            "--head", fixture.HeadCommit);

        exitCode.ShouldBe(1);
        stdout.ShouldContain("\"passed\": false");
    }

    /// <summary>Proves malformed CLI arguments return exit code 2 with a metadata-only report.</summary>
    [Fact]
    public static void UnknownCommandShouldReturnTwoWithMetadataOnlyReport()
    {
        using GateFixture fixture = new();
        int exitCode = InvokeMain(fixture, out string stdout, "not-a-command");

        exitCode.ShouldBe(2);
        stdout.ShouldContain("\"passed\": false");
        stdout.ShouldContain(GateReason.StatusMismatch);
    }

    /// <summary>Proves every command rejects options outside its explicit allowlist.</summary>
    [Theory]
    [InlineData("validate")]
    [InlineData("attest")]
    [InlineData("detect")]
    [InlineData("ci")]
    public static void UnknownCommandOptionShouldReturnTwoWithMetadataOnlyReport(string command)
    {
        using GateFixture fixture = new();
        int exitCode = InvokeMain(
            fixture,
            out string stdout,
            command,
            "--foreign-option", "sensitive-value");

        exitCode.ShouldBe(2);
        stdout.ShouldContain(GateReason.StatusMismatch);
        stdout.ShouldNotContain("sensitive-value");
    }

    /// <summary>Proves the protected CI entry point succeeds without consuming inactive evidence.</summary>
    [Fact]
    public static void CiCommandNoTransitionShouldPass()
    {
        using GateFixture fixture = new();
        string reports = Path.Combine(fixture.ResultsRoot, "reports-no-transition");
        int exitCode = InvokeMain(
            fixture,
            out string stdout,
            "ci",
            "--repository-root", fixture.RepositoryRoot,
            "--base", fixture.BaseCommit,
            "--head", fixture.BaseCommit,
            "--results", fixture.ResultsRoot,
            "--report-directory", reports);

        exitCode.ShouldBe(0);
        stdout.ShouldContain("no-transition");
        File.Exists(Path.Combine(reports, "no-transition.json")).ShouldBeTrue();
    }

    /// <summary>Proves no-transition CI still validates the complete pinned policy before passing.</summary>
    [Fact]
    public static void CiCommandPolicyWeakeningShouldFailBeforeNoTransitionReport()
    {
        using GateFixture fixture = new();
        fixture.MutatePolicy(policy => policy["maximumFutureClockSkewMinutes"] = 60);
        string reports = Path.Combine(fixture.ResultsRoot, "reports-invalid-policy");

        int exitCode = InvokeMain(
            fixture,
            out string stdout,
            "ci",
            "--repository-root", fixture.RepositoryRoot,
            "--policy", fixture.PolicyPath,
            "--base", fixture.BaseCommit,
            "--head", fixture.BaseCommit,
            "--results", fixture.ResultsRoot,
            "--report-directory", reports);

        exitCode.ShouldBe(2);
        stdout.ShouldContain(GateReason.ScopeDigestMismatch);
        File.Exists(Path.Combine(reports, "no-transition.json")).ShouldBeFalse();
    }

    /// <summary>Proves the protected CI entry point attests and accepts an exact snapshot lifecycle event.</summary>
    [Fact]
    public static void CiCommandValidSnapshotTransitionShouldPass()
    {
        using GateFixture fixture = new();
        fixture.UseSnapshotCompletion();
        string reports = Path.Combine(fixture.ResultsRoot, "reports-valid");
        int exitCode = InvokeMain(
            fixture,
            out string stdout,
            "ci",
            "--repository-root", fixture.RepositoryRoot,
            "--policy", fixture.PolicyPath,
            "--base", fixture.BaseCommit,
            "--head", fixture.HeadCommit,
            "--results", fixture.ResultsRoot,
            "--report-directory", reports);

        exitCode.ShouldBe(0);
        stdout.ShouldContain("\"eventPathCount\": 4");
        File.Exists(Path.Combine(reports, "gate-fixture.json")).ShouldBeTrue();
    }

    /// <summary>Proves a later invalid contract cannot partially attest an earlier valid contract.</summary>
    [Fact]
    public static void CiCommandShouldPreflightAllActiveContractsBeforeAnySidecarWrite()
    {
        using GateFixture fixture = new();
        PrepareSecondSnapshotContract(fixture, malformedTrx: true, sharedProvenance: false);
        byte[] firstBefore = File.ReadAllBytes(fixture.ProvenancePath);
        string secondProvenance = Path.Combine(fixture.ResultsRoot, "gate-second.provenance.json");
        string reports = Path.Combine(fixture.ResultsRoot, "reports-two-contracts");
        string summary = Path.Combine(fixture.ResultsRoot, "two-contract-summary.md");
        string? originalSummary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");

        int exitCode;
        string stdout;
        try
        {
            Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", summary);
            exitCode = InvokeMain(
                fixture,
                out stdout,
                "ci",
                "--repository-root", fixture.RepositoryRoot,
                "--policy", fixture.PolicyPath,
                "--base", fixture.BaseCommit,
                "--head", fixture.HeadCommit,
                "--results", fixture.ResultsRoot,
                "--report-directory", reports);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", originalSummary);
        }

        exitCode.ShouldBe(1);
        stdout.ShouldContain("gate-fixture");
        stdout.ShouldContain("gate-fixture-second");
        File.Exists(Path.Combine(reports, "gate-fixture.json")).ShouldBeTrue();
        File.Exists(Path.Combine(reports, "gate-fixture-second.json")).ShouldBeTrue();
        File.ReadAllText(summary).ShouldContain("gate-fixture-second");
        File.ReadAllBytes(fixture.ProvenancePath).ShouldBe(firstBefore);
        File.Exists(secondProvenance).ShouldBeFalse();
    }

    /// <summary>Proves active CI contracts cannot share one provenance destination.</summary>
    [Fact]
    public static void CiCommandShouldRejectCrossContractResultPathCollisionBeforeWrites()
    {
        using GateFixture fixture = new();
        PrepareSecondSnapshotContract(fixture, malformedTrx: false, sharedProvenance: true);
        byte[] firstBefore = File.ReadAllBytes(fixture.ProvenancePath);
        string reports = Path.Combine(fixture.ResultsRoot, "reports-collision");

        int exitCode = InvokeMain(
            fixture,
            out string stdout,
            "ci",
            "--repository-root", fixture.RepositoryRoot,
            "--policy", fixture.PolicyPath,
            "--base", fixture.BaseCommit,
            "--head", fixture.HeadCommit,
            "--results", fixture.ResultsRoot,
            "--report-directory", reports);

        exitCode.ShouldBe(1);
        stdout.ShouldContain("result-path-collision");
        File.ReadAllBytes(fixture.ProvenancePath).ShouldBe(firstBefore);
        File.Exists(Path.Combine(reports, "gate-fixture.json")).ShouldBeTrue();
        File.Exists(Path.Combine(reports, "gate-fixture-second.json")).ShouldBeTrue();
    }

    /// <summary>Proves the protected CI entry point rejects an extra lifecycle event path.</summary>
    [Fact]
    public static void CiCommandInvalidSnapshotTransitionShouldFail()
    {
        using GateFixture fixture = new();
        fixture.UseSnapshotCompletion(() => File.AppendAllText(fixture.SourcePath, "unauthorized\n"));
        int exitCode = InvokeMain(
            fixture,
            out string stdout,
            "ci",
            "--repository-root", fixture.RepositoryRoot,
            "--policy", fixture.PolicyPath,
            "--base", fixture.BaseCommit,
            "--head", fixture.HeadCommit,
            "--results", fixture.ResultsRoot,
            "--report-directory", Path.Combine(fixture.ResultsRoot, "reports-invalid"));

        exitCode.ShouldBe(1);
        stdout.ShouldContain(GateReason.StatusMismatch);
    }

    /// <summary>Proves detect writes JSON transitions and returns exit code 0.</summary>
    [Fact]
    public static void DetectCommandShouldReturnZeroWithTransitionJson()
    {
        using GateFixture fixture = new();
        int exitCode = InvokeMain(
            fixture,
            out string stdout,
            "detect",
            "--repository-root", fixture.RepositoryRoot,
            "--base", fixture.BaseCommit,
            "--head", fixture.HeadCommit);

        exitCode.ShouldBe(0);
        stdout.TrimStart().ShouldStartWith("[");
    }

    /// <summary>Proves the side-effect-free planner emits only validated producer requirements.</summary>
    [Fact]
    public static void PlanCommandNoTransitionShouldReturnEmptyRequirements()
    {
        using GateFixture fixture = new();
        string output = Path.Combine(fixture.ResultsRoot, "production-plan.json");
        int exitCode = InvokeMain(
            fixture,
            out string stdout,
            "plan",
            "--repository-root", fixture.RepositoryRoot,
            "--policy", fixture.PolicyPath,
            "--base", fixture.BaseCommit,
            "--head", fixture.HeadCommit,
            "--results", fixture.ResultsRoot,
            "--output", output);

        exitCode.ShouldBe(0);
        stdout.ShouldContain("\"requiresTopology\": false");
        stdout.ShouldContain("\"requiresRecovery\": false");
        stdout.ShouldContain("\"retainedLocators\": []");
        File.Exists(output).ShouldBeTrue();
    }

    /// <summary>Proves attest honors --policy and returns a success envelope.</summary>
    [Fact]
    public static void AttestCommandShouldHonorPolicyOption()
    {
        using GateFixture fixture = new();
        int exitCode = InvokeMain(
            fixture,
            out string stdout,
            "attest",
            "--repository-root", fixture.RepositoryRoot,
            "--policy", fixture.PolicyPath,
            "--contract", fixture.ContractPath,
            "--results", fixture.ResultsRoot,
            "--base", fixture.BaseCommit,
            "--head", fixture.HeadCommit);

        exitCode.ShouldBe(0);
        stdout.ShouldContain("\"operation\":\"attest\"");
    }

    /// <summary>Proves InvalidOperationException paths emit a fail-closed metadata report.</summary>
    [Fact]
    public static void DetectCommandShouldFailClosedWhenOutputHasNoParent()
    {
        using GateFixture fixture = new();
        int exitCode = InvokeMain(
            fixture,
            out string stdout,
            "detect",
            "--repository-root", fixture.RepositoryRoot,
            "--base", fixture.BaseCommit,
            "--head", fixture.HeadCommit,
            "--output", Path.GetPathRoot(fixture.RepositoryRoot)!);

        exitCode.ShouldBe(2);
        stdout.ShouldContain("\"passed\": false");
        stdout.ShouldContain("io-or-process");
    }

    private static int InvokeMain(GateFixture fixture, out string stdout, params string[] args)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        TextWriter original = Console.Out;
        using StringWriter buffer = new();
        Console.SetOut(buffer);
        try
        {
            int exitCode = Program.Main(args);
            stdout = buffer.ToString();
            return exitCode;
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    private static void PrepareSecondSnapshotContract(
        GateFixture fixture,
        bool malformedTrx,
        bool sharedProvenance)
    {
        fixture.UseSnapshotBootstrap();
        const string SecondStoryRelative = "_bmad-output/implementation-artifacts/spec-gate-fixture-second.md";
        const string SecondContractRelative =
            "_bmad-output/implementation-artifacts/evidence/gate-fixture-second.json";
        File.AppendAllText(
            fixture.StoryPath,
            $"- `{SecondStoryRelative}`\n- `{SecondContractRelative}`\n");
        fixture.MutateContract(contract =>
        {
            JsonObject repository = EvidenceJson.RequiredArray(
                EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch),
                "repositories",
                GateReason.ScopeDigestMismatch)[0]!.AsObject();
            EvidenceJson.RequiredArray(repository, "includePaths", GateReason.ScopeDigestMismatch).Add(SecondStoryRelative);
            EvidenceJson.RequiredArray(repository, "includePaths", GateReason.ScopeDigestMismatch).Add(SecondContractRelative);
        }, refreshEvidence: false);

        string secondStory = Path.Combine(fixture.RepositoryRoot, SecondStoryRelative);
        string secondContract = Path.Combine(fixture.RepositoryRoot, SecondContractRelative);
        File.WriteAllText(secondStory, File.ReadAllText(fixture.StoryPath));
        JsonObject clone = JsonNode.Parse(File.ReadAllText(fixture.ContractPath))!.AsObject();
        clone["storyKey"] = "gate-fixture-second";
        clone["storyPath"] = SecondStoryRelative;
        clone["reportPath"] =
            "_bmad-output/implementation-artifacts/evidence/reports/gate-fixture-second.json";
        EvidenceJson.RequiredObject(clone, "scope", GateReason.ScopeDigestMismatch)["transitionPaths"] = new JsonArray(
            SecondStoryRelative,
            SecondContractRelative,
            "_bmad-output/planning-artifacts/technical-enablers.md",
            "_bmad-output/implementation-artifacts/sprint-status.yaml");
        JsonObject result = EvidenceJson.RequiredArray(clone, "results", GateReason.MachineResultsInvalid)[0]!.AsObject();
        result["lane"] = "gate-unit-second";
        result["trx"] = "gate-second.trx";
        result["provenance"] = sharedProvenance ? "gate.provenance.json" : "gate-second.provenance.json";
        result["artifactLocator"] = "file:gate-second.trx";
        File.WriteAllText(secondContract, clone.ToJsonString(JsonReportWriter.SerializerOptions));
        string secondTrx = Path.Combine(fixture.ResultsRoot, "gate-second.trx");
        File.Copy(fixture.TrxPath, secondTrx);
        if (malformedTrx)
        {
            File.WriteAllText(secondTrx, "not xml");
        }

        fixture.CommitAdditionalHead("test: add second active snapshot contract");
    }

    private static string RunGit(string repositoryPath, params string[] arguments)
    {
        ProcessStartInfo startInfo = new("git")
        {
            WorkingDirectory = repositoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Git failed to start.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(error);
        }

        return output;
    }
}
