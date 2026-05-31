namespace Hexalith.ChatBot.Conformance.Tests.Harness;

/// <summary>
/// Thrown when a rendered artifact (problem, captured outcome, audit failure fact, status/projection response,
/// or test diagnostic) contains a leakage sentinel. The message names the persona, the channel, and the matched
/// sentinel CLASS and token, but intentionally never dumps the offending artifact body (which is what the gate
/// exists to keep metadata-only).
/// </summary>
internal sealed class CrossTenantLeakageException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="CrossTenantLeakageException"/> class.</summary>
    /// <param name="persona">The persona whose artifact leaked.</param>
    /// <param name="channelLabel">The rendered-artifact channel label.</param>
    /// <param name="sentinel">The matched sentinel.</param>
    public CrossTenantLeakageException(string persona, string channelLabel, LeakageSentinel sentinel)
        : base($"Cross-tenant leakage: persona '{persona}' leaked a '{sentinel?.Channel}'-class sentinel " +
               $"('{sentinel?.Value}') through the '{channelLabel}' channel. (Artifact body withheld.)")
    {
        Persona = persona;
        ChannelLabel = channelLabel;
        SentinelChannel = sentinel?.Channel ?? string.Empty;
        SentinelValue = sentinel?.Value ?? string.Empty;
    }

    /// <summary>The persona whose artifact leaked.</summary>
    public string Persona { get; }

    /// <summary>The rendered-artifact channel label.</summary>
    public string ChannelLabel { get; }

    /// <summary>The matched sentinel class.</summary>
    public string SentinelChannel { get; }

    /// <summary>The matched sentinel token.</summary>
    public string SentinelValue { get; }
}

/// <summary>
/// The shared cross-tenant leakage scanner. Every negative case routes its rendered artifacts through this one
/// gate so a future Epic 2/3 endpoint reuses it rather than creating a parallel test style. The scanner is
/// itself guarded against vacuity: scanning with an empty sentinel set throws (a no-op scan would silently pass).
/// </summary>
internal static class CrossTenantLeakageScanner
{
    /// <summary>
    /// Scans a rendered artifact for any of the supplied sentinels, failing with the persona/channel and the
    /// matched sentinel class on the first hit.
    /// </summary>
    /// <param name="persona">The persona label.</param>
    /// <param name="channelLabel">The rendered-artifact channel label.</param>
    /// <param name="renderedArtifact">The rendered artifact string to scan.</param>
    /// <param name="sentinels">The sentinels to scan for (must be non-empty).</param>
    public static void Scan(string persona, string channelLabel, string renderedArtifact, IEnumerable<LeakageSentinel> sentinels)
    {
        ArgumentNullException.ThrowIfNull(persona);
        ArgumentNullException.ThrowIfNull(channelLabel);
        ArgumentNullException.ThrowIfNull(renderedArtifact);
        ArgumentNullException.ThrowIfNull(sentinels);

        List<LeakageSentinel> materialized = [.. sentinels];
        if (materialized.Count == 0)
        {
            // Vacuity guard: a scan with no sentinels would pass on ANY body, including a fully leaking one.
            throw new InvalidOperationException(
                $"Leakage scan for persona '{persona}' channel '{channelLabel}' was invoked with no sentinels — a no-op scan would vacuously pass.");
        }

        foreach (LeakageSentinel sentinel in materialized)
        {
            if (renderedArtifact.Contains(sentinel.Value, StringComparison.OrdinalIgnoreCase))
            {
                throw new CrossTenantLeakageException(persona, channelLabel, sentinel);
            }
        }
    }

    /// <summary>Scans a rendered artifact against the entire corpus sentinel set.</summary>
    /// <param name="persona">The persona label.</param>
    /// <param name="channelLabel">The rendered-artifact channel label.</param>
    /// <param name="renderedArtifact">The rendered artifact string to scan.</param>
    public static void ScanAll(string persona, string channelLabel, string renderedArtifact)
        => Scan(persona, channelLabel, renderedArtifact, CrossTenantLeakageCorpus.Sentinels);
}
