namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectAssociationWhyEvidenceModel(
    string Kind,
    string? SignalClass,
    string DisplayToken,
    string Fingerprint,
    string Reference,
    string VisibilityState,
    string RedactionState,
    string FreshnessState,
    double? ConfidenceContribution);
