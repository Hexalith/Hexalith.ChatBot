namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Recovery flows governed by the UX-DR40 foundation.
/// </summary>
public enum ChatBotRecoveryFlow
{
    AssociationReview,
    AiActionReview,
    QueueRetry,
    Correction,
    TenantConfiguration,
}
