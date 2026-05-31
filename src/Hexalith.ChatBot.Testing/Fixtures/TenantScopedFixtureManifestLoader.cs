using System.Reflection;
using System.Text.Json;

namespace Hexalith.ChatBot.Testing.Fixtures;

/// <summary>
/// Loads embedded tenant-scoped fixture manifests and fails closed when the resource is absent.
/// </summary>
public static class TenantScopedFixtureManifestLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Loads and validates the Story 1.13 manifest from an assembly embedded resource.
    /// </summary>
    /// <param name="assembly">The assembly containing the embedded resource.</param>
    /// <param name="resourceName">The resource logical name.</param>
    /// <returns>The validated tenant-scoped evaluation dataset.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the resource is missing or empty.</exception>
    /// <exception cref="TenantScopedFixtureValidationException">Thrown when validation fails.</exception>
    public static TenantScopedEvaluationDataset LoadFromEmbeddedResource(
        Assembly assembly,
        string resourceName = TenantScopedFixtureConstants.ResourceName)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded tenant-scoped fixture manifest '{resourceName}' was not found. The fixture gate cannot run without the manifest resource.");
        }

        return Load(stream, resourceName);
    }

    /// <summary>
    /// Loads and validates a tenant-scoped fixture manifest stream.
    /// </summary>
    /// <param name="stream">The manifest stream.</param>
    /// <param name="resourceName">The manifest resource name used in diagnostics.</param>
    /// <returns>The validated tenant-scoped evaluation dataset.</returns>
    public static TenantScopedEvaluationDataset Load(Stream stream, string resourceName)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        TenantScopedEvaluationDataset? dataset = JsonSerializer.Deserialize<TenantScopedEvaluationDataset>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Tenant-scoped fixture manifest '{resourceName}' deserialized to null.");

        TenantScopedFixtureValidator.Validate(dataset);
        return dataset;
    }

    /// <summary>
    /// Serializes fixture metadata for scanner or diagnostics tests.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>JSON serialized with web options.</returns>
    public static string SerializeMetadata(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.Serialize(value, JsonOptions);
    }
}
