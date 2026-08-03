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

        fixture.RefreshEvidence();
        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
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

        fixture.RefreshEvidence();
        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
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
                new JsonArray(TestName);
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
                new JsonArray("class:MissingFixture"));

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
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
    [InlineData("tests/Module/Recovery/Scenario.cs", "recovery", "recovery-primary")]
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

    /// <summary>Proves every load-bearing security field remains pinned within policy version 1.0.</summary>
    [Theory]
    [InlineData("age")]
    [InlineData("tree-source")]
    [InlineData("primary-selector")]
    [InlineData("metadata-bound")]
    public static void SameVersionSecurityPolicyMutationShouldFail(string kind)
    {
        using GateFixture fixture = new();
        fixture.MutatePolicy(policy =>
        {
            if (kind == "age")
            {
                policy["maximumEvidenceAgeHours"] = 721;
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

    /// <summary>Proves report and lifecycle digest exclusions are policy-constrained.</summary>
    [Theory]
    [InlineData("report")]
    [InlineData("lifecycle")]
    [InlineData("event-resolution")]
    public static void InvalidPolicyBoundScopeExclusionShouldFailWithScopeReason(string kind)
    {
        using GateFixture fixture = new();
        if (kind == "report")
        {
            fixture.MutateContract(contract => contract["reportPath"] = "src/gate.txt", refreshEvidence: false);
        }
        else if (kind == "lifecycle")
        {
            fixture.MutateContract(contract =>
                EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch)["lifecycleBookkeepingFields"] =
                    new JsonArray("implementationDigest", "payload"), refreshEvidence: false);
        }
        else
        {
            fixture.MutatePolicy(policy =>
                EvidenceJson.RequiredObject(policy, "eventBaseHeadResolution", GateReason.ScopeDigestMismatch)["pullRequestHead"] =
                    "checked-out merge commit");
        }

        fixture.Validate().Issues.Single().ReasonCode.ShouldBe(GateReason.ScopeDigestMismatch);
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
              "schemaVersion": "1.0",
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
                "implementationDigest": "unused",
                "repositories": [],
                "lifecycleBookkeepingFields": []
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
              "schemaVersion": "1.0",
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
                "implementationDigest": "unused",
                "repositories": [],
                "lifecycleBookkeepingFields": []
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
              "schemaVersion": "1.0",
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
                "implementationDigest": "unused",
                "repositories": [],
                "lifecycleBookkeepingFields": []
              },
              "results": [],
              "primaryPaths": [],
              "mappings": [],
              "outOfScopeDisclosures": [],
              "reportPath": "_bmad-output/implementation-artifacts/evidence/reports/te-x.json"
            }
            """;
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
