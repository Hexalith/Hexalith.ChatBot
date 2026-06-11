using Hexalith.ChatBot.Server.Association;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal interface ICorrectionPropagationActivityCatalog
{
    IReadOnlyList<string> Scope { get; }

    CorrectionPropagationScope SloScope { get; }

    bool IsReady { get; }

    bool TryGet(string storeKey, out ICorrectionPropagationStoreActivity activity);
}

internal sealed class CorrectionPropagationActivityCatalog(IEnumerable<ICorrectionPropagationStoreActivity> activities)
    : ICorrectionPropagationActivityCatalog
{
    private readonly IReadOnlyDictionary<string, ICorrectionPropagationStoreActivity> _activities =
        activities.ToDictionary(static activity => activity.StoreKey, StringComparer.Ordinal);

    public IReadOnlyList<string> Scope => _activities.ContainsKey(CorrectionPropagationStoreKeys.VectorReindex)
        ? CorrectionPropagationStoreKeys.RequiredM2
        : CorrectionPropagationStoreKeys.RequiredM0;

    public CorrectionPropagationScope SloScope => _activities.ContainsKey(CorrectionPropagationStoreKeys.VectorReindex)
        ? CorrectionPropagationScope.M2
        : CorrectionPropagationScope.M0M1;

    public bool IsReady => Scope.All(_activities.ContainsKey);

    public bool TryGet(string storeKey, out ICorrectionPropagationStoreActivity activity)
        => _activities.TryGetValue(storeKey, out activity!);
}
