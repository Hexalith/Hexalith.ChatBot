namespace Hexalith.ChatBot.UI.Services;

/// <summary>Tracks which association-review failure has taken focus in this circuit.</summary>
public sealed class AssociationReviewFocusState
{
    private long _latestActivation = -1;

    /// <summary>Claims the page focus for a failure activation once.</summary>
    internal bool TryClaimFocus(long activation)
    {
        if (activation <= _latestActivation)
        {
            return false;
        }

        _latestActivation = activation;
        return true;
    }
}
