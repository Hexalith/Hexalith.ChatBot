using System.Text.RegularExpressions;
using System.Xml.Linq;

using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests;

public static class ScaffoldArchitectureTests
{
    [Fact]
    public static void SolutionShouldContainRequiredSourceAndTestProjects()
    {
        XDocument solution = XDocument.Load(Path.Combine(RepositoryRoot(), "Hexalith.ChatBot.slnx"));
        HashSet<string> projects = solution
            .Descendants("Project")
            .Select(static element => element.Attribute("Path")?.Value)
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Select(static path => path!)
            .ToHashSet(StringComparer.Ordinal);

        string[] expected =
        [
            "src/Hexalith.ChatBot.Contracts/Hexalith.ChatBot.Contracts.csproj",
            "src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj",
            "src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj",
            "src/Hexalith.ChatBot.Aspire/Hexalith.ChatBot.Aspire.csproj",
            "src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj",
            "src/Hexalith.ChatBot.ServiceDefaults/Hexalith.ChatBot.ServiceDefaults.csproj",
            "src/Hexalith.ChatBot.Testing/Hexalith.ChatBot.Testing.csproj",
            "tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj",
            "tests/Hexalith.ChatBot.Client.Tests/Hexalith.ChatBot.Client.Tests.csproj",
            "tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj",
            "tests/Hexalith.ChatBot.ServiceDefaults.Tests/Hexalith.ChatBot.ServiceDefaults.Tests.csproj",
            "tests/Hexalith.ChatBot.Aspire.Tests/Hexalith.ChatBot.Aspire.Tests.csproj",
            "tests/Hexalith.ChatBot.AppHost.Tests/Hexalith.ChatBot.AppHost.Tests.csproj",
            "tests/Hexalith.ChatBot.Testing.Tests/Hexalith.ChatBot.Testing.Tests.csproj",
            "tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj",
            "tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj",
            "tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj",
        ];

        foreach (string project in expected)
        {
            projects.ShouldContain(project);
        }
    }

    [Fact]
    public static void ProjectReferencesShouldFollowContractsClientServerDirection()
    {
        ProjectReferences("src/Hexalith.ChatBot.Contracts/Hexalith.ChatBot.Contracts.csproj").ShouldBeEmpty();
        ProjectReferences("src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj")
            .ShouldBe(["..\\Hexalith.ChatBot.Contracts\\Hexalith.ChatBot.Contracts.csproj"], ignoreOrder: true);

        string[] serverReferences = ProjectReferences("src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj");
        serverReferences.ShouldContain("..\\Hexalith.ChatBot.Client\\Hexalith.ChatBot.Client.csproj");
        serverReferences.ShouldContain("$(HexalithEventStoreRoot)\\src\\Hexalith.EventStore.Contracts\\Hexalith.EventStore.Contracts.csproj");
        serverReferences.ShouldContain("$(HexalithTenantsRoot)\\src\\Hexalith.Tenants.Contracts\\Hexalith.Tenants.Contracts.csproj");
        serverReferences.ShouldNotContain(reference => reference.Contains("Hexalith.ChatBot.AppHost", StringComparison.Ordinal));
        serverReferences.ShouldNotContain(reference => reference.Contains("Hexalith.ChatBot.Aspire", StringComparison.Ordinal));

        string appHostSource = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));
        appHostSource.ShouldContain("Hexalith.EventStore");
        appHostSource.ShouldContain("Hexalith.Tenants");
        appHostSource.ShouldContain("RootProjectPath");
    }

    [Fact]
    public static void FutureSurfaceAdaptersMustNotReferenceServerInternals()
    {
        string root = RepositoryRoot();
        string[] adapterProjects = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(static path => path.Contains(".UI", StringComparison.Ordinal)
                || path.Contains(".Cli", StringComparison.Ordinal)
                || path.Contains(".Mcp", StringComparison.Ordinal)
                || path.Contains(".Workers", StringComparison.Ordinal))
            .ToArray();

        foreach (string project in adapterProjects)
        {
            ProjectReferencesFromPath(project).ShouldNotContain(reference => reference.Contains("Hexalith.ChatBot.Server", StringComparison.Ordinal));
        }
    }

    [Fact]
    public static void ProjectFilesShouldNotUseInlinePackageVersions()
    {
        string root = RepositoryRoot();
        string[] projectFiles = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "tests"), "*.csproj", SearchOption.AllDirectories))
            .ToArray();

        List<string> inlineVersions = [];
        foreach (string projectFile in projectFiles)
        {
            XDocument project = XDocument.Load(projectFile);
            IEnumerable<string> violations = project
                .Descendants("PackageReference")
                .Where(static element => element.Attribute("Version") is not null || element.Element("Version") is not null)
                .Select(element => Path.GetRelativePath(root, projectFile) + ":" + element.Attribute("Include")?.Value);
            inlineVersions.AddRange(violations);
        }

        inlineVersions.ShouldBeEmpty();
    }

    [Fact]
    public static void RootConfigurationShouldPinSdkTargetFrameworkAndCentralPackages()
    {
        string root = RepositoryRoot();

        File.ReadAllText(Path.Combine(root, "global.json")).ShouldContain("\"version\": \"10.0.300\"");
        File.ReadAllText(Path.Combine(root, "global.json")).ShouldContain("\"rollForward\": \"latestPatch\"");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<TargetFramework>net10.0</TargetFramework>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<Nullable>enable</Nullable>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<ImplicitUsings>enable</ImplicitUsings>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<Deterministic>true</Deterministic>");
        File.ReadAllText(Path.Combine(root, "Directory.Packages.props")).ShouldContain("<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>");
    }

    [Fact]
    public static void RootSubmoduleDeclarationsShouldRemainRootLevelAndUnique()
    {
        string modules = File.ReadAllText(Path.Combine(RepositoryRoot(), ".gitmodules"));

        Regex.Matches(modules, "path = Hexalith.EventStore").Count.ShouldBe(1);
        modules.ShouldContain("path = Hexalith.Tenants");
        modules.ShouldContain("path = Hexalith.FrontComposer");
        modules.ShouldNotContain("Hexalith.EventStore/Hexalith.EventStore");
    }

    [Fact]
    public static void CiShouldInitializeOnlyRootSubmodulesNonRecursively()
    {
        string workflow = File.ReadAllText(Path.Combine(RepositoryRoot(), ".github", "workflows", "ci.yml"));

        workflow.ShouldContain("submodules: false");
        workflow.ShouldContain("git submodule update --init");
        workflow.ShouldNotContain("--recursive");
    }

    [Fact]
    public static void WorkflowsAndToolsShouldNotUseRecursiveSubmoduleCommands()
    {
        string root = RepositoryRoot();
        IEnumerable<string> files = Directory.EnumerateFiles(Path.Combine(root, ".github", "workflows"), "*", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "tests", "tools"), "*", SearchOption.AllDirectories));

        Regex forbidden = new(@"git\s+submodule\s+(?:update|foreach)\b[^\r\n]*--recursive", RegexOptions.IgnoreCase);
        string[] violations = files
            .Where(static file => !file.EndsWith(".gitkeep", StringComparison.Ordinal))
            .Where(file => forbidden.IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        violations.ShouldBeEmpty();
    }

    private static string[] ProjectReferences(string relativeProjectPath)
    {
        return ProjectReferencesFromPath(Path.Combine(RepositoryRoot(), relativeProjectPath));
    }

    private static string[] ProjectReferencesFromPath(string projectPath)
    {
        XDocument project = XDocument.Load(projectPath);
        return project
            .Descendants("ProjectReference")
            .Select(static reference => reference.Attribute("Include")?.Value)
            .Where(static include => !string.IsNullOrWhiteSpace(include))
            .Select(static include => include!)
            .ToArray();
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
