using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

public static class CorrectionPropagationWorkflowConformanceTests
{
    [Fact]
    public static void ProductionStoryMustRegisterHostedDaprWorkflowRuntime()
    {
        string gateway = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.Server",
            "Gateway",
            "CommandGatewayServiceCollectionExtensions.cs"));
        string program = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.Server",
            "Program.cs"));

        gateway.ShouldContain("AddDaprWorkflow");
        gateway.ShouldContain("RegisterWorkflow<CorrectionPropagationWorkflow>");
        gateway.ShouldContain("RegisterActivity<CorrectionPropagationRunStoreActivity>");
        program.ShouldContain("UseDaprWorkflowRuntime");
    }

    [Fact]
    public static void ContractsUiCliAndMcpMustNotReferenceDaprWorkflow()
    {
        string[] projectRoots =
        [
            "src/Hexalith.ChatBot.Contracts",
            "src/Hexalith.ChatBot.UI",
            "src/Hexalith.ChatBot.Cli",
            "src/Hexalith.ChatBot.Mcp",
        ];

        foreach (string projectRoot in projectRoots)
        {
            string fullRoot = Path.Combine(RepositoryRoot(), projectRoot);
            foreach (string file in Directory.EnumerateFiles(fullRoot, "*.*", SearchOption.AllDirectories)
                .Where(static file => file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".csproj", StringComparison.Ordinal)))
            {
                string source = File.ReadAllText(file);
                source.Contains("Dapr.Workflow", StringComparison.Ordinal).ShouldBeFalse(file);
                source.Contains("DaprWorkflowClient", StringComparison.Ordinal).ShouldBeFalse(file);
            }
        }
    }

    [Fact]
    public static void WorkflowRuntimeMustNotDirectlyMutateSiblingBoundedContexts()
    {
        string workflowRoot = Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.Server",
            "Lifecycle",
            "Workflows");
        string[] forbidden =
        [
            "Hexalith.Projects.Client",
            "Hexalith.Conversations.Client",
            "Hexalith.Folders.Client",
            "Hexalith.Memories",
            "Hexalith.EventStore.Server",
        ];

        foreach (string file in Directory.EnumerateFiles(workflowRoot, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(file);
            foreach (string token in forbidden)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(file);
            }
        }
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
