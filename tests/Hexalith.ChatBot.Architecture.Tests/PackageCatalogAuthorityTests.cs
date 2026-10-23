using System.Text.Json;
using System.Xml.Linq;

using Hexalith.ChatBot.Tests;

using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests;

/// <summary>Exercises evaluated catalog authority and standalone Timesheets fallback compatibility.</summary>
public static class PackageCatalogAuthorityTests
{
    /// <summary>Preserves an imported version and supplies the published fallback only for an absent exact identity.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public static void TimesheetsWrapperShouldPreserveImportedWorksContractOrSupplyStandaloneFallback(bool includeContract)
    {
        string temporaryRoot = Directory.CreateTempSubdirectory("hexalith-timesheets-catalog-").FullName;
        try
        {
            string catalogPath = Path.Combine(temporaryRoot, "catalog.props");
            XElement packages = new("ItemGroup",
                new XElement("PackageVersion",
                    new XAttribute("Include", "Hexalith.Works.Contracts.Tools"),
                    new XAttribute("Version", "9.9.9")));
            if (includeContract)
            {
                packages.Add(new XElement("PackageVersion",
                    new XAttribute("Include", "Hexalith.Works.Contracts"),
                    new XAttribute("Version", "9.9.9")));
            }

            new XDocument(new XElement("Project",
                new XElement("PropertyGroup", new XElement("HexalithVersionsLoaded", "true")),
                packages)).Save(catalogPath);

            string wrapperPath = Path.Combine(RepositoryRoot(), "references", "Hexalith.Timesheets", "Directory.Packages.props");
            using JsonDocument evaluation = PackageCatalogTestHelper.EvaluateWrapper(wrapperPath,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["UseHexalithProjectReferences"] = "false",
                    ["Hexalith1BuildPackageProps"] = catalogPath,
                });
            JsonElement[] contracts = evaluation.RootElement.GetProperty("Items").GetProperty("PackageVersion")
                .EnumerateArray()
                .Where(static item => item.GetProperty("Identity").GetString() == "Hexalith.Works.Contracts")
                .ToArray();

            contracts.Length.ShouldBe(1);
            contracts[0].GetProperty("Version").GetString().ShouldBe(includeContract ? "9.9.9" : "1.0.2");
            contracts[0].GetProperty("DefiningProjectFullPath").GetString()
                .ShouldBe(includeContract ? catalogPath : wrapperPath);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    /// <summary>Accepts inert compatibility items while rejecting effective catalog mutations.</summary>
    [Theory]
    [InlineData("inactive-fallback", null)]
    [InlineData("duplicate", "duplicate ID 'Hexalith.Works.Contracts'")]
    [InlineData("update", "changes 'Hexalith.Works.Contracts'")]
    [InlineData("release-update", "changes 'Hexalith.Works.Contracts'")]
    [InlineData("remove", "omits catalog ID 'Hexalith.Works.Contracts'")]
    [InlineData("foreign", "outside")]
    public static void SiblingAuthorityShouldDistinguishInactiveFallbackFromEffectiveCatalogMutation(string mutation, string? expectedFailure)
    {
        string temporaryRoot = Directory.CreateTempSubdirectory("hexalith-sibling-catalog-").FullName;
        try
        {
            XElement item = mutation switch
            {
                "inactive-fallback" => new XElement("PackageVersion",
                    new XAttribute("Include", "Hexalith.Works.Contracts"),
                    new XAttribute("Version", "9.9.9"),
                    new XAttribute("Condition", "'@(PackageVersion->WithMetadataValue('Identity', 'Hexalith.Works.Contracts'))' == ''")),
                "duplicate" => new XElement("PackageVersion",
                    new XAttribute("Include", "Hexalith.Works.Contracts"),
                    new XAttribute("Version", "1.0.2")),
                "update" => new XElement("PackageVersion",
                    new XAttribute("Update", "Hexalith.Works.Contracts"),
                    new XAttribute("Version", "9.9.9")),
                "release-update" => new XElement("PackageVersion",
                    new XAttribute("Update", "Hexalith.Works.Contracts"),
                    new XAttribute("Version", "9.9.9"),
                    new XAttribute("Condition", "'$(Configuration)' == 'Release'")),
                "remove" => new XElement("PackageVersion", new XAttribute("Remove", "Hexalith.Works.Contracts")),
                "foreign" => new XElement("PackageVersion",
                    new XAttribute("Include", "Hexalith.ChatBot.Foreign"),
                    new XAttribute("Version", "9.9.9")),
                _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
            };
            string wrapperPath = Path.Combine(temporaryRoot, "Directory.Packages.props");
            new XDocument(new XElement("Project",
                new XElement("Import", new XAttribute("Project", "$(Hexalith1BuildPackageProps)")),
                new XElement("ItemGroup", item))).Save(wrapperPath);

            if (expectedFailure is null)
            {
                PackageCatalogTestHelper.AssertSiblingAuthority(wrapperPath);
            }
            else
            {
                string message = Should.Throw<InvalidOperationException>(() => PackageCatalogTestHelper.AssertSiblingAuthority(wrapperPath)).Message;
                message.ShouldContain(expectedFailure);
                if (mutation is "duplicate" or "foreign")
                {
                    message.ShouldContain(wrapperPath);
                }
            }
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the ChatBot repository root.");
    }
}
