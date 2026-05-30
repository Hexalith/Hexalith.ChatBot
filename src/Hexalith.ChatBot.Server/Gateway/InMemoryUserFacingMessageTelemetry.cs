namespace Hexalith.ChatBot.Server.Gateway;

internal sealed class InMemoryUserFacingMessageTelemetry : IUserFacingMessageTelemetry
{
    private readonly object _sync = new();
    private readonly Dictionary<(string CatalogVersion, string FallbackCode), int> _counts = [];

    public IReadOnlyDictionary<(string CatalogVersion, string FallbackCode), int> Counts
    {
        get
        {
            lock (_sync)
            {
                return new Dictionary<(string CatalogVersion, string FallbackCode), int>(_counts);
            }
        }
    }

    public void RecordUncategorizedMessage(string catalogVersion, string fallbackCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackCode);

        lock (_sync)
        {
            (string CatalogVersion, string FallbackCode) key = (catalogVersion, fallbackCode);
            _counts[key] = _counts.GetValueOrDefault(key) + 1;
        }
    }
}
