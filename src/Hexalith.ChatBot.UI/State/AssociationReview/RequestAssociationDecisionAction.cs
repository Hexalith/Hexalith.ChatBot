namespace Hexalith.ChatBot.UI.State.AssociationReview;

/// <summary>
/// Asks the surface to confirm a decision before anything durable happens. This action alone never submits a
/// command; it only opens the confirmation for <paramref name="DecisionCode"/>. The durable submit runs only
/// on <see cref="ConfirmAssociationDecisionAction"/>.
/// </summary>
public sealed record RequestAssociationDecisionAction(string DecisionCode);
