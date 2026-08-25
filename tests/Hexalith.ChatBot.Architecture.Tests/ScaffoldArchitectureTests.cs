using System.Text.RegularExpressions;
using System.Xml.Linq;

using Hexalith.ChatBot.Tests;

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
            "tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj",
            "tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj",
        ];

        foreach (string project in expected)
        {
            projects.ShouldContain(project);
        }

        projects.ShouldNotContain("src/Hexalith.ChatBot.Aspire/Hexalith.ChatBot.Aspire.csproj");
        projects.ShouldNotContain("src/Hexalith.ChatBot.ServiceDefaults/Hexalith.ChatBot.ServiceDefaults.csproj");
        projects.ShouldNotContain("tests/Hexalith.ChatBot.Aspire.Tests/Hexalith.ChatBot.Aspire.Tests.csproj");
        projects.ShouldNotContain("tests/Hexalith.ChatBot.ServiceDefaults.Tests/Hexalith.ChatBot.ServiceDefaults.Tests.csproj");
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
            .Where(static file => !file.EndsWith(Path.Combine("Commands", "TenantExportContracts.cs"), StringComparison.Ordinal))
            // Story 9.9: DeletionErasureClassStatuses is a bounded, AC4-mandated deletion-status token set (succeeded /
            // failed-retryable / failed-terminal) — the same compliance domain as TenantExportContracts, not the legacy
            // lifecycle enum. It legitimately owns the "succeeded" token exactly like the status enums above.
            .Where(static file => !file.EndsWith(Path.Combine("Commands", "DeletionErasureContracts.cs"), StringComparison.Ordinal))
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
    public static void RootConfigurationShouldPinSdkTargetFrameworkAndCentralPackages()
    {
        string root = RepositoryRoot();

        File.ReadAllText(Path.Combine(root, "global.json")).ShouldContain("\"version\": \"10.0.302\"");
        File.ReadAllText(Path.Combine(root, "global.json")).ShouldContain("\"rollForward\": \"latestPatch\"");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<TargetFramework>net10.0</TargetFramework>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<Nullable>enable</Nullable>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<ImplicitUsings>enable</ImplicitUsings>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>");
        File.ReadAllText(Path.Combine(root, "Directory.Build.props")).ShouldContain("<Deterministic>true</Deterministic>");
        PackageCatalogTestHelper.AssertExclusiveAuthority();
    }

    [Fact]
    public static void RootSubmoduleDeclarationsShouldRemainUnderReferencesAndUnique()
    {
        string modules = File.ReadAllText(Path.Combine(RepositoryRoot(), ".gitmodules"));

        Regex.Matches(modules, "path = references/Hexalith.EventStore").Count.ShouldBe(1);
        modules.ShouldContain("path = references/Hexalith.Tenants");
        modules.ShouldContain("path = references/Hexalith.FrontComposer");
        modules.ShouldNotContain("path = Hexalith.EventStore");
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

        policy.ShouldContain("\"schemaVersion\": \"2.1\"");
        policy.ShouldContain("\"repositoryIdentity\": \"Hexalith/Hexalith.ChatBot\"");
        policy.ShouldContain("\"maximumCurrentRunAgeMinutes\": 60");
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
        workflow.ShouldContain("declare -A seen_lanes=()");
        workflow.ShouldContain("find tests -type f -name '*.csproj'");
        workflow.ShouldContain("No test projects were discovered under tests/");
        workflow.ShouldContain("Colliding test lane");
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
            "--results-directory \"${{ runner.temp }}/raw-recovery-results\"");
        workflow.ShouldContain(
            "- name: Stop DAPR runtime for transition-declared current recovery primary\n"
            + "        if: always() && steps.artifacts.outputs.requires_recovery == 'true'");
        workflow.ShouldContain("HEXALITH_CHATBOT_RECOVERY_WORKFLOW_TIMEOUT_MINUTES: \"250\"");
        workflow.ShouldNotContain("HEXALITH_CHATBOT_RECOVERY_EVIDENCE_ARTIFACT: story-evidence-integrity-reports");
        workflow.ShouldContain("HEXALITH_CHATBOT_RECOVERY_EVIDENCE_ARTIFACT: completion-recovery-evidence");
        workflow.ShouldContain("summarize-recovery-attempt");
        workflow.ShouldContain("No test lanes executed; refusing to report a green required job.");
        workflow.ShouldContain("timeout-minutes: 285");
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
            "FullyQualifiedName=Hexalith.ChatBot.IntegrationTests.TrivialGovernedCommandAspireE2eTests."
            + "TrivialGovernedCommandShouldFlowEndToEndThroughTheRealDaprTopology");
        workflow.ShouldContain("trx;LogFileName=topology-acceptance.trx");
        releaseWorkflow.ShouldContain(
            "FullyQualifiedName=Hexalith.ChatBot.IntegrationTests.TrivialGovernedCommandAspireE2eTests."
            + "TrivialGovernedCommandShouldFlowEndToEndThroughTheRealDaprTopology");
        releaseWorkflow.ShouldContain("trx;LogFileName=topology-acceptance.trx");
        releaseWorkflow.ShouldContain(
            "semantic-release:\n"
            + "    needs:\n"
            + "      - topology-acceptance\n"
            + "      - live-recovery-evidence-gate");
        workflow.ShouldContain(
            "FullyQualifiedName=Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests."
            + "LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate");
        workflow.ShouldContain("trx;LogFileName=live-recovery-validation.trx");
        releaseWorkflow.ShouldContain(
            "FullyQualifiedName=Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests."
            + "LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate");
        releaseWorkflow.ShouldContain("trx;LogFileName=live-recovery-validation.trx");
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
