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
            "tests/Hexalith.ChatBot.RecoverySandbox/Hexalith.ChatBot.RecoverySandbox.csproj",
            "tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj",
            "tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj",
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
        projects.ShouldNotContain(
            static path => path.StartsWith("references/", StringComparison.Ordinal),
            "sibling library projects enter through conditional project/package references, not as explicit solution projects");
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

                string packageId = include
                    .Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
                    .Last()
                    .Replace(".csproj", string.Empty, StringComparison.Ordinal);
                XElement? packageReference = project
                    .Descendants("PackageReference")
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
        packageCounterpartCount.ShouldBe(
            externalSourceReferenceCount,
            "every external source dependency must have an exact package-mode counterpart");
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
        localCompositionResources.ShouldAllBe(
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

        PackageCatalogTestHelper.Version("ModelContextProtocol").ShouldBe("2.2.0");

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
                .Any(value => legacyLifecycleStates.Contains(value, StringComparer.Ordinal)))
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

        string[] localVersionDeclarations = wrappers
            .SelectMany(wrapper => XDocument.Load(wrapper)
                .Descendants("PackageVersion")
                .Select(version => Path.GetRelativePath(root, wrapper) + ":"
                    + (version.Attribute("Include")?.Value ?? version.Attribute("Update")?.Value)))
            .ToArray();
        localVersionDeclarations.ShouldBeEmpty(
            "root-declared sibling wrappers import the shared Builds catalog and must not become version authorities");

        PackageCatalogTestHelper.Version("Hexalith.Folders.Client").ShouldBe("1.0.0");
        PackageCatalogTestHelper.Version("Hexalith.Folders.Contracts").ShouldBe("1.0.0");
        PackageCatalogTestHelper.Version("Hexalith.Projects.Client").ShouldBe("1.0.0");
        PackageCatalogTestHelper.Version("Hexalith.Projects.Contracts").ShouldBe("1.0.0");
        PackageCatalogTestHelper.Version("LibGit2Sharp").ShouldBe("0.32.0");
        PackageCatalogTestHelper.AssertExclusiveAuthority();
    }

    [Fact]
    public static void RootConfigurationShouldPinSdkTargetFrameworkAndCentralPackages()
    {
        string root = RepositoryRoot();

        File.ReadAllText(Path.Combine(root, "global.json")).ShouldContain("\"version\": \"10.0.400\"");
        File.ReadAllText(Path.Combine(root, "global.json")).ShouldContain("\"rollForward\": \"latestPatch\"");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<TargetFramework>net10.0</TargetFramework>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<Nullable>enable</Nullable>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<ImplicitUsings>enable</ImplicitUsings>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<LangVersion>14.0</LangVersion>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<Deterministic>true</Deterministic>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<NuGetAudit>true</NuGetAudit>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<NuGetAuditMode>all</NuGetAuditMode>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain(
            "<UseHexalithProjectReferences Condition=\"'$(UseHexalithProjectReferences)' == ''\">false</UseHexalithProjectReferences>");
        File.ReadAllText(Path.Combine(root, "src", "Hexalith.ChatBot.AppHost", "Hexalith.ChatBot.AppHost.csproj"))
            .ShouldContain("<IsPublishable>false</IsPublishable>");
        PackageCatalogTestHelper.AssertExclusiveAuthority();
    }

    [Fact]
    public static void ReleaseMetadataShouldBindExactInventoryAndRemainBlockedOnOpenEvidenceGates()
    {
        string root = RepositoryRoot();
        using JsonDocument document = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "release-metadata.json")));
        JsonElement metadata = document.RootElement;

        metadata.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        metadata.GetProperty("releasePosture").GetString().ShouldBe("blocked-open-gates");

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
        }

        JsonElement[] blockers = metadata.GetProperty("openBlockers").EnumerateArray().ToArray();
        blockers.Select(static blocker => blocker.GetProperty("gate").GetString()).ShouldBe(
            ["A5", "A6", "A9a", "A13"],
            ignoreOrder: true);
        blockers.ShouldAllBe(static blocker => blocker.GetProperty("state").GetString() == "open");

        JsonElement a9aEvidence = blockers
            .Single(static blocker => blocker.GetProperty("gate").GetString() == "A9a")
            .GetProperty("requiredEvidence");
        string[] exactA9aArtifacts = a9aEvidence.EnumerateArray()
            .Select(static artifact => string.Join(
                ':',
                artifact.GetProperty("kernel").GetString(),
                artifact.GetProperty("runtimeArtifact").GetString(),
                artifact.GetProperty("state").GetString()))
            .ToArray();
        exactA9aArtifacts.ShouldBe(
            [
                "association:AssociationScorer:open-exact-evidence-required",
                "task-intent:TaskIntentDetector:open-exact-evidence-required",
                "action-risk:ActionRiskClassifier:open-exact-evidence-required",
            ],
            ignoreOrder: true);

        metadata.GetProperty("authorizedReadinessClaims").GetArrayLength().ShouldBe(0);
        string[] prohibitedClaims = metadata.GetProperty("prohibitedClaims")
            .EnumerateArray()
            .Select(static claim => claim.GetString()!)
            .ToArray();
        prohibitedClaims.ShouldContain("M0 readiness");
        prohibitedClaims.ShouldContain("M1 readiness");
        prohibitedClaims.ShouldContain("pilot readiness");
        prohibitedClaims.ShouldContain("compliance readiness");
        prohibitedClaims.ShouldContain("production readiness");
        prohibitedClaims.ShouldContain("gate readiness");
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

    [Fact]
    public static void CiShouldInitializeOnlyReferencesSubmodulesNonRecursively()
    {
        string workflow = File.ReadAllText(Path.Combine(RepositoryRoot(), ".github", "workflows", "ci.yml"));

        workflow.ShouldContain("submodules: false");
        workflow.ShouldContain("git submodule update --init");
        workflow.ShouldNotContain("--recursive");
    }

    [Fact]
    public static void PullRequestMergeRefValidationShouldRemainIsolatedFromExactHeadEvidence()
    {
        string workflow = File.ReadAllText(Path.Combine(RepositoryRoot(), ".github", "workflows", "ci.yml"));
        // Bounded by the next top-level job key rather than by a named successor: a job inserted between the two
        // would otherwise be folded into this block and silently satisfy the isolation assertions below.
        string mergeJob = WorkflowJob(workflow, "pull-request-merge-ref");
        workflow.IndexOf("\n  pull-request-merge-ref:", StringComparison.Ordinal)
            .ShouldBeLessThan(workflow.IndexOf("\n  build:", StringComparison.Ordinal));

        mergeJob.ShouldContain("name: pull-request synthetic merge build and test");
        mergeJob.ShouldContain("if: github.event_name == 'pull_request'");
        mergeJob.ShouldContain("timeout-minutes: 60");
        mergeJob.ShouldContain("permissions:\n      contents: read");
        mergeJob.ShouldNotContain("\n    needs:");
        mergeJob.ShouldContain("ref: ${{ github.sha }}");
        mergeJob.ShouldContain("persist-credentials: false");
        mergeJob.ShouldContain("submodules: false");
        mergeJob.ShouldContain(
            "verify-pull-request-merge.sh \"$MERGE_SHA\" \"$PR_BASE_SHA\" \"$PR_HEAD_SHA\"");
        mergeJob.ShouldContain("git submodule update --init");
        mergeJob.ShouldNotContain("--recursive");
        mergeJob.ShouldContain("dotnet restore Hexalith.ChatBot.slnx");
        mergeJob.ShouldContain(
            "dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Release -p:UseHexalithProjectReferences=false -m:1 /nr:false");
        mergeJob.ShouldContain("bash .github/scripts/run-merge-test-lanes.sh");
        mergeJob.ShouldNotContain("actions/upload-artifact");
        mergeJob.ShouldNotContain("machine-test-results");
        mergeJob.ShouldNotContain("topology-acceptance-evidence");
        mergeJob.ShouldNotContain("story-evidence-integrity-reports");
        mergeJob.ShouldNotContain("provenance");

        int mergeJobIndex = workflow.IndexOf(mergeJob, StringComparison.Ordinal);
        mergeJobIndex.ShouldBeGreaterThanOrEqualTo(0);
        string otherJobs = workflow.Remove(mergeJobIndex, mergeJob.Length);
        otherJobs.ShouldNotContain("pull-request-merge-ref");
    }

    [Fact]
    public static void StoryEvidenceIntegrityGateShouldRemainFailClosedAndMachineBound()
    {
        string root = RepositoryRoot();
        string policy = File.ReadAllText(Path.Combine(root, "story-evidence-policy.json"));
        string workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        string releaseWorkflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "release.yml"));
        string daprInstaller = File.ReadAllText(Path.Combine(root, ".github", "scripts", "install-dapr-cli.sh"));
        string browserPrimarySource = File.ReadAllText(Path.Combine(
            root,
            "tests",
            "Hexalith.ChatBot.UI.E2E.Tests",
            "RealRenderCrossSurfaceE2ETests.cs"));
        string signalRPrimarySource = File.ReadAllText(Path.Combine(
            root,
            "tests",
            "Hexalith.ChatBot.Server.Tests",
            "Projections",
            "ChatBotProjectConversationHubE2ETests.cs"));
        string hostingAssetsPrimarySource = File.ReadAllText(Path.Combine(
            root,
            "tests",
            "Hexalith.ChatBot.UI.E2E.Tests",
            "FrontComposerShellIntegrationE2ETests.cs"));
        string aspireDaprPrimarySource = File.ReadAllText(Path.Combine(
            root,
            "tests",
            "Hexalith.ChatBot.IntegrationTests",
            "TrivialGovernedCommandAspireE2eTests.cs"));
        string recoveryPrimarySource = File.ReadAllText(Path.Combine(
            root,
            "tests",
            "Hexalith.ChatBot.IntegrationTests",
            "Recovery",
            "LiveContinuityAspireE2eTests.cs"));
        string toolProject = File.ReadAllText(Path.Combine(
            root,
            "tools",
            "Hexalith.ChatBot.StoryEvidenceGate",
            "Hexalith.ChatBot.StoryEvidenceGate.csproj"));
        string toolProgram = File.ReadAllText(Path.Combine(
            root,
            "tools",
            "Hexalith.ChatBot.StoryEvidenceGate",
            "Program.cs"));
        string lifecycleValidator = File.ReadAllText(Path.Combine(
            root,
            "tools",
            "Hexalith.ChatBot.StoryEvidenceGate",
            "LifecycleTransitionValidator.cs"));
        string trxReader = File.ReadAllText(Path.Combine(
            root,
            "tools",
            "Hexalith.ChatBot.StoryEvidenceGate",
            "TrxEvidenceReader.cs"));
        string attestor = File.ReadAllText(Path.Combine(
            root,
            "tools",
            "Hexalith.ChatBot.StoryEvidenceGate",
            "ProvenanceAttestor.cs"));
        string completionJob = WorkflowBlock(workflow, "story-evidence-integrity", "topology-acceptance");
        string completionProducer = WorkflowStep(
            completionJob,
            "Produce transition-declared current recovery primary result");
        string completionSummary = WorkflowStep(
            completionJob,
            "Summarize recovery attempt into metadata-only failure record");
        string completionUpload = WorkflowStep(
            completionJob,
            "Upload metadata-only recovery attempt evidence");
        string scheduledRecoveryJob = WorkflowBlock(
            workflow,
            "live-recovery-validation",
            "live-recovery-evidence-gate");
        string scheduledRecoveryProducer = WorkflowStep(
            scheduledRecoveryJob,
            "Run all live recovery coordinators and retain evidence");

        policy.ShouldContain("\"schemaVersion\": \"2.1\"");
        policy.ShouldContain("\"repositoryIdentity\": \"Hexalith/Hexalith.ChatBot\"");
        policy.ShouldContain("\"maximumCurrentRunAgeMinutes\": 60");
        policy.ShouldContain("\"maximumLaneCurrentRunAgeMinutes\": 1440");
        policy.ShouldContain("\"maximumCurrentRunAgeMinutes\": 360");
        policy.ShouldContain("\"snapshot-plus-transition\"");
        policy.ShouldContain("\"scope_digest_mismatch\"");
        policy.ShouldContain("\"primary_path_not_executed\"");
        policy.ShouldContain("\"checked_item_evidence_mismatch\"");
        policy.ShouldContain("\"eventBaseHeadResolution\"");
        policy.ShouldContain("\"pullRequestHead\": \"github.event.pull_request.head.sha\"");
        policy.ShouldContain("\"zeroPushBaseFallback\": \"git rev-parse HEAD^\"");
        policy.ShouldContain("\"unavailableNonZeroPushBase\": \"fail\"");
        policy.ShouldContain("\"nonPushEventRange\": \"github.sha..github.sha\"");
        policy.ShouldNotContain("\"allowedLifecycleBookkeepingFields\"");
        policy.ShouldContain("\"immutableContentSource\": \"git-tree\"");
        policy.ShouldContain("\"worktreeModeSource\": \"git-index\"");
        policy.ShouldContain("\"recognizedLaneBindings\"");
        policy.ShouldContain("\"allowedLocatorSchemes\"");
        policy.ShouldContain(
            "\"selector\": \"class:Hexalith.ChatBot.UI.E2E.Tests.RealRenderCrossSurfaceE2ETests\"");
        policy.ShouldContain(
            "\"selector\": \"class:Hexalith.ChatBot.Server.Tests.Projections.ChatBotProjectConversationHubE2ETests\"");
        policy.ShouldContain(
            "\"selector\": \"class:Hexalith.ChatBot.UI.E2E.Tests.FrontComposerShellIntegrationE2ETests\"");
        policy.ShouldContain(
            "\"selector\": \"class:Hexalith.ChatBot.IntegrationTests.TrivialGovernedCommandAspireE2eTests\"");
        policy.ShouldContain(
            "\"selector\": \"class:Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests\"");
        policy.ShouldContain("\"trx\": \"recovery-primary/live-recovery-validation.trx\"");
        policy.ShouldContain(
            "\"provenance\": \"recovery-primary/live-recovery-validation.provenance.json\"");
        policy.ShouldContain("\".github/workflows/ci.yml\"");
        policy.ShouldContain("\".github/workflows/release.yml\"");
        policy.ShouldContain("\"src/Hexalith.ChatBot.AppHost/**\"");
        policy.ShouldContain(
            "\"tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs\"");
        policy.ShouldNotContain("\"**/*Dapr*.cs\"");
        policy.ShouldNotContain("\"**/*Aspire*.cs\"");
        policy.ShouldNotContain("Hexalith.ChatBot.BrowserPrimaryTests");
        policy.ShouldNotContain("Hexalith.ChatBot.SignalRPrimaryTests");
        policy.ShouldNotContain("Hexalith.ChatBot.HostingAssetsPrimaryTests");
        policy.ShouldNotContain("Hexalith.ChatBot.AspireDaprPrimaryTests");
        policy.ShouldNotContain("Hexalith.ChatBot.RecoveryPrimaryTests");
        browserPrimarySource.ShouldContain("namespace Hexalith.ChatBot.UI.E2E.Tests;");
        browserPrimarySource.ShouldContain("public sealed class RealRenderCrossSurfaceE2ETests");
        signalRPrimarySource.ShouldContain("namespace Hexalith.ChatBot.Server.Tests.Projections;");
        signalRPrimarySource.ShouldContain("public sealed class ChatBotProjectConversationHubE2ETests");
        hostingAssetsPrimarySource.ShouldContain("namespace Hexalith.ChatBot.UI.E2E.Tests;");
        hostingAssetsPrimarySource.ShouldContain("public sealed class FrontComposerShellIntegrationE2ETests");
        aspireDaprPrimarySource.ShouldContain("namespace Hexalith.ChatBot.IntegrationTests;");
        aspireDaprPrimarySource.ShouldContain("public sealed class TrivialGovernedCommandAspireE2eTests");
        recoveryPrimarySource.ShouldContain("namespace Hexalith.ChatBot.IntegrationTests.Recovery;");
        recoveryPrimarySource.ShouldContain("public sealed class LiveContinuityAspireE2eTests");
        workflow.ShouldContain("story-evidence-integrity:");
        workflow.ShouldContain("name: story-evidence-integrity");
        workflow.ShouldContain("needs: [build, topology-acceptance]\n    if: always()");
        workflow.ShouldContain("timeout-minutes: 360");
        workflow.ShouldContain("if: needs.build.result != 'success'");
        workflow.ShouldContain(
            "if: steps.artifacts.outputs.requires_topology == 'true' && needs.topology-acceptance.result != 'success'");
        workflow.ShouldContain("ref: ${{ github.event.pull_request.head.sha || github.sha }}");
        Regex.Matches(workflow, "ref: \\$\\{\\{ github\\.event\\.pull_request\\.head\\.sha \\|\\| github\\.sha \\}\\}")
            .Count.ShouldBeGreaterThanOrEqualTo(3);
        workflow.ShouldContain("actions: read");
        workflow.ShouldContain("bash .github/scripts/run-merge-test-lanes.sh");
        string mergeLaneScript = File.ReadAllText(Path.Combine(root, ".github", "scripts", "run-merge-test-lanes.sh"));
        mergeLaneScript.ShouldContain("declare -A seen_lanes=()");
        mergeLaneScript.ShouldContain("expected_lanes=\"${MERGE_TEST_EXPECTED_LANES:-13}\"");
        mergeLaneScript.ShouldContain("the merge lane set must match the build job exactly");
        mergeLaneScript.ShouldContain("Colliding merge test lane");
        mergeLaneScript.ShouldContain("run-xunit-v4.sh");
        workflow.ShouldContain("Non-zero push base %s is unavailable; refusing a one-commit fallback.");
        workflow.ShouldContain("base_sha=\"$head_sha\"");
        workflow.ShouldContain("Plan proposed completion production");
        workflow.ShouldContain("--configuration Release --no-build -- plan");
        workflow.ShouldContain("--output \"$PRODUCTION_PLAN_PATH\"");
        workflow.ShouldContain("Resolve transition-declared artifact requirements");
        workflow.ShouldContain(".requiresTopology");
        workflow.ShouldContain(".requiresRecovery");
        workflow.ShouldContain(".retainedLocators[]?");
        workflow.ShouldNotContain(".source == \"current-run\" and .lane == \"recovery-primary\"");
        workflow.ShouldContain("requires_topology=%s");
        workflow.ShouldContain("requires_recovery=%s");
        workflow.ShouldContain(
            "- name: Download transition-declared current topology primary result\n"
            + "        if: steps.artifacts.outputs.requires_topology == 'true'");
        workflow.ShouldContain("name: topology-acceptance-evidence");
        workflow.ShouldContain(
            "- name: Setup DAPR CLI for transition-declared current recovery primary\n"
            + "        if: steps.artifacts.outputs.requires_recovery == 'true'");
        workflow.ShouldContain(
            "- name: Produce transition-declared current recovery primary result\n"
            + "        id: recovery\n"
            + "        if: steps.artifacts.outputs.requires_recovery == 'true'");
        workflow.ShouldContain(
            "${{ runner.temp }}/raw-recovery-results/live-recovery-validation.ctrf.json");
        workflow.ShouldContain(
            "- name: Stop DAPR runtime for transition-declared current recovery primary\n"
            + "        if: always() && steps.artifacts.outputs.requires_recovery == 'true'");
        workflow.ShouldNotContain("HEXALITH_CHATBOT_RECOVERY_EVIDENCE_ARTIFACT: story-evidence-integrity-reports");
        completionProducer.ShouldContain("timeout-minutes: 285");
        completionProducer.ShouldContain("HEXALITH_CHATBOT_RECOVERY_WORKFLOW_TIMEOUT_MINUTES: \"250\"");
        completionProducer.ShouldContain("HEXALITH_CHATBOT_RECOVERY_EVIDENCE_ARTIFACT: completion-recovery-evidence");
        completionProducer.ShouldContain("bash .github/scripts/run-xunit-v4.sh");
        completionProducer.ShouldContain("XUNIT_REQUIRE_ZERO_SKIPS=1");
        completionSummary.ShouldContain("if: always() && steps.artifacts.outputs.requires_recovery == 'true'");
        completionSummary.ShouldContain("summarize-recovery-attempt");
        completionUpload.ShouldContain("if: always() && steps.artifacts.outputs.requires_recovery == 'true'");
        completionUpload.ShouldContain("name: completion-recovery-evidence");
        completionUpload.ShouldContain("path: TestResults");
        completionUpload.ShouldNotContain("path: |");
        Regex.Match(
                completionProducer,
                "HEXALITH_CHATBOT_RECOVERY_EVIDENCE_ARTIFACT: (?<name>[A-Za-z0-9_.-]+)")
            .Groups["name"].Value.ShouldBe(
                Regex.Match(completionUpload, "(?m)^          name: (?<name>[A-Za-z0-9_.-]+)")
                    .Groups["name"].Value);
        scheduledRecoveryProducer.ShouldContain("timeout-minutes: 300");
        scheduledRecoveryProducer.ShouldContain("HEXALITH_CHATBOT_RECOVERY_WORKFLOW_TIMEOUT_MINUTES: \"265\"");
        scheduledRecoveryProducer.ShouldContain("bash .github/scripts/run-xunit-v4.sh");
        scheduledRecoveryProducer.ShouldContain("XUNIT_REQUIRE_ZERO_SKIPS=1");
        workflow.ShouldContain("elapsed_seconds >= 2400");
        workflow.ShouldContain("job_start_epoch + (330 * 60)");
        workflow.ShouldContain("remaining_seconds - 900");
        workflow.ShouldContain("timeout --signal=INT --kill-after=15m");
        workflow.ShouldContain("dapr init --runtime-version 1.18.0");
        workflow.ShouldContain("actions/upload-artifact@v7");
        workflow.ShouldContain("actions/download-artifact@v8");
        workflow.ShouldContain(
            "- name: Setup checksum-pinned DAPR CLI\n"
            + "        run: bash .github/scripts/install-dapr-cli.sh");
        daprInstaller.ShouldContain("dapr_version=\"1.18.0\"");
        daprInstaller.ShouldContain("2a94739e0aa101289d88418225319562bc6800db273b3d9cf819a0efd1ea1bfe");
        daprInstaller.ShouldContain("sha256sum --check --strict");
        workflow.ShouldContain("sanitize-recovery-trx");
        workflow.ShouldContain(
            "--output \"${{ runner.temp }}/machine-results/recovery-primary/live-recovery-validation.trx\"");
        workflow.ShouldContain(
            "-method \"Hexalith.ChatBot.IntegrationTests.TrivialGovernedCommandAspireE2eTests."
            + "TrivialGovernedCommandShouldFlowEndToEndThroughTheRealDaprTopology");
        workflow.ShouldContain(
            "-method \"Hexalith.ChatBot.IntegrationTests.Story132ProductionBrowserAspireE2ETests."
            + "AuthenticatedProductionClientShouldMessageAskAndStopAcrossRequiredChromeMatrix");
        workflow.ShouldContain("TestResults/topology-acceptance.ctrf.json");
        workflow.ShouldContain("TestResults/story132-topology-acceptance.ctrf.json");
        // Retain the consumer-facing TRX counter guard in addition to the xUnit v4 boundary's CTRF validation.
        workflow.ShouldContain("Require one executed topology acceptance test and zero skips");
        workflow.ShouldContain("E.parse('TestResults/topology-acceptance.trx')");
        workflow.ShouldContain("Require one executed Story 13.2 test and zero skips");
        workflow.ShouldContain("E.parse('TestResults/story132-topology-acceptance.trx')");
        workflow.ShouldContain("c.get('notExecuted') == '0'");
        releaseWorkflow.ShouldContain(
            "-method \"Hexalith.ChatBot.IntegrationTests.TrivialGovernedCommandAspireE2eTests."
            + "TrivialGovernedCommandShouldFlowEndToEndThroughTheRealDaprTopology");
        releaseWorkflow.ShouldContain(
            "-method \"Hexalith.ChatBot.IntegrationTests.Story132ProductionBrowserAspireE2ETests."
            + "AuthenticatedProductionClientShouldMessageAskAndStopAcrossRequiredChromeMatrix");
        releaseWorkflow.ShouldContain("TestResults/topology-acceptance.ctrf.json");
        releaseWorkflow.ShouldContain("TestResults/story132-topology-acceptance.ctrf.json");
        releaseWorkflow.ShouldContain("Require one executed topology acceptance test and zero skips");
        releaseWorkflow.ShouldContain("E.parse('TestResults/topology-acceptance.trx')");
        releaseWorkflow.ShouldContain("Require one executed Story 13.2 test and zero skips");
        releaseWorkflow.ShouldContain("E.parse('TestResults/story132-topology-acceptance.trx')");
        releaseWorkflow.ShouldContain("c.get('notExecuted') == '0'");
        releaseWorkflow.ShouldContain(
            "semantic-release:\n"
            + "    needs:\n"
            + "      - topology-acceptance\n"
            + "      - live-recovery-evidence-gate");
        workflow.ShouldContain(
            "-method \"Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests."
            + "LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate");
        workflow.ShouldContain("live-recovery-validation.ctrf.json");
        releaseWorkflow.ShouldContain(
            "-method \"Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests."
            + "LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate");
        releaseWorkflow.ShouldContain("live-recovery-validation.ctrf.json");
        workflow.ShouldContain("Collect transition-declared retained exact-run artifacts");
        workflow.ShouldContain("done < \"$RETAINED_LOCATORS_PATH\"");
        workflow.ShouldNotContain("_bmad-output/implementation-artifacts/evidence/*.json | sort -u");
        workflow.ShouldContain("gh run download \"$run_id\"");
        workflow.ShouldContain("github-actions://([A-Za-z0-9_.-]+)");
        workflow.ShouldContain("if [[ \"$repository\" != \"$GITHUB_REPOSITORY\" ]]; then");
        workflow.ShouldContain("Verify general producer artifact binds the exact event head");
        workflow.ShouldContain(
            "- name: Verify topology producer artifact binds the exact event head when transition-declared\n"
            + "        if: steps.artifacts.outputs.requires_topology == 'true'");
        int resolveBoundsIndex = workflow.IndexOf("Resolve exact transition bounds", StringComparison.Ordinal);
        int detectTransitionsIndex = workflow.IndexOf("Plan proposed completion production", StringComparison.Ordinal);
        int collectArtifactsIndex = workflow.IndexOf(
            "Collect transition-declared retained exact-run artifacts",
            StringComparison.Ordinal);
        int produceCurrentRecoveryIndex = workflow.IndexOf(
            "Produce transition-declared current recovery primary result",
            StringComparison.Ordinal);
        int stopCurrentRecoveryIndex = workflow.IndexOf(
            "Stop DAPR runtime for transition-declared current recovery primary",
            StringComparison.Ordinal);
        int sanitizeCurrentRecoveryIndex = workflow.IndexOf(
            "Project recovery result into metadata-only completion TRX",
            StringComparison.Ordinal);
        int attestTransitionsIndex = workflow.IndexOf(
            "Attest and evaluate proposed completion transitions",
            StringComparison.Ordinal);
        resolveBoundsIndex.ShouldBeGreaterThanOrEqualTo(0);
        detectTransitionsIndex.ShouldBeGreaterThanOrEqualTo(0);
        collectArtifactsIndex.ShouldBeGreaterThanOrEqualTo(0);
        produceCurrentRecoveryIndex.ShouldBeGreaterThanOrEqualTo(0);
        stopCurrentRecoveryIndex.ShouldBeGreaterThanOrEqualTo(0);
        sanitizeCurrentRecoveryIndex.ShouldBeGreaterThanOrEqualTo(0);
        attestTransitionsIndex.ShouldBeGreaterThanOrEqualTo(0);
        resolveBoundsIndex.ShouldBeLessThan(detectTransitionsIndex);
        detectTransitionsIndex.ShouldBeLessThan(produceCurrentRecoveryIndex);
        produceCurrentRecoveryIndex.ShouldBeLessThan(stopCurrentRecoveryIndex);
        stopCurrentRecoveryIndex.ShouldBeLessThan(sanitizeCurrentRecoveryIndex);
        sanitizeCurrentRecoveryIndex.ShouldBeLessThan(collectArtifactsIndex);
        collectArtifactsIndex.ShouldBeLessThan(attestTransitionsIndex);
        Regex.Matches(workflow, "producer-head\\.sha").Count.ShouldBeGreaterThanOrEqualTo(4);
        toolProgram.ShouldContain("GITHUB_STEP_SUMMARY");
        toolProgram.ShouldContain("\"ci\" => RunCi");
        lifecycleValidator.ShouldContain("snapshot-plus-transition");
        lifecycleValidator.ShouldContain("lifecycle-event-paths");
        lifecycleValidator.ShouldContain("status: 'in-review'");
        trxReader.ShouldContain("http://microsoft.com/schemas/VisualStudio/TeamTest/2010");
        trxReader.ShouldContain("RejectForeignStructuralElements");
        attestor.ShouldContain("PreflightContract");
        toolProgram.ShouldContain("result-path-collision");
        workflow.ShouldContain("fetch-depth: 0");
        workflow.ShouldContain("StoryEvidenceGate.Tests");
        workflow.ShouldNotContain("git submodule update --init --recursive");
        toolProject.ShouldNotContain("PackageReference");
    }

    [Fact]
    public static void WorkflowsAndToolsShouldNotUseRecursiveSubmoduleCommands()
    {
        string root = RepositoryRoot();
        IEnumerable<string> files = Directory.EnumerateFiles(Path.Combine(root, ".github", "workflows"), "*", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "tests", "tools"), "*", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "tools"), "*.cs", SearchOption.AllDirectories));

        Regex forbidden = new(@"git\s+submodule\s+(?:update|foreach)\b[^\r\n]*--recursive", RegexOptions.IgnoreCase);
        string[] violations = files
            .Where(static file => !file.EndsWith(".gitkeep", StringComparison.Ordinal))
            .Where(file => forbidden.IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        violations.ShouldBeEmpty();
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

    private static string WorkflowJob(string source, string name)
    {
        Match match = Regex.Match(
            source,
            $"(?ms)^  {Regex.Escape(name)}:.*?(?=^  \\S|\\z)");
        match.Success.ShouldBeTrue($"workflow job '{name}' must exist");
        return match.Value;
    }

    private static string WorkflowBlock(string source, string name, string nextName)
    {
        Match match = Regex.Match(
            source,
            $"(?ms)^  {Regex.Escape(name)}:.*?(?=^  {Regex.Escape(nextName)}:)");
        match.Success.ShouldBeTrue($"workflow job '{name}' must exist before '{nextName}'");
        return match.Value;
    }

    private static string WorkflowStep(string job, string name)
    {
        Match match = Regex.Match(
            job,
            $"(?ms)^      - name: {Regex.Escape(name)}\\n.*?(?=^      - name: |\\z)");
        match.Success.ShouldBeTrue($"workflow step '{name}' must exist in its owning job");
        return match.Value;
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
