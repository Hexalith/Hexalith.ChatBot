using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Proves sandbox composition uses the owning content root regardless of AppHost placement.</summary>
public sealed class RecoverySandboxProjectPathTests
{
    [Theory]
    [InlineData("")]
    [InlineData("src/Hexalith.ChatBot.AppHost")]
    [InlineData("moved/hosts/local/deep/AppHost")]
    public void ProjectResolutionFollowsTheContentRoot(string appHostRelativePath)
    {
        string root = Path.Combine(Path.GetTempPath(), $"recovery-project-path-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "Hexalith.ChatBot.slnx"), "<Solution />");
            string project = SandboxPath(root);
            Directory.CreateDirectory(Path.GetDirectoryName(project)!);
            File.WriteAllText(project, "<Project />");
            string appHost = Path.Combine(root, appHostRelativePath);
            Directory.CreateDirectory(appHost);

            RecoverySandboxTopologyComposer.ResolveProjectPath(appHost).ShouldBe(project);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProjectResolutionRefusesMissingRootOrProject(bool hasRootMarker)
    {
        string root = Path.Combine(Path.GetTempPath(), $"recovery-project-path-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            if (hasRootMarker)
            {
                File.WriteAllText(Path.Combine(root, "Hexalith.ChatBot.slnx"), "<Solution />");
            }
            else
            {
                // A coincidental project directory is insufficient without the owning repository marker.
                Directory.CreateDirectory(Path.GetDirectoryName(SandboxPath(root))!);
                File.WriteAllText(SandboxPath(root), "<Project />");
            }

            Should.Throw<InvalidOperationException>(() => RecoverySandboxTopologyComposer.ResolveProjectPath(root))
                .Message.ShouldBe(hasRootMarker
                    ? "The ChatBot content root has no recovery sandbox project."
                    : "The AppHost is outside a ChatBot content root.");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void MissingSandboxAtTheNearestRootCannotSelectAnotherCheckout()
    {
        string outer = Path.Combine(Path.GetTempPath(), $"recovery-project-path-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SandboxPath(outer))!);
            File.WriteAllText(Path.Combine(outer, "Hexalith.ChatBot.slnx"), "<Solution />");
            File.WriteAllText(SandboxPath(outer), "<Project />");
            string inner = Path.Combine(outer, "nested-checkout");
            string appHost = Path.Combine(inner, "moved", "AppHost");
            Directory.CreateDirectory(appHost);
            File.WriteAllText(Path.Combine(inner, "Hexalith.ChatBot.slnx"), "<Solution />");

            Should.Throw<InvalidOperationException>(() => RecoverySandboxTopologyComposer.ResolveProjectPath(appHost))
                .Message.ShouldBe("The ChatBot content root has no recovery sandbox project.");
        }
        finally
        {
            if (Directory.Exists(outer))
            {
                Directory.Delete(outer, recursive: true);
            }
        }
    }

    private static string SandboxPath(string root)
        => Path.Combine(root, "tests", "Hexalith.ChatBot.RecoverySandbox", "Hexalith.ChatBot.RecoverySandbox.csproj");
}
