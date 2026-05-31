namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Disabled-action explanation metadata for governed UI controls that must remain discoverable.
/// </summary>
/// <param name="ActionName">Accessible action name.</param>
/// <param name="DisabledReasonId">Stable id for the disabled reason element.</param>
/// <param name="DisabledReasonLabel">Accessible disabled reason text.</param>
/// <param name="UsesAriaDisabled">Whether the control exposes <c>aria-disabled</c>.</param>
/// <param name="KeepsKeyboardFocusOrder">Whether the unavailable control remains keyboard discoverable.</param>
/// <param name="ReferencesReachableReason">Whether the control links to a reachable reason.</param>
/// <param name="SuppressesActivationWhenDisabled">Whether activation fails closed while disabled.</param>
/// <param name="UsesTooltipOnlyReason">Whether the explanation is only exposed through a tooltip.</param>
public sealed record ChatBotDisabledActionContract(
    string ActionName,
    string DisabledReasonId,
    string DisabledReasonLabel,
    bool UsesAriaDisabled,
    bool KeepsKeyboardFocusOrder,
    bool ReferencesReachableReason,
    bool SuppressesActivationWhenDisabled,
    bool UsesTooltipOnlyReason)
{
    /// <summary>Gets a value indicating whether the disabled-action accessibility behavior is fully specified.</summary>
    public bool IsComplete
        => !string.IsNullOrWhiteSpace(ActionName)
            && !string.IsNullOrWhiteSpace(DisabledReasonId)
            && !string.IsNullOrWhiteSpace(DisabledReasonLabel)
            && UsesAriaDisabled
            && KeepsKeyboardFocusOrder
            && ReferencesReachableReason
            && SuppressesActivationWhenDisabled
            && !UsesTooltipOnlyReason;

    /// <summary>Creates the default disabled-action contract for a governed control.</summary>
    /// <param name="actionName">Accessible action name.</param>
    /// <param name="disabledReasonId">Stable id for the disabled reason element.</param>
    /// <param name="disabledReasonLabel">Accessible disabled reason text.</param>
    /// <returns>A disabled-action contract.</returns>
    public static ChatBotDisabledActionContract CreateGovernedAction(
        string actionName,
        string disabledReasonId,
        string disabledReasonLabel)
        => new(
            actionName,
            disabledReasonId,
            disabledReasonLabel,
            UsesAriaDisabled: true,
            KeepsKeyboardFocusOrder: true,
            ReferencesReachableReason: true,
            SuppressesActivationWhenDisabled: true,
            UsesTooltipOnlyReason: false);
}
