using System.Reflection;

using Hexalith.ChatBot.Testing.Fixtures;

namespace Hexalith.ChatBot.Conformance.Tests.Harness;

/// <summary>
/// Story 1.13 sandbox harness over the shared tenant-scoped evaluation manifest. Pure manifest channels stay as
/// scaffold records; command-execution cases drive the existing governed-command conformance gateway lane.
/// </summary>
internal static class TenantScopedFixtureHarness
{
    /// <summary>Loads the embedded Story 1.13 fixture manifest from this test assembly.</summary>
    /// <returns>The validated fixture dataset.</returns>
    public static TenantScopedEvaluationDataset LoadDataset()
        => TenantScopedFixtureManifestLoader.LoadFromEmbeddedResource(Assembly.GetExecutingAssembly());

    /// <summary>Returns command-execution cases that can run through the in-process gateway sandbox.</summary>
    /// <param name="dataset">The validated fixture dataset.</param>
    /// <returns>The command-execution cases.</returns>
    public static IReadOnlyList<TenantScopedFixtureCase> CommandExecutionCases(TenantScopedEvaluationDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        return dataset.Cases
            .Where(static fixtureCase => fixtureCase.WorkflowChannels.Contains("command-execution", StringComparer.Ordinal))
            .ToArray();
    }

    /// <summary>Runs a command-execution fixture through the existing conformance gateway lane.</summary>
    /// <param name="fixtureCase">The command-execution fixture case.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The captured arm outcome.</returns>
    public static async Task<ArmOutcome> RunCommandExecutionFixtureAsync(
        TenantScopedFixtureCase fixtureCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fixtureCase);

        // The in-process governed-command sandbox binds the shared own tenant (Story 1.12 BoundTenant). A fixture case
        // scoped to any other tenant must NOT silently execute under the bound tenant — that would mask a tenant-scope
        // divergence in exactly the lane AC3/AC5 guard. Fail closed (metadata-only) until the lane is tenant-parameterized.
        if (!string.Equals(fixtureCase.TenantId, CrossTenantLeakageCorpus.BoundTenant, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Command-execution fixture '{fixtureCase.CaseId}' declares a tenant the in-process sandbox does not bind; refusing to run it under the bound tenant.");
        }

        TenantScopedFixtureResource noteResource = fixtureCase.TenantOwnedResources.Single(static resource =>
            string.Equals(resource.ResourceType, "governed-note", StringComparison.Ordinal));
        string noteId = noteResource.ResourceId[(noteResource.ResourceId.LastIndexOf(':') + 1)..];

        return await GovernedCommandConformanceHarness
            .RunSuccessAsync(new UiApiSurfaceArm(), new SemanticIntent(noteId), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Serializes the tenant/resource-bearing surface of a case (the only fields that can carry a tenant or
    /// resource-id sentinel) so the shared leakage scanner can actually catch a foreign token smuggled into an
    /// own-tenant case. A projection that strips these fields would make the scan vacuous.
    /// </summary>
    /// <param name="fixtureCase">The fixture case.</param>
    /// <returns>A JSON projection of the case's tenant scope.</returns>
    public static string SerializeCaseScope(TenantScopedFixtureCase fixtureCase)
    {
        ArgumentNullException.ThrowIfNull(fixtureCase);

        return TenantScopedFixtureManifestLoader.SerializeMetadata(new
        {
            fixtureCase.CaseId,
            fixtureCase.TenantId,
            fixtureCase.TenantOwnedResources,
        });
    }

    /// <summary>
    /// The tenant tokens a case may legitimately render: its own tenant plus every declared resource tenant. Only
    /// these are excluded from the leakage scan, so a foreign tenant token that is NOT declared (e.g. smuggled into a
    /// resourceId string of an own-tenant case) still trips the scanner.
    /// </summary>
    /// <param name="fixtureCase">The fixture case.</param>
    /// <returns>The set of tenant tokens the case legitimately declares.</returns>
    public static IReadOnlyCollection<string> DeclaredTenants(TenantScopedFixtureCase fixtureCase)
    {
        ArgumentNullException.ThrowIfNull(fixtureCase);

        HashSet<string> tenants = new(StringComparer.Ordinal) { fixtureCase.TenantId };
        foreach (TenantScopedFixtureResource resource in fixtureCase.TenantOwnedResources)
        {
            tenants.Add(resource.TenantId);
        }

        return tenants;
    }
}
