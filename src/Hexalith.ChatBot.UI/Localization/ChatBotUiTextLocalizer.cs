using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.UI.Design;

using Microsoft.Extensions.Localization;

namespace Hexalith.ChatBot.UI.Localization;

/// <summary>
/// Typed phrase-level localizer for governed ChatBot UI surfaces.
/// </summary>
public sealed class ChatBotUiTextLocalizer(IStringLocalizer<SharedResource> localizer)
{
    public string this[string key] => Get(key);

    public string Get(string key, params object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        LocalizedString value = arguments.Length == 0 ? localizer[key] : localizer[key, arguments];
        if (value.ResourceNotFound)
        {
            throw new InvalidOperationException($"Missing ChatBot UI localization resource '{key}'.");
        }

        return value.Value;
    }

    public string ActorCategoryLabel(ChatBotActorCategory category)
        => Get(ChatBotGovernedUiText.GetActorCategoryResourceKey(category));

    public string ActorBadgeAccessibleLabel(ChatBotActorCategory category, string displayLabel)
        => Get(ChatBotUiTextKey.ActorBadgeAccessibleLabel, ActorCategoryLabel(category), displayLabel);

    public string ActorBadgeResolveAccessibleLabel(ChatBotActorCategory category, string displayLabel)
        => Get(ChatBotUiTextKey.ActorBadgeResolveAccessibleLabel, ActorCategoryLabel(category), displayLabel);

    public string ActorTypeLabel(ActorType actorType)
        => Get(actorType switch
        {
            ActorType.Human => ChatBotUiTextKey.ActorLabelHuman,
            ActorType.Ai => ChatBotUiTextKey.ActorLabelAi,
            ActorType.Service => ChatBotUiTextKey.ActorLabelService,
            ActorType.System => ChatBotUiTextKey.ActorLabelSystem,
            _ => throw new ArgumentOutOfRangeException(nameof(actorType), actorType, null),
        });

    public string BlockedReasonLabel(ChatBotBlockedReason reason)
        => Get(ChatBotGovernedUiText.GetBlockedReasonResourceKey(reason));

    public string BlockedStateAccessibleLabel(ChatBotBlockedReason reason, string reasonText, string safeNextAction)
        => Get(ChatBotUiTextKey.BlockedStateAccessibleLabel, BlockedReasonLabel(reason), reasonText, safeNextAction);

    public string ConfidenceBandLabel(ThresholdBand band)
        => Get(band switch
        {
            ThresholdBand.Below => ChatBotUiTextKey.ConfidenceBandBelow,
            ThresholdBand.Within => ChatBotUiTextKey.ConfidenceBandWithin,
            ThresholdBand.Above => ChatBotUiTextKey.ConfidenceBandAbove,
            ThresholdBand.Critical => ChatBotUiTextKey.ConfidenceBandCritical,
            _ => throw new ArgumentOutOfRangeException(nameof(band), band, null),
        });

    public string EvidenceStateLabel(ChatBotEvidenceState state)
        => Get(ChatBotGovernedUiText.GetEvidenceStateResourceKey(state));

    public string EvidenceAccessibleLabel(ChatBotEvidenceState state, string label, string? reason)
        => string.IsNullOrWhiteSpace(reason)
            ? Get(ChatBotUiTextKey.EvidenceAccessibleLabel, EvidenceStateLabel(state), label)
            : Get(ChatBotUiTextKey.EvidenceAccessibleLabelWithReason, EvidenceStateLabel(state), label, reason);

    public string FeedbackKindLabel(ChatBotFeedbackKind kind)
        => Get(ChatBotGovernedUiText.GetFeedbackKindResourceKey(kind));

    public string RiskActionClassLabel(ChatBotRiskActionClass riskClass)
        => Get(ChatBotGovernedUiText.GetRiskActionClassResourceKey(riskClass));

    public string RiskAccessibleLabel(ChatBotRiskActionClass riskClass, string policyReason)
        => Get(ChatBotUiTextKey.RiskAccessibleLabel, RiskActionClassLabel(riskClass), policyReason);

    public string StatusAccessibleLabel(ChatBotFeedbackKind kind, string message)
        => Get(ChatBotUiTextKey.StatusAccessibleLabel, FeedbackKindLabel(kind), message);

    public string WhyUnavailableAccessibleLabel(string disabledReason)
        => Get(ChatBotUiTextKey.WhyUnavailableAccessible, disabledReason);
}
