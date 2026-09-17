using Hexalith.ChatBot.UI.Design;

namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed record AssociationReviewActionModel(
    string Code,
    string Label,
    string Consequence,
    ChatBotGovernedActionState State,
    string DisabledReason);
