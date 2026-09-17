namespace Hexalith.ChatBot.UI.State.AssociationReview;

/// <summary>Identity of the last accepted governed command, so the surface can show what it accepted.</summary>
public sealed record AssociationOperationIdentity(
    string CommandId,
    string CorrelationId,
    string? TaskId,
    string LifecycleState);
