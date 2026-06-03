using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// Server-side seam onto the NFR42a published-SLO catalog (Story 8.3). The canonical token data is the single
/// source of truth in <see cref="OperatingBaselineCatalog"/> (in <c>.Contracts</c>, so both this projector seam and
/// the UI placeholder consume one list — the UI never references <c>.Server</c>). This provider is where the
/// dashboard projector reads the static catalog before layering each SLO's live, fail-safe error-budget burn.
/// </summary>
internal static class OperatingBaselineCatalogProvider
{
    /// <summary>Returns the static published-SLO catalog (each entry with its default fail-safe Unknown burn).</summary>
    public static IReadOnlyList<PublishedSlo> GetCatalog() => OperatingBaselineCatalog.Published;
}
