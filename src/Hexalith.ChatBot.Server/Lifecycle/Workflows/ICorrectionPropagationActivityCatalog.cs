using Hexalith.ChatBot.Server.Association;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal interface ICorrectionPropagationActivityCatalog
{
    IReadOnlyList<string> Scope { get; }

    CorrectionPropagationScope SloScope { get; }

    bool IsReady { get; }

    bool TryGet(string storeKey, out ICorrectionPropagationStoreActivity activity);
}
