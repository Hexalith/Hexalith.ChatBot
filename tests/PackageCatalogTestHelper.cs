using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;

namespace Hexalith.ChatBot.Tests;

internal static class PackageCatalogTestHelper
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Snapshot = new(EvaluateCatalog, LazyThreadSafetyMode.PublicationOnly);

    public static string Version(string packageId)
        => Snapshot.Value.TryGetValue(packageId, out string? version)
            ? version
            : throw new InvalidOperationException($"The evaluated package catalog does not contain '{packageId}'.");

    public static void AssertExclusiveAuthority()
        => _ = Snapshot.Value;

    /// <summary>Requires a sibling wrapper to preserve every effective root catalog item and version.</summary>
    public static void AssertSiblingAuthority(string wrapperPath)
    {
        string authorityPath = AuthorityPath();
        Dictionary<string, string> globalProperties = new(StringComparer.Ordinal)
        {
            ["UseHexalithProjectReferences"] = "false",
            ["Hexalith1BuildPackageProps"] = authorityPath,
            ["HexalithBuildPackageVersions1"] = authorityPath,
            ["HexalithBuildsResolvedRoot"] = Path.GetDirectoryName(Path.GetDirectoryName(authorityPath))!,
        };
        using JsonDocument evaluation = EvaluateWrapper(wrapperPath, globalProperties);
        IReadOnlyDictionary<string, string> versions = ReadVersions(evaluation, authorityPath, wrapperPath);
        foreach ((string identity, string expectedVersion) in Snapshot.Value)
        {
            if (!versions.TryGetValue(identity, out string? actualVersion))
            {
                throw new InvalidOperationException($"The package wrapper '{wrapperPath}' omits catalog ID '{identity}'.");
            }

            if (!string.Equals(actualVersion, expectedVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The package wrapper '{wrapperPath}' changes '{identity}' from catalog version '{expectedVersion}' to '{actualVersion}'.");
            }
        }
    }

    public static void AssertUiFoundationPins()
    {
        (string PackageId, string ExpectedVersion)[] expected =
        [
            ("Microsoft.FluentUI.AspNetCore.Components", "5.0.1"),
            ("Microsoft.FluentUI.AspNetCore.Components.Icons", "5.0.1"),
            ("Fluxor", "6.11.0"),
            ("Microsoft.Playwright", "1.64.0"),
            ("xunit.v3", "4.0.2"),
            ("bunit", "2.12.5"),
        ];

        foreach ((string packageId, string expectedVersion) in expected)
        {
            string actualVersion = Version(packageId);
            if (!string.Equals(actualVersion, expectedVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The evaluated version for '{packageId}' is '{actualVersion}', expected '{expectedVersion}'.");
            }
        }
    }

    private static IReadOnlyDictionary<string, string> EvaluateCatalog()
    {
        StringComparison pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        string repositoryRoot = RepositoryRoot();
        string wrapperPath = Path.Combine(repositoryRoot, "Directory.Packages.props");
        string authorityPath = AuthorityPath();

        XDocument wrapper = XDocument.Load(wrapperPath);
        if (wrapper.Descendants("PackageVersion").Any())
        {
            throw new InvalidOperationException("The ChatBot package wrapper must not define PackageVersion items.");
        }

        XElement[] imports = wrapper.Descendants("Import").ToArray();
        if (imports.Length != 1)
        {
            throw new InvalidOperationException("The ChatBot package wrapper must contain exactly one catalog import.");
        }

        string importExpression = imports[0].Attribute("Project")?.Value
            ?? throw new InvalidOperationException("The ChatBot package wrapper import has no Project value.");
        string resolvedImport = Path.GetFullPath(
            importExpression.Replace("$(MSBuildThisFileDirectory)", repositoryRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal));
        if (!string.Equals(resolvedImport, authorityPath, pathComparison))
        {
            throw new InvalidOperationException(
                $"The ChatBot package wrapper imports '{resolvedImport}', expected '{authorityPath}'.");
        }

        using JsonDocument evaluation = EvaluateWrapper(wrapperPath);
        return ReadVersions(evaluation, authorityPath, wrapperPath);
    }

    /// <summary>Evaluates an actual package wrapper with a bounded MSBuild subprocess and optional global resolver properties.</summary>
    public static JsonDocument EvaluateWrapper(string wrapperPath, IReadOnlyDictionary<string, string>? globalProperties = null)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = RepositoryRoot(),
        };
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(wrapperPath);
        startInfo.ArgumentList.Add("-property:Configuration=Release");
        startInfo.ArgumentList.Add("-getProperty:ManagePackageVersionsCentrally");
        startInfo.ArgumentList.Add("-getProperty:CentralPackageVersionOverrideEnabled");
        startInfo.ArgumentList.Add("-getItem:PackageVersion");
        if (globalProperties is not null)
        {
            foreach ((string name, string value) in globalProperties)
            {
                startInfo.ArgumentList.Add($"-property:{name}={value}");
            }
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet msbuild to evaluate the package catalog.");
        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(120_000))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException("Package catalog evaluation timed out after 120 seconds.");
        }

        string standardOutput = standardOutputTask.GetAwaiter().GetResult();
        string standardError = standardErrorTask.GetAwaiter().GetResult();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Package wrapper '{wrapperPath}' evaluation failed with exit code {process.ExitCode}:{Environment.NewLine}{standardError}{standardOutput}");
        }

        return JsonDocument.Parse(standardOutput);
    }

    private static IReadOnlyDictionary<string, string> ReadVersions(JsonDocument evaluation, string authorityPath, string wrapperPath)
    {
        StringComparison pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        JsonElement properties = evaluation.RootElement.GetProperty("Properties");
        RequireProperty(properties, "ManagePackageVersionsCentrally", "true");
        RequireProperty(properties, "CentralPackageVersionOverrideEnabled", "false");

        Dictionary<string, string> versions = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement item in evaluation.RootElement.GetProperty("Items").GetProperty("PackageVersion").EnumerateArray())
        {
            string identity = item.GetProperty("Identity").GetString()
                ?? throw new InvalidOperationException("An evaluated PackageVersion item has no identity.");
            string version = item.GetProperty("Version").GetString()
                ?? throw new InvalidOperationException($"The evaluated PackageVersion '{identity}' has no version.");
            string definingProject = Path.GetFullPath(
                item.GetProperty("DefiningProjectFullPath").GetString()
                ?? throw new InvalidOperationException($"The evaluated PackageVersion '{identity}' has no defining project."));

            if (!versions.TryAdd(identity, version))
            {
                throw new InvalidOperationException($"The evaluated package wrapper '{wrapperPath}' contains duplicate ID '{identity}'.");
            }

            if (!string.Equals(definingProject, authorityPath, pathComparison))
            {
                throw new InvalidOperationException(
                    $"The evaluated PackageVersion '{identity}' in wrapper '{wrapperPath}' is defined by '{definingProject}', outside '{authorityPath}'.");
            }
        }

        return versions;
    }

    private static void RequireProperty(JsonElement properties, string name, string expectedValue)
    {
        string? actualValue = properties.GetProperty(name).GetString();
        if (!string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The evaluated MSBuild property '{name}' is '{actualValue}', expected '{expectedValue}'.");
        }
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the ChatBot repository root.");
    }

    private static string AuthorityPath()
        => Path.GetFullPath(Path.Combine(RepositoryRoot(), "references", "Hexalith.Builds", "Props", "Directory.Packages.props"));
}
