using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Reconciles one prospective completion transition against exact source and machine evidence.
/// </summary>
public static class StoryEvidenceValidator
{
    /// <summary>The policy schema this gate implementation understands.</summary>
    internal const string SupportedPolicyVersion = "2.1";

    /// <summary>Validates one contract and returns a metadata-only report.</summary>
    /// <param name="options">The normalized gate options.</param>
    /// <returns>The pass or fail report.</returns>
    public static GateReport Validate(GateOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        GateReport report = new()
        {
            BaseCommit = SafeRevision(options.BaseCommit),
            HeadCommit = SafeRevision(options.HeadCommit),
            EvaluatedAtUtc = options.NowUtc.ToUniversalTime(),
        };

        try
        {
            JsonObject policy = EvidenceJson.LoadPolicy(options.PolicyPath);
            JsonObject contract = EvidenceJson.LoadContract(options.ContractPath);
            report.StoryKey = EvidenceJson.RequiredStoryKey(contract);
            string expectedContractPath =
                $"_bmad-output/implementation-artifacts/evidence/{report.StoryKey}.json";
            if (!NormalizePath(Path.GetRelativePath(options.RepositoryRoot, options.ContractPath))
                    .Equals(expectedContractPath, StringComparison.Ordinal))
            {
                throw new GateValidationException(GateReason.StatusMismatch, "contract-path");
            }

            report.PolicyVersion = EvidenceJson.RequiredString(policy, "schemaVersion", GateReason.ScopeDigestMismatch);
            ValidateVersionsAndReasons(policy, contract);

            StoryRecord story = MarkdownStoryReader.Read(options.StoryPath);
            if (story.FileList.Count == 0)
            {
                throw new GateValidationException(GateReason.FileListDiffMismatch, "file-list-section");
            }

            if (story.MandatoryItems.Count == 0)
            {
                throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, "mandatory-sections");
            }

            ValidateStatus(options, contract, story);
            ScopeEvaluation scope = ScopeEvaluator.Evaluate(
                options.RepositoryRoot,
                policy,
                contract,
                options.BaseCommit.ToLowerInvariant(),
                options.HeadCommit.ToLowerInvariant());
            report.ImplementationDigest = scope.Digest;
            report.RepositoryScopes = scope.Scopes.Select(static value => value.Path).ToArray();
            IReadOnlySet<string> changedPaths = ScopeEvaluator.FileListPaths(scope);
            IReadOnlySet<string> eventPaths = ScopeEvaluator.EventPaths(scope);
            report.FileListCount = story.FileList.Count;
            report.ScopedDiffCount = changedPaths.Count;
            report.EventPathCount = eventPaths.Count;
            if (!story.FileList.SetEquals(changedPaths))
            {
                throw new GateValidationException(GateReason.FileListDiffMismatch, "file-list");
            }

            JsonObject scopeNode = EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch);
            string scopeMode = EvidenceJson.RequiredString(scopeNode, "mode", GateReason.ScopeDigestMismatch);
            string declaredDigest = EvidenceJson.RequiredString(
                scopeNode,
                "implementationDigest",
                GateReason.ScopeDigestMismatch);
            if (!declaredDigest.Equals(scope.Digest, StringComparison.OrdinalIgnoreCase))
            {
                throw new GateValidationException(GateReason.ScopeDigestMismatch, "implementation-digest");
            }

            LifecycleTransitionValidator.Validate(
                options.RepositoryRoot,
                contract,
                options.BaseCommit,
                options.HeadCommit,
                scope.EventPaths);
            List<LaneResult> lanes = ReadResults(options, policy, contract, scope.Digest);
            report.Lanes = lanes;
            IReadOnlySet<string> primaryTriggerPaths = scopeMode.Equals("snapshot-plus-transition", StringComparison.Ordinal)
                ? eventPaths
                : changedPaths;
            report.PrimaryPaths = ValidatePrimaryPaths(policy, contract, story, primaryTriggerPaths, lanes);
            (int checkedItems, int mappedItems) = ValidateMappings(contract, story, changedPaths, lanes);
            report.CheckedItemCount = checkedItems;
            report.MappedItemCount = mappedItems;
            report.Passed = true;
        }
        catch (GateValidationException exception)
        {
            report.Passed = false;
            report.Issues = [GateIssue.Create(exception.ReasonCode, exception.Subject)];
        }
        catch (Exception exception) when (exception is IOException
            or ArgumentException
            or UnauthorizedAccessException
            or NotSupportedException
            or System.Security.SecurityException)
        {
            report.Passed = false;
            report.Issues = [GateIssue.Create(GateReason.ScopeDigestMismatch, "io-or-path")];
        }

        if (!string.IsNullOrWhiteSpace(options.ReportPath))
        {
            try
            {
                JsonReportWriter.Write(options.ReportPath, report);
            }
            catch (Exception exception) when (exception is IOException
                or ArgumentException
                or UnauthorizedAccessException
                or NotSupportedException
                or System.Security.SecurityException)
            {
                report.Passed = false;
                report.Issues = [GateIssue.Create(GateReason.ScopeDigestMismatch, "report-write")];
            }
        }

        return report;
    }

    private static void ValidateVersionsAndReasons(JsonObject policy, JsonObject contract)
    {
        ValidatePinnedPolicy(policy);
        ValidateAttestationContract(contract);
    }

    internal static void ValidateAttestationContract(JsonObject contract)
    {
        if (!EvidenceJson.RequiredString(contract, "schemaVersion", GateReason.ScopeDigestMismatch)
                .Equals("2.0", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "schema-version");
        }

        _ = EvidenceJson.RequiredStoryKey(contract);
    }

    internal static ScopeEvaluation PreflightProductionContract(GateOptions options, JsonObject policy, JsonObject contract)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(contract);
        ValidatePinnedPolicy(policy);
        ValidateAttestationContract(contract);
        string storyKey = EvidenceJson.RequiredStoryKey(contract);
        string expectedContractPath = $"_bmad-output/implementation-artifacts/evidence/{storyKey}.json";
        if (!NormalizePath(Path.GetRelativePath(options.RepositoryRoot, options.ContractPath))
                .Equals(expectedContractPath, StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "contract-path");
        }

        StoryRecord story = MarkdownStoryReader.Read(options.StoryPath);
        if (story.FileList.Count == 0)
        {
            throw new GateValidationException(GateReason.FileListDiffMismatch, "file-list-section");
        }

        if (story.MandatoryItems.Count == 0 || !story.MandatoryItems.SetEquals(story.CheckedItems))
        {
            throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, "unchecked-mandatory-item");
        }

        ValidateStatus(options, contract, story);
        ScopeEvaluation scope = ScopeEvaluator.Evaluate(
            options.RepositoryRoot,
            policy,
            contract,
            options.BaseCommit.ToLowerInvariant(),
            options.HeadCommit.ToLowerInvariant());
        IReadOnlySet<string> changedPaths = ScopeEvaluator.FileListPaths(scope);
        if (!story.FileList.SetEquals(changedPaths))
        {
            throw new GateValidationException(GateReason.FileListDiffMismatch, "file-list");
        }

        string declaredDigest = EvidenceJson.RequiredString(
            EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch),
            "implementationDigest",
            GateReason.ScopeDigestMismatch);
        if (!declaredDigest.Equals(scope.Digest, StringComparison.OrdinalIgnoreCase))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "implementation-digest");
        }

        LifecycleTransitionValidator.Validate(
            options.RepositoryRoot,
            contract,
            options.BaseCommit,
            options.HeadCommit,
            scope.EventPaths);
        string scopeMode = EvidenceJson.RequiredString(
            EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch),
            "mode",
            GateReason.ScopeDigestMismatch);
        IReadOnlySet<string> primaryTriggerPaths = scopeMode.Equals(
            "snapshot-plus-transition",
            StringComparison.Ordinal)
            ? ScopeEvaluator.EventPaths(scope)
            : changedPaths;
        ValidateProductionDeclarations(
            options.ResultsRoot,
            policy,
            contract,
            story,
            primaryTriggerPaths);
        ValidateMappingDeclarations(contract, story, changedPaths);
        return scope;
    }

    private static void ValidateProductionDeclarations(
        string resultsRoot,
        JsonObject policy,
        JsonObject contract,
        StoryRecord story,
        IReadOnlySet<string> primaryTriggerPaths)
    {
        JsonArray results = EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid);
        if (results.Count == 0)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, "results");
        }

        Dictionary<string, JsonObject> resultContracts = new(StringComparer.Ordinal);
        HashSet<string> resultPaths = new(StringComparer.OrdinalIgnoreCase);
        string repositoryIdentity = EvidenceJson.RequiredString(
            policy,
            "repositoryIdentity",
            GateReason.EvidenceStaleOrUnbound);
        foreach (JsonNode? node in results)
        {
            JsonObject result = node as JsonObject
                ?? throw new GateValidationException(GateReason.MachineResultsInvalid, "results");
            TrxEvidenceReader.PreflightDefinition(result, resultsRoot, repositoryIdentity);
            string lane = EvidenceJson.RequiredString(result, "lane", GateReason.MachineResultsInvalid);
            if (!resultContracts.TryAdd(lane, result))
            {
                throw new GateValidationException(GateReason.MachineResultsInvalid, "duplicate-lane");
            }

            string trxPath = TrxEvidenceReader.ResolveSafeResultPath(
                resultsRoot,
                EvidenceJson.RequiredString(result, "trx", GateReason.MachineResultsInvalid));
            string provenancePath = TrxEvidenceReader.ResolveSafeResultPath(
                resultsRoot,
                EvidenceJson.RequiredString(result, "provenance", GateReason.EvidenceStaleOrUnbound));
            if (!resultPaths.Add(trxPath) || !resultPaths.Add(provenancePath))
            {
                throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, "result-path-collision");
            }
        }

        Dictionary<string, JsonObject> triggers = new(StringComparer.Ordinal);
        HashSet<string> requiredClasses = new(StringComparer.Ordinal);
        foreach (JsonNode? node in EvidenceJson.RequiredArray(
                     policy,
                     "primaryPathTriggers",
                     GateReason.PrimaryPathNotExecuted))
        {
            JsonObject trigger = node as JsonObject
                ?? throw new GateValidationException(GateReason.PrimaryPathNotExecuted, "primaryPathTriggers");
            string pathClass = EvidenceJson.RequiredString(trigger, "class", GateReason.PrimaryPathNotExecuted);
            triggers[pathClass] = trigger;
            IReadOnlyList<string> patterns = EvidenceJson.RequiredStrings(
                trigger,
                "pathPatterns",
                GateReason.PrimaryPathNotExecuted);
            IReadOnlyList<string> claims = EvidenceJson.RequiredStrings(
                trigger,
                "claimPatterns",
                GateReason.PrimaryPathNotExecuted);
            if (primaryTriggerPaths.Any(path => patterns.Any(pattern => GlobMatch(path, pattern)))
                || claims.Any(claim => story.EvidenceText.Contains(claim, StringComparison.OrdinalIgnoreCase)))
            {
                requiredClasses.Add(pathClass);
            }
        }

        Dictionary<string, string> declarations = new(StringComparer.Ordinal);
        foreach (JsonNode? node in EvidenceJson.RequiredArray(
                     contract,
                     "primaryPaths",
                     GateReason.PrimaryPathNotExecuted))
        {
            JsonObject declaration = node as JsonObject
                ?? throw new GateValidationException(GateReason.PrimaryPathNotExecuted, "primaryPaths");
            string pathClass = EvidenceJson.RequiredString(declaration, "class", GateReason.PrimaryPathNotExecuted);
            string lane = EvidenceJson.RequiredString(declaration, "lane", GateReason.PrimaryPathNotExecuted);
            if (!declarations.TryAdd(pathClass, lane))
            {
                throw new GateValidationException(GateReason.PrimaryPathNotExecuted, pathClass);
            }

            requiredClasses.Add(pathClass);
        }

        foreach (string requiredClass in requiredClasses)
        {
            if (!triggers.TryGetValue(requiredClass, out JsonObject? trigger)
                || !declarations.TryGetValue(requiredClass, out string? laneName)
                || !EvidenceJson.RequiredStrings(
                        trigger,
                        "recognizedLanes",
                        GateReason.PrimaryPathNotExecuted)
                    .Contains(laneName, StringComparer.Ordinal)
                || !resultContracts.TryGetValue(laneName, out JsonObject? result))
            {
                throw new GateValidationException(GateReason.PrimaryPathNotExecuted, requiredClass);
            }

            JsonObject binding = EvidenceJson.RequiredArray(
                trigger,
                "recognizedLaneBindings",
                GateReason.PrimaryPathNotExecuted)[0]!.AsObject();
            string? pinnedTrx = binding["trx"] is null
                ? null
                : EvidenceJson.RequiredString(binding, "trx", GateReason.PrimaryPathNotExecuted);
            string? pinnedProvenance = binding["provenance"] is null
                ? null
                : EvidenceJson.RequiredString(binding, "provenance", GateReason.PrimaryPathNotExecuted);
            if (!EvidenceJson.RequiredString(binding, "lane", GateReason.PrimaryPathNotExecuted)
                    .Equals(laneName, StringComparison.Ordinal)
                || !string.Equals(
                    EvidenceJson.RequiredNullableString(result, "primaryPathClass", GateReason.PrimaryPathNotExecuted),
                    requiredClass,
                    StringComparison.Ordinal)
                || !EvidenceJson.RequiredStrings(result, "selectors", GateReason.PrimaryPathNotExecuted)
                    .Contains(
                        EvidenceJson.RequiredString(binding, "selector", GateReason.PrimaryPathNotExecuted),
                        StringComparer.Ordinal)
                || !EvidenceJson.RequiredStrings(binding, "sources", GateReason.PrimaryPathNotExecuted)
                    .Contains(
                        EvidenceJson.RequiredString(result, "source", GateReason.EvidenceStaleOrUnbound),
                        StringComparer.Ordinal)
                || (pinnedTrx is not null
                    && !EvidenceJson.RequiredString(result, "trx", GateReason.PrimaryPathNotExecuted)
                        .Equals(pinnedTrx, StringComparison.Ordinal))
                || (pinnedProvenance is not null
                    && !EvidenceJson.RequiredString(result, "provenance", GateReason.PrimaryPathNotExecuted)
                        .Equals(pinnedProvenance, StringComparison.Ordinal)))
            {
                throw new GateValidationException(GateReason.PrimaryPathNotExecuted, requiredClass);
            }
        }

        foreach ((string laneName, JsonObject result) in resultContracts)
        {
            string? primaryPathClass = EvidenceJson.RequiredNullableString(
                result,
                "primaryPathClass",
                GateReason.PrimaryPathNotExecuted);
            if (primaryPathClass is not null
                && (!declarations.TryGetValue(primaryPathClass, out string? declaredLane)
                    || !declaredLane.Equals(laneName, StringComparison.Ordinal)))
            {
                throw new GateValidationException(GateReason.PrimaryPathNotExecuted, primaryPathClass);
            }
        }
    }

    internal static void ValidatePinnedPolicy(JsonObject policy)
    {
        string policyVersion = EvidenceJson.RequiredString(policy, "schemaVersion", GateReason.ScopeDigestMismatch);
        string minimumVersion = EvidenceJson.RequiredString(
            policy,
            "minimumSupportedVersion",
            GateReason.ScopeDigestMismatch);
        if (!Version.TryParse(policyVersion, out Version? parsedPolicyVersion)
            || !Version.TryParse(minimumVersion, out Version? parsedMinimumVersion)
            || parsedPolicyVersion < parsedMinimumVersion)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "minimum-supported-version");
        }

        if (!policyVersion.Equals(SupportedPolicyVersion, StringComparison.Ordinal)
            || !minimumVersion.Equals(SupportedPolicyVersion, StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(policy, "repositoryIdentity", GateReason.ScopeDigestMismatch)
                .Equals("Hexalith/Hexalith.ChatBot", StringComparison.Ordinal)
            || EvidenceJson.RequiredInteger(policy, "maximumCurrentRunAgeMinutes", GateReason.ScopeDigestMismatch) != 60
            || EvidenceJson.RequiredInteger(policy, "maximumLaneCurrentRunAgeMinutes", GateReason.ScopeDigestMismatch) != 1440
            || EvidenceJson.RequiredInteger(policy, "maximumRetainedEvidenceAgeHours", GateReason.ScopeDigestMismatch) != 720
            || EvidenceJson.RequiredInteger(policy, "maximumFutureClockSkewMinutes", GateReason.ScopeDigestMismatch) != 5
            || !EvidenceJson.RequiredStrings(policy, "allowedScopeModes", GateReason.ScopeDigestMismatch)
                .SequenceEqual(["diff", "snapshot-plus-transition"], StringComparer.Ordinal))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "schema-version");
        }

        HashSet<string> configuredReasons = EvidenceJson
            .RequiredStrings(policy, "reasonCodes", GateReason.ScopeDigestMismatch)
            .ToHashSet(StringComparer.Ordinal);
        string[] requiredReasons =
        [
            GateReason.StatusMismatch,
            GateReason.FileListDiffMismatch,
            GateReason.GitlinkScopeMismatch,
            GateReason.ScopeDigestMismatch,
            GateReason.MachineResultsInvalid,
            GateReason.EvidenceStaleOrUnbound,
            GateReason.PrimaryPathNotExecuted,
            GateReason.CheckedItemEvidenceMismatch,
            GateReason.EvidencePayloadForbidden,
        ];
        if (!configuredReasons.SetEquals(requiredReasons))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "reason-codes");
        }

        IReadOnlyList<string> formats = EvidenceJson.RequiredStrings(
            policy,
            "acceptedResultFormats",
            GateReason.MachineResultsInvalid);
        if (formats.Count != 1 || !formats[0].Equals("trx", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, "result-format");
        }

        if (!EvidenceJson.RequiredStrings(policy, "requiredStorySections", GateReason.ScopeDigestMismatch)
                .ToHashSet(StringComparer.Ordinal)
                .SetEquals(["Tasks & Acceptance", "Acceptance Criteria", "Tasks / Subtasks", "File List"])
            || !EvidenceJson.RequiredStrings(policy, "mandatoryCheckboxSections", GateReason.ScopeDigestMismatch)
                .ToHashSet(StringComparer.Ordinal)
                .SetEquals(["Execution", "Tasks / Subtasks"])
            || !EvidenceJson.RequiredStrings(policy, "reportExcludedPaths", GateReason.ScopeDigestMismatch)
                .SequenceEqual(["_bmad-output/implementation-artifacts/evidence/reports/"], StringComparer.Ordinal)
            || EvidenceJson.RequiredArray(policy, "exceptions", GateReason.EvidencePayloadForbidden).Count != 0)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "policy-grammar");
        }

        ValidateStoryGrammars(policy);
        ValidateEventResolution(policy);
        ValidatePrimaryPolicy(policy);
        ValidateMetadataPolicy(policy);

        JsonObject digest = EvidenceJson.RequiredObject(policy, "sourceDigest", GateReason.ScopeDigestMismatch);
        if (!EvidenceJson.RequiredString(digest, "algorithm", GateReason.ScopeDigestMismatch)
                .Equals("SHA-256", StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(digest, "tuple", GateReason.ScopeDigestMismatch)
                .Equals("repository/path/mode/blob", StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(digest, "sort", GateReason.ScopeDigestMismatch)
                .Equals("ordinal", StringComparison.Ordinal)
            || !EvidenceJson.RequiredBoolean(digest, "rootDeclaredSubmodulesOnly", GateReason.GitlinkScopeMismatch)
            || !EvidenceJson.RequiredString(digest, "immutableContentSource", GateReason.ScopeDigestMismatch)
                .Equals("git-tree", StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(digest, "worktreeModeSource", GateReason.ScopeDigestMismatch)
                .Equals("git-index", StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(digest, "symlinkMode", GateReason.ScopeDigestMismatch)
                .Equals("120000", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "source-digest-policy");
        }
    }

    private static void ValidatePrimaryPolicy(JsonObject policy)
    {
        Dictionary<string, (
            string[] Paths,
            string Claim,
            string Lane,
            string Selector,
            string? Trx,
            string? Provenance,
            string[] Sources)> expected =
            new(StringComparer.Ordinal)
            {
                ["browser"] = (
                    ["src/**/*.razor", "src/**/wwwroot/**", "tests/**/*E2E*"],
                    "[claim:browser]",
                    "browser-primary",
                    "class:Hexalith.ChatBot.UI.E2E.Tests.RealRenderCrossSurfaceE2ETests",
                    null,
                    null,
                    ["current-run"]),
                ["signalr"] = (
                    ["src/**/*Hub*.cs", "src/**/*SignalR*.cs", "tests/**/*SignalR*.cs"],
                    "[claim:signalr]",
                    "signalr-primary",
                    "class:Hexalith.ChatBot.Server.Tests.Projections.ChatBotProjectConversationHubE2ETests",
                    null,
                    null,
                    ["current-run"]),
                ["hosting-assets"] = (
                    ["src/**/*AppHost*", "src/**/wwwroot/**", "src/**/*.css", "src/**/App.razor"],
                    "[claim:hosting-assets]",
                    "hosting-assets-primary",
                    "class:Hexalith.ChatBot.UI.E2E.Tests.FrontComposerShellIntegrationE2ETests",
                    null,
                    null,
                    ["current-run"]),
                ["aspire-dapr"] = (
                    [
                        "src/Hexalith.ChatBot.AppHost/**",
                        "tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs",
                    ],
                    "[claim:aspire-dapr]",
                    "aspire-dapr-primary",
                    "class:Hexalith.ChatBot.IntegrationTests.TrivialGovernedCommandAspireE2eTests",
                    null,
                    null,
                    ["current-run"]),
                ["recovery"] = (
                    ["tests/**/Recovery/**", ".github/workflows/ci.yml", ".github/workflows/release.yml"],
                    "[claim:recovery]",
                    "recovery-primary",
                    "class:Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests",
                    "recovery-primary/live-recovery-validation.trx",
                    "recovery-primary/live-recovery-validation.provenance.json",
                    ["current-run"]),
            };
        JsonArray triggers = EvidenceJson.RequiredArray(policy, "primaryPathTriggers", GateReason.ScopeDigestMismatch);
        if (triggers.Count != expected.Count)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "primary-path-policy");
        }

        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (JsonNode? node in triggers)
        {
            JsonObject trigger = node as JsonObject
                ?? throw new GateValidationException(GateReason.ScopeDigestMismatch, "primary-path-policy");
            string pathClass = EvidenceJson.RequiredString(trigger, "class", GateReason.ScopeDigestMismatch);
            if (!seen.Add(pathClass) || !expected.TryGetValue(pathClass, out var configured)
                || !EvidenceJson.RequiredStrings(trigger, "pathPatterns", GateReason.ScopeDigestMismatch)
                    .SequenceEqual(configured.Paths, StringComparer.Ordinal)
                || !EvidenceJson.RequiredStrings(trigger, "claimPatterns", GateReason.ScopeDigestMismatch)
                    .SequenceEqual([configured.Claim], StringComparer.Ordinal)
                || !EvidenceJson.RequiredStrings(trigger, "recognizedLanes", GateReason.ScopeDigestMismatch)
                    .SequenceEqual([configured.Lane], StringComparer.Ordinal))
            {
                throw new GateValidationException(GateReason.ScopeDigestMismatch, "primary-path-policy");
            }

            JsonArray bindings = EvidenceJson.RequiredArray(
                trigger,
                "recognizedLaneBindings",
                GateReason.ScopeDigestMismatch);
            if (bindings.Count != 1 || bindings[0] is not JsonObject binding
                || !EvidenceJson.RequiredString(binding, "lane", GateReason.ScopeDigestMismatch)
                    .Equals(configured.Lane, StringComparison.Ordinal)
                || !EvidenceJson.RequiredString(binding, "selector", GateReason.ScopeDigestMismatch)
                    .Equals(configured.Selector, StringComparison.Ordinal)
                || !OptionalPinnedString(binding, "trx", configured.Trx)
                || !OptionalPinnedString(binding, "provenance", configured.Provenance)
                // The per-lane freshness ceiling was the ONLY recognizedLaneBindings field this function did not
                // pin. Every sibling (lane/selector/trx/provenance/sources) is hard-pinned against an in-code
                // constant and the global ceiling is pinned to 60 above, but the override was bounded only by a
                // <= 1440 range check inside the resolver -- so any lane could be raised to 24 hours by editing
                // story-evidence-policy.json alone, with no version bump, no reason code and no failing test. That
                // file is in no trigger's pathPatterns, so such a change need produce no primary-path evidence at
                // all. Pinning it here is what makes the ceiling tamper-evident.
                || !OptionalPinnedInteger(binding, "maximumCurrentRunAgeMinutes", ExpectedLaneCurrentRunAgeMinutes)
                || !EvidenceJson.RequiredStrings(binding, "sources", GateReason.ScopeDigestMismatch)
                    .SequenceEqual(configured.Sources, StringComparer.Ordinal))
            {
                throw new GateValidationException(GateReason.ScopeDigestMismatch, "primary-lane-policy");
            }
        }
    }

    /// <summary>The per-lane current-run ceiling every declared primary lane must carry, pinned in code.</summary>
    private const int ExpectedLaneCurrentRunAgeMinutes = 360;

    private static bool OptionalPinnedInteger(JsonObject value, string name, int expected)
        => value.TryGetPropertyValue(name, out JsonNode? node)
            && node is JsonValue candidate
            && candidate.TryGetValue(out int actual)
            && actual == expected;

    private static bool OptionalPinnedString(JsonObject value, string name, string? expected)
    {
        if (expected is null)
        {
            return !value.ContainsKey(name);
        }

        return EvidenceJson.RequiredString(value, name, GateReason.ScopeDigestMismatch)
            .Equals(expected, StringComparison.Ordinal);
    }

    private static void ValidateMetadataPolicy(JsonObject policy)
    {
        JsonObject metadata = EvidenceJson.RequiredObject(policy, "metadataOnly", GateReason.ScopeDigestMismatch);
        if (!EvidenceJson.RequiredStrings(metadata, "forbiddenFieldNames", GateReason.ScopeDigestMismatch)
                .SequenceEqual(["secret", "password", "credential", "token", "payload", "prompt"], StringComparer.Ordinal)
            || !EvidenceJson.RequiredStrings(metadata, "allowedArtifactFields", GateReason.ScopeDigestMismatch)
                .SequenceEqual(["artifactLocator", "trxSha256", "implementationDigest"], StringComparer.Ordinal)
            || !EvidenceJson.RequiredStrings(metadata, "allowedLocatorSchemes", GateReason.ScopeDigestMismatch)
                .SequenceEqual(["file", "github-actions"], StringComparer.Ordinal)
            || EvidenceJson.RequiredInteger(metadata, "maximumStringLength", GateReason.ScopeDigestMismatch) != 512
            || !EvidenceJson.RequiredString(metadata, "redactedFailureSubject", GateReason.ScopeDigestMismatch)
                .Equals("redacted", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "metadata-policy");
        }
    }

    private static void ValidateStoryGrammars(JsonObject policy)
    {
        JsonArray grammars = EvidenceJson.RequiredArray(policy, "storyGrammars", GateReason.ScopeDigestMismatch);
        if (grammars.Count != 2)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "story-grammars");
        }

        JsonObject[] grammarObjects = grammars
            .Select(node => node as JsonObject
                ?? throw new GateValidationException(GateReason.ScopeDigestMismatch, "story-grammars"))
            .ToArray();
        if (grammarObjects
                .Select(value => EvidenceJson.RequiredString(value, "name", GateReason.ScopeDigestMismatch))
                .Distinct(StringComparer.Ordinal)
                .Count() != grammarObjects.Length)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "story-grammars");
        }

        Dictionary<string, JsonObject> byName = grammarObjects.ToDictionary(
            value => EvidenceJson.RequiredString(value, "name", GateReason.ScopeDigestMismatch),
            StringComparer.Ordinal);
        ValidateStoryGrammar(
            byName,
            "te-spec",
            "frontmatter:title",
            "frontmatter:status",
            "## Tasks & Acceptance / **Execution:**",
            "## Tasks & Acceptance / **Acceptance Criteria:**",
            "## File List");
        ValidateStoryGrammar(
            byName,
            "bmad-product-story",
            "# Story ...",
            "Status: ...",
            "## Tasks / Subtasks",
            "## Acceptance Criteria",
            "### File List");
    }

    private static void ValidateStoryGrammar(
        IReadOnlyDictionary<string, JsonObject> grammars,
        string name,
        string titleSource,
        string statusSource,
        string tasksSection,
        string acceptanceSection,
        string fileListSection)
    {
        if (!grammars.TryGetValue(name, out JsonObject? grammar)
            || !EvidenceJson.RequiredString(grammar, "titleSource", GateReason.ScopeDigestMismatch)
                .Equals(titleSource, StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(grammar, "statusSource", GateReason.ScopeDigestMismatch)
                .Equals(statusSource, StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(grammar, "tasksSection", GateReason.ScopeDigestMismatch)
                .Equals(tasksSection, StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(grammar, "acceptanceSection", GateReason.ScopeDigestMismatch)
                .Equals(acceptanceSection, StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(grammar, "fileListSection", GateReason.ScopeDigestMismatch)
                .Equals(fileListSection, StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, name);
        }
    }

    private static void ValidateEventResolution(JsonObject policy)
    {
        JsonObject resolution = EvidenceJson.RequiredObject(
            policy,
            "eventBaseHeadResolution",
            GateReason.ScopeDigestMismatch);
        if (!EvidenceJson.RequiredString(resolution, "schemaVersion", GateReason.ScopeDigestMismatch)
                .Equals("1.0", StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(resolution, "pullRequestBase", GateReason.ScopeDigestMismatch)
                .Equals("github.event.pull_request.base.sha", StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(resolution, "pullRequestHead", GateReason.ScopeDigestMismatch)
                .Equals("github.event.pull_request.head.sha", StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(resolution, "pushBase", GateReason.ScopeDigestMismatch)
                .Equals("github.event.before", StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(resolution, "pushHead", GateReason.ScopeDigestMismatch)
                .Equals("github.sha", StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(
                    resolution,
                    "zeroPushBaseFallback",
                    GateReason.ScopeDigestMismatch)
                .Equals("git rev-parse HEAD^", StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(
                    resolution,
                    "unavailableNonZeroPushBase",
                    GateReason.ScopeDigestMismatch)
                .Equals("fail", StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(
                    resolution,
                    "nonPushEventRange",
                    GateReason.ScopeDigestMismatch)
                .Equals("github.sha..github.sha", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "event-base-head-resolution");
        }
    }

    private static void ValidateStatus(GateOptions options, JsonObject contract, StoryRecord story)
    {
        string recordKind = EvidenceJson.RequiredString(contract, "recordKind", GateReason.StatusMismatch);
        string recordLedgerKey = EvidenceJson.RequiredString(contract, "recordLedgerKey", GateReason.StatusMismatch);
        string storyTitle = EvidenceJson.RequiredString(contract, "storyTitle", GateReason.StatusMismatch);
        string storyPath = NormalizePath(EvidenceJson.RequiredString(contract, "storyPath", GateReason.StatusMismatch));
        string actualStoryPath = NormalizePath(Path.GetRelativePath(options.RepositoryRoot, options.StoryPath));
        string targetStatus = EvidenceJson.RequiredString(contract, "targetStatus", GateReason.StatusMismatch);
        string persistedStatus = EvidenceJson.RequiredString(contract, "persistedStatus", GateReason.StatusMismatch);
        string sprintStatusKey = EvidenceJson.RequiredString(contract, "sprintStatusKey", GateReason.StatusMismatch);
        bool bootstrap = EvidenceJson.RequiredBoolean(contract, "bootstrap", GateReason.StatusMismatch);
        string sprintPath = Path.Combine(
            options.RepositoryRoot,
            "_bmad-output",
            "implementation-artifacts",
            "sprint-status.yaml");

        if (!story.Title.Equals(storyTitle, StringComparison.Ordinal)
            || !storyPath.Equals(actualStoryPath, StringComparison.Ordinal)
            || !targetStatus.Equals(options.TargetStatus, StringComparison.Ordinal)
            || !targetStatus.Equals("done", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "story-identity");
        }

        if (recordKind.Equals("technicalEnabler", StringComparison.Ordinal))
        {
            string actionStatus = SprintLedgerReader.ActionStatus(sprintPath, sprintStatusKey) ?? string.Empty;
            string technicalLedgerPath = Path.Combine(
                options.RepositoryRoot,
                "_bmad-output",
                "planning-artifacts",
                "technical-enablers.md");
            string ledgerStatus = TechnicalEnablerLedgerReader.Status(technicalLedgerPath, recordLedgerKey) ?? string.Empty;
            if (bootstrap)
            {
                if (!persistedStatus.Equals("complete", StringComparison.Ordinal)
                    || !(story.Status.Equals("in-progress", StringComparison.Ordinal)
                        || story.Status.Equals("review", StringComparison.Ordinal)
                        || story.Status.Equals("in-review", StringComparison.Ordinal))
                    || !ledgerStatus.Equals("review", StringComparison.Ordinal)
                    || !actionStatus.Equals("open", StringComparison.Ordinal))
                {
                    throw new GateValidationException(GateReason.StatusMismatch, "technical-enabler-bootstrap");
                }

                return;
            }

            string? baseTechnicalStory = GitReader.Show(options.RepositoryRoot, options.BaseCommit, storyPath);
            string? baseLedger = GitReader.Show(
                options.RepositoryRoot,
                options.BaseCommit,
                "_bmad-output/planning-artifacts/technical-enablers.md");
            string? baseSprint = GitReader.Show(
                options.RepositoryRoot,
                options.BaseCommit,
                "_bmad-output/implementation-artifacts/sprint-status.yaml");
            if (!persistedStatus.Equals("complete", StringComparison.Ordinal)
                || !story.Status.Equals(persistedStatus, StringComparison.Ordinal)
                || !ledgerStatus.Equals("complete", StringComparison.Ordinal)
                || !actionStatus.Equals("done", StringComparison.Ordinal)
                || baseTechnicalStory is null
                || MarkdownStoryReader.ReadStatus(baseTechnicalStory).Equals(persistedStatus, StringComparison.Ordinal)
                || baseLedger is null
                || string.Equals(
                    TechnicalEnablerLedgerReader.StatusFromText(baseLedger, recordLedgerKey),
                    "complete",
                    StringComparison.Ordinal)
                || baseSprint is null
                || string.Equals(
                    SprintLedgerReader.ActionStatusFromText(baseSprint, sprintStatusKey),
                    "done",
                    StringComparison.Ordinal))
            {
                throw new GateValidationException(GateReason.StatusMismatch, "technical-enabler-transition");
            }

            return;
        }

        if (!recordKind.Equals("story", StringComparison.Ordinal)
            || !recordLedgerKey.Equals(sprintStatusKey, StringComparison.Ordinal)
            || bootstrap
            || !persistedStatus.Equals("done", StringComparison.Ordinal)
            || !story.Status.Equals("done", StringComparison.Ordinal)
            || !string.Equals(SprintLedgerReader.StoryStatus(sprintPath, sprintStatusKey), "done", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "story-transition");
        }

        string? baseStory = GitReader.Show(options.RepositoryRoot, options.BaseCommit, storyPath);
        string? baseProductSprint = GitReader.Show(
            options.RepositoryRoot,
            options.BaseCommit,
            "_bmad-output/implementation-artifacts/sprint-status.yaml");
        if (baseStory is null
            || !MarkdownStoryReader.ReadStatus(baseStory).Equals("review", StringComparison.Ordinal)
            || baseProductSprint is null
            || !string.Equals(
                SprintLedgerReader.StoryStatusFromText(baseProductSprint, sprintStatusKey),
                "review",
                StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "transition-base");
        }
    }

    private static List<LaneResult> ReadResults(
        GateOptions options,
        JsonObject policy,
        JsonObject contract,
        string implementationDigest)
    {
        JsonArray results = EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid);
        if (results.Count == 0)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, "results");
        }

        List<LaneResult> lanes = [];
        foreach (JsonNode? node in results)
        {
            JsonObject laneContract = node as JsonObject
                ?? throw new GateValidationException(GateReason.MachineResultsInvalid, "results");
            string source = EvidenceJson.RequiredString(laneContract, "source", GateReason.EvidenceStaleOrUnbound);
            if (source is not ("current-run" or "retained"))
            {
                throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, "source");
            }

            lanes.Add(TrxEvidenceReader.Read(
                laneContract,
                options.ResultsRoot,
                options.BaseCommit,
                options.HeadCommit,
                implementationDigest,
                EvidenceJson.RequiredString(policy, "repositoryIdentity", GateReason.EvidenceStaleOrUnbound),
                EvidenceJson.ResolveCurrentRunAgeMinutes(
                    policy,
                    EvidenceJson.RequiredString(laneContract, "lane", GateReason.MachineResultsInvalid)),
                EvidenceJson.RequiredInteger(policy, "maximumRetainedEvidenceAgeHours", GateReason.EvidenceStaleOrUnbound),
                EvidenceJson.RequiredInteger(policy, "maximumFutureClockSkewMinutes", GateReason.EvidenceStaleOrUnbound),
                options.NowUtc));
        }

        if (lanes.Select(static lane => lane.Lane).Distinct(StringComparer.Ordinal).Count() != lanes.Count)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, "duplicate-lane");
        }

        return lanes;
    }

    private static IReadOnlyList<PrimaryPathVerdict> ValidatePrimaryPaths(
        JsonObject policy,
        JsonObject contract,
        StoryRecord story,
        IReadOnlySet<string> changedPaths,
        IReadOnlyList<LaneResult> lanes)
    {
        Dictionary<string, IReadOnlySet<string>> recognizedLanes = new(StringComparer.Ordinal);
        Dictionary<string, (
            string Selector,
            string? Trx,
            string? Provenance,
            IReadOnlySet<string> Sources)> recognizedBindings = new(StringComparer.Ordinal);
        HashSet<string> requiredClasses = new(StringComparer.Ordinal);
        JsonArray triggers = EvidenceJson.RequiredArray(
            policy,
            "primaryPathTriggers",
            GateReason.PrimaryPathNotExecuted);
        foreach (JsonNode? node in triggers)
        {
            JsonObject trigger = node as JsonObject
                ?? throw new GateValidationException(GateReason.PrimaryPathNotExecuted, "primaryPathTriggers");
            string pathClass = EvidenceJson.RequiredString(trigger, "class", GateReason.PrimaryPathNotExecuted);
            IReadOnlyList<string> patterns = EvidenceJson.RequiredStrings(
                trigger,
                "pathPatterns",
                GateReason.PrimaryPathNotExecuted);
            recognizedLanes[pathClass] = EvidenceJson
                .RequiredStrings(trigger, "recognizedLanes", GateReason.PrimaryPathNotExecuted)
                .ToHashSet(StringComparer.Ordinal);
            JsonObject binding = EvidenceJson.RequiredArray(
                trigger,
                "recognizedLaneBindings",
                GateReason.PrimaryPathNotExecuted)[0]!.AsObject();
            recognizedBindings[pathClass] = (
                EvidenceJson.RequiredString(binding, "selector", GateReason.PrimaryPathNotExecuted),
                binding["trx"] is null
                    ? null
                    : EvidenceJson.RequiredString(binding, "trx", GateReason.PrimaryPathNotExecuted),
                binding["provenance"] is null
                    ? null
                    : EvidenceJson.RequiredString(binding, "provenance", GateReason.PrimaryPathNotExecuted),
                EvidenceJson.RequiredStrings(binding, "sources", GateReason.PrimaryPathNotExecuted)
                    .ToHashSet(StringComparer.Ordinal));
            IReadOnlyList<string> claims = EvidenceJson.RequiredStrings(
                trigger,
                "claimPatterns",
                GateReason.PrimaryPathNotExecuted);
            if (changedPaths.Any(path => patterns.Any(pattern => GlobMatch(path, pattern)))
                || claims.Any(claim => story.EvidenceText.Contains(claim, StringComparison.OrdinalIgnoreCase)))
            {
                requiredClasses.Add(pathClass);
            }
        }

        Dictionary<string, string> declarations = new(StringComparer.Ordinal);
        JsonArray primaryPaths = EvidenceJson.RequiredArray(contract, "primaryPaths", GateReason.PrimaryPathNotExecuted);
        foreach (JsonNode? node in primaryPaths)
        {
            JsonObject declaration = node as JsonObject
                ?? throw new GateValidationException(GateReason.PrimaryPathNotExecuted, "primaryPaths");
            string pathClass = EvidenceJson.RequiredString(declaration, "class", GateReason.PrimaryPathNotExecuted);
            string lane = EvidenceJson.RequiredString(declaration, "lane", GateReason.PrimaryPathNotExecuted);
            if (!declarations.TryAdd(pathClass, lane))
            {
                throw new GateValidationException(GateReason.PrimaryPathNotExecuted, pathClass);
            }

            requiredClasses.Add(pathClass);
        }

        List<PrimaryPathVerdict> verdicts = [];
        JsonArray resultContracts = EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid);
        foreach (string requiredClass in requiredClasses.Order(StringComparer.Ordinal))
        {
            JsonObject? laneContract = resultContracts
                .OfType<JsonObject>()
                .SingleOrDefault(candidate => declarations.TryGetValue(requiredClass, out string? declared)
                    && EvidenceJson.RequiredString(candidate, "lane", GateReason.MachineResultsInvalid)
                        .Equals(declared, StringComparison.Ordinal));
            if (!declarations.TryGetValue(requiredClass, out string? laneName)
                || !recognizedLanes.TryGetValue(requiredClass, out IReadOnlySet<string>? allowed)
                || !allowed.Contains(laneName)
                || !recognizedBindings.TryGetValue(requiredClass, out var binding)
                || lanes.SingleOrDefault(lane => lane.Lane.Equals(laneName, StringComparison.Ordinal)) is not LaneResult lane
                || laneContract is null
                || (binding.Trx is not null
                    && !EvidenceJson.RequiredString(laneContract, "trx", GateReason.PrimaryPathNotExecuted)
                        .Equals(binding.Trx, StringComparison.Ordinal))
                || (binding.Provenance is not null
                    && !EvidenceJson.RequiredString(laneContract, "provenance", GateReason.PrimaryPathNotExecuted)
                        .Equals(binding.Provenance, StringComparison.Ordinal))
                || !string.Equals(lane.PrimaryPathClass, requiredClass, StringComparison.Ordinal)
                || !lane.Selectors.Contains(binding.Selector, StringComparer.Ordinal)
                || !binding.Sources.Contains(lane.Source)
                || lane.Executed <= 0
                || lane.Failed != 0
                || lane.Skipped != 0)
            {
                throw new GateValidationException(GateReason.PrimaryPathNotExecuted, requiredClass);
            }

            verdicts.Add(new PrimaryPathVerdict(requiredClass, true, laneName));
        }

        foreach (LaneResult lane in lanes.Where(static lane => lane.PrimaryPathClass is not null))
        {
            if (!declarations.TryGetValue(lane.PrimaryPathClass!, out string? declaredLane)
                || !declaredLane.Equals(lane.Lane, StringComparison.Ordinal))
            {
                throw new GateValidationException(GateReason.PrimaryPathNotExecuted, lane.PrimaryPathClass!);
            }
        }

        return verdicts;
    }

    private static (int CheckedItems, int MappedItems) ValidateMappings(
        JsonObject contract,
        StoryRecord story,
        IReadOnlySet<string> changedPaths,
        IReadOnlyList<LaneResult> lanes)
    {
        if (!story.MandatoryItems.SetEquals(story.CheckedItems))
        {
            throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, "unchecked-mandatory-item");
        }

        HashSet<string> passingAssertions = lanes
            .SelectMany(static lane => lane.PassedTests)
            .ToHashSet(StringComparer.Ordinal);
        JsonArray mappings = EvidenceJson.RequiredArray(
            contract,
            "mappings",
            GateReason.CheckedItemEvidenceMismatch);
        HashSet<string> mappedIds = new(StringComparer.Ordinal);
        foreach (JsonNode? node in mappings)
        {
            JsonObject mapping = node as JsonObject
                ?? throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, "mappings");
            string id = EvidenceJson.RequiredString(mapping, "id", GateReason.CheckedItemEvidenceMismatch);
            string kind = EvidenceJson.RequiredString(mapping, "kind", GateReason.CheckedItemEvidenceMismatch);
            IReadOnlyList<string> paths = EvidenceJson.RequiredStrings(
                mapping,
                "paths",
                GateReason.CheckedItemEvidenceMismatch);
            IReadOnlyList<string> assertions = EvidenceJson.RequiredStrings(
                mapping,
                "assertions",
                GateReason.CheckedItemEvidenceMismatch);
            if (!mappedIds.Add(id)
                || !story.MandatoryItems.Contains(id)
                || (kind.Equals("task", StringComparison.Ordinal) != id.StartsWith("task-", StringComparison.Ordinal))
                || (kind.Equals("acceptanceCriterion", StringComparison.Ordinal) != id.StartsWith("ac-", StringComparison.Ordinal))
                || (paths.Count == 0 && assertions.Count == 0)
                || paths.Any(path => !changedPaths.Contains(NormalizePath(path)))
                || assertions.Any(assertion => !passingAssertions.Contains(assertion)))
            {
                throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, id);
            }
        }

        if (!mappedIds.SetEquals(story.MandatoryItems))
        {
            throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, "mapping-coverage");
        }

        return (story.CheckedItems.Count, mappedIds.Count);
    }

    private static void ValidateMappingDeclarations(
        JsonObject contract,
        StoryRecord story,
        IReadOnlySet<string> changedPaths)
    {
        JsonArray mappings = EvidenceJson.RequiredArray(
            contract,
            "mappings",
            GateReason.CheckedItemEvidenceMismatch);
        HashSet<string> mappedIds = new(StringComparer.Ordinal);
        foreach (JsonNode? node in mappings)
        {
            JsonObject mapping = node as JsonObject
                ?? throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, "mappings");
            string id = EvidenceJson.RequiredString(mapping, "id", GateReason.CheckedItemEvidenceMismatch);
            string kind = EvidenceJson.RequiredString(mapping, "kind", GateReason.CheckedItemEvidenceMismatch);
            IReadOnlyList<string> paths = EvidenceJson.RequiredStrings(
                mapping,
                "paths",
                GateReason.CheckedItemEvidenceMismatch);
            IReadOnlyList<string> assertions = EvidenceJson.RequiredStrings(
                mapping,
                "assertions",
                GateReason.CheckedItemEvidenceMismatch);
            if (!mappedIds.Add(id)
                || !story.MandatoryItems.Contains(id)
                || (kind.Equals("task", StringComparison.Ordinal) != id.StartsWith("task-", StringComparison.Ordinal))
                || (kind.Equals("acceptanceCriterion", StringComparison.Ordinal) != id.StartsWith("ac-", StringComparison.Ordinal))
                || (paths.Count == 0 && assertions.Count == 0)
                || paths.Any(path => !changedPaths.Contains(NormalizePath(path))))
            {
                throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, id);
            }
        }

        if (!mappedIds.SetEquals(story.MandatoryItems))
        {
            throw new GateValidationException(GateReason.CheckedItemEvidenceMismatch, "mapping-coverage");
        }
    }

    private static bool GlobMatch(string path, string pattern)
    {
        string normalized = pattern.Replace('\\', '/');
        StringBuilder expression = new("^");
        for (int index = 0; index < normalized.Length; index++)
        {
            if (normalized[index] == '*' && index + 1 < normalized.Length && normalized[index + 1] == '*')
            {
                bool followedBySlash = index + 2 < normalized.Length && normalized[index + 2] == '/';
                expression.Append(followedBySlash ? "(?:.*/)?" : ".*");
                index += followedBySlash ? 2 : 1;
            }
            else if (normalized[index] == '*')
            {
                expression.Append("[^/]*");
            }
            else if (normalized[index] == '?')
            {
                expression.Append("[^/]");
            }
            else
            {
                expression.Append(Regex.Escape(normalized[index].ToString()));
            }
        }

        expression.Append('$');
        return Regex.IsMatch(path, expression.ToString(), RegexOptions.CultureInvariant);
    }

    private static string NormalizePath(string path)
    {
        string normalized = path.Replace('\\', '/');
        if (normalized.StartsWith("/", StringComparison.Ordinal)
            || (normalized.Length >= 3 && char.IsLetter(normalized[0]) && normalized[1] == ':' && normalized[2] == '/')
            || normalized.Split('/').Any(static segment => segment.Length == 0 || segment is "." or ".."))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "path");
        }

        return normalized;
    }

    private static string SafeRevision(string revision)
    {
        return revision.Length is 40 or 64 && revision.All(Uri.IsHexDigit)
            ? revision.ToLowerInvariant()
            : "redacted";
    }
}
