using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Reconciles explicit repository scopes and computes their canonical SHA-256 digest.
/// </summary>
public static partial class ScopeEvaluator
{
    /// <summary>Evaluates the exact contract scope.</summary>
    /// <param name="repositoryRoot">The root repository path.</param>
    /// <param name="contract">The strict evidence contract.</param>
    /// <param name="baseCommit">The CLI base revision.</param>
    /// <param name="headCommit">The CLI head revision.</param>
    /// <returns>The reconciled scope.</returns>
    public static ScopeEvaluation Evaluate(
        string repositoryRoot,
        JsonObject policy,
        JsonObject contract,
        string baseCommit,
        string headCommit)
    {
        JsonObject scopeNode = EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch);
        string mode = EvidenceJson.RequiredString(scopeNode, "mode", GateReason.ScopeDigestMismatch);
        IReadOnlyList<string> allowedModes = EvidenceJson.RequiredStrings(
            policy,
            "allowedScopeModes",
            GateReason.ScopeDigestMismatch);
        if (!allowedModes.SequenceEqual(["diff", "snapshot-plus-transition"], StringComparer.Ordinal)
            || !allowedModes.Contains(mode, StringComparer.Ordinal))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "scope-mode");
        }

        string[] transitionPaths = EvidenceJson.RequiredStrings(
                scopeNode,
                "transitionPaths",
                GateReason.ScopeDigestMismatch)
            .Select(NormalizeFilePath)
            .ToArray();
        if (transitionPaths.Distinct(StringComparer.Ordinal).Count() != transitionPaths.Length)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "transition-paths");
        }

        IReadOnlyList<RepositoryScope> scopes = ParseScopes(scopeNode);
        ValidateRepositoryGraph(repositoryRoot, scopes);

        RepositoryScope rootScope = scopes.SingleOrDefault(static scope => scope.Path == ".")
            ?? throw new GateValidationException(GateReason.ScopeDigestMismatch, "root-scope");
        if (mode.Equals("snapshot-plus-transition", StringComparison.Ordinal)
            && transitionPaths.Any(path => !rootScope.IncludePaths.Contains(path)))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "transition-path-ownership");
        }
        string normalizedBase = GitReader.ResolveExactCommit(repositoryRoot, baseCommit);
        string normalizedHead = GitReader.ResolveExactCommit(repositoryRoot, headCommit);
        string boundRootBase = BindRootRevision(rootScope.BaseCommit, "$BASE", normalizedBase);
        string boundRootHead = BindRootRevision(rootScope.HeadCommit, "$HEAD", normalizedHead);
        if (!boundRootBase.Equals(normalizedBase, StringComparison.OrdinalIgnoreCase)
            || !boundRootHead.Equals(normalizedHead, StringComparison.OrdinalIgnoreCase)
            || !GitReader.Head(repositoryRoot).Equals(normalizedHead, StringComparison.OrdinalIgnoreCase))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "root-revision");
        }

        Dictionary<string, string> disclosures = ParseDisclosures(contract);
        if (mode.Equals("snapshot-plus-transition", StringComparison.Ordinal) && disclosures.Count != 0)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "snapshot-disclosures");
        }
        string reportPath = NormalizeFilePath(EvidenceJson.RequiredString(
            contract,
            "reportPath",
            GateReason.ScopeDigestMismatch));
        IReadOnlyList<string> reportPrefixes = EvidenceJson.RequiredStrings(
            policy,
            "reportExcludedPaths",
            GateReason.ScopeDigestMismatch);
        string storyKey = EvidenceJson.RequiredStoryKey(contract);
        string evidenceContractPath = $"_bmad-output/implementation-artifacts/evidence/{storyKey}.json";
        if (reportPrefixes.Count != 1
            || !reportPath.Equals($"{reportPrefixes[0]}{storyKey}.json", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "report-path");
        }

        Dictionary<string, (string Base, string Head)> resolvedRevisions = new(StringComparer.Ordinal);
        foreach (RepositoryScope repositoryScope in scopes)
        {
            string repositoryPath = repositoryScope.Path == "."
                ? repositoryRoot
                : Path.Combine(repositoryRoot, repositoryScope.Path);
            string configuredBase = repositoryScope.Path == "."
                ? BindRootRevision(repositoryScope.BaseCommit, "$BASE", normalizedBase)
                : repositoryScope.BaseCommit;
            string configuredHead = repositoryScope.Path == "."
                ? BindRootRevision(repositoryScope.HeadCommit, "$HEAD", normalizedHead)
                : repositoryScope.HeadCommit;
            string resolvedBase = GitReader.ResolveExactCommit(repositoryPath, configuredBase);
            string resolvedHead = GitReader.ResolveExactCommit(repositoryPath, configuredHead);
            if (!GitReader.Head(repositoryPath).Equals(resolvedHead, StringComparison.OrdinalIgnoreCase))
            {
                throw new GateValidationException(GateReason.ScopeDigestMismatch, repositoryScope.Name);
            }

            resolvedRevisions[repositoryScope.Name] = (resolvedBase, resolvedHead);
        }

        ValidateGitlinks(repositoryRoot, scopes, resolvedRevisions);
        List<ChangedPath> eventPaths = [];
        List<ChangedPath> ownedChanges = [];
        foreach (RepositoryScope repositoryScope in scopes)
        {
            string repositoryPath = repositoryScope.Path == "."
                ? repositoryRoot
                : Path.Combine(repositoryRoot, repositoryScope.Path);
            (string resolvedBase, string resolvedHead) = resolvedRevisions[repositoryScope.Name];

            Dictionary<string, string> committedChanges = new(
                GitReader.Diff(repositoryPath, resolvedBase, resolvedHead),
                StringComparer.Ordinal);
            if (mode.Equals("snapshot-plus-transition", StringComparison.Ordinal))
            {
                string? unownedEventPath = committedChanges.Keys
                    .Where(path => !repositoryScope.IncludePaths.Contains(path))
                    .Order(StringComparer.Ordinal)
                    .FirstOrDefault();
                if (unownedEventPath is not null)
                {
                    throw new GateValidationException(
                        GateReason.ScopeDigestMismatch,
                        $"{repositoryScope.Name}:{unownedEventPath}");
                }
            }

            foreach ((string path, string status) in committedChanges.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            {
                eventPaths.Add(CreateEventPath(
                    repositoryPath,
                    repositoryScope.Name,
                    path,
                    status,
                    resolvedBase,
                    resolvedHead));
            }

            if (mode.Equals("snapshot-plus-transition", StringComparison.Ordinal))
            {
                if (repositoryScope.IncludeWorkingTree)
                {
                    throw new GateValidationException(GateReason.ScopeDigestMismatch, "snapshot-working-tree");
                }

                IReadOnlyDictionary<string, string> dirtyPaths = GitReader.WorktreeDiff(repositoryPath, resolvedHead);
                if (dirtyPaths.Count != 0)
                {
                    string dirtyPath = dirtyPaths.Keys.Order(StringComparer.Ordinal).First();
                    throw new GateValidationException(
                        GateReason.ScopeDigestMismatch,
                        $"{repositoryScope.Name}:{dirtyPath}");
                }

                foreach (string path in repositoryScope.IncludePaths.Order(StringComparer.Ordinal))
                {
                    ownedChanges.Add(CreateChangedPath(
                        repositoryPath,
                        repositoryScope.Name,
                        path,
                        committedChanges.GetValueOrDefault(path, "S"),
                        resolvedHead,
                        immutable: true,
                        maskImplementationDigest: repositoryScope.Path == "."
                            && path.Equals(evidenceContractPath, StringComparison.Ordinal)));
                }

                continue;
            }

            if (!repositoryScope.IncludeWorkingTree)
            {
                IReadOnlyDictionary<string, string> dirtyPaths = GitReader.WorktreeDiff(repositoryPath, resolvedHead);
                if (dirtyPaths.Count != 0)
                {
                    string dirtyPath = dirtyPaths.Keys.Order(StringComparer.Ordinal).First();
                    throw new GateValidationException(
                        GateReason.ScopeDigestMismatch,
                        $"{repositoryScope.Name}:{dirtyPath}");
                }
            }

            Dictionary<string, string> allChanges = new(committedChanges, StringComparer.Ordinal);
            if (repositoryScope.IncludeWorkingTree)
            {
                foreach ((string path, string status) in GitReader.WorktreeDiff(repositoryPath, resolvedHead))
                {
                    allChanges[path] = status;
                }
            }

            foreach ((string path, string status) in allChanges.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            {
                if (repositoryScope.IncludePaths.Contains(path))
                {
                    ownedChanges.Add(CreateChangedPath(
                        repositoryPath,
                        repositoryScope.Name,
                        path,
                        status,
                        resolvedHead,
                        immutable: !repositoryScope.IncludeWorkingTree,
                        maskImplementationDigest: repositoryScope.Path == "."
                            && path.Equals(evidenceContractPath, StringComparison.Ordinal)));
                    continue;
                }

                string disclosureKey = $"{repositoryScope.Name}:{path}";
                if (repositoryScope.Path == "." && path.Equals(reportPath, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!repositoryScope.IncludeWorkingTree
                    || committedChanges.ContainsKey(path)
                    || !disclosures.TryGetValue(disclosureKey, out string? classification)
                    || !classification.Equals("preExistingLocalChange", StringComparison.Ordinal))
                {
                    throw new GateValidationException(GateReason.ScopeDigestMismatch, disclosureKey);
                }
            }

            foreach (string expectedPath in repositoryScope.IncludePaths)
            {
                if (!allChanges.ContainsKey(expectedPath))
                {
                    throw new GateValidationException(GateReason.FileListDiffMismatch, expectedPath);
                }
            }
        }

        ValidateCurrentGitlinks(scopes, ownedChanges, resolvedRevisions);
        string digest = ComputeDigest(ownedChanges);
        return new ScopeEvaluation(scopes, ownedChanges, eventPaths, digest);
    }

    /// <summary>Gets the path strings used to reconcile the story File List.</summary>
    /// <param name="evaluation">The evaluated scope.</param>
    /// <returns>Root-relative paths, with submodule paths prefixed.</returns>
    public static IReadOnlySet<string> FileListPaths(ScopeEvaluation evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return RootRelativePaths(evaluation, evaluation.ChangedPaths);
    }

    /// <summary>Gets the base-to-head event paths used to evaluate primary-path triggers.</summary>
    /// <param name="evaluation">The evaluated scope.</param>
    /// <returns>Root-relative event paths, with submodule paths prefixed.</returns>
    public static IReadOnlySet<string> EventPaths(ScopeEvaluation evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return RootRelativePaths(evaluation, evaluation.EventPaths);
    }

    private static IReadOnlySet<string> RootRelativePaths(
        ScopeEvaluation evaluation,
        IReadOnlyList<ChangedPath> paths)
    {
        Dictionary<string, string> roots = evaluation.Scopes.ToDictionary(
            static scope => scope.Name,
            static scope => scope.Path,
            StringComparer.Ordinal);
        return paths
            .Select(change => roots[change.Repository] == "."
                ? change.Path
                : $"{roots[change.Repository]}/{change.Path}")
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IReadOnlyList<RepositoryScope> ParseScopes(JsonObject scopeNode)
    {
        JsonArray repositories = EvidenceJson.RequiredArray(scopeNode, "repositories", GateReason.ScopeDigestMismatch);
        List<RepositoryScope> result = [];
        foreach (JsonNode? node in repositories)
        {
            JsonObject value = node as JsonObject
                ?? throw new GateValidationException(GateReason.ScopeDigestMismatch, "repositories");
            string path = NormalizeRepositoryPath(EvidenceJson.RequiredString(value, "path", GateReason.ScopeDigestMismatch));
            IReadOnlyList<string> rawIncludePaths = EvidenceJson.RequiredStrings(
                value,
                "includePaths",
                GateReason.ScopeDigestMismatch);
            string[] normalizedIncludePaths = rawIncludePaths.Select(NormalizeFilePath).ToArray();
            HashSet<string> includePaths = normalizedIncludePaths.ToHashSet(StringComparer.Ordinal);
            if (includePaths.Count == 0 || includePaths.Count != normalizedIncludePaths.Length)
            {
                throw new GateValidationException(GateReason.ScopeDigestMismatch, "includePaths");
            }

            result.Add(new RepositoryScope(
                EvidenceJson.RequiredString(value, "name", GateReason.ScopeDigestMismatch),
                path,
                EvidenceJson.RequiredString(value, "baseCommit", GateReason.ScopeDigestMismatch),
                EvidenceJson.RequiredString(value, "headCommit", GateReason.ScopeDigestMismatch),
                EvidenceJson.RequiredBoolean(value, "includeWorkingTree", GateReason.ScopeDigestMismatch),
                includePaths));
        }

        if (result.Select(static scope => scope.Name).Distinct(StringComparer.Ordinal).Count() != result.Count
            || result.Select(static scope => scope.Path).Distinct(StringComparer.Ordinal).Count() != result.Count)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "duplicate-repository-scope");
        }

        return result;
    }

    private static void ValidateRepositoryGraph(string root, IReadOnlyList<RepositoryScope> scopes)
    {
        HashSet<string> declared = [];
        string modulesPath = Path.Combine(root, ".gitmodules");
        if (File.Exists(modulesPath))
        {
            foreach (string line in File.ReadLines(modulesPath))
            {
                Match match = SubmodulePath().Match(line);
                if (match.Success)
                {
                    string declaredPath = NormalizeRepositoryPath(match.Groups[1].Value);
                    if (!declared.Add(declaredPath))
                    {
                        throw new GateValidationException(GateReason.GitlinkScopeMismatch, "duplicate-gitmodule-path");
                    }
                }
            }
        }

        foreach (RepositoryScope scope in scopes.Where(static scope => scope.Path != "."))
        {
            if (!declared.Contains(scope.Path))
            {
                throw new GateValidationException(GateReason.GitlinkScopeMismatch, scope.Path);
            }
        }
    }

    private static Dictionary<string, string> ParseDisclosures(JsonObject contract)
    {
        Dictionary<string, string> result = new(StringComparer.Ordinal);
        JsonArray disclosures = EvidenceJson.RequiredArray(
            contract,
            "outOfScopeDisclosures",
            GateReason.ScopeDigestMismatch);
        foreach (JsonNode? node in disclosures)
        {
            JsonObject value = node as JsonObject
                ?? throw new GateValidationException(GateReason.ScopeDigestMismatch, "outOfScopeDisclosures");
            string repository = EvidenceJson.RequiredString(value, "repository", GateReason.ScopeDigestMismatch);
            string path = NormalizeFilePath(EvidenceJson.RequiredString(value, "path", GateReason.ScopeDigestMismatch));
            _ = EvidenceJson.RequiredString(value, "owner", GateReason.ScopeDigestMismatch);
            _ = EvidenceJson.RequiredString(value, "reason", GateReason.ScopeDigestMismatch);
            string classification = EvidenceJson.RequiredString(value, "classification", GateReason.ScopeDigestMismatch);
            if (!result.TryAdd($"{repository}:{path}", classification))
            {
                throw new GateValidationException(GateReason.ScopeDigestMismatch, path);
            }
        }

        return result;
    }

    private static ChangedPath CreateChangedPath(
        string repositoryPath,
        string repositoryName,
        string path,
        string status,
        string resolvedHead,
        bool immutable,
        bool maskImplementationDigest)
    {
        string fullPath = Path.GetFullPath(Path.Combine(repositoryPath, path));
        if (!fullPath.StartsWith(Path.GetFullPath(repositoryPath) + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !fullPath.Equals(Path.GetFullPath(repositoryPath), StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, path);
        }

        if (status.Equals("D", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.FileListDiffMismatch, path);
        }

        if (immutable)
        {
            (string Mode, string ObjectId)? entry = GitReader.TreeEntry(repositoryPath, resolvedHead, path);
            if (entry is null)
            {
                throw new GateValidationException(GateReason.FileListDiffMismatch, path);
            }

            if (entry.Value.Mode.Equals("160000", StringComparison.Ordinal))
            {
                return new ChangedPath(repositoryName, path, status, entry.Value.Mode, entry.Value.ObjectId);
            }

            if (entry.Value.Mode is not ("100644" or "100755" or "120000"))
            {
                throw new GateValidationException(GateReason.ScopeDigestMismatch, path);
            }

            byte[] treeBytes = GitReader.BlobBytes(repositoryPath, entry.Value.ObjectId);
            byte[] canonicalTreeBytes = CanonicalBytes(treeBytes, maskImplementationDigest);
            string treeObjectId = Convert.ToHexString(SHA256.HashData(canonicalTreeBytes)).ToLowerInvariant();
            return new ChangedPath(repositoryName, path, status, entry.Value.Mode, treeObjectId);
        }

        FileInfo fileInfo = new(fullPath);
        string? linkTarget = fileInfo.LinkTarget;
        if (linkTarget is not null)
        {
            if (linkTarget.Length == 0 || linkTarget.Length > 4096 || linkTarget.Any(char.IsControl))
            {
                throw new GateValidationException(GateReason.ScopeDigestMismatch, path);
            }

            byte[] linkBytes = Encoding.UTF8.GetBytes(linkTarget);
            string linkObjectId = Convert.ToHexString(SHA256.HashData(linkBytes)).ToLowerInvariant();
            return new ChangedPath(repositoryName, path, status, "120000", linkObjectId);
        }

        if (Directory.Exists(fullPath)
            && (Directory.Exists(Path.Combine(fullPath, ".git")) || File.Exists(Path.Combine(fullPath, ".git"))))
        {
            return new ChangedPath(repositoryName, path, status, "160000", GitReader.Head(fullPath));
        }

        if (!File.Exists(fullPath))
        {
            throw new GateValidationException(GateReason.FileListDiffMismatch, path);
        }

        byte[] bytes = CanonicalBytes(File.ReadAllBytes(fullPath), maskImplementationDigest);
        string objectId = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        string mode = GitReader.IndexMode(repositoryPath, path) ?? GetUntrackedMode(fullPath);
        if (mode is not ("100644" or "100755"))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, path);
        }

        return new ChangedPath(repositoryName, path, status, mode, objectId);
    }

    private static ChangedPath CreateEventPath(
        string repositoryPath,
        string repositoryName,
        string path,
        string status,
        string resolvedBase,
        string resolvedHead)
    {
        (string Mode, string ObjectId)? entry = GitReader.TreeEntry(repositoryPath, resolvedHead, path)
            ?? GitReader.TreeEntry(repositoryPath, resolvedBase, path);
        return new ChangedPath(
            repositoryName,
            path,
            status,
            entry?.Mode ?? string.Empty,
            entry?.ObjectId ?? string.Empty);
    }

    private static byte[] CanonicalBytes(
        byte[] bytes,
        bool maskImplementationDigest)
    {
        if (!maskImplementationDigest)
        {
            return bytes;
        }

        try
        {
            return MaskImplementationDigest(bytes);
        }
        catch (JsonException)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "implementation-digest-mask");
        }
    }

    private static byte[] MaskImplementationDigest(byte[] bytes)
    {
        Utf8JsonReader reader = new(bytes, new JsonReaderOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
        });
        int scopeDepth = -1;
        bool nextObjectIsScope = false;
        bool implementationDigestProperty = false;
        List<(int Start, int End)> spans = [];
        int found = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string name = reader.GetString() ?? string.Empty;
                nextObjectIsScope = scopeDepth < 0
                    && reader.CurrentDepth == 1
                    && name.Equals("scope", StringComparison.Ordinal);
                implementationDigestProperty = scopeDepth >= 0
                    && reader.CurrentDepth == scopeDepth + 1
                    && name.Equals("implementationDigest", StringComparison.Ordinal);
                continue;
            }

            if (nextObjectIsScope)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    scopeDepth = reader.CurrentDepth;
                }

                nextObjectIsScope = false;
            }

            if (implementationDigestProperty)
            {
                if (reader.TokenType != JsonTokenType.String || ++found != 1)
                {
                    throw new JsonException("Invalid implementation digest field.");
                }

                spans.Add((checked((int)reader.TokenStartIndex), checked((int)reader.BytesConsumed)));
                implementationDigestProperty = false;
            }

            if (scopeDepth >= 0 && reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == scopeDepth)
            {
                scopeDepth = -1;
            }
        }

        if (found != 1)
        {
            throw new JsonException("Missing implementation digest field.");
        }

        byte[] replacement = "\"implementation-digest-masked\""u8.ToArray();
        using MemoryStream output = new();
        int offset = 0;
        foreach ((int start, int end) in spans.OrderBy(static span => span.Start))
        {
            output.Write(bytes, offset, start - offset);
            output.Write(replacement);
            offset = end;
        }

        output.Write(bytes, offset, bytes.Length - offset);
        return output.ToArray();
    }

    private static string ComputeDigest(IEnumerable<ChangedPath> changes)
    {
        string canonical = string.Join(
            "\n",
            changes
                .OrderBy(static change => change.Repository, StringComparer.Ordinal)
                .ThenBy(static change => change.Path, StringComparer.Ordinal)
                .ThenBy(static change => change.Mode, StringComparer.Ordinal)
                .ThenBy(static change => change.ObjectId, StringComparer.Ordinal)
                .Select(static change =>
                    $"{change.Repository}\0{change.Path}\0{change.Mode}\0{change.ObjectId}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static string GetUntrackedMode(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return "100644";
        }

        UnixFileMode mode = File.GetUnixFileMode(path);
        return (mode & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0
            ? "100755"
            : "100644";
    }

    private static void ValidateGitlinks(
        string repositoryRoot,
        IReadOnlyList<RepositoryScope> scopes,
        IReadOnlyDictionary<string, (string Base, string Head)> revisions)
    {
        RepositoryScope root = scopes.Single(static scope => scope.Path == ".");
        (string rootBase, string rootHead) = revisions[root.Name];
        foreach (RepositoryScope submodule in scopes.Where(static scope => scope.Path != "."))
        {
            (string submoduleBase, string submoduleHead) = revisions[submodule.Name];
            string? baseGitlink = GitReader.Gitlink(repositoryRoot, rootBase, submodule.Path);
            string? headGitlink = GitReader.Gitlink(repositoryRoot, rootHead, submodule.Path);
            if (!string.Equals(baseGitlink, submoduleBase, StringComparison.OrdinalIgnoreCase)
                || (!root.IncludeWorkingTree
                    && !string.Equals(headGitlink, submoduleHead, StringComparison.OrdinalIgnoreCase)))
            {
                throw new GateValidationException(GateReason.GitlinkScopeMismatch, submodule.Path);
            }
        }
    }

    private static void ValidateCurrentGitlinks(
        IReadOnlyList<RepositoryScope> scopes,
        IReadOnlyList<ChangedPath> changes,
        IReadOnlyDictionary<string, (string Base, string Head)> revisions)
    {
        RepositoryScope root = scopes.Single(static scope => scope.Path == ".");
        HashSet<string> declaredSubmodules = scopes
            .Where(static scope => scope.Path != ".")
            .Select(static scope => scope.Path)
            .ToHashSet(StringComparer.Ordinal);
        if (changes.Any(change => change.Repository.Equals(root.Name, StringComparison.Ordinal)
            && change.Mode.Equals("160000", StringComparison.Ordinal)
            && !declaredSubmodules.Contains(change.Path)))
        {
            throw new GateValidationException(GateReason.GitlinkScopeMismatch, "undeclared-gitlink-scope");
        }

        foreach (RepositoryScope submodule in scopes.Where(static scope => scope.Path != "."))
        {
            ChangedPath? gitlink = changes.SingleOrDefault(change =>
                change.Repository.Equals(root.Name, StringComparison.Ordinal)
                && change.Path.Equals(submodule.Path, StringComparison.Ordinal));
            if (gitlink is null
                || !gitlink.Mode.Equals("160000", StringComparison.Ordinal)
                || !gitlink.ObjectId.Equals(revisions[submodule.Name].Head, StringComparison.OrdinalIgnoreCase))
            {
                throw new GateValidationException(GateReason.GitlinkScopeMismatch, submodule.Path);
            }
        }
    }

    private static string NormalizeRepositoryPath(string path)
    {
        string candidate = path.Replace('\\', '/');
        if (candidate.StartsWith("/", StringComparison.Ordinal)
            || (candidate.Length >= 3 && char.IsLetter(candidate[0]) && candidate[1] == ':' && candidate[2] == '/'))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "repository-path");
        }

        string normalized = candidate.Trim('/');
        if (normalized.Length == 0 || normalized.Equals(".", StringComparison.Ordinal))
        {
            return ".";
        }

        if (normalized.Split('/').Any(static segment => segment.Length == 0 || segment is "." or ".."))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "repository-path");
        }

        return normalized;
    }

    private static string NormalizeFilePath(string path)
    {
        string normalized = path.Replace('\\', '/');
        if (normalized.Length == 0
            || normalized.StartsWith("/", StringComparison.Ordinal)
            || (normalized.Length >= 3 && char.IsLetter(normalized[0]) && normalized[1] == ':' && normalized[2] == '/')
            || normalized.Equals(".", StringComparison.Ordinal)
            || normalized.Split('/').Any(static segment => segment.Length == 0 || segment is "." or ".."))
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, "path");
        }

        return normalized;
    }

    private static string BindRootRevision(string configured, string token, string exactRevision)
    {
        return configured.Equals(token, StringComparison.Ordinal) ? exactRevision : configured.ToLowerInvariant();
    }

    [GeneratedRegex("^\\s*path\\s*=\\s*(.+?)\\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex SubmodulePath();
}
