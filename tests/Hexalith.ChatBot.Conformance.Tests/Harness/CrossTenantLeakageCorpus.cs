using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Conformance.Tests.Harness;

/// <summary>
/// One sentinel the cross-tenant leakage scanner hunts for. <paramref name="Channel"/> is the sentinel CLASS
/// (tenant, resource-id, candidate, evidence, file, cursor, path-fragment, provider-snippet, exception-text,
/// error-body) named in a leak diagnostic; <paramref name="Value"/> is the literal token that must never appear
/// in a tenant-alpha-rendered artifact.
/// </summary>
/// <param name="Channel">The sentinel class.</param>
/// <param name="Value">The literal sentinel token.</param>
/// <param name="Description">Why this token is a leak if rendered.</param>
internal sealed record LeakageSentinel(string Channel, string Value, string Description);

/// <summary>
/// The Story 1.12 cross-tenant leakage corpus, loaded from the embedded
/// <c>story-1-12-cross-tenant-leakage-corpus.json</c> shared fixture. Single source of truth for the tenant
/// boundary fixtures and the sentinel tokens. Current M0 only has command/status/audit/projection surfaces, but
/// the candidate/evidence/file/cursor channels exist here NOW so future Epic 2/3 endpoints plug into the same
/// gate (the leakage scanner) instead of inventing a parallel test style. Loaded from an embedded manifest
/// resource (never copy-to-output) so the gate fails closed rather than vacuously passing on a missing file.
/// </summary>
internal static class CrossTenantLeakageCorpus
{
    /// <summary>The embedded manifest resource name (the csproj sets this exact LogicalName).</summary>
    public const string ResourceName = "story-1-12-cross-tenant-leakage-corpus.json";

    private static readonly CorpusData Data = Load();

    /// <summary>The corpus schema version token.</summary>
    public static string SchemaVersion => Data.SchemaVersion;

    /// <summary>The caller's own (bound) tenant.</summary>
    public static string BoundTenant => Data.BoundTenant;

    /// <summary>The foreign tenant a bound caller must never reach.</summary>
    public static string ForeignTenant => Data.ForeignTenant;

    /// <summary>A foreign-tenant governed-note id used to seed the negative read cases.</summary>
    public static string ForeignNoteId => Data.Identifiers.ForeignNoteId;

    /// <summary>A foreign-tenant operation id used to seed the negative read cases.</summary>
    public static string ForeignOperationId => Data.Identifiers.ForeignOperationId;

    /// <summary>The caller's own governed-note id used for the positive control.</summary>
    public static string OwnNoteId => Data.Identifiers.OwnNoteId;

    /// <summary>The caller's own operation id used for the positive control.</summary>
    public static string OwnOperationId => Data.Identifiers.OwnOperationId;

    /// <summary>A well-formed but never-seeded id used for the unknown-id collapse case.</summary>
    public static string UnknownId => Data.Identifiers.UnknownId;

    /// <summary>The required leakage channels every persona case must keep covered (non-vacuity).</summary>
    public static IReadOnlyList<string> RequiredChannels => Data.RequiredChannels;

    /// <summary>Every sentinel token across all channels.</summary>
    public static IReadOnlyList<LeakageSentinel> Sentinels => Data.Sentinels;

    /// <summary>Returns the first sentinel value for a channel (throws if the channel is absent).</summary>
    /// <param name="channel">The sentinel class.</param>
    /// <returns>The first sentinel token in that channel.</returns>
    public static string Sentinel(string channel)
        => Sentinels.FirstOrDefault(sentinel => string.Equals(sentinel.Channel, channel, StringComparison.Ordinal))?.Value
            ?? throw new InvalidOperationException($"Leakage corpus has no sentinel for channel '{channel}'.");

    /// <summary>Returns the full sentinel set minus the supplied tokens (used when an own-tenant id legitimately appears).</summary>
    /// <param name="values">Tokens to exclude.</param>
    /// <returns>The remaining sentinels.</returns>
    public static IReadOnlyList<LeakageSentinel> SentinelsExcluding(params string[] values)
    {
        HashSet<string> excluded = new(values, StringComparer.Ordinal);
        return Sentinels.Where(sentinel => !excluded.Contains(sentinel.Value)).ToArray();
    }

    private static CorpusData Load()
    {
        using Stream? stream = typeof(CrossTenantLeakageCorpus).Assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded leakage corpus '{ResourceName}' was not found. The cross-tenant isolation gate cannot run without its sentinel corpus.");
        }

        CorpusData? data = JsonSerializer.Deserialize<CorpusData>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"Embedded leakage corpus '{ResourceName}' deserialized to null.");
        Validate(data);
        return data;
    }

    private static void Validate(CorpusData data)
    {
        if (string.IsNullOrWhiteSpace(data.SchemaVersion)
            || string.IsNullOrWhiteSpace(data.BoundTenant)
            || string.IsNullOrWhiteSpace(data.ForeignTenant)
            || string.Equals(data.BoundTenant, data.ForeignTenant, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Leakage corpus header (schemaVersion/boundTenant/foreignTenant) is incomplete or degenerate.");
        }

        foreach (string identifier in new[]
                 {
                     data.Identifiers.ForeignNoteId,
                     data.Identifiers.ForeignOperationId,
                     data.Identifiers.OwnNoteId,
                     data.Identifiers.OwnOperationId,
                     data.Identifiers.UnknownId,
                 })
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new InvalidOperationException("Leakage corpus identifiers must all be present.");
            }
        }

        if (data.RequiredChannels.Count == 0 || data.Sentinels.Count == 0)
        {
            throw new InvalidOperationException("Leakage corpus must declare required channels and a non-empty sentinel set.");
        }

        if (data.Sentinels.Any(sentinel => string.IsNullOrWhiteSpace(sentinel.Channel) || string.IsNullOrWhiteSpace(sentinel.Value)))
        {
            throw new InvalidOperationException("Every leakage sentinel must declare a channel and a value.");
        }

        foreach (string channel in data.RequiredChannels)
        {
            if (!data.Sentinels.Any(sentinel => string.Equals(sentinel.Channel, channel, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException($"Leakage corpus required channel '{channel}' has zero sentinels — the gate would not scan it.");
            }
        }
    }

    private sealed record CorpusData(
        [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
        [property: JsonPropertyName("boundTenant")] string BoundTenant,
        [property: JsonPropertyName("foreignTenant")] string ForeignTenant,
        [property: JsonPropertyName("identifiers")] CorpusIdentifiers Identifiers,
        [property: JsonPropertyName("requiredChannels")] IReadOnlyList<string> RequiredChannels,
        [property: JsonPropertyName("sentinels")] IReadOnlyList<LeakageSentinel> Sentinels);

    private sealed record CorpusIdentifiers(
        [property: JsonPropertyName("foreignNoteId")] string ForeignNoteId,
        [property: JsonPropertyName("foreignOperationId")] string ForeignOperationId,
        [property: JsonPropertyName("ownNoteId")] string OwnNoteId,
        [property: JsonPropertyName("ownOperationId")] string OwnOperationId,
        [property: JsonPropertyName("unknownId")] string UnknownId);
}
