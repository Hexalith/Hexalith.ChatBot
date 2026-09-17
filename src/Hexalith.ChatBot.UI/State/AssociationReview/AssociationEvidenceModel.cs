using Hexalith.ChatBot.UI.Design;

namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed record AssociationEvidenceModel(
    string Reference,
    string Fingerprint,
    string Kind,
    ChatBotEvidenceState State,
    string UnavailableReason);
