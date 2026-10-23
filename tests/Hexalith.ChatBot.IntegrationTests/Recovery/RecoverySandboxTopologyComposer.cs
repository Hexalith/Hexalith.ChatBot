using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Composes the fault-capable recovery sandbox only into an opted-in integration-test resource model.</summary>
internal static class RecoverySandboxTopologyComposer
{
    /// <summary>Adds the closed recovery sandbox after validating the test-only capability boundary.</summary>
    public static IResourceBuilder<ProjectResource> AddRecoverySandbox(
        this IDistributedApplicationTestingBuilder builder,
        string environmentName,
        string tenantRef,
        string storageTenantRef,
        string controllerCapability,
        string controllerSecret,
        string providerMessageId)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // The storage tenant is DERIVED from the guarded logical tenant, not accepted alongside it. Previously the
        // replay-test: predicate covered only the logical label while every durable write went to an independently
        // supplied physical name whose sole protection was a tenant-alpha/tenant-beta exclusion list — which is
        // exactly the second test-tenant predicate Task 2 forbids. Now one predicate governs both.
        string? derivedStorageTenant = ReplayTenantPolicy.StorageTenantFor(tenantRef);
        if ((!string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase)) ||
            !string.Equals(controllerCapability, LiveRecoveryValidationOptions.AspireControllerCapability, StringComparison.Ordinal) ||
            !ReplayTenantPolicy.IsTestTenant(tenantRef) ||
            derivedStorageTenant is null ||
            !string.Equals(storageTenantRef, derivedStorageTenant, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(controllerSecret) ||
            string.IsNullOrWhiteSpace(providerMessageId))
        {
            throw new InvalidOperationException("The test-only live-recovery controller capability, tenant, or secret is invalid.");
        }

        // A distinct concern from tenant classification: the topology's own control tenants must never be the target
        // of fault injection, even if someone declares a logical tenant that derives to one of their names.
        if (string.Equals(storageTenantRef, "tenant-alpha", StringComparison.Ordinal) ||
            string.Equals(storageTenantRef, RecoveryValidationTopology.ControlTenantRef, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The live-recovery sandbox must never target a topology control tenant.");
        }

        IResourceBuilder<ProjectResource> chatBot = builder.Resources
            .OfType<ProjectResource>()
            .Where(resource => string.Equals(resource.Name, "chatbot", StringComparison.Ordinal))
            .Select(builder.CreateResourceBuilder)
            .Single();
        string projectPath = ResolveProjectPath(builder.AppHostDirectory);
        return builder
            .AddProject("recovery-sandbox", projectPath)
            .WithReference(chatBot)
            .WaitFor(chatBot)
            .WithEnvironment("Recovery__Enabled", "true")
            .WithEnvironment("Recovery__TenantRef", tenantRef)
            .WithEnvironment("Recovery__StorageTenantRef", storageTenantRef)
            .WithEnvironment("Recovery__ControllerSecret", controllerSecret)
            .WithEnvironment("Recovery__ProviderMessageId", providerMessageId)
            .WithEnvironment("Recovery__ChatBotBaseAddress", chatBot.GetEndpoint("http"))
            .WithHttpHealthCheck("/health");
    }

    /// <summary>Finds the sandbox under the owning content root, independent of AppHost depth or working directory.</summary>
    /// <param name="appHostDirectory">The selected AppHost's content directory.</param>
    /// <returns>The existing sandbox project path.</returns>
    internal static string ResolveProjectPath(string appHostDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appHostDirectory);
        for (DirectoryInfo? directory = new(Path.GetFullPath(appHostDirectory)); directory is not null; directory = directory.Parent)
        {
            if (!File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
            {
                continue;
            }

            string projectPath = Path.Combine(
                directory.FullName, "tests", "Hexalith.ChatBot.RecoverySandbox", "Hexalith.ChatBot.RecoverySandbox.csproj");
            return File.Exists(projectPath)
                ? projectPath
                : throw new InvalidOperationException("The ChatBot content root has no recovery sandbox project.");
        }

        throw new InvalidOperationException("The AppHost is outside a ChatBot content root.");
    }
}
