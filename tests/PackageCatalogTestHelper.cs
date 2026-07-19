using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;

namespace Hexalith.ChatBot.Tests;

internal static class PackageCatalogTestHelper
{
    private static readonly Lazy<CatalogSnapshot> Snapshot = new(EvaluateCatalog, LazyThreadSafetyMode.PublicationOnly);

    public static string Version(string packageId)
        => Snapshot.Value.Versions.TryGetValue(packageId, out string? version)
            ? version
            : throw new InvalidOperationException($"The evaluated package catalog does not contain '{packageId}'.");

    public static void AssertExclusiveAuthority()
        => _ = Snapshot.Value;

    public static void AssertUiFoundationPins()
    {
        (string PackageId, string ExpectedVersion)[] expected =
        [
            ("Microsoft.FluentUI.AspNetCore.Components", "5.0.0-rc.4-26180.1"),
            ("Fluxor", "6.10.0"),
            ("Microsoft.Playwright", "1.61.0"),
            ("xunit.v3", "3.2.2"),
            ("bunit", "2.8.4-preview"),
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

    private static CatalogSnapshot EvaluateCatalog()
    {
        StringComparison pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        string repositoryRoot = RepositoryRoot();
        string wrapperPath = Path.Combine(repositoryRoot, "Directory.Packages.props");
        string authorityPath = Path.GetFullPath(
            Path.Combine(repositoryRoot, "references", "Hexalith.Builds", "Props", "Directory.Packages.props"));

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

        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = repositoryRoot,
        };
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(wrapperPath);
        startInfo.ArgumentList.Add("-getProperty:ManagePackageVersionsCentrally");
        startInfo.ArgumentList.Add("-getProperty:CentralPackageVersionOverrideEnabled");
        startInfo.ArgumentList.Add("-getItem:PackageVersion");

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
                $"Package catalog evaluation failed with exit code {process.ExitCode}:{Environment.NewLine}{standardError}");
        }

        using JsonDocument evaluation = JsonDocument.Parse(standardOutput);
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

            if (!string.Equals(definingProject, authorityPath, pathComparison))
            {
                throw new InvalidOperationException(
                    $"The evaluated PackageVersion '{identity}' is defined by '{definingProject}', outside '{authorityPath}'.");
            }

            if (!versions.TryAdd(identity, version))
            {
                throw new InvalidOperationException($"The evaluated package catalog contains duplicate ID '{identity}'.");
            }
        }

        return new CatalogSnapshot(versions);
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

    private sealed record CatalogSnapshot(IReadOnlyDictionary<string, string> Versions);
}
