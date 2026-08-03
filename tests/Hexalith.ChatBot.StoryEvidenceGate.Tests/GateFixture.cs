using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

using Hexalith.ChatBot.StoryEvidenceGate;

namespace Hexalith.ChatBot.StoryEvidenceGate.Tests;

/// <summary>
/// Creates an isolated synthetic Git repository, story, contract, TRX, and provenance set.
/// </summary>
internal sealed class GateFixture : IDisposable
{
    private const string ActionText = "Add a mechanical story-evidence integrity gate.";
    private readonly string _temporaryRoot;

    /// <summary>Initializes a valid technical-enabler bootstrap fixture.</summary>
    public GateFixture()
    {
        _temporaryRoot = Path.Combine(Path.GetTempPath(), $"story-evidence-gate-{Guid.NewGuid():N}");
        RepositoryRoot = Path.Combine(_temporaryRoot, "repository");
        ResultsRoot = Path.Combine(_temporaryRoot, "results");
        Directory.CreateDirectory(RepositoryRoot);
        Directory.CreateDirectory(ResultsRoot);
        RunGit("init", "--initial-branch=main");
        RunGit("config", "user.email", "gate@example.invalid");
        RunGit("config", "user.name", "Story Evidence Gate Tests");

        PolicyPath = Path.Combine(RepositoryRoot, "story-evidence-policy.json");
        File.Copy(FindPolicy(), PolicyPath);
        StoryPath = Path.Combine(RepositoryRoot, "_bmad-output", "implementation-artifacts", "spec-gate-fixture.md");
        SprintPath = Path.Combine(RepositoryRoot, "_bmad-output", "implementation-artifacts", "sprint-status.yaml");
        string technicalLedgerPath = Path.Combine(RepositoryRoot, "_bmad-output", "planning-artifacts", "technical-enablers.md");
        Directory.CreateDirectory(Path.GetDirectoryName(StoryPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(technicalLedgerPath)!);
        File.WriteAllText(StoryPath, "---\ntitle: 'Gate Fixture'\nstatus: 'in-progress'\n---\n\n# Baseline\n");
        File.WriteAllText(
            SprintPath,
            $"development_status:\naction_items:\n  - epic: 13\n    action: \"{ActionText}\"\n    owner: \"Amelia / Murat\"\n    status: open\n");
        File.WriteAllText(technicalLedgerPath, "# Technical Enablers\n\n## TE-X — Gate Fixture\n\n- **Status:** review; bootstrap pending.\n");
        RunGit("add", ".");
        RunGit("commit", "-m", "test: create fixture baseline");
        BaseCommit = RunGit("rev-parse", "HEAD").Trim();
        HeadCommit = BaseCommit;

        SourcePath = Path.Combine(RepositoryRoot, "src", "gate.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(SourcePath)!);
        File.WriteAllText(SourcePath, "gate implementation\n");
        File.WriteAllText(StoryPath, StoryText("src/gate.txt"));
        ContractPath = Path.Combine(
            RepositoryRoot,
            "_bmad-output",
            "implementation-artifacts",
            "evidence",
            "gate-fixture.json");
        Directory.CreateDirectory(Path.GetDirectoryName(ContractPath)!);
        WriteContract(CreateContract());
        TrxPath = Path.Combine(ResultsRoot, "gate.trx");
        ProvenancePath = Path.Combine(ResultsRoot, "gate.provenance.json");
        WritePassingTrx();
        RefreshEvidence();
    }

    /// <summary>Gets the fixture repository root.</summary>
    public string RepositoryRoot { get; }

    /// <summary>Gets the policy path.</summary>
    public string PolicyPath { get; }

    /// <summary>Gets the story path.</summary>
    public string StoryPath { get; }

    /// <summary>Gets the sprint ledger path.</summary>
    public string SprintPath { get; }

    /// <summary>Gets the changed source path.</summary>
    public string SourcePath { get; private set; }

    /// <summary>Gets the evidence contract path.</summary>
    public string ContractPath { get; }

    /// <summary>Gets the results root.</summary>
    public string ResultsRoot { get; }

    /// <summary>Gets the TRX path.</summary>
    public string TrxPath { get; private set; }

    /// <summary>Gets the provenance path.</summary>
    public string ProvenancePath { get; private set; }

    /// <summary>Gets the exact base revision.</summary>
    public string BaseCommit { get; private set; }

    /// <summary>Gets the exact head revision.</summary>
    public string HeadCommit { get; private set; }

    /// <summary>Validates the fixture.</summary>
    /// <param name="nowUtc">An optional evaluation clock.</param>
    /// <returns>The gate report.</returns>
    public GateReport Validate(DateTimeOffset? nowUtc = null, string? reportPath = null, string? resultsRoot = null)
    {
        return StoryEvidenceValidator.Validate(new GateOptions
        {
            RepositoryRoot = RepositoryRoot,
            PolicyPath = PolicyPath,
            StoryPath = StoryPath,
            ContractPath = ContractPath,
            TargetStatus = "done",
            BaseCommit = BaseCommit,
            HeadCommit = HeadCommit,
            ResultsRoot = resultsRoot ?? ResultsRoot,
            NowUtc = nowUtc ?? DateTimeOffset.UtcNow,
            ReportPath = reportPath,
        });
    }

    /// <summary>Changes the bootstrap story status and refreshes its evidence.</summary>
    public void SetBootstrapStoryStatus(string status)
    {
        File.WriteAllText(
            StoryPath,
            File.ReadAllText(StoryPath).Replace("status: 'in-progress'", $"status: '{status}'", StringComparison.Ordinal));
        RefreshEvidence();
    }

    /// <summary>Adds another owned file and reconciles it into scope, the File List, and a task mapping.</summary>
    public void AddOwnedFile(string relativePath, string content)
    {
        string normalized = relativePath.Replace('\\', '/');
        string fullPath = Path.Combine(RepositoryRoot, normalized.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        File.WriteAllText(StoryPath, StoryText("src/gate.txt", normalized));
        MutateContract(contract =>
        {
            JsonObject repository = EvidenceJson.RequiredArray(
                EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch),
                "repositories",
                GateReason.ScopeDigestMismatch)[0]!.AsObject();
            JsonArray includePaths = EvidenceJson.RequiredArray(repository, "includePaths", GateReason.ScopeDigestMismatch);
            includePaths.Add(normalized);
            JsonObject mapping = EvidenceJson.RequiredArray(
                contract,
                "mappings",
                GateReason.CheckedItemEvidenceMismatch)[0]!.AsObject();
            EvidenceJson.RequiredArray(mapping, "paths", GateReason.CheckedItemEvidenceMismatch).Add(normalized);
        });
    }

    /// <summary>Replaces the owned source bytes and refreshes the evidence.</summary>
    public void SetOwnedSourceText(string content)
    {
        File.WriteAllText(SourcePath, content);
        RefreshEvidence();
    }

    /// <summary>Marks the owned source executable on Unix and refreshes the evidence.</summary>
    public void SetOwnedSourceExecutable()
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                SourcePath,
                File.GetUnixFileMode(SourcePath) | UnixFileMode.UserExecute);
        }

        RefreshEvidence();
    }

    /// <summary>Replaces the owned source with a symbolic link whose target text is evidence.</summary>
    public void SetOwnedSourceSymlink(string linkTarget)
    {
        File.Delete(SourcePath);
        File.CreateSymbolicLink(SourcePath, linkTarget);
        RefreshEvidence();
    }

    /// <summary>Mutates the contract and optionally refreshes digest-bound evidence.</summary>
    /// <param name="mutation">The mutation.</param>
    /// <param name="refreshEvidence">Whether to refresh digest and provenance.</param>
    public void MutateContract(Action<JsonObject> mutation, bool refreshEvidence = true)
    {
        JsonObject contract = ReadContract();
        mutation(contract);
        WriteContract(contract);
        if (refreshEvidence)
        {
            RefreshEvidence();
        }
    }

    /// <summary>Mutates the provenance sidecar.</summary>
    /// <param name="mutation">The mutation.</param>
    public void MutateProvenance(Action<JsonObject> mutation)
    {
        JsonObject sidecar = JsonNode.Parse(File.ReadAllText(ProvenancePath))!.AsObject();
        mutation(sidecar);
        File.WriteAllText(ProvenancePath, sidecar.ToJsonString(JsonReportWriter.SerializerOptions));
    }

    /// <summary>Writes a TRX with the requested counters and outcomes.</summary>
    /// <param name="total">The total count.</param>
    /// <param name="executed">The executed count.</param>
    /// <param name="passed">The passed count.</param>
    /// <param name="failed">The failed count.</param>
    /// <param name="skipped">The not-executed count.</param>
    /// <param name="summaryOutcome">The summary outcome.</param>
    public void WriteTrx(
        int total,
        int executed,
        int passed,
        int failed,
        int skipped,
        string summaryOutcome,
        string testName = "GateFixture.ValidAssertion")
    {
        StringBuilder results = new();
        for (int index = 0; index < passed; index++)
        {
            string name = index == 0 ? testName : $"{testName}.Case{index + 1}";
            results.AppendLine($"    <UnitTestResult testName=\"{name}\" outcome=\"Passed\" />");
        }

        for (int index = 0; index < failed; index++)
        {
            results.AppendLine($"    <UnitTestResult testName=\"{testName}.Failed{index + 1}\" outcome=\"Failed\" />");
        }

        for (int index = 0; index < skipped; index++)
        {
            results.AppendLine($"    <UnitTestResult testName=\"{testName}.Skipped{index + 1}\" outcome=\"NotExecuted\" />");
        }

        File.WriteAllText(
            TrxPath,
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <Results>
            {results.ToString().TrimEnd()}
              </Results>
              <ResultSummary outcome="{summaryOutcome}">
                <Counters total="{total}" executed="{executed}" passed="{passed}" failed="{failed}" error="0" timeout="0" aborted="0" inconclusive="0" notExecuted="{skipped}" />
              </ResultSummary>
            </TestRun>
            """);
    }

    /// <summary>Refreshes the canonical digest and provenance sidecar.</summary>
    public void RefreshEvidence()
    {
        JsonObject contract = ReadContract();
        EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch)["implementationDigest"] = new string('0', 64);
        WriteContract(contract);
        JsonObject policy = EvidenceJson.LoadPolicy(PolicyPath);
        ScopeEvaluation evaluation = ScopeEvaluator.Evaluate(RepositoryRoot, policy, contract, BaseCommit, HeadCommit);
        contract = ReadContract();
        EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch)["implementationDigest"] = evaluation.Digest;
        WriteContract(contract);
        ProvenanceAttestor.AttestContract(
            RepositoryRoot,
            ContractPath,
            BaseCommit,
            HeadCommit,
            ResultsRoot,
            DateTimeOffset.UtcNow,
            PolicyPath);
    }

    /// <summary>Moves the changed source to a primary browser path and declares a lane.</summary>
    /// <param name="lane">The declared lane.</param>
    public void UseBrowserPrimaryPath(string lane)
    {
        UsePrimaryPath("src/Pages/View.razor", "browser", lane);
    }

    /// <summary>Moves the source into a triggered primary class and declares its satisfying lane.</summary>
    /// <param name="relativePath">The triggered relative path.</param>
    /// <param name="pathClass">The expected primary class.</param>
    /// <param name="lane">The declared lane.</param>
    public void UsePrimaryPath(string relativePath, string pathClass, string lane)
    {
        File.Delete(SourcePath);
        SourcePath = Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(SourcePath)!);
        File.WriteAllText(SourcePath, "<p>primary</p>\n");
        File.WriteAllText(StoryPath, StoryText(relativePath));
        string testClass = PrimaryTestClass(pathClass);
        MutateContract(contract =>
        {
            JsonObject repository = EvidenceJson.RequiredArray(
                EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch),
                "repositories",
                GateReason.ScopeDigestMismatch)[0]!.AsObject();
            repository["includePaths"] = new JsonArray(
                "_bmad-output/implementation-artifacts/spec-gate-fixture.md",
                "_bmad-output/implementation-artifacts/evidence/gate-fixture.json",
                relativePath);
            contract["primaryPaths"] = new JsonArray(new JsonObject
            {
                ["class"] = pathClass,
                ["lane"] = lane,
            });
            EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!.AsObject()["lane"] = lane;
            EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!.AsObject()["primaryPathClass"] = pathClass;
            EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!.AsObject()["selectors"] =
                new JsonArray($"class:{testClass}");
            EvidenceJson.RequiredArray(contract, "mappings", GateReason.CheckedItemEvidenceMismatch)[0]!.AsObject()["paths"] =
                new JsonArray(relativePath);
            EvidenceJson.RequiredArray(contract, "mappings", GateReason.CheckedItemEvidenceMismatch)[2]!.AsObject()["assertions"] =
                new JsonArray($"{testClass}.ValidAssertion");
        }, refreshEvidence: false);
        WritePassingTrx($"{testClass}.ValidAssertion");
        RefreshEvidence();
    }

    /// <summary>Adds an explicit fence-free claim and a policy-bound primary lane without a path trigger.</summary>
    public void UseClaimPrimaryPath(string pathClass, string lane)
    {
        File.AppendAllText(StoryPath, $"\n[claim:{pathClass}]\n");
        string testClass = PrimaryTestClass(pathClass);
        MutateContract(contract =>
        {
            contract["primaryPaths"] = new JsonArray(new JsonObject
            {
                ["class"] = pathClass,
                ["lane"] = lane,
            });
            JsonObject result = EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!.AsObject();
            result["lane"] = lane;
            result["primaryPathClass"] = pathClass;
            result["selectors"] = new JsonArray($"class:{testClass}");
            EvidenceJson.RequiredArray(contract, "mappings", GateReason.CheckedItemEvidenceMismatch)[2]!.AsObject()["assertions"] =
                new JsonArray($"{testClass}.ValidAssertion");
        }, refreshEvidence: false);
        WritePassingTrx($"{testClass}.ValidAssertion");
        RefreshEvidence();
    }

    /// <summary>Adds a claim phrase inside a fenced example.</summary>
    public void AddFencedClaim(string pathClass)
    {
        File.AppendAllText(StoryPath, $"\n```text\n[claim:{pathClass}]\n```\n");
        RefreshEvidence();
    }

    /// <summary>Adds a changed root-declared submodule and reconciles its gitlink and inner diff.</summary>
    public void UseRootDeclaredSubmodule(string relativeSubmodulePath = "references/Synthetic.Module")
    {
        File.Delete(SourcePath);
        string normalizedSubmodulePath = relativeSubmodulePath.Replace('\\', '/');
        string submodulePath = Path.Combine(RepositoryRoot, normalizedSubmodulePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(submodulePath);
        RunGitAt(submodulePath, "init", "--initial-branch=main");
        RunGitAt(submodulePath, "config", "user.email", "gate@example.invalid");
        RunGitAt(submodulePath, "config", "user.name", "Story Evidence Gate Tests");
        File.WriteAllText(Path.Combine(submodulePath, "module.txt"), "base\n");
        RunGitAt(submodulePath, "add", "module.txt");
        RunGitAt(submodulePath, "commit", "-m", "test: create submodule baseline");
        string submoduleBase = RunGitAt(submodulePath, "rev-parse", "HEAD").Trim();
        string modules = $"[submodule \"Synthetic.Module\"]\n\tpath = {normalizedSubmodulePath}\n\turl = ./{normalizedSubmodulePath}\n";
        File.WriteAllText(Path.Combine(RepositoryRoot, ".gitmodules"), modules);
        RunGit("add", ".gitmodules", normalizedSubmodulePath);
        RunGit("commit", "-m", "test: pin submodule baseline");
        BaseCommit = RunGit("rev-parse", "HEAD").Trim();
        HeadCommit = BaseCommit;
        File.WriteAllText(Path.Combine(submodulePath, "module.txt"), "head\n");
        RunGitAt(submodulePath, "add", "module.txt");
        RunGitAt(submodulePath, "commit", "-m", "test: change submodule scope");
        string submoduleHead = RunGitAt(submodulePath, "rev-parse", "HEAD").Trim();
        File.WriteAllText(
            StoryPath,
            StoryText(
                normalizedSubmodulePath,
                $"{normalizedSubmodulePath}/module.txt"));
        MutateContract(contract =>
        {
            JsonObject scope = EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch);
            scope["repositories"] = new JsonArray(
                new JsonObject
                {
                    ["name"] = "root",
                    ["path"] = ".",
                    ["baseCommit"] = BaseCommit,
                    ["headCommit"] = HeadCommit,
                    ["includeWorkingTree"] = true,
                    ["includePaths"] = new JsonArray(
                        "_bmad-output/implementation-artifacts/spec-gate-fixture.md",
                        "_bmad-output/implementation-artifacts/evidence/gate-fixture.json",
                        normalizedSubmodulePath),
                },
                new JsonObject
                {
                    ["name"] = "synthetic-module",
                    ["path"] = normalizedSubmodulePath,
                    ["baseCommit"] = submoduleBase,
                    ["headCommit"] = submoduleHead,
                    ["includeWorkingTree"] = false,
                    ["includePaths"] = new JsonArray("module.txt"),
                });
            JsonArray mappings = EvidenceJson.RequiredArray(contract, "mappings", GateReason.CheckedItemEvidenceMismatch);
            mappings[0]!.AsObject()["paths"] = new JsonArray($"{normalizedSubmodulePath}/module.txt");
        });
    }

    /// <summary>Removes the explicit submodule scope while leaving its owned root gitlink.</summary>
    public void RemoveSubmoduleScope()
    {
        MutateContract(contract =>
        {
            JsonArray repositories = EvidenceJson.RequiredArray(
                EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch),
                "repositories",
                GateReason.ScopeDigestMismatch);
            repositories.RemoveAt(1);
        }, refreshEvidence: false);
    }

    /// <summary>Breaks the declared submodule base away from the root base gitlink.</summary>
    public void BreakSubmoduleBaseGitlinkBinding()
    {
        MutateContract(contract =>
        {
            JsonObject submodule = EvidenceJson.RequiredArray(
                EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch),
                "repositories",
                GateReason.ScopeDigestMismatch)[1]!.AsObject();
            submodule["baseCommit"] = EvidenceJson.RequiredString(
                submodule,
                "headCommit",
                GateReason.ScopeDigestMismatch);
        }, refreshEvidence: false);
    }

    /// <summary>Renames the owned source in scope while intentionally leaving the story File List stale.</summary>
    public void RenameOwnedSourceWithoutUpdatingFileList()
    {
        string renamedPath = Path.Combine(RepositoryRoot, "src", "renamed-gate.txt");
        File.Move(SourcePath, renamedPath);
        SourcePath = renamedPath;
        MutateContract(contract =>
        {
            JsonObject repository = EvidenceJson.RequiredArray(
                EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch),
                "repositories",
                GateReason.ScopeDigestMismatch)[0]!.AsObject();
            repository["includePaths"] = new JsonArray(
                "_bmad-output/implementation-artifacts/spec-gate-fixture.md",
                "_bmad-output/implementation-artifacts/evidence/gate-fixture.json",
                "src/renamed-gate.txt");
            EvidenceJson.RequiredArray(contract, "mappings", GateReason.CheckedItemEvidenceMismatch)[0]!.AsObject()["paths"] =
                new JsonArray("src/renamed-gate.txt");
        });
    }

    /// <summary>Converts the fixture into a valid explicit product-story completion proposal.</summary>
    public void UseProductCompletion()
    {
        PrepareProductBase("review", "review");
        ConfigureProductCompletion(refreshEvidence: true);
    }

    /// <summary>Creates a story completion whose sprint entry was already done at base.</summary>
    public void UseProductCompletionWithDoneSprintAtBase()
    {
        PrepareProductBase("review", "done");
        ConfigureProductCompletion(refreshEvidence: false);
    }

    /// <summary>Creates a product completion from explicit non-review base states.</summary>
    public void UseProductCompletionFromBase(string? storyStatus, string? sprintStatus)
    {
        PrepareProductBase(storyStatus, sprintStatus);
        ConfigureProductCompletion(refreshEvidence: false);
    }

    private void PrepareProductBase(string? storyStatus, string? sprintStatus)
    {
        string story = StoryText("src/gate.txt");
        story = storyStatus is null
            ? story.Replace("status: 'in-progress'\n", string.Empty, StringComparison.Ordinal)
            : story.Replace("status: 'in-progress'", $"status: '{storyStatus}'", StringComparison.Ordinal);
        File.WriteAllText(StoryPath, story);
        string sprintEntry = sprintStatus is null ? string.Empty : $"  explicit-product-key: {sprintStatus}\n";
        File.WriteAllText(SprintPath, $"development_status:\n{sprintEntry}action_items:\n");
        RunGit("add", "_bmad-output/implementation-artifacts/spec-gate-fixture.md");
        RunGit("add", "_bmad-output/implementation-artifacts/sprint-status.yaml");
        RunGit("commit", "-m", "test: create product review base");
        BaseCommit = RunGit("rev-parse", "HEAD").Trim();
        HeadCommit = BaseCommit;
    }

    private void ConfigureProductCompletion(bool refreshEvidence)
    {
        File.WriteAllText(
            StoryPath,
            StoryText("_bmad-output/implementation-artifacts/sprint-status.yaml", "src/gate.txt")
                .Replace("status: 'in-progress'", "status: 'done'", StringComparison.Ordinal));
        File.WriteAllText(SprintPath, "development_status:\n  explicit-product-key: done\naction_items:\n");
        MutateContract(contract =>
        {
            contract["recordKind"] = "story";
            contract["recordLedgerKey"] = "explicit-product-key";
            contract["persistedStatus"] = "done";
            contract["sprintStatusKey"] = "explicit-product-key";
            contract["bootstrap"] = false;
            JsonObject repository = EvidenceJson.RequiredArray(
                EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch),
                "repositories",
                GateReason.ScopeDigestMismatch)[0]!.AsObject();
            repository["includePaths"] = new JsonArray(
                "_bmad-output/implementation-artifacts/spec-gate-fixture.md",
                "_bmad-output/implementation-artifacts/evidence/gate-fixture.json",
                "_bmad-output/implementation-artifacts/sprint-status.yaml",
                "src/gate.txt");
            EvidenceJson.RequiredArray(contract, "mappings", GateReason.CheckedItemEvidenceMismatch)[1]!.AsObject()["paths"] =
                new JsonArray("_bmad-output/implementation-artifacts/sprint-status.yaml");
        }, refreshEvidence: false);
        if (refreshEvidence)
        {
            RefreshEvidence();
        }
    }

    /// <summary>Converts the fixture into the canonical existing BMAD product-story grammar.</summary>
    public void UseCanonicalProductCompletion()
    {
        UseProductCompletion();
        MutateContract(contract => contract["storyTitle"] = "Story 13.2: Canonical Gate Fixture", refreshEvidence: false);
        File.WriteAllText(
            StoryPath,
            """
            # Story 13.2: Canonical Gate Fixture

            Status: done

            ## Acceptance Criteria

            1. Given exact evidence, when validated, then the gate passes.

            ## Tasks / Subtasks

            - [x] Implement the gate.
              - [x] Prove the gate.

            ### File List

            - `_bmad-output/implementation-artifacts/spec-gate-fixture.md` (modified)
            - `_bmad-output/implementation-artifacts/evidence/gate-fixture.json` (new)
            - `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified)
            - `src/gate.txt` (new)
            """);
        RefreshEvidence();
    }

    /// <summary>Converts the fixture into a normal technical-enabler completion transition.</summary>
    public void UseTechnicalEnablerCompletion()
    {
        string ledgerPath = Path.Combine(RepositoryRoot, "_bmad-output", "planning-artifacts", "technical-enablers.md");
        File.WriteAllText(ledgerPath, File.ReadAllText(ledgerPath).Replace("review;", "complete;", StringComparison.Ordinal));
        File.WriteAllText(SprintPath, File.ReadAllText(SprintPath).Replace("status: open", "status: done", StringComparison.Ordinal));
        File.WriteAllText(
            StoryPath,
            StoryText(
                    "_bmad-output/implementation-artifacts/sprint-status.yaml",
                    "_bmad-output/planning-artifacts/technical-enablers.md",
                    "src/gate.txt")
                .Replace("status: 'in-progress'", "status: 'complete'", StringComparison.Ordinal));
        MutateContract(contract =>
        {
            contract["bootstrap"] = false;
            JsonObject repository = EvidenceJson.RequiredArray(
                EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch),
                "repositories",
                GateReason.ScopeDigestMismatch)[0]!.AsObject();
            repository["includePaths"] = new JsonArray(
                "_bmad-output/implementation-artifacts/spec-gate-fixture.md",
                "_bmad-output/implementation-artifacts/evidence/gate-fixture.json",
                "_bmad-output/implementation-artifacts/sprint-status.yaml",
                "_bmad-output/planning-artifacts/technical-enablers.md",
                "src/gate.txt");
        });
    }

    /// <summary>Changes the lane to retained and installs a correctly bound immutable sidecar.</summary>
    public void UseRetainedEvidence()
    {
        JsonObject contract = ReadContract();
        JsonObject lane = EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid)[0]!.AsObject();
        string retainedRoot = Path.Combine(ResultsRoot, "retained", "12345", "gate-evidence");
        Directory.CreateDirectory(retainedRoot);
        string retainedTrx = Path.Combine(retainedRoot, "gate.trx");
        string retainedProvenance = Path.Combine(retainedRoot, "gate.provenance.json");
        File.Move(TrxPath, retainedTrx);
        File.Move(ProvenancePath, retainedProvenance);
        TrxPath = retainedTrx;
        ProvenancePath = retainedProvenance;
        lane["source"] = "retained";
        lane["trx"] = "retained/12345/gate-evidence/gate.trx";
        lane["provenance"] = "retained/12345/gate-evidence/gate.provenance.json";
        lane["artifactLocator"] = "github-actions://hexalith/chatbot/runs/12345/artifacts/gate-evidence";
        EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch)["implementationDigest"] = new string('0', 64);
        WriteContract(contract);
        JsonObject policy = EvidenceJson.LoadPolicy(PolicyPath);
        ScopeEvaluation evaluation = ScopeEvaluator.Evaluate(RepositoryRoot, policy, contract, BaseCommit, HeadCommit);
        contract = ReadContract();
        EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch)["implementationDigest"] = evaluation.Digest;
        WriteContract(contract);

        JsonObject sidecar = JsonNode.Parse(File.ReadAllText(ProvenancePath))!.AsObject();
        sidecar["baseCommit"] = BaseCommit;
        sidecar["headCommit"] = HeadCommit;
        sidecar["implementationDigest"] = evaluation.Digest;
        sidecar["trxSha256"] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(TrxPath))).ToLowerInvariant();
        sidecar["source"] = "retained";
        sidecar["artifactLocator"] = EvidenceJson.RequiredString(lane, "artifactLocator", GateReason.EvidenceStaleOrUnbound);
        sidecar["producedAtUtc"] = DateTimeOffset.UtcNow.ToString("O");
        File.WriteAllText(ProvenancePath, sidecar.ToJsonString(JsonReportWriter.SerializerOptions));
    }

    /// <summary>Runs production attestation against the fixture contract.</summary>
    public void Attest() => ProvenanceAttestor.AttestContract(
        RepositoryRoot,
        ContractPath,
        BaseCommit,
        HeadCommit,
        ResultsRoot,
        DateTimeOffset.UtcNow.AddMinutes(1),
        PolicyPath);

    /// <summary>Mutates the copied policy.</summary>
    public void MutatePolicy(Action<JsonObject> mutation)
    {
        JsonObject policy = JsonNode.Parse(File.ReadAllText(PolicyPath))!.AsObject();
        mutation(policy);
        File.WriteAllText(PolicyPath, policy.ToJsonString(JsonReportWriter.SerializerOptions));
    }

    /// <summary>Deletes the owned source while keeping it declared.</summary>
    public void DeleteOwnedSource() => File.Delete(SourcePath);

    /// <summary>Commits the exact owned product-completion scope and updates the immutable head.</summary>
    public void CommitOwnedCompletion()
    {
        RunGit("add", "_bmad-output/implementation-artifacts/spec-gate-fixture.md");
        RunGit("add", "_bmad-output/implementation-artifacts/evidence/gate-fixture.json");
        RunGit("add", "_bmad-output/implementation-artifacts/sprint-status.yaml");
        RunGit("add", "src/gate.txt");
        RunGit("commit", "-m", "test: commit exact story scope");
        HeadCommit = RunGit("rev-parse", "HEAD").Trim();
        ProvenanceAttestor.AttestContract(
            RepositoryRoot,
            ContractPath,
            BaseCommit,
            HeadCommit,
            ResultsRoot,
            DateTimeOffset.UtcNow,
            PolicyPath);
    }

    /// <summary>Commits an immutable scope whose contract forbids all worktree/index drift.</summary>
    public void CommitStrictImmutableCompletion()
    {
        JsonObject contract = ReadContract();
        JsonObject repository = EvidenceJson.RequiredArray(
            EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch),
            "repositories",
            GateReason.ScopeDigestMismatch)[0]!.AsObject();
        repository["includeWorkingTree"] = false;
        EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch)["implementationDigest"] = new string('0', 64);
        WriteContract(contract);
        RunGit("add", "_bmad-output/implementation-artifacts/spec-gate-fixture.md");
        RunGit("add", "_bmad-output/implementation-artifacts/evidence/gate-fixture.json");
        RunGit("add", "_bmad-output/implementation-artifacts/sprint-status.yaml");
        RunGit("add", "src/gate.txt");
        RunGit("commit", "-m", "test: commit strict immutable scope");
        HeadCommit = RunGit("rev-parse", "HEAD").Trim();

        JsonObject policy = EvidenceJson.LoadPolicy(PolicyPath);
        ScopeEvaluation evaluation = ScopeEvaluator.Evaluate(
            RepositoryRoot,
            policy,
            ReadContract(),
            BaseCommit,
            HeadCommit);
        contract = ReadContract();
        EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch)["implementationDigest"] = evaluation.Digest;
        WriteContract(contract);
        RunGit("add", "_bmad-output/implementation-artifacts/evidence/gate-fixture.json");
        RunGit("commit", "-m", "test: bind strict immutable digest");
        HeadCommit = RunGit("rev-parse", "HEAD").Trim();
        ProvenanceAttestor.AttestContract(
            RepositoryRoot,
            ContractPath,
            BaseCommit,
            HeadCommit,
            ResultsRoot,
            DateTimeOffset.UtcNow,
            PolicyPath);
    }

    /// <summary>Commits an unrelated path despite disclosing it as pre-existing local work.</summary>
    public void CommitCompletionWithDisclosedUnrelatedChange()
    {
        string unrelatedPath = Path.Combine(RepositoryRoot, "unrelated.txt");
        File.WriteAllText(unrelatedPath, "unrelated committed scope\n");
        MutateContract(contract =>
        {
            contract["outOfScopeDisclosures"] = new JsonArray(new JsonObject
            {
                ["repository"] = "root",
                ["path"] = "unrelated.txt",
                ["owner"] = "another-story",
                ["reason"] = "Synthetic disclosure cannot waive immutable mixed scope.",
                ["classification"] = "preExistingLocalChange",
            });
        });
        RunGit("add", "_bmad-output/implementation-artifacts/spec-gate-fixture.md");
        RunGit("add", "_bmad-output/implementation-artifacts/evidence/gate-fixture.json");
        RunGit("add", "_bmad-output/implementation-artifacts/sprint-status.yaml");
        RunGit("add", "src/gate.txt");
        RunGit("add", "unrelated.txt");
        RunGit("commit", "-m", "test: commit disclosed mixed scope");
        HeadCommit = RunGit("rev-parse", "HEAD").Trim();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_temporaryRoot))
        {
            Directory.Delete(_temporaryRoot, recursive: true);
        }
    }

    private static string FindPolicy()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "story-evidence-policy.json")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(
            directory?.FullName ?? throw new InvalidOperationException("Could not locate policy."),
            "story-evidence-policy.json");
    }

    private static JsonObject CreateContract()
    {
        return new JsonObject
        {
            ["schemaVersion"] = "1.0",
            ["recordKind"] = "technicalEnabler",
            ["recordLedgerKey"] = "TE-X",
            ["storyKey"] = "gate-fixture",
            ["storyTitle"] = "Gate Fixture",
            ["storyPath"] = "_bmad-output/implementation-artifacts/spec-gate-fixture.md",
            ["targetStatus"] = "done",
            ["persistedStatus"] = "complete",
            ["sprintStatusKey"] = ActionText,
            ["bootstrap"] = true,
            ["scope"] = new JsonObject
            {
                ["implementationDigest"] = new string('0', 64),
                ["lifecycleBookkeepingFields"] = new JsonArray("implementationDigest"),
                ["repositories"] = new JsonArray(new JsonObject
                {
                    ["name"] = "root",
                    ["path"] = ".",
                    ["baseCommit"] = "$BASE",
                    ["headCommit"] = "$HEAD",
                    ["includeWorkingTree"] = true,
                    ["includePaths"] = new JsonArray(
                        "_bmad-output/implementation-artifacts/spec-gate-fixture.md",
                        "_bmad-output/implementation-artifacts/evidence/gate-fixture.json",
                        "src/gate.txt"),
                }),
            },
            ["results"] = new JsonArray(new JsonObject
            {
                ["lane"] = "gate-unit",
                ["trx"] = "gate.trx",
                ["provenance"] = "gate.provenance.json",
                ["artifactLocator"] = "file:gate.trx",
                ["source"] = "current-run",
                ["selectors"] = new JsonArray("class:GateFixture"),
                ["allowSkipped"] = false,
                ["primaryPathClass"] = null,
            }),
            ["primaryPaths"] = new JsonArray(),
            ["mappings"] = new JsonArray(
                Mapping("task-1", "task", "src/gate.txt"),
                Mapping("task-2", "task", "_bmad-output/implementation-artifacts/evidence/gate-fixture.json"),
                new JsonObject
                {
                    ["id"] = "ac-1",
                    ["kind"] = "acceptanceCriterion",
                    ["paths"] = new JsonArray(),
                    ["assertions"] = new JsonArray("GateFixture.ValidAssertion"),
                }),
            ["outOfScopeDisclosures"] = new JsonArray(),
            ["reportPath"] = "_bmad-output/implementation-artifacts/evidence/reports/gate-fixture.json",
        };
    }

    private static JsonObject Mapping(string id, string kind, string path)
    {
        return new JsonObject
        {
            ["id"] = id,
            ["kind"] = kind,
            ["paths"] = new JsonArray(path),
            ["assertions"] = new JsonArray(),
        };
    }

    private static string StoryText(params string[] sourcePaths)
    {
        string fileList = string.Join("\n", sourcePaths.Select(static path => $"- `{path}`"));
        return $"""
            ---
            title: 'Gate Fixture'
            status: 'in-progress'
            ---

            ## Tasks & Acceptance

            **Execution:**
            - [x] Implement the gate.
            - [x] Prove the gate.

            **Acceptance Criteria:**
            - Given exact evidence, when validated, then the gate passes.

            ## File List

            - `_bmad-output/implementation-artifacts/spec-gate-fixture.md`
            - `_bmad-output/implementation-artifacts/evidence/gate-fixture.json`
            {fileList}
            """;
    }

    private JsonObject ReadContract() => JsonNode.Parse(File.ReadAllText(ContractPath))!.AsObject();

    private void WriteContract(JsonObject contract) =>
        File.WriteAllText(ContractPath, contract.ToJsonString(JsonReportWriter.SerializerOptions));

    private void WritePassingTrx(string testName = "GateFixture.ValidAssertion") =>
        WriteTrx(1, 1, 1, 0, 0, "Completed", testName);

    private static string PrimaryTestClass(string pathClass) => pathClass switch
    {
        "browser" => "Hexalith.ChatBot.UI.E2E.Tests.RealRenderCrossSurfaceE2ETests",
        "signalr" => "Hexalith.ChatBot.Server.Tests.Projections.ChatBotProjectConversationHubE2ETests",
        "hosting-assets" => "Hexalith.ChatBot.UI.E2E.Tests.FrontComposerShellIntegrationE2ETests",
        "aspire-dapr" => "Hexalith.ChatBot.IntegrationTests.TrivialGovernedCommandAspireE2eTests",
        "recovery" => "Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests",
        _ => throw new ArgumentOutOfRangeException(nameof(pathClass)),
    };

    private string RunGit(params string[] arguments)
    {
        return RunGitAt(RepositoryRoot, arguments);
    }

    private static string RunGitAt(string repositoryPath, params string[] arguments)
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
