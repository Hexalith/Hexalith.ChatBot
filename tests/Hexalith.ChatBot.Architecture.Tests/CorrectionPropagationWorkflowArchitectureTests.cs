using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests;

public static class CorrectionPropagationWorkflowArchitectureTests
{
    [Fact]
    public static void DaprWorkflowTypesStayInsideServerWorkflowRuntimeLayer()
    {
        string root = RepositoryRoot();
        string[] allowedRoots =
        [
            Path.Combine(root, "src", "Hexalith.ChatBot.Server", "Lifecycle", "Workflows"),
            Path.Combine(root, "src", "Hexalith.ChatBot.Server", "Gateway"),
        ];

        foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(file);
            if (!source.Contains("Dapr.Workflow", StringComparison.Ordinal) &&
                !source.Contains("DaprWorkflowClient", StringComparison.Ordinal))
            {
                continue;
            }

            allowedRoots.Any(allowed => file.StartsWith(allowed, StringComparison.Ordinal)).ShouldBeTrue(file);
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
