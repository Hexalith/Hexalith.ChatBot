using System.Text.RegularExpressions;
using System.Text.Json;
using System.Xml.Linq;

using Hexalith.ChatBot.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            "src/Hexalith.ChatBot.Cli/Hexalith.ChatBot.Cli.csproj",
            "src/Hexalith.ChatBot.Mcp/Hexalith.ChatBot.Mcp.csproj",
            "src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj",
            "src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj",
            "src/Hexalith.ChatBot.Testing/Hexalith.ChatBot.Testing.csproj",
            "src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj",
            "src/Hexalith.ChatBot.Workers/Hexalith.ChatBot.Workers.csproj",
            "tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj",
            "tests/Hexalith.ChatBot.Client.Tests/Hexalith.ChatBot.Client.Tests.csproj",
            "tests/Hexalith.ChatBot.Cli.Tests/Hexalith.ChatBot.Cli.Tests.csproj",
            "tests/Hexalith.ChatBot.Mcp.Tests/Hexalith.ChatBot.Mcp.Tests.csproj",
            "tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj",
            "tests/Hexalith.ChatBot.AppHost.Tests/Hexalith.ChatBot.AppHost.Tests.csproj",
            "tests/Hexalith.ChatBot.Testing.Tests/Hexalith.ChatBot.Testing.Tests.csproj",
            "tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj",
            "tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj",
            "tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj",
            "tests/Hexalith.ChatBot.Live.IntegrationTests/Hexalith.ChatBot.Live.IntegrationTests.csproj",
            "tests/Hexalith.ChatBot.RecoverySandbox/Hexalith.ChatBot.RecoverySandbox.csproj",
            "tests/Hexalith.ChatBot.Recovery.Tests/Hexalith.ChatBot.Recovery.Tests.csproj",
            "tests/Hexalith.ChatBot.Release.Tests/Hexalith.ChatBot.Release.Tests.csproj",
            "tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj",
            "tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj",
            "tests/Hexalith.ChatBot.Workers.Tests/Hexalith.ChatBot.Workers.Tests.csproj",
        ];

        foreach (string project in expected)
        {
            projects.ShouldContain(project);
        }

        projects.ShouldNotContain("src/Hexalith.ChatBot.Aspire/Hexalith.ChatBot.Aspire.csproj");
        projects.ShouldNotContain("src/Hexalith.ChatBot.ServiceDefaults/Hexalith.ChatBot.ServiceDefaults.csproj");
        projects.ShouldNotContain("tests/Hexalith.ChatBot.Aspire.Tests/Hexalith.ChatBot.Aspire.Tests.csproj");
        projects.ShouldNotContain("tests/Hexalith.ChatBot.ServiceDefaults.Tests/Hexalith.ChatBot.ServiceDefaults.Tests.csproj");

        projects.Where(static path => path.StartsWith("src/", StringComparison.Ordinal)).ShouldBe(
            expected.Where(static path => path.StartsWith("src/", StringComparison.Ordinal)),
            ignoreOrder: true,
            customMessage: "the canonical module contains exactly nine source projects");
        projects.Where(static path => path.StartsWith("tests/", StringComparison.Ordinal)).ShouldBe(
            expected.Where(static path => path.StartsWith("tests/", StringComparison.Ordinal)),
            ignoreOrder: true,
            customMessage: "every independent test lane must remain in the solution inventory");
        projects.ShouldNotContain(static path => path.StartsWith("references/", StringComparison.Ordinal),
            "ordinary sibling libraries must be restored as packages; AppHost composition references stay in its project");
    }

    [Fact]
    public static void EveryNonGeneratedProductionSourceShouldContainExactlyOneNamedTopLevelDeclaration()
    {
        string root = RepositoryRoot();
        string sourceRoot = Path.Combine(root, "src");
        string[] sourceFiles = Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            .Where(static path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        sourceFiles.Length.ShouldBeGreaterThan(
            1_000,
            "the one-type guard must prove it scanned the real production tree rather than pass over an empty path");

        List<string> violations = [];
        foreach (string sourceFile in sourceFiles)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                File.ReadAllText(sourceFile),
                new CSharpParseOptions(LanguageVersion.CSharp14),
                sourceFile,
                cancellationToken: TestContext.Current.CancellationToken);
            Diagnostic[] parseErrors = tree.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .ToArray();
            if (parseErrors.Length > 0)
            {
                violations.Add(
                    $"{Path.GetRelativePath(root, sourceFile)}: parse errors: "
                    + string.Join(" | ", parseErrors.Select(static diagnostic => diagnostic.ToString())));
                continue;
            }

            CompilationUnitSyntax compilationUnit = tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken);
            SyntaxList<MemberDeclarationSyntax> members = compilationUnit.Members.Count == 1
                && compilationUnit.Members[0] is BaseNamespaceDeclarationSyntax namespaceDeclaration
                    ? namespaceDeclaration.Members
                    : compilationUnit.Members;
            MemberDeclarationSyntax[] declarations = members
                .Where(static member => member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
                .ToArray();
            bool hasTopLevelStatements = compilationUnit.Members.OfType<GlobalStatementSyntax>().Any();
            bool hasExplicitTopLevelProgramDeclaration = declarations.Length == 1
                && declarations[0] is TypeDeclarationSyntax typeDeclaration
                && typeDeclaration.Identifier.ValueText == "Program"
                && typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);
            bool isTopLevelProgram = hasTopLevelStatements
                && (declarations.Length == 0 || hasExplicitTopLevelProgramDeclaration);
            int logicalObjectCount = isTopLevelProgram
                ? 1
                : declarations.Length + (hasTopLevelStatements ? 1 : 0);

            if (logicalObjectCount != 1)
            {
                violations.Add(
                    $"{Path.GetRelativePath(root, sourceFile)}: expected exactly one top-level declaration/object, "
                    + $"found {logicalObjectCount}");
                continue;
            }

            if (isTopLevelProgram)
            {
                Path.GetFileName(sourceFile).ShouldBe("Program.cs");
                continue;
            }

            string declarationName = declarations[0] switch
            {
                BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
                DelegateDeclarationSyntax type => type.Identifier.ValueText,
                _ => throw new InvalidOperationException("Unsupported top-level declaration."),
            };
            string fileOwnerName = Path.GetFileName(sourceFile).Split('.', 2)[0];
            if (!string.Equals(fileOwnerName, declarationName, StringComparison.Ordinal))
            {
                violations.Add(
                    $"{Path.GetRelativePath(root, sourceFile)}: declaration '{declarationName}' must live in its named file");
            }
        }

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void DomainModuleShouldNotRegrowReusableHostingProjects()
    {
        string root = RepositoryRoot();
        string[] forbiddenProjects = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(static path => path.Contains("Hexalith.ChatBot.Aspire", StringComparison.Ordinal)
                || path.Contains("Hexalith.ChatBot.ServiceDefaults", StringComparison.Ordinal))
            .ToArray();

        forbiddenProjects.ShouldBeEmpty();

        string solution = File.ReadAllText(Path.Combine(root, "Hexalith.ChatBot.slnx"));
        solution.ShouldNotContain("Hexalith.ChatBot.Aspire");
        solution.ShouldNotContain("Hexalith.ChatBot.ServiceDefaults");
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
        serverReferences.ShouldNotContain(reference => reference.Contains("Hexalith.ChatBot.ServiceDefaults", StringComparison.Ordinal));

        // The AppHost wires the EventStore + Tenants submodule projects as TYPED Aspire project resources
        // (Projects.Hexalith_EventStore / Projects.Hexalith_Tenants). The typed form is required: the generated
        // project metadata keeps each dapr sidecar's auto-detected app-port aligned with the app's Kestrel
        // listener under the Aspire testing builder (the path-based AddProject overload diverges). The project
        // resources are pulled in via AppHost ProjectReferences.
        string[] appHostReferences = ProjectReferences("src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj");
        appHostReferences.ShouldContain("$(HexalithEventStoreRoot)\\src\\Hexalith.EventStore\\Hexalith.EventStore.csproj");
        appHostReferences.ShouldContain("$(HexalithTenantsRoot)\\src\\Hexalith.Tenants\\Hexalith.Tenants.csproj");

        string appHostSource = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));
        appHostSource.ShouldContain("Hexalith_EventStore");
        appHostSource.ShouldContain("Hexalith_Tenants");

        ProjectReferences("src/Hexalith.ChatBot.Workers/Hexalith.ChatBot.Workers.csproj")
            .ShouldBe(["..\\Hexalith.ChatBot.Client\\Hexalith.ChatBot.Client.csproj"]);
    }

    [Fact]
    public static void ReleaseDependencyGraphShouldUsePackagesOutsideAppHostCompositionResources()
    {
        const string SourceModeCondition = "'$(UseHexalithProjectReferences)' == 'true'";
        const string PackageModeCondition = "'$(UseHexalithProjectReferences)' != 'true'";
        string root = RepositoryRoot();
        string appHostProject = Path.Combine(
            root,
            "src",
            "Hexalith.ChatBot.AppHost",
            "Hexalith.ChatBot.AppHost.csproj");
        string[] projectFiles = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "tests"), "*.csproj", SearchOption.AllDirectories))
            .Where(project => !string.Equals(project, appHostProject, StringComparison.Ordinal))
            .ToArray();

        List<string> violations = [];
        int externalSourceReferenceCount = 0;
        int packageCounterpartCount = 0;
        int sourceOnlyOwnerFixtureCount = 0;
        foreach (string projectFile in projectFiles)
        {
            XDocument project = XDocument.Load(projectFile);
            XElement[] externalProjectReferences = project
                .Descendants("ProjectReference")
                .Where(static reference =>
                    reference.Attribute("Include")?.Value.StartsWith("$(Hexalith", StringComparison.Ordinal) == true)
                .ToArray();
            externalSourceReferenceCount += externalProjectReferences.Length;

            foreach (XElement projectReference in externalProjectReferences)
            {
                string include = projectReference.Attribute("Include")!.Value;
                string projectReferenceCondition = DependencyCondition(projectReference) ?? "<unconditional>";
                if (!string.Equals(projectReferenceCondition, SourceModeCondition, StringComparison.Ordinal))
                {
                    violations.Add(
                        $"{Path.GetRelativePath(root, projectFile)}:{include} must be source-mode-only; "
                        + $"found {projectReferenceCondition}");
                }

                // This aliased executable hosts the real owner fixture, which is deliberately absent in package mode.
                // Client/Contracts remain subject to the ordinary package-counterpart checks below.
                if (string.Equals(
                        Path.GetRelativePath(root, projectFile),
                        Path.Combine("tests", "Hexalith.ChatBot.Server.Tests", "Hexalith.ChatBot.Server.Tests.csproj"),
                        StringComparison.Ordinal)
                    && string.Equals(
                        include,
                        @"$(HexalithFoldersRoot)\src\Hexalith.Folders.Server\Hexalith.Folders.Server.csproj",
                        StringComparison.Ordinal))
                {
                    (projectReference.Attribute("Aliases")?.Value).ShouldBe("FoldersOwner");
                    XElement fixtureExclusion = project.Descendants("Compile").Single(element =>
                        element.Attribute("Remove")?.Value == "Adapters/Folders/FoldersNativeAttachmentTests.cs");
                    DependencyCondition(fixtureExclusion).ShouldBe(PackageModeCondition);
                    sourceOnlyOwnerFixtureCount++;
                    continue;
                }

                string packageId = include
                    .Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
                    .Last()
                    .Replace(".csproj", string.Empty, StringComparison.Ordinal);
                XElement? packageReference = project
                    .Descendants("PackageReference")
                    // A source-mode ExcludeAssets="all" entry only suppresses transitive package assets.
                    // It cannot supply the usable package dependency required by the Release graph.
                    .Where(static reference => !string.Equals(
                        (reference.Attribute("ExcludeAssets")?.Value ?? reference.Element("ExcludeAssets")?.Value)?.Trim(),
                        "all",
                        StringComparison.OrdinalIgnoreCase))
                    .SingleOrDefault(reference =>
                        string.Equals(reference.Attribute("Include")?.Value, packageId, StringComparison.Ordinal));
                if (packageReference is null)
                {
                    violations.Add(
                        $"{Path.GetRelativePath(root, projectFile)}:{include} has no {packageId} package-mode counterpart");
                    continue;
                }

                packageCounterpartCount++;
                string packageReferenceCondition = DependencyCondition(packageReference) ?? "<unconditional>";
                if (!string.Equals(packageReferenceCondition, PackageModeCondition, StringComparison.Ordinal))
                {
                    violations.Add(
                        $"{Path.GetRelativePath(root, projectFile)}:{packageId} must be package-mode-only; "
                        + $"found {packageReferenceCondition}");
                }
            }
        }

        externalSourceReferenceCount.ShouldBeGreaterThan(
            10,
            "the package-mode guard must inspect the real cross-repository dependency graph");
        sourceOnlyOwnerFixtureCount.ShouldBe(1, "only the source-only aliased Folders owner fixture is exempt");
        packageCounterpartCount.ShouldBe(
            externalSourceReferenceCount - sourceOnlyOwnerFixtureCount,
            "every external library source dependency must have an exact package-mode counterpart");
        violations.ShouldBeEmpty();

        XDocument appHost = XDocument.Load(appHostProject);
        XElement[] localCompositionResources = appHost
            .Descendants("ProjectReference")
            .Where(static reference =>
                reference.Attribute("Include")?.Value.StartsWith("$(Hexalith", StringComparison.Ordinal) == true)
            .ToArray();
        localCompositionResources.Length.ShouldBeGreaterThan(
            5,
            "the sole exception must remain the real typed local-composition topology, not an empty AppHost");
        const string CatalogHelperProject = "$(HexalithEventStoreRoot)\\src\\Hexalith.EventStore.Aspire\\Hexalith.EventStore.Aspire.csproj";
        XElement catalogHelper = localCompositionResources.Single(reference =>
            string.Equals(reference.Attribute("Include")?.Value, CatalogHelperProject, StringComparison.Ordinal));
        (catalogHelper.Attribute("IsAspireProjectResource")?.Value).ShouldBe("false");
        DependencyCondition(catalogHelper).ShouldBe(SourceModeCondition);
        XElement catalogHelperPackage = appHost.Descendants("PackageReference").Single(reference =>
            string.Equals(reference.Attribute("Include")?.Value, "Hexalith.EventStore.Aspire", StringComparison.Ordinal));
        DependencyCondition(catalogHelperPackage).ShouldBe(PackageModeCondition);

        localCompositionResources.Where(reference => !ReferenceEquals(reference, catalogHelper)).ShouldAllBe(
            static reference => DependencyCondition(reference) == null,
            "AppHost executable references are unconditional local composition resources, not library dependency authority");
    }

    [Fact]
    public static void ChatBotUiAdapterMustDependOnlyOnClientFacadeAndNeverServerInternals()
    {
        const string UiProject = "src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj";
        string[] references = ProjectReferences(UiProject);

        // The UI is a surface adapter: it depends only on the typed Client facade and the framework-owned
        // FrontComposer Shell composition layer.
        references.ShouldBe(
            [
                "..\\Hexalith.ChatBot.Client\\Hexalith.ChatBot.Client.csproj",
                "$(HexalithFrontComposerRoot)\\src\\Hexalith.FrontComposer.Shell\\Hexalith.FrontComposer.Shell.csproj",
            ],
            ignoreOrder: true);
        references.ShouldNotContain(reference => reference.Contains("Hexalith.ChatBot.Server", StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains("Dapr", StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains("Gateway", StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains("Audit", StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains("Idempotency", StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains("ProjectionStore", StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains("Hexalith.ChatBot.ServiceDefaults", StringComparison.Ordinal));

        XDocument uiProject = XDocument.Load(Path.Combine(RepositoryRoot(), UiProject));
        string[] packageReferences = uiProject
            .Descendants("PackageReference")
            .Select(static reference => reference.Attribute("Include")?.Value)
            .Where(static include => !string.IsNullOrWhiteSpace(include))
            .Select(static include => include!)
            .ToArray();
        packageReferences.ShouldNotBeEmpty("the package-reference boundary scan must be non-vacuous");

        string[] forbiddenDependencyTokens =
        [
            "Dapr",
            "EventStore",
            "Gateway",
            "Audit",
            "Idempotency",
            "ProjectionStore",
            "Hexalith.ChatBot.Server",
            "Hexalith.ChatBot.ServiceDefaults",
        ];
        string[] forbiddenPackages = packageReferences
            .Where(package => forbiddenDependencyTokens.Any(
                token => package.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        forbiddenPackages.ShouldBeEmpty(
            "the UI adapter must not bypass its Client/FrontComposer project boundary through a package dependency");

        // It submits ONLY through IChatBotClient — never the gateway stages, audit/idempotency seams,
        // the dispatcher, or the aggregate/processor (those live only in .Server).
        string[] forbidden =
        [
            "IRiskClassifier",
            "IChatBotAdmissionEvidenceValidator",
            "IChatBotAdmissionEvidenceSource",
            "SenderAuthorityClassifier",
            "Server.Governance.Outbound",
            "IApprovalGate",
            "IAuditWriter",
            "IIdempotencyStore",
            "AuditEnvelope",
            "ICommandDispatcher",
            "DispatchAsync",
            "GovernedOperationAggregate",
        ];

        string root = RepositoryRoot();
        string[] violations = Directory
            .EnumerateFiles(Path.Combine(root, "src", "Hexalith.ChatBot.UI"), "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(file => forbidden.Any(token => File.ReadAllText(file).Contains(token, StringComparison.Ordinal)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void RemovedHostingProjectsMustNotBeReferencedByProjectsOrAspireConfig()
    {
        string root = RepositoryRoot();
        string[] forbidden =
        [
            "Hexalith.ChatBot.ServiceDefaults",
            "Hexalith.ChatBot.Aspire",
        ];

        string[] projectReferenceViolations = Directory
            .EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(static path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(path => forbidden.Any(token => File.ReadAllText(path).Contains(token, StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        projectReferenceViolations.ShouldBeEmpty();

        string aspireConfig = File.ReadAllText(Path.Combine(root, "aspire.config.json"));
        aspireConfig.ShouldContain("src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj");
        aspireConfig.ShouldNotContain("Hexalith.ChatBot.Aspire");
        aspireConfig.ShouldNotContain("Hexalith.ChatBot.ServiceDefaults");
    }

    [Fact]
    public static void ChatBotCliAdapterMustDependOnlyOnClientFacadeAndNeverServerOrDataPlaneInternals()
    {
        string[] references = ProjectReferences("src/Hexalith.ChatBot.Cli/Hexalith.ChatBot.Cli.csproj");

        references.ShouldBe(
            [
                "..\\Hexalith.ChatBot.Client\\Hexalith.ChatBot.Client.csproj",
            ],
            ignoreOrder: true);
        references.ShouldNotContain(reference => reference.Contains("Hexalith.ChatBot.Server", StringComparison.Ordinal));

        XDocument project = XDocument.Load(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.Cli", "Hexalith.ChatBot.Cli.csproj"));
        project.Descendants("PackageReference")
            .Select(static element => element.Attribute("Include")?.Value)
            .ShouldBe(["System.CommandLine"], ignoreOrder: true);

        string[] forbidden =
        [
            "Hexalith.ChatBot.Server",
            "Gateway.Stages",
            "Server.Governance.Outbound",
            "DaprClient",
            "EventStore.Contracts",
            "AuditEnvelope",
            "IRiskClassifier",
            "IApprovalGate",
            "IAuditWriter",
            "IIdempotencyStore",
            "Hexalith.Projects.Client",
            "Hexalith.Folders.Client",
            "Hexalith.Conversations.Client",
            "ProjectionStore",
            "IProjectionStore",
        ];

        string root = RepositoryRoot();
        string[] violations = Directory
            .EnumerateFiles(Path.Combine(root, "src", "Hexalith.ChatBot.Cli"), "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(file => forbidden.Any(token => File.ReadAllText(file).Contains(token, StringComparison.Ordinal)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void ChatBotMcpAdapterMustDependOnlyOnClientFacadeAndNeverServerOrDataPlaneInternals()
    {
        string[] references = ProjectReferences("src/Hexalith.ChatBot.Mcp/Hexalith.ChatBot.Mcp.csproj");

        references.ShouldBe(
            [
                "..\\Hexalith.ChatBot.Client\\Hexalith.ChatBot.Client.csproj",
            ],
            ignoreOrder: true);
        references.ShouldNotContain(reference => reference.Contains("Hexalith.ChatBot.Server", StringComparison.Ordinal));

        XDocument project = XDocument.Load(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.Mcp", "Hexalith.ChatBot.Mcp.csproj"));
        project.Descendants("PackageReference")
            .Select(static element => element.Attribute("Include")?.Value)
            .ShouldBe(["Microsoft.Extensions.Hosting", "ModelContextProtocol"], ignoreOrder: true);

        PackageCatalogTestHelper.Version("ModelContextProtocol").ShouldBe("2.4.0");

        string[] forbidden =
        [
            "Hexalith.ChatBot.Server",
            "Gateway.Stages",
            "Server.Governance.Outbound",
            "DaprClient",
            "EventStore.Contracts",
            "AuditEnvelope",
            "IRiskClassifier",
            "IApprovalGate",
            "IAuditWriter",
            "IIdempotencyStore",
            "Hexalith.Projects.Client",
            "Hexalith.Folders.Client",
            "Hexalith.Conversations.Client",
            "ProjectionStore",
            "IProjectionStore",
            "/api/v1/commands",
        ];

        string root = RepositoryRoot();
        string[] violations = Directory
            .EnumerateFiles(Path.Combine(root, "src", "Hexalith.ChatBot.Mcp"), "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(file => forbidden.Any(token => File.ReadAllText(file).Contains(token, StringComparison.Ordinal)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        violations.ShouldBeEmpty();
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
    public static void GatewayStageSeamsShouldRemainInternalToServer()
    {
        string root = RepositoryRoot();
        string[] stageInterfaces =
        [
            "IRiskClassifier",
            "IApprovalGate",
            "IAuditWriter",
            "IAuditReplayIntentQueue",
            "IOperatorAlertSink",
            "ISystemClock",
            "IIdempotencyStore",
            "IOperationStatusStore",
        ];

        string[] serverSources = Directory
            .EnumerateFiles(Path.Combine(root, "src", "Hexalith.ChatBot.Server"), "*.cs", SearchOption.AllDirectories)
            .ToArray();

        foreach (string interfaceName in stageInterfaces)
        {
            string declaration = serverSources
                .Select(File.ReadAllText)
                .Single(source => source.Contains($"interface {interfaceName}", StringComparison.Ordinal));

            declaration.ShouldContain($"internal interface {interfaceName}");
        }

        IEnumerable<string> publicSurfaceFiles = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.Contains("Hexalith.ChatBot.Server", StringComparison.Ordinal));

        string[] leakedReferences = publicSurfaceFiles
            .Where(file => stageInterfaces.Any(interfaceName => File.ReadAllText(file).Contains(interfaceName, StringComparison.Ordinal)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        leakedReferences.ShouldBeEmpty();
    }

    [Fact]
    public static void DurableStateWritesShouldDispatchOnlyThroughCommandGateway()
    {
        string root = RepositoryRoot();
        Regex directDispatchCall = new(@"\.\s*DispatchAsync\s*\(", RegexOptions.CultureInvariant);
        string[] violations = Directory
            .EnumerateFiles(Path.Combine(root, "src", "Hexalith.ChatBot.Server"), "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.EndsWith(Path.Combine("Gateway", "CommandGateway.cs"), StringComparison.Ordinal))
            .Where(file => directDispatchCall.IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void ParticipantDirectoryShouldStayOutOfAggregatesAndGatewayStagesShouldStayOutOfAdapter()
    {
        string root = RepositoryRoot();
        string aggregateSource = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Hexalith.ChatBot.Server",
            "Operations",
            "GovernedOperationAggregate.cs"));
        aggregateSource.ShouldNotContain("IParticipantDirectory", Case.Sensitive);
        aggregateSource.ShouldNotContain("IProjectDirectory", Case.Sensitive);
        aggregateSource.ShouldNotContain("Hexalith.Parties", Case.Sensitive);
        aggregateSource.ShouldNotContain("Hexalith.Projects", Case.Sensitive);

        string adapterRoot = Path.Combine(root, "src", "Hexalith.ChatBot.Server", "Adapters", "Parties");
        string[] adapterViolations = Directory
            .EnumerateFiles(adapterRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => File.ReadAllText(file).Contains("Gateway.Stages", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();
        adapterViolations.ShouldBeEmpty();
    }

    [Fact]
    public static void UiAndWorkersMustNotReferenceGatewayGovernanceSeams()
    {
        string root = RepositoryRoot();
        string[] forbidden =
        [
            "IRiskClassifier",
            "IApprovalGate",
            "IAuditWriter",
            "IIdempotencyStore",
            "IParticipantDirectory",
            "IProjectDirectory",
        ];

        string[] violations = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(static file => file.Contains(".UI", StringComparison.Ordinal) || file.Contains(".Workers", StringComparison.Ordinal))
            .Where(file => forbidden.Any(token => File.ReadAllText(file).Contains(token, StringComparison.Ordinal)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void ServerAndContractsShouldKeepTimeUtcAtTheBoundary()
    {
        string root = RepositoryRoot();
        string[] allowed =
        [
            Path.Combine("src", "Hexalith.ChatBot.Server", "Audit", "SystemClock.cs"),
        ];
        Regex forbidden = new(@"DateTime\.Now|DateTimeOffset\.Now|\.ToLocalTime\s*\(|TimeZoneInfo\.ConvertTime", RegexOptions.CultureInvariant);
        string[] violations = Directory
            .EnumerateFiles(Path.Combine(root, "src", "Hexalith.ChatBot.Server"), "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "src", "Hexalith.ChatBot.Contracts"), "*.cs", SearchOption.AllDirectories))
            .Where(file => !allowed.Any(allowedPath => file.EndsWith(allowedPath, StringComparison.Ordinal)))
            .Where(file => forbidden.IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void SurfaceAdaptersShouldNotWriteAuditRecordsDirectly()
    {
        string root = RepositoryRoot();
        string[] adapterSources = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(static file => file.Contains(".UI", StringComparison.Ordinal)
                || file.Contains(".Cli", StringComparison.Ordinal)
                || file.Contains(".Mcp", StringComparison.Ordinal)
                || file.Contains(".Workers", StringComparison.Ordinal))
            .ToArray();

        string[] forbidden =
        [
            "IAuditWriter",
            "RecordPreCommitAsync",
            "RecordPostCommitAsync",
            "AuditEnvelope",
        ];

        string[] violations = adapterSources
            .Where(file => forbidden.Any(token => File.ReadAllText(file).Contains(token, StringComparison.Ordinal)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void ChatBotServerMustNotUseEventStoreActorIdempotencyChecker()
    {
        string root = RepositoryRoot();
        string[] violations = Directory
            .EnumerateFiles(Path.Combine(root, "src", "Hexalith.ChatBot.Server"), "*.cs", SearchOption.AllDirectories)
            .Where(file => File.ReadAllText(file).Contains("IdempotencyChecker", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void SurfaceAdaptersMustNotReferenceGatewayIdempotencyStages()
    {
        string root = RepositoryRoot();
        string[] adapterSources = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(static file => file.Contains(".UI", StringComparison.Ordinal)
                || file.Contains(".Cli", StringComparison.Ordinal)
                || file.Contains(".Mcp", StringComparison.Ordinal)
                || file.Contains(".Workers", StringComparison.Ordinal))
            .ToArray();

        string[] forbidden =
        [
            "IIdempotencyStore",
            "CoarseIdempotency",
            "Gateway.Idempotency",
            "IRiskClassifier",
            "IApprovalGate",
            "IAuditWriter",
            "DaprClient",
            "EventStore.Contracts.Commands",
            "IDomainProcessor",
        ];

        string[] violations = adapterSources
            .Where(file => forbidden.Any(token => File.ReadAllText(file).Contains(token, StringComparison.Ordinal)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void CommandGatewayRegistrationMustNotUsePassThroughIdempotencyStore()
    {
        string root = RepositoryRoot();
        string registrationSource = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Hexalith.ChatBot.Server",
            "Gateway",
            "CommandGatewayServiceCollectionExtensions.cs"));

        registrationSource.ShouldNotContain("PassThroughIdempotencyStore", Case.Sensitive);
        registrationSource.ShouldContain("IIdempotencyStore", Case.Sensitive);
    }

    [Fact]
    public static void SpineCommandAllowlistMustBindToHardcodedSetAndAdmitNoAllowAllDoubleInProductionSource()
    {
        string root = RepositoryRoot();

        // (1) Production DI binds the spine allowlist to the hardcoded M0 set, never a permissive double.
        string registrationSource = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Hexalith.ChatBot.Server",
            "Gateway",
            "CommandGatewayServiceCollectionExtensions.cs"));
        registrationSource.ShouldContain("ISpineCommandAllowlist, ChatBotSpineCommandAllowlist", Case.Sensitive);

        // (2) The ONLY ISpineCommandAllowlist implementation anywhere under src/ is the hardcoded set, so a
        // permissive/allow-all test double (which the gateway and bootstrap tests inject) lives only in test
        // assemblies and can never be wired into production DI.
        Regex implementsAllowlist = new(@":\s*ISpineCommandAllowlist\b", RegexOptions.CultureInvariant);
        string[] implementations = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(file => implementsAllowlist.IsMatch(File.ReadAllText(file)))
            .Select(static file => Path.GetFileNameWithoutExtension(file))
            .ToArray();
        implementations.ShouldBe(["ChatBotSpineCommandAllowlist"], ignoreOrder: true);

        // (3) No production allowlist may admit every command via an unconditional `IsAllowed(...) => true`.
        Regex allowAll = new(@"bool\s+IsAllowed\s*\([^)]*\)\s*=>\s*true\b", RegexOptions.CultureInvariant);
        string[] permissive = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(file => allowAll.IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();
        permissive.ShouldBeEmpty();
    }

    [Fact]
    public static void NonGeneratedChatBotSourceShouldNotHardCodeLegacyLifecycleLiterals()
    {
        string root = RepositoryRoot();
        string[] legacyLifecycleStates = ["pending", "accepted", "running", "succeeded", "cancelled"];
        Regex stringLiteral = new("\"(?<value>pending|accepted|running|succeeded|cancelled)\"", RegexOptions.CultureInvariant);
        string[] violations = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.Contains(Path.Combine("Generated", string.Empty), StringComparison.Ordinal))
            .Where(static file => !file.EndsWith(Path.Combine("Enums", "ProjectConversationAttachmentStatus.cs"), StringComparison.Ordinal))
            .Where(static file => !file.EndsWith(Path.Combine("Enums", "ApprovalStatus.cs"), StringComparison.Ordinal))
            .Where(static file => !file.EndsWith(Path.Combine("Enums", "AiOutcomeStatus.cs"), StringComparison.Ordinal))
            // Story 10.6b: AI response progress uses a bounded transport/projection token set that intentionally
            // includes pending/cancelled but is distinct from the legacy command lifecycle enum.
            .Where(static file => !file.EndsWith(Path.Combine("Enums", "AiResponseProgressState.cs"), StringComparison.Ordinal))
            .Where(static file => !file.EndsWith(Path.Combine("Enums", "AiResponseTerminalReason.cs"), StringComparison.Ordinal))
            .Where(static file => !file.EndsWith(Path.Combine("ProjectConversation", "ProjectConversationAiResponseProgressStates.cs"), StringComparison.Ordinal))
            // Story 9.8: TenantExportClassStatuses is a bounded, AC3-mandated export-status token set (succeeded /
            // failed-retryable / failed-terminal) — a distinct compliance domain, not the legacy lifecycle enum. It
            // legitimately owns the "succeeded" token exactly like the status enums above.
            .Where(static file => !file.EndsWith(Path.Combine("Commands", "TenantExportClassStatuses.cs"), StringComparison.Ordinal))
            // Story 9.9: DeletionErasureClassStatuses is a bounded, AC4-mandated deletion-status token set (succeeded /
            // failed-retryable / failed-terminal) — the same compliance domain as TenantExportContracts, not the legacy
            // lifecycle enum. It legitimately owns the "succeeded" token exactly like the status enums above.
            .Where(static file => !file.EndsWith(Path.Combine("Commands", "DeletionErasureClassStatuses.cs"), StringComparison.Ordinal))
            .Where(static file => !file.EndsWith(Path.Combine("Localization", "ChatBotUiTextLocalizer.cs"), StringComparison.Ordinal))
            .Where(file => stringLiteral.Matches(File.ReadAllText(file))
                .Select(static match => match.Groups["value"].Value)
                .Any(value => legacyLifecycleStates.Contains(value, StringComparer.Ordinal)
                    // The native evidence token owner may declare accepted; other lifecycle tokens remain guarded.
                    && !(value == "accepted"
                        && file.EndsWith(Path.Combine("Adapters", "Folders", "NativeEvidenceTokens.cs"), StringComparison.Ordinal))))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void AdapterFacingCommandSubmissionMustNotExposeTenantAuthority()
    {
        string root = RepositoryRoot();
        string clientFacade = File.ReadAllText(Path.Combine(root, "src", "Hexalith.ChatBot.Client", "IChatBotClient.cs"));
        string openApi = File.ReadAllText(Path.Combine(root, "src", "Hexalith.ChatBot.Contracts", "openapi", "hexalith.chatbot.v1.yaml"));

        clientFacade.ShouldNotContain("tenantId", Case.Insensitive);

        Match requestSchema = Regex.Match(
            openApi,
            @"CommandSubmissionRequest:(?<schema>[\s\S]*?)CommandSubmissionResponse:",
            RegexOptions.CultureInvariant);
        requestSchema.Success.ShouldBeTrue();
        requestSchema.Groups["schema"].Value.ShouldNotContain("tenantId", Case.Insensitive);
    }

    [Fact]
    public static void GatewaySourcesMustNotDeclareCompetingContractWireDtos()
    {
        string gatewayRoot = Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.Server", "Gateway");
        HashSet<string> generatedNames = ["CommandSubmissionRequest", "CommandSubmissionResponse", "PriorCommandOutcome", "OperationStatus", "ProblemDetails"];
        HashSet<string> acceptedFields = ["CommandId", "CorrelationId", "OperationId", "LifecycleState", "AcceptedAt", "ReasonCode", "RetryEligible", "PriorOutcome"];
        List<string> violations = [];

        foreach (string file in Directory.EnumerateFiles(gatewayRoot, "*.cs", SearchOption.AllDirectories))
        {
            SyntaxNode root = CSharpSyntaxTree.ParseText(
                File.ReadAllText(file), cancellationToken: TestContext.Current.CancellationToken)
                .GetRoot(TestContext.Current.CancellationToken);
            foreach (TypeDeclarationSyntax declaration in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                string name = declaration.Identifier.ValueText;
                HashSet<string> fields = declaration.Members.OfType<PropertyDeclarationSyntax>()
                    .Select(static property => property.Identifier.ValueText)
                    .Concat(declaration is RecordDeclarationSyntax record
                        ? record.ParameterList?.Parameters.Select(static parameter => parameter.Identifier.ValueText) ?? []
                        : [])
                    .ToHashSet(StringComparer.Ordinal);
                if (generatedNames.Contains(name) ||
                    name.EndsWith("WireModel", StringComparison.Ordinal) ||
                    name.EndsWith("WireRequest", StringComparison.Ordinal) ||
                    name.EndsWith("WireResponse", StringComparison.Ordinal) ||
                    (name != "OperationStatusRecord" && fields.Count(acceptedFields.Contains) >= 5))
                {
                    violations.Add($"{Path.GetRelativePath(RepositoryRoot(), file)}:{name}");
                }
            }
        }

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void ContractSpineOperationsShouldBindTypedClientAndSeparatelyGovernedSurfaces()
    {
        string openApi = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.Contracts", "openapi", "hexalith.chatbot.v1.yaml"));
        openApi.ShouldContain("operationId: SubmitCommand");
        openApi.ShouldContain("operationId: GetOperationStatus");

        Type facade = typeof(Hexalith.ChatBot.Client.IChatBotClient);
        foreach (Type type in new[] { facade, typeof(Hexalith.ChatBot.Client.ChatBotClient) })
        {
            AssertSignature(type, "SubmitAsync", typeof(Task<Hexalith.ChatBot.Client.Generated.CommandSubmissionResponse>),
                typeof(Hexalith.ChatBot.Contracts.Commands.IChatBotCommand), typeof(string), typeof(string),
                typeof(Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin), typeof(CancellationToken));
            AssertSignature(type, "SubmitWithCommandIdAsync", typeof(Task<Hexalith.ChatBot.Client.Generated.CommandSubmissionResponse>),
                typeof(Hexalith.ChatBot.Contracts.Commands.IChatBotCommand), typeof(string), typeof(string), typeof(string),
                typeof(Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin), typeof(CancellationToken));
            AssertSignature(type, "GetOperationStatusAsync", typeof(Task<Hexalith.ChatBot.Client.Generated.OperationStatus>),
                typeof(string), typeof(string), typeof(string), typeof(CancellationToken));
        }
        foreach (Type type in new[] { typeof(Hexalith.ChatBot.Client.Generated.IClient), typeof(Hexalith.ChatBot.Client.Generated.Client) })
        {
            AssertSignature(type, "SubmitCommandAsync", typeof(Task<Hexalith.ChatBot.Client.Generated.CommandSubmissionResponse>),
                typeof(string), typeof(string), typeof(Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest), typeof(CancellationToken));
            AssertSignature(type, "GetOperationStatusAsync", typeof(Task<Hexalith.ChatBot.Client.Generated.OperationStatus>),
                typeof(string), typeof(string), typeof(string), typeof(CancellationToken));
        }
        Hexalith.ChatBot.Mcp.ChatBotMcpToolCatalog.TryGet("chatbot.operation.status", out var statusTool).ShouldBeTrue();
        statusTool.ContractName.ShouldBe("GetOperationStatus");
        statusTool.StateChanging.ShouldBeFalse();
        statusTool.RequiredArguments.ShouldBe(["operationId"]);
        statusTool.OptionalArguments.ShouldBe(["correlationId", "taskId", "tenant"]);
        AssertSignature(typeof(Hexalith.ChatBot.Mcp.ChatBotMcpService), "InvokeAsync", typeof(Task<JsonElement>),
            typeof(Hexalith.ChatBot.Mcp.ChatBotMcpInvocation), typeof(CancellationToken));
        AssertSignature(typeof(Hexalith.ChatBot.Cli.ChatBotCliService), "ShowOperationStatusAsync", typeof(Task<int>),
            typeof(string), typeof(Hexalith.ChatBot.Cli.ChatBotCliOptions), typeof(CancellationToken));
        AssertSignature(typeof(Hexalith.ChatBot.Cli.ChatBotCliService), "RunSafelyAsync", typeof(Task<int>),
            typeof(Func<Task<int>>), typeof(Hexalith.ChatBot.Cli.ChatBotCliOptions));
    }

    private static void AssertSignature(Type type, string name, Type returnType, params Type[] parameterTypes)
    {
        var method = type.GetMethod(name, parameterTypes).ShouldNotBeNull($"{type.FullName}.{name} must retain its contract signature");
        method.ReturnType.ShouldBe(returnType);
        method.GetParameters().Select(static parameter => parameter.ParameterType).ShouldBe(parameterTypes);
    }

    [Fact]
    public static void ContractsQueriesShouldStayLowDependency()
    {
        string root = RepositoryRoot();
        string queriesPath = Path.Combine(root, "src", "Hexalith.ChatBot.Contracts", "Queries");
        Directory.Exists(queriesPath).ShouldBeTrue();
        string[] forbidden =
        [
            "Hexalith.ChatBot.Server",
            "Dapr",
            "Microsoft.AspNetCore",
            "OpenTelemetry",
            "ILogger",
        ];

        string[] violations = Directory
            .EnumerateFiles(queriesPath, "*.cs", SearchOption.AllDirectories)
            .Where(file => forbidden.Any(token => File.ReadAllText(file).Contains(token, StringComparison.Ordinal)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void ProjectFilesAndPackageWrapperShouldPreserveExclusiveCentralAuthority()
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
                .Descendants()
                .Where(static element =>
                    (element.Name.LocalName is "PackageReference" or "GlobalPackageReference")
                    && (element.Attribute("Version") is not null
                        || element.Attribute("VersionOverride") is not null
                        || element.Elements().Any(static child => child.Name.LocalName is "Version" or "VersionOverride")))
                .Select(element => Path.GetRelativePath(root, projectFile) + ":" + element.Attribute("Include")?.Value)
                .Concat(project
                    .Descendants()
                    .Where(static element => element.Name.LocalName == "PackageVersion")
                    .Select(element => Path.GetRelativePath(root, projectFile) + ":PackageVersion="
                        + (element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value)))
                .Concat(project
                    .Descendants()
                    .Where(static element =>
                        element.Name.LocalName == "ManagePackageVersionsCentrally"
                        && string.Equals(element.Value.Trim(), "false", StringComparison.OrdinalIgnoreCase))
                    .Select(_ => Path.GetRelativePath(root, projectFile) + ":ManagePackageVersionsCentrally=false"))
                .Concat(project
                    .Descendants()
                    .Where(static element =>
                        element.Name.LocalName == "CentralPackageVersionOverrideEnabled"
                        && string.Equals(element.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase))
                    .Select(_ => Path.GetRelativePath(root, projectFile) + ":CentralPackageVersionOverrideEnabled=true"));
            inlineVersions.AddRange(violations);
        }

        // The repo-root shared build files are additional version-override escape vectors: PackageVersion items,
        // VersionOverride attributes/elements, and Hexalith*Version version-family properties would all bypass the
        // shared catalog. The legitimate Hexalith*Root path-detection properties do not match the version regex.
        Regex hexalithVersionProperty = new("^Hexalith.*Version$", RegexOptions.CultureInvariant);
        string[] rootBuildFiles =
        [
            Path.Combine(root, "Directory.Build.props"),
            Path.Combine(root, "Directory.Build.targets"),
        ];
        foreach (string rootBuildFile in rootBuildFiles.Where(File.Exists))
        {
            XDocument rootBuild = XDocument.Load(rootBuildFile);
            IEnumerable<string> violations = rootBuild
                .Descendants()
                .Where(static element => element.Name.LocalName == "PackageVersion")
                .Select(element => Path.GetRelativePath(root, rootBuildFile) + ":PackageVersion="
                    + (element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value))
                .Concat(rootBuild
                    .Descendants()
                    .Where(static element =>
                        element.Attribute("VersionOverride") is not null
                        || element.Name.LocalName == "VersionOverride")
                    .Select(element => Path.GetRelativePath(root, rootBuildFile) + ":VersionOverride=" + element.Name.LocalName))
                .Concat(rootBuild
                    .Descendants()
                    .Where(element => hexalithVersionProperty.IsMatch(element.Name.LocalName))
                    .Select(element => Path.GetRelativePath(root, rootBuildFile) + ":" + element.Name.LocalName));
            inlineVersions.AddRange(violations);
        }

        inlineVersions.ShouldBeEmpty();
        PackageCatalogTestHelper.AssertExclusiveAuthority();
    }

    [Fact]
    public static void RootDeclaredSiblingPackageWrappersShouldDelegateVersionsToSharedBuildsCatalog()
    {
        string root = RepositoryRoot();
        string gitmodules = File.ReadAllText(Path.Combine(root, ".gitmodules"));
        string[] siblingRoots = Regex.Matches(
                gitmodules,
                "(?m)^\\s*path\\s*=\\s*(?<path>references/[^\\r\\n]+)\\s*$")
            .Select(static match => Path.Combine(
                RepositoryRoot(),
                match.Groups["path"].Value.Replace('/', Path.DirectorySeparatorChar)))
            .ToArray();
        siblingRoots.Length.ShouldBeGreaterThan(
            5,
            "the shared-authority guard must inspect the actual root-declared sibling set");

        string[] wrappers = siblingRoots
            .Select(static sibling => Path.Combine(sibling, "Directory.Packages.props"))
            .Where(File.Exists)
            .ToArray();
        wrappers.Length.ShouldBeGreaterThan(
            5,
            "root-declared package wrappers must remain present and centrally auditable");

        foreach (string wrapper in wrappers)
        {
            PackageCatalogTestHelper.AssertSiblingAuthority(wrapper);
        }

        PackageCatalogTestHelper.Version("Hexalith.Folders.Client").ShouldBe("2.0.1");
        PackageCatalogTestHelper.Version("Hexalith.Folders.Contracts").ShouldBe("2.0.1");
        PackageCatalogTestHelper.Version("Hexalith.Projects.Client").ShouldBe("2.0.0");
        PackageCatalogTestHelper.Version("Hexalith.Projects.Contracts").ShouldBe("2.0.0");
        PackageCatalogTestHelper.Version("LibGit2Sharp").ShouldBe("0.32.0");
        PackageCatalogTestHelper.AssertExclusiveAuthority();
    }

    /// <summary>The evaluated server chooses one EventStore reference family for the effective dependency mode.</summary>
    /// <param name="configuration">The explicit configuration, if supplied.</param>
    /// <param name="ci">Whether to set the CI property.</param>
    /// <param name="explicitMode">The explicit dependency-mode override, if supplied.</param>
    /// <param name="usesSource">Whether the server should reference EventStore projects.</param>
    [Theory]
    [InlineData(null, false, null, true)]
    [InlineData("Debug", false, null, true)]
    [InlineData("Release", false, null, false)]
    [InlineData(null, true, null, false)]
    [InlineData("Debug", true, null, false)]
    [InlineData("Debug", false, "false", false)]
    [InlineData("Release", false, "true", true)]
    public static void EvaluatedServerShouldSelectMatchingEventStoreReferenceFamily(
        string? configuration, bool ci, string? explicitMode, bool usesSource)
    {
        List<string> arguments = [];
        if (configuration is not null)
        {
            arguments.Add($"-property:Configuration={configuration}");
        }

        if (ci)
        {
            arguments.Add("-property:CI=true");
        }

        if (explicitMode is not null)
        {
            arguments.Add($"-property:UseHexalithProjectReferences={explicitMode}");
        }

        string project = Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.Server", "Hexalith.ChatBot.Server.csproj");
        using JsonDocument evaluation = PackageCatalogTestHelper.EvaluateProjectReferences(project, [.. arguments]);
        JsonElement properties = evaluation.RootElement.GetProperty("Properties");
        properties.GetProperty("UseHexalithProjectReferences").GetString().ShouldBe(usesSource ? "true" : "false");

        JsonElement items = evaluation.RootElement.GetProperty("Items");
        string[] expectedEventStore = ["Hexalith.EventStore.Client", "Hexalith.EventStore.Contracts", "Hexalith.EventStore.DomainService"];
        JsonElement[] eventStoreProjects = items.GetProperty("ProjectReference").EnumerateArray()
            .Where(static item => item.GetProperty("Filename").GetString()!.StartsWith("Hexalith.EventStore.", StringComparison.Ordinal))
            .OrderBy(static item => item.GetProperty("Filename").GetString(), StringComparer.Ordinal)
            .ToArray();
        eventStoreProjects.Select(static item => item.GetProperty("Filename").GetString()!)
            .ShouldBe(usesSource ? expectedEventStore : []);
        EventStoreItems(items, "PackageReference", "Identity").ShouldBe(usesSource ? [] : expectedEventStore);
        if (usesSource)
        {
            string expectedRoot = Path.Combine(RepositoryRoot(), "references", "Hexalith.EventStore");
            string? actualRoot = properties.GetProperty("HexalithEventStoreRoot").GetString();
            actualRoot.ShouldNotBeNullOrWhiteSpace();
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(actualRoot!)).ShouldBe(expectedRoot);
            string[] expectedProjects = expectedEventStore.Select(name => Path.Combine(expectedRoot, "src", name, name + ".csproj")).ToArray();
            string[] actualProjects = eventStoreProjects.Select(static item => item.GetProperty("FullPath").GetString()!).ToArray();
            actualProjects.ShouldBe(expectedProjects);
            actualProjects.ShouldAllBe(static path => File.Exists(path));
        }
        foreach (string name in expectedEventStore)
        {
            JsonElement version = items.GetProperty("PackageVersion").EnumerateArray()
                .Single(item => item.GetProperty("Identity").GetString() == name);
            version.GetProperty("Version").GetString().ShouldBe(PackageCatalogTestHelper.Version(name));
        }

        static string[] EventStoreItems(JsonElement items, string itemType, string metadata)
            => items.GetProperty(itemType).EnumerateArray()
                .Select(item => item.GetProperty(metadata).GetString()!)
                .Where(name => name.StartsWith("Hexalith.EventStore.", StringComparison.Ordinal))
                .Order(StringComparer.Ordinal)
                .ToArray();
    }

    [Fact]
    public static void RootConfigurationShouldPinSdkTargetFrameworkAndCentralPackages()
    {
        string root = RepositoryRoot();

        using JsonDocument global = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "global.json")));
        JsonElement sdk = global.RootElement.GetProperty("sdk");
        Version sdkVersion = Version.Parse(sdk.GetProperty("version").GetString()!);
        sdkVersion.Major.ShouldBe(11);
        sdkVersion.Minor.ShouldBe(0);
        (sdkVersion.Build / 100).ShouldBe(1, "the repository pins the .NET 11.0.1xx SDK feature band");
        sdk.GetProperty("rollForward").GetString().ShouldBe("latestPatch");

        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<TargetFramework>net11.0</TargetFramework>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<Nullable>enable</Nullable>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<ImplicitUsings>enable</ImplicitUsings>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<LangVersion>14.0</LangVersion>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<Deterministic>true</Deterministic>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<NuGetAudit>true</NuGetAudit>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<NuGetAuditMode>all</NuGetAuditMode>");
        File.ReadAllText(Path.Combine(root, "eng", "ChatBot.Composition.props")).ShouldContain(
            "<UseHexalithProjectReferences Condition=\"'$(UseHexalithProjectReferences)' == ''\">false</UseHexalithProjectReferences>");
        File.ReadAllText(Path.Combine(root, "src", "Hexalith.ChatBot.AppHost", "Hexalith.ChatBot.AppHost.csproj"))
            .ShouldContain("<IsPublishable>false</IsPublishable>");
        PackageCatalogTestHelper.AssertExclusiveAuthority();
    }

    [Fact]
    public static void ReleaseMetadataShouldBindExactPackageAndContainerInventory()
    {
        string root = RepositoryRoot();
        using JsonDocument document = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "release-metadata.json")));
        JsonElement metadata = document.RootElement;

        metadata.GetProperty("schemaVersion").GetInt32().ShouldBe(1);

        Dictionary<string, string> packages = metadata.GetProperty("inventory").GetProperty("packages")
            .EnumerateArray()
            .ToDictionary(
                static artifact => artifact.GetProperty("id").GetString()!,
                static artifact => artifact.GetProperty("project").GetString()!,
                StringComparer.Ordinal);
        packages.ShouldBe(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Hexalith.ChatBot.Contracts"] = "src/Hexalith.ChatBot.Contracts/Hexalith.ChatBot.Contracts.csproj",
                ["Hexalith.ChatBot.Client"] = "src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj",
                ["Hexalith.ChatBot.Testing"] = "src/Hexalith.ChatBot.Testing/Hexalith.ChatBot.Testing.csproj",
            },
            ignoreOrder: true);
        foreach (string projectPath in packages.Values)
        {
            XDocument.Load(Path.Combine(root, projectPath))
                .Descendants("IsPackable")
                .Single()
                .Value
                .ShouldBe("true");
        }

        Dictionary<string, string> containers = metadata.GetProperty("inventory").GetProperty("containers")
            .EnumerateArray()
            .ToDictionary(
                static artifact => artifact.GetProperty("id").GetString()!,
                static artifact => artifact.GetProperty("project").GetString()!,
                StringComparer.Ordinal);
        containers.ShouldBe(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["hexalith-chatbot-server"] = "src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj",
                ["hexalith-chatbot-ui"] = "src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj",
            },
            ignoreOrder: true);
        foreach ((string containerId, string projectPath) in containers)
        {
            XDocument project = XDocument.Load(Path.Combine(root, projectPath));
            project.Descendants("IsPublishable").Single().Value.ShouldBe("true");
            project.Descendants("EnableContainer").Single().Value.ShouldBe("true");
            project.Descendants("ContainerRepository").Single().Value.ShouldBe(containerId);

            using JsonDocument publish = PackageCatalogTestHelper.EvaluateReleasePublishFiles(Path.Combine(root, projectPath));
            JsonElement publishFiles = publish.RootElement.GetProperty("Items").GetProperty("ResolvedFileToPublish");
            publishFiles.GetArrayLength().ShouldBeGreaterThan(0);
            foreach (JsonElement file in publishFiles.EnumerateArray())
            {
                const string localAcl = "accesscontrol.local.yaml";
                string source = file.GetProperty("FullPath").GetString()!;
                string target = file.GetProperty("RelativePath").GetString()!.Replace('\\', '/');
                (string.Equals(Path.GetFileName(source), localAcl, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Path.GetFileName(target), localAcl, StringComparison.OrdinalIgnoreCase))
                    .ShouldBeFalse($"{containerId} would publish '{source}' as '{target}'.");
            }
        }

    }

    [Fact]
    public static void RootSubmoduleDeclarationsShouldRemainUnderReferencesAndUnique()
    {
        string modules = File.ReadAllText(Path.Combine(RepositoryRoot(), ".gitmodules"));

        Regex.Matches(modules, "path = references/Hexalith.EventStore").Count.ShouldBe(1);
        modules.ShouldContain("path = references/Hexalith.Tenants");
        modules.ShouldContain("path = references/Hexalith.FrontComposer");
        modules.ShouldNotContain("path = Hexalith.EventStore");

        string[] paths = Regex.Matches(modules, @"(?m)^\s*path\s*=\s*(\S+)\s*$")
            .Select(static match => match.Groups[1].Value)
            .ToArray();
        paths.ShouldNotBeEmpty("the root-declared sibling scan must be non-vacuous");
        paths.Distinct(StringComparer.Ordinal).Count().ShouldBe(paths.Length);
        paths.ShouldAllBe(static path => path.StartsWith("references/", StringComparison.Ordinal));
    }

    /// <summary>Preserves production refusal ordering and the committed-state M2 product proof.</summary>
    [Fact]
    public static void TopologySmokeShouldPreserveRefusalOrderAndCommittedStateProof()
    {
        string root = RepositoryRoot();
        string source = File.ReadAllText(Path.Combine(root, "tests", "Hexalith.ChatBot.IntegrationTests",
            "TrivialGovernedCommandAspireE2eTests.cs"));
        MethodDeclarationSyntax[] methods = CSharpSyntaxTree.ParseText(source,
                cancellationToken: TestContext.Current.CancellationToken).GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
        const string smokeName = "RunGovernedCommandRefusalAsync";
        const string m2Name = "RunCommittedGovernedStateM2GateAsync";
        string smoke = methods.Single(method => method.Identifier.ValueText == smokeName).Body!.ToString();
        string release = methods.Single(method => method.Identifier.ValueText == m2Name).Body!.ToString();
        smoke.ShouldContain("EnsureCurrentRequesterOwnersAsync");
        smoke.ShouldContain("HttpStatusCode.ServiceUnavailable");
        smoke.ShouldContain("ChatBotMessageCodes.CanonicalExecutionUnavailable");
        smoke.ShouldContain("refusedNote.Body, HttpStatusCode.Forbidden");
        smoke.ShouldContain("ChatBotMessageCodes.AuthorizationDenied");
        smoke.IndexOf("SubmitGovernedNoteAsync", StringComparison.Ordinal)
            .ShouldBeGreaterThan(smoke.IndexOf("ChatBotMessageCodes.CanonicalExecutionUnavailable", StringComparison.Ordinal));
        smoke.ShouldContain("ReadGovernedStateSnapshotAsync");
        smoke.ShouldContain("AssertRefusedCommandHasNoReservationsAsync");
        smoke.ShouldNotContain("SeedCurrentGovernedNoteAuthorityAsync");
        smoke.ShouldNotContain("AssertM2ReleaseGateIsClearAsync");
        release.ShouldContain("SeedCurrentGovernedNoteAuthorityAsync");
        release.ShouldContain("SubmitGovernedNoteUntilAcceptedAsync");
        release.ShouldContain("AssertM2ReleaseGateIsClearAsync");
        release.IndexOf("AssertM2ReleaseGateIsClearAsync", StringComparison.Ordinal)
            .ShouldBeGreaterThan(release.IndexOf("SubmitGovernedNoteUntilAcceptedAsync", StringComparison.Ordinal));

    }

    [Fact]
    public static void ServerProblemDetailsTextShouldStayInsideCatalogResolverOrRedactionBoundary()
    {
        string root = RepositoryRoot();
        Regex problemTextLiteral = new(@"\b(?:Title|Message|Detail)\s*=\s*""", RegexOptions.CultureInvariant);
        string[] allowed =
        [
            Path.Combine("Gateway", "ChatBotProblemDetailsFactory.cs"),
            Path.Combine("Gateway", "Redaction", "CoarseUserFacingRedactionStage.cs"),
        ];

        string[] violations = Directory
            .EnumerateFiles(Path.Combine(root, "src", "Hexalith.ChatBot.Server"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !allowed.Any(allowedPath => file.EndsWith(allowedPath, StringComparison.Ordinal)))
            .Where(file => problemTextLiteral.IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void RuntimeGatewayRegistrationMustNotResolveAlwaysControlOrRateLimitProviders()
    {
        string root = RepositoryRoot();
        string registrationSource = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Hexalith.ChatBot.Server",
            "Gateway",
            "CommandGatewayServiceCollectionExtensions.cs"));

        string[] forbiddenRegistrations =
        [
            "AddScoped<IServiceClientControlStateProvider, AlwaysActive",
            "AddScoped<IAiActorControlStateProvider, AlwaysActive",
            "AddScoped<ICommandCapabilityControlStateProvider, AlwaysActive",
            "AddScoped<IOutboundChannelControlStateProvider, AlwaysActive",
            "AddScoped<IServiceClientRateLimitProvider, AlwaysUnlimited",
            "AddScoped<IAiActorRateLimitProvider, AlwaysUnlimited",
            "AddScoped<ICommandCapabilityRateLimitProvider, AlwaysUnlimited",
            "AddScoped<IOutboundChannelRateLimitProvider, AlwaysUnlimited",
        ];

        foreach (string forbidden in forbiddenRegistrations)
        {
            registrationSource.ShouldNotContain(forbidden, Case.Sensitive);
        }

        registrationSource.ShouldContain("ProjectionBackedServiceClientControlStateProvider");
        registrationSource.ShouldContain("ProjectionBackedOutboundChannelRateLimitProvider");
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

    private static string? DependencyCondition(XElement dependency)
    {
        return dependency.Attribute("Condition")?.Value
            ?? dependency.Ancestors("ItemGroup").FirstOrDefault()?.Attribute("Condition")?.Value;
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
