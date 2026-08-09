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
            Path.Combine(root, "src", "Hexalith.ChatBot.Server", "Gateway", "CommandGatewayServiceCollectionExtensions.cs"),
        ];

        foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(file);
            if (!source.Contains("Dapr.Workflow", StringComparison.Ordinal) &&
                !source.Contains("DaprWorkflowClient", StringComparison.Ordinal))
            {
                continue;
            }

            bool allowed = allowedRoots.Any(allowed =>
                file.Equals(allowed, StringComparison.Ordinal) ||
                file.StartsWith(allowed + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                file.StartsWith(allowed + Path.AltDirectorySeparatorChar, StringComparison.Ordinal));
            allowed.ShouldBeTrue(file);
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
